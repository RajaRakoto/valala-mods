using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ValalaMods
{
    /// <summary>
    /// Dispatches the landing impact to the effect matching the active element.
    /// Lightning keeps its dedicated Thor impact and high-fall strike.
    /// </summary>
    internal static class LandingEffects
    {
        internal static void Spawn(Element element, Vector3 position, float fallHeight)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return;

            if (!ValalaModsPlugin.Enabled.Value)
                return;

            try
            {
                if (element == Element.Lightning)
                {
                    if (ValalaModsPlugin.LandingBoom.Value)
                        LandingBoom.Spawn(position, fallHeight);

                    if (ValalaModsPlugin.LandingThunder.Value)
                        LandingThunder.Spawn(position, fallHeight);

                    return;
                }

                if (!ValalaModsPlugin.LandingBoom.Value)
                    return;

                ElementDef def = ElementRegistry.Get(element);
                if (!def.Implemented)
                    return;

                ElementFx.SpawnLanding(
                    element,
                    position,
                    ValalaModsPlugin.Duration.Value,
                    ValalaModsPlugin.LandingRadius.Value,
                    def.Tint);

                LandingBoom.PlayImpactSound(position);
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log?.LogWarning("Landing effect failed: " + e.Message);
            }
        }
    }
}
