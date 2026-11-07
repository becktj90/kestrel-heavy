// Player-facing booster recovery after the first stage lets go. The autopilot flies boostback and entry; with
// Auto-assist off the player flies the final landing burn (DECISIONS #79: Core's ArcadeLandingSession hands over on the
// suicide-burn curve and turns the throttle slider and the lean into a RecoveryStick for the same sim). Auto-assist
// (kid play) and an expendable stage keep the autopilot. The flight logic is Core's BoosterLanding (every released
// first stage gets its landing; an expendable one says why it is lost and still flies the attempt, ungraded); this
// class draws it. The picture is the separated stage: grid fins, legs, the landing plume, dust or spray, Haven moving
// in the swell, and a #247 break when the touchdown is too hard. The ascent camera stays on the rocket until entry,
// then follows the booster, and cuts in close on the deck (or the pad) for the player's burn. Boostback keeps a corner inset.
using System;
using System.Collections.Generic;
using System.Globalization;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Vehicle;
using Kestrel.Game.Failure;
using Kestrel.Vehicle;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class RecoveryShow : MonoBehaviour
    {
        readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();

        FlightDriver flight;
        FlightStageShow staging;
        FinCluster fins;
        StrutCluster legs;
        VisualElement hud;
        Label callout, numbers;
        Camera inset;
        BoosterLanding landing;
        ArcadeLandingSession arcade;
        static ArcadeLandingTune arcadeTune;
        bool autoAssist, wasHanded, eastRight = true;
        // The landing layer (player's burn): call, cells, cue, the suicide-burn gauge, lean buttons and the throttle slider.
        VisualElement layer, gaugeTrack, gaugeAlt, gaugeStop, gaugeLight, throttleTrack, throttleFill, throttleHover, leanLeft, leanRight;
        Label arcadeTitle, arcadeLine, arcadeCue;
        readonly Label[] cellKey = new Label[ArcadeHud.Count], cellVal = new Label[ArcadeHud.Count];
        readonly VisualElement[] cells = new VisualElement[ArcadeHud.Count];
        double slider;
        int leanHeld;                 // -1 left, +1 right (screen), 0 none
        int dragId = -1, sliderId = -1;
        float dragX0;
        double dragSteer;
        float layerContentPt;         // the layer's call + cells + cue height, for the camera's top cover
        Transform glow, centreA, centreB, hull;
        ParticleSystem spray;
        RecoveryCraft craft => landing != null ? landing.Craft : default;
        RecoveryBody body => landing != null ? landing.Body : default;
        Transform[] finBits = new Transform[4];
        Transform[] legBits = new Transform[4];
        Transform plume, deck, dustRoot;
        ParticleSystem dust;
        UnityEngine.Material hotMat, deckMat;
        Mesh cube;
        double downAge;
        float units = 1f;
        bool armed, started, engineer, reduce, boomed, puffed;

        /// <summary>Real seconds the grade splash holds the landing layer (and the ascent) after touchdown.</summary>
        public const double SplashHoldSec = 4.0;

        public bool SteerCamera { get; private set; }

        /// <summary>
        /// The player is flying the landing burn (from the hand-over to the end of the grade splash). SliceApp holds the
        /// ascent clock meanwhile and hides the ascent dock, so nothing upstairs needs a thumb while the booster lands.
        /// </summary>
        public bool PlayerBurn => started && arcade != null && arcade.Pilot == ArcadePilot.Player && arcade.HandedOver
            && (!landing.Touched || arcade.DownWallSec < SplashHoldSec);

        /// <summary>For a "next stage" skip: true while the player is still to take the burn. The skip may run to the
        /// hand-over (<see cref="RunToHandover"/>) but never past it.</summary>
        public bool SkipStopsAtHandover => started && arcade != null && arcade.SkipStopsAtHandover;

        /// <summary>Fast-forward the autopilot to the player's hand-over (the skip). True once the burn is the player's.</summary>
        public bool RunToHandover() => started && arcade != null && arcade.RunToHandover();

        public ArcadeLandingSession ArcadeForCapture => arcade;
        /// <summary>Capture hook (Kestrel.Capture.Tests LandingHudCaptureTests): Arm treats Auto-assist as off, so the player's burn shows.</summary>
        public static bool PlayerBurnForCapture;
        /// <summary>Capture hook: when set, flies the player's burn instead of the layer's slider and lean pads.</summary>
        public static Func<ArcadeLandingSession, ArcadeInput> PilotForCapture;
        public VisualElement LayerForCapture => layer;
        /// <summary>The layer's touch targets (lean pads and the throttle slider), for the 44 pt check.</summary>
        public VisualElement[] TouchTargetsForCapture => new[] { leanLeft, leanRight, throttleTrack };

        // Capture hooks (Kestrel.Capture.Tests AppStoreScreenshotTests). Nothing in play sets HoldForCapture.
        /// <summary>When set, the landing does not advance, so one moment can be shot at several screen sizes.</summary>
        public static bool HoldForCapture;
        /// <summary>The booster has separated and its landing is flying.</summary>
        public bool StartedForCapture => started;
        public RecoveryPhase PhaseForCapture => started && landing != null ? landing.Sample().Phase : RecoveryPhase.Idle;
        public double AltitudeForCapture => started && landing != null ? landing.Sample().Alt : double.NaN;
        public double ThrottleForCapture => started && landing != null ? landing.Sample().Throttle : double.NaN;

        /// <summary>
        /// Altitude the sky should be graded for while the camera follows the booster (else NaN). The sky used to stay on the
        /// ascent's altitude, so a booster landing at sea level was shown against black space (#381 landing frame, ascent at
        /// 159 km): what players saw too whenever Follow was on the booster.
        /// </summary>
        public double ViewAltitude => SteerCamera && started && landing != null ? Math.Max(0, landing.Sample().Alt) : double.NaN;

        /// <summary>The booster's HUD numbers from this frame's sample (read-only; valid while <see cref="SteerCamera"/>).
        /// SliceApp hands it to AscentHud so the strip reads the stage the camera is on.</summary>
        public BoosterHudReadout HudReadout { get; private set; }

        public void Bind(FinCluster finCluster, StrutCluster legCluster, VisualElement flightHud)
        {
            fins = finCluster;
            legs = legCluster;
            hud = flightHud;
        }

        public void Arm(FlightDriver driver, FlightStageShow stageShow, Catalog catalog, Design design, bool engineerDepth, bool reduceMotion)
            => Arm(driver, stageShow, catalog, design, engineerDepth, reduceMotion, !engineerDepth);

        /// <param name="assist">The flight's Auto-assist (StepperProgress.AutoAssistOn, as the ascent director reads it). On: the
        /// autopilot lands the booster and the player watches. Off: the player flies the final burn.</param>
        public void Arm(FlightDriver driver, FlightStageShow stageShow, Catalog catalog, Design design, bool engineerDepth, bool reduceMotion, bool assist)
        {
            Stop();
            flight = driver;
            staging = stageShow;
            engineer = engineerDepth;
            reduce = reduceMotion;
            autoAssist = assist && !PlayerBurnForCapture;
            // Before: only a design with recovery.mode and a landing craft armed here (TryFrom), so every
            // expendable starter (KH-3..KH-5 and the templates) dropped its booster with no landing and no word.
            landing = BoosterLanding.Arm(catalog, design);
            if (!landing.Armed) return;
            if (staging == null || !staging.CoreCanRelease || staging.CoreSerial != landing.CoreSerial)
            {
                landing.Release();
                return;
            }
            armed = true;
            arcade = new ArcadeLandingSession(landing, Tune(), autoAssist);
            slider = 0;
            leanHeld = 0;
            dragSteer = 0;
            wasHanded = false;
        }

        /// <summary>data/arcade-landing.json: the synced Resources copy on a device, the checkout in the editor, else the built-in default.</summary>
        static ArcadeLandingTune Tune()
        {
            if (arcadeTune != null) return arcadeTune;
            try
            {
                var asset = Resources.Load<TextAsset>("KestrelData/arcade-landing");
                if (asset != null) arcadeTune = ArcadeLandingTune.FromJson(Kestrel.Core.Json.MiniJson.ParseObject(asset.text));
                else
                {
                    string root = Catalog.RepoRootOrNull();
                    arcadeTune = root != null ? ArcadeLandingTune.Load(root) : ArcadeLandingTune.Default;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Recovery] arcade-landing.json unreadable, using defaults: " + e.Message);
                arcadeTune = ArcadeLandingTune.Default;
            }
            return arcadeTune;
        }

        public void Layout(float unitsPerPoint)
        {
            units = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
            PlaceChrome();
        }

        public void Stop()
        {
            armed = false;
            started = false;
            SteerCamera = false;
            boomed = false;
            puffed = false;
            downAge = 0;
            if (landing != null) landing.Release();
            if (staging != null) staging.SetRecoveryOwnsBooster(false);
            if (fins != null) fins.enabled = true;
            if (legs != null) legs.enabled = true;
            if (inset != null) inset.enabled = false;
            if (callout != null) callout.style.display = DisplayStyle.None;
            if (numbers != null) numbers.style.display = DisplayStyle.None;
            if (layer != null) layer.style.display = DisplayStyle.None;
            if (deck != null) deck.gameObject.SetActive(false);
            if (plume != null) plume.gameObject.SetActive(false);
            if (glow != null) glow.gameObject.SetActive(false);
            if (spray != null) spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            arcade = null;
            wasHanded = false;
            leanHeld = 0;
            dragId = -1;
            sliderId = -1;
            RecoveryLog.Release();
        }

        public void Present(float dt)
        {
            if (HoldForCapture) dt = 0f;
            if (!armed || flight == null || flight.Sim == null || staging == null)
            {
                SteerCamera = false;
                return;
            }
            if (!started)
            {
                Watch(dt);
                return;
            }
            if (body.Phase == RecoveryPhase.Down) downAge += dt;
            // t466: once the ascent is over (insertion or a clean cutoff), the booster finishes at once so the report opens.
            // On the player's burn the autopilot runs straight to the hand-over instead (ArcadeLandingSession.Conclude).
            arcade.Conclude(flight.Result != null && !flight.Running, flight.Lost);
            if (!wasHanded && arcade.HandedOver)
            {
                wasHanded = true;
                slider = arcade.Input.Throttle;   // the slider starts where the engine is
                leanHeld = 0; dragSteer = 0; dragId = -1; sliderId = -1;
            }
            if (arcade.PlayerFlying) arcade.Input = PilotForCapture != null ? PilotForCapture(arcade) : ReadInput();
            // Presented time goes in; the session scales it for slow motion and BoosterLanding runs its fixed steps.
            var sample = arcade.Step(dt);
            string cause = RecoveryLog.Current != null && RecoveryLog.Current.Attempted ? RecoveryLog.Current.Cause : null;
            var pic = RecoveryViews.Of(sample, landing.TrimMarks(), cause, craft.VLeg, sample.Phase == RecoveryPhase.Down);
            pic.Call = landing.Callout(sample);
            Pose(sample);
            PoseHardware(sample);
            PosePlume(sample);
            PoseDeck(sample);
            PoseGlowAndSpray(sample, pic);
            Touch(pic, sample);
            Call(pic, sample);
            PaintLayer(sample);
            SteerCamera = pic.Follow && (sample.Phase != RecoveryPhase.Down || downAge < 8);
            HudReadout = BoosterHud.Read(sample, craft);
            AimInset(pic.Inset && !SteerCamera && !PlayerBurn, sample);
        }

        /// <summary>The player's axes from the layer: the slider, the held lean buttons or a drag on the scene (screen right is
        /// east when the camera shows east to the right).</summary>
        ArcadeInput ReadInput()
        {
            double screen = leanHeld != 0 ? leanHeld : dragSteer;
            return new ArcadeInput { Throttle = slider, Steer = eastRight ? screen : -screen };
        }

        public void Aim(Camera cam, float groundY) => Aim(cam, groundY, 0f, 0f);

        /// <summary>Margin (viewport) kept between the booster's framing points and the HUD's top strip and bottom row.</summary>
        public const float HudMargin = 0.03f;
        /// <summary>How far under the booster's lowest point the shot keeps in view (booster heights): its flame and, low down, the deck.</summary>
        public const float BelowHeights = 1.6f;

        /// <summary>
        /// Follow the booster, and keep the whole booster (renderer bounds: body, fins, legs, landing flame) plus the sea or deck
        /// up to <see cref="BelowHeights"/> under it inside the band between the HUD's top strip (<paramref name="topCover"/>)
        /// and its bottom control row (<paramref name="bottomCover"/>), both measured viewport fractions. ChaseCamera.FitBand
        /// backs the camera off (a wider shot on a 4:3 iPad) without turning it, so the held horizon stays where Aim put it.
        /// </summary>
        public void Aim(Camera cam, float groundY, float topCover, float bottomCover)
        {
            if (!SteerCamera || cam == null || staging == null || staging.Booster == null) return;
            var target = staging.Booster.position;
            float h = 26f;
            float alt = (float)Math.Max(0, landing.Sample().Alt);
            var offset = ChaseCamera.DesiredOffset(Vector3.up, h, alt);
            var pos = PadGround.ClampAboveGround(target + offset, groundY);
            var rot = ChaseCamera.Aim(pos, target + Vector3.up * (h * 0.25f), cam.fieldOfView, alt, pos.y - groundY);
            bool close = arcade != null && arcade.Pilot == ArcadePilot.Player && arcade.HandedOver && deck != null;
            if (close)
            {
                // The cut: from the hand-over the shot sits low by Haven (or the pad), on the chase camera's side, and keeps
                // the deck and the booster together; it closes in as the booster comes down.
                var flat = new Vector3(offset.x, 0f, offset.z);
                if (flat.sqrMagnitude < 1e-4f) flat = Vector3.back;
                flat.Normalize();
                var deckTop = deck.position;
                float dist = Mathf.Clamp(90f + 0.32f * alt, 90f, 900f);
                var mid = Vector3.Lerp(deckTop, target, Mathf.Clamp01(0.35f + alt / 6000f));
                pos = mid + flat * dist + Vector3.up * (10f + 0.05f * alt);
                pos.y = Mathf.Max(pos.y, groundY + 4f);
                rot = Quaternion.LookRotation((mid - pos).normalized, Vector3.up);
                // Keep the landing layer's call, cells and cue (and the lean buttons) out of the picture.
                if (PlayerBurn && LayerCovers(out float layerTop, out float layerBottom))
                {
                    topCover = Mathf.Max(topCover, layerTop);
                    bottomCover = Mathf.Max(bottomCover, layerBottom);
                }
            }
            // Which way east runs on screen, so the lean buttons and the L/R readouts follow the picture.
            var east = staging.Booster.parent != null ? staging.Booster.parent.TransformDirection(Vector3.right) : Vector3.right;
            var right = cam.transform.right;
            eastRight = Vector3.Dot(east, right) >= 0f;
            if (topCover > 0f || bottomCover > 0f)
            {
                int n = FramePoints(groundY, h, close);
                if (n > 0)
                {
                    float yMax = 1f - Mathf.Clamp01(topCover) - HudMargin, yMin = Mathf.Clamp01(bottomCover) + HudMargin;
                    pos = ChaseCamera.FitBandAbove(ref rot, pos, framePts, n, cam.fieldOfView, cam.aspect, yMin, yMax, groundY + 1f);
                    pos.y = Mathf.Max(pos.y, groundY + 1f);
                }
            }
            cam.transform.position = pos;
            cam.transform.rotation = rot;
        }

        readonly System.Collections.Generic.List<Renderer> boosterRenderers = new System.Collections.Generic.List<Renderer>();
        readonly Vector3[] framePts = new Vector3[12];

        /// <summary>Viewport covers (0..1 from the top and from the bottom) of the layer's top block and its lean buttons.</summary>
        bool LayerCovers(out float top, out float bottom)
        {
            top = 0f; bottom = 0f;
            if (layer == null || layer.panel == null || arcadeCue == null || leanLeft == null) return false;
            var p = layer.panel.visualTree.worldBound;
            if (!(p.height > 1f)) return false;
            var cue = arcadeCue.worldBound;
            var lean = leanLeft.worldBound;
            if (float.IsNaN(cue.yMax) || float.IsNaN(lean.yMin)) return false;
            top = Mathf.Clamp01((cue.yMax - p.yMin) / p.height);
            bottom = Mathf.Clamp01((p.yMax - lean.yMin) / p.height);
            if (top + bottom > 0.8f) { top = Mathf.Min(top, 0.45f); bottom = Mathf.Min(bottom, 0.3f); }
            return true;
        }

        /// <summary>The booster's drawn bounds (8 corners) and the point <see cref="BelowHeights"/> under it, not below the sea or deck.
        /// On the player's burn the deck's two ends join, so the target stays in the shot.</summary>
        int FramePoints(float groundY, float h, bool withDeck = false)
        {
            staging.Booster.GetComponentsInChildren(false, boosterRenderers);
            bool any = false; var bounds = new Bounds();
            foreach (var r in boosterRenderers)
            {
                if (r == null || !r.enabled || r is ParticleSystemRenderer) continue;
                if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
            }
            if (!any) return 0;
            var mn = bounds.min; var mx = bounds.max; int n = 0;
            for (int i = 0; i < 8; i++)
                framePts[n++] = new Vector3((i & 1) == 0 ? mn.x : mx.x, (i & 2) == 0 ? mn.y : mx.y, (i & 4) == 0 ? mn.z : mx.z);
            float floorY = Mathf.Max(groundY, mn.y - BelowHeights * h);
            framePts[n++] = new Vector3(bounds.center.x, floorY, bounds.center.z);
            if (withDeck && deck != null)
            {
                float r = (float)RecoverySim.DeckRadiusM;
                var d = deck.position;
                var east = deck.parent != null ? deck.parent.TransformDirection(Vector3.right) : Vector3.right;
                framePts[n++] = d - east * r;
                framePts[n++] = d + east * r;
            }
            return n;
        }

        void Watch(float dt)
        {
            bool over = flight.Lost || (flight.Result != null && !flight.Running);
            if (landing.Watch(flight.SimTime, flight.Sim.StageIdx, flight.Downrange, flight.Altitude, flight.Speed, flight.AttitudeDeg, flight.HasAttitude, over))
            {
                Begin();
                return;
            }
            if (!landing.Armed) armed = false;
        }

        void Begin()
        {
            started = true;
            staging.SetRecoveryOwnsBooster(true);
            if (fins != null) fins.enabled = false;
            if (legs != null) legs.enabled = false;
            BuildHardware();
            BuildDeck();
            EnsureChrome();
        }

        void Pose(in RecoverySample sample)
        {
            var pivot = staging != null ? staging.Booster : null;
            if (pivot == null || flight == null) return;
            var space = flight.Vehicle != null ? flight.Vehicle.parent : null;
            var local = new Vector3((float)(sample.Arc - flight.OriginX), (float)(sample.Alt - flight.OriginY), 0f);
            var world = space != null ? space.TransformPoint(local) : local;
            var spin = Quaternion.Euler(0f, 0f, (float)((sample.Pitch - Math.PI / 2) * Mathf.Rad2Deg));
            if (boomed && sample.Phase == RecoveryPhase.Down)
            {
                var cue = new FailureCue
                {
                    Radius = (float)Math.Max(0.4, craft.DiameterM * 0.5),
                    Sink = (float)sample.Sink,
                    VLeg = (float)craft.VLeg,
                };
                var plan = FailureFx.Plan(BoomNow(sample), "", cue, (float)downAge, reduce);
                spin = spin * Quaternion.Euler(0f, 0f, plan.AngleDeg);
            }
            var rot = space != null ? space.rotation * spin : spin;
            pivot.SetPositionAndRotation(world, rot);
        }

        string BoomNow(in RecoverySample sample)
        {
            var judged = RecoveryLog.Current;
            if (judged != null && judged.Failure != null) return RecoveryViews.Boom(judged.Cause, sample.Sink, craft.VLeg);
            return null;
        }

        void PoseHardware(in RecoverySample sample)
        {
            float fin = (float)sample.Fins;
            float leg = (float)sample.Legs01;
            for (int i = 0; i < finBits.Length; i++)
            {
                if (finBits[i] == null) continue;
                float outRad = Mathf.Lerp(0.15f, 1.15f, fin);
                finBits[i].localRotation = Quaternion.Euler(0f, i * 90f, 0f) * Quaternion.Euler(0f, 0f, -70f * (1f - fin));
                finBits[i].localScale = new Vector3(0.12f, 0.08f, outRad);
            }
            for (int i = 0; i < legBits.Length; i++)
            {
                if (legBits[i] == null) continue;
                float fold = Mathf.Lerp(8f, 58f, leg);
                legBits[i].localRotation = Quaternion.Euler(0f, i * 90f, 0f) * Quaternion.Euler(fold, 0f, 0f);
            }
        }

        void PosePlume(in RecoverySample sample)
        {
            if (plume == null) return;
            bool on = sample.Throttle > 0.005 && sample.Phase != RecoveryPhase.Down;
            plume.gameObject.SetActive(on);
            if (!on) return;
            float len = 2.2f + 10f * Drive(sample);
            plume.localScale = new Vector3(0.55f, len, 0.55f);
            plume.localPosition = new Vector3(0f, -len * 0.5f, 0f);
        }

        /// <summary>How hard the engine is driving, 0..1, for the flame and the glow: thrust over the slider's full T/W on the
        /// player's burn (a near-empty core hovers at a few percent throttle), else the throttle.</summary>
        float Drive(in RecoverySample sample)
        {
            if (arcade != null && arcade.HandedOver && arcade.Pilot == ArcadePilot.Player && arcade.Tune.MaxTw > 0)
                return Mathf.Clamp01((float)(arcade.Readout.Tw / arcade.Tune.MaxTw));
            return Mathf.Clamp01((float)sample.Throttle);
        }

        /// <summary>Haven (or the pad) under the booster. At sea the deck heaves and rolls in a drawn swell
        /// (ArcadeLanding.Deck, on the recovery clock so slow motion slows it too); the sim's deck stays flat at sea level.</summary>
        void PoseDeck(in RecoverySample sample)
        {
            if (deck == null || flight == null) return;
            deck.gameObject.SetActive(true);
            var space = flight.Vehicle != null ? flight.Vehicle.parent : null;
            var swell = ArcadeLanding.Deck(sample.T, craft.Mode, Tune());
            if (reduce) swell = default;
            var local = new Vector3((float)(body.BargeArc - flight.OriginX), (float)(swell.HeaveM - flight.OriginY), 0f);
            deck.position = space != null ? space.TransformPoint(local) : local;
            var roll = Quaternion.Euler(0f, 0f, (float)swell.RollDeg);
            deck.rotation = space != null ? space.rotation * roll : roll;
        }

        /// <summary>Engine glow on the deck under a lit engine and spray (sea) or dust (pad) blown off it, both stronger as the
        /// booster comes down. Drawn only; nothing here touches the sim.</summary>
        void PoseGlowAndSpray(in RecoverySample sample, in RecoveryPicture pic)
        {
            float drive = sample.Throttle > 0.005 && sample.Phase != RecoveryPhase.Down ? Drive(sample) : 0f;
            float near = Mathf.Clamp01(1f - (float)sample.Alt / 90f);
            float k = drive * near;
            if (glow != null && deck != null && staging != null && staging.Booster != null)
            {
                bool on = k > 0.02f;
                glow.gameObject.SetActive(on);
                if (on)
                {
                    var b = staging.Booster.position;
                    glow.position = new Vector3(b.x, deck.position.y + 0.25f, b.z);
                    glow.rotation = deck.rotation;
                    float r = Mathf.Lerp(3f, 14f, k);
                    glow.localScale = new Vector3(r, 0.05f, r);
                }
            }
            if (spray != null && staging != null && staging.Booster != null && deck != null)
            {
                var b = staging.Booster.position;
                spray.transform.position = new Vector3(b.x, deck.position.y + 0.4f, b.z);
                var emission = spray.emission;
                emission.rateOverTime = (reduce ? 20f : 90f) * k;
                if (k > 0.02f && !spray.isPlaying) spray.Play(true);
                else if (k <= 0.02f && spray.isPlaying) spray.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        void Touch(in RecoveryPicture pic, in RecoverySample sample)
        {
            if (sample.Phase != RecoveryPhase.Down) return;
            if (!puffed)
            {
                puffed = true;
                Burst(pic);
            }
            if (!boomed && !string.IsNullOrEmpty(pic.BoomKind))
            {
                boomed = true;
                Shatter(pic.BoomKind, sample);
            }
        }

        void Burst(in RecoveryPicture pic)
        {
            if (dust == null || staging == null || staging.Booster == null) return;
            var main = dust.main;
            main.startColor = pic.Splash ? new Color(0.55f, 0.75f, 0.95f, 0.85f) : new Color(0.72f, 0.62f, 0.45f, 0.9f);
            dust.transform.position = staging.Booster.position;
            dust.Play(true);
        }

        void Shatter(string kind, in RecoverySample sample)
        {
            if (staging == null || staging.Booster == null) return;
            var bp = staging.Booster.position;
            if (!Kestrel.Kit.CinematicEvents.FlightSinkActive)   // a bound CinematicFlightSink raises the crash from OnCrash
                Kestrel.Kit.CinematicEvents.Crash(bp.x, bp.y, bp.z, Mathf.Clamp01((float)(sample.Sink / Math.Max(0.1, craft.VLeg * 4.0))), (float)Math.Max(0.4, craft.DiameterM * 0.5), true, reduce);
            if (reduce) return;
            var cue = new FailureCue
            {
                Radius = (float)Math.Max(0.4, craft.DiameterM * 0.5),
                Sink = (float)sample.Sink,
                VLeg = (float)craft.VLeg,
            };
            var plan = FailureFx.Plan(kind, "", cue, 0.2f, false);
            int n = plan.Burst > 0 ? plan.Burst : 8;
            if (n > 12) n = 12;
            for (int i = 0; i < n; i++)
            {
                var shard = FailureFx.Shard(i, n, Mathf.Max(1.5f, plan.Gap), 0f, 0.35f);
                var bit = MakeBit("Shard", hotMat != null ? hotMat : deckMat);
                bit.transform.position = staging.Booster.position + new Vector3(shard.X, shard.Y, shard.Z);
                bit.transform.localScale = Vector3.one * Mathf.Max(0.25f, shard.Scale);
            }
        }

        void Call(in RecoveryPicture pic, in RecoverySample sample)
        {
            EnsureChrome();
            bool burn = PlayerBurn;
            if (callout != null)
            {
                if (!burn && pic.Call.Visible && !string.IsNullOrEmpty(pic.Call.Line))
                {
                    callout.text = pic.Call.Line;
                    callout.style.display = DisplayStyle.Flex;
                }
                else callout.style.display = DisplayStyle.None;
            }
            if (numbers != null)
            {
                numbers.style.display = engineer && !burn ? DisplayStyle.Flex : DisplayStyle.None;
                if (engineer && !burn)
                {
                    var inv = CultureInfo.InvariantCulture;
                    numbers.text = sample.Phase + "  sink " + sample.Sink.ToString("0.0", inv)
                        + " m/s  tilt " + sample.TiltDeg.ToString("0.0", inv)
                        + "°  miss " + sample.MissM.ToString("0", inv)
                        + " m  prop " + sample.Prop.ToString("0", inv) + " kg";
                }
            }
        }

        /// <summary>The landing layer for the player's burn: call, cells, cue, gauge, the slider and the lean buttons.</summary>
        void PaintLayer(in RecoverySample sample)
        {
            bool burn = PlayerBurn;
            if (layer == null) return;
            layer.style.display = burn ? DisplayStyle.Flex : DisplayStyle.None;
            layer.pickingMode = burn ? PickingMode.Position : PickingMode.Ignore;
            if (!burn) return;
            PlaceLayer();
            var r = arcade.Readout;
            bool down = landing.Touched;
            if (arcadeTitle != null)
            {
                bool call = arcade.CallVisible;
                arcadeTitle.text = call ? arcade.CallTitle : "";
                arcadeLine.text = call ? arcade.CallLine : "";
                bool good = down ? arcade.Splash.Good : arcade.CallTitle != ArcadeLandingSession.LateTitle;
                arcadeTitle.style.color = good ? FlightDusk.Cream : FlightDusk.Alarm;
                arcadeTitle.style.fontSize = (down ? ArcadeHud.SplashFontPt : ArcadeHud.TitleFontPt) * units;
            }
            var c = ArcadeHud.Cells(r, eastRight, craft.Mode, craft.VLeg);
            for (int i = 0; i < ArcadeHud.Count; i++)
            {
                if (cellKey[i] == null) continue;
                cellKey[i].text = c[i].Key;
                cellVal[i].text = c[i].Value;
                cellVal[i].style.color = Tone(c[i].Tone);
                cells[i].tooltip = c[i].Spoken;
                cells[i].style.display = down ? DisplayStyle.None : DisplayStyle.Flex;
            }
            if (arcadeCue != null)
            {
                arcadeCue.text = down ? "" : ArcadeHud.CueText(r);
                arcadeCue.style.color = r.Cue == LightCue.LightNow ? FlightDusk.Sun : r.Cue == LightCue.Late ? FlightDusk.Alarm : FlightDusk.Teal;
            }
            // The suicide-burn gauge: where the booster is, the full-slider stopping height and the top of the light-now band.
            ArcadeHud.Gauge(r, arcade.HandoverAltM, arcade.Tune, out double alt01, out double stop01, out double light01);
            if (gaugeAlt != null)
            {
                gaugeAlt.style.height = Length.Percent((float)(alt01 * 100.0));
                gaugeStop.style.bottom = Length.Percent((float)(stop01 * 100.0));
                gaugeLight.style.bottom = Length.Percent((float)(stop01 * 100.0));
                gaugeLight.style.height = Length.Percent((float)Math.Max(0, (light01 - stop01) * 100.0));
                gaugeAlt.style.backgroundColor = r.Cue == LightCue.Late ? FlightDusk.Alarm : FlightDusk.Teal;
            }
            if (throttleFill != null)
            {
                throttleFill.style.height = Length.Percent((float)(Math.Max(0, Math.Min(1, arcade.Input.Throttle)) * 100.0));
                double hover = arcade.SliderForTw(1);
                throttleHover.style.bottom = Length.Percent((float)(hover * 100.0));
            }
            bool fly = arcade.PlayerFlying;
            if (leanLeft != null) leanLeft.style.display = fly ? DisplayStyle.Flex : DisplayStyle.None;
            if (leanRight != null) leanRight.style.display = fly ? DisplayStyle.Flex : DisplayStyle.None;
            if (throttleTrack != null) throttleTrack.style.display = fly ? DisplayStyle.Flex : DisplayStyle.None;
            if (gaugeTrack != null) gaugeTrack.style.display = fly ? DisplayStyle.Flex : DisplayStyle.None;
            Kestrel.Game.Shell.TextFit.Apply(layer);
        }

        static Color Tone(BoosterHudTone tone)
        {
            switch (tone)
            {
                case BoosterHudTone.Amber: return FlightDusk.Sun;
                case BoosterHudTone.Green: return FlightDusk.Teal;
                default: return FlightDusk.Cream;
            }
        }

        void AimInset(bool on, in RecoverySample sample)
        {
            if (on) EnsureInset();
            if (inset == null || staging == null || staging.Booster == null)
            {
                if (inset != null) inset.enabled = false;
                return;
            }
            inset.enabled = on;
            if (!on) return;
            var target = staging.Booster.position;
            inset.transform.position = target + new Vector3(-18f, 10f, -24f);
            inset.transform.LookAt(target + Vector3.up * 4f, Vector3.up);
        }

        void BuildHardware()
        {
            var pivot = staging != null ? staging.Booster : null;
            if (pivot == null) return;
            var mesh = Cube();
            var src = pivot.GetComponentInChildren<MeshRenderer>();
            hotMat = null;
            if (src != null && src.sharedMaterial != null) hotMat = new UnityEngine.Material(src.sharedMaterial);
            else
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader != null) hotMat = new UnityEngine.Material(shader);
            }
            if (hotMat != null) owned.Add(hotMat);
            if (hotMat != null)
            {
                hotMat.color = new Color(1f, 0.42f, 0.12f);
                if (hotMat.HasProperty("_BaseColor")) hotMat.SetColor("_BaseColor", hotMat.color);
            }
            var vis = src != null ? src.transform : pivot;
            float top = 8f, bot = 0.4f;
            var filter = vis.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                top = filter.sharedMesh.bounds.max.y - 0.4f;
                bot = filter.sharedMesh.bounds.min.y + 0.6f;
            }
            for (int i = 0; i < 4; i++)
            {
                finBits[i] = MakeBit("GridFin", hotMat).transform;
                finBits[i].SetParent(vis, false);
                finBits[i].localPosition = new Vector3(0f, top, 0f);
                legBits[i] = MakeBit("Leg", hotMat).transform;
                legBits[i].SetParent(vis, false);
                legBits[i].localPosition = new Vector3(0f, bot, 0f);
                legBits[i].localScale = new Vector3(0.12f, 2.4f, 0.12f);
            }
            var flame = MakeBit("LandingPlume", hotMat);
            flame.transform.SetParent(vis, false);
            plume = flame.transform;
            plume.gameObject.SetActive(false);
            dustRoot = new GameObject("Touchdown").transform;
            dustRoot.SetParent(transform, false);
            owned.Add(dustRoot.gameObject);
            dust = dustRoot.gameObject.AddComponent<ParticleSystem>();
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = dust.main;
            main.playOnAwake = false;
            main.duration = 0.8f;
            main.loop = false;
            main.startLifetime = 1.1f;
            main.startSpeed = 7f;
            main.startSize = 0.8f;
            main.maxParticles = 48;
            var emission = dust.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, reduce ? (short)8 : (short)28) });
            // Engine glow on the deck and the spray (or dust) the landing burn blows off it.
            glow = MakeBit("EngineGlow", hotMat).transform;
            glow.gameObject.SetActive(false);
            var sprayRoot = new GameObject("LandingSpray");
            sprayRoot.transform.SetParent(transform, false);
            owned.Add(sprayRoot);
            spray = sprayRoot.AddComponent<ParticleSystem>();
            spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var sm = spray.main;
            sm.playOnAwake = false;
            sm.loop = true;
            sm.startLifetime = 1.4f;
            sm.startSpeed = 11f;
            sm.startSize = 1.6f;
            sm.maxParticles = reduce ? 40 : 160;
            sm.startColor = craft.Mode == "rtls" ? new Color(0.72f, 0.62f, 0.45f, 0.75f) : new Color(0.82f, 0.9f, 0.98f, 0.8f);
            var se = spray.emission;
            se.rateOverTime = 0f;
        }

        void BuildDeck()
        {
            if (deck != null)
            {
                deck.name = craft.Mode == "rtls" ? "LandingPad" : "Haven";
                if (hull != null) hull.gameObject.SetActive(craft.Mode != "rtls");
                return;
            }
            if (hotMat != null) deckMat = new UnityEngine.Material(hotMat);
            else
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader != null) deckMat = new UnityEngine.Material(shader);
            }
            if (deckMat != null) owned.Add(deckMat);
            if (deckMat != null)
            {
                deckMat.color = new Color(0.16f, 0.28f, 0.38f);
                if (deckMat.HasProperty("_BaseColor")) deckMat.SetColor("_BaseColor", deckMat.color);
            }
            bool pad = craft.Mode == "rtls";
            var go = MakeBit(pad ? "LandingPad" : "Haven", deckMat);
            go.transform.localScale = new Vector3((float)RecoverySim.DeckRadiusM * 2f, 0.35f, (float)RecoverySim.DeckRadiusM * 2f);
            deck = go.transform;
            // The target the player flies at: a painted cross on the deck (children, so they heave and roll with it).
            UnityEngine.Material paint = null;
            if (deckMat != null)
            {
                paint = new UnityEngine.Material(deckMat);
                owned.Add(paint);
                paint.color = new Color(0.95f, 0.78f, 0.32f);
                if (paint.HasProperty("_BaseColor")) paint.SetColor("_BaseColor", paint.color);
            }
            centreA = MakeBit("DeckMarkA", paint).transform;
            centreA.SetParent(deck, false);
            centreA.localPosition = new Vector3(0f, 0.55f, 0f);
            centreA.localScale = new Vector3(0.42f, 0.2f, 0.035f);
            centreB = MakeBit("DeckMarkB", paint).transform;
            centreB.SetParent(deck, false);
            centreB.localPosition = new Vector3(0f, 0.55f, 0f);
            centreB.localScale = new Vector3(0.035f, 0.2f, 0.42f);
            if (!pad)
            {
                // Haven's hull under the deck, riding the same swell.
                hull = MakeBit("HavenHull", deckMat).transform;
                hull.SetParent(deck, false);
                hull.localPosition = new Vector3(0f, -9f, 0f);
                hull.localScale = new Vector3(1.15f, 18f, 0.8f);
            }
        }

        GameObject MakeBit(string label, UnityEngine.Material mat)
        {
            var go = new GameObject(label);
            go.layer = gameObject.layer;
            owned.Add(go);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = Cube();
            var rend = go.AddComponent<MeshRenderer>();
            if (mat != null) rend.sharedMaterial = mat;
            return go;
        }

        Mesh Cube()
        {
            if (cube != null) return cube;
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube = temp.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Destroy(temp);
            else DestroyImmediate(temp);
            return cube;
        }

        void EnsureChrome()
        {
            if (hud == null) return;
            var host = hud.Q("ascent-hud");
            if (host == null) host = hud;
            if (callout == null || callout.parent != host)
            {
                callout = host.Q<Label>("recovery-callout");
                if (callout == null)
                {
                    callout = new Label { name = "recovery-callout", pickingMode = PickingMode.Ignore };
                    callout.style.position = Position.Absolute;
                    callout.style.unityTextAlign = TextAnchor.MiddleCenter;
                    callout.style.unityFontStyleAndWeight = FontStyle.Bold;
                    callout.style.whiteSpace = WhiteSpace.Normal;
                    callout.style.display = DisplayStyle.None;
                    host.Add(callout);
                }
            }
            if (engineer && (numbers == null || numbers.parent != host))
            {
                numbers = host.Q<Label>("recovery-numbers");
                if (numbers == null)
                {
                    numbers = new Label { name = "recovery-numbers", pickingMode = PickingMode.Ignore };
                    numbers.style.position = Position.Absolute;
                    numbers.style.unityTextAlign = TextAnchor.MiddleLeft;
                    numbers.style.whiteSpace = WhiteSpace.Normal;
                    numbers.style.color = new Color(0.85f, 0.92f, 0.96f);
                    numbers.style.display = DisplayStyle.None;
                    host.Add(numbers);
                }
            }
            if (arcade != null && arcade.Pilot == ArcadePilot.Player && (layer == null || layer.parent != host)) BuildLayer(host);
            PlaceChrome();
        }

        /// <summary>Lean button size (a square), throttle slider width and its tallest travel, points. All at or over the 44 pt touch target.</summary>
        public const float LeanPt = 64f, SliderWPt = 56f, SliderMaxHPt = 220f, GaugeWPt = 12f;
        const float CellHPt = 38f, TitlePt = 40f, LinePt = 34f, CuePt = 26f;

        void BuildLayer(VisualElement host)
        {
            layer = host.Q("arcade-landing");
            if (layer != null) layer.RemoveFromHierarchy();
            layer = new VisualElement { name = "arcade-landing", pickingMode = PickingMode.Ignore };
            layer.style.position = Position.Absolute;
            layer.style.display = DisplayStyle.None;
            arcadeTitle = Text("arcade-title", ArcadeHud.TitleFontPt, true);
            arcadeLine = Text("arcade-line", ArcadeHud.LineFontPt, false);
            layer.Add(arcadeTitle);
            layer.Add(arcadeLine);
            for (int i = 0; i < ArcadeHud.Count; i++)
            {
                var cell = new VisualElement { name = "arcade-cell-" + i, pickingMode = PickingMode.Ignore };
                cell.style.position = Position.Absolute;
                cell.style.backgroundColor = new Color(FlightDusk.Ink.r, FlightDusk.Ink.g, FlightDusk.Ink.b, 0.62f);
                Round(cell, 6f);
                cellKey[i] = Text("arcade-cell-key-" + i, FlightHudLayout.LabelFontPt, false);
                cellKey[i].style.color = new Color(FlightDusk.Cream.r, FlightDusk.Cream.g, FlightDusk.Cream.b, 0.75f);
                cellVal[i] = Text("arcade-cell-value-" + i, FlightHudLayout.ValueFontPt + 2f, true);
                cell.Add(cellKey[i]);
                cell.Add(cellVal[i]);
                cells[i] = cell;
                layer.Add(cell);
            }
            arcadeCue = Text("arcade-cue", ArcadeHud.CueFontPt, true);
            layer.Add(arcadeCue);

            gaugeTrack = Box("arcade-gauge", new Color(FlightDusk.Ink.r, FlightDusk.Ink.g, FlightDusk.Ink.b, 0.7f));
            gaugeLight = Box("arcade-gauge-light", new Color(FlightDusk.Sun.r, FlightDusk.Sun.g, FlightDusk.Sun.b, 0.55f));
            gaugeAlt = Box("arcade-gauge-alt", FlightDusk.Teal);
            gaugeStop = Box("arcade-gauge-stop", FlightDusk.Alarm);
            foreach (var e in new[] { gaugeLight, gaugeAlt, gaugeStop }) { e.style.left = 0f; e.style.right = 0f; gaugeTrack.Add(e); }
            gaugeAlt.style.bottom = 0f;
            layer.Add(gaugeTrack);

            throttleTrack = Box("arcade-throttle", new Color(FlightDusk.Ink.r, FlightDusk.Ink.g, FlightDusk.Ink.b, 0.78f));
            throttleTrack.pickingMode = PickingMode.Position;
            throttleFill = Box("arcade-throttle-fill", FlightDusk.Sun);
            throttleFill.style.left = 0f; throttleFill.style.right = 0f; throttleFill.style.bottom = 0f;
            throttleHover = Box("arcade-throttle-hover", FlightDusk.Cream);
            throttleHover.style.left = 0f; throttleHover.style.right = 0f;
            throttleTrack.Add(throttleFill);
            throttleTrack.Add(throttleHover);
            throttleTrack.RegisterCallback<PointerDownEvent>(e => { sliderId = e.pointerId; SlideTo(e.position.y); e.StopPropagation(); });
            layer.Add(throttleTrack);

            leanLeft = Lean("arcade-lean-left", "<", -1);
            leanRight = Lean("arcade-lean-right", ">", +1);
            layer.Add(leanLeft);
            layer.Add(leanRight);

            // A drag anywhere else on the layer leans too (release springs back to upright); the slider keeps its setting.
            layer.RegisterCallback<PointerDownEvent>(e => { if (dragId < 0) { dragId = e.pointerId; dragX0 = e.position.x; dragSteer = 0; } });
            layer.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (e.pointerId == sliderId) SlideTo(e.position.y);
                else if (e.pointerId == dragId)
                {
                    float span = 90f * (units > 0.5f ? units : 1f);
                    dragSteer = Mathf.Clamp((e.position.x - dragX0) / span, -1f, 1f);
                }
            });
            layer.RegisterCallback<PointerUpEvent>(e => EndPointer(e.pointerId));
            layer.RegisterCallback<PointerCancelEvent>(e => EndPointer(e.pointerId));
            host.Add(layer);
        }

        void EndPointer(int id)
        {
            if (id == sliderId) sliderId = -1;
            if (id == dragId) { dragId = -1; dragSteer = 0; }
        }

        void SlideTo(float panelY)
        {
            if (throttleTrack == null) return;
            var b = throttleTrack.worldBound;
            if (!(b.height > 1f)) return;
            slider = Mathf.Clamp01((b.yMax - panelY) / b.height);
        }

        Label Text(string name, float pt, bool bold)
        {
            var l = new Label { name = name, pickingMode = PickingMode.Ignore };
            l.style.position = Position.Absolute;
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.overflow = Overflow.Hidden;
            l.style.color = FlightDusk.Cream;
            l.style.fontSize = pt * (units > 0.5f ? units : 1f);
            l.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
            l.style.marginLeft = 0f; l.style.marginRight = 0f; l.style.marginTop = 0f; l.style.marginBottom = 0f;
            l.style.paddingLeft = 0f; l.style.paddingRight = 0f; l.style.paddingTop = 0f; l.style.paddingBottom = 0f;
            return l;
        }

        static VisualElement Box(string name, Color c)
        {
            var e = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            e.style.position = Position.Absolute;
            e.style.backgroundColor = c;
            return e;
        }

        static void Round(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
        }

        /// <summary>A hold-to-lean pad: down leans that way (screen side), up or leaving it stands the booster back up.</summary>
        VisualElement Lean(string name, string glyph, int side)
        {
            var pad = Box(name, new Color(FlightDusk.Ink.r, FlightDusk.Ink.g, FlightDusk.Ink.b, 0.78f));
            pad.pickingMode = PickingMode.Position;
            Round(pad, 10f);
            pad.tooltip = side < 0 ? "Lean left (hold)" : "Lean right (hold)";
            var l = Text(name + "-label", 26f, true);
            l.style.left = 0f; l.style.right = 0f; l.style.top = 0f; l.style.bottom = 0f;
            l.text = glyph;
            pad.Add(l);
            pad.RegisterCallback<PointerDownEvent>(e => { leanHeld = side; e.StopPropagation(); });
            pad.RegisterCallback<PointerUpEvent>(e => { if (leanHeld == side) leanHeld = 0; });
            pad.RegisterCallback<PointerLeaveEvent>(e => { if (leanHeld == side) leanHeld = 0; });
            pad.RegisterCallback<PointerCancelEvent>(e => { if (leanHeld == side) leanHeld = 0; });
            return pad;
        }

        /// <summary>
        /// The layer fills the band between the HUD's top strip and its tools row (the camera's own band): the call and line,
        /// six cells in two rows, the cue; at the bottom the lean pads (left) and the throttle slider (right), the gauge
        /// between them. Everything is in points times the app's units.
        /// </summary>
        void PlaceLayer()
        {
            if (layer == null || layer.parent == null) return;
            float u = units > 0.5f ? units : 1f;
            var host = layer.parent;
            var hb = host.worldBound;
            float top = FlightStaging.CalloutTopInsideHud() * u, bottom = 70f * u;
            var strip = hud != null ? hud.Q("ascent-telemetry") : null;
            if (strip != null && strip.panel != null && strip.worldBound.height > 1f && !float.IsNaN(strip.worldBound.yMax))
                top = strip.worldBound.yMax - hb.yMin + 6f * u;
            var tools = hud != null ? hud.Q("flight-tools") : null;
            if (tools != null && tools.panel != null && tools.worldBound.height > 1f && !float.IsNaN(tools.worldBound.yMin)
                && tools.worldBound.yMin > hb.yMin + hb.height * 0.4f)
                bottom = hb.yMax - tools.worldBound.yMin + 6f * u;
            layer.style.left = 0f; layer.style.right = 0f;
            layer.style.top = top; layer.style.bottom = bottom;
            float w = hb.width > 1f ? hb.width : 320f * u;
            float side = FlightHudLayout.SideMarginPt * u;
            float inner = w - 2f * side;
            float y = 0f;
            Place(arcadeTitle, side, y, inner, TitlePt * u); y += TitlePt * u;
            Place(arcadeLine, side, y, inner, LinePt * u); y += LinePt * u + 4f * u;
            float gap = FlightHudLayout.CellGapPt * u;
            float cw = (inner - 2f * gap) / 3f, ch = CellHPt * u;
            for (int i = 0; i < ArcadeHud.Count; i++)
            {
                if (cells[i] == null) continue;
                int row = i / 3, col = i % 3;
                Place(cells[i], side + col * (cw + gap), y + row * (ch + gap), cw, ch);
                Place(cellKey[i], 0f, 2f * u, cw, 14f * u);
                Place(cellVal[i], 0f, 16f * u, cw, ch - 18f * u);
            }
            y += 2f * ch + gap + 6f * u;
            Place(arcadeCue, side, y, inner, CuePt * u); y += CuePt * u;
            layerContentPt = y / u;
            float bandH = (hb.height > 1f ? hb.height : 600f * u) - top - bottom;
            float leanS = LeanPt * u;
            float sliderH = Mathf.Clamp(bandH - y - 12f * u, 88f * u, SliderMaxHPt * u);
            Place(leanLeft, side, bandH - leanS, leanS, leanS);
            Place(leanRight, side + leanS + 12f * u, bandH - leanS, leanS, leanS);
            Place(throttleTrack, w - side - SliderWPt * u, bandH - sliderH, SliderWPt * u, sliderH);
            Place(gaugeTrack, w - side - SliderWPt * u - (GaugeWPt + 14f) * u, bandH - sliderH, GaugeWPt * u, sliderH);
            if (gaugeStop != null) gaugeStop.style.height = 3f * u;
            if (throttleHover != null) throttleHover.style.height = 3f * u;
        }

        static void Place(VisualElement e, float x, float y, float w, float h)
        {
            if (e == null) return;
            e.style.left = x; e.style.top = y; e.style.width = w; e.style.height = h;
        }

        void PlaceChrome()
        {
            float u = units > 0.5f ? units : 1f;
            float top = (FlightStaging.CalloutTopInsideHud() + FlightStaging.CalloutPt + 6f) * u;
            if (callout != null)
            {
                callout.style.left = FlightHudLayout.SideMarginPt * u;
                callout.style.right = FlightHudLayout.SideMarginPt * u;
                FlightDusk.Callout(callout, u, false);
                callout.style.top = top;
            }
            if (numbers != null)
            {
                numbers.style.top = top + (FlightStaging.CalloutPt * 2f + 4f) * u;
                numbers.style.left = FlightHudLayout.SideMarginPt * u;
                numbers.style.right = FlightHudLayout.SideMarginPt * u;
                numbers.style.fontSize = 11f * u;
            }
            if (layer != null && PlayerBurn) PlaceLayer();
        }

        void OnDestroy()
        {
            for (int i = 0; i < owned.Count; i++)
                if (owned[i] != null) Destroy(owned[i]);
            if (callout != null && callout.parent != null) callout.RemoveFromHierarchy();
            if (numbers != null && numbers.parent != null) numbers.RemoveFromHierarchy();
            if (layer != null && layer.parent != null) layer.RemoveFromHierarchy();
        }

        void EnsureInset()
        {
            if (inset != null) return;
            var go = new GameObject("HavenInset");
            go.transform.SetParent(transform, false);
            inset = go.AddComponent<Camera>();
            inset.rect = new Rect(0.56f, 0.64f, 0.40f, 0.28f);
            inset.depth = 8f;
            inset.clearFlags = CameraClearFlags.Depth;
            inset.fieldOfView = 42f;
            inset.enabled = false;
            owned.Add(go);
        }
    }
}
