// A physical panel. The collider is the click target; the material alpha and emission are the hover.
using System;
using UnityEngine;

namespace Kestrel.Game.Shell
{
    public sealed class MainMenuHotspot : MonoBehaviour
    {
        public string Id;
        public bool Live = true;
        public string Spoken;
        public Material Face;
        public Color RestTint = Color.white;
        public float Alpha = 1f;
        public float Glow;
        public float ShownAlpha = 1f;
        public float ShownGlow;
        public Action Pressed;

        public void Snap(bool hover, bool reduceMotion)
        {
            MainMenuLayout.GlassVisual(Live, hover, out float a, out float g);
            if (!UsesGlass()) { a = 1f; g = hover ? 1.4f : 0.35f; }
            ShownAlpha = MainMenuLayout.Approach(ShownAlpha, a, reduceMotion ? 1f : 0f, reduceMotion);
            ShownGlow = MainMenuLayout.Approach(ShownGlow, g, reduceMotion ? 1f : 0f, reduceMotion);
            Apply();
        }

        public void Tick(bool hover, float dt, bool reduceMotion)
        {
            MainMenuLayout.GlassVisual(Live, hover, out float a, out float g);
            if (!UsesGlass()) { a = 1f; g = hover ? 1.4f : 0.35f; }
            ShownAlpha = MainMenuLayout.Approach(ShownAlpha, a, dt, reduceMotion);
            ShownGlow = MainMenuLayout.Approach(ShownGlow, g, dt, reduceMotion);
            Apply();
        }

        public bool UsesGlass() => Id != "launch";

        public void Apply()
        {
            if (Face == null) return;
            var c = RestTint;
            c.a = UsesGlass() ? ShownAlpha : 1f;
            Face.SetColor("_BaseColor", c);
            if (Face.HasProperty("_Color")) Face.SetColor("_Color", c);
            if (Face.HasProperty("_EmissionColor"))
            {
                var glow = UsesGlass() ? MainMenuMaterials.Cyan * ShownGlow : new Color(0.55f, 0.02f, 0.04f) * ShownGlow;
                Face.SetColor("_EmissionColor", glow);
            }
        }
    }
}
