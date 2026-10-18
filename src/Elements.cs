using System;
using ValalaMods.NorseFx;
using UnityEngine;

namespace ValalaMods
{
    /// <summary>Elemental class of the VFX, selected by the player and synced over the network.</summary>
    internal enum Element
    {
        Lightning = 0,
        Rock = 1,
        Fire = 2,
        Poison = 3,
        Ice = 4
    }

    /// <summary>Native Valheim damage type used by an element.</summary>
    internal enum ElementDamage
    {
        Lightning,
        Blunt,
        Fire,
        Poison,
        Frost
    }

    /// <summary>
    /// Static definition of an element class: display name, tint, availability and the vanilla
    /// VFX prefab candidates used for the strike (resolved at runtime by name, first match wins).
    /// </summary>
    internal sealed class ElementDef
    {
        internal Element Id { get; }
        internal string Name { get; }
        internal string ColorHex { get; }
        internal bool Implemented { get; }
        internal ElementDamage Damage { get; }
        internal float StaggerMultiplier { get; }
        internal string[] BoltPrefabs { get; }
        internal string[] AoePrefabs { get; }
        internal string[] LandingAoePrefabs { get; }
        internal string[] SparkPrefabs { get; }
        internal string NorseEffectId { get; }
        internal string NorseSfxId { get; }

        internal ElementDef(
            Element id, string name, string colorHex, bool implemented,
            ElementDamage damage, float staggerMultiplier,
            string[] boltPrefabs, string[] aoePrefabs, string[] sparkPrefabs,
            string[] landingAoePrefabs = null,
            string norseEffectId = null, string norseSfxId = null)
        {
            Id = id;
            Name = name;
            ColorHex = colorHex;
            Implemented = implemented;
            Damage = damage;
            StaggerMultiplier = staggerMultiplier;
            BoltPrefabs = boltPrefabs;
            AoePrefabs = aoePrefabs;
            LandingAoePrefabs = landingAoePrefabs ?? aoePrefabs;
            SparkPrefabs = sparkPrefabs;
            NorseEffectId = norseEffectId;
            NorseSfxId = norseSfxId;
        }

        internal Color Tint => ValalaModsPlugin.ParseColor(ColorHex);
    }

    /// <summary>Registry of the available element classes and the current selection.</summary>
    internal static class ElementRegistry
    {
        internal static readonly ElementDef[] All =
        {
            new ElementDef(
                Element.Lightning, "Foudre", "59BFFF", true,
                ElementDamage.Lightning, 1f,
                new[] { "fx_Lightning" },
                new[] { "lightningAOE" },
                new[] { "fx_chainlightning_hit" }),

            new ElementDef(
                Element.Rock, "Roche", "B08A5A", true,
                ElementDamage.Blunt, 2f,
                new[] { "vfx_RockHit", "vfx_stonegolem_attack_hit" },
                new[] { "vfx_RockDestroyed_large", "vfx_RockDestroyed", "vfx_RockHit" },
                new[] { "vfx_RockHit", "vfx_stonegolem_attack_hit" }),

            new ElementDef(
                Element.Fire, "Feu", "FF7A3C", true,
                ElementDamage.Fire, 1f,
                new[] { "fireball_explosion", "fx_fireball_explosion", "fx_groundfire", "fx_burning" },
                new[] { "fx_groundfire" },
                new[] { "fx_sword_fire_hit", "fx_burning" },
                new[] { "fireball_explosion", "fx_fireball_explosion", "fx_groundfire" }),

            new ElementDef(
                Element.Poison, "Poison", "8BD450", true,
                ElementDamage.Poison, 1f,
                new[] { "vfx_BombBlob_explode_poison", "vfx_BombBlob_explode_poisonelite", "vfx_Poison" },
                new[] { "vfx_BombBlob_explode_poisonelite", "vfx_BombBlob_explode_poison", "vfx_Poison" },
                new[] { "vfx_poisonarrow_hit", "vfx_Poison" }),

            new ElementDef(
                Element.Ice, "Glace", "9FE8FF", true,
                ElementDamage.Frost, 1f,
                new[] { "vfx_BombBlob_explode_frost", "FenringIceNova_aoe", "vfx_FrostOrbs" },
                new[] { "FenringIceNova_aoe", "vfx_BombBlob_explode_frost", "vfx_Frost" },
                new[] { "fx_iceshard_hit", "vfx_frostarrow_hit", "vfx_blob_frost_hit" },
                new[] { "vfx_BombBlob_explode_frost", "vfx_Frost", "FenringIceNova_aoe" })
        };

        /// <summary>Currently selected class (defaults to Lightning).</summary>
        internal static Element Current { get; set; } = Element.Lightning;

        internal static ElementDef Get(Element id)
        {
            int index = (int)id;
            return index >= 0 && index < All.Length ? All[index] : All[(int)Element.Lightning];
        }

        /// <summary>Resolves a class from its display name or enum name; falls back to Lightning.</summary>
        internal static Element Parse(string name)
        {
            if (string.IsNullOrEmpty(name))
                return Element.Lightning;

            // Migration from the retired Wind class.
            if (string.Equals(name, "Vent", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "Wind", StringComparison.OrdinalIgnoreCase))
                return Element.Rock;

            foreach (ElementDef def in All)
            {
                if (string.Equals(def.Name, name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(def.Id.ToString(), name, StringComparison.OrdinalIgnoreCase))
                    return def.Id;
            }

            return Element.Lightning;
        }
    }

    /// <summary>Dispatches a strike to the visual effect matching the selected element class.</summary>
    internal static class ElementFx
    {
        internal static void Spawn(Element element, Vector3 pos, float duration, float radius, float height, Color color)
        {
            ElementDef def = ElementRegistry.Get(element);
            if (!def.Implemented)
                return;

            ElementVfx.Spawn(def, pos, duration, radius, height, color);
        }

        internal static void SpawnLanding(Element element, Vector3 pos, float duration, float radius, Color color)
        {
            ElementDef def = ElementRegistry.Get(element);
            if (!def.Implemented)
                return;

            ElementVfx.SpawnLanding(def, pos, duration, radius, color);
        }
    }
}
