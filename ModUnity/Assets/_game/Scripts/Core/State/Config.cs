using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace InsanityWorldMod.Core
{
    [Serializable]
    public class InsanityWorldConfig
    {
        public const int CURRENT_VERSION = 1;

        public int Version = CURRENT_VERSION;

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
        public const int CURRENT_VERSION = 1;

        public int Version = CURRENT_VERSION;

        public string WorldId;

        [JsonConverter(typeof(StringEnumConverter))]
        public SessionMode Mode;
    }
}
