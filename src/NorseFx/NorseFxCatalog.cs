using System;
using System.Collections.Generic;

namespace ValalaMods.NorseFx
{
    /// <summary>
    /// One opt-in effect extracted from the NorseDemigods bundle. Entries are resolved from an
    /// explicit allow-list, so only the selected bundle assets are ever loaded.
    /// </summary>
    internal sealed class NorseFxEntry
    {
        internal string Id { get; }
        internal string PrefabPath { get; }
        internal string SfxPath { get; }
        internal float Lifetime { get; }
        internal bool Loop { get; }
        internal float Volume { get; }
        internal float MinDistance { get; }
        internal float MaxDistance { get; }
        internal float SfxStartTime { get; }

        internal NorseFxEntry(
            string id,
            string prefabPath = null,
            string sfxPath = null,
            float lifetime = 5f,
            bool loop = false,
            float volume = 1f,
            float minDistance = 10f,
            float maxDistance = 50f,
            float sfxStartTime = 0f)
        {
            Id = id;
            PrefabPath = prefabPath;
            SfxPath = sfxPath;
            Lifetime = lifetime;
            Loop = loop;
            Volume = volume;
            MinDistance = minDistance;
            MaxDistance = maxDistance;
            SfxStartTime = sfxStartTime;
        }

        internal bool HasVfx => !string.IsNullOrEmpty(PrefabPath);
        internal bool HasSfx => !string.IsNullOrEmpty(SfxPath);
    }

    /// <summary>
    /// Explicit allow-list of the effects valala-mods uses as bundle fallbacks. Only the entries
    /// below are loaded from the local bundle; an empty list keeps the module inert.
    /// </summary>
    internal static class NorseFxCatalog
    {
        private static readonly NorseFxEntry[] Selected =
        {
            // Fallback only: Thor landing uses the vanilla sledge impact prefabs.
            new NorseFxEntry(
                "norse.landing.lightning_boom",
                "assets/customassets/prefabs/vfx_lightning_explosion_1.prefab",
                "assets/customassets/sounds/sfx_thunder_impact.wav",
                lifetime: 3f,
                volume: 1f,
                minDistance: 10f,
                maxDistance: 50f,
                sfxStartTime: 0.5f),
        };

        private static readonly Dictionary<string, NorseFxEntry> ById = Build();

        private static Dictionary<string, NorseFxEntry> Build()
        {
            var map = new Dictionary<string, NorseFxEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (NorseFxEntry entry in Selected)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Id))
                    continue;

                map[entry.Id] = entry;
            }

            return map;
        }

        internal static int Count => ById.Count;

        internal static bool TryGet(string id, out NorseFxEntry entry)
        {
            return ById.TryGetValue(id ?? string.Empty, out entry);
        }
    }
}
