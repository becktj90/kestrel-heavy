// Where a D4 umbilical or service arm bolts to the tower. Reads TowerAnchors only — never tower width,
// standoff, or level height. Graphics Dev owns the tower mesh (D2); this stays valid when that changes.
using Kestrel.Kit.Contracts;

namespace Kestrel.Pad
{
    public struct MountFrame
    {
        public bool Ok;
        public string AnchorId;
        public Vec3 Position;
        public Quat Rotation;
    }

    public static class UmbilicalMount
    {
        /// <summary>
        /// Prefer <c>arm.{attachId}</c>. Otherwise the vehicle-face anchor nearest <paramref name="plateY"/>
        /// (<see cref="TowerAnchors.OnFaceNear"/>). A null anchor set (Rail pads) returns Ok = false and does not throw.
        /// </summary>
        public static MountFrame Resolve(TowerAnchors anchors, string attachId, float plateY)
        {
            if (anchors == null || string.IsNullOrEmpty(attachId)) return default;
            if (anchors.TryGet(TowerAnchors.ArmId(attachId), out var arm))
                return new MountFrame { Ok = true, AnchorId = arm.Id, Position = arm.Position, Rotation = arm.Rotation };
            var face = anchors.OnFaceNear(plateY);
            return new MountFrame { Ok = true, AnchorId = face.Id, Position = face.Position, Rotation = face.Rotation };
        }
    }
}
