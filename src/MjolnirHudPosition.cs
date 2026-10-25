using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValalaMods
{
    internal static class MjolnirHudPosition
    {
        private const string PresenterTypeName =
            "ValheimMjolnir.Presentation.MjolnirAbilityHudPresenter";
        private const string MethodName = "ResolvePanelPosition";
        private const float PanelWidth = 250f;
        private const float DefaultRightMargin = 12f;
        private const float DefaultBottomMargin = 24f;

        internal static void Apply(Harmony harmony)
        {
            try
            {
                Type presenterType = AccessTools.TypeByName(PresenterTypeName);
                MethodInfo resolvePosition = presenterType == null
                    ? null
                    : AccessTools.Method(presenterType, MethodName);

                if (resolvePosition == null)
                {
                    ValalaModsPlugin.Log.LogWarning(
                        "Mjolnir HUD position patch skipped: " + PresenterTypeName +
                        "." + MethodName + " not found.");
                    return;
                }

                harmony.Patch(
                    resolvePosition,
                    postfix: new HarmonyMethod(
                        typeof(MjolnirHudPosition), nameof(Postfix)));

                ValalaModsPlugin.Log.LogInfo(
                    "Mjolnir HUD anchored bottom-right: right margin " +
                    RightMargin() + " px, bottom margin " +
                    BottomMargin() + " px (Valala config).");
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log.LogError(
                    "Could not patch Mjolnir HUD position: " + e);
            }
        }

        private static float RightMargin()
        {
            return Mathf.Clamp(
                ValalaModsPlugin.MjolnirHudRightMargin?.Value ?? DefaultRightMargin,
                0f,
                200f);
        }

        private static float BottomMargin()
        {
            return Mathf.Clamp(
                ValalaModsPlugin.MjolnirHudBottomMargin?.Value ?? DefaultBottomMargin,
                0f,
                200f);
        }

        private static void Postfix(float height, ref float left, ref float top)
        {
            float rightMargin = RightMargin();
            float bottomMargin = BottomMargin();

            left = Mathf.Clamp(
                Screen.width - PanelWidth - rightMargin,
                0f,
                Mathf.Max(0f, Screen.width - PanelWidth));
            top = Mathf.Clamp(
                Screen.height - height - bottomMargin,
                0f,
                Mathf.Max(0f, Screen.height - height));
        }
    }
}
