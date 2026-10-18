using System;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;

namespace ValalaMods
{
    /// <summary>
    /// Extends ExtraSlotsCustomSlots' first user-defined slot to every Trinket item.
    /// The slot itself is configured in shudnal.ExtraSlotsCustomSlots.cfg; this only
    /// replaces its item predicate at runtime, so no third-party file is modified.
    /// </summary>
    internal static class SecondTrinketSlot
    {
        private const string PluginGuid = "shudnal.ExtraSlotsCustomSlots";

        internal static void Apply(Harmony harmony)
        {
            if (!ValalaModsPlugin.EnableSecondTrinketSlot.Value)
                return;

            if (!Chainloader.PluginInfos.ContainsKey(PluginGuid))
            {
                ValalaModsPlugin.Log.LogInfo(
                    "ExtraSlotsCustomSlots not installed; second trinket slot disabled.");
                return;
            }

            try
            {
                Type slotType = Type.GetType("ExtraSlotsCustomSlots.UserDefinedSlot, ExtraSlotsCustomSlots");
                FieldInfo slotsField = slotType?.GetField(
                    "userDefinedSlots", BindingFlags.Public | BindingFlags.Static);
                Array slots = slotsField?.GetValue(null) as Array;
                object slot = slots != null && slots.Length > 0 ? slots.GetValue(0) : null;
                FieldInfo predicateField = slotType?.GetField(
                    "itemIsValid", BindingFlags.Public | BindingFlags.Instance);
                FieldInfo enabledField = slotType?.GetField(
                    "slotEnabled", BindingFlags.Public | BindingFlags.Instance);

                Type slotsType = Type.GetType("ExtraSlots.Slots, ExtraSlots");
                MethodInfo isCustomSlotItem = slotsType?.GetMethod(
                    "IsCustomSlotItem", BindingFlags.Public | BindingFlags.Static);

                if (slot == null || predicateField == null || isCustomSlotItem == null)
                    throw new MissingFieldException("ExtraSlots custom slot support was not initialized.");

                ConfigEntry<bool> enabled = enabledField?.GetValue(slot) as ConfigEntry<bool>;
                bool slotEnabled = enabled != null && enabled.Value;
                if (!slotEnabled)
                {
                    ValalaModsPlugin.Log.LogWarning(
                        "ExtraSlotsCustomSlots custom slot 1 is disabled; set Enabled=true in shudnal.ExtraSlotsCustomSlots.cfg.");
                }

                Func<ItemDrop.ItemData, bool> original =
                    predicateField.GetValue(slot) as Func<ItemDrop.ItemData, bool>;

                harmony.Patch(isCustomSlotItem,
                    postfix: new HarmonyMethod(typeof(SecondTrinketSlot), nameof(IsCustomSlotItemPostfix)));

                predicateField.SetValue(slot, new Func<ItemDrop.ItemData, bool>(item =>
                    (original != null && original(item)) || IsTrinket(item)));

                if (slotEnabled)
                {
                    ValalaModsPlugin.Log.LogInfo(
                        "Second trinket slot enabled through ExtraSlotsCustomSlots custom slot 1.");
                }
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log.LogError("Could not enable the second trinket slot: " + e);
            }
        }

        private static bool IsTrinket(ItemDrop.ItemData item)
        {
            return item != null &&
                   item.m_shared != null &&
                   item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trinket;
        }

        // ExtraSlots normally blocks a custom-slot item from falling back to a regular
        // equipment slot. A trinket must still be able to use the vanilla trinket slot
        // once the custom slot is occupied.
        private static void IsCustomSlotItemPostfix(ItemDrop.ItemData item, ref bool __result)
        {
            if (__result && IsTrinket(item))
                __result = false;
        }
    }
}
