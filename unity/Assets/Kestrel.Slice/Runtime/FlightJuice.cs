// Applies FlightPresenter to the live chase camera, the drawn airframe and a breakup cloud.
// FlightDriver still owns the sim. This only changes how that sim is shown.
using Kestrel.Core.Feel;
using UnityEngine;

namespace Kestrel.Slice
{
    public sealed class FlightJuice : MonoBehaviour
    {
        public const float BaseFov = 50f;

        readonly FlightPresenter presenter = new FlightPresenter(40);
        DebrisView debris;
        Light glow;
        Material body;
        Color baseColor = Color.white;

        public Vector3 Lead { get; private set; }
        public Vector3 Shake { get; private set; }
        public float Fov { get; private set; } = BaseFov;
        public string Call => presenter.Call;
        public string Line => presenter.Line;
        public string Band => presenter.Band;

        public void Arm(Transform world, Material shardSource)
        {
            if (debris == null && world != null) debris = DebrisView.Create(world, shardSource);
        }

        public void BindBody(Material mat)
        {
            body = mat;
            if (mat != null) baseColor = mat.color;
        }

        public void Reset()
        {
            presenter.Reset();
            Lead = Vector3.zero;
            Shake = Vector3.zero;
            Fov = BaseFov;
            debris?.Hide();
            if (glow != null) { glow.intensity = 0f; glow.enabled = false; }
            if (body != null) body.color = baseColor;
        }

        public void Present(FlightDriver flight, float vehicleHeight, double qLimPa, double dvMargin, bool reduced, float dt)
        {
            if (flight == null || flight.Sim == null)
            {
                Reset();
                return;
            }
            var sample = new FlightSample
            {
                Running = flight.Running,
                Lost = flight.Lost,
                HasAttitude = flight.HasAttitude,
                Altitude = flight.Altitude,
                Speed = flight.Speed,
                Mach = flight.Mach,
                DynPressure = flight.DynPressure,
                ThrustFraction = flight.ThrustFraction,
                Downrange = flight.Downrange,
                AttitudeDeg = flight.AttitudeDeg,
                VehicleHeight = vehicleHeight,
                QLimitPa = qLimPa,
                DvMargin = dvMargin,
                Health01 = flight.Lost ? 0 : 1,
                Fuel01 = flight.Meco ? 0.08 : 1,
                Stage = flight.Sim.StageIdx,
            };
            presenter.Step(sample, dt, reduced);
            Lead = new Vector3((float)presenter.LeadX, (float)presenter.LeadY, 0f);
            Shake = new Vector3((float)presenter.ShakeX, (float)presenter.ShakeY, 0f);
            Fov = BaseFov * Mathf.Clamp((float)presenter.FovScale, 0.9f, 1.15f);

            if (flight.HasAttitude && !double.IsNaN(presenter.Pitch) && flight.Vehicle != null)
                flight.Vehicle.localRotation = Quaternion.Euler(0f, 0f, (float)presenter.Pitch - 90f);

            if (presenter.HapticMs > 0)
                HangarNative.Play(presenter.HapticHard ? Haptic.Warning : Haptic.Snap, new[] { presenter.HapticMs });

            debris?.Sync(presenter.Debris, flight.OriginX, flight.OriginY);
            Tint(reduced);
            Glow(flight, vehicleHeight, reduced);
        }

        void Tint(bool reduced)
        {
            if (body == null) return;
            AirframeBand.Tint(presenter.Band, out double r, out double g, out double b);
            var color = baseColor * new Color((float)r, (float)g, (float)b, 1f);
            if (!reduced) color = Color.Lerp(color, Color.white, Mathf.Clamp01((float)presenter.Flash) * 0.7f);
            body.color = color;
        }

        void Glow(FlightDriver flight, float vehicleHeight, bool reduced)
        {
            if (flight.Vehicle == null) return;
            if (glow == null)
            {
                var go = new GameObject("PlumeGlow");
                go.transform.SetParent(flight.Vehicle, false);
                go.transform.localPosition = new Vector3(0f, -1.5f, 0f);
                glow = go.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.shadows = LightShadows.None;
                glow.color = new Color(1f, 0.62f, 0.28f);
                glow.intensity = 0f;
                glow.enabled = false;
            }
            float inten = (float)(flight.ThrustFraction * 3.2 + presenter.Flash * 6.0);
            if (reduced) inten *= 0.35f;
            if (!flight.Running && !flight.Lost) inten = 0f;
            glow.range = Mathf.Max(18f, vehicleHeight * 0.55f);
            glow.intensity = inten;
            glow.enabled = inten > 0.05f;
        }
    }
}
