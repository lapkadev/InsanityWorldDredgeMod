using System;
using Newtonsoft.Json.Linq;

namespace InsanityWorldMod.Core
{
    internal static class SaveStateMigrator
    {
        private static readonly Func<JObject, JObject>[] MIGRATIONS =
        {
            MigrateV0ToV1,
            MigrateV1ToV2,
        };

        public static SaveState MigrateAndDeserialize(JToken token)
        {
            try
            {
                var version = ReadVersion(token);
                if (version > SaveState.CURRENT_VERSION)
                    Log.Warn($"SaveState v{version} is newer than code v{SaveState.CURRENT_VERSION} - proceeding anyway, new fields may be dropped.");

                token = Migrate(token, version);

                var result = token.ToObject<SaveState>();
                if (result == null)
                {
                    Log.Warn("SaveStateMigrator: deserialized null, using default.");
                    return new SaveState();
                }

                result.Version = SaveState.CURRENT_VERSION;
                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"SaveStateMigrator: failed to deserialize, falling back to default: {ex}");
                return new SaveState();
            }
        }

        private static int ReadVersion(JToken token)
        {
            var current = token["Version"];
            if (current != null)
                return current.Value<int>();

            var legacy = token["SchemaVersion"];
            if (legacy != null)
                return legacy.Value<int>();

            return 0;
        }

        private static JToken Migrate(JToken token, int version)
        {
            if (MIGRATIONS.Length != SaveState.CURRENT_VERSION)
                Log.Error($"SaveStateMigrator: table has {MIGRATIONS.Length} steps but SaveState.CURRENT_VERSION is {SaveState.CURRENT_VERSION}");

            if (!(token is JObject obj))
                return token;

            for (int from = Math.Max(version, 0); from < MIGRATIONS.Length; from++)
            {
                obj = MIGRATIONS[from](obj);
                Log.Info($"SaveStateMigrator: migrated v{from} -> v{from + 1}");
            }

            return obj;
        }

        private static JObject MigrateV0ToV1(JObject obj)
        {
            // No-op: no legacy v0 saves exist yet.
            return obj;
        }

        private static JObject MigrateV1ToV2(JObject obj)
        {
            var legacy = obj["SchemaVersion"];
            if (legacy == null)
                return obj;

            obj["Version"] = legacy;
            obj.Remove("SchemaVersion");
            return obj;
        }
    }
}
