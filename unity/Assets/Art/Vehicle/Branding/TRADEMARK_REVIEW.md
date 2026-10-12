# Branding trademark review checklist (A2)

Sign-off: **Chief of Staff** signs this checklist in every PR that adds or changes glyphs, frames, palette entries, preset brands, or guard rules. **Trevor has the final say** before any store screenshot shows branding (this also covers NAME_REVIEW).

- [ ] All 12 glyphs and 6 frames are original geometric primitives made for this game (generator: `Tools~/make_branding.py`); none was traced from or modelled on a real logo.
- [ ] SVGs contain no text, letterforms, images or external references (enforced by tests).
- [ ] No glyph or frame is a recognisable agency roundel, meatball, swoosh, wing badge or company mark on its own.
- [ ] Palette is generic; no colour set is presented as a real organisation's brand colours.
- [ ] Blocked combinations in `MarkGuard` cover the agency-roundel and swoosh families; reviewer tried 10 random combinations and none read as a real mark.
- [ ] `NameGuard` deny-list covers real agencies, launch companies, vehicles, pads and the names called out in #142 (Blue Origin, New Glenn, Jacklyn, LC-36).
- [ ] Preset brand names (if any) pass `NameGuard` and a manual search.
- [ ] No real pad names, serial formats or mission patches.

Signed (Chief of Staff): ________  Date: ________
Final approval (Trevor): ________  Date: ________
