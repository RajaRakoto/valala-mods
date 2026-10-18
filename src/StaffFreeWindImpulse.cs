using System;
using System.Reflection;
using BepInEx.Configuration;
using ValheimConsole = global::Console;
using HarmonyLib;
using MT.Core;
using MT.Core.Habilidades;
using MT.Core.I18n;
using MT.Core.Mana;
using MT.Magia;
using UnityEngine;

namespace ValalaMods
{
    internal static class StaffFreeWindImpulse
    {
        private const string WindImpulseId = "impulso_viento";
        private const string NoFallStatus = "MT_ImpulsoViento";
        private const string DashStatus = "MT_ImpulsoCarrera";
        private const int MaxRingQuality = 4;
        private const float MaxChargeMultiplier = 5f;
        private const float MinChargeTime = 0.25f;

        private static readonly Type MagicPluginType = AccessTools.TypeByName("MT.Magia.MTMagiaPlugin");
        private static readonly FieldInfo ChargeSpeedField = MagicPluginType == null
            ? null
            : AccessTools.Field(MagicPluginType, "CargaVelocidad");
        private static readonly FieldInfo ChargeManaField = MagicPluginType == null
            ? null
            : AccessTools.Field(MagicPluginType, "CargaMana");
        private static readonly MethodInfo CanTakeInputMethod = AccessTools.Method(typeof(Caster), "CanTakeInput");

        private static bool _staffFreeCharging;
        private static float _chargeStart;
        private static float _chargePaid;
        private static float _chargeMult = 1f;
        private static int _chargeQuality;
        private static Spell _chargeSpell;
        private static Player _chargePlayer;
        private static bool _errorLogged;

        internal static bool IsCharging
        {
            get
            {
                Player player = Player.m_localPlayer;
                return _staffFreeCharging &&
                       player != null &&
                       _chargePlayer == player &&
                       !player.IsDead();
            }
        }

        internal static float ChargeFraction
        {
            get
            {
                return Mathf.Clamp01((_chargeMult - 1f) / (MaxChargeMultiplier - 1f));
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
                Type pasoType = AccessTools.TypeByName("MT.Paso.HabilidadPaso");
                MethodInfo tokiStep = pasoType == null ? null : AccessTools.Method(pasoType, "Usar");

                if (update == null)
                    throw new MissingMethodException("Required MT.Magia method Caster.Update was not found.");

                harmony.Patch(update,
                    postfix: new HarmonyMethod(typeof(StaffFreeWindImpulse), nameof(UpdatePostfix)));
                if (tokiStep != null)
                {
                    harmony.Patch(tokiStep,
                        prefix: new HarmonyMethod(typeof(StaffFreeWindImpulse), nameof(TokiStepPrefix)));
                }
            }
            catch (Exception e)
            {
                LogErrorOnce("Could not patch staff-free Wind Impulse: " + e);
            }
        }

        private static bool TokiStepPrefix(Player p, ref bool __result)
        {
            if (!ShouldBlockTokiStep(p))
                return true;

            __result = false;
            return false;
        }

        private static bool ShouldBlockTokiStep(Player player)
        {
            if (_staffFreeCharging)
                return true;

            if (!ValalaModsPlugin.Enabled.Value ||
                !ValalaModsPlugin.EnableStaffFreeWindImpulse.Value ||
                player == null ||
                player.IsDead() ||
                player.InPlaceMode() ||
                IsInputBlocked() ||
                Focus.HasFocus(player) ||
                ValalaRings.GetBestEquippedQuality(player) <= 0 ||
                !IsCastKeyHeld())
                return false;

            Spell spell = FindWindImpulse();
            return spell != null &&
                   Pergaminos.Knows(player, spell.Id) &&
                   Pergaminos.CanCast(player, spell);
        }

        private static void UpdatePostfix(Caster __instance)
        {
            try
            {
                if (__instance == null)
                {
                    if (_staffFreeCharging)
                        CancelCharge(Player.m_localPlayer);
                    return;
                }

                if (_staffFreeCharging)
                {
                    UpdateStaffFreeCharge();
                    return;
                }

                if (!ValalaModsPlugin.Enabled.Value ||
                    !ValalaModsPlugin.EnableStaffFreeWindImpulse.Value ||
                    __instance != Caster.Instance ||
                    IsInputBlocked() ||
                    !IsCastKeyDown())
                    return;

                TryStartCharge();
            }
            catch (Exception e)
            {
                CancelCharge(Player.m_localPlayer);
                LogErrorOnce("Staff-free Wind Impulse tick failed: " + e);
            }
        }

        private static void TryStartCharge()
        {
            Player player = Player.m_localPlayer;
            if (player == null ||
                player.IsDead() ||
                player.InPlaceMode() ||
                IsInputBlocked() ||
                Focus.HasFocus(player) ||
                Caster.IsActiveCasting ||
                Caster.SegundoActivo)
                return;

            int quality = ValalaRings.GetBestEquippedQuality(player);
            if (quality <= 0)
                return;

            Spell spell = FindWindImpulse();
            if (spell == null ||
                !Pergaminos.Knows(player, spell.Id) ||
                !Pergaminos.CanCast(player, spell))
                return;

            if (Agotamiento.IsExhausted(player))
            {
                ((Character)player).Message(
                    MessageHud.MessageType.TopLeft,
                    Tr.Get("magia_are_exhausted_can_t_cast"),
                    0,
                    null,
                    false);
                return;
            }

            float cooldown = Esperas.Restante(player, spell);
            if (cooldown > 0f)
            {
                ((Character)player).Message(
                    MessageHud.MessageType.TopLeft,
                    spell.Name + ": " + Mathf.CeilToInt(cooldown) + " s",
                    0,
                    null,
                    false);
                return;
            }

            string reason = null;
            if (!spell.Alcanza(player, out reason))
            {
                if (!string.IsNullOrEmpty(reason))
                    ((Character)player).Message(MessageHud.MessageType.TopLeft, reason, 0, null, false);
                return;
            }

            _staffFreeCharging = true;
            _chargeStart = Time.time;
            _chargePaid = 0f;
            _chargeMult = 1f;
            _chargeQuality = quality;
            _chargeSpell = spell;
            _chargePlayer = player;
        }

        private static void UpdateStaffFreeCharge()
        {
            Player player = Player.m_localPlayer;
            if (_chargePlayer != player)
            {
                CancelCharge(_chargePlayer);
                return;
            }
            if (!ValalaModsPlugin.Enabled.Value ||
                !ValalaModsPlugin.EnableStaffFreeWindImpulse.Value ||
                player == null ||
                player.IsDead() ||
                player.IsStaggering() ||
                player.InPlaceMode() ||
                player.IsSwimming() ||
                player.IsAttached() ||
                player.IsEncumbered() ||
                player.InDodge() ||
                IsInputBlocked() ||
                Focus.HasFocus(player) ||
                Caster.IsActiveCasting ||
                Caster.SegundoActivo ||
                _chargeSpell == null ||
                !Pergaminos.Knows(player, _chargeSpell.Id) ||
                !Pergaminos.CanCast(player, _chargeSpell))
            {
                CancelCharge(player);
                return;
            }

            int quality = ValalaRings.GetBestEquippedQuality(player);
            if (quality <= 0)
            {
                CancelCharge(player);
                return;
            }

            if (!IsCastKeyHeld())
            {
                FinishCharge(player);
                return;
            }

            _chargeQuality = quality;

            float elapsed = Time.time - _chargeStart;
            float maxCharge = ChargeMaxForQuality(quality);
            float target = Mathf.Min(
                maxCharge,
                1f + Mathf.Max(0f, elapsed - MinChargeTime) * ChargeSpeed());
            float unitCost = Mathf.Max(Caster.ManaCost(player, _chargeSpell) * ChargeManaCost(), 0.01f);
            float desired = (target - 1f) * unitCost;

            if (desired > _chargePaid)
            {
                float delta = desired - _chargePaid;
                if (player.GetEitr() - Caster.ManaCost(player, _chargeSpell) >= delta)
                {
                    ((Character)player).UseEitr(delta);
                    _chargePaid += delta;
                }
            }

            _chargeMult = Mathf.Min(1f + _chargePaid / unitCost, maxCharge);
        }

        private static void FinishCharge(Player player)
        {
            Spell spell = _chargeSpell;
            int quality = _chargeQuality;
            float charge = _chargeMult;
            float paid = _chargePaid;

            _staffFreeCharging = false;
            _chargeSpell = null;
            _chargeQuality = 0;
            _chargePlayer = null;
            _chargePaid = 0f;
            _chargeMult = 1f;

            if (player == null || spell == null || player.IsDead())
            {
                Refund(player, paid);
                return;
            }

            quality = ValalaRings.GetBestEquippedQuality(player);
            if (quality <= 0 ||
                !Pergaminos.Knows(player, spell.Id) ||
                !Pergaminos.CanCast(player, spell))
            {
                Refund(player, paid);
                return;
            }

            if (Esperas.Restante(player, spell) > 0f)
            {
                Refund(player, paid);
                return;
            }

            try
            {
                bool conToki;
                if (!spell.Pagar(player, out conToki))
                {
                    Refund(player, paid);
                    return;
                }
            }
            catch (Exception e)
            {
                Refund(player, paid);
                LogErrorOnce("Staff-free Wind Impulse payment failed: " + e);
                return;
            }

            try
            {
                UsoMagia.Lanzado(player, spell);
                Caster.PlayLaunchSound(player, spell);
                ((Character)player).RaiseSkill(
                    Escuelas.Skill(spell.School),
                    Mathf.Max(spell.Mana, 5f) / 10f);

                ApplyJump(player, quality, charge);
                ApplyLaunchStatuses(player);
                ValalaModsPlugin.TrySpawnElementStrike(player);
            }
            catch (Exception e)
            {
                LogErrorOnce("Staff-free Wind Impulse release failed: " + e);
            }
        }

        private static void ApplyLaunchStatuses(Player player)
        {
            SEMan seman = ((Character)player).GetSEMan();
            if (seman == null)
                return;

            int noFallHash = StringExtensionMethods.GetStableHashCode(NoFallStatus);
            seman.RemoveStatusEffect(noFallHash, false);
            seman.AddStatusEffect(noFallHash, true, 0, 0f, (short)(-1));

            Fx.AddSE((Character)player, DashStatus);
        }

        private static void ApplyJump(Player player, int quality, float charge)
        {
            Vector3 direction = GameCamera.instance != null
                ? GameCamera.instance.transform.forward
                : Fx.AimDir(player);

            float maxCharge = ChargeMaxForQuality(quality);
            float progress = Mathf.InverseLerp(1f, maxCharge, Mathf.Clamp(charge, 1f, maxCharge));
            float power = Mathf.Lerp(1f, JumpMaxForQuality(quality), progress);

            if (Levitacion.Active)
            {
                Levitacion.Impulsar(direction * 26f * power);
                return;
            }

            float vertical = direction.y * 26f * power + 4f;
            if (((Character)player).IsOnGround() && direction.y < 0.15f)
                vertical = Mathf.Max(vertical, 6f);

            ((Character)player).ForceJump(new Vector3(
                direction.x * 26f * power,
                vertical,
                direction.z * 26f * power), false);
            ImpulsoInercia.Lanzar(new Vector3(direction.x, 0f, direction.z) * 11f * power);
        }

        private static void CancelCharge(Player player)
        {
            float paid = _chargePaid;
            _staffFreeCharging = false;
            _chargeSpell = null;
            _chargeQuality = 0;
            _chargePlayer = null;
            _chargePaid = 0f;
            _chargeMult = 1f;

            Refund(player, paid);
        }

        private static void Refund(Player player, float paid)
        {
            if (player != null && paid > 0f)
                ((Character)player).AddEitr(paid);
        }

        private static float ReadConfig(FieldInfo field, float fallback)
        {
            return field != null && field.GetValue(null) is ConfigEntry<float> entry
                ? entry.Value
                : fallback;
        }

        private static float ChargeSpeed()
        {
            return ReadConfig(ChargeSpeedField, 0.4f);
        }

        private static float ChargeManaCost()
        {
            return ReadConfig(ChargeManaField, 6f);
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
            if (!Application.isFocused ||
                MTKeys.Bloqueadas ||
                Caster.MenuOpen ||
                ElementSelectorUi.IsMenuVisible)
                return true;

            try
            {
                if (CanTakeInputMethod != null &&
                    !(bool)CanTakeInputMethod.Invoke(null, null))
                    return true;
            }
            catch
            {
                // Fall through to the explicit checks below.
            }

            return ValheimConsole.IsVisible() ||
                   global::Menu.IsVisible() ||
                   InventoryGui.IsVisible() ||
                   TextInput.IsVisible() ||
                   Minimap.IsOpen() ||
                   (Chat.instance != null && Chat.instance.HasFocus());
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
