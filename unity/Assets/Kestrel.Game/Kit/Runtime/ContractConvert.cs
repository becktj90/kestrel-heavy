// Unity <-> engine-free contract types.
using Kestrel.Kit.Contracts;
using UnityEngine;

namespace Kestrel.Kit
{
    public static class ContractConvert
    {
        public static Vector3 ToUnity(this Vec3 v) => new Vector3(v.X, v.Y, v.Z);
        public static Quaternion ToUnity(this Quat q) => new Quaternion(q.X, q.Y, q.Z, q.W);
        public static Vec3 ToContract(this Vector3 v) => new Vec3(v.x, v.y, v.z);
        public static Quat ToContract(this Quaternion q) => new Quat(q.x, q.y, q.z, q.w);
        public static LocalPose ToContract(this Transform local) => new LocalPose(local.localPosition.ToContract(), local.localRotation.ToContract());
        /// <summary>Attach point pose in world space given the vehicle root transform.</summary>
        public static Matrix4x4 WorldPose(this AttachPoint p, Transform vehicleRoot) => vehicleRoot.localToWorldMatrix * Matrix4x4.TRS(p.Pose.Position.ToUnity(), p.Pose.Rotation.ToUnity(), Vector3.one);
    }
}
