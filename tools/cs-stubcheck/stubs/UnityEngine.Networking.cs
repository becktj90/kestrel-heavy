// Compile-only stub for UnityEngine.Networking used by the guide clip loader.
using System;
namespace UnityEngine
{
    public enum AudioType { UNKNOWN, ACC, AIFF, IT, MOD, MPEG, OGGVORBIS, S3M, WAV, XM, XMA, VAG, AUDIOQUEUE }
}
namespace UnityEngine.Networking
{
    public class UnityWebRequest : IDisposable
    {
        public enum Result { InProgress, Success, ConnectionError, ProtocolError, DataProcessingError }
        public Result result => Result.Success;
        public int timeout { get; set; }
        public UnityWebRequestAsyncOperation SendWebRequest() => null;
        public void Dispose() { }
    }
    public class UnityWebRequestAsyncOperation : AsyncOperation { }
    public static class UnityWebRequestMultimedia
    {
        public static UnityWebRequest GetAudioClip(string url, AudioType type) => null;
    }
    public class DownloadHandlerAudioClip
    {
        public static AudioClip GetContent(UnityWebRequest request) => null;
    }
}
