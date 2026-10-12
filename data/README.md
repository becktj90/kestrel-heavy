# data/: engine-agnostic source of truth

| File | Schema | Loaded by |
|---|---|---|
| propellants.json | schemas/propellants.schema.json | web/js/data/propellants.js |
| engines.json | schemas/engines.schema.json | web/js/data/engines.js |
| materials.json | schemas/materials.schema.json | web/js/data/materials.js |
| payloads.json | schemas/payloads.schema.json | web/js/data/payloads.js |
| presets.json | schemas/presets.schema.json | web/js/data/presets.js |
| tanks.json | schemas/tanks.schema.json | web/js/data/tanks.js |
| fairings.json | schemas/fairings.schema.json | web/js/data/fairings.js |
| interstages.json | schemas/interstages.schema.json | web/js/data/interstages.js |
| fins.json | schemas/fins.schema.json | web/js/data/fins.js |
| liveries.json | loader in Kestrel.Livery (no JSON Schema file) | earned paint schemes |
| finishes.json | loader in Kestrel.Livery (no JSON Schema file) | per-part paint |
| codex.json | schemas/codex.schema.json | CodexBook (Unity); web/js/data/gen/codex.js |
| golden/*.json | see golden/README.md and docs/PHYSICS.md | ports (tolerances are per quantity for catalog-flight) |

Economy constants stay in `web/js/economy-data/constants.json` (already JSON + schema).

Workflow after editing a catalog:
1. `npm run gen:catalogs` regenerates `web/js/data/gen/*.js` (the browser/WKWebView loads these plain ES modules; no JSON import attributes needed).
2. `npm run gen:golden` re-exports golden vectors if outputs changed.
3. `npm run pack:ios` copies web/ into the iOS bundle as before.
4. `npm test` checks schemas, mirror drift and golden drift.
