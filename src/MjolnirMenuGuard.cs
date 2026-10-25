using System;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Configs;

namespace ValalaMods
{
    internal static class MjolnirMenuGuard
    {
        private const string InputBindingTypeName = "ValheimMjolnir.Input.InputBindingService";
        private const string SettingsMenuTypeName = "ValheimMjolnir.UI.MjolnirSettingsMenu";
        private const string OpenSettingsButtonName = "OpenSettings";
        private const string OpenSettingsBindingToken = "$kewb_mjolnir_settings";

        private static bool _applied;
        private static PropertyInfo _bindingNameProperty;

        internal static void Apply(Harmony harmony)
        {
            if (_applied)
                return;

            try
            {
                Type inputType = AccessTools.TypeByName(InputBindingTypeName);
                MethodInfo isDown = inputType == null
                    ? null
                    : AccessTools.Method(inputType, "IsDown", new[] { typeof(ButtonConfig) });
                if (isDown != null)
                {
                    harmony.Patch(
                        isDown,
                        prefix: new HarmonyMethod(
                            typeof(MjolnirMenuGuard), nameof(IsDownPrefix)));
                }
                else
                {
                    ValalaModsPlugin.Log?.LogWarning(
                        "Mjolnir menu guard: " + InputBindingTypeName +
                        ".IsDown not found.");
                }

                Type menuType = AccessTools.TypeByName(SettingsMenuTypeName);
                MethodInfo canOpen = menuType == null
                    ? null
                    : AccessTools.Method(menuType, "CanOpen");
                if (canOpen != null)
                {
                    harmony.Patch(
                        canOpen,
                        prefix: new HarmonyMethod(
                            typeof(MjolnirMenuGuard), nameof(CanOpenPrefix)));
                }
                else
                {
                    ValalaModsPlugin.Log?.LogWarning(
                        "Mjolnir menu guard: " + SettingsMenuTypeName +
                        ".CanOpen not found.");
                }

                MethodInfo isBindingVisible = menuType == null
                    ? null
                    : AccessTools.Method(menuType, "IsBindingVisible");
                if (isBindingVisible != null)
                {
                    harmony.Patch(
                        isBindingVisible,
                        postfix: new HarmonyMethod(
                            typeof(MjolnirMenuGuard), nameof(IsBindingVisiblePostfix)));
                }
                else
                {
                    ValalaModsPlugin.Log?.LogWarning(
                        "Mjolnir menu guard: " + SettingsMenuTypeName +
                        ".IsBindingVisible not found.");
                }

                _applied = true;
                KeyboardShortcut shortcut =
                    ValalaModsPlugin.MjolnirMenuOpenKey?.Value ?? KeyboardShortcut.Empty;
                ValalaModsPlugin.Log?.LogInfo(
                    "Mjolnir settings menu " +
                    (Enabled() ? "enabled" : "disabled") +
                    "; open key: " + shortcut.Serialize() + " (Valala config).");
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log?.LogWarning(
                    "Could not patch Mjolnir settings menu guard: " + e.Message);
            }
        }

        private static bool Enabled()
        {
            return ValalaModsPlugin.MjolnirMenuEnabled?.Value == true;
        }

        private static bool IsDownPrefix(ButtonConfig __0, ref bool __result)
        {
            if (__0 == null ||
                !string.Equals(__0.Name, OpenSettingsButtonName, StringComparison.Ordinal))
                return true;

            __result = false;
            if (!Enabled())
                return false;

            KeyboardShortcut shortcut =
                ValalaModsPlugin.MjolnirMenuOpenKey?.Value ?? KeyboardShortcut.Empty;
            __result = shortcut.IsDown();
            return false;
        }

        private static bool CanOpenPrefix(ref bool __result)
        {
            if (Enabled())
                return true;

            __result = false;
            return false;
        }

        private static void IsBindingVisiblePostfix(object __0, ref bool __result)
        {
            if (!__result || __0 == null)
                return;

            try
            {
                Type rowType = __0.GetType();
                if (_bindingNameProperty == null ||
                    _bindingNameProperty.DeclaringType != rowType)
                {
                    _bindingNameProperty = AccessTools.Property(rowType, "Name");
                }

                if (_bindingNameProperty == null)
                    return;

                if (string.Equals(
                    _bindingNameProperty.GetValue(__0) as string,
                    OpenSettingsBindingToken,
                    StringComparison.Ordinal))
                {
                    __result = false;
                }
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log?.LogWarning(
                    "Mjolnir menu guard: could not hide open-settings binding: " +
                    e.Message);
            }
        }
    }
}
