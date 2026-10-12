# fastlane (Kestrel Heavy)
- `bundle exec fastlane dry_run` — local only; runs `tools/asc/check_limits.mjs` and counts staged screenshots. Safe anytime.
- `metadata_preview`, `upload_testflight`, `push_metadata` — talk to App Store Connect and abort unless `TREVOR_APPROVED=yes` is set for that run, after Trevor says yes.
- No submit lane exists on purpose.
- Secrets: `ASC_KEY_ID`, `ASC_ISSUER_ID`, `ASC_KEY_P8` come from CI/UBA secrets. `.p8`, `asc_key.json`, certificates and profiles are git-ignored. Never commit them.
- Zero-Mac: the primary upload path is Unity Build Automation's App Store Connect delivery; these lanes are the fallback on a CI runner.
