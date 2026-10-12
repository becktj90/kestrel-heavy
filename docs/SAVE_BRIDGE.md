# Save bridge — proposal (not applied)

Status: **proposal only.** The JS side (`web/js/save/store.js`, `web/js/save/bridge.js`) is merged and works today on localStorage alone. Nothing below has been applied to Swift or the Xcode project; it needs Trevor's OK first (it is an Xcode project edit).

## Protocol

JS posts `window.webkit.messageHandlers.kestrelSave.postMessage({op, key, value})`:

| op | key | value | reply |
|---|---|---|---|
| `put` | save key (`kh.save.v1.…`) | JSON string | `true` |
| `get` | save key | `null` | JSON string or `null` |
| `delete` | save key | `null` | `true` |
| `list` | key prefix | `null` | `[String]` of keys |

Registering with `WKScriptMessageHandlerWithReply` makes `postMessage` return a Promise (iOS 14+). With a legacy handler, JS treats it as fire-and-forget mirroring (`hydrate()` then does nothing). localStorage stays the working copy either way.

Keys are restricted to `[A-Za-z0-9._-]` and the `kh.save.` prefix; values are capped at 1 MB.

## 1. New file `ios/KestrelHeavy/KestrelHeavySaveStore.swift`

```swift
import Foundation
import WebKit

/// Durable on-disk mirror for the web save store (Application Support, excluded
/// from nothing — backed up by iCloud like other app data). Survives WKWebView
/// localStorage eviction and app updates.
final class KestrelHeavySaveStore: NSObject, WKScriptMessageHandlerWithReply {
    static let handlerName = "kestrelSave"
    private static let maxBytes = 1_000_000
    private let queue = DispatchQueue(label: "com.beckify.kestrelheavy.saves")
    private let dir: URL

    override init() {
        let base = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
        dir = base.appendingPathComponent("KestrelSaves", isDirectory: true)
        try? FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
        super.init()
    }

    private func url(for key: String) -> URL? {
        guard key.hasPrefix("kh.save."), key.count <= 200,
              key.range(of: "^[A-Za-z0-9._-]+$", options: .regularExpression) != nil else { return nil }
        return dir.appendingPathComponent(key + ".json")
    }

    func userContentController(
        _ userContentController: WKUserContentController,
        didReceive message: WKScriptMessage,
        replyHandler: @escaping (Any?, String?) -> Void
    ) {
        guard let body = message.body as? [String: Any],
              let op = body["op"] as? String,
              let key = body["key"] as? String else {
            replyHandler(nil, "bad message"); return
        }
        let value = body["value"] as? String
        queue.async {
            let (result, error) = self.perform(op: op, key: key, value: value)
            DispatchQueue.main.async { replyHandler(result, error) }
        }
    }

    private func perform(op: String, key: String, value: String?) -> (Any?, String?) {
        switch op {
        case "list":
            let names = (try? FileManager.default.contentsOfDirectory(atPath: dir.path)) ?? []
            let keys = names.filter { $0.hasSuffix(".json") }
                .map { String($0.dropLast(5)) }
                .filter { $0.hasPrefix(key) }
            return (keys, nil)
        case "get":
            guard let u = url(for: key) else { return (nil, "bad key") }
            return ((try? String(contentsOf: u, encoding: .utf8)) as Any?, nil)
        case "put":
            guard let u = url(for: key) else { return (nil, "bad key") }
            guard let v = value, let data = v.data(using: .utf8), data.count <= Self.maxBytes else {
                return (nil, "bad value")
            }
            do { try data.write(to: u, options: [.atomic]); return (true, nil) }   // temp file + rename
            catch { return (nil, error.localizedDescription) }
        case "delete":
            guard let u = url(for: key) else { return (nil, "bad key") }
            try? FileManager.default.removeItem(at: u)
            return (true, nil)
        default:
            return (nil, "unknown op")
        }
    }
}
```

## 2. `ios/KestrelHeavy/KestrelHeavyGameView.swift`

Add a stored store on the Coordinator (or a static), and register it next to the existing handlers in `makeUIView`:

```swift
        controller.add(context.coordinator, name: "kestrelHaptics")
        controller.add(context.coordinator, name: "kestrelGameCenter")
+       controller.addScriptMessageHandler(
+           context.coordinator.saveStore,
+           contentWorld: .page,
+           name: KestrelHeavySaveStore.handlerName
+       )
        config.userContentController = controller
```

```swift
    final class Coordinator: ... {
+       let saveStore = KestrelHeavySaveStore()
```

And in teardown, next to the other `removeScriptMessageHandler` calls:

```swift
+       cabinet.webView.configuration.userContentController
+           .removeScriptMessageHandler(forName: KestrelHeavySaveStore.handlerName, contentWorld: .page)
```

No change to the existing `userContentController(_:didReceive:)` — the save handler is a separate object. Do **not** touch `.supportedInterfaceOrientations` (removed for the Xcode 16.4 build).

## 3. `ios/KestrelHeavy.xcodeproj/project.pbxproj`

Four insertions, using unused IDs `KHCF…0040` / `KHCF…0041` (check they are still free):

```
/* PBXBuildFile section */
		KHCF00000000000000000041 /* KestrelHeavySaveStore.swift in Sources */ = {isa = PBXBuildFile; fileRef = KHCF00000000000000000040 /* KestrelHeavySaveStore.swift */; };
/* PBXFileReference section */
		KHCF00000000000000000040 /* KestrelHeavySaveStore.swift */ = {isa = PBXFileReference; lastKnownFileType = sourcecode.swift; path = KestrelHeavySaveStore.swift; sourceTree = "<group>"; };
/* KestrelHeavy group children (after KestrelHeavySchemeHandler.swift) */
				KHCF00000000000000000040 /* KestrelHeavySaveStore.swift */,
/* PBXSourcesBuildPhase files (after KestrelHeavySchemeHandler.swift in Sources) */
				KHCF00000000000000000041 /* KestrelHeavySaveStore.swift in Sources */,
```

No entitlement, Info.plist, or privacy-manifest change is needed (local files only, no new required-reason API).

## JS usage

```js
import { createStore } from './save/store.js';
const store = createStore();          // auto-detects kestrelSave, else localStorage
await store.hydrate();                // on boot: restore evicted saves from native
const { id } = store.designs.put(design, { name: 'Scout' });
```
