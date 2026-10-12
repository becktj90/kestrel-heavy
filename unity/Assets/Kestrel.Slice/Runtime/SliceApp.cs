// Vertical-slice entry point: DESIGN stub (UI Toolkit) -> flight liftoff..MECO -> HUD -> Game Center.
// Everything is created from code at runtime; the scene only needs one GameObject with this
// component (the editor bootstrap generates Assets/Scenes/Slice.unity).
using System.Linq;
using Kestrel.Core.Economy;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Slice
{
    public sealed class SliceApp : MonoBehaviour
    {
        public PanelSettings PanelSettings;   // assigned by the editor bootstrap (Resources/UI/KH_PanelSettings)
        Design design; Camera cam; Transform worldRoot, vehicleRoot, tower; FlightDriver flight; GameObject rocketGO;
        VisualElement ui, designPanel, hud; Label rDv, rTwr, rCost, rRel, rMass, hT, hStage, hAlt, hVel, hMach, hQ, hThr, hDr, hMsg; Button again;
        Material bodyMat, plumeMat; bool submitted;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            bodyMat = Resources.Load<Material>("Materials/KH_Body");
            plumeMat = Resources.Load<Material>("Materials/KH_Plume");
            BuildScene();
            BuildUI();
            _ = GameCenterBridge.Impl.Authenticate();
            SelectPreset("stock");
        }

        void BuildScene()
        {
            cam = new GameObject("Camera").AddComponent<Camera>(); cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.45f, 0.62f, 0.85f);
            cam.nearClipPlane = 0.5f; cam.farClipPlane = 200000f; cam.fieldOfView = 50;
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.3f; sun.transform.rotation = Quaternion.Euler(40, -30, 0);
            worldRoot = new GameObject("World").transform;
            vehicleRoot = new GameObject("Vehicle").transform; vehicleRoot.SetParent(worldRoot, false);
            flight = gameObject.AddComponent<FlightDriver>(); flight.World = worldRoot; flight.Vehicle = vehicleRoot;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.name = "Ground"; ground.transform.SetParent(worldRoot, false);
            ground.transform.localScale = new Vector3(400, 1, 400); // 4 km square
            if (bodyMat != null) { var gm = new Material(bodyMat) { color = new Color(0.30f, 0.36f, 0.28f) }; ground.GetComponent<Renderer>().sharedMaterial = gm; }
            flight.Anchor(ground.transform, 0, 0, 0);
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder); pad.name = "Pad"; pad.transform.SetParent(worldRoot, false);
            pad.transform.localScale = new Vector3(30, 0.5f, 30);
            flight.Anchor(pad.transform, 0, -0.5, 0);
            tower = PadTower.Create(bodyMat); tower.SetParent(worldRoot, false);
            flight.Anchor(tower, 0, 0, 0); // sized per design in BuildRocket
            flight.OnMeco += OnMeco;
        }

        void BuildUI()
        {
            var doc = gameObject.AddComponent<UIDocument>();
            doc.panelSettings = PanelSettings != null ? PanelSettings : Resources.Load<PanelSettings>("UI/KH_PanelSettings");
            doc.visualTreeAsset = Resources.Load<VisualTreeAsset>("UI/Slice");
            ui = doc.rootVisualElement;
            designPanel = ui.Q("design"); hud = ui.Q("hud");
            ui.RegisterCallback<GeometryChangedEvent>(_ => ApplyHudSafeArea());
            rDv = ui.Q<Label>("r-dv"); rTwr = ui.Q<Label>("r-twr"); rCost = ui.Q<Label>("r-cost"); rRel = ui.Q<Label>("r-rel"); rMass = ui.Q<Label>("r-mass");
            hT = ui.Q<Label>("h-t"); hStage = ui.Q<Label>("h-stage"); hAlt = ui.Q<Label>("h-alt"); hVel = ui.Q<Label>("h-vel");
            hMach = ui.Q<Label>("h-mach"); hQ = ui.Q<Label>("h-q"); hThr = ui.Q<Label>("h-thr"); hDr = ui.Q<Label>("h-dr"); hMsg = ui.Q<Label>("h-msg");
            again = ui.Q<Button>("again");

            var presets = SliceData.Catalog.Presets;
            var dd = ui.Q<DropdownField>("preset");
            dd.choices = presets.Select(p => p.name).ToList();
            dd.RegisterValueChangedCallback(e => SelectPreset(presets.First(p => p.name == e.newValue).id));
            ui.Q<SliderInt>("engines").RegisterValueChangedCallback(e => { design.stages[0].engineCount = e.newValue; Refresh(); });
            ui.Q<Slider>("prop").RegisterValueChangedCallback(e => { design.stages[0].propMass = Mathf.Round(e.newValue) * 1000.0; Refresh(); });
            ui.Q<Button>("launch").clicked += Launch;
            again.clicked += () => { flight.enabled = false; hud.AddToClassList("hidden"); designPanel.RemoveFromClassList("hidden"); again.AddToClassList("hidden"); SelectPreset(design.id); };
            // Accessibility labels (VoiceOver wiring via UnityEngine.Accessibility is a listed gap; see RESULTS.md)
            ui.Q<Button>("launch").tooltip = "Roll out and launch the current design";
        }

        /// <summary>Pad the flight HUD to Screen.safeArea (+8 pt sides) so the top band clears the notch /
        /// Dynamic Island and stays above the vehicle (see HudLayout).</summary>
        void ApplyHudSafeArea()
        {
            var size = ui.layout.size; if (size.x <= 0 || size.y <= 0) return;
            float dpr = Screen.dpi > 0 ? Mathf.Max(1f, Mathf.Round(Screen.dpi / 160f)) : 3f;
            var p = HudLayout.SafePadding(new Vector2(Screen.width, Screen.height), Screen.safeArea, size, dpr);
            hud.style.paddingLeft = p.x; hud.style.paddingTop = p.y; hud.style.paddingRight = p.z; hud.style.paddingBottom = p.w;
        }

        void SelectPreset(string id)
        {
            design = SliceData.Catalog.Preset(id).Clone();
            ui.Q<DropdownField>("preset").SetValueWithoutNotify(design.name);
            ui.Q<SliderInt>("engines").SetValueWithoutNotify(design.stages[0].engineCount);
            ui.Q<Slider>("prop").SetValueWithoutNotify((float)(design.stages[0].propMass / 1000));
            Refresh();
        }

        /// <summary>Live readout from the ported core (same numbers as the web designer).</summary>
        void Refresh()
        {
            var c = SliceData.Catalog;
            var (stages, payload) = Stack.SerialStages(c, design);
            var dv = Headless.StackDeltaV(stages.ConvertAll(s => s.ToSim()), payload);
            double m0 = payload; foreach (var s in stages) m0 += s.propMass + s.dryMass;
            double twr = stages[0].thrustSL / (m0 * Atmosphere.G0);
            var req = Stack.RequiredDeltaV(c, design.payloadOrbit);
            var price = CostModel.PriceDesign(c, design);
            var rel = Reliability.Of(c, design);
            rDv.text = $"Δv {dv.total:N0} m/s  (need {req.required:N0})";
            rTwr.text = $"Liftoff TWR {twr:F2}";
            rCost.text = $"Per launch {CostModel.FormatKc(c, price.recurring)}";
            rRel.text = $"Reliability {rel.reliability * 100:F1}%";
            rMass.text = $"Liftoff {m0 / 1000:N0} t";
            rTwr.style.color = twr < 1.15 ? new Color(1f, 0.4f, 0.4f) : new Color(0.78f, 0.86f, 1f);
            BuildRocket();
        }

        void BuildRocket()
        {
            if (rocketGO != null) Destroy(rocketGO);
            var r = RocketMeshBuilder.Build(SliceData.Catalog, design);
            rocketGO = new GameObject("Rocket"); rocketGO.transform.SetParent(vehicleRoot, false);
            rocketGO.AddComponent<MeshFilter>().sharedMesh = r.mesh;
            var mr = rocketGO.AddComponent<MeshRenderer>();
            if (bodyMat != null) { var m = new Material(bodyMat); ColorUtility.TryParseHtmlString(design.liveryPrimary, out var col); m.color = col; mr.sharedMaterial = m; }
            flight.Plume = PlumeController.Create(rocketGO.transform, Vector3.zero, Mathf.Max(1f, Mathf.Sqrt(design.stages[0].engineCount) * r.exitDiameter), plumeMat);
            flight.Plume.Apply(0, Atmosphere.P0, 0);
            vehicleRoot.localPosition = Vector3.zero; vehicleRoot.localRotation = Quaternion.identity;
            PadTower.Apply(tower, r.height, r.mesh.bounds.extents.x);
            FrameCamera(r.height);
        }

        float rocketHeight = 70; Vector3 camOffset;
        void FrameCamera(float h) { rocketHeight = h; cam.transform.position = new Vector3(h * 0.9f, h * 0.45f, -h * 1.4f); cam.transform.LookAt(new Vector3(0, h * 0.45f, 0)); camOffset = cam.transform.position - (vehicleRoot != null ? vehicleRoot.position : Vector3.zero); }

        void Launch()
        {
            submitted = false;
            designPanel.AddToClassList("hidden"); hud.RemoveFromClassList("hidden"); hMsg.text = "";
            flight.enabled = true;
            flight.Launch(design);
        }

        void LateUpdate()
        {
            if (flight == null || flight.Sim == null || hud.ClassListContains("hidden")) return;
            // Chase camera, in floating-origin space (vehicle stays near the origin).
            // Smooth the offset from the vehicle, not the absolute position: the vehicle teleports on every
            // floating-origin rebase and moves ~800 m/s at max-Q (see ChaseCamera).
            var v = vehicleRoot.position; var up = vehicleRoot.up;
            camOffset = ChaseCamera.StepOffset(camOffset, ChaseCamera.DesiredOffset(up, rocketHeight), 4f, Time.deltaTime);
            cam.transform.position = v + camOffset;
            cam.transform.LookAt(v + up * rocketHeight * 0.5f);
            hT.text = $"T+ {flight.SimTime:F1}";
            hStage.text = $"S{flight.Sim.StageIdx + 1}";
            hAlt.text = $"ALT {flight.Altitude / 1000:F1} km";
            hVel.text = $"VEL {flight.Speed:N0} m/s";
            hMach.text = $"MACH {flight.Mach:F2}";
            hQ.text = $"Q {flight.DynPressure / 1000:F1} kPa";
            hThr.text = $"THR {flight.ThrustFraction * 100:F0}%";
            hDr.text = $"DR {flight.Downrange / 1000:F1} km";
            if (!flight.Running && !flight.Meco && flight.Sim.Done) { hMsg.text = "Flight ended before MECO"; again.RemoveFromClassList("hidden"); }
        }

        async void OnMeco(FlightDriver f)
        {
            if (submitted) return; submitted = true;
            hMsg.text = $"MECO  ·  {f.Altitude / 1000:F1} km  ·  {f.Speed:N0} m/s";
            again.RemoveFromClassList("hidden");
            bool ok = await GameCenterBridge.Impl.SubmitScore(GameCenterBridge.MecoAltitudeBoard, (long)System.Math.Round(f.Altitude));
            hMsg.text += ok ? "\nScore sent to Game Center" : "";
        }
    }
}
