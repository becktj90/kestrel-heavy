// Applies Kit.CameraFxState (trauma shake, FOV punch) to the game camera, and publishes the slow-mo as a
// PRESENTATION time scale (CinematicEvents.PresentationScale). It never sets Time.timeScale and never touches a physics dt.
//
// How it avoids fighting the chase/design cameras: SliceApp and the design cameras set the camera pose in LateUpdate and
// may read it back next frame, so this does not edit the transform there. It offsets the camera only inside the render
// callbacks (URP beginCameraRendering ... endCameraRendering), then restores it. Nothing else ever sees the offset.
// CaptureCamera.Active (the -khCameraAt override) wins: no effect is applied while it is active.
using Kestrel.Kit;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kestrel.Game.Environment
{
    public sealed class CameraFx : MonoBehaviour
    {
        public static CameraFx Instance { get; private set; }
        /// <summary>Reduce-motion / reduce-flashing at apply time. Tests set it; the game points it at Comfort.ReduceMotion.</summary>
        public static System.Func<bool> ReduceMotion = () => PlayerPrefs.GetInt("kh.a11y.reduceMotion", 0) != 0 || PlayerPrefs.GetInt("kh.a11y.reduceFlashing", 0) != 0;   // same keys as Slice Comfort

        public readonly CameraFxState State = new CameraFxState();
        Camera target;
        Vector3 savedPos; Quaternion savedRot; float savedFov; bool applied;
        System.Action<CameraBeatEvent> onBeat; System.Action<CinematicEvent> onBlast;

        public Camera Target { get => target != null ? target : (target = Camera.main); set => target = value; }
        public bool AppliedThisFrame => applied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("KH_CameraFx") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            go.AddComponent<CameraFx>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; CinematicEvents.PresentationScale = 1.0; }

        void OnEnable() => Subscribe();
        void OnDisable() => Unsubscribe();

        public void Subscribe()
        {
            Instance = this;
            if (onBeat == null) { onBeat = OnBeat; onBlast = OnBlast; }
            Unsubscribe(); Instance = this;
            CinematicEvents.CameraBeatRaised += onBeat; CinematicEvents.Raised += onBlast;
            RenderPipelineManager.beginCameraRendering += BeginRender;
            RenderPipelineManager.endCameraRendering += EndRender;
        }

        public void Unsubscribe()
        {
            if (onBeat != null) { CinematicEvents.CameraBeatRaised -= onBeat; CinematicEvents.Raised -= onBlast; }
            RenderPipelineManager.beginCameraRendering -= BeginRender;
            RenderPipelineManager.endCameraRendering -= EndRender;
            if (Instance == this) Instance = null;
            CinematicEvents.PresentationScale = 1.0;
        }

        void OnBeat(CameraBeatEvent e)
        {
            if (ReduceMotion()) e.Reduce = true;
            State.Trigger(e);
        }

        // Existing explosion cues: a recovery crash has no shake of its own; the in-flight breakup already shakes via FlightPresenter.
        void OnBlast(CinematicEvent e)
        {
            if (e.Kind == CinematicKind.Crash) OnBeat(new CameraBeatEvent { Beat = CameraBeat.Crash, Intensity01 = Mathf.Max(0.5f, e.Energy01), ShakeMul = 1f, Reduce = e.Reduce });
            else if (e.Kind == CinematicKind.Failure) OnBeat(new CameraBeatEvent { Beat = CameraBeat.Crash, Intensity01 = 1f, ShakeMul = 0.35f, Reduce = e.Reduce });
        }

        void Update() => Tick(Time.unscaledDeltaTime);

        public void Tick(float realDt)
        {
            if (ReduceMotion() && (State.Trauma > 0f || State.SlowMoActive)) State.Reset();
            State.Step(realDt);
            CinematicEvents.PresentationScale = State.TimeScale;
        }

        bool ShouldApply(Camera c)
        {
            if (c == null || c != Target || c.cameraType != CameraType.Game) return false;
            if (CaptureCamera.Active) return false;
            if (State.Trauma <= 0f && State.FovDeg <= 0f) return false;
            return !ReduceMotion();
        }

        void BeginRender(ScriptableRenderContext ctx, Camera c) => Apply(c);
        void EndRender(ScriptableRenderContext ctx, Camera c) => Restore(c);

        /// <summary>Offset <paramref name="c"/> for this render if it is the target and nothing overrides it. Paired with Restore.</summary>
        public void Apply(Camera c)
        {
            if (!ShouldApply(c)) return;
            var t = c.transform;
            savedPos = t.position; savedRot = t.rotation; savedFov = c.fieldOfView; applied = true;
            float u = CameraFxState.OffsetMetres(1f, CinematicEvents.ShakeReferenceM);
            t.position = savedPos + t.right * (State.ShakeX * u) + t.up * (State.ShakeY * u);
            t.rotation = savedRot * Quaternion.Euler(0f, 0f, State.RollDeg);
            c.fieldOfView = savedFov + State.FovDeg;
        }

        public void Restore(Camera c)
        {
            if (!applied || c != Target) return;
            var t = c.transform;
            t.SetPositionAndRotation(savedPos, savedRot);
            c.fieldOfView = savedFov;
            applied = false;
        }
    }
}
