// Player-facing booster recovery after the first stage lets go. Simple flies the
// auto-assist law. Engineer sets pitch and throttle. The picture is the separated
// stage: grid fins, legs, the landing plume, dust or splash, and a #247 break
// when the touchdown is too hard. The ascent camera stays on the rocket until
// entry, then follows the booster. Boostback keeps a corner inset.
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
        struct Shot
        {
            public double Down, Alt, Speed, PitchDeg;
        }

        readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();

        FlightDriver flight;
        FlightStageShow staging;
        FinCluster fins;
        StrutCluster legs;
        VisualElement hud;
        Label callout, numbers;
        VisualElement stick;
        Camera inset;
        RecoveryCraft craft;
        RecoveryBody body;
        RecoveryMark[] marks = Array.Empty<RecoveryMark>();
        int markCount;
        Shot shot;
        Transform[] finBits = new Transform[4];
        Transform[] legBits = new Transform[4];
        Transform plume, deck, dustRoot;
        ParticleSystem dust;
        UnityEngine.Material hotMat, deckMat;
        Mesh cube;
        double accum, downAge;
        float units = 1f;
        bool armed, haveShot, started, engineer, reduce, boomed, touched, puffed;
        double pitchCmd = Math.PI / 2;
        double throttleCmd;

        public bool SteerCamera { get; private set; }

        public void Bind(FinCluster finCluster, StrutCluster legCluster, VisualElement flightHud)
        {
            fins = finCluster;
            legs = legCluster;
            hud = flightHud;
        }

        public void Arm(FlightDriver driver, FlightStageShow stageShow, Catalog catalog, Design design, bool engineerDepth, bool reduceMotion)
        {
            Stop();
            flight = driver;
            staging = stageShow;
            engineer = engineerDepth;
            reduce = reduceMotion;
            RecoveryLog.ArmFlight();
            if (staging == null || !staging.CoreCanRelease) return;
            if (!RecoveryCrafts.TryFrom(catalog, design, out craft)) return;
            RecoveryLog.Engage();
            armed = true;
            pitchCmd = Math.PI / 2;
            throttleCmd = 0;
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
            haveShot = false;
            SteerCamera = false;
            boomed = false;
            touched = false;
            puffed = false;
            accum = 0;
            downAge = 0;
            if (staging != null) staging.SetRecoveryOwnsBooster(false);
            if (fins != null) fins.enabled = true;
            if (legs != null) legs.enabled = true;
            if (inset != null) inset.enabled = false;
            if (callout != null) callout.style.display = DisplayStyle.None;
            if (numbers != null) numbers.style.display = DisplayStyle.None;
            if (stick != null) stick.style.display = DisplayStyle.None;
            if (deck != null) deck.gameObject.SetActive(false);
            if (plume != null) plume.gameObject.SetActive(false);
            RecoveryLog.Release();
        }

        public void Present(float dt)
        {
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
            if (body.Phase != RecoveryPhase.Down)
            {
                if (dt < 0f || float.IsNaN(dt)) dt = 0f;
                accum += Math.Min(dt, 0.1f);
                var stickIn = Stick();
                int guard = 0;
                while (guard++ < 8 && body.Phase != RecoveryPhase.Down)
                {
                    RecoveryFrames.Axes(body.X, body.Y, out _, out double alt, out _, out _, out _, out _, out _);
                    double step = alt < RecoverySim.NearAlt ? RecoverySim.NearDt : RecoverySim.Dt;
                    if (accum + 1e-9 < step) break;
                    var before = body.Phase;
                    RecoverySim.Step(ref body, craft, stickIn, step);
                    accum -= step;
                    if (body.Phase != before) Remember();
                }
            }
            else downAge += dt;
            var sample = RecoverySim.Sample(body, craft);
            if (sample.Phase == RecoveryPhase.Down && !touched)
            {
                touched = true;
                var verdict = RecoveryScore.Judge(sample, craft, TrimMarks());
                verdict.Attempted = true;
                RecoveryLog.Publish(verdict);
            }
            string cause = RecoveryLog.Current != null && RecoveryLog.Current.Attempted ? RecoveryLog.Current.Cause : null;
            var pic = RecoveryViews.Of(sample, TrimMarks(), cause, craft.VLeg, sample.Phase == RecoveryPhase.Down);
            Pose(sample);
            PoseHardware(sample);
            PosePlume(sample);
            PoseDeck();
            Touch(pic, sample);
            Call(pic, sample);
            SteerCamera = pic.Follow && (sample.Phase != RecoveryPhase.Down || downAge < 8);
            AimInset(pic.Inset && !SteerCamera, sample);
        }

        public void Aim(Camera cam, float groundY)
        {
            if (!SteerCamera || cam == null || staging == null || staging.Booster == null) return;
            var target = staging.Booster.position;
            float h = 26f;
            var offset = ChaseCamera.DesiredOffset(Vector3.up, h, (float)Math.Max(0, RecoverySim.Sample(body, craft).Alt));
            var pos = PadGround.ClampAboveGround(target + offset, groundY);
            cam.transform.position = pos;
            cam.transform.rotation = ChaseCamera.Aim(pos, target + Vector3.up * (h * 0.25f), cam.fieldOfView,
                (float)Math.Max(0, RecoverySim.Sample(body, craft).Alt), pos.y - groundY);
        }

        void Watch(float dt)
        {
            if (flight.Sim.StageIdx == staging.CoreSerial && flight.HasAttitude)
            {
                shot = new Shot
                {
                    Down = flight.Downrange,
                    Alt = flight.Altitude,
                    Speed = flight.Speed,
                    PitchDeg = flight.AttitudeDeg,
                };
                haveShot = true;
            }
            bool over = flight.Lost || (flight.Result != null && !flight.Running);
            if (!started && haveShot && flight.Sim.StageIdx > staging.CoreSerial)
            {
                Begin();
                return;
            }
            if (over)
            {
                RecoveryLog.Release();
                armed = false;
            }
        }

        void Begin()
        {
            double rad = shot.PitchDeg * Math.PI / 180.0;
            double vEast = shot.Speed * Math.Cos(rad);
            double vUp = shot.Speed * Math.Sin(rad);
            body = RecoverySim.Begin(craft, shot.Down, shot.Alt, vEast, vUp);
            marks = new RecoveryMark[8];
            markCount = 0;
            Remember();
            started = true;
            staging.SetRecoveryOwnsBooster(true);
            if (fins != null) fins.enabled = false;
            if (legs != null) legs.enabled = false;
            BuildHardware();
            BuildDeck();
            EnsureChrome();
        }

        RecoveryStick Stick()
        {
            if (!engineer) return RecoveryStick.Assisted;
            return new RecoveryStick { Auto = false, PitchRad = pitchCmd, Throttle = throttleCmd };
        }

        void Remember()
        {
            if (markCount > 0 && marks[markCount - 1].Phase == body.Phase) return;
            if (markCount >= marks.Length)
            {
                var grown = new RecoveryMark[marks.Length * 2];
                for (int i = 0; i < marks.Length; i++) grown[i] = marks[i];
                marks = grown;
            }
            var sample = RecoverySim.Sample(body, craft);
            marks[markCount++] = new RecoveryMark
            {
                Phase = body.Phase,
                T = body.T,
                Alt = sample.Alt,
                Speed = sample.Speed,
                Sink = sample.Sink,
                TiltDeg = sample.TiltDeg,
                MissM = sample.MissM,
                Prop = sample.Prop,
            };
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
            bool on = sample.Throttle > 0.02 && sample.Phase != RecoveryPhase.Down;
            plume.gameObject.SetActive(on);
            if (!on) return;
            float len = 2.2f + 10f * (float)sample.Throttle;
            plume.localScale = new Vector3(0.55f, len, 0.55f);
            plume.localPosition = new Vector3(0f, -len * 0.5f, 0f);
        }

        void PoseDeck()
        {
            if (deck == null || flight == null) return;
            deck.gameObject.SetActive(true);
            var space = flight.Vehicle != null ? flight.Vehicle.parent : null;
            var local = new Vector3((float)(body.BargeArc - flight.OriginX), (float)(0 - flight.OriginY), 0f);
            deck.position = space != null ? space.TransformPoint(local) : local;
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
            if (reduce || staging == null || staging.Booster == null) return;
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
            if (callout != null)
            {
                if (pic.Call.Visible && !string.IsNullOrEmpty(pic.Call.Line))
                {
                    callout.text = pic.Call.Line;
                    callout.style.display = DisplayStyle.Flex;
                }
                else callout.style.display = DisplayStyle.None;
            }
            if (numbers != null)
            {
                numbers.style.display = engineer ? DisplayStyle.Flex : DisplayStyle.None;
                if (engineer)
                {
                    var inv = CultureInfo.InvariantCulture;
                    numbers.text = sample.Phase + "  sink " + sample.Sink.ToString("0.0", inv)
                        + " m/s  tilt " + sample.TiltDeg.ToString("0.0", inv)
                        + "°  miss " + sample.MissM.ToString("0", inv)
                        + " m  prop " + sample.Prop.ToString("0", inv) + " kg";
                }
            }
            if (stick != null) stick.style.display = engineer && sample.Phase != RecoveryPhase.Down ? DisplayStyle.Flex : DisplayStyle.None;
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
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        void BuildDeck()
        {
            if (deck != null) return;
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
            var go = MakeBit("Haven", deckMat);
            go.transform.localScale = new Vector3((float)RecoverySim.DeckRadiusM * 2f, 0.35f, (float)RecoverySim.DeckRadiusM * 2f);
            deck = go.transform;
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

        RecoveryMark[] TrimMarks()
        {
            int n = markCount;
            if (n < 0) n = 0;
            if (n > marks.Length) n = marks.Length;
            var kept = new RecoveryMark[n];
            for (int i = 0; i < n; i++) kept[i] = marks[i];
            return kept;
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
                    callout.style.whiteSpace = WhiteSpace.NoWrap;
                    callout.style.color = new Color(0.55f, 0.9f, 1f);
                    callout.style.backgroundColor = new Color(0.03f, 0.055f, 0.09f, 0.82f);
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
                if (stick == null || stick.parent != host)
                {
                    stick = host.Q("recovery-stick");
                    if (stick == null)
                    {
                        stick = new VisualElement { name = "recovery-stick" };
                        stick.style.position = Position.Absolute;
                        stick.style.flexDirection = FlexDirection.Row;
                        stick.style.display = DisplayStyle.None;
                        stick.Add(Hold("Pitch −", () => pitchCmd -= 0.08));
                        stick.Add(Hold("Pitch +", () => pitchCmd += 0.08));
                        stick.Add(Hold("Thr −", () => throttleCmd = Math.Max(0, throttleCmd - 0.1)));
                        stick.Add(Hold("Thr +", () => throttleCmd = Math.Min(1, throttleCmd + 0.1)));
                        host.Add(stick);
                    }
                }
            }
            PlaceChrome();
        }

        Button Hold(string label, Action tap)
        {
            var button = new Button(tap) { text = label };
            button.style.height = 36f * units;
            button.style.marginRight = 4f * units;
            button.style.fontSize = 12f * units;
            return button;
        }

        void PlaceChrome()
        {
            float u = units;
            float top = (FlightStaging.CalloutTopInsideHud() + FlightStaging.CalloutPt + 6f) * u;
            if (callout != null)
            {
                callout.style.top = top;
                callout.style.height = FlightStaging.CalloutPt * u;
                callout.style.fontSize = FlightHudLayout.LabelFontPt * u;
                callout.style.left = FlightHudLayout.SideMarginPt * u;
                callout.style.right = FlightHudLayout.SideMarginPt * u;
            }
            if (numbers != null)
            {
                numbers.style.top = top + (FlightStaging.CalloutPt + 4f) * u;
                numbers.style.left = FlightHudLayout.SideMarginPt * u;
                numbers.style.right = FlightHudLayout.SideMarginPt * u;
                numbers.style.fontSize = 11f * u;
            }
            if (stick != null)
            {
                stick.style.bottom = 18f * u;
                stick.style.left = 12f * u;
                stick.style.right = 12f * u;
            }
        }

        void OnDestroy()
        {
            for (int i = 0; i < owned.Count; i++)
                if (owned[i] != null) Destroy(owned[i]);
            if (callout != null && callout.parent != null) callout.RemoveFromHierarchy();
            if (numbers != null && numbers.parent != null) numbers.RemoveFromHierarchy();
            if (stick != null && stick.parent != null) stick.RemoveFromHierarchy();
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
