// Plays one failure on the vehicle already built by SliceApp, then opens the post-flight card.
// Registered once from SliceApp after the #187–#192 detail art landed on main.
// Particles use URP Particles/Unlit with the same keywords PadGlow already enables.
// No new shader keyword, so the iOS stripper keeps the variant that build 101 lost.
using System.Collections.Generic;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using Kestrel.Slice;
using Kestrel.Vehicle;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Kestrel.Game.Failure
{
    public sealed class FailureHost : MonoBehaviour
    {
        public const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";

        FlightDriver flight;
        VisualElement ui;
        FailureCue cue = new FailureCue();
        Vector3[] exits = new Vector3[0];
        readonly TelemetryLog log = new TelemetryLog();
        bool played;
        bool reported;
        float age;
        bool reduce;
        FailureEvent failure;
        FailurePlan plan = new FailurePlan();
        int seenEvents;
        double nextSample = -1, prevT, prevV, lossG, lossD, lossS;
        Quaternion baseRotation = Quaternion.identity;
        Mesh lowerMesh, upperMesh;
        Transform upper;
        Transform[] bits = new Transform[8];
        Transform marker;
        ParticleSystem burst;
        bool emitted;
        PlumeController plume;
        Material particleMat;
        readonly FailurePool pool = new FailurePool();

        public TelemetryLog Log => log;
        public FailurePlan Plan => plan;
        public bool Played => played;
        public VisualElement Card { get; private set; }

        public static FailureHost Attach(GameObject rocket, FlightDriver flight, VisualElement ui, RocketMeshBuilder.Result built)
        {
            if (rocket == null) return null;
            var host = rocket.GetComponent<FailureHost>();
            if (host == null) host = rocket.AddComponent<FailureHost>();
            host.Bind(flight, ui, built);
            return host;
        }

        public void Bind(FlightDriver flightDriver, VisualElement root, RocketMeshBuilder.Result built)
        {
            flight = flightDriver;
            ui = root;
            plume = flight != null ? flight.Plume : null;
            cue = FromRocket(built);
            exits = Exits(built);
        }

        /// <summary>Capture and tests: play a failure without a running flight.</summary>
        public void Play(FailureEvent ev, FailureCue scene, Vector3[] engines, bool? reduceMotion)
        {
            if (scene != null) cue = scene;
            if (engines != null) exits = engines;
            reduce = reduceMotion ?? Comfort.ReduceMotion;
            Begin(ev);
        }

        public void Seek(float seconds)
        {
            age = seconds < 0f ? 0f : seconds;
            ApplyPose();
        }

        void Update()
        {
            if (!Application.isPlaying || flight == null) return;
            Sample();
            if (played)
            {
                Seek(age + Time.deltaTime);
                return;
            }
            var result = flight.Result;
            if (result == null || reported) return;
            if (result.failure == null)
            {
                if (flight.Running) return;
                if (RecoveryLog.Active) return;
                reported = true;
                reduce = Comfort.ReduceMotion;
                StampLosses(result);
                failure = null;
                ShowCard();
                return;
            }
            reduce = Comfort.ReduceMotion;
            StampLosses(result);
            Begin(result.failure);
            reported = true;
        }

        void Begin(FailureEvent ev)
        {
            failure = ev;
            played = true;
            age = 0f;
            baseRotation = transform.localRotation;
            if (ev != null)
            {
                cue.EngineIndex = ev.engineIndex;
                if (ev.kind == "structural-maxq" && ev.cause == "q") cue.QPa = (float)ev.value;
                if (ev.kind == "leg-collapse") { cue.Sink = (float)ev.value; cue.VLeg = (float)ev.limit; }
            }
            SeedIfEmpty(ev);
            plan = FailureFx.Plan(ev != null ? ev.kind : "", ev != null ? ev.cause : "", cue, 0f, reduce);
            ApplyPose();
            ShowCard();
        }

        void ApplyPose()
        {
            plan = FailureFx.Plan(failure != null ? failure.kind : "", failure != null ? failure.cause : "", cue, age, reduce);
            if (plan.Mode == FailureFx.BreakMode || plan.Mode == FailureFx.TumbleMode || plan.Mode == FailureFx.BumpMode)
                EnsureSplit();
            if (upper != null)
            {
                upper.localPosition = new Vector3(plan.Shake, plan.Gap, 0f);
                float spin = plan.Mode == FailureFx.BreakMode ? plan.AngleDeg : plan.AngleDeg * 0.25f;
                upper.localRotation = Quaternion.Euler(spin * 0.15f, 0f, spin);
            }
            if (plan.Mode == FailureFx.TumbleMode || plan.Mode == FailureFx.BumpMode || plan.Mode == FailureFx.FoldMode)
                transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, plan.AngleDeg);
            if (plan.Mode == FailureFx.FoldMode) PlaceLegs();
            else if (plan.Mode == FailureFx.BreakMode) PlaceShards();
            else HideBits();
            ShowMarker();
            CutPlume();
            EmitOnce();
        }

        void ShowMarker()
        {
            bool on = plan.Mode == FailureFx.BellMode || plan.Mode == FailureFx.SputterMode
                || plan.Mode == FailureFx.GlowMode || plan.Mode == FailureFx.FrostMode
                || plan.Mode == FailureFx.BreakMode;
            if (!on)
            {
                if (marker != null) marker.gameObject.SetActive(false);
                return;
            }
            if (marker == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "FailureMarker";
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                go.transform.SetParent(transform, false);
                marker = go.transform;
            }
            marker.gameObject.SetActive(true);
            marker.localPosition = BurstPoint(0f);
            float size = plan.Mode == FailureFx.BreakMode ? 2.4f + plan.Gap * 0.45f : plan.Flash ? 1.8f : 1.1f;
            marker.localScale = Vector3.one * size;
            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (renderer.material != null && renderer.material.HasProperty("_BaseColor"))
                    renderer.material.SetColor("_BaseColor", Tint(plan.Tint));
                else if (renderer.material != null) renderer.material.color = Tint(plan.Tint);
            }
        }

        void EnsureSplit()
        {
            if (plan.Gap <= 0.01f && plan.Mode != FailureFx.BreakMode) return;
            if (upper != null) return;
            var filter = GetComponent<MeshFilter>();
            var renderer = GetComponent<MeshRenderer>();
            if (filter == null || filter.sharedMesh == null) return;
            FailureCut.Halves(filter.sharedMesh, plan.JointY, out lowerMesh, out upperMesh);
            if (lowerMesh.vertexCount > 0) filter.sharedMesh = lowerMesh;
            if (upperMesh.vertexCount == 0) return;
            var go = new GameObject("FailureUpper");
            go.transform.SetParent(transform, false);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = upperMesh;
            var mr = go.AddComponent<MeshRenderer>();
            if (renderer != null) mr.sharedMaterial = renderer.sharedMaterial;
            upper = go.transform;
        }

        void PlaceShards()
        {
            if (plan.Gap < 0.4f) { HideBits(); return; }
            const int n = 8;
            EnsureBits(n);
            for (int i = 0; i < n; i++)
            {
                var shard = FailureFx.Shard(i, n, plan.Gap, plan.JointY, age);
                var bit = bits[i];
                if (bit == null) continue;
                bit.gameObject.SetActive(true);
                bit.localPosition = new Vector3(shard.X, shard.Y, shard.Z);
                bit.localRotation = Quaternion.Euler(shard.Spin, shard.Spin * 0.7f, i * 20f);
                bit.localScale = new Vector3(shard.Scale, shard.Scale * 1.5f, shard.Scale);
            }
        }

        void PlaceLegs()
        {
            var legs = new LegPose[LandingLayout.Count];
            FailureFx.FoldedLegs(cue.Radius, cue.AftY, plan.Fold01, legs);
            EnsureBits(LandingLayout.Count * 2);
            for (int i = 0; i < legs.Length; i++)
            {
                PoseBit(bits[i * 2], legs[i].HipX, legs[i].HipY, legs[i].HipZ, legs[i].KneeX, legs[i].KneeY, legs[i].KneeZ);
                PoseBit(bits[i * 2 + 1], legs[i].KneeX, legs[i].KneeY, legs[i].KneeZ, legs[i].FootX, legs[i].FootY, legs[i].FootZ);
            }
        }

        void PoseBit(Transform bit, float x0, float y0, float z0, float x1, float y1, float z1)
        {
            if (bit == null) return;
            bit.gameObject.SetActive(true);
            var a = new Vector3(x0, y0, z0);
            var b = new Vector3(x1, y1, z1);
            var span = b - a;
            float len = span.magnitude;
            bit.localPosition = (a + b) * 0.5f;
            bit.localRotation = len > 1e-4f ? Quaternion.FromToRotation(Vector3.up, span / len) : Quaternion.identity;
            bit.localScale = new Vector3(0.08f, Mathf.Max(0.05f, len * 0.5f), 0.08f);
        }

        void EnsureBits(int count)
        {
            if (bits.Length < count) bits = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                if (bits[i] != null) continue;
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "FailureLeg";
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                go.transform.SetParent(transform, false);
                bits[i] = go.transform;
            }
        }

        void HideBits()
        {
            for (int i = 0; i < bits.Length; i++)
                if (bits[i] != null) bits[i].gameObject.SetActive(false);
        }

        void CutPlume()
        {
            if (plume == null) plume = GetComponentInChildren<PlumeController>();
            if (plume == null) return;
            double ambient = flight != null ? flight.AmbientPressure : Atmosphere.P0;
            double speed = flight != null ? flight.Speed : 0;
            plume.Apply(plan.PlumeScale, ambient, speed);
        }

        void EmitOnce()
        {
            if (emitted || plan.Burst <= 0) return;
            emitted = true;
            var ps = Burst();
            if (ps == null) return;
            int n = plan.Burst;
            var emit = new ParticleSystem.EmitParams();
            emit.applyShapeToPosition = false;
            var tint = Tint(plan.Tint);
            emit.startColor = tint;
            bool blast = plan.Mode == FailureFx.BreakMode;
            emit.startSize = blast ? 3.4f : plan.Mode == FailureFx.BellMode ? 0.8f : 1.6f;
            emit.startLifetime = blast ? 0.85f : reduce ? 8f : 1.4f;
            for (int i = 0; i < n; i++)
            {
                pool.Acquire();
                float ang = i * Mathf.PI * 2f / n;
                Vector3 at = BurstPoint(ang);
                Vector3 vel = Vector3.zero;
                if (!reduce && plan.ParticleSpeed > 0f)
                {
                    var dir = new Vector3(Mathf.Cos(ang), blast ? 1.35f : 0.6f, Mathf.Sin(ang));
                    vel = dir.normalized * plan.ParticleSpeed;
                }
                emit.position = transform.TransformPoint(at);
                emit.velocity = vel;
                ps.Emit(emit, 1);
            }
        }

        Vector3 BurstPoint(float ang)
        {
            if (plan.Mode == FailureFx.BellMode || plan.Mode == FailureFx.SputterMode)
            {
                int i = plan.EngineIndex;
                if (exits != null && i >= 0 && i < exits.Length) return exits[i];
                return Vector3.zero;
            }
            float r = Mathf.Max(0.4f, cue.Radius);
            return new Vector3(Mathf.Cos(ang) * r, plan.JointY, Mathf.Sin(ang) * r);
        }

        ParticleSystem Burst()
        {
            if (burst != null) return burst;
            var go = new GameObject("FailureBurst");
            go.transform.SetParent(transform, false);
            burst = go.AddComponent<ParticleSystem>();
            burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = burst.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = FailurePool.Capacity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            var emission = burst.emission;
            emission.enabled = false;
            var sh = burst.shape;
            sh.enabled = false;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            particleMat = ParticleMaterial();
            if (particleMat != null) renderer.sharedMaterial = particleMat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            burst.Play();
            return burst;
        }

        static Material ParticleMaterial()
        {
            var shader = Shader.Find(ParticleShader);
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return null;
            var mat = new Material(shader) { name = "FailureBurst" };
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 2f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.One);
            mat.SetFloat("_ZWrite", 0f);
            // Keywords already required by PadGlow. Soft particles stay off.
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_BLENDMODE_ADD");
            mat.DisableKeyword("_SOFTPARTICLES_ON");
            mat.SetOverrideTag("RenderType", "Transparent");
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", SoftParticle.Texture);
            mat.mainTexture = SoftParticle.Texture;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            return mat;
        }

        static Color Tint(string name)
        {
            switch (name)
            {
                case "green": return new Color(0.45f, 0.95f, 0.35f, 0.9f);
                case "frost": return new Color(0.75f, 0.9f, 1f, 0.85f);
                case "white": return new Color(1f, 0.95f, 0.85f, 1f);
                case "dim": return new Color(0.55f, 0.45f, 0.3f, 0.7f);
                case "none": return new Color(0f, 0f, 0f, 0f);
                default: return new Color(1f, 0.5f, 0.18f, 0.9f);
            }
        }

        void ShowCard()
        {
            // A finished flight with no failure event still owes the report. Only a missing tree bails.
            if (ui == null) return;
            var old = ui.Q(ReportView.RootName);
            if (old != null && old.userData == this) old.RemoveFromHierarchy();
            else if (old != null) old.RemoveFromHierarchy();
            float dpr = Screen.dpi > 160f ? Mathf.Max(1f, Mathf.Round(Screen.dpi / 160f)) : 2f;
            float wPt = Screen.width > 8 ? Screen.width / dpr : 375f;
            float hPt = Screen.height > 8 ? Screen.height / dpr : 667f;
            if (hPt < wPt) { float s = wPt; wPt = hPt; hPt = s; }
            var safe = Screen.safeArea;
            float top = Screen.height > 8 ? (Screen.height - safe.yMax) / dpr : 20f;
            float bottom = Screen.height > 8 ? safe.yMin / dpr : 0f;
            float left = Screen.width > 8 ? safe.xMin / dpr : 0f;
            float right = Screen.width > 8 ? (Screen.width - safe.xMax) / dpr : 0f;
            var frame = ReportLayout.Solve(wPt, hPt, top, bottom, left, right, reduce);
            float upp = 1f;
            if (ui.layout.width > 1f && wPt > 1f) upp = ui.layout.width / wPt;
            else if (dpr > 1f) upp = dpr;
            var shown = failure;
            if (shown == null && RecoveryLog.Current != null && RecoveryLog.Current.Attempted && RecoveryLog.Current.Failure != null)
                shown = RecoveryLog.Current.Failure;
            FlightDebrief debrief = null;
            try { debrief = PostFlightSource.Publish(flight, failure); }
            catch (System.Exception e) { Debug.LogWarning("[Kestrel] post-flight report: " + e.Message); }
            var actions = PostFlightSource.Actions();
            string kind = shown != null ? shown.kind : "";
            var design = ui.Q("design");
            if (design != null) design.AddToClassList("hidden");
            System.Action reveal = () =>
            {
                var sheet = ui.Q("design");
                if (sheet != null) sheet.RemoveFromClassList("hidden");
            };
            if (actions != null)
            {
                var next = actions.Next;
                var again = actions.Again;
                var missions = actions.Missions;
                var home = actions.Home;
                System.Action leave = () =>
                {
                    ReportCardChrome.Dismiss(ui);
                    reveal();
                };
                actions.Next = () => { leave(); if (next != null) next(); };
                actions.Again = () => { leave(); if (again != null) again(); };
                actions.Missions = () => { leave(); if (missions != null) missions(); };
                actions.Home = () => { leave(); if (home != null) home(); };
                actions.Fix = () => { leave(); FixField.Apply(ui, FixField.For(kind)); };
            }
            Card = ReportView.Build(shown, log, frame, reduce, () => { reveal(); FixField.Apply(ui, FixField.For(kind)); }, debrief, actions);
            Card.userData = this;
            ReportCardChrome.Present(ui, Card, upp, top, bottom);
            if (ui.layout.width <= 1f)
            {
                var sheet = ui;
                var shownCard = Card;
                EventCallback<GeometryChangedEvent> relayout = null;
                relayout = _ =>
                {
                    sheet.UnregisterCallback(relayout);
                    float live = sheet.layout.width > 1f && wPt > 1f ? sheet.layout.width / wPt : upp;
                    ReportCardChrome.Dress(shownCard, live, top, bottom);
                    ReportCardChrome.Cover(shownCard);
                    ReportCardChrome.RetireFlight(sheet);
                    shownCard.BringToFront();
                };
                sheet.RegisterCallback(relayout);
            }
        }

        void Sample()
        {
            if (flight.Sim == null) return;
            var events = flight.Sim.Events;
            while (events != null && seenEvents < events.Count)
            {
                var ev = events[seenEvents++];
                bool fail = ev.type == "failure";
                string kind = fail && flight.Result != null && flight.Result.failure != null ? flight.Result.failure.kind : ev.type;
                log.Mark(ev.t, kind, fail);
            }
            double t = flight.SimTime;
            if (t + 1e-4 < nextSample) return;
            nextSample = t + 0.1;
            double v = flight.Speed;
            double dt = t - prevT;
            double g = dt > 1e-3 ? (v - prevV) / dt / FailureModel.G0 : 0;
            double mass = flight.Sim.Y != null && flight.Sim.Y.Length > 4 ? flight.Sim.Y[4] : 0;
            if (flight.Result != null) StampLosses(flight.Result);
            log.Push(t, flight.Altitude, v, flight.DynPressure, g, mass, lossG, lossD, lossS);
            prevT = t;
            prevV = v;
        }

        void StampLosses(SimResult result)
        {
            if (result == null) return;
            lossG = result.lossGravity;
            lossD = result.lossDrag;
            lossS = result.lossSteering;
            log.StampLatestLosses(lossG, lossD, lossS);
        }

        void SeedIfEmpty(FailureEvent ev)
        {
            if (log.Count > 0 || ev == null) return;
            double t = ev.t > 0 ? ev.t : 1;
            for (int i = 0; i <= 8; i++)
            {
                double u = i / 8.0;
                log.Push(t * u, 1200 * u, 220 * u, cue.QPa * u, 1.2 + u, 8000 * (1 - 0.3 * u), 40 * u, 12 * u, 6 * u);
            }
            log.Mark(t, ev.kind, true);
        }

        static FailureCue FromRocket(RocketMeshBuilder.Result built)
        {
            var cue = new FailureCue();
            if (built == null) return cue;
            var joints = new List<FailureJoint>();
            if (built.interstages != null)
                for (int i = 0; i < built.interstages.Count; i++)
                {
                    var s = built.interstages[i];
                    FailureFx.AddInterstage(joints, s.Y0, s.Y1, s.R0, s.R1);
                }
            if (built.fairing.Present)
            {
                var f = built.fairing;
                FailureFx.AddFairing(joints, f.BaseY, f.Radius, f.Length, f.NoseFraction, f.Conic);
            }
            cue.Joints = joints.ToArray();
            cue.EngineCount = built.engines != null ? built.engines.Count : 0;
            if (built.tanks != null)
                for (int i = 0; i < built.tanks.Count; i++)
                    if (built.tanks[i].Stage == 0 && !built.tanks[i].StrapOn) cue.Radius = built.tanks[i].Radius;
            cue.AftY = built.stage1AftY;
            return cue;
        }

        static Vector3[] Exits(RocketMeshBuilder.Result built)
        {
            if (built == null || built.engines == null || built.engines.Count == 0) return new Vector3[0];
            var exits = new Vector3[built.engines.Count];
            for (int i = 0; i < exits.Length; i++) exits[i] = built.engines[i].Exit;
            return exits;
        }

        void OnDestroy()
        {
            if (Card != null && Card.parent != null) Card.RemoveFromHierarchy();
            if (lowerMesh != null) Destroy(lowerMesh);
            if (upperMesh != null) Destroy(upperMesh);
            if (particleMat != null) Destroy(particleMat);
        }
    }
}
