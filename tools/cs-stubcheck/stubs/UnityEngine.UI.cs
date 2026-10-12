// Stub UIElements + URP for tools/cs-stubcheck (compile-only).
using System;
using System.Collections.Generic;
namespace UnityEngine.UIElements
{
    public enum DisplayStyle { Flex, None }
    public enum Position { Relative, Absolute }
    public enum PickingMode { Position, Ignore }
    public enum FlexDirection { Column, ColumnReverse, Row, RowReverse }
    public enum Align { Auto, FlexStart, Center, FlexEnd, Stretch }
    public enum Justify { FlexStart, Center, FlexEnd, SpaceBetween, SpaceAround }
    public enum Visibility { Visible, Hidden }
    public enum Overflow { Visible, Hidden }
    public enum LengthUnit { Pixel, Percent }
    public enum StyleKeyword { Undefined, Null, Auto, None, Initial }
    public struct Length { public Length(float v) { value = v; unit = default; } public Length(float v, LengthUnit u) { value = v; unit = u; } public float value; public LengthUnit unit; public static Length Percent(float v) => default; public static implicit operator Length(float v) => default; }
    public struct StyleLength { public StyleLength(float v) { value = default; keyword = default; } public StyleLength(Length v) { value = v; keyword = default; } public StyleLength(StyleKeyword k) { value = default; keyword = k; } public Length value; public StyleKeyword keyword; public static implicit operator StyleLength(float v) => default; public static implicit operator StyleLength(Length v) => default; public static implicit operator StyleLength(StyleKeyword v) => default; }
    public struct StyleFloat { public float value; public static implicit operator StyleFloat(float v) => default; public static implicit operator StyleFloat(StyleKeyword v) => default; }
    public struct StyleInt { public int value; public static implicit operator StyleInt(int v) => default; }
    public struct StyleColor { public Color value; public static implicit operator StyleColor(Color v) => default; public static implicit operator StyleColor(StyleKeyword v) => default; }
    public struct StyleEnum<T> where T : struct, IConvertible { public T value; public static implicit operator StyleEnum<T>(T v) => default; public static implicit operator StyleEnum<T>(StyleKeyword v) => default; }
    public struct StyleBackground { public static implicit operator StyleBackground(Texture2D v) => default; }
    public interface IStyle { StyleEnum<DisplayStyle> display { get; set; } StyleEnum<Position> position { get; set; } StyleLength left { get; set; } StyleLength right { get; set; } StyleLength top { get; set; } StyleLength bottom { get; set; } StyleLength width { get; set; } StyleLength height { get; set; } StyleLength minWidth { get; set; } StyleLength minHeight { get; set; } StyleLength maxWidth { get; set; } StyleLength maxHeight { get; set; } StyleFloat flexGrow { get; set; } StyleFloat flexShrink { get; set; } StyleEnum<FlexDirection> flexDirection { get; set; } StyleColor backgroundColor { get; set; } StyleColor color { get; set; } StyleFloat opacity { get; set; } StyleLength paddingLeft { get; set; } StyleLength paddingRight { get; set; } StyleLength paddingTop { get; set; } StyleLength paddingBottom { get; set; } StyleLength marginLeft { get; set; } StyleLength marginRight { get; set; } StyleLength marginTop { get; set; } StyleLength marginBottom { get; set; } StyleEnum<Visibility> visibility { get; set; } StyleEnum<Align> alignItems { get; set; } StyleEnum<Justify> justifyContent { get; set; } StyleLength fontSize { get; set; } StyleLength letterSpacing { get; set; } StyleBackground backgroundImage { get; set; } StyleEnum<Overflow> overflow { get; set; } StyleLength borderTopLeftRadius { get; set; } StyleLength borderTopRightRadius { get; set; } StyleLength borderBottomLeftRadius { get; set; } StyleLength borderBottomRightRadius { get; set; } StyleEnum<TextAnchor> unityTextAlign { get; set; } StyleFloat borderLeftWidth { get; set; } StyleFloat borderRightWidth { get; set; } StyleFloat borderTopWidth { get; set; } StyleFloat borderBottomWidth { get; set; } StyleColor borderLeftColor { get; set; } StyleColor borderRightColor { get; set; } StyleColor borderTopColor { get; set; } StyleColor borderBottomColor { get; set; } StyleEnum<FontStyle> unityFontStyleAndWeight { get; set; } StyleEnum<Wrap> flexWrap { get; set; } StyleEnum<WhiteSpace> whiteSpace { get; set; } StyleLength flexBasis { get; set; } StyleEnum<Align> alignSelf { get; set; } StyleEnum<Align> alignContent { get; set; } }
    public enum Wrap { NoWrap, Wrap, WrapReverse }
    public enum WhiteSpace { Normal, NoWrap }
    public interface IResolvedStyle { float width { get; } float height { get; } float left { get; } float top { get; } float fontSize { get; } DisplayStyle display { get; } }
    public class VisualElementStyleSheetSet { public void Add(StyleSheet s) { } public void Remove(StyleSheet s) { } public int count => 0; }
    public enum TrickleDown { NoTrickleDown, TrickleDown }
    public class EventBase { public void StopPropagation() { } public void StopImmediatePropagation() { } public void PreventDefault() { } }
    public class EventBase<T> : EventBase where T : EventBase<T>, new() { }
    public class GeometryChangedEvent : EventBase<GeometryChangedEvent> { public Rect oldRect => default; public Rect newRect => default; }
    public class AttachToPanelEvent : EventBase<AttachToPanelEvent> { }
    public class DetachFromPanelEvent : EventBase<DetachFromPanelEvent> { }
    public class ClickEvent : EventBase<ClickEvent> { }
    public class PointerDownEvent : EventBase<PointerDownEvent> { public Vector3 position => default; public Vector3 localPosition => default; public int pointerId => 0; }
    public class PointerMoveEvent : EventBase<PointerMoveEvent> { public Vector3 position => default; public Vector3 localPosition => default; public int pointerId => 0; public Vector3 deltaPosition => default; }
    public class PointerUpEvent : EventBase<PointerUpEvent> { public Vector3 position => default; public Vector3 localPosition => default; public int pointerId => 0; }
    public class ChangeEvent<T> : EventBase<ChangeEvent<T>> { public T newValue => default; public T previousValue => default; }
    public delegate void EventCallback<in TEventType>(TEventType evt);
    public interface IPanel { VisualElement visualTree { get; } }
    public class VisualElement { public enum MeasureMode { Undefined, Exactly, AtMost } public VisualElement() { } public string tooltip { get; set; } public string name { get; set; } public IStyle style => null; public IResolvedStyle resolvedStyle => null; public Rect layout => default; public Rect worldBound => default; public Rect contentRect => default; public Vector2 MeasureTextSize(string textToMeasure, float width, MeasureMode widthMode, float height, MeasureMode heightMode) => default; public VisualElement parent => null; public IPanel panel => null; public int childCount => 0; public VisualElement this[int i] => null; public VisualElementStyleSheetSet styleSheets => null; public PickingMode pickingMode { get; set; } public bool visible { get; set; } public bool enabledSelf => true; public object userData { get; set; } public VisualElement contentContainer => this;
        public Action<MeshGenerationContext> generateVisualContent { get; set; }
        public void Add(VisualElement c) { } public void Insert(int i, VisualElement c) { } public void Remove(VisualElement c) { } public void RemoveAt(int i) { } public void Clear() { } public void RemoveFromHierarchy() { } public void BringToFront() { } public void SendToBack() { } public IEnumerable<VisualElement> Children() => null; public T Q<T>(string n = null, string c = null) where T : VisualElement => null; public VisualElement Q(string n = null, string c = null) => null;
        public void AddToClassList(string c) { } public void RemoveFromClassList(string c) { } public void EnableInClassList(string c, bool e) { } public bool ClassListContains(string c) => false; public void MarkDirtyRepaint() { } public void SetEnabled(bool e) { } public void Focus() { }
        public void RegisterCallback<T>(EventCallback<T> cb, TrickleDown t = TrickleDown.NoTrickleDown) where T : EventBase<T>, new() { } public void UnregisterCallback<T>(EventCallback<T> cb) where T : EventBase<T>, new() { } public IVisualElementScheduler schedule => null; }
    public interface IVisualElementScheduler { IVisualElementScheduledItem Execute(Action a); }
    public interface IVisualElementScheduledItem { IVisualElementScheduledItem Every(long ms); IVisualElementScheduledItem StartingIn(long ms); void Pause(); void Resume(); }
    public class TextElement : VisualElement { public string text { get; set; } }
    public class Label : TextElement { public Label() { } public Label(string t) { } }
    public class Button : TextElement { public Button() { } public Button(Action a) { } public event Action clicked; }
    public class Image : VisualElement { public Texture image { get; set; } public Sprite sprite { get; set; } }
    public class ScrollView : VisualElement { }
    public interface INotifyValueChanged<T> { T value { get; set; } void SetValueWithoutNotify(T v); }
    public class BaseField<T> : VisualElement, INotifyValueChanged<T> { public T value { get; set; } public string label { get; set; } public void SetValueWithoutNotify(T v) { } }
    public static class INotifyValueChangedExtensions { public static bool RegisterValueChangedCallback<T>(this INotifyValueChanged<T> c, EventCallback<ChangeEvent<T>> cb) => true; public static bool UnregisterValueChangedCallback<T>(this INotifyValueChanged<T> c, EventCallback<ChangeEvent<T>> cb) => true; }
    public class Toggle : BaseField<bool> { }
    public class Slider : BaseField<float> { public Slider() { } public Slider(float a, float b) { } public float lowValue { get; set; } public float highValue { get; set; } }
    public class SliderInt : BaseField<int> { public SliderInt() { } public int lowValue { get; set; } public int highValue { get; set; } }
    public class DropdownField : BaseField<string> { public System.Collections.Generic.List<string> choices { get; set; } public int index { get; set; } }
    public class TextField : BaseField<string> { }
    public class StyleSheet : ScriptableObject { }
    public class ThemeStyleSheet : StyleSheet { }
    public class PanelSettings : ScriptableObject { public ThemeStyleSheet themeStyleSheet { get; set; } public PanelScaleMode scaleMode { get; set; } public Vector2Int referenceResolution { get; set; } public float match { get; set; } public float scale { get; set; } public int sortingOrder { get; set; } public PanelScreenMatchMode screenMatchMode { get; set; } public RenderTexture targetTexture { get; set; } public float referenceDpi { get; set; } public float fallbackDpi { get; set; } public bool clearColor { get; set; } public Color colorClearValue { get; set; } }
    public enum PanelScaleMode { ConstantPixelSize, ConstantPhysicalSize, ScaleWithScreenSize }
    public enum PanelScreenMatchMode { MatchWidthOrHeight, Shrink, Expand }
    public class VisualTreeAsset : ScriptableObject { public TemplateContainer Instantiate() => null; public void CloneTree(VisualElement t) { } }
    public class TemplateContainer : VisualElement { }
    public class UIDocument : MonoBehaviour { public PanelSettings panelSettings { get; set; } public VisualTreeAsset visualTreeAsset { get; set; } public VisualElement rootVisualElement => null; public float sortingOrder { get; set; } }
    public static class RuntimePanelUtils { public static Vector2 ScreenToPanel(IPanel p, Vector2 s) => s; public static Vector2 CameraTransformWorldToPanel(IPanel p, Vector3 w, Camera c) => default; }
    public enum LineJoin { Miter, Bevel, Round }
    public enum LineCap { Butt, Round }
    public enum FillRule { NonZero, OddEven }
    public enum ArcDirection { Clockwise, CounterClockwise }
    public class Painter2D { public Color fillColor { get; set; } public Color strokeColor { get; set; } public float lineWidth { get; set; } public LineJoin lineJoin { get; set; } public LineCap lineCap { get; set; } public Gradient strokeGradient { get; set; } public void BeginPath() { } public void ClosePath() { } public void MoveTo(Vector2 p) { } public void LineTo(Vector2 p) { } public void BezierCurveTo(Vector2 a, Vector2 b, Vector2 c) { } public void QuadraticCurveTo(Vector2 a, Vector2 b) { } public void Arc(Vector2 c, float r, Angle a, Angle b, ArcDirection d = ArcDirection.Clockwise) { } public void ArcTo(Vector2 a, Vector2 b, float r) { } public void Fill(FillRule r = FillRule.NonZero) { } public void Stroke() { } }
    public struct Angle { public Angle(float v) { } public static Angle Degrees(float v) => default; public static Angle Radians(float v) => default; public static implicit operator Angle(float v) => default; }
    public struct Vertex { public const float nearZ = -1f; public Vector3 position; public Color32 tint; public Vector2 uv; }
    public class MeshWriteData { public void SetNextVertex(Vertex v) { } public void SetNextIndex(ushort i) { } public void SetAllVertices(Vertex[] v) { } public void SetAllIndices(ushort[] i) { } public int vertexCount => 0; }
    public class MeshGenerationContext { public VisualElement visualElement => null; public Painter2D painter2D => null; public MeshWriteData Allocate(int v, int i, Texture t = null) => null; }
}
namespace UnityEngine.Rendering.Universal
{
    public class UniversalRenderPipelineAsset : RenderPipelineAsset { public int msaaSampleCount { get; set; } public float renderScale { get; set; } public bool supportsHDR { get; set; } public bool supportsCameraDepthTexture { get; set; } public bool supportsCameraOpaqueTexture { get; set; } public float shadowDistance { get; set; } public int mainLightShadowmapResolution { get; set; } public int maxAdditionalLightsCount { get; set; } public bool supportsMainLightShadows { get; set; } public int shadowCascadeCount { get; set; } public static UniversalRenderPipelineAsset Create(ScriptableRendererData d = null) => null; }
    public class ScriptableRendererData : ScriptableObject { }
    public class UniversalRendererData : ScriptableRendererData { }
    public class UniversalAdditionalCameraData : MonoBehaviour { public bool renderPostProcessing { get; set; } public AntialiasingMode antialiasing { get; set; } public bool renderShadows { get; set; } public bool requiresDepthTexture { get; set; } public CameraRenderType renderType { get; set; } public List<Camera> cameraStack => null; }
    public enum AntialiasingMode { None, FastApproximateAntialiasing, SubpixelMorphologicalAntiAliasing, TemporalAntiAliasing }
    public enum CameraRenderType { Base, Overlay }
    public class Bloom : VolumeComponent { public MinFloatParameter threshold, intensity; public ClampedFloatParameter scatter; public ColorParameter tint; }
    public class Tonemapping : VolumeComponent { public VolumeParameter<TonemappingMode> mode; }
    public enum TonemappingMode { None, Neutral, ACES }
    public class ColorAdjustments : VolumeComponent { public FloatParameter postExposure, contrast, saturation, hueShift; public ColorParameter colorFilter; }
    public class Vignette : VolumeComponent { public ClampedFloatParameter intensity, smoothness; public ColorParameter color; }
}
