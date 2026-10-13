// Compile-only stand-in. System.Text.Json is not in the netstandard2.1 reference pack stubcheck uses.
// Vehicle catalog tests parse data/engines.json with these members; dotnet test uses the real assembly.
using System;
namespace System.Text.Json
{
    public readonly struct JsonElement
    {
        public JsonElement GetProperty(string name) => default;
        public string GetString() => "";
        public double GetDouble() => 0;
        public JsonElement[] EnumerateArray() => new JsonElement[0];
    }

    public sealed class JsonDocument : IDisposable
    {
        public static JsonDocument Parse(string json) => new JsonDocument();
        public JsonElement RootElement => default;
        public void Dispose() { }
    }
}
