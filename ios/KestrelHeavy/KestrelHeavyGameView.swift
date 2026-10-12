import GameKit
import SwiftUI
import UIKit
import WebKit

/// WKWebView shell for the packed Phaser 4 game.
/// Loads `kestrel-heavy://game/index.html` through `KestrelHeavySchemeHandler`
/// so ES modules, vendor/phaser.min.js, and audio/ share one origin.
/// Hosted in a portrait-locked view controller so the cabinet cannot rotate
/// into the landscape pillarbox Trevor saw on TestFlight 1.0 (2).
struct KestrelHeavyGameView: UIViewControllerRepresentable {
    var onCabinetReady: () -> Void = {}

    static func packAvailable() -> Bool {
        KestrelHeavyOrigin.packRoot() != nil
    }

    func makeCoordinator() -> Coordinator {
        Coordinator(onCabinetReady: onCabinetReady)
    }

    func makeUIViewController(context: Context) -> KestrelHeavyCabinetController {
        let config = WKWebViewConfiguration()
        config.allowsInlineMediaPlayback = true
        config.mediaTypesRequiringUserActionForPlayback = []
        config.allowsAirPlayForMediaPlayback = false
        config.allowsPictureInPictureMediaPlayback = false
        config.suppressesIncrementalRendering = false
        config.dataDetectorTypes = []
        config.setURLSchemeHandler(context.coordinator.schemeHandler, forURLScheme: KestrelHeavyOrigin.scheme)

        let page = WKWebpagePreferences()
        page.allowsContentJavaScript = true
        page.preferredContentMode = .mobile
        config.defaultWebpagePreferences = page

        let controller = WKUserContentController()
        controller.addUserScript(WKUserScript(
            source: Self.bootScript,
            injectionTime: .atDocumentStart,
            forMainFrameOnly: true
        ))
        controller.add(context.coordinator, name: "kestrelHaptics")
        controller.add(context.coordinator, name: "kestrelGameCenter")
        config.userContentController = controller

        let webView = ArcadeWebView(frame: .zero, configuration: config)
        webView.navigationDelegate = context.coordinator
        webView.uiDelegate = context.coordinator
        webView.isOpaque = true
        webView.backgroundColor = UIColor(red: 5 / 255, green: 5 / 255, blue: 13 / 255, alpha: 1)
        webView.scrollView.backgroundColor = webView.backgroundColor
        webView.scrollView.bounces = false
        webView.scrollView.alwaysBounceVertical = false
        webView.scrollView.alwaysBounceHorizontal = false
        webView.scrollView.isScrollEnabled = false
        webView.scrollView.showsVerticalScrollIndicator = false
        webView.scrollView.showsHorizontalScrollIndicator = false
        webView.scrollView.contentInsetAdjustmentBehavior = .never
        webView.scrollView.pinchGestureRecognizer?.isEnabled = false
        webView.allowsBackForwardNavigationGestures = false
        webView.allowsLinkPreview = false
        #if DEBUG
        if #available(iOS 16.4, *) {
            webView.isInspectable = true
        }
        #endif

        let cabinet = KestrelHeavyCabinetController(webView: webView)
        context.coordinator.loadCabinet(webView)
        return cabinet
    }

    func updateUIViewController(_ cabinet: KestrelHeavyCabinetController, context: Context) {
        context.coordinator.onCabinetReady = onCabinetReady
        if cabinet.webView.url == nil {
            context.coordinator.loadCabinet(cabinet.webView)
        }
        cabinet.webView.publishViewportIfNeeded()
        KestrelHeavyOrientation.lockScene()
    }

    static func dismantleUIViewController(_ cabinet: KestrelHeavyCabinetController, coordinator: Coordinator) {
        cabinet.webView.configuration.userContentController.removeScriptMessageHandler(forName: "kestrelHaptics")
        cabinet.webView.configuration.userContentController.removeScriptMessageHandler(forName: "kestrelGameCenter")
    }

    private static let bootScript = """
    document.documentElement.classList.add('is-ios-app');
    document.documentElement.style.webkitUserSelect = 'none';
    document.documentElement.style.webkitTouchCallout = 'none';
    document.addEventListener('gesturestart', function (e) { e.preventDefault(); }, { passive: false });
    """

    final class Coordinator: NSObject, WKNavigationDelegate, WKUIDelegate, WKScriptMessageHandler {
        let schemeHandler = KestrelHeavySchemeHandler()
        var onCabinetReady: () -> Void
        private let light = UIImpactFeedbackGenerator(style: .light)
        private let medium = UIImpactFeedbackGenerator(style: .medium)
        private var didAnnounceReady = false

        init(onCabinetReady: @escaping () -> Void) {
            self.onCabinetReady = onCabinetReady
        }

        func loadCabinet(_ webView: WKWebView) {
            webView.load(URLRequest(url: KestrelHeavyOrigin.index))
        }

        func webViewWebContentProcessDidTerminate(_ webView: WKWebView) {
            didAnnounceReady = false
            loadCabinet(webView)
        }

        func webView(
            _ webView: WKWebView,
            decidePolicyFor navigationAction: WKNavigationAction,
            decisionHandler: @escaping (WKNavigationActionPolicy) -> Void
        ) {
            guard let url = navigationAction.request.url else {
                decisionHandler(.cancel)
                return
            }
            let scheme = url.scheme?.lowercased() ?? ""
            if scheme == KestrelHeavyOrigin.scheme || scheme == "about" || scheme == "blob" {
                decisionHandler(.allow)
                return
            }
            decisionHandler(.cancel)
        }

        func webView(_ webView: WKWebView, didFinish navigation: WKNavigation!) {
            webView.becomeFirstResponder()
            (webView as? ArcadeWebView)?.publishViewportIfNeeded(force: true)
            announceReady()
            KestrelHeavyGameCenter.shared.authenticate()
        }

        func webView(
            _ webView: WKWebView,
            contextMenuConfigurationForElement elementInfo: WKContextMenuElementInfo,
            completionHandler: @escaping (UIContextMenuConfiguration?) -> Void
        ) {
            completionHandler(nil)
        }

        func userContentController(
            _ userContentController: WKUserContentController,
            didReceive message: WKScriptMessage
        ) {
            if message.name == "kestrelGameCenter" {
                KestrelHeavyGameCenter.shared.handle(message.body)
                return
            }
            guard message.name == "kestrelHaptics" else { return }
            light.prepare()
            medium.prepare()
            fire(pattern: message.body)
        }

        private func announceReady() {
            guard !didAnnounceReady else { return }
            didAnnounceReady = true
            // Keep the brand frame up just long enough to match LaunchScreen.
            DispatchQueue.main.asyncAfter(deadline: .now() + 0.45, execute: onCabinetReady)
        }

        private func fire(pattern: Any) {
            if let value = pattern as? Int {
                impact(milliseconds: value)
                return
            }
            if let value = pattern as? Double {
                impact(milliseconds: Int(value))
                return
            }
            if let values = pattern as? [Int] {
                var delay: TimeInterval = 0
                for (index, value) in values.enumerated() {
                    if index % 2 == 0 {
                        DispatchQueue.main.asyncAfter(deadline: .now() + delay) { [weak self] in
                            self?.impact(milliseconds: value)
                        }
                    }
                    delay += TimeInterval(max(value, 0)) / 1000
                }
                return
            }
            if let values = pattern as? [Any] {
                fire(pattern: values.compactMap { $0 as? Int })
            }
        }

        private func impact(milliseconds: Int) {
            if milliseconds >= 40 {
                medium.impactOccurred()
            } else {
                light.impactOccurred()
            }
        }
    }
}

/// Owns the WKWebView and refuses landscape so Phaser cannot pillarbox.
final class KestrelHeavyCabinetController: UIViewController {
    let webView: ArcadeWebView

    init(webView: ArcadeWebView) {
        self.webView = webView
        super.init(nibName: nil, bundle: nil)
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    override var supportedInterfaceOrientations: UIInterfaceOrientationMask {
        KestrelHeavyOrientation.mask
    }

    override var preferredInterfaceOrientationForPresentation: UIInterfaceOrientation { .portrait }

    override var shouldAutorotate: Bool { false }

    override func viewDidLoad() {
        super.viewDidLoad()
        view.backgroundColor = UIColor(red: 5 / 255, green: 5 / 255, blue: 13 / 255, alpha: 1)
        webView.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(webView)
        NSLayoutConstraint.activate([
            webView.topAnchor.constraint(equalTo: view.topAnchor),
            webView.leadingAnchor.constraint(equalTo: view.leadingAnchor),
            webView.trailingAnchor.constraint(equalTo: view.trailingAnchor),
            webView.bottomAnchor.constraint(equalTo: view.bottomAnchor),
        ])
    }

    override func viewWillAppear(_ animated: Bool) {
        super.viewWillAppear(animated)
        KestrelHeavyOrientation.lockScene()
    }

    override func viewDidLayoutSubviews() {
        super.viewDidLayoutSubviews()
        webView.publishViewportIfNeeded()
    }
}

/// Keeps hardware-keyboard / iPad key events flowing into Phaser.
final class ArcadeWebView: WKWebView {
    private var lastPublished = CGSize.zero

    override var canBecomeFirstResponder: Bool { true }

    override func layoutSubviews() {
        super.layoutSubviews()
        publishViewportIfNeeded()
    }

    /// Push the real WKWebView point size into the cabinet so Phaser ENVELOP
    /// covers the shell instead of a stale 16:9 visualViewport letterbox.
    func publishViewportIfNeeded(force: Bool = false) {
        let size = bounds.size
        guard size.width > 1, size.height > 1 else { return }
        if !force, size == lastPublished { return }
        lastPublished = size
        let width = Int(size.width.rounded())
        let height = Int(size.height.rounded())
        let script = """
        (function () {
          var w = \(width);
          var h = \(height);
          if (!(w > 0 && h > 0)) return;
          var root = document.documentElement;
          if (!root) return;
          root.style.setProperty('--game-vv-top', '0px');
          root.style.setProperty('--game-vv-left', '0px');
          root.style.setProperty('--game-vv-width', w + 'px');
          root.style.setProperty('--game-vv-height', h + 'px');
          var nodes = [root, document.body, document.querySelector('.cabinet'), document.getElementById('arcade-fs-wrapper')];
          for (var i = 0; i < nodes.length; i++) {
            var n = nodes[i];
            if (!n) continue;
            n.style.width = w + 'px';
            n.style.height = h + 'px';
            n.style.minHeight = h + 'px';
            n.style.maxWidth = 'none';
            n.style.maxHeight = 'none';
          }
          if (document.body) document.body.classList.add('arcade-host-fill', 'is-ios-app');
          try { window.dispatchEvent(new Event('resize')); } catch (e) {}
        })();
        """
        evaluateJavaScript(script, completionHandler: nil)
    }
}

/// Game Center leaderboards, one per KH-n flight.
/// The cabinet posts `{ type: "score", leaderboard, score }` on the
/// `kestrelGameCenter` message handler at the end of a flight. Everything here
/// fails silently: offline, signed out, or leaderboard not yet live in App
/// Store Connect all just drop the score (a few are queued until sign-in).
final class KestrelHeavyGameCenter {
    static let shared = KestrelHeavyGameCenter()
    static let leaderboardPrefix = "com.beckify.kestrelheavy.lb."

    private var authStarted = false
    private var pending: [(id: String, score: Int)] = []

    func authenticate() {
        DispatchQueue.main.async {
            guard !self.authStarted else { return }
            self.authStarted = true
            GKLocalPlayer.local.authenticateHandler = { [weak self] viewController, error in
                DispatchQueue.main.async {
                    if let viewController {
                        Self.topViewController()?.present(viewController, animated: true)
                        return
                    }
                    guard error == nil, GKLocalPlayer.local.isAuthenticated else { return }
                    self?.flush()
                }
            }
        }
    }

    func handle(_ body: Any) {
        guard let dict = body as? [String: Any], let type = dict["type"] as? String else { return }
        let id = dict["leaderboard"] as? String ?? ""
        switch type {
        case "score":
            let score = (dict["score"] as? NSNumber)?.intValue ?? 0
            submit(score: score, leaderboardID: id)
        case "show":
            show(leaderboardID: id)
        default:
            return
        }
    }

    private func submit(score: Int, leaderboardID: String) {
        guard score > 0, leaderboardID.hasPrefix(Self.leaderboardPrefix) else { return }
        DispatchQueue.main.async {
            guard GKLocalPlayer.local.isAuthenticated else {
                self.pending.append((leaderboardID, score))
                if self.pending.count > 20 { self.pending.removeFirst(self.pending.count - 20) }
                self.authenticate()
                return
            }
            GKLeaderboard.submitScore(
                score,
                context: 0,
                player: GKLocalPlayer.local,
                leaderboardIDs: [leaderboardID]
            ) { _ in }
        }
    }

    private func flush() {
        let queued = pending
        pending.removeAll()
        for item in queued {
            submit(score: item.score, leaderboardID: item.id)
        }
    }

    private func show(leaderboardID: String) {
        DispatchQueue.main.async {
            guard GKLocalPlayer.local.isAuthenticated else { return }
            let sheet: GKGameCenterViewController
            if leaderboardID.hasPrefix(Self.leaderboardPrefix) {
                sheet = GKGameCenterViewController(
                    leaderboardID: leaderboardID,
                    playerScope: .global,
                    timeScope: .allTime
                )
            } else {
                sheet = GKGameCenterViewController(state: .leaderboards)
            }
            sheet.gameCenterDelegate = GameCenterDismisser.shared
            Self.topViewController()?.present(sheet, animated: true)
        }
    }

    static func topViewController() -> UIViewController? {
        let scenes = UIApplication.shared.connectedScenes.compactMap { $0 as? UIWindowScene }
        let window = scenes.flatMap(\.windows).first(where: { $0.isKeyWindow }) ?? scenes.first?.windows.first
        var top = window?.rootViewController
        while let next = top?.presentedViewController {
            top = next
        }
        return top
    }
}

private final class GameCenterDismisser: NSObject, GKGameCenterControllerDelegate {
    static let shared = GameCenterDismisser()

    func gameCenterViewControllerDidFinish(_ gameCenterViewController: GKGameCenterViewController) {
        gameCenterViewController.dismiss(animated: true)
    }
}
