using System;
using Newtonsoft.Json.Linq;

namespace InsanityWorldMod.Core
{
    internal static class SaveStateMigrator
    {
        public static SaveState MigrateAndDeserialize(JToken token)
        {
            try
            {
                var version = token["SchemaVersion"]?.Value<int>() ?? 0;

                if (version < 1) token = MigrateV0ToV1(token);
                // if (version < 2) token = MigrateV1ToV2(token);

                if (version > SaveState.CurrentSchemaVersion)
                    G.Log.Warn($"SaveState schema v{version} is newer than code v{SaveState.CurrentSchemaVersion} - proceeding anyway, new fields may be dropped.");

                var result = token.ToObject<SaveState>();
                if (result == null)
                {
                    G.Log.Warn("SaveStateMigrator: deserialized null, using default.");
                    return new SaveState();
                }
                result.SchemaVersion = SaveState.CurrentSchemaVersion;
                return result;
            }
            catch (Exception ex)
            {
                G.Log.Error($"SaveStateMigrator: failed to deserialize, falling back to default: {ex}");
                return new SaveState();
            }
        }

        private static JToken MigrateV0ToV1(JToken token)
        {
            // No-op: no legacy v0 saves exist yet.
            return token;
        }
    }
}
