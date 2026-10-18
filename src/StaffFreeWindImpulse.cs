using System;
using System.Reflection;
using BepInEx.Configuration;
using ValheimConsole = global::Console;
using HarmonyLib;
using MT.Core;
using MT.Magia;
using UnityEngine;

namespace ValalaMods
{
    internal static class StaffFreeWindImpulse
    {
        private const string WindImpulseId = "impulso_viento";
        private const int MaxRingQuality = 4;
        private const float MaxChargeMultiplier = 5f;

        private static readonly MethodInfo TryCastMethod = AccessTools.Method(typeof(Caster), "TryCast");
        private static readonly MethodInfo EmpezarConTeclaMethod = AccessTools.Method(typeof(Caster), "EmpezarConTecla");
        private static readonly MethodInfo ExecuteMethod = AccessTools.Method(typeof(Caster), "Execute");
        private static readonly MethodInfo StartLoopAnimMethod = AccessTools.Method(typeof(Caster), "StartLoopAnim");
        private static readonly MethodInfo KeepLoopAnimMethod = AccessTools.Method(typeof(Caster), "KeepLoopAnim");
        private static readonly MethodInfo StopLoopAnimMethod = AccessTools.Method(typeof(Caster), "StopLoopAnim");
        private static readonly PropertyInfo ChargingSpellProperty = AccessTools.Property(typeof(Caster), "HechizoCargando");

        private static readonly AccessTools.FieldRef<Caster, float> ChargeStart =
            AccessTools.FieldRefAccess<Caster, float>("_chargeStart");
        private static readonly AccessTools.FieldRef<Caster, float> ChargePaid =
            AccessTools.FieldRefAccess<Caster, float>("_chargePaid");
        private static readonly AccessTools.FieldRef<Caster, float> ChargeMultiplier =
            AccessTools.FieldRefAccess<Caster, float>("_chargeMult");

        private static readonly Type MagicPluginType = AccessTools.TypeByName("MT.Magia.MTMagiaPlugin");
        private static readonly FieldInfo ChargeSpeedField = MagicPluginType == null
            ? null
            : AccessTools.Field(MagicPluginType, "CargaVelocidad");
        private static readonly FieldInfo ChargeManaField = MagicPluginType == null
            ? null
            : AccessTools.Field(MagicPluginType, "CargaMana");

        private static bool _bypassFocus;
        private static bool _redirectExecute;
        private static bool _staffFreeCharging;
        private static bool _executingWind;
        private static int _chargeQuality;
        private static bool _errorLogged;

        internal static bool IsCharging
        {
            get
            {
                Caster caster = Caster.Instance;
                return _staffFreeCharging &&
                       caster != null &&
                       Player.m_localPlayer != null &&
                       !Player.m_localPlayer.IsDead() &&
                       GetChargingSpell(caster) != null;
            }
        }

        internal static float ChargeFraction
        {
            get
            {
                Caster caster = Caster.Instance;
                if (caster == null)
                    return 0f;

                return Mathf.Clamp01((ChargeMultiplier(caster) - 1f) / (MaxChargeMultiplier - 1f));
            }
        }

        internal static float LimitFraction
        {
            get
            {
                if (_chargeQuality <= 0)
                    return 0f;

                return Mathf.Clamp01((ChargeMaxForQuality(_chargeQuality) - 1f) / (MaxChargeMultiplier - 1f));
            }
        }

        internal static void Initialize(Harmony harmony)
        {
            try
            {
                MethodInfo update = AccessTools.Method(typeof(Caster), "Update");
                MethodInfo updateCharge = AccessTools.Method(typeof(Caster), "UpdateCharge");
                MethodInfo cancel = AccessTools.Method(typeof(Caster), "CancelCurrentCast");
                MethodInfo execute = AccessTools.Method(typeof(Caster), "Execute");
                MethodInfo maxCharge = AccessTools.Method(typeof(Caster), "MaxCharge");
                MethodInfo forceJump = AccessTools.Method(typeof(Character), "ForceJump");
                MethodInfo levitationImpulse = AccessTools.Method(typeof(Levitacion), "Impulsar");
                MethodInfo focus = AccessTools.Method(typeof(Focus), nameof(Focus.HasFocus));

                if (update == null || updateCharge == null || cancel == null || execute == null ||
                    maxCharge == null || forceJump == null || levitationImpulse == null || focus == null ||
                    TryCastMethod == null || EmpezarConTeclaMethod == null ||
                    ExecuteMethod == null || StartLoopAnimMethod == null ||
                    KeepLoopAnimMethod == null || StopLoopAnimMethod == null ||
                    ChargingSpellProperty == null)
                    throw new MissingMethodException("Required MT.Magia methods were not found.");

                harmony.Patch(update,
                    postfix: new HarmonyMethod(typeof(StaffFreeWindImpulse), nameof(UpdatePostfix)));
                harmony.Patch(updateCharge,
                    prefix: new HarmonyMethod(typeof(StaffFreeWindImpulse), nameof(UpdateChargePrefix)));
                harmony.Patch(cancel,
                    prefix: new HarmonyMethod(typeof(StaffFreeWindImpulse), nameof(CancelCurrentCastPrefix)));
                harmony.Patch(execute,
                    prefix: new HarmonyMethod(typeof(StaffFreeWindImpulse), nameof(ExecutePrefix)));
                harmony.Patch(maxCharge,
                    postfix: new HarmonyMethod(typeof(StaffFreeWindImpulse), nameof(MaxChargePostfix)));
                harmony.Patch(forceJump,
                    prefix: new HarmonyMethod(typeof(StaffFreeWindImpulse), nameof(ForceJumpPrefix)));
                harmony.Patch(levitationImpulse,
                    prefix: new HarmonyMethod(typeof(StaffFreeWindImpulse), nameof(LevitationImpulsePrefix)));
                harmony.Patch(focus,
                    prefix: new HarmonyMethod(typeof(StaffFreeWindImpulse), nameof(FocusPrefix)));
            }
            catch (Exception e)
            {
                LogErrorOnce("Could not patch staff-free Wind Impulse: " + e);
            }
        }

        private static void UpdatePostfix(Caster __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                if (_staffFreeCharging && GetChargingSpell(__instance) == null)
                    _staffFreeCharging = false;

                if (!ValalaModsPlugin.EnableStaffFreeWindImpulse.Value ||
                    __instance != Caster.Instance ||
                    IsInputBlocked() ||
                    !IsCastKeyDown())
                    return;

                Player player = Player.m_localPlayer;
                if (player == null || player.IsDead() || player.InPlaceMode() || Focus.HasFocus(player))
                    return;

                int quality = ValalaRings.GetBestEquippedQuality(player);
                if (quality <= 0)
                    return;

                Spell spell = FindWindImpulse();
                if (spell == null ||
                    !Pergaminos.Knows(player, spell.Id) ||
                    !Pergaminos.CanCast(player, spell))
                    return;

                _bypassFocus = true;
                _redirectExecute = true;
                _chargeQuality = quality;
                try
                {
                    TryCastMethod.Invoke(__instance, new object[] { player, spell });
                }
                finally
                {
                    _bypassFocus = false;
                    _redirectExecute = false;
                }
            }
            catch (Exception e)
            {
                _bypassFocus = false;
                _redirectExecute = false;
                _staffFreeCharging = false;
                LogErrorOnce("Staff-free Wind Impulse failed: " + e);
            }
        }

        private static bool UpdateChargePrefix(Caster __instance, Player p)
        {
            if (!_staffFreeCharging)
                return true;

            Spell spell = GetChargingSpell(__instance);
            if (spell == null || spell.Id != WindImpulseId)
            {
                _staffFreeCharging = false;
                return true;
            }

            try
            {
                UpdateStaffFreeCharge(__instance, p, spell);
            }
            catch (Exception e)
            {
                CancelCharge(__instance, p);
                LogErrorOnce("Staff-free Wind Impulse update failed: " + e);
            }

            return false;
        }

        private static bool CancelCurrentCastPrefix()
        {
            return !_staffFreeCharging;
        }

        private static bool ExecutePrefix(Caster __instance, Player p, Spell s)
        {
            if (!_redirectExecute)
                return true;

            _redirectExecute = false;
            if (s == null || s.Id != WindImpulseId)
                return true;

            try
            {
                object started = EmpezarConTeclaMethod.Invoke(__instance, new object[] { p, s, new Func<bool>(IsCastKeyHeld) });
                _staffFreeCharging = started is bool value && value;
            }
            catch (Exception e)
            {
                _staffFreeCharging = false;
                LogErrorOnce("Staff-free Wind Impulse charge failed: " + e);
            }

            return false;
        }

        private static void MaxChargePostfix(Player p, Spell s, ref float __result)
        {
            try
            {
                if (s == null || s.Id != WindImpulseId || p == null || Focus.HasFocus(p))
                    return;

                int quality = ValalaRings.GetBestEquippedQuality(p);
                if (quality > 0)
                    __result = ChargeMaxForQuality(quality);
            }
            catch (Exception e)
            {
                LogErrorOnce("Staff-free Wind Impulse max charge failed: " + e);
            }
        }

        private static void ForceJumpPrefix(Character __instance, ref Vector3 vel)
        {
            if (__instance == null || __instance != Player.m_localPlayer)
                return;

            ApplyJumpScale(ref vel.y);
        }

        private static void LevitationImpulsePrefix(ref Vector3 vel)
        {
            ApplyJumpScale(ref vel.y);
        }

        private static void ApplyJumpScale(ref float verticalVelocity)
        {
            try
            {
                if (!_executingWind)
                    return;

                Player player = Player.m_localPlayer;
                int quality = ValalaRings.GetBestEquippedQuality(player);
                if (quality <= 0)
                    return;

                float chargeMax = ChargeMaxForQuality(quality);
                float progress = Mathf.InverseLerp(1f, chargeMax, Fx.ChargeMult);
                float current = Mathf.Lerp(1f, 2.5f, progress);
                float target = Mathf.Lerp(1f, JumpMaxForQuality(quality), progress);
                if (current <= 0.001f)
                    return;

                verticalVelocity *= target / current;
            }
            catch (Exception e)
            {
                LogErrorOnce("Staff-free Wind Impulse jump scaling failed: " + e);
            }
        }

        private static bool FocusPrefix(ref bool __result)
        {
            if (!_bypassFocus)
                return true;

            __result = true;
            return false;
        }

        private static void UpdateStaffFreeCharge(Caster caster, Player player, Spell spell)
        {
            if (!ValalaModsPlugin.EnableStaffFreeWindImpulse.Value ||
                player == null ||
                player.IsDead() ||
                player.IsStaggering() ||
                player.InPlaceMode() ||
                IsInputBlocked() ||
                Focus.HasFocus(player) ||
                ValalaRings.GetBestEquippedQuality(player) <= 0)
            {
                CancelCharge(caster, player);
                return;
            }

            if (!IsCastKeyHeld())
            {
                FinishCharge(caster, player, spell);
                return;
            }

            int quality = ValalaRings.GetBestEquippedQuality(player);
            _chargeQuality = quality;

            float elapsed = Time.time - ChargeStart(caster);
            float maxCharge = ChargeMaxForQuality(quality);
            float target = Mathf.Min(maxCharge, 1f + Mathf.Max(0f, elapsed - 0.25f) * ChargeSpeed());
            float unitCost = Mathf.Max(Caster.ManaCost(player, spell) * ChargeManaCost(), 0.01f);
            float paid = ChargePaid(caster);
            float desired = (target - 1f) * unitCost;

            if (desired > paid)
            {
                float delta = desired - paid;
                if (player.GetEitr() - Caster.ManaCost(player, spell) >= delta)
                {
                    ((Character)player).UseEitr(delta);
                    paid += delta;
                    ChargePaid(caster) = paid;
                }
            }

            ChargeMultiplier(caster) = Mathf.Min(1f + paid / unitCost, maxCharge);

            if (elapsed >= 0.25f)
            {
                StartLoopAnim(caster, player);
                KeepLoopAnim(caster, player);
            }
        }

        private static void FinishCharge(Caster caster, Player player, Spell spell)
        {
            ChargePaid(caster) = 0f;
            _staffFreeCharging = false;
            SetChargingSpell(caster, null);
            StopLoopAnim(caster, player);

            _executingWind = true;
            try
            {
                ExecuteMethod.Invoke(caster, new object[] { player, spell });
            }
            catch (Exception e)
            {
                LogErrorOnce("Staff-free Wind Impulse release failed: " + e);
            }
            finally
            {
                _executingWind = false;
            }
        }

        private static void CancelCharge(Caster caster, Player player)
        {
            float paid = ChargePaid(caster);
            ChargePaid(caster) = 0f;
            ChargeMultiplier(caster) = 1f;
            _staffFreeCharging = false;
            SetChargingSpell(caster, null);
            StopLoopAnim(caster, player);

            if (player != null && paid > 0f)
                ((Character)player).AddEitr(paid);
        }

        private static void StartLoopAnim(Caster caster, Player player)
        {
            try
            {
                StartLoopAnimMethod.Invoke(caster, new object[] { player, null });
            }
            catch (Exception e)
            {
                LogErrorOnce("Staff-free Wind Impulse loop animation failed: " + e);
            }
        }

        private static void KeepLoopAnim(Caster caster, Player player)
        {
            try
            {
                KeepLoopAnimMethod.Invoke(caster, new object[] { player });
            }
            catch (Exception e)
            {
                LogErrorOnce("Staff-free Wind Impulse loop animation failed: " + e);
            }
        }

        private static void StopLoopAnim(Caster caster, Player player)
        {
            try
            {
                StopLoopAnimMethod.Invoke(caster, new object[] { player });
            }
            catch (Exception e)
            {
                LogErrorOnce("Staff-free Wind Impulse loop animation stop failed: " + e);
            }
        }

        private static Spell GetChargingSpell(Caster caster)
        {
            return caster == null ? null : ChargingSpellProperty.GetValue(caster, null) as Spell;
        }

        private static void SetChargingSpell(Caster caster, Spell spell)
        {
            if (caster != null)
                ChargingSpellProperty.SetValue(caster, spell, null);
        }

        private static float ChargeSpeed()
        {
            return ReadConfig(ChargeSpeedField, 0.4f);
        }

        private static float ChargeManaCost()
        {
            return ReadConfig(ChargeManaField, 6f);
        }

        private static float ReadConfig(FieldInfo field, float fallback)
        {
            return field != null && field.GetValue(null) is ConfigEntry<float> entry
                ? entry.Value
                : fallback;
        }

        private static float ChargeMaxForQuality(int quality)
        {
            return 1f + Mathf.Clamp(quality, 1, MaxRingQuality);
        }

        private static float JumpMaxForQuality(int quality)
        {
            return 2.5f + 0.5f * (Mathf.Clamp(quality, 1, MaxRingQuality) - 1);
        }

        private static Spell FindWindImpulse()
        {
            foreach (Spell spell in SpellBook.All)
            {
                if (spell.Id == WindImpulseId)
                    return spell;
            }

            return null;
        }

        private static bool IsCastKeyDown()
        {
            return TryParseKey(ValalaModsPlugin.StaffFreeCastKey.Value, out KeyCode key, out bool alt) &&
                   Input.GetKeyDown(key) &&
                   (!alt || IsAltHeld());
        }

        private static bool IsCastKeyHeld()
        {
            return TryParseKey(ValalaModsPlugin.StaffFreeCastKey.Value, out KeyCode key, out bool alt) &&
                   Input.GetKey(key) &&
                   (!alt || IsAltHeld());
        }

        private static bool TryParseKey(string value, out KeyCode key, out bool alt)
        {
            key = KeyCode.None;
            alt = false;

            if (string.IsNullOrEmpty(value))
                return false;

            string text = value.Trim();
            if (text.StartsWith("Alt+", StringComparison.OrdinalIgnoreCase))
            {
                alt = true;
                text = text.Substring(4);
            }

            return Enum.TryParse(text, true, out key) && key != KeyCode.None;
        }

        private static bool IsAltHeld()
        {
            return Input.GetKey(KeyCode.LeftAlt) ||
                   Input.GetKey(KeyCode.RightAlt) ||
                   Input.GetKey(KeyCode.AltGr);
        }

        private static bool IsInputBlocked()
        {
            if (MTKeys.Bloqueadas || ValheimConsole.IsVisible() || TextInput.IsVisible())
                return true;

            if (Chat.instance != null && Chat.instance.HasFocus())
                return true;

            return ElementSelectorUi.IsMenuVisible;
        }

        private static void LogErrorOnce(string message)
        {
            if (_errorLogged)
                return;

            _errorLogged = true;
            ValalaModsPlugin.Log.LogError(message);
        }
    }
}
