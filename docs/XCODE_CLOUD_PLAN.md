# Xcode Cloud plan (Kestrel Heavy 2.0.0)

> **Active again 2026-10-12 (DECISIONS #59, reverses #53):** Trevor does the one-time Mac setup (docs/XCODE_CLOUD_FIRST_SETUP.md). The export branch is `xcode-export` (read `ios-export` below as `xcode-export`).

Status: **PLAN ONLY.** No builds, no App Store Connect (ASC) changes, no signing changes, no workflow files,
no Unity Build Automation (UBA) runs until Trevor says "ready". Decision: DECISIONS #52.

Pipeline: **Unity CLI on the box (Linux) → Xcode project export → committed to `ios-export` branch → Xcode Cloud
(manual start) → TestFlight / App Store.** Budget $0: Xcode Cloud includes 25 compute hours/month with the
Apple Developer Program ([Apple: Xcode Cloud](https://developer.apple.com/xcode-cloud/),
[Apple news, Dec 2023](https://developer.apple.com/news/?id=ik9z4ll6)).

## 0. Key findings (read first)

1. **The very first Xcode Cloud setup for an app needs Xcode on a Mac.** Apple: "You need to use Xcode to initially
   configure your project or workspace to use Xcode Cloud. After you've started your first build, you can manage
   workflows in either Xcode or App Store Connect."
   ([workflow reference](https://developer.apple.com/documentation/xcode/xcode-cloud-workflow-reference),
   [first workflow](https://developer.apple.com/documentation/xcode/configuring-your-first-xcode-cloud-workflow),
   [start conditions](https://developer.apple.com/documentation/xcode/configuring-start-conditions)).
   The ASC API can list products and create/edit workflows and start builds, but it cannot do the initial
   product + repository onboarding. This conflicts with "Trevor does no Mac setup"; see §7a for options.
2. Manual-only is supported: a **Manual Start** start condition (Xcode 15.1+), with every other start condition
   removed. Builds started from ASC or the ASC API count as manual and need a matching Manual Start condition
   ([WWDC24 "Extend your Xcode Cloud workflows"](https://www.youtube.com/watch?v=f__0R-W9H6k)).
   **Xcode proposes a Branch Changes condition on the default branch by default.** That default is what produced
   Beckify build 326; it must be deleted before the first save.
3. Unity cannot run inside Xcode Cloud (macOS-only Apple images, no Unity license/Editor). So the export is
   produced on the box and committed; Xcode Cloud only compiles it.
4. GitHub LFS free quota is now **10 GiB storage + 10 GiB download bandwidth/month** (not 1 GB) on GitHub Free/Pro
   ([GitHub docs: Git LFS billing](https://docs.github.com/en/billing/concepts/product-billing/git-lfs)). With a
   $0 budget, overage blocks LFS for the rest of the month instead of billing. Xcode Cloud supports Git LFS.
5. One file in a Unity iOS export (`Libraries/libiPhone-lib.a`, typically ~100–600 MB) exceeds GitHub's 100 MB
   hard file limit, so LFS (or a slimmer export) is unavoidable for a committed export.

## 1. Export flow (box, Unity CLI only)

Only the lead agent (the sole Unity CLI driver) runs this, via the official CLI per unity/README.md §0
(never the raw Editor with `-batchmode`).

```
tools/ios-export/export.sh <build-number> [git-ref]   # Unity CLI on the box (signed-in `unity`)
  # or GitHub Actions → workflow "Xcode export" (workflow_dispatch) with build_number + ref
  1. clean worktree at the chosen ref (default origin/main); record SHA
  2. unity build . --target iOS --execute-method Kestrel.Slice.Editor.BuildScript.BuildIOS
     env KH_BUILD_PATH=/tmp/kh-ios-out  KH_BUILD_NUMBER=<n>
  3. verify: Info.plist CFBundleIdentifier=com.beckify.kestrelheavy (Unity may leave \${PRODUCT_BUNDLE_IDENTIFIER}; the Xcode project must set it), CFBundleShortVersionString=2.0.0,
     CFBundleVersion=<n>, Unity-iPhone.xcodeproj present
  4. tools/ios-export/publish.sh replaces orphan branch `xcode-export` (single commit + tag export/2.0.0-<n>)
```

CI also uploads a non-shipping `xcode-ios-compile` artifact from Unity CI's iOS compile-check; that artifact is for PR
verification only. Shipping exports go through `export.sh` or the **Xcode export** workflow onto `xcode-export`.

- `xcode-export` is a dedicated orphan branch. **Never merged to main**; main never contains generated Xcode output.
- Stamping: `PlayerSettings.iOS.buildNumber` from `-khBuildNumber`/`KH_BUILD_NUMBER` (already supported in
  BuildScript.cs). Version string 2.0.0 and bundle id must move from the proto values
  (`bundleVersion: 0.1.0`, `com.beckify.kestrelheavy.unityproto`, build 1) to the shipping values **only when Trevor
  approves**; until then exports use the proto bundle and never go near the live record.
- The scheme Xcode Cloud builds is Unity's generated `Unity-iPhone` (not the Toolbox `Beckify` scheme and not the
  old native `ios/KestrelHeavy` project).

## 2. Bundle, version, build numbers

| Field | Value |
|---|---|
| Bundle id | `com.beckify.kestrelheavy` (existing ASC record). Proto: `com.beckify.kestrelheavy.unityproto` |
| Marketing version | `2.0.0` |
| First build number | `100`, then +1 per export (101, 102, …). Never reused, never lower than any build already in ASC |
| Source of truth | the last `export/2.0.0-<n>` tag; script refuses a number ≤ the latest tag |

Xcode Cloud has its own `CI_BUILD_NUMBER` counter. We **do not** let it overwrite `CFBundleVersion`; the committed
export already carries the number, so the archive matches the tag. (If ASC rejects a number already used by a
pre-2.0 build, bump the starting number and record it in DECISIONS.)

## 3. Signing

- Xcode Cloud **automatic (cloud-managed) signing** under team 9TR6R5LV8M. Xcode Cloud creates/uses the
  distribution certificate and profile itself.
- The export keeps `Automatic` signing style and the team id only. **No certificates, .p12, profiles or .p8 keys
  in the repo**, ever.

## 4. Workflow: manual start only

Workflow name `Kestrel 2.0 Release (manual)`:

- Start conditions: **Manual Start only**, restricted to branch `ios-export`. **Delete** the default
  Branch Changes condition; no Pull Request Changes, Tag Changes or Schedule conditions.
- Why: Beckify auto-triggered on every push and broke at build 326 (unwanted builds, burned hours, number churn).
  Manual-only also caps hour usage: a Unity IL2CPP archive is roughly 20–45 min, so 25 h ≈ 30+ builds/month.
- Environment: latest release Xcode/macOS; Clean build on.
- Action: **Archive – iOS**, scheme `Unity-iPhone`, distribution **TestFlight (internal testing only)**.
  App Store submission stays a separate manual step in ASC by Trevor.
- Post-actions: none (or internal TestFlight group notify only).

## 5. ci_scripts

None needed: the export is committed complete, CocoaPods are not used, and Xcode Cloud fetches LFS objects itself.
Optional, if ever needed: `ci_scripts/ci_post_clone.sh` at the export root running `git lfs pull` as a safety net,
or a sanity check of `CFBundleVersion`. Unity itself can't run there.

## 6. Repo size and LFS

- Current `unity/` source is ~1.9 MB (procedural assets). Budget IPA target is 40 MB.
- Typical Unity 6 IL2CPP iOS export: **~0.5–1.5 GB** working tree. Most of it is `Libraries/libiPhone-lib.a`
  and other static libs (hundreds of MB), `Classes/Native/*.cpp` IL2CPP output (~50–200 MB, compresses well),
  `il2cpp` headers, and `Data/` (small for us).
- Plan: track `*.a` (and anything >50 MB) with LFS **on the `ios-export` branch only** (`.gitattributes` exists only there).
  Estimated LFS per export ~0.3–0.6 GB.
- Keep the branch **orphan with a single commit** (force-push replaces it each export) so normal git history doesn't
  grow. Caveat: LFS objects of old exports **still count against storage** after a force-push; freeing them needs
  deleting the objects/repo. 10 GiB ≈ 15–30 exports total, and each Xcode Cloud clone downloads the LFS payload, so
  10 GiB bandwidth ≈ 15–30 builds/month.
- Recommended mitigation: put the export in a **separate private repo** (e.g. `becktj90/kestrel-heavy-ios`) instead of a
  branch here, so it can be deleted and recreated to reset LFS without touching the game repo; set the GitHub LFS
  budget to $0 (blocks rather than bills). Decision for Trevor/CoS at go time; the default in this plan stays the
  `ios-export` branch.

## 7. Click-steps (all **needs Trevor's OK**)

### 7a. One-time onboarding (needs Xcode on a Mac, Apple requirement)
Options, in order of preference:
1. **Check first (read-only, ASC web):** ASC → Apps → Kestrel Heavy → **Xcode Cloud** tab. If a product already
   exists for this app (e.g. from earlier native work), skip onboarding and go to 7b: new workflows can be created
   on the web. (The product's repository is fixed at onboarding; if it points elsewhere, add `kestrel-heavy` under
   Xcode Cloud Settings → Repositories.)
2. **One 10-minute Xcode session by Trevor** (the only Mac step, an exception to "no Mac setup" he must OK):
   clone `ios-export`, open `Unity-iPhone.xcodeproj`, Integrate → Create Workflow, pick team 9TR6R5LV8M and the
   existing Kestrel Heavy record, Grant Access to GitHub (`becktj90/kestrel-heavy` only), **delete Branch Changes,
   add Manual Start (ios-export)**, save, start one build. After that everything is web-only.
3. If neither is OK: skip Xcode Cloud and use the UBA fallback (§8), which needs no Mac.

(A rented cloud Mac costs money and breaks $0; not proposed.)

### 7b. Web-only steps after onboarding (ASC)
1. App Store Connect → Apps → Kestrel Heavy → Xcode Cloud → Manage Workflows → **+**.
2. General: name `Kestrel 2.0 Release (manual)`, repository `kestrel-heavy`, project `Unity-iPhone.xcodeproj`.
3. Environment: latest Xcode, Clean on.
4. Start Conditions: remove Branch Changes; **+ Manual Start**, branch `ios-export`.
5. Actions: **+ Archive**, platform iOS, scheme `Unity-iPhone`, Deployment Preparation: TestFlight (Internal Testing only).
6. Post-actions: none. Save.
7. When Trevor says ready: Xcode Cloud → workflow → **Start Build** → branch `ios-export`.
8. Builds appear in TestFlight; App Store submission of 2.0.0 is Trevor's separate decision.

## 8. Fallback and risks

**Fallback: Unity Build Automation** (Unity Cloud; free tier minutes) builds directly from `main`/`unity/`, no Mac and
no committed export, signing with an uploaded certificate + profile (which itself needs Trevor's OK). The existing
`unity-proto-ios.yml` (GameCI, manual dispatch, held) stays held. No UBA runs until Trevor says ready.

Risks:
- Mac requirement for onboarding (§7a), the main blocker.
- LFS quota exhaustion (§6); mitigated by manual-only, separate repo, $0 LFS budget.
- Accidental auto-trigger: mitigated by Manual Start only, checked before every save; never make `ios-export` the default branch.
- Build-number clash with old builds in the record; script enforces monotonic numbers ≥100.
- Export drift: export always from a recorded main SHA in a clean worktree; tag records it.
- Xcode/Unity version skew: pin Xcode in the workflow environment if a new Xcode breaks the Unity export.
- Hours: 25 h/month; at ~30–45 min per archive, budget ≤ 25 builds/month.
