// Pooled shards for a FlightPresenter debris cloud. The pool is built on the first breakup,
// so a hangar frame does not pay for meshes that are not in the air. Positions are pad metres
// minus the floating origin, the same space the vehicle is drawn in.
using Kestrel.Core.Feel;
using UnityEngine;

namespace Kestrel.Slice
{
    public sealed class DebrisView : MonoBehaviour
    {
        Transform[] nodes;
        Material mat;

        public static DebrisView Create(Transform parent, Material source)
        {
            var go = new GameObject("Debris");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<DebrisView>();
            if (source != null)
            {
                view.mat = new Material(source) { color = new Color(0.95f, 0.46f, 0.18f) };
                if (view.mat.HasProperty("_EmissionColor"))
                {
                    view.mat.EnableKeyword("_EMISSION");
                    view.mat.SetColor("_EmissionColor", new Color(1.4f, 0.35f, 0.08f));
                }
            }
            return view;
        }

        public void Sync(DebrisField field, double originX, double originY)
        {
            if (field == null || field.Live <= 0)
            {
                Hide();
                return;
            }
            Ensure(field.Shards.Length);
            for (int i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                if (i >= field.Shards.Length || !field.Shards[i].Alive)
                {
                    if (node.gameObject.activeSelf) node.gameObject.SetActive(false);
                    continue;
                }
                var s = field.Shards[i];
                if (!node.gameObject.activeSelf) node.gameObject.SetActive(true);
                float life = Mathf.Clamp01((float)(s.Life / 1.2));
                float diam = (float)s.Radius * 2f * Mathf.Lerp(0.35f, 1f, life);
                node.localPosition = new Vector3((float)(s.X - originX), (float)(s.Y - originY), (i - nodes.Length * 0.5f) * 0.2f);
                node.localRotation = Quaternion.Euler(0f, 0f, (float)(s.Angle * Mathf.Rad2Deg));
                node.localScale = new Vector3(diam, diam, diam);
            }
        }

        public void Hide()
        {
            if (nodes == null) return;
            for (int i = 0; i < nodes.Length; i++)
                if (nodes[i] != null && nodes[i].gameObject.activeSelf) nodes[i].gameObject.SetActive(false);
        }

        void Ensure(int count)
        {
            if (nodes != null) return;
            nodes = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Shard" + i;
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
                go.transform.SetParent(transform, false);
                go.SetActive(false);
                nodes[i] = go.transform;
            }
        }
    }
}
