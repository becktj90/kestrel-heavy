# Xcode Cloud: one-time first setup (Trevor, on your Mac, ~10 min)

Decision: DECISIONS #59 (#53 reversed 2026-10-12 12:26 PM). Background: docs/XCODE_CLOUD_PLAN.md.

**What this does:** connects the committed Unity iOS export (branch `xcode-export`) to Xcode Cloud with a
**manual-only** workflow. **Do not start a build** at the end; we start builds only when you say "ready".
Cost: $0. Xcode Cloud includes **25 compute hours/month** with the Apple Developer Program (a Unity archive is
roughly 20–45 min, so ~30 builds/month at most).

Export settings already baked in: bundle `com.beckify.kestrelheavy`, version `2.0.0`, build `100`,
team `9TR6R5LV8M`, automatic signing, Game Center as the only capability, portrait only.

## 0. Before you start
- Xcode 15.1 or newer (needed for the Manual Start condition), signed in to your Apple ID under
  **Xcode > Settings > Accounts** (team 9TR6R5LV8M).
- Git LFS, once: `brew install git-lfs && git lfs install` (the export stores big libraries in LFS).

## 1. Get the export
```sh
git clone --branch xcode-export --single-branch https://github.com/becktj90/kestrel-heavy.git kestrel-heavy-xcode
cd kestrel-heavy-xcode
git lfs pull
```

## 2. Open it
Open **`Unity-iPhone.xcodeproj`** (in the clone's root folder; double-click it, or `open Unity-iPhone.xcodeproj`).
Not a `.xcworkspace`, and not the old `ios/` project on main.
If Xcode asks about signing, check **Signing & Capabilities** of the `Unity-iPhone` target shows
"Automatically manage signing", Team 9TR6R5LV8M, bundle `com.beckify.kestrelheavy`, and only **Game Center**.

## 3. Create the Xcode Cloud workflow
1. Open the **Report navigator** (⌘9) > **Cloud** tab > **Get Started**
   (same as menu **Integrate > Create Workflow…**).
2. Pick the product: the **Unity-iPhone** app. When asked, link it to the **existing Kestrel Heavy** app record
   (`com.beckify.kestrelheavy`); do not create a new app.
3. Click **Next**, then **Edit Workflow** (don't accept the default workflow as-is).
4. **General:** name `Kestrel 2.0 Release (manual)`.
5. **Start Conditions:** select the default **Branch Changes** condition and **delete it** (−).
   Click **+** > **Manual Start**, branch **`xcode-export`**. There must be no other start condition
   (no Branch Changes, Pull Request Changes, Tag Changes or Schedule).
6. **Environment:** latest release Xcode/macOS; **Clean** on.
7. **Actions:** keep or add **Archive**, platform **iOS**, scheme **`Unity-iPhone`**,
   Deployment Preparation **TestFlight (Internal Testing Only)**. Remove any Build/Test/Analyze actions.
8. **Post-Actions:** none.
9. Click **Save**.
10. **Grant access to GitHub** when prompted (Grant Access / Connect): sign in to GitHub and allow
    **only** `becktj90/kestrel-heavy`. Return to Xcode.
11. **Stop here.** If Xcode offers **Start Build**, click **Cancel/Not Now**. Don't start a build.

## 4. Check (30 s)
- App Store Connect > Apps > Kestrel Heavy > **Xcode Cloud** > Manage Workflows shows one workflow whose only
  start condition is **Manual Start: xcode-export**.
- Tell us it's done. We start builds only when you say "ready" (from ASC: workflow > Start Build > `xcode-export`).

## Notes
- `ci_scripts/ci_post_clone.sh` on the branch runs `git lfs pull` so Xcode Cloud gets the large files.
- Never make `xcode-export` the default branch and never merge it into main; each export replaces it.
- If Xcode says "no products/app record" or links to the wrong repository, stop and tell us; don't create a new app.
