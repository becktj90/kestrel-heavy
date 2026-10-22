# Design fixtures (tests only)

Named rockets that tests rely on for a specific behaviour. They are not shipped presets, so retuning a preset cannot move them.

| File | What it is for |
|---|---|
| `maxq-high-kid.json` | The pre-#313 kid preset (no guidance). Its directed readout peaks at 90.07 kPa: the `maxq-high` warning that warns and does not block (`ReadoutV2Tests.StabilityBand…`). |
| `weak-scout.json` | The pre-#313 scout preset (composite tanks, no guidance). The breakup tests drop its strength to 1.1 on al-li to force a structural max-Q breakup and impact (`FlightSimSinkTests`, `PlayedBreakupTests`). |
