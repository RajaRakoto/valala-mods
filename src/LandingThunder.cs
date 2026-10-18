using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ValalaMods
{
    /// <summary>
    /// High-fall landing strike for the Lightning class: the same lightning VFX as the
    /// Wind Impulse release, plus an enemy-only damage pulse around the impact point.
    /// The normal Thor landing impact is handled separately and remains unchanged.
    /// </summary>
    internal static class LandingThunder
    {
        internal static bool IsAboveThreshold(float fallHeight)
        {
            float threshold = Mathf.Max(
                ValalaModsPlugin.LandingThunderHeight.Value,
                ValalaModsPlugin.LandingHeight.Value);
            return fallHeight >= threshold;
        }

        internal static void Spawn(Vector3 position, float fallHeight)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return;

            if (!ValalaModsPlugin.Enabled.Value || !ValalaModsPlugin.LandingThunder.Value)
                return;

            if (!IsAboveThreshold(fallHeight))
                return;

            try
            {
                ElementFx.Spawn(
                    Element.Lightning,
                    position,
                    ValalaModsPlugin.Duration.Value,
                    ValalaModsPlugin.Radius.Value,
                    ValalaModsPlugin.Height.Value,
                    ValalaModsPlugin.ParseColor(ValalaModsPlugin.ColorHex.Value));
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log?.LogWarning("Landing thunder VFX failed: " + e.Message);
            }
        }

        internal static void Damage(Player player, Vector3 position, float fallHeight)
        {
            if (player == null)
                return;

            if (!ValalaModsPlugin.Enabled.Value ||
                !ValalaModsPlugin.LandingThunder.Value ||
                !ValalaModsPlugin.Damage.Value)
                return;

            if (!IsAboveThreshold(fallHeight))
                return;

            try
            {
                ValalaModsPlugin.DamageEnemiesAt(
                    player,
                    position,
                    ValalaModsPlugin.LandingThunderRadius.Value,
                    Element.Lightning);
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log?.LogWarning("Landing thunder damage failed: " + e.Message);
            }
        }
    }
}
