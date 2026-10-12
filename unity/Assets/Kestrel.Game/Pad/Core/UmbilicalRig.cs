// Builds one service link per vehicle attach point: mount (tower anchor or, on a Rail pad, a free lanyard
// post), hose bundle, quick-disconnect plates, and a mast when the release is not already a swing arm.
// Does not read or write tower members.
using System;
using System.Collections.Generic;
using Kestrel.Kit;
using Kestrel.Kit.Contracts;

namespace Kestrel.Pad
{
    public enum ServiceRigid { None, Swing, Mast, Hinge }

    public sealed class ServiceLink
    {
        public string AttachId;
        public UmbilicalType Type;
        public ReleaseStyle Release;
        public string AnchorId;          // null when the pad has no tower anchors
        public Vec3 Anchor;
        public Quat AnchorRotation;
        public Vec3 Plate;
        public Vec3 Outward;
        public float PlateRadius;
        public float MatedYaw = 180f;
        public float RetractYawDeg = 95f;
        public float DropDeg = 72f;
        public float Length;
        public ServiceRigid Rigid;
        public Vec3 MastHead;
        public Vec3 HingeAxis;
        public readonly List<Member> Mast = new List<Member>();
        public HoseStrand[] Strands;
    }

    public static class UmbilicalRig
    {
        public static readonly LocalPose Identity = new LocalPose(new Vec3(0, 0, 0), Quat.Identity);

        /// <summary>Replace <paramref name="layout"/>.Links. Safe when <see cref="PadLayout.Anchors"/> is null.</summary>
        public static void Attach(PadLayout layout, IReadOnlyList<AttachPoint> points, LocalPose vehicleToPad)
        {
            layout.Links.Clear();
            if (points == null) return;
            string parent = layout.Graph != null && layout.Graph.Contains(TowerLayout.TowerId) ? TowerLayout.TowerId : TowerLayout.SiteId;
            foreach (var p in points)
            {
                if (p == null || string.IsNullOrEmpty(p.Id)) continue;
                var link = FromPoint(layout, p, vehicleToPad);
                layout.Links.Add(link);
                if (layout.Graph == null || layout.Graph.Contains($"umbilical.{p.Id}")) continue;
                var e = layout.Graph.Add($"umbilical.{p.Id}", ElementCategory.Connector, parent, "umbilical", $"attach:{p.Id}", $"umbilical:{UmbilicalIds.Short(p.Type)}");
                e.DisplayName = Name(p.Type, link.Rigid);
                e.Center = new Vec3((link.Anchor.X + link.Plate.X) * 0.5f, (link.Anchor.Y + link.Plate.Y) * 0.5f, (link.Anchor.Z + link.Plate.Z) * 0.5f);
                e.Size = new Vec3(Math.Abs(link.Plate.X - link.Anchor.X) + 0.6f, Math.Abs(link.Plate.Y - link.Anchor.Y) + 0.6f, Math.Abs(link.Plate.Z - link.Anchor.Z) + 0.6f);
            }
        }

        /// <summary>Recompute reach, mast and hose ends after Connect moves the plate. Does not touch the element graph.</summary>
        public static void Retarget(ServiceLink link, Vec3 plate, Vec3 outward)
        {
            link.Plate = plate;
            if (ContractMath.Len(outward) > 1e-6f) link.Outward = ContractMath.Norm(outward);
            if (link.AnchorId == null)
                link.Anchor = ContractMath.Add(plate, ContractMath.Scale(link.Outward, Math.Max(1.2f, link.PlateRadius * 4f)));
            PlaceHead(link);
        }

        /// <summary>Vehicle-frame plates into pad space. <paramref name="vehicleToPad"/> is the pad rotation of a vehicle-space vector.</summary>
        public static List<AttachPoint> IntoPadFrame(IReadOnlyList<AttachPoint> points, Quat vehicleToPad)
        {
            var list = new List<AttachPoint>(points?.Count ?? 0);
            if (points == null) return list;
            foreach (var p in points)
            {
                if (p == null) continue;
                list.Add(new AttachPoint
                {
                    Id = p.Id, Type = p.Type, Stage = p.Stage, PlateRadius = p.PlateRadius, Release = p.Release,
                    Pose = new LocalPose(ContractMath.Rotate(vehicleToPad, p.Pose.Position), ContractMath.Mul(vehicleToPad, p.Pose.Rotation)),
                });
            }
            return list;
        }

        public static int Stamp(IReadOnlyList<AttachPoint> points)
        {
            if (points == null) return 0;
            int h = 17;
            foreach (var p in points)
            {
                if (p == null) continue;
                h = h * 31 + (p.Id != null ? p.Id.GetHashCode() : 0);
                h = h * 31 + p.Pose.Position.X.GetHashCode();
                h = h * 31 + p.Pose.Position.Y.GetHashCode();
                h = h * 31 + p.Pose.Position.Z.GetHashCode();
                h = h * 31 + (int)p.Release + 3 * (int)p.Type;
            }
            return h;
        }

        static ServiceLink FromPoint(PadLayout layout, AttachPoint p, LocalPose vehicleToPad)
        {
            var link = new ServiceLink
            {
                AttachId = p.Id, Type = p.Type, Release = p.Release,
                PlateRadius = Math.Max(0.05f, p.PlateRadius),
                Strands = HoseBundle.For(p.Type),
                Plate = PadGenerator.ToPad(vehicleToPad, p.Pose.Position),
            };
            var outward = ContractMath.Rotate(ContractMath.Mul(vehicleToPad.Rotation, p.Pose.Rotation), new Vec3(0, 0, 1));
            link.Outward = ContractMath.Len(outward) > 1e-6f ? ContractMath.Norm(outward) : new Vec3(0, 0, -1);

            var mount = UmbilicalMount.Resolve(layout.Anchors, p.Id, link.Plate.Y);
            if (!mount.Ok)
            {
                link.AnchorId = null;
                link.AnchorRotation = Quat.Identity;
                link.HingeAxis = new Vec3(1, 0, 0);
                link.Rigid = ServiceRigid.None;
                link.Anchor = ContractMath.Add(link.Plate, ContractMath.Scale(link.Outward, Math.Max(1.2f, link.PlateRadius * 4f)));
            }
            else
            {
                link.AnchorId = mount.AnchorId;
                link.Anchor = mount.Position;
                link.AnchorRotation = mount.Rotation;
                link.HingeAxis = ContractMath.Rotate(mount.Rotation, new Vec3(1, 0, 0));
                if (ContractMath.Len(link.HingeAxis) < 1e-6f) link.HingeAxis = new Vec3(1, 0, 0);
                link.Rigid = p.Release switch
                {
                    ReleaseStyle.SwingArmRetract => ServiceRigid.Swing,
                    ReleaseStyle.TailServiceMastDrop => ServiceRigid.Mast,
                    ReleaseStyle.Hinged => ServiceRigid.Hinge,
                    _ => ServiceRigid.None,
                };
            }
            if (layout.Arms != null)
                foreach (var a in layout.Arms)
                    if (a.AttachId == p.Id) { link.RetractYawDeg = a.RetractYawDeg; break; }
            PlaceHead(link);
            return link;
        }

        static void PlaceHead(ServiceLink link)
        {
            link.Mast.Clear();
            float dx = link.Plate.X - link.Anchor.X, dz = link.Plate.Z - link.Anchor.Z;
            float horiz = (float)Math.Sqrt(dx * dx + dz * dz);
            link.MatedYaw = horiz < 1e-4f ? 180f : (float)(Math.Atan2(dx, dz) * 180.0 / Math.PI);
            link.Length = Math.Max(0.3f, horiz - link.PlateRadius * 0.5f);
            if (link.Rigid == ServiceRigid.Swing)
            {
                link.MastHead = ServiceArmMotion.Tip(link.Anchor, link.MatedYaw, link.Length);
                return;
            }
            if (link.Rigid == ServiceRigid.None) { link.MastHead = link.Anchor; return; }

            var to = new Vec3(link.Plate.X - link.Anchor.X, link.Plate.Y - link.Anchor.Y, link.Plate.Z - link.Anchor.Z);
            float len = ContractMath.Len(to);
            float keep = link.Rigid == ServiceRigid.Hinge ? Math.Min(Math.Max(len, 0.4f), 2.2f) : Math.Max(0.4f, len - link.PlateRadius - 0.2f);
            float s = len < 1e-3f ? 0f : keep / len;
            link.MastHead = new Vec3(link.Anchor.X + to.X * s, link.Anchor.Y + to.Y * s, link.Anchor.Z + to.Z * s);
            var axis = link.HingeAxis;
            var along = ContractMath.Norm(to);
            if (Math.Abs(ContractMath.Dot(axis, along)) > 0.95f) axis = new Vec3(0, 1, 0);
            float off = 0.22f;
            Vec3 Side(Vec3 p, float k) => new Vec3(p.X + axis.X * k, p.Y + axis.Y * k, p.Z + axis.Z * k);
            void M(string n, Vec3 a, Vec3 b) => link.Mast.Add(new Member
            {
                Id = $"umb.{link.AttachId}.{n}", Kind = MemberKind.ArmChord, A = a, B = b,
                ParentId = $"umbilical.{link.AttachId}", Material = MaterialRole.SafetyYellowSteel,
            });
            M("l", Side(link.Anchor, off), Side(link.MastHead, off));
            M("r", Side(link.Anchor, -off), Side(link.MastHead, -off));
            M("a", Side(link.Anchor, off), Side(link.MastHead, -off));
            M("b", Side(link.Anchor, -off), Side(link.MastHead, off));
        }

        static string Name(UmbilicalType t, ServiceRigid rigid) => rigid switch
        {
            ServiceRigid.Swing => $"{t} swing arm",
            ServiceRigid.Mast => $"{t} tail mast",
            ServiceRigid.Hinge => $"{t} hinged plate",
            _ => $"{t} umbilical",
        };
    }
}
