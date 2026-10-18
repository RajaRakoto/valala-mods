using System;
using HarmonyLib;
using ValalaMods.NorseFx;
using UnityEngine;
using UnityEngine.Rendering;

namespace ValalaMods
{
    /// <summary>
    /// Spawns Thor's vanilla landing impact (iron sledge hit) when the Lightning class lands
    /// from a meaningful height. Mirrors NorseDemigods AttributeTitanLegs (BLUE theme).
    /// </summary>
    internal static class LandingBoom
    {
        internal const string EffectId = "norse.landing.lightning_boom";

        private const string ThorVfxPrefab = "vfx_sledge_iron_hit";
        private const string ThorSfxPrefab = "sfx_sledge_iron_hit";
        private const float DestroyAfter = 5f;

        private static bool _thorVfxWarned;
        private static bool _thorSfxWarned;

        internal static void Spawn(Vector3 position, float fallHeight)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return;

            if (!ValalaModsPlugin.Enabled.Value || !ValalaModsPlugin.LandingBoom.Value)
                return;

            try
            {
                if (SpawnThorImpact(position))
                    return;

                GameObject norse = NorseFxService.SpawnVfx(
                    EffectId, position, Quaternion.identity, null, 3f, 1f);

                if (norse != null)
                {
                    NorseFxService.PlaySfx(EffectId, position);
                    return;
                }

                ElementDef lightning = ElementRegistry.Get(Element.Lightning);
                ElementFx.Spawn(Element.Lightning, position, 1.5f, 1f, 1f, lightning.Tint);
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log?.LogWarning("Landing boom failed: " + e.Message);
            }
        }

        // Exact copy of NorseDemigods AttributeTitanLegs.OnLanded (BLUE): vanilla sledge
        // impact VFX and sound, spawned locally by every client through the landing RPC.
        private static bool SpawnThorImpact(Vector3 position)
        {
            if (ZNetScene.instance == null)
                return false;

            GameObject vfxPrefab = ZNetScene.instance.GetPrefab(ThorVfxPrefab);
            if (vfxPrefab == null)
            {
                if (!_thorVfxWarned)
                {
                    _thorVfxWarned = true;
                    ValalaModsPlugin.Log?.LogWarning(
                        "Vanilla VFX not found: " + ThorVfxPrefab + " (using Norse fallback)");
                }

                return false;
            }

            SpawnLocal(vfxPrefab, position);
            PlayImpactSound(position);
            return true;
        }

        internal static void PlayImpactSound(Vector3 position)
        {
            if (ZNetScene.instance == null)
                return;

            try
            {
                GameObject sfxPrefab = ZNetScene.instance.GetPrefab(ThorSfxPrefab);
                if (sfxPrefab != null)
                {
                    SpawnLocal(sfxPrefab, position);
                    return;
                }

                if (!_thorSfxWarned)
                {
                    _thorSfxWarned = true;
                    ValalaModsPlugin.Log?.LogWarning("Vanilla SFX not found: " + ThorSfxPrefab);
                }
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log?.LogWarning("Landing impact sound failed: " + e.Message);
            }
        }

        private static void SpawnLocal(GameObject prefab, Vector3 position)
        {
            GameObject go = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            UnityEngine.Object.Destroy(go, DestroyAfter);
        }
    }

    /// <summary>
    /// Reads the fall height before the vanilla method resets it, then broadcasts the impact.
    /// </summary>
    [HarmonyPatch(typeof(Character), "UpdateGroundContact")]
    internal static class LandingBoomPatch
    {
        private static readonly AccessTools.FieldRef<Character, bool> GroundContact =
            AccessTools.FieldRefAccess<Character, bool>("m_groundContact");

        private static readonly AccessTools.FieldRef<Character, float> MaxAirAltitude =
            AccessTools.FieldRefAccess<Character, float>("m_maxAirAltitude");

        private static readonly AccessTools.FieldRef<Character, Vector3> GroundContactPoint =
            AccessTools.FieldRefAccess<Character, Vector3>("m_groundContactPoint");

        private static float _nextAllowedTime;

        private static float LandingThreshold(bool landingBoom, bool landingThunder)
        {
            float threshold = float.MaxValue;
            if (landingBoom)
                threshold = Mathf.Min(threshold, ValalaModsPlugin.LandingHeight.Value);
            if (landingThunder)
                threshold = Mathf.Min(threshold, Mathf.Max(
                    ValalaModsPlugin.LandingThunderHeight.Value,
                    ValalaModsPlugin.LandingHeight.Value));
            return threshold;
        }

        private static void Prefix(Character __instance)
        {
            try
            {
                Player player = __instance as Player;
                if (player == null || player != Player.m_localPlayer)
                    return;

                Element element = ElementRegistry.Current;
                ElementDef def = ElementRegistry.Get(element);
                bool landingBoom = ValalaModsPlugin.LandingBoom.Value;
                bool landingThunder = element == Element.Lightning &&
                                      ValalaModsPlugin.LandingThunder.Value;

                if (!ValalaModsPlugin.Enabled.Value ||
                    !def.Implemented ||
                    !ValalaRings.IsEquipped(player, element) ||
                    (!landingBoom && !landingThunder))
                    return;

                if (!GroundContact(__instance) ||
                    __instance.IsDead() ||
                    __instance.IsTeleporting() ||
                    __instance.IsSwimming())
                    return;

                float fallHeight = MaxAirAltitude(__instance) - __instance.transform.position.y;
                if (fallHeight < LandingThreshold(landingBoom, landingThunder))
                    return;

                if (Time.time < _nextAllowedTime)
                    return;

                _nextAllowedTime = Time.time + 0.5f;

                Vector3 point = GroundContactPoint(__instance);
                if (point == Vector3.zero)
                    point = __instance.transform.position;

                bool highFallLightning = landingThunder && LandingThunder.IsAboveThreshold(fallHeight);

                RpcSync.BroadcastLanding(element, point, fallHeight);

                if (!ValalaModsPlugin.Damage.Value)
                    return;

                if (highFallLightning)
                    LandingThunder.Damage(player, point, fallHeight);
                else if (landingBoom)
                    ValalaModsPlugin.DamageEnemiesAt(
                        player,
                        point,
                        ValalaModsPlugin.LandingRadius.Value,
                        element);
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log?.LogWarning("Landing boom check failed: " + e.Message);
            }
        }
    }
}
