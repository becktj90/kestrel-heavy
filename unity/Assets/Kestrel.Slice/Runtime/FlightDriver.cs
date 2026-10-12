// Runs the engine-agnostic core sim (Kestrel.Core.Physics.AscentSim) from liftoff to MECO at a
// fixed 1/120 s physics step, decoupled from the render rate, and presents it with a floating origin.
//
// Frames: the sim is 2-D, Earth-centred inertial (double). We convert to a pad-fixed local frame
// (downrange metres east, altitude metres up) in double, then render RELATIVE to a floating origin
// that re-centres whenever the vehicle is > RebaseDistance from it, so float precision near the
// camera stays sub-millimetre at any altitude/downrange.
using System;
using System.Collections.Generic;
using Kestrel.Core.Physics;
using Kestrel.Core.Vehicle;
using UnityEngine;

namespace Kestrel.Slice
{
    public sealed class FlightDriver : MonoBehaviour
    {
        public const double PhysicsDt = 1.0 / 120;
        public const float RebaseDistance = 2000f;

        public Transform World;        // pad, ground, scenery: children are placed in local (double) coords minus origin
        public Transform Vehicle;      // rocket root (pivot at engine exit plane)
        public PlumeController Plume;
        public event Action<FlightDriver> OnMeco;

        public AscentSim Sim { get; private set; }
        public double SimTime => Sim?.T ?? 0;
        public double Downrange { get; private set; }
        public double Altitude { get; private set; }
        public double Speed { get; private set; }
        public double Mach => Sim?.LastMach ?? 0;
        public double DynPressure => Sim?.LastQ ?? 0;
        public double AmbientPressure => Sim?.LastP ?? Atmosphere.P0;
        public double ThrustFraction { get; private set; }
        public bool Meco { get; private set; }
        public bool Running { get; private set; }
        public double OriginX { get; private set; }   // floating origin in pad-local metres (double)
        public double OriginY { get; private set; }
        public int Rebases { get; private set; }

        double clock; int mecoStage; double thrustRef = 1; double prevX, prevY;
        readonly List<(Transform t, double x, double y, double z)> anchored = new List<(Transform, double, double, double)>();
        double omegaAir;

        /// <summary>Register a world object at a fixed pad-local position (double precision).</summary>
        public void Anchor(Transform t, double x, double y, double z) { anchored.Add((t, x, y, z)); Place(t, x, y, z); }

        public void Launch(Design d)
        {
            var c = SliceData.Catalog;
            var (stages, opts) = Stack.SimInputs(c, d, PhysicsDt);
            opts.tMax = 600;
            Sim = new AscentSim(stages, opts);
            // MECO = burnout of the core first stage (serial entry whose role is core with index 0).
            var serial = Stack.SerialStages(c, d).stages;
            mecoStage = serial.FindIndex(s => s.role == "core" && s.index == 0) + 1;
            thrustRef = Math.Max(1, stages[0].thrustVac);
            omegaAir = opts.rotate ? Earth.SiteRotationSpeed(opts.latitude) / Gravity.R_EARTH : 0;
            clock = 0; Meco = false; Running = true; OriginX = OriginY = 0; Rebases = 0; prevX = prevY = 0;
            UpdateFrame();
        }

        void Update()
        {
            if (!Running || Sim == null) return;
            clock += Math.Min(Time.deltaTime, 0.1); // clamp hitches; physics catches up at fixed dt
            int guard = 0;
            while (Sim.T < clock && !Sim.Done && guard++ < 64)
            {
                Sim.Tick();
                if (!Meco && HasBurnout(mecoStage)) { Meco = true; Running = false; break; }
            }
            if (Sim.Done && !Meco) Running = false;
            UpdateFrame();
            if (Meco) OnMeco?.Invoke(this);
        }

        bool HasBurnout(int stage)
        {
            for (int i = Sim.Events.Count - 1; i >= 0 && i >= Sim.Events.Count - 4; i--)
                if (Sim.Events[i].type == "burnout" && Sim.Events[i].stage == stage) return true;
            return false;
        }

        void UpdateFrame()
        {
            double x = Sim.Y[0], y = Sim.Y[1];
            double r = Math.Sqrt(x * x + y * y);
            double padAngle = Math.PI / 2 + omegaAir * Sim.T;     // pad co-rotates with Earth (CCW)
            double ang = Math.Atan2(y, x);
            Downrange = Gravity.R_EARTH * (ang - padAngle);       // east positive
            Altitude = r - Gravity.R_EARTH;
            double vx = Sim.Y[2] + omegaAir * y, vy = Sim.Y[3] - omegaAir * x; // air-relative (= ground-relative)
            Speed = Math.Sqrt(vx * vx + vy * vy);
            ThrustFraction = Sim.Thrusting ? Sim.LastThrust / thrustRef : 0;

            // Floating origin: rebase when the vehicle drifts too far from (OriginX, OriginY).
            if (Math.Abs(Downrange - OriginX) > RebaseDistance || Math.Abs(Altitude - OriginY) > RebaseDistance)
            {
                OriginX = Downrange; OriginY = Altitude; Rebases++;
                foreach (var a in anchored) Place(a.t, a.x, a.y, a.z);
            }
            if (Vehicle != null)
            {
                Vehicle.localPosition = new Vector3((float)(Downrange - OriginX), (float)(Altitude - OriginY), 0);
                double dx = Downrange - prevX, dy = Altitude - prevY;
                if (dx * dx + dy * dy > 1e-6 && Altitude > 20)
                {
                    float pitch = (float)(Math.Atan2(dy, dx) * 180 / Math.PI); // from horizontal
                    Vehicle.localRotation = Quaternion.Euler(0, 0, pitch - 90);
                }
                prevX = Downrange; prevY = Altitude;
            }
            if (Plume != null) Plume.Apply(ThrustFraction, AmbientPressure, Speed);
        }

        void Place(Transform t, double x, double y, double z) => t.localPosition = new Vector3((float)(x - OriginX), (float)(y - OriginY), (float)z);
    }
}
