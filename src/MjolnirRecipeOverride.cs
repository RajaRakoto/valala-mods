using System;
using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace ValalaMods
{
    /// <summary>
    /// Rewrites Mjolnir's recipe after Jotunn has registered it. The recipe stays owned by
    /// Mjolnir; only its station and requirements are replaced in memory.
    /// </summary>
    internal static class MjolnirRecipeOverride
    {
        private const string MjolnirPrefab = "Kewb_Mjolnir_Item";
        private const string RecipeName = "Recipe_Kewb_Mjolnir_Item";
        private const string BlackForgePrefab = "blackforge";
        private const string DescriptionToken = "$item_kewb_mjolnir_description";
        private const int MinStationLevel = 6;

        private const string DescriptionEnglish =
            "Forged by the dwarven brothers Brokkr and Sindri, its short haft still " +
            "bears the mark of Loki's meddling. Mjölnir returns to Thor's hand, carries " +
            "the storm, and guards Asgard and Midgard alike. In Valheim the old tales " +
            "hold true: whosoever proves worthy may lift it, yet the hammer weighs " +
            "dignity, not strength. Deeds, not boasts, wake its deeper thunder.";

        private const string DescriptionFrench =
            "Forgée par les frères nains Brokkr et Sindri, sa poignée courte garde la " +
            "marque de la ruse de Loki. Mjölnir revient dans la main de Thor, porte " +
            "l'orage et veille sur Asgard comme sur Midgard. Ici, les vieilles légendes " +
            "disent vrai : quiconque se montre digne peut la soulever, car le marteau " +
            "pèse la dignité, non la force. Les actes, non les serments, éveillent son " +
            "tonnerre.";

        private static readonly RequirementSpec[] Requirements =
        {
            new RequirementSpec("FlametalNew", 30),
            new RequirementSpec("CelestialFeather", 10),
            new RequirementSpec("MoltenCore", 10),
            new RequirementSpec("ProustitePowder", 10),
            new RequirementSpec("FrostCore", 10),
            new RequirementSpec("NornThread", 10),
            new RequirementSpec("OrbThunderBlood", 10),
            new RequirementSpec("Thunderstone", 20),
            new RequirementSpec("YggdrasilWood", 30),
            new RequirementSpec("Eitr", 20),
            new RequirementSpec("Carapace", 20),
            new RequirementSpec("Gold", 20),
            new RequirementSpec("StaffLightning", 1)
        };

        internal static void Register()
        {
            ItemManager.OnItemsRegistered -= Apply;
            ItemManager.OnItemsRegistered += Apply;
        }

        internal static void Unregister()
        {
            ItemManager.OnItemsRegistered -= Apply;
        }

        private static void Apply()
        {
            ApplyDescription();
            try
            {
                ApplyInternal();
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log.LogError("Mjolnir recipe override failed: " + e);
            }
        }

        private static void ApplyDescription()
        {
            try
            {
                CustomLocalization localization = LocalizationManager.Instance.GetLocalization();
                localization.AddTranslation("English", DescriptionToken, DescriptionEnglish);
                localization.AddTranslation("French", DescriptionToken, DescriptionFrench);
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log.LogWarning(
                    "Mjolnir description override failed; keeping the bundled text: " + e.Message);
            }
        }

        private static void ApplyInternal()
        {
            ObjectDB db = ObjectDB.instance;
            if (db == null)
                return;

            GameObject itemPrefab = db.GetItemPrefab(MjolnirPrefab);
            ItemDrop itemDrop = itemPrefab != null ? itemPrefab.GetComponent<ItemDrop>() : null;
            if (itemDrop == null || itemDrop.m_itemData == null)
            {
                ValalaModsPlugin.Log.LogDebug(
                    "Mjolnir item is not registered; recipe override skipped.");
                return;
            }

            Recipe recipe = db.GetRecipe(itemDrop.m_itemData);
            if (recipe == null)
            {
                foreach (Recipe candidate in db.m_recipes)
                {
                    if (candidate != null && candidate.name == RecipeName)
                    {
                        recipe = candidate;
                        break;
                    }
                }
            }

            if (recipe == null)
            {
                ValalaModsPlugin.Log.LogWarning(
                    "Mjolnir recipe not found; recipe override skipped.");
                return;
            }

            CraftingStation blackForge = FindBlackForge(db);
            if (blackForge == null)
            {
                recipe.m_enabled = false;
                ValalaModsPlugin.Log.LogError(
                    "Black Forge station not found; Mjolnir recipe disabled.");
                return;
            }

            if (!TryBuildRequirements(db, out Piece.Requirement[] requirements))
            {
                recipe.m_enabled = false;
                return;
            }

            recipe.m_craftingStation = blackForge;
            recipe.m_repairStation = blackForge;
            recipe.m_minStationLevel = MinStationLevel;
            recipe.m_resources = requirements;
            recipe.m_requireOnlyOneIngredient = false;

            ValalaModsPlugin.Log.LogInfo(
                "Mjolnir recipe overridden: blackforge level " + MinStationLevel +
                ", " + requirements.Length + " requirements.");
        }

        private static CraftingStation FindBlackForge(ObjectDB db)
        {
            foreach (Recipe recipe in db.m_recipes)
            {
                CraftingStation station = recipe != null ? recipe.m_craftingStation : null;
                if (station != null && station.name == BlackForgePrefab)
                    return station;
            }

            ZNetScene scene = ZNetScene.instance;
            if (scene == null)
                return null;

            GameObject prefab = scene.GetPrefab(BlackForgePrefab);
            return prefab != null ? prefab.GetComponent<CraftingStation>() : null;
        }

        private static bool TryBuildRequirements(
            ObjectDB db, out Piece.Requirement[] requirements)
        {
            List<Piece.Requirement> resolved =
                new List<Piece.Requirement>(Requirements.Length);
            List<string> missing = null;

            foreach (RequirementSpec spec in Requirements)
            {
                GameObject prefab = db.GetItemPrefab(spec.Prefab);
                ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (drop == null)
                {
                    if (missing == null)
                        missing = new List<string>();
                    missing.Add(spec.Prefab);
                    continue;
                }

                resolved.Add(new Piece.Requirement
                {
                    m_resItem = drop,
                    m_amount = spec.Amount,
                    m_amountPerLevel = spec.AmountPerLevel,
                    m_recover = true
                });
            }

            if (missing != null)
            {
                requirements = null;
                ValalaModsPlugin.Log.LogError(
                    "Mjolnir recipe disabled; missing item prefabs: " +
                    string.Join(", ", missing));
                return false;
            }

            requirements = resolved.ToArray();
            return true;
        }

        private readonly struct RequirementSpec
        {
            internal readonly string Prefab;
            internal readonly int Amount;
            internal readonly int AmountPerLevel;

            internal RequirementSpec(string prefab, int amount, int amountPerLevel = 0)
            {
                Prefab = prefab;
                Amount = amount;
                AmountPerLevel = amountPerLevel;
            }
        }
    }
}
