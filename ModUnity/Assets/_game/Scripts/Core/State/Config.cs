using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace InsanityWorldMod.Core
{
    [Serializable]
    public class InsanityWorldConfig
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;

        public bool IsTransitionPhaseCompleted;

        public bool IsDev;

        public string PlayerName = "Player";
    }

    public enum SessionMode
    {
        Offline,
        OnlineHost,
        OnlineJoin
    }

    [Serializable]
    public class LastGameSession
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;

        public string WorldId;

        [JsonConverter(typeof(StringEnumConverter))]
        public SessionMode Mode;
    }
}
