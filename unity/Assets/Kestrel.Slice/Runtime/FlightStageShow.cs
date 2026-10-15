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
        }

        sealed class Half
        {
            public Transform Pivot;
            public float Mid, Sign;
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
        float fullDiameter = 1f, coreDiameter = 1f, upperDiameter = 1f, coreRadius = 1f;
        int boosterSerial = -1, coreSerial, builtMask = -1, bellMask = -1, flashedSerial = -2;
        bool armed, hasFairing, hasBoosters, coreReleases, reduce;
        double playT, jettisonM = 110000;

        public IFlightStagingNarration Narration => narration ?? silent;

        /// <summary>Replace the sink. Null keeps the silent hook. This does not start Thrum.</summary>
        public void SetNarration(IFlightStagingNarration sink) => narration = sink;

        public void Prepare(Catalog catalog, Design design, RocketMeshBuilder.Result built, VisualElement flightHud)
        {
            ClearGeometry();
            hud = flightHud;
            narration = silent;
            if (catalog == null || design == null || built == null || design.stages == null || design.stages.Count == 0) return;
            bodyFilter = GetComponent<MeshFilter>();
            original = bodyFilter != null ? bodyFilter.sharedMesh : null;
            plume = GetComponentInChildren<PlumeController>();
            if (plume != null) fullDiameter = Mathf.Max(0.4f, plume.ExitDiameter);
            hasFairing = design.fairingLength > 0.2 && design.fairingDiameter > 0.1;
            jettisonM = design.fairingJettisonAltitudeKm > 0 ? design.fairingJettisonAltitudeKm * 1000 : 110000;
            hasBoosters = design.StrapOnCount > 0;

            List<SerialStage> serial;
            try { serial = Stack.SerialStages(catalog, design).stages; }
            catch (System.Exception e) { Debug.LogWarning("[Staging] stack skipped: " + e.Message); return; }
            var bodies = FlightStagingPlan.Bodies(serial, design.StrapOnCount);
            nozzleY = new float[Mathf.Max(1, serial.Count)];
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
            playT = 0;
            builtMask = -1;
            bellMask = -1;
            flashedSerial = -2;
            staging.Reset(hasFairing, jettisonM, hasBoosters ? BoosterCount() : 0, reduceMotion);
            RestoreMesh();
            for (int i = 0; i < pieces.Count; i++)
            {
                pieces[i].Shown = false;
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
            EnsureChip();
            if (chip != null) chip.style.display = DisplayStyle.None;
        }

        public void Layout(float unitsPerPoint)
        {
            if (chip == null) return;
            float u = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
            float top = FlightStaging.CalloutTopInsideHud() * u;
            chip.style.top = top;
            chip.style.height = FlightStaging.CalloutPt * u;
            chip.style.fontSize = FlightHudLayout.LabelFontPt * u;
            chip.style.left = FlightHudLayout.SideMarginPt * u;
            chip.style.right = FlightHudLayout.SideMarginPt * u;
            float r = 8f * u;
            chip.style.borderTopLeftRadius = r;
            chip.style.borderTopRightRadius = r;
            chip.style.borderBottomLeftRadius = r;
            chip.style.borderBottomRightRadius = r;
        }

        public void Present()
        {
            if (!armed || flight == null || flight.Sim == null) return;
            if (flight.Lost)
            {
                HideMoving();
                if (chip != null) chip.style.display = DisplayStyle.None;
                var host = GetComponent<FailureHost>();
                if (host == null || !host.Played) RestoreMesh();
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
            staging.Advance(flight.Sim.Events, playT, flight.Altitude, flight.Speed, flight.DynPressure, flight.ThrustFraction);
            int mask = Mask(playT);
            ApplyMask(mask);
            ApplyPieces(playT);
            ApplyFairing(playT);
            ApplyBells(playT);
            var read = staging.Plume(playT, flight.ThrustFraction);
            ApplyPlume(read);
            Offer();
            ApplyCallout(playT);
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

        Piece MakePiece(string role, int serial, int index, Vector3 offset, float midY, Mesh mesh, Material mat)
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

        Half MakeHalf(Mesh mesh, Material mat, float mid, float sign)
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
                }
                p.Pivot.localPosition = p.Rest + new Vector3((float)pose.X, (float)pose.Y, (float)pose.Z);
                p.Pivot.localRotation = Quaternion.Euler((float)pose.Pitch, (float)pose.Yaw, (float)pose.Roll);
            }
        }

        void ApplyFairing(double t)
        {
            var pose = staging.FairingAt(t);
            bool on = pose.Separated && leftHalf != null && rightHalf != null;
            if (!on)
            {
                if (leftHalf != null && leftHalf.Pivot.gameObject.activeSelf) leftHalf.Pivot.gameObject.SetActive(false);
                if (rightHalf != null && rightHalf.Pivot.gameObject.activeSelf) rightHalf.Pivot.gameObject.SetActive(false);
                return;
            }
            PlaceHalf(leftHalf, pose);
            PlaceHalf(rightHalf, pose);
        }

        static void PlaceHalf(Half half, FairingPose pose)
        {
            if (half.Pivot == null) return;
            if (!half.Pivot.gameObject.activeSelf) half.Pivot.gameObject.SetActive(true);
            float yaw = half.Sign * (float)pose.YawDeg;
            float outM = half.Sign * (float)pose.OutM;
            half.Pivot.localPosition = new Vector3(outM, half.Mid - (float)pose.DropM, 0f);
            half.Pivot.localRotation = Quaternion.Euler(0f, yaw, 0f);
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
            plume.Apply(read.Scale, flight.AmbientPressure, flight.Speed);
            if (read.Heat > 0.001 || read.Flash > 0.001)
                plume.SetStagingTint((float)read.Heat, (float)read.Flash);
            if (read.Flash > 0.7 && serial != flashedSerial)
            {
                flashedSerial = serial;
                Burst(flashPs, new Vector3(0f, y, 0f), new Color(1f, 0.96f, 0.82f, 1f), 18, 14f, -6f);
            }
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

        ParticleSystem MakeBurst(string name, Material mat, float life, float size)
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
