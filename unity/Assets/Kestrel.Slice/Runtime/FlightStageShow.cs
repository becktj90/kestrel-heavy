// Flight staging picture. Sim events (burnout, cutoff, separation, ignition, fairing) drive
// plume collapse, a kinematic tumble of the spent mesh, the next plume, and fairing halves.
// The combined rocket stays the picture until a body actually leaves. FailureHost still owns the
// breakup mesh. Narration is offered and not spoken — flight Thrum stays silent.
using System.Collections.Generic;
using Kestrel.Core.Data;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using Kestrel.Game.Failure;
using Kestrel.Kit;
using Kestrel.Vehicle;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class FlightStageShow : MonoBehaviour
    {
        sealed class Piece
        {
            public string Role;
            public int Serial, Index;
            public Transform Pivot;
            public Vector3 Rest;
            public float SepY;
            public bool Shown;
            public int Debris;   // DebrisFx rigid-body handle once separated, else 0
        }

        sealed class Half
        {
            public Transform Pivot;
            public float Mid, Sign;
            public int Debris;
        }

        struct KeptMount
        {
            public BellMount Bell;
            public bool Strap;
        }

        readonly FlightStaging staging = new FlightStaging();
        readonly List<Piece> pieces = new List<Piece>();
        readonly List<KeptMount> mounts = new List<KeptMount>();
        readonly List<Object> owned = new List<Object>();
        readonly SilentFlightNarration silent = new SilentFlightNarration();

        IFlightStagingNarration narration;
        FlightDriver flight;
        VisualElement hud;
        Label chip;
        float units = 1f;
        Transform root;
        MeshFilter bodyFilter;
        Mesh original;
        Mesh clipped;
        int[] groups = System.Array.Empty<int>();
        float[] nozzleY = System.Array.Empty<float>();
        Piece[] bySerialStage;
        Half leftHalf, rightHalf;
        ParticleSystem puff, flashPs;
        PlumeController plume;
        float fairingLen = 12f, fairingRadius = 2f;
        bool fairingRaised;
        float fullDiameter = 1f, coreDiameter = 1f, upperDiameter = 1f, coreRadius = 1f;
        int boosterSerial = -1, coreSerial, builtMask = -1, bellMask = -1, flashedSerial = -2;
        bool armed, hasFairing, hasBoosters, coreReleases, reduce, recoveryOwns, canLand;
        double playT, jettisonM = 110000;
        string[] fuelBySerial;
        // Since #288 the painted Hangar parts are the visible rocket and the combined mesh is undrawn, so ApplyMask
        // alone hides nothing on screen. These hide the painted part a moving piece replaces (StagingPartVisibility).
        HangarRocketView partsView;
        Behaviour fairingSeams;
        StagingPartVisibility partVisibility;

        public IFlightStagingNarration Narration => narration ?? silent;

        /// <summary>The first stage can leave the stack. A single-stage rocket never does.</summary>
        public bool CoreCanRelease => coreReleases;

        public int CoreSerial => coreSerial;

        /// <summary>
        /// Base of the stack still attached, metres up the vehicle axis from the pivot: the top of the
        /// highest stage the sim has released (0 before the first separation). The flight camera frames
        /// the stack from here, so the upper stage is not framed as if the spent core were still on it.
        /// </summary>
        public float KeptBase
        {
            get
            {
                if (!armed || bySerialStage == null) return 0f;
                float y = 0f;
                for (int i = 0; i < bySerialStage.Length; i++)
                {
                    var p = bySerialStage[i];
                    if (p != null && staging.Released(p.Serial, playT) && p.SepY > y) y = p.SepY;
                }
                return y;
            }
        }

        /// <summary>Separated first-stage mesh. Recovery poses this instead of the kinematic tumble.</summary>
        public Transform Booster
        {
            get
            {
                if (bySerialStage == null || bySerialStage.Length == 0 || bySerialStage[0] == null) return null;
                return bySerialStage[0].Pivot;
            }
        }

        public void SetRecoveryOwnsBooster(bool owns) => recoveryOwns = owns;

        /// <summary>Replace the sink. Null keeps the silent hook. This does not start Thrum.</summary>
        public void SetNarration(IFlightStagingNarration sink) => narration = sink;

        public void Prepare(Catalog catalog, Design design, RocketMeshBuilder.Result built, VisualElement flightHud)
            => Prepare(catalog, design, built, flightHud, null, null);

        /// <param name="paintedParts">The painted per-part rocket (SliceApp's HangarRocketView). Released stages, the booster
        /// set and the separated fairing are hidden on it, and the payload is shown after fairing separation.</param>
        /// <param name="seams">The fairing's seam, base-ring and shoulder cluster; disabled when the fairing separates.</param>
        public void Prepare(Catalog catalog, Design design, RocketMeshBuilder.Result built, VisualElement flightHud, HangarRocketView paintedParts, Behaviour seams)
        {
            ClearGeometry();
            ResetParts();
            partsView = paintedParts;
            fairingSeams = seams;
            partVisibility = null;
            hud = flightHud;
            narration = silent;
            canLand = false;
            fuelBySerial = null;
            if (catalog == null || design == null || built == null || design.stages == null || design.stages.Count == 0) return;
            bodyFilter = GetComponent<MeshFilter>();
            original = bodyFilter != null ? bodyFilter.sharedMesh : null;
            plume = GetComponentInChildren<PlumeController>();
            if (plume != null) fullDiameter = Mathf.Max(0.4f, plume.ExitDiameter);
            hasFairing = design.fairingLength > 0.2 && design.fairingDiameter > 0.1;
            jettisonM = design.fairingJettisonAltitudeKm > 0 ? design.fairingJettisonAltitudeKm * 1000 : 110000;
            hasBoosters = design.StrapOnCount > 0;
            canLand = CanRecover(design);
            fuelBySerial = null;

            List<SerialStage> phases;
            try { phases = Stack.SerialStages(catalog, design).stages; }
            catch (System.Exception e) { Debug.LogWarning("[Staging] stack skipped: " + e.Message); return; }
            var bodies = FlightStagingPlan.Bodies(phases, design.StrapOnCount);
            nozzleY = new float[Mathf.Max(1, phases != null ? phases.Count : 1)];
            boosterSerial = -1;
            coreSerial = 0;
            coreReleases = false;
            for (int i = 0; i < bodies.Count; i++)
            {
                if (bodies[i].Role == "booster") boosterSerial = bodies[i].Serial;
                if (bodies[i].Role != "stage") continue;
                coreSerial = bodies[i].Serial;
                coreReleases = bodies[i].Releases;
                break;
            }
            RememberFuel(design, bodies);
            partVisibility = new StagingPartVisibility(bodies, design.stages.Count, hasFairing);

            List<RocketMeshBuilder.PartMesh> parts;
            try { parts = RocketMeshBuilder.BuildParts(catalog, design); }
            catch (System.Exception e) { Debug.LogWarning("[Staging] parts skipped: " + e.Message); return; }

            root = new GameObject("StagingShow").transform;
            root.SetParent(transform, false);
            var mat = GetComponent<MeshRenderer>() != null ? GetComponent<MeshRenderer>().sharedMaterial : null;
            int stageCount = design.stages.Count;
            bySerialStage = new Piece[stageCount];
            var stageY0 = new float[stageCount];
            var stageY1 = new float[stageCount];
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                if (p == null || p.Mesh == null) continue;
                owned.Add(p.Mesh);
                if (p.Part.kind == PartKind.Stage && p.Part.index >= 0 && p.Part.index < stageCount)
                {
                    stageY0[p.Part.index] = p.Y0;
                    stageY1[p.Part.index] = p.Y1;
                }
            }
            float fairingBase = built.fairing.Present ? built.fairing.BaseY : float.PositiveInfinity;
            for (int i = 0; i < stageCount; i++)
            {
                float top = i + 1 < stageCount ? stageY0[i + 1] : (built.fairing.Present ? fairingBase : stageY1[i]);
                if (top < stageY1[i]) top = stageY1[i];
                stageY1[i] = top;
            }
            for (int i = 0; i < bodies.Count; i++)
            {
                if (bodies[i].Role != "stage") continue;
                int idx = bodies[i].Index;
                if (idx >= 0 && idx < stageY0.Length && bodies[i].Serial >= 0 && bodies[i].Serial < nozzleY.Length)
                    nozzleY[bodies[i].Serial] = stageY0[idx];
            }

            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                if (p == null || p.Mesh == null) continue;
                if (p.Part.kind == PartKind.Stage)
                {
                    int index = p.Part.index;
                    int serial = SerialOf("stage", index, bodies);
                    var piece = MakePiece("stage", serial, index, Vector3.zero, (p.Y0 + stageY1[index]) * 0.5f, p.Mesh, mat);
                    piece.SepY = stageY1[index];
                    if (index >= 0 && index < bySerialStage.Length) bySerialStage[index] = piece;
                    pieces.Add(piece);
                }
                else if (p.Part.kind == PartKind.Boosters && p.Instances != null)
                {
                    float mid = (p.Y0 + p.Y1) * 0.5f;
                    for (int k = 0; k < p.Instances.Length; k++)
                    {
                        int serial = SerialOf("booster", k, bodies);
                        var piece = MakePiece("booster", serial, k, p.Instances[k], mid, p.Mesh, mat);
                        piece.SepY = mid;
                        pieces.Add(piece);
                    }
                }
                else if (p.Part.kind == PartKind.Fairing && hasFairing)
                {
                    float mid = (p.Y0 + p.Y1) * 0.5f;
                    fairingLen = Mathf.Max(1f, p.Y1 - p.Y0);
                    fairingRadius = Mathf.Max(0.3f, (float)design.fairingDiameter * 0.5f);
                    leftHalf = MakeHalf(p.Mesh, mat, mid, -1f);
                    rightHalf = MakeHalf(p.Mesh, mat, mid, 1f);
                }
            }
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                if (p == null || p.Mesh == null || p.Part.kind != PartKind.Interstage) continue;
                int lower = p.Part.index - 1;
                if (lower < 0 || bySerialStage == null || lower >= bySerialStage.Length) continue;
                var host = bySerialStage[lower];
                if (host == null || host.Pivot == null) continue;
                var go = new GameObject("Interstage");
                go.layer = gameObject.layer;
                go.transform.SetParent(host.Pivot, false);
                go.transform.localPosition = new Vector3(0f, -host.Rest.y, 0f);
                go.transform.localRotation = Quaternion.identity;
                go.AddComponent<MeshFilter>().sharedMesh = p.Mesh;
                var mr = go.AddComponent<MeshRenderer>();
                if (mat != null) mr.sharedMaterial = mat;
            }

            if (original != null) groups = Classify(original.vertices, built, stageY0, stageY1, fairingBase);
            RememberBells(built, design);
            coreDiameter = CoreFootprint(built);
            if (!hasBoosters) coreDiameter = fullDiameter;
            upperDiameter = Mathf.Max(0.4f, coreDiameter * 0.62f);
            var puffMat = plume != null && plume.Body != null ? plume.Body.GetComponent<ParticleSystemRenderer>()?.sharedMaterial : null;
            puff = MakeBurst("SepPuff", puffMat, 0.5f, 1.5f);
            flashPs = MakeBurst("IgnitionFlash", puffMat, 0.28f, 2.2f);
        }

        public void Arm(FlightDriver driver, bool reduceMotion)
        {
            flight = driver;
            reduce = reduceMotion;
            armed = flight != null;
            recoveryOwns = false;
            playT = 0;
            builtMask = -1;
            bellMask = -1;
            flashedSerial = -2;
            fairingRaised = false;
            CinematicEvents.DebrisClear();
            if (leftHalf != null) leftHalf.Debris = 0;
            if (rightHalf != null) rightHalf.Debris = 0;
            staging.Reset(hasFairing, jettisonM, hasBoosters ? BoosterCount() : 0, reduceMotion);
            staging.RecoverBooster = canLand;
            RestoreMesh();
            for (int i = 0; i < pieces.Count; i++)
            {
                pieces[i].Shown = false;
                pieces[i].Debris = 0;
                if (pieces[i].Pivot != null)
                {
                    pieces[i].Pivot.gameObject.SetActive(false);
                    pieces[i].Pivot.localPosition = pieces[i].Rest;
                    pieces[i].Pivot.localRotation = Quaternion.identity;
                }
            }
            if (leftHalf != null) leftHalf.Pivot.gameObject.SetActive(false);
            if (rightHalf != null) rightHalf.Pivot.gameObject.SetActive(false);
            if (plume != null) plume.ExitDiameter = fullDiameter;
            ResetParts();
            EnsureChip();
            if (chip != null) chip.style.display = DisplayStyle.None;
        }

        public void Layout(float unitsPerPoint)
        {
            if (chip == null) return;
            units = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
            float u = units;
            float top = FlightStaging.CalloutTopInsideHud() * u;
            chip.style.top = top;
            chip.style.left = FlightHudLayout.SideMarginPt * u;
            chip.style.right = FlightHudLayout.SideMarginPt * u;
            FlightDusk.Callout(chip, u, false);
        }

        public void Present()
        {
            if (!armed || flight == null || flight.Sim == null) return;
            if (flight.Lost)
            {
                HideMoving();
                if (chip != null) chip.style.display = DisplayStyle.None;
                var failure = GetComponent<FlightFailureShow>();
                var host = GetComponent<FailureHost>();
                bool pictured = (failure != null && failure.Showing) || (host != null && host.Played);
                if (!pictured) RestoreMesh();
                return;
            }
            double simT = flight.SimTime;
            if (flight.Running || flight.Held) playT = simT;
            else
            {
                if (playT < simT) playT = simT;
                double cap = simT + FlightStaging.TailSec;
                if (playT < cap) playT += Time.unscaledDeltaTime;
                if (playT > cap) playT = cap;
            }
            staging.FairingWaitsForSimEvent = flight.Sim.FairingHeatGated;
            ApplyStaging(flight.Sim.Events, playT, flight.Altitude, flight.Speed, flight.DynPressure, flight.ThrustFraction);
            ApplyBells(playT);
            var read = staging.Plume(playT, flight.ThrustFraction);
            ApplyPlume(read);
            Offer();
            ApplyCallout(playT);
        }

        /// <summary>
        /// The staging picture at sim time <paramref name="t"/> without a FlightDriver: pieces, fairing halves and
        /// the painted parts they replace. Device tests and captures drive this after Arm(null, ...) resets the show.
        /// The caller still owns SetVisible on the parts view (SliceApp.ShowFlightPicture does it every frame).
        /// </summary>
        public void PreviewAt(IList<SimEvent> events, double t, double alt, double speed, double q, double thrust01)
        {
            playT = t;
            ApplyStaging(events, t, alt, speed, q, thrust01);
        }

        void ApplyStaging(IList<SimEvent> events, double t, double alt, double speed, double q, double thrust01)
        {
            staging.Advance(events, t, alt, speed, q, thrust01);
            ApplyMask(Mask(t));
            ApplyPieces(t);
            ApplyFairing(t);
            ApplyParts(t);
        }

        /// <summary>Moving piece for a stage ("stage", index) or one booster ("booster", k); null when none was built.</summary>
        public Transform PieceOf(string role, int index)
        {
            for (int i = 0; i < pieces.Count; i++)
                if (pieces[i].Role == role && pieces[i].Index == index) return pieces[i].Pivot;
            return null;
        }

        /// <summary>FairingL (right = false) or FairingR; null for a rocket without a fairing.</summary>
        public Transform FairingHalf(bool right)
        {
            var h = right ? rightHalf : leftHalf;
            return h != null ? h.Pivot : null;
        }

        /// <summary>Painted parts hidden so far this flight (released bodies and the separated fairing).</summary>
        public StagingPartVisibility Parts => partVisibility;

        void ApplyParts(double t)
        {
            if (partVisibility == null) return;
            if (partVisibility.Update(staging, t)) PushParts();
        }

        void PushParts()
        {
            if (partVisibility == null) return;
            if (partsView != null)
            {
                foreach (var part in partVisibility.Hidden) partsView.SetPartHidden(part, true);
                partsView.SetPayloadShown(partVisibility.PayloadShown);
            }
            if (fairingSeams != null) fairingSeams.enabled = !partVisibility.FairingSeparated;
        }

        /// <summary>Assembled stack again: every painted part may draw, the payload is enclosed, the seams are back.
        /// SliceApp's next SetVisible (ShowFlightPicture) turns the parts back on.</summary>
        void ResetParts()
        {
            partVisibility?.Reset();
            if (partsView != null) partsView.ClearHidden();
            if (fairingSeams != null) fairingSeams.enabled = true;
        }

        void OnDestroy()
        {
            if (chip != null && chip.parent != null) chip.RemoveFromHierarchy();
            ClearGeometry();
        }

        int BoosterCount()
        {
            int n = 0;
            for (int i = 0; i < pieces.Count; i++) if (pieces[i].Role == "booster") n++;
            return n;
        }

        static int SerialOf(string role, int index, List<StagingBody> bodies)
        {
            for (int i = 0; i < bodies.Count; i++)
                if (bodies[i].Role == role && bodies[i].Index == index) return bodies[i].Serial;
            return index;
        }

        Piece MakePiece(string role, int serial, int index, Vector3 offset, float midY, Mesh mesh, UnityEngine.Material mat)
        {
            var pivot = new GameObject(role + "-" + index).transform;
            pivot.gameObject.layer = gameObject.layer;
            pivot.SetParent(root, false);
            var rest = offset + new Vector3(0f, midY, 0f);
            pivot.localPosition = rest;
            var vis = new GameObject("Mesh");
            vis.layer = gameObject.layer;
            vis.transform.SetParent(pivot, false);
            vis.transform.localPosition = new Vector3(0f, -midY, 0f);
            vis.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = vis.AddComponent<MeshRenderer>();
            if (mat != null) mr.sharedMaterial = mat;
            pivot.gameObject.SetActive(false);
            return new Piece { Role = role, Serial = serial, Index = index, Pivot = pivot, Rest = rest };
        }

        Half MakeHalf(Mesh mesh, UnityEngine.Material mat, float mid, float sign)
        {
            var halfMesh = SplitHalf(mesh, sign > 0f);
            owned.Add(halfMesh);
            var pivot = new GameObject(sign < 0f ? "FairingL" : "FairingR").transform;
            pivot.gameObject.layer = gameObject.layer;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(0f, mid, 0f);
            var vis = new GameObject("Mesh");
            vis.layer = gameObject.layer;
            vis.transform.SetParent(pivot, false);
            vis.transform.localPosition = new Vector3(0f, -mid, 0f);
            vis.AddComponent<MeshFilter>().sharedMesh = halfMesh;
            var mr = vis.AddComponent<MeshRenderer>();
            if (mat != null) mr.sharedMaterial = mat;
            pivot.gameObject.SetActive(false);
            return new Half { Pivot = pivot, Mid = mid, Sign = sign };
        }

        int Mask(double t)
        {
            int mask = 0;
            if (staging.FairingAt(t).Separated) mask |= 1;
            if (boosterSerial >= 0 && staging.Released(boosterSerial, t)) mask |= 2;
            if (bySerialStage == null) return mask;
            for (int i = 0; i < bySerialStage.Length; i++)
            {
                var p = bySerialStage[i];
                if (p != null && staging.Released(p.Serial, t)) mask |= 4 << i;
            }
            return mask;
        }

        void ApplyMask(int mask)
        {
            if (bodyFilter == null || original == null || mask == builtMask) return;
            builtMask = mask;
            if (mask == 0)
            {
                bodyFilter.sharedMesh = original;
                return;
            }
            var next = Filter(original, groups, mask);
            bodyFilter.sharedMesh = next;
            if (clipped != null) Kill(clipped);
            clipped = next;
        }

        void ApplyPieces(double t)
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                var p = pieces[i];
                if (p.Pivot == null) continue;
                bool outNow = staging.Released(p.Serial, t);
                if (recoveryOwns && p.Role == "stage" && p.Index == 0 && outNow)
                {
                    if (!p.Shown)
                    {
                        p.Shown = true;
                        p.Pivot.gameObject.SetActive(true);
                    }
                    continue;
                }
                if (!outNow)
                {
                    if (p.Shown)
                    {
                        p.Shown = false;
                        p.Pivot.gameObject.SetActive(false);
                    }
                    continue;
                }
                var pose = staging.Pose(p.Role, p.Index, p.Serial, t);
                if (!p.Shown)
                {
                    p.Shown = true;
                    p.Pivot.gameObject.SetActive(true);
                    Burst(puff, new Vector3(p.Rest.x, p.SepY, p.Rest.z), new Color(1f, 0.55f, 0.16f, 0.9f), 14, 9f, -2.5f);
                    RaiseStageSeparation(p);
                }
                if (DebrisDrives(p.Debris)) continue;   // DebrisFx poses this pivot from the separation impulse
                p.Pivot.localPosition = p.Rest + new Vector3((float)pose.X, (float)pose.Y, (float)pose.Z);
                p.Pivot.localRotation = Quaternion.Euler((float)pose.Pitch, (float)pose.Yaw, (float)pose.Roll);
            }
        }

        void ApplyFairing(double t)
        {
            var pose = staging.FairingAt(t);
            if (pose.Gone)
            {
                if (leftHalf != null) leftHalf.Pivot.gameObject.SetActive(false);
                if (rightHalf != null) rightHalf.Pivot.gameObject.SetActive(false);
                return;
            }
            bool on = pose.Separated && leftHalf != null && rightHalf != null;
            if (!on)
            {
                if (leftHalf != null && leftHalf.Pivot.gameObject.activeSelf) leftHalf.Pivot.gameObject.SetActive(false);
                if (rightHalf != null && rightHalf.Pivot.gameObject.activeSelf) rightHalf.Pivot.gameObject.SetActive(false);
                return;
            }
            if (!fairingRaised) RaiseFairingSeparation();
            if (!DebrisDrives(leftHalf.Debris)) PlaceHalf(leftHalf, pose);
            if (!DebrisDrives(rightHalf.Debris)) PlaceHalf(rightHalf, pose);
        }

        // Physical separation (Kit.CinematicEvents -> Environment/DebrisFx). Mass is an estimate from skin area; the
        // impulse is a 2.5 m/s stage push and a 2 m/s fairing push. Gameplay can replace these with real numbers later.
        static bool DebrisDrives(int handle)
        {
            if (handle == 0) return false;
            var fx = Kestrel.Game.Environment.DebrisFx.Instance;
            return fx != null && fx.IsDriving(handle);
        }

        void RaiseStageSeparation(Piece p)
        {
            if (p.Role != "stage" || p.Pivot == null) return;
            float len = Mathf.Max(1f, 2f * (p.SepY - p.Rest.y));
            float rad = Mathf.Max(0.3f, Diameter(p.Serial) * 0.5f);
            float mass = 25f * 2f * Mathf.PI * rad * len;
            var at = p.Pivot.localPosition;
            CinematicEvents.StageSeparation(at.x, at.y, at.z, 0f, 0f, 0f, p.Index, mass, len, rad, mass * 2.5f, 0.12f * len, 4f, reduce, p.Serial);
            p.Debris = CinematicEvents.LastDebrisHandle;
            var fx = Kestrel.Game.Environment.DebrisFx.Instance;
            if (fx != null && p.Debris != 0) fx.Attach(p.Debris, p.Pivot);
        }

        void RaiseFairingSeparation()
        {
            fairingRaised = true;
            // First frame the halves show: the painted fairing, its seams and rings go, and the payload is revealed,
            // on this same tick (ApplyParts would do it a few lines later in the frame; doing it here keeps the
            // swap atomic even if a caller only ever runs ApplyFairing).
            if (partVisibility != null && partVisibility.Update((System.Func<int, bool>)null, true)) PushParts();
            if (leftHalf == null || rightHalf == null) return;
            float mass = 12f * Mathf.PI * fairingRadius * fairingLen;
            CinematicEvents.FairingSeparation(0f, leftHalf.Mid, 0f, 0f, 0f, 0f, mass, fairingLen, fairingRadius, mass * 2f, 0.5f, 4f, reduce);
            leftHalf.Debris = CinematicEvents.LastDebrisHandle;
            rightHalf.Debris = CinematicEvents.LastDebrisHandleB;
            var fx = Kestrel.Game.Environment.DebrisFx.Instance;
            if (fx == null) return;
            if (leftHalf.Debris != 0) fx.Attach(leftHalf.Debris, leftHalf.Pivot);
            if (rightHalf.Debris != 0) fx.Attach(rightHalf.Debris, rightHalf.Pivot);
        }

        static void PlaceHalf(Half half, FairingPose pose)
        {
            if (half.Pivot == null) return;
            if (!half.Pivot.gameObject.activeSelf) half.Pivot.gameObject.SetActive(true);
            float yaw = half.Sign * (float)pose.YawDeg;
            float outM = half.Sign * (float)pose.OutM;
            float spin = (float)pose.SpinDeg;
            half.Pivot.localPosition = new Vector3(outM, half.Mid - (float)pose.DropM, 0f);
            half.Pivot.localRotation = Quaternion.Euler(spin * 0.65f, yaw, half.Sign * spin * 0.35f);
        }

        void ApplyPlume(PlumeRead read)
        {
            if (plume == null) return;
            int serial = read.Serial;
            if (serial < 0) serial = 0;
            float y = serial < nozzleY.Length ? nozzleY[serial] : 0f;
            var lp = plume.transform.localPosition;
            lp.y = y;
            plume.transform.localPosition = lp;
            plume.ExitDiameter = Diameter(serial);
            ApplyFuel(serial);
            plume.Apply(read.Scale, flight.AmbientPressure, flight.Speed);
            if (read.Heat > 0.001 || read.Flash > 0.001)
                plume.SetStagingTint((float)read.Heat, (float)read.Flash);
            if (read.Flash > 0.7 && serial != flashedSerial)
            {
                flashedSerial = serial;
                Burst(flashPs, new Vector3(0f, y, 0f), new Color(1f, 0.96f, 0.82f, 1f), 18, 14f, -6f);
            }
        }

        static bool CanRecover(Design design)
        {
            if (design == null || !design.legs || design.stages == null || design.stages.Count == 0) return false;
            var first = design.stages[0];
            if (first != null && first.landingPropMass > 1) return true;
            return design.landingReserveSet && design.landingReserveFraction > 0;
        }

        void RememberFuel(Design design, List<StagingBody> bodies)
        {
            if (design == null || bodies == null) return;
            int n = 1;
            for (int i = 0; i < bodies.Count; i++)
                if (bodies[i].Serial + 1 > n) n = bodies[i].Serial + 1;
            fuelBySerial = new string[n];
            for (int i = 0; i < bodies.Count; i++)
            {
                var b = bodies[i];
                if (b.Serial < 0 || b.Serial >= fuelBySerial.Length) continue;
                if (b.Role == "booster")
                    fuelBySerial[b.Serial] = design.strapOns != null ? design.strapOns.propellant : "kerolox";
                else if (b.Index >= 0 && design.stages != null && b.Index < design.stages.Count)
                    fuelBySerial[b.Serial] = design.stages[b.Index].propellant;
            }
        }

        void ApplyFuel(int serial)
        {
            if (plume == null || fuelBySerial == null || serial < 0 || serial >= fuelBySerial.Length) return;
            plume.SetFuel(PropellantFlight.Flame(fuelBySerial[serial]));
        }

        float Diameter(int serial)
        {
            if (hasBoosters && boosterSerial >= 0 && serial <= boosterSerial) return fullDiameter;
            if (serial <= coreSerial) return coreDiameter;
            return upperDiameter;
        }

        void ApplyBells(double t)
        {
            int mask = 0;
            if (boosterSerial >= 0 && staging.Released(boosterSerial, t)) mask |= 1;
            if (coreReleases && staging.Released(coreSerial, t)) mask |= 2;
            if (mask == bellMask) return;
            bellMask = mask;
            var cluster = GetComponent<BellCluster>();
            if (cluster == null) return;
            var keep = new List<BellMount>(mounts.Count);
            for (int i = 0; i < mounts.Count; i++)
            {
                bool strap = mounts[i].Strap;
                if ((mask & 1) != 0 && strap) continue;
                if ((mask & 2) != 0 && !strap && mounts[i].Bell.Stage == 0) continue;
                keep.Add(mounts[i].Bell);
            }
            cluster.Init(keep, KitBudgets.Medium);
            var bay = GetComponent<BayCluster>();
            if (bay != null) bay.Init(keep, KitBudgets.Medium);
        }

        void ApplyCallout(double t)
        {
            EnsureChip();
            if (chip == null) return;
            if (!staging.TryCallout(t, out var call) || string.IsNullOrEmpty(call.Line))
            {
                chip.style.display = DisplayStyle.None;
                return;
            }
            chip.text = call.Line;
            chip.style.display = DisplayStyle.Flex;
            FlightDusk.Callout(chip, units, false);
            chip.BringToFront();
        }

        void Offer()
        {
            var sink = narration ?? silent;
            while (staging.TakeCue(out var cue)) sink.Offer(cue);
        }

        void EnsureChip()
        {
            if (hud == null) return;
            var host = hud.Q("ascent-hud");
            if (host == null) host = hud;
            if (chip != null && chip.parent != host) chip = null;
            if (chip == null) chip = host.Q<Label>("staging-callout");
            if (chip != null) return;
            chip = new Label { name = "staging-callout", pickingMode = PickingMode.Ignore };
            chip.style.position = Position.Absolute;
            chip.style.unityTextAlign = TextAnchor.MiddleCenter;
            chip.style.unityFontStyleAndWeight = FontStyle.Bold;
            chip.style.whiteSpace = WhiteSpace.NoWrap;
            chip.style.overflow = Overflow.Hidden;
            chip.style.color = new Color(1f, 0.69f, 0f);
            chip.style.backgroundColor = new Color(0.03f, 0.055f, 0.09f, 0.82f);
            chip.style.display = DisplayStyle.None;
            host.Add(chip);
            Layout(1f);
        }

        void HideMoving()
        {
            for (int i = 0; i < pieces.Count; i++)
                if (pieces[i].Pivot != null) pieces[i].Pivot.gameObject.SetActive(false);
            if (leftHalf != null) leftHalf.Pivot.gameObject.SetActive(false);
            if (rightHalf != null) rightHalf.Pivot.gameObject.SetActive(false);
        }

        void RestoreMesh()
        {
            if (bodyFilter != null && original != null) bodyFilter.sharedMesh = original;
            builtMask = 0;
        }

        void RememberBells(RocketMeshBuilder.Result built, Design design)
        {
            mounts.Clear();
            if (built.engines == null) return;
            ulong seed = BellVariation.HashId(design.id ?? "kh");
            for (int i = 0; i < built.engines.Count; i++)
            {
                var e = built.engines[i];
                mounts.Add(new KeptMount
                {
                    Strap = e.StrapOn,
                    Bell = new BellMount
                    {
                        EngineId = e.EngineId,
                        Cycle = e.Cycle,
                        ExpansionRatio = e.Expansion,
                        ExitDiameter = e.ExitDiameter,
                        X = e.Exit.x,
                        Y = e.Exit.y,
                        Z = e.Exit.z,
                        Stage = e.Stage,
                        Slot = e.Slot,
                        Seed = seed,
                        SkinRadius = e.StageRadius,
                        Gimbal = e.GimbalDeg > 0.01f,
                        Propellant = e.Propellant(design),   // same fuel cue as SliceApp.AddBells after staging
                    },
                });
            }
            if (built.tanks != null)
                for (int i = 0; i < built.tanks.Count; i++)
                    if (built.tanks[i].Stage == 0 && !built.tanks[i].StrapOn) coreRadius = built.tanks[i].Radius;
        }

        static float CoreFootprint(RocketMeshBuilder.Result built)
        {
            if (built.engines == null) return 1f;
            var exits = new List<Vector3>();
            float d = 0f;
            int n = 0;
            for (int i = 0; i < built.engines.Count; i++)
            {
                var e = built.engines[i];
                if (e.StrapOn || e.Stage != 0) continue;
                exits.Add(e.Exit);
                if (e.ExitDiameter > d) d = e.ExitDiameter;
                n++;
            }
            if (n == 0) return 1f;
            return PlumeCluster.Footprint(n, d, exits);
        }

        int[] Classify(Vector3[] verts, RocketMeshBuilder.Result built, float[] y0, float[] y1, float fairingBase)
        {
            var g = new int[verts.Length];
            float fairR = built.fairing.Present ? built.fairing.Radius * 1.35f + 0.4f : 0f;
            for (int i = 0; i < verts.Length; i++)
            {
                var v = verts[i];
                if (InBooster(v, built)) { g[i] = 2; continue; }
                if (hasFairing && v.y >= fairingBase - 0.02f && v.x * v.x + v.z * v.z <= fairR * fairR) { g[i] = 1; continue; }
                int stage = -1;
                for (int s = 0; s < y0.Length; s++)
                    if (v.y >= y0[s] - 0.02f && v.y < y1[s] + 0.02f) stage = s;
                if (stage >= 0) g[i] = 100 + stage;
            }
            return g;
        }

        static bool InBooster(Vector3 v, RocketMeshBuilder.Result built)
        {
            if (built.tanks == null) return false;
            for (int i = 0; i < built.tanks.Count; i++)
            {
                var t = built.tanks[i];
                if (!t.StrapOn) continue;
                float dx = v.x - t.Offset.x, dz = v.z - t.Offset.z;
                float rad = t.Radius * 1.35f;
                float top = t.BarrelBottom + t.BarrelLength + t.Radius * 2.5f;
                if (dx * dx + dz * dz <= rad * rad && v.y >= -0.3f && v.y <= top) return true;
            }
            return false;
        }

        static Mesh Filter(Mesh src, int[] group, int mask)
        {
            var verts = src.vertices;
            var normals = src.normals;
            var uvs = src.uv;
            var tris = src.triangles;
            var map = new int[verts.Length];
            var nv = new List<Vector3>(verts.Length);
            var nn = new List<Vector3>(verts.Length);
            var nu = new List<Vector2>(verts.Length);
            bool hasN = normals != null && normals.Length == verts.Length;
            bool hasU = uvs != null && uvs.Length == verts.Length;
            for (int i = 0; i < verts.Length; i++)
            {
                if (i < group.Length && Drop(group[i], mask)) { map[i] = -1; continue; }
                map[i] = nv.Count;
                nv.Add(verts[i]);
                if (hasN) nn.Add(normals[i]);
                if (hasU) nu.Add(uvs[i]);
            }
            var nt = new List<int>(tris.Length);
            for (int i = 0; i + 2 < tris.Length; i += 3)
            {
                int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                if (a < 0 || b < 0 || c < 0 || a >= map.Length || b >= map.Length || c >= map.Length) continue;
                if (map[a] < 0 || map[b] < 0 || map[c] < 0) continue;
                nt.Add(map[a]); nt.Add(map[b]); nt.Add(map[c]);
            }
            var mesh = new Mesh { name = "StagingBody", indexFormat = nv.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(nv);
            if (nn.Count == nv.Count) mesh.SetNormals(nn);
            if (nu.Count == nv.Count) mesh.SetUVs(0, nu);
            mesh.SetTriangles(nt, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static bool Drop(int group, int mask)
        {
            if (group == 1) return (mask & 1) != 0;
            if (group == 2) return (mask & 2) != 0;
            if (group >= 100) return (mask & (4 << (group - 100))) != 0;
            return false;
        }

        static Mesh SplitHalf(Mesh src, bool positive)
        {
            var verts = src.vertices;
            var normals = src.normals;
            var uvs = src.uv;
            var tris = src.triangles;
            var keep = new bool[verts.Length];
            var nt = new List<int>();
            for (int i = 0; i + 2 < tris.Length; i += 3)
            {
                int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                float x = (verts[a].x + verts[b].x + verts[c].x) / 3f;
                if (positive ? x < 0f : x >= 0f) continue;
                keep[a] = keep[b] = keep[c] = true;
                nt.Add(a); nt.Add(b); nt.Add(c);
            }
            var map = new int[verts.Length];
            var nv = new List<Vector3>();
            var nn = new List<Vector3>();
            var nu = new List<Vector2>();
            bool hasN = normals != null && normals.Length == verts.Length;
            bool hasU = uvs != null && uvs.Length == verts.Length;
            for (int i = 0; i < verts.Length; i++)
            {
                if (!keep[i]) { map[i] = -1; continue; }
                map[i] = nv.Count;
                nv.Add(verts[i]);
                if (hasN) nn.Add(normals[i]);
                if (hasU) nu.Add(uvs[i]);
            }
            for (int i = 0; i < nt.Count; i++) nt[i] = map[nt[i]];
            var mesh = new Mesh { name = positive ? "FairingR" : "FairingL", indexFormat = nv.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(nv);
            if (nn.Count == nv.Count) mesh.SetNormals(nn);
            if (nu.Count == nv.Count) mesh.SetUVs(0, nu);
            mesh.SetTriangles(nt, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        ParticleSystem MakeBurst(string name, UnityEngine.Material mat, float life, float size)
        {
            var go = new GameObject(name);
            go.layer = gameObject.layer;
            go.transform.SetParent(root != null ? root : transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = life;
            main.startLifetime = life;
            main.startSpeed = 0f;
            main.startSize = size;
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = ps.emission;
            em.enabled = false;
            var sh = ps.shape;
            sh.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            if (mat != null) r.sharedMaterial = mat;
            return ps;
        }

        static void Burst(ParticleSystem ps, Vector3 at, Color color, int count, float speed, float down)
        {
            if (ps == null || count <= 0) return;
            var emit = new ParticleSystem.EmitParams();
            emit.applyShapeToPosition = false;
            emit.position = at;
            emit.startColor = color;
            emit.startSize = ps.main.startSize.constant;
            emit.startLifetime = ps.main.startLifetime.constant;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                emit.velocity = new Vector3(Mathf.Cos(a), down, Mathf.Sin(a)).normalized * speed;
                ps.Emit(emit, 1);
            }
        }

        void ClearGeometry()
        {
            if (clipped != null) Kill(clipped);
            clipped = null;
            builtMask = -1;
            for (int i = 0; i < owned.Count; i++) if (owned[i] != null) Kill(owned[i]);
            owned.Clear();
            pieces.Clear();
            if (root != null) Kill(root.gameObject);
            root = null;
            leftHalf = null;
            rightHalf = null;
            puff = null;
            flashPs = null;
        }

        static void Kill(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }
}
