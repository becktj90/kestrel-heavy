// Plays the failure the sim already explained. The plume, the pieces, and the
// callout come from FlightFailure, which is a function of that cause and the
// seconds since the failure event. Narration is not spoken from here.
using System.Collections.Generic;
using Kestrel.Core.Flight;
using Kestrel.Core.Physics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class FlightFailureShow : MonoBehaviour
    {
        readonly List<Object> owned = new List<Object>();

        FlightDriver flight;
        PlumeController plume;
        VisualElement hud;
        Label chip;
        FlightFailureCue cue;
        Transform root, flame, fireball, tumble, lower, upper, stageA, stageB;
        Transform[] bits = new Transform[8];
        AudioSource source;
        AudioClip made;
        Material hot;
        float jointY = 6f, bodyR = 1.6f, nozzle = 1.4f, units = 1f;
        double playT, eventT;
        bool latched, sounded, reduce;
        string latchedCause;
        double latchedT = double.NaN;

        public bool Showing { get; private set; }

        public void Prepare(VisualElement flightHud)
        {
            hud = flightHud;
            Drop();
        }

        public void Arm(FlightDriver driver, bool reduceMotion)
        {
            flight = driver;
            reduce = reduceMotion;
            plume = GetComponentInChildren<PlumeController>();
            if (plume != null) plume.ClearFailurePlume();
            latched = false;
            sounded = false;
            Showing = false;
            playT = 0;
            latchedCause = null;
            latchedT = double.NaN;
            if (root != null) root.gameObject.SetActive(false);
            if (chip != null) chip.style.display = DisplayStyle.None;
        }

        public void Layout(float unitsPerPoint)
        {
            units = unitsPerPoint > 0.5f ? unitsPerPoint : 1f;
            if (chip == null) return;
            float u = units;
            float top = FlightStaging.CalloutTopInsideHud() * u;
            chip.style.top = top;
            chip.style.height = 40f * u;
            chip.style.fontSize = FlightHudLayout.LabelFontPt * u;
            chip.style.left = FlightHudLayout.SideMarginPt * u;
            chip.style.right = FlightHudLayout.SideMarginPt * u;
            float r = 8f * u;
            chip.style.borderTopLeftRadius = r;
            chip.style.borderTopRightRadius = r;
            chip.style.borderBottomLeftRadius = r;
            chip.style.borderBottomRightRadius = r;
        }

        public void Present(bool reduceMotion)
        {
            reduce = reduceMotion;
            if (flight == null || flight.Sim == null)
            {
                Quiet();
                return;
            }
            var failure = flight.Result != null ? flight.Result.failure : null;
            if (failure == null)
            {
                Quiet();
                return;
            }
            if (!latched || latchedCause != failure.cause || System.Math.Abs(latchedT - failure.t) > 1e-4)
                Latch(failure);
            double simT = flight.SimTime;
            if (flight.Running || flight.Held) playT = simT;
            else
            {
                if (playT < simT) playT = simT;
                double cap = eventT + FlightFailure.TailSec;
                if (playT < cap) playT += Time.unscaledDeltaTime;
                if (playT > cap) playT = cap;
            }
            double age = playT - eventT;
            if (age < 0 || double.IsNaN(age)) age = 0;
            var picture = FlightFailure.At(cue, age, reduce);
            Showing = picture.Visible;
            ApplyPlume(picture);
            ApplyPicture(picture, age);
            ApplyCallout(picture);
            if (!sounded)
            {
                sounded = true;
                Play(FlightFailure.ToneOf(cue.EffectId));
            }
        }

        void Latch(FailureEvent failure)
        {
            cue = FlightFailure.CueOf(failure);
            eventT = FlightFailure.EventTime(flight.Sim.Events, failure);
            latched = true;
            sounded = false;
            latchedCause = failure.cause;
            latchedT = failure.t;
            playT = flight.SimTime;
            if (playT < eventT) playT = eventT;
        }

        void Quiet()
        {
            Showing = false;
            latched = false;
            if (plume != null) plume.ClearFailurePlume();
            if (root != null) root.gameObject.SetActive(false);
            if (chip != null) chip.style.display = DisplayStyle.None;
        }

        void ApplyPlume(in FailurePicture picture)
        {
            if (plume == null) plume = GetComponentInChildren<PlumeController>();
            if (plume == null) return;
            plume.SetFailurePlume(picture.PlumeScale, picture.SputterAge, picture.Heat);
            plume.Apply(1, flight.AmbientPressure, flight.Speed);
        }

        void ApplyPicture(in FailurePicture picture, double age)
        {
            Ensure();
            root.gameObject.SetActive(true);
            bool throat = picture.EffectId == FlightFailure.SputterCut || picture.EffectId == FlightFailure.DryCutoff;
            flame.gameObject.SetActive(throat && picture.PlumeScale > 0.02f);
            if (flame.gameObject.activeSelf)
            {
                flame.localPosition = new Vector3(0f, -0.35f, 0f);
                float s = Mathf.Max(0.35f, picture.PlumeScale) * nozzle;
                flame.localScale = new Vector3(s, s * 1.6f, s);
                var lit = picture.PlumeScale > 0.4f ? new Color(1f, 0.93f, 0.72f) : new Color(0.32f, 0.2f, 0.1f);
                if (picture.EffectId == FlightFailure.DryCutoff)
                    lit = Color.Lerp(new Color(0.2f, 0.16f, 0.14f), new Color(1f, 0.55f, 0.18f), picture.PlumeScale);
                Paint(flame, lit);
            }

            bool boom = picture.Fireball > 0.05f;
            fireball.gameObject.SetActive(boom);
            if (boom)
            {
                float ang = cue.EngineIndex > 0 ? cue.EngineIndex * 0.9f : 0.4f;
                fireball.localPosition = new Vector3(Mathf.Sin(ang) * 0.6f, 0.2f, Mathf.Cos(ang) * 0.6f);
                fireball.localScale = Vector3.one * Mathf.Max(0.25f, picture.Fireball);
                Paint(fireball, new Color(1f, 0.42f, 0.08f));
            }

            bool shear = picture.EffectId == FlightFailure.Shear;
            lower.gameObject.SetActive(shear);
            upper.gameObject.SetActive(shear);
            if (shear)
            {
                var slab = new Vector3(bodyR * 2.2f, 0.32f, bodyR * 2.2f);
                lower.localPosition = new Vector3(0f, jointY - 0.15f, 0f);
                lower.localRotation = Quaternion.identity;
                lower.localScale = slab;
                upper.localPosition = new Vector3(picture.ShearGap * 0.18f, jointY + picture.ShearGap, 0f);
                upper.localRotation = Quaternion.Euler(0f, 0f, picture.ShearDeg);
                upper.localScale = slab;
                Paint(lower, metalColor);
                Paint(upper, new Color(0.9f, 0.46f, 0.2f));
            }

            bool spin = picture.EffectId == FlightFailure.TumbleSpin;
            tumble.gameObject.SetActive(spin);
            if (spin)
                tumble.localRotation = Quaternion.Euler(picture.TumbleDeg * 0.35f, picture.TumbleDeg * 0.2f, picture.TumbleDeg);

            bool bump = picture.EffectId == FlightFailure.Recontact;
            stageA.gameObject.SetActive(bump);
            stageB.gameObject.SetActive(bump);
            if (bump)
            {
                float h = Mathf.Max(1.6f, bodyR * 1.5f);
                stageA.localPosition = new Vector3(0f, jointY, 0f);
                stageB.localPosition = new Vector3(0.2f, jointY + h + picture.RecontactM, 0f);
                stageA.localScale = new Vector3(bodyR, h * 0.5f, bodyR);
                stageB.localScale = new Vector3(bodyR * 0.82f, h * 0.42f, bodyR * 0.82f);
                Paint(stageA, metalColor);
                Paint(stageB, new Color(0.45f, 0.55f, 0.68f));
            }

            int n = picture.Debris;
            if (n > bits.Length) n = bits.Length;
            for (int i = 0; i < bits.Length; i++)
            {
                bool on = i < n;
                bits[i].gameObject.SetActive(on);
                if (!on) continue;
                var bit = FlightFailure.BitAt(picture.EffectId, i, Mathf.Max(1, n), age, reduce, cue.EngineIndex);
                bits[i].localPosition = new Vector3(bit.X, jointY * 0.25f + bit.Y, bit.Z);
                bits[i].localRotation = Quaternion.Euler(bit.Spin, bit.Spin * 0.45f, i * 18f);
                float s = Mathf.Max(0.25f, bit.Scale) * Mathf.Max(0.8f, bodyR);
                bits[i].localScale = new Vector3(s, s * 0.55f, s * 0.8f);
                Paint(bits[i], new Color(0.95f, 0.4f, 0.12f));
            }
        }

        static readonly Color metalColor = new Color(0.74f, 0.76f, 0.8f);

        void ApplyCallout(in FailurePicture picture)
        {
            EnsureChip();
            if (chip == null) return;
            if (!picture.CalloutOn || string.IsNullOrEmpty(cue.Callout))
            {
                chip.style.display = DisplayStyle.None;
                return;
            }
            chip.text = cue.Callout;
            chip.tooltip = cue.CauseId;
            chip.style.display = DisplayStyle.Flex;
            chip.BringToFront();
        }

        void Play(in FailureTone tone)
        {
            if (Kestrel.Game.Audio.AudioPrefs.SfxMuted) return;
            AudioClip clip = null;
            if (!string.IsNullOrEmpty(tone.Clip))
                clip = Resources.Load<AudioClip>("Sfx/" + tone.Clip);
            if (clip == null)
            {
                int rate = 22050;
                int n = Mathf.Max(1, Mathf.RoundToInt(tone.Seconds * rate));
                var data = new float[n];
                FlightFailure.Write(tone, data, rate);
                if (made != null) Kill(made);
                made = AudioClip.Create("kh-fail-" + tone.SoundId, n, 1, rate, false);
                made.SetData(data, 0);
                clip = made;
            }
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
            }
            source.PlayOneShot(clip, 0.8f);
        }

        void EnsureChip()
        {
            if (hud == null) return;
            var host = hud.Q("ascent-hud");
            if (host == null) host = hud;
            if (chip != null && chip.parent != host) chip = null;
            if (chip == null) chip = host.Q<Label>("failure-callout");
            if (chip != null) return;
            chip = new Label { name = "failure-callout", pickingMode = PickingMode.Ignore };
            chip.style.position = Position.Absolute;
            chip.style.unityTextAlign = TextAnchor.MiddleCenter;
            chip.style.unityFontStyleAndWeight = FontStyle.Bold;
            chip.style.whiteSpace = WhiteSpace.Normal;
            chip.style.color = new Color(1f, 0.82f, 0.72f);
            chip.style.backgroundColor = new Color(0.18f, 0.04f, 0.03f, 0.88f);
            chip.style.display = DisplayStyle.None;
            host.Add(chip);
            Layout(units);
        }

        void Ensure()
        {
            if (root != null) return;
            var filter = GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                var b = filter.sharedMesh.bounds;
                jointY = Mathf.Max(2f, b.center.y);
                bodyR = Mathf.Max(0.8f, b.extents.x);
            }
            if (plume != null) nozzle = Mathf.Max(0.8f, plume.ExitDiameter * 0.85f);
            root = new GameObject("FailureShow").transform;
            root.SetParent(transform, false);
            root.gameObject.layer = gameObject.layer;
            hot = MakeMat(new Color(1f, 0.5f, 0.12f));
            flame = Piece("Flame", PrimitiveType.Sphere, hot);
            fireball = Piece("Fireball", PrimitiveType.Sphere, hot);
            lower = Piece("ShearLower", PrimitiveType.Cube, hot);
            upper = Piece("ShearUpper", PrimitiveType.Cube, hot);
            tumble = new GameObject("Tumble").transform;
            tumble.gameObject.layer = gameObject.layer;
            tumble.SetParent(root, false);
            float arm = bodyR * 1.7f;
            MakeArm(tumble, "ArmX", new Vector3(arm, jointY, 0f));
            MakeArm(tumble, "ArmNX", new Vector3(-arm, jointY, 0f));
            MakeArm(tumble, "ArmZ", new Vector3(0f, jointY, arm));
            MakeArm(tumble, "ArmNZ", new Vector3(0f, jointY, -arm));
            stageA = Piece("StageA", PrimitiveType.Cylinder, hot);
            stageB = Piece("StageB", PrimitiveType.Cylinder, hot);
            for (int i = 0; i < bits.Length; i++)
                bits[i] = Piece("Debris" + i, PrimitiveType.Cube, hot);
            flame.gameObject.SetActive(false);
            fireball.gameObject.SetActive(false);
            lower.gameObject.SetActive(false);
            upper.gameObject.SetActive(false);
            tumble.gameObject.SetActive(false);
            stageA.gameObject.SetActive(false);
            stageB.gameObject.SetActive(false);
            for (int i = 0; i < bits.Length; i++) bits[i].gameObject.SetActive(false);
        }

        void MakeArm(Transform parent, string name, Vector3 at)
        {
            var bit = Piece(name, PrimitiveType.Cube, hot);
            bit.SetParent(parent, false);
            bit.localPosition = at;
            bit.localScale = new Vector3(bodyR * 0.55f, bodyR * 0.28f, bodyR * 0.28f);
            Paint(bit, new Color(0.9f, 0.78f, 0.45f));
        }

        Transform Piece(string name, PrimitiveType type, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.layer = gameObject.layer;
            var col = go.GetComponent<Collider>();
            if (col != null) Kill(col);
            go.transform.SetParent(root, false);
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && mat != null) renderer.sharedMaterial = mat;
            return go.transform;
        }

        void Paint(Transform bit, Color color)
        {
            if (bit == null) return;
            var renderer = bit.GetComponent<Renderer>();
            if (renderer == null) return;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }

        Material MakeMat(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return null;
            var mat = new Material(shader) { color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            owned.Add(mat);
            return mat;
        }

        void Drop()
        {
            if (chip != null && chip.parent != null) chip.RemoveFromHierarchy();
            chip = null;
            if (made != null) Kill(made);
            made = null;
            for (int i = 0; i < owned.Count; i++) if (owned[i] != null) Kill(owned[i]);
            owned.Clear();
            if (root != null) Kill(root.gameObject);
            root = null;
            flame = fireball = tumble = lower = upper = stageA = stageB = null;
            hot = null;
        }

        void OnDestroy()
        {
            if (plume != null) plume.ClearFailurePlume();
            Drop();
        }

        static void Kill(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }
}
