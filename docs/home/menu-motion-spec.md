# Kestrel Heavy main menu: motion specs (all concepts)
Common: 60 fps UI Toolkit/transform animation only, no glow or bloom pulses. Ease = cubic-out unless noted. Tap targets >= 44 pt. Every animation is skipped when Reduce Motion is on (state changes become instant, one frame).
Mock data (not real yet): "3 NEW PARTS", stars, T-00:14:00. Missions / Codex / Settings are disabled stubs ("SOON"). Fleet does not exist.

## A. Title Card (Fraunces + Space Grotesk, warm dusk poster)
- Intro (2.4 s): letterbox bars slide in from the edges (0.4 s); title letters fade up in order with 40 ms stagger, HEAVY rules draw outward from centre (0.5 s); sun rises 60 px behind the rocket (1.2 s); light shafts fade 0 to 46 alpha; menu rows slide up 24 px and fade, 70 ms stagger, LAUNCH last.
- Idle: 3 parallax layers (sun/sky 0.15x, hills 0.4x, rocket+gantry 0.7x) shift up to 10 px with device tilt, or a slow 20 s sine drift without gyro. Shafts breathe (alpha +/-8, 6 s). Grain re-seeds at 12 fps.
- Tap: row scales to 0.97 and drops its shadow (80 ms), face tint flips to cream, ripple ring expands 34 to 100 px while fading (350 ms), then the route runs. LAUNCH adds a 150 ms flash to white at 20% opacity max.
- Reduced motion: no intro (final frame), no drift or breathing, grain static, press = instant colour change only, no ripple.

## B. Gantry Dawn (Bungee + Righteous + Space Grotesk, low-angle hero)
- Intro (2.0 s): camera tilts up from the pad deck to the rocket (dolly 90 px, 1.4 s); title slams in left to right, 100 ms apart; LAUNCH bar and tiles rise with overshoot (+8 px, 120 ms settle).
- Idle: cloud bands drift sideways at 6 / 10 / 14 px/s by layer; gantry steam wisp every 9 s; HANGAR lamp steady, "new parts" badge nudges 2 px once every 8 s.
- Tap: tile depresses 4 px and tints cream-to-orange (80 ms); LAUNCH ripples from the touch point. Long-press shows the one-line caption in a tooltip for VoiceOver users too.
- Reduced motion: static framing, no drift or steam, no overshoot, no nudge.

## C. Mission Reel (Big Shoulders + Syne, film chapter list)
- Intro (2.6 s): film-leader countdown 3-2-1 skipped; bars slide in with sprockets scrolling for 0.8 s; rocket rises 40 px and plume flickers (4 frames at 12 fps); title card holds 0.6 s then rows wipe in left to right, 60 ms stagger.
- Idle: cloud layers cross at 8 / 14 px/s; sprockets crawl 12 px/s; plume flicker at 8 fps (warm tones only); rocket bobs 3 px over 5 s.
- Tap: chapter row wipes to cream from the left (120 ms), orange pointer slides to the row; numerals count up on selection. LAUNCH pill compresses 4% then flashes.
- Reduced motion: no scrolling sprockets, no bob, plume is a static frame, wipe replaced by an instant fill.

## Accessibility
Each row's spoken label = "<Name>. <caption>." plus "Coming soon, unavailable" on stubs. Focus order: Launch, Hangar, Missions, Codex, Settings. Captions are 26 to 30 px at 1170 wide (about 9 to 10 pt on a 3x phone, so keep them >= 11 pt in the real UI).

---
# MIX (Trevor's pick): dusk title card + gantry hero + reel bars
Files: mix-menu.png (tiles, HANGAR focused/pressed), mix-menu-v2.png (rows, LAUNCH pressed with ripple, HANGAR focus ring). Same motion for both.
Type: Fraunces (title, LAUNCH, item names), Space Grotesk (captions, tags, hint). Palette = mix-v2 home: navy, cream, orange, brick red, warm brown.
- Intro (2.4 s): sprocket letterbox bars slide in (0.4 s). Sun rises 50 px behind the hero (1.2 s). Title fades up (letter stagger 40 ms), HEAVY rules draw from centre. Cloud layers fade in. LAUNCH bar rises 24 px + fade, then HANGAR/MISSIONS/CODEX/SETTINGS follow at 70 ms stagger. Lamps switch on last.
- Idle: 4 parallax layers (sky+sun 0.1x, far clouds 0.2x, hills+gantry 0.5x, rocket 0.7x, near haze 1.0x) with up to 10 px tilt shift, else a 20 s sine drift. Haze/shafts breathe (alpha +/-8, 6 s). Grain at 12 fps. Gantry steam wisp every 9 s. Sprockets crawl 12 px/s in the bars (stops when idle for the focus state).
- Focus (keyboard/VoiceOver/gamepad): 4 px cream ring, no motion.
- Tap: panel drops 4 px and loses its shadow (80 ms); tile tint to cream (or LAUNCH darkens to brick); ripple 34 to 100 px fading over 350 ms; then the route. LAUNCH also does a 150 ms 20% white flash. Stubs (SOON) only do a 2 px horizontal shake (120 ms) and a "Coming soon" VoiceOver announcement.
- Reduced motion: skip intro (final frame), no tilt/drift/breathing/steam/sprockets, grain static, tap = instant tint change, stub shake removed.
- Routes: LAUNCH = start next mission (needs a route that does not exist on main yet; today only Design exists), HANGAR = designer, others disabled stubs.
