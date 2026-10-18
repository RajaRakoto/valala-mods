using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace ValalaMods
{
    /// <summary>
    /// Registers the Valala silver rings and exposes the equipped-ring checks used by the
    /// elemental effects. Rings are cloned from EpicLoot's SilverRing in memory; no file
    /// from EpicLoot is ever modified.
    /// </summary>
    internal static class ValalaRings
    {
        internal const int MaxQuality = 4;

        private const string BasePrefab = "SilverRing";

        private static bool _registered;

        internal static void Register()
        {
            if (_registered)
                return;

            _registered = true;

            try
            {
                if (PrefabManager.Instance.GetPrefab(BasePrefab) == null)
                {
                    ValalaModsPlugin.Log.LogError(
                        "EpicLoot SilverRing prefab not found; Valala rings are disabled.");
                    return;
                }

                RegisterLocalizations();

                RegisterRing(Element.Lightning);
                RegisterRing(Element.Rock);
                RegisterRing(Element.Fire);
                RegisterRing(Element.Poison);
                RegisterRing(Element.Ice);
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log.LogError("Valala ring registration failed: " + e);
            }
        }

        internal static bool IsEquipped(Player player, Element element)
        {
            return GetEquippedQuality(player, element) > 0;
        }

        internal static bool IsAnyMaxRingEquipped(Player player)
        {
            return GetBestEquippedQuality(player) >= MaxQuality;
        }

        internal static int GetBestEquippedQuality(Player player)
        {
            if (player == null)
                return 0;

            Inventory inventory = player.GetInventory();
            if (inventory == null)
                return 0;

            int quality = 0;
            foreach (ItemDrop.ItemData item in inventory.GetEquippedItems())
            {
                if (item == null || item.m_shared == null || !IsValalaRing(item))
                    continue;

                quality = Mathf.Max(quality, item.m_quality);
            }

            return quality;
        }

        internal static float GetDamageMultiplier(Player player, Element element)
        {
            int quality = GetEquippedQuality(player, element);
            if (quality <= 0)
                return 1f;

            float bonus = Mathf.Clamp01(ValalaModsPlugin.RingDamageBonus.Value);
            float perLevel = Mathf.Max(0f, ValalaModsPlugin.RingDamageBonusPerLevel.Value);
            return 1f + bonus + perLevel * (quality - 1);
        }

        internal static int GetEquippedQuality(Player player, Element element)
        {
            string prefabName = PrefabName(element);
            if (player == null || prefabName == null)
                return 0;

            Inventory inventory = player.GetInventory();
            if (inventory == null)
                return 0;

            string token = NameToken(element);
            int quality = 0;
            foreach (ItemDrop.ItemData item in inventory.GetEquippedItems())
            {
                if (item == null || !Matches(item, prefabName, token))
                    continue;

                quality = Mathf.Max(quality, item.m_quality);
            }

            return quality;
        }

        private static bool Matches(ItemDrop.ItemData item, string prefabName, string token)
        {
            if (item.m_dropPrefab != null && item.m_dropPrefab.name == prefabName)
                return true;

            return item.m_shared != null && item.m_shared.m_name == token;
        }

        private static bool IsValalaRing(ItemDrop.ItemData item)
        {
            if (item.m_dropPrefab != null &&
                item.m_dropPrefab.name.StartsWith("ValalaSilverRing", StringComparison.Ordinal))
                return true;

            return item.m_shared != null &&
                   item.m_shared.m_name != null &&
                   item.m_shared.m_name.StartsWith("$item_valala_silverring", StringComparison.Ordinal);
        }

        private static void RegisterRing(Element element)
        {
            try
            {
                ItemConfig config = new ItemConfig
                {
                    Name = NameToken(element),
                    Description = DescriptionToken(element),
                    StackSize = 1,
                    Weight = 0.1f,
                    Amount = 1,
                    CraftingStation = "forge",
                    RepairStation = "forge",
                    MinStationLevel = 2,
                    Requirements = Requirements(element)
                };

                CustomItem item = new CustomItem(PrefabName(element), BasePrefab, config);
                ItemDrop drop = item.ItemDrop;
                if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
                {
                    ValalaModsPlugin.Log.LogError("Could not clone ring prefab: " + PrefabName(element));
                    return;
                }

                ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;
                shared.m_itemType = ItemDrop.ItemData.ItemType.Trinket;
                shared.m_useDurability = false;
                shared.m_maxDurability = 100f;
                shared.m_maxQuality = MaxQuality;
                shared.m_maxStackSize = 1;
                shared.m_weight = 0.1f;

                drop.m_itemData.m_durability = 100f;
                drop.m_itemData.m_dropPrefab = drop.gameObject;

                if (!ItemManager.Instance.AddItem(item))
                    ValalaModsPlugin.Log.LogError("Could not register ring item: " + PrefabName(element));
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log.LogError("Could not register ring " + PrefabName(element) + ": " + e);
            }
        }

        private static RequirementConfig[] Requirements(Element element)
        {
            switch (element)
            {
                case Element.Lightning:
                    return new[]
                    {
                        new RequirementConfig("Silver", 20, 5),
                        new RequirementConfig("Crystal", 10, 2),
                        new RequirementConfig("Thunderstone", 2, 1),
                        new RequirementConfig("FineWood", 10, 2)
                    };

                case Element.Rock:
                    return new[]
                    {
                        new RequirementConfig("Silver", 20, 5),
                        new RequirementConfig("Stone", 50, 12),
                        new RequirementConfig("Crystal", 10, 2),
                        new RequirementConfig("FineWood", 10, 2)
                    };

                case Element.Fire:
                    return new[]
                    {
                        new RequirementConfig("Silver", 20, 5),
                        new RequirementConfig("Ruby", 2, 1),
                        new RequirementConfig("SurtlingCore", 5, 1),
                        new RequirementConfig("Coal", 20, 5)
                    };

                case Element.Poison:
                    return new[]
                    {
                        new RequirementConfig("Silver", 20, 5),
                        new RequirementConfig("Ooze", 10, 2),
                        new RequirementConfig("Bloodbag", 5, 1),
                        new RequirementConfig("BoneFragments", 20, 5)
                    };

                case Element.Ice:
                    return new[]
                    {
                        new RequirementConfig("Silver", 20, 5),
                        new RequirementConfig("FreezeGland", 5, 1),
                        new RequirementConfig("Crystal", 10, 2),
                        new RequirementConfig("FineWood", 10, 2)
                    };

                default:
                    return Array.Empty<RequirementConfig>();
            }
        }

        private static string PrefabName(Element element)
        {
            switch (element)
            {
                case Element.Lightning: return "ValalaSilverRingLightning";
                case Element.Rock: return "ValalaSilverRingRock";
                case Element.Fire: return "ValalaSilverRingFire";
                case Element.Poison: return "ValalaSilverRingPoison";
                case Element.Ice: return "ValalaSilverRingIce";
                default: return null;
            }
        }

        private static string NameToken(Element element)
        {
            switch (element)
            {
                case Element.Lightning: return "$item_valala_silverring_lightning";
                case Element.Rock: return "$item_valala_silverring_rock";
                case Element.Fire: return "$item_valala_silverring_fire";
                case Element.Poison: return "$item_valala_silverring_poison";
                case Element.Ice: return "$item_valala_silverring_ice";
                default: return null;
            }
        }

        private static string DescriptionToken(Element element)
        {
            return NameToken(element) + "_desc";
        }

        private static void RegisterLocalizations()
        {
            AddLocalization(
                "$item_valala_silverring_lightning",
                "Valala Silver Ring — Lightning",
                "Bague en argent de Valala — Foudre");
            AddLocalization(
                "$item_valala_silverring_lightning_desc",
                "A Valala silver ring attuned to Lightning. Equip it to enable the Lightning element.",
                "Une bague en argent de Valala accordée à la Foudre. À équiper pour activer l’élément Foudre.");

            AddLocalization(
                "$item_valala_silverring_rock",
                "Valala Silver Ring — Rock",
                "Bague en argent de Valala — Roche");
            AddLocalization(
                "$item_valala_silverring_rock_desc",
                "A Valala silver ring attuned to Rock. Equip it to enable the Rock element.",
                "Une bague en argent de Valala accordée à la Roche. À équiper pour activer l’élément Roche.");

            AddLocalization(
                "$item_valala_silverring_fire",
                "Valala Silver Ring — Fire",
                "Bague en argent de Valala — Feu");
            AddLocalization(
                "$item_valala_silverring_fire_desc",
                "A Valala silver ring attuned to Fire. Equip it to enable the Fire element.",
                "Une bague en argent de Valala accordée au Feu. À équiper pour activer l’élément Feu.");

            AddLocalization(
                "$item_valala_silverring_poison",
                "Valala Silver Ring — Poison",
                "Bague en argent de Valala — Poison");
            AddLocalization(
                "$item_valala_silverring_poison_desc",
                "A Valala silver ring attuned to Poison. Equip it to enable the Poison element.",
                "Une bague en argent de Valala accordée au Poison. À équiper pour activer l’élément Poison.");

            AddLocalization(
                "$item_valala_silverring_ice",
                "Valala Silver Ring — Ice",
                "Bague en argent de Valala — Glace");
            AddLocalization(
                "$item_valala_silverring_ice_desc",
                "A Valala silver ring attuned to Ice. Equip it to enable the Ice element.",
                "Une bague en argent de Valala accordée à la Glace. À équiper pour activer l’élément Glace.");
        }

        private static void AddLocalization(string token, string english, string french)
        {
            try
            {
                CustomLocalization localization = LocalizationManager.Instance.GetLocalization();
                localization.AddTranslation("English", token, english);
                localization.AddTranslation("French", token, french);
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log.LogWarning("Could not add localization " + token + ": " + e.Message);
            }
        }
    }
}
