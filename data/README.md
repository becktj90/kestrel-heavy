# data/: engine-agnostic source of truth

| File | Schema | Loaded by |
|---|---|---|
| propellants.json | schemas/propellants.schema.json | web/js/data/propellants.js |
| engines.json | schemas/engines.schema.json | web/js/data/engines.js |
| materials.json | schemas/materials.schema.json | web/js/data/materials.js |
| payloads.json | schemas/payloads.schema.json | web/js/data/payloads.js |
| presets.json | schemas/presets.schema.json | web/js/data/presets.js |
| golden/*.json | see golden/README.md | ports (e.g. unity/Assets/Kestrel.Core.Tests) |

Economy constants stay in `web/js/economy-data/constants.json` (already JSON + schema).

Workflow after editing a catalog:
1. `npm run gen:catalogs` regenerates `web/js/data/gen/*.js` (the browser/WKWebView loads these plain ES modules; no JSON import attributes needed).
2. `npm run gen:golden` re-exports golden vectors if outputs changed.
3. `npm run pack:ios` copies web/ into the iOS bundle as before.
4. `npm test` checks schemas, mirror drift and golden drift.
