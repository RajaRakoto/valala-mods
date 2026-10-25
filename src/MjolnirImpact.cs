using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValalaMods
{
    /// <summary>
    /// Adds a lightning strike and area damage to thrown Mjolnir physical impacts.
    /// Mjolnir already carves terrain craters and creates a bolt on enemy hits; this
    /// module only fills the terrain/structure gap and the area damage.
    /// </summary>
    internal static class MjolnirImpactPatch
    {
        private const string ActorTypeName = "ValheimMjolnir.Gameplay.MjolnirWorldActor";

        private static PropertyInfo _ownerProperty;
        private static PropertyInfo _stateProperty;
        private static PropertyInfo _speedProperty;
        private static FieldInfo _velocityBeforePhysicsField;
        private static bool _applied;
        private static bool _errorLogged;
        private static float _nextAllowedTime;

        internal static void Apply(Harmony harmony)
        {
            if (_applied)
                return;

            try
            {
                Type actorType = AccessTools.TypeByName(ActorTypeName);
                MethodInfo resolveImpact = actorType == null
                    ? null
                    : AccessTools.Method(actorType, "ResolveImpact", new[]
                    {
                        typeof(Collider),
                        typeof(Vector3),
                        typeof(Vector3),
                        typeof(Vector3),
                        typeof(bool)
                    });

                if (resolveImpact == null)
                {
                    ValalaModsPlugin.Log?.LogWarning(
                        "Mjolnir impact patch skipped: MjolnirWorldActor.ResolveImpact not found.");
                    return;
                }

                _ownerProperty = AccessTools.Property(actorType, "Owner");
                _stateProperty = AccessTools.Property(actorType, "LogicalState");
                _speedProperty = AccessTools.Property(actorType, "Speed");
                _velocityBeforePhysicsField =
                    AccessTools.Field(actorType, "outboundVelocityBeforePhysics");

                Type protectionType = AccessTools.TypeByName(
                    "ValheimMjolnir.Gameplay.MjolnirProtectionApi");
                MjolnirImpactDamage.ProtectionAllows = protectionType == null
                    ? null
                    : AccessTools.Method(protectionType, "Allows", new[]
                    {
                        typeof(Player),
                        typeof(Component),
                        typeof(Vector3)
                    });

                if (_ownerProperty == null || _stateProperty == null || _speedProperty == null)
                {
                    ValalaModsPlugin.Log?.LogWarning(
                        "Mjolnir impact patch skipped: MjolnirWorldActor properties not found.");
                    return;
                }

                harmony.Patch(
                    resolveImpact,
                    prefix: new HarmonyMethod(typeof(MjolnirImpactPatch), nameof(Prefix)),
                    postfix: new HarmonyMethod(typeof(MjolnirImpactPatch), nameof(Postfix)));

                _applied = true;
                ValalaModsPlugin.Log?.LogInfo(
                    "Mjolnir impact module active: lightning and area damage on thrown impacts.");
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log?.LogWarning(
                    "Could not patch Mjolnir impacts: " + e.Message);
            }
        }

        private static void Prefix(object __instance, Collider __0, bool __4, out bool __state)
        {
            __state = false;

            try
            {
                if (!ValalaModsPlugin.Enabled.Value ||
                    !ValalaModsPlugin.MjolnirImpactEnabled.Value ||
                    __4)
                    return;

                if (__instance == null || __0 == null)
                    return;

                Player owner = _ownerProperty?.GetValue(__instance, null) as Player;
                if (owner == null || owner != Player.m_localPlayer)
                    return;

                string state = _stateProperty?.GetValue(__instance, null)?.ToString();
                if (state != "ThrownOutbound")
                    return;

                float speed = _speedProperty == null
                    ? 0f
                    : Convert.ToSingle(_speedProperty.GetValue(__instance, null));

                // Speed is read before ResolveImpact mutates the body, but use the
                // pre-physics velocity when available to avoid post-collision damping.
                if (_velocityBeforePhysicsField != null)
                {
                    Vector3 velocity = (Vector3)_velocityBeforePhysicsField.GetValue(__instance);
                    if (velocity.sqrMagnitude > 0.0001f)
                        speed = velocity.magnitude;
                }

                if (speed < ValalaModsPlugin.MjolnirImpactMinSpeed.Value)
                    return;

                Character character = __0.GetComponentInParent<Character>();
                if (character == owner)
                    return;

                bool isCharacter = character != null;
                bool isTerrain = IsTerrain(__0);
                bool isStructure = !isCharacter &&
                    __0.GetComponentInParent(typeof(IDestructible)) is Component;

                if (!isCharacter && !isTerrain && !isStructure)
                    return;

                __state = true;
            }
            catch (Exception e)
            {
                LogErrorOnce(e);
            }
        }

        private static void Postfix(
            object __instance, Collider __0, Vector3 __1, Vector3 __2, bool __4, bool __state)
        {
            if (!__state)
                return;

            try
            {
                float now = Time.time;
                if (now < _nextAllowedTime)
                    return;

                _nextAllowedTime = now + ValalaModsPlugin.MjolnirImpactCooldown.Value;

                Player owner = _ownerProperty?.GetValue(__instance, null) as Player;
                if (owner == null)
                    return;

                bool isCharacter = __0 != null && __0.GetComponentInParent<Character>() != null;

                if (!ValalaModsPlugin.IsHeadless &&
                    ValalaModsPlugin.MjolnirImpactVfx.Value &&
                    !isCharacter)
                {
                    Vector3 vfxPoint = __1;
                    if (__2.sqrMagnitude > 0.0001f)
                        vfxPoint += __2.normalized * 0.1f;

                    RpcSync.Broadcast(
                        Element.Lightning,
                        vfxPoint,
                        ValalaModsPlugin.Duration.Value,
                        ValalaModsPlugin.MjolnirImpactRadius.Value,
                        ValalaModsPlugin.Height.Value,
                        ElementRegistry.Get(Element.Lightning).Tint);
                }

                if (!ValalaModsPlugin.Damage.Value ||
                    !ValalaModsPlugin.MjolnirImpactDamageEnabled.Value)
                    return;

                ValalaModsPlugin.DamageEnemiesWithLightning(
                    owner,
                    __1,
                    ValalaModsPlugin.MjolnirImpactRadius.Value,
                    ValalaModsPlugin.MjolnirImpactDamageAmount.Value);

                if (ValalaModsPlugin.MjolnirImpactStructures.Value)
                {
                    MjolnirImpactDamage.DamageStructuresAt(
                        owner,
                        __1,
                        ValalaModsPlugin.MjolnirImpactRadius.Value,
                        ValalaModsPlugin.MjolnirImpactDamageAmount.Value,
                        ValalaModsPlugin.MjolnirImpactPlayerStructures.Value);
                }
            }
            catch (Exception e)
            {
                LogErrorOnce(e);
            }
        }

        private static bool IsTerrain(Collider collider)
        {
            if (collider.GetComponentInParent<Terrain>() != null)
                return true;

            string name = collider.name ?? string.Empty;
            return name.IndexOf("terrain", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("heightmap", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void LogErrorOnce(Exception e)
        {
            if (_errorLogged)
                return;

            _errorLogged = true;
            ValalaModsPlugin.Log?.LogWarning("Mjolnir impact effect failed: " + e);
        }
    }

    /// <summary>Flat lightning damage applied to structures around a Mjolnir impact.</summary>
    internal static class MjolnirImpactDamage
    {
        internal static MethodInfo ProtectionAllows;

        internal static void DamageStructuresAt(
            Player player, Vector3 center, float radius, float damage, bool damagePlayerStructures)
        {
            if (player == null || damage <= 0f)
                return;

            Collider[] colliders = Physics.OverlapSphere(
                center, radius, -5, QueryTriggerInteraction.Collide);
            HashSet<int> damaged = new HashSet<int>();

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                    continue;

                Component component = collider.GetComponentInParent(typeof(IDestructible)) as Component;
                if (component == null)
                    continue;

                IDestructible destructible = component as IDestructible;
                if (destructible == null)
                    continue;

                if (component.GetComponentInParent<Character>() != null)
                    continue;

                if (!damaged.Add(component.GetInstanceID()))
                    continue;

                Vector3 position = component.transform.position;

                if (ProtectionAllows != null)
                {
                    try
                    {
                        if (!(bool)ProtectionAllows.Invoke(
                                null, new object[] { player, component, position }))
                            continue;
                    }
                    catch
                    {
                        continue;
                    }
                }

                Piece piece = component.GetComponentInParent<Piece>();
                Ship ship = component.GetComponentInParent<Ship>();
                if (piece != null || ship != null)
                {
                    if (!damagePlayerStructures)
                        continue;

                    if (!PrivateArea.CheckAccess(position, 0f, false, true))
                        continue;
                }

                Vector3 direction = position - center;
                if (direction.sqrMagnitude < 0.0001f)
                    direction = Vector3.up;

                // Mjolnir's own structure impact is mostly blunt; keep the AoE effective
                // against wood and stone instead of relying on lightning-only modifiers.
                float blunt = damage * 0.65f;
                float lightning = damage - blunt;

                HitData hit = new HitData();
                hit.m_damage.m_blunt = blunt;
                hit.m_damage.m_lightning = lightning;
                hit.m_point = position;
                hit.m_dir = direction.normalized;
                hit.m_pushForce = 40f;
                hit.m_toolTier = 4;
                hit.m_dodgeable = false;
                hit.m_blockable = false;
                hit.m_ranged = true;
                hit.m_skill = Skills.SkillType.ElementalMagic;
                hit.SetAttacker(player);
                destructible.Damage(hit);
            }
        }
    }
}
