// A panel at a device size for EditMode device tests: an editor window sized to the device (or, when the window cannot
// take that size, a UIDocument on a PanelSettings rendering into a RenderTexture of that size), with Pump() to run
// layout. The stage carries the shipping runtime theme (KH_ShellPanelSettings' theme), so a Button or ScrollView
// gets the player's default look and metrics, not the editor's. Shared by IosPortraitHomeDeviceTests and, later,
// StarterDesignerDeviceTests on feat(designer-starter). Pair it with Kestrel.UiChecks.UiTreeChecks.
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.Tests
{
public sealed class UiDeviceSurface : IDisposable
    {
        EditorWindow window;
        GameObject go;
        PanelSettings settings;
        RenderTexture texture;
        public VisualElement Stage { get; private set; }

        public static UiDeviceSurface Open(float w, float h)
        {
            var surface = new UiDeviceSurface();
            surface.TryWindow(w, h);
            surface.Pump(4);
            if (!surface.Sized(w, h))
            {
                surface.CloseWindow();
                surface.TryDocument(w, h);
                surface.Pump(6);
            }
            if (!surface.Sized(w, h))
                Assert.Fail("panel did not take the device size " + w + "x" + h);
            return surface;
        }

        public void Pump(int passes)
        {
            for (int i = 0; i < passes; i++)
            {
                if (Stage != null && Stage.panel != null)
                {
                    var tree = Stage.panel.visualTree;
                    if (tree != null && Stage.resolvedStyle.width > 2f)
                    {
                        tree.style.width = Stage.resolvedStyle.width;
                        tree.style.height = Stage.resolvedStyle.height;
                    }
                }
                if (settings != null)
                {
                    var apply = typeof(PanelSettings).GetMethod("ApplyPanelSettings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (apply != null) apply.Invoke(settings, null);
                }
                var utility = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility");
                var update = utility != null ? utility.GetMethod("UpdatePanels", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null;
                if (update != null && update.GetParameters().Length == 0) update.Invoke(null, null);
                if (window != null) window.Repaint();
                var panel = Stage != null ? Stage.panel : null;
                if (panel != null)
                {
                    var validate = panel.GetType().GetMethod("ValidateLayout", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (validate != null && validate.GetParameters().Length == 0) validate.Invoke(panel, null);
                }
            }
        }

        bool Sized(float w, float h)
        {
            if (Stage == null || Stage.panel == null) return false;
            var bound = Stage.worldBound;
            if (bound.width < 8f || bound.height < 8f) return false;
            float sx = bound.width / w, sy = bound.height / h;
            return sx >= 0.8f && sy >= 0.8f && sx <= 3.5f && sy <= 3.5f && Math.Abs(sx - sy) <= 0.15f;
        }

        void TryWindow(float w, float h)
        {
            try
            {
                window = ScriptableObject.CreateInstance<UiDeviceWindow>();
                window.position = new Rect(0f, 0f, w, h);
                window.minSize = new Vector2(w, h);
                window.maxSize = new Vector2(w, h);
                window.ShowUtility();
                var root = window.rootVisualElement;
                if (root == null) return;
                Stage = MakeStage(root, w, h);
            }
            catch (Exception) { }
        }

        void TryDocument(float w, float h)
        {
            try
            {
                texture = new RenderTexture(Mathf.Max(8, (int)w), Mathf.Max(8, (int)h), 0, RenderTextureFormat.ARGB32);
                texture.Create();
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                var shipping = Resources.Load<PanelSettings>("UI/KH_ShellPanelSettings");
                if (shipping != null) settings.themeStyleSheet = shipping.themeStyleSheet;
                settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                settings.scale = 1f;
                settings.targetTexture = texture;
                go = new GameObject("kh-ui-device");
                go.hideFlags = HideFlags.HideAndDontSave;
                var doc = go.AddComponent<UIDocument>();
                doc.panelSettings = settings;
                var root = doc.rootVisualElement;
                if (root == null) return;
                Stage = MakeStage(root, w, h);
            }
            catch (Exception) { }
        }

        static VisualElement MakeStage(VisualElement panelRoot, float w, float h)
        {
            var tree = panelRoot.panel != null ? panelRoot.panel.visualTree : null;
            if (tree != null)
            {
                tree.style.width = w;
                tree.style.height = h;
            }
            panelRoot.style.width = w;
            panelRoot.style.height = h;
            var stage = new VisualElement { name = "stage", pickingMode = PickingMode.Ignore };
            stage.style.position = Position.Absolute;
            stage.style.left = 0f;
            stage.style.top = 0f;
            stage.style.width = w;
            stage.style.height = h;
            stage.style.overflow = Overflow.Hidden;
            var real = Resources.Load<PanelSettings>("UI/KH_ShellPanelSettings");
            if (real != null && real.themeStyleSheet != null) stage.styleSheets.Add(real.themeStyleSheet);
            panelRoot.Add(stage);
            return stage;
        }

        void CloseWindow()
        {
            if (window == null) return;
            window.Close();
            window = null;
            Stage = null;
        }

        public void Dispose()
        {
            CloseWindow();
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
            if (settings != null) UnityEngine.Object.DestroyImmediate(settings);
            if (texture != null)
            {
                texture.Release();
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }

    sealed class UiDeviceWindow : EditorWindow { }
}
