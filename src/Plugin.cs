using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MT.Core;
using MT.Magia;
using ValalaMods.NorseFx;
using UnityEngine;
using UnityEngine.Rendering;

namespace ValalaMods
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("mushokutensei.magia", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("randyknapp.mods.epicloot", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.jotunn.jotunn", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.kewb.valheimmjolnir", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("shudnal.ExtraSlots", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("shudnal.ExtraSlotsCustomSlots", BepInDependency.DependencyFlags.SoftDependency)]
    public class ValalaModsPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "odin.valala.raja.mods";
        public const string PluginName = "Valala Mods";
        public const string PluginVersion = "1.5.1";
        private const int CastLandingDamageMinQuality = 3;

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<string> Mode;
        internal static ConfigEntry<float> Duration;
        internal static ConfigEntry<float> Radius;
        internal static ConfigEntry<float> Height;
        internal static ConfigEntry<string> ColorHex;
        internal static ConfigEntry<bool> AffectMk1Jump;
        internal static ConfigEntry<bool> Damage;
        internal static ConfigEntry<float> DamageMin;
        internal static ConfigEntry<float> DamageMax;
        internal static ConfigEntry<bool> SyncToOthers;
        internal static ConfigEntry<string> MenuKey;
        internal static ConfigEntry<string> SelectedElement;
        internal static ConfigEntry<float> RingDamageBonus;
        internal static ConfigEntry<float> RingDamageBonusPerLevel;
        internal static ConfigEntry<bool> EnableSecondTrinketSlot;
        internal static ConfigEntry<bool> EnableStaffFreeWindImpulse;
        internal static ConfigEntry<string> StaffFreeCastKey;
        internal static ConfigEntry<bool> EnableNorseFx;
        internal static ConfigEntry<bool> LandingBoom;
        internal static ConfigEntry<float> LandingHeight;
        internal static ConfigEntry<float> LandingRadius;
        internal static ConfigEntry<bool> LandingThunder;
        internal static ConfigEntry<float> LandingThunderHeight;
        internal static ConfigEntry<float> LandingThunderRadius;
        internal static ConfigEntry<bool> MjolnirImpactEnabled;
        internal static ConfigEntry<bool> MjolnirImpactVfx;
        internal static ConfigEntry<bool> MjolnirImpactDamageEnabled;
        internal static ConfigEntry<float> MjolnirImpactRadius;
        internal static ConfigEntry<float> MjolnirImpactDamageAmount;
        internal static ConfigEntry<float> MjolnirImpactMinSpeed;
        internal static ConfigEntry<bool> MjolnirImpactStructures;
        internal static ConfigEntry<bool> MjolnirImpactPlayerStructures;
        internal static ConfigEntry<float> MjolnirImpactCooldown;
        internal static ConfigEntry<float> MjolnirHudRightMargin;
        internal static ConfigEntry<float> MjolnirHudBottomMargin;
        internal static ConfigEntry<bool> MjolnirMenuEnabled;
        internal static ConfigEntry<KeyboardShortcut> MjolnirMenuOpenKey;
        internal static KeyCode MenuKeyCode = KeyCode.Semicolon;
        internal static ManualLogSource Log;
        internal static ValalaModsPlugin Instance;
        internal static bool IsHeadless { get; private set; }

        // Interchangeable presets shown as a dropdown in the Gale / BepInEx config editor.
        private static readonly string[] MenuKeyPresets =
        {
            "Semicolon", "F7", "F8", "F9", "F10",
            "B", "N", "J", "K", "L",
            "LeftBracket", "RightBracket", "Minus", "Equals", "BackQuote", "Quote"
        };

        private static readonly string[] StaffFreeCastKeyPresets =
        {
            "Alt+E", "Alt+R", "Alt+F", "Alt+Q", "Alt+T", "Alt+G", "Alt+V", "Alt+C",
            "E", "R", "F", "Q", "T", "G", "V", "C"
        };

        private Harmony _harmony;
        private FileSystemWatcher _configWatcher;
        private volatile bool _pendingReload;
        private float _reloadDelay;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            IsHeadless = Application.isBatchMode ||
                         SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

            try
            {
                if (BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(
                        "com.kewb.valheimmjolnir", out BepInEx.PluginInfo mjolnir))
                {
                    Log.LogInfo("Bundled Mjolnir active: v" + mjolnir.Metadata.Version +
                                " (" + mjolnir.Location + ")");

                    string mjolnirConfigPath = mjolnir.Instance?.Config?.ConfigFilePath;
                    if (!string.IsNullOrEmpty(mjolnirConfigPath))
                    {
                        Log.LogInfo("Mjolnir config: " + mjolnirConfigPath);
                        if (!string.Equals(Path.GetFileName(mjolnirConfigPath),
                                ValalaConfigRedirectPlugin.NewFileName,
                                StringComparison.Ordinal))
                        {
                            Log.LogWarning("Mjolnir config path redirect did not apply.");
                        }
                    }

                    ApplyBundledMjolnirDefaults(mjolnir.Instance?.Config);
                }
            }
            catch (Exception e)
            {
                Log.LogWarning("Could not verify bundled Mjolnir: " + e.Message);
            }

            Enabled = Config.Bind("General", "Enabled", true,
                "Add or replace an elemental VFX on the Wind Impulse / Wind Boost dash.");
            Mode = Config.Bind("General", "Mode", "Add",
                new ConfigDescription("Add = keep the wind VFX and overlay the selected element. Replace = selected element only when its ring is equipped. Staff-free casts always use the Valala VFX.",
                    new AcceptableValueList<string>("Add", "Replace")));
            Duration = Config.Bind("General", "Duration", 1.5f,
                "Lifetime of the single elemental strike VFX in seconds.");
            Radius = Config.Bind("General", "Radius", 0.5f,
                "Blast radius for the strike visuals and damage.");
            Height = Config.Bind("General", "Height", 1.8f,
                "Elemental VFX height factor.");
            ColorHex = Config.Bind("General", "Color", "59BFFF",
                "Lightning tint as hex RRGGBB. Other elements use their own tint.");
            AffectMk1Jump = Config.Bind("General", "AffectMk1Jump", false,
                "Also trigger the strike (and damage) on the Mk-I armor jump (which shares the same VFX call).");
            Damage = Config.Bind("General", "Damage", true,
                "Deal elemental damage to hostile enemies only (never to you, other players, tames or NPCs).");
            DamageMin = Config.Bind("General", "DamageMin", 8f,
                "Elemental damage at Elemental Magic skill level 0.");
            DamageMax = Config.Bind("General", "DamageMax", 60f,
                "Elemental damage at Elemental Magic skill level 100.");
            SyncToOthers = Config.Bind("General", "SyncToOthers", true,
                "Broadcast the strike to every player in range (client-side RPC). Off = only you see it.");
            MenuKey = Config.Bind("General", "MenuKey", "Semicolon",
                new ConfigDescription("Key that toggles the element class menu (preset dropdown).",
                    new AcceptableValueList<string>(MenuKeyPresets)));
            SelectedElement = Config.Bind("General", "SelectedElement", "Foudre",
                new ConfigDescription("Selected element class (persisted).",
                    new AcceptableValueList<string>("Foudre", "Roche", "Feu", "Poison", "Glace")));
            RingDamageBonus = Config.Bind("Rings", "DamageBonus", 0.15f,
                new ConfigDescription(
                    "Damage bonus applied when the matching Valala ring is equipped. 0.15 = +15%.",
                    new AcceptableValueRange<float>(0f, 1f)));
            RingDamageBonusPerLevel = Config.Bind("Rings", "DamageBonusPerLevel", 0.15f,
                new ConfigDescription(
                    "Additional damage bonus per ring quality above 1. 0.15 = +15% per level; quality 4 gives +60% total.",
                    new AcceptableValueRange<float>(0f, 0.5f)));
            EnableSecondTrinketSlot = Config.Bind("Rings", "SecondTrinketSlot", true,
                "Enable a second trinket slot through ExtraSlotsCustomSlots. Set ExtraSlotsCustomSlots custom slot 1 Enabled=true.");
            EnableStaffFreeWindImpulse = Config.Bind("Rings", "StaffFreeWindImpulse", true,
                "Cast Wind Impulse without a staff using any Valala ring quality. Own charge/jump system with free movement and no charge animation.");
            StaffFreeCastKey = Config.Bind("Rings", "StaffFreeCastKey", "Alt+F",
                new ConfigDescription(
                    "Shortcut for staff-free Wind Impulse. Alt+F keeps plain F free for MT Toki Step.",
                    new AcceptableValueList<string>(StaffFreeCastKeyPresets)));
            LandingBoom = Config.Bind("General", "LandingBoom", true,
                "Spawn an elemental landing impact when landing after a fall (requires the matching ring; non-Lightning impacts have no light flash).");
            LandingHeight = Config.Bind("General", "LandingHeight", 10f,
                new ConfigDescription(
                    "Minimum fall height for the landing impact, meters. Vanilla fall damage: 10 m ~= 37.5, 20 m = 100.",
                    new AcceptableValueRange<float>(4f, 50f)));
            LandingRadius = Config.Bind("General", "LandingRadius", 4f,
                new ConfigDescription(
                    "Elemental landing and q3/q4 cast damage radius, meters. Damage uses DamageMin/DamageMax, the ElementalMagic skill and the ring bonus.",
                    new AcceptableValueRange<float>(1f, 10f)));
            LandingThunder = Config.Bind("General", "LandingThunder", true,
                "Spawn the high-fall lightning VFX on landing from a higher fall (Lightning class only).");
            LandingThunderHeight = Config.Bind("General", "LandingThunderHeight", 20f,
                new ConfigDescription(
                    "Minimum fall height for the high-fall lightning VFX and damage, meters. Effective threshold is at least LandingHeight.",
                    new AcceptableValueRange<float>(10f, 80f)));
            LandingThunderRadius = Config.Bind("General", "LandingThunderRadius", 4f,
                new ConfigDescription(
                    "High-fall lightning damage radius, meters. Damage uses DamageMin/DamageMax and the ElementalMagic skill.",
                    new AcceptableValueRange<float>(1f, 10f)));
            MjolnirImpactEnabled = Config.Bind("Mjolnir Impact", "Enabled", true,
                "Add a lightning strike and area damage when a thrown Mjolnir hits terrain, a structure or an enemy.");
            MjolnirImpactVfx = Config.Bind("Mjolnir Impact", "LightningVfx", true,
                "Spawn the lightning strike VFX on terrain and structure impacts. Enemy impacts keep Mjolnir's native bolt.");
            MjolnirImpactDamageEnabled = Config.Bind("Mjolnir Impact", "Damage", true,
                "Deal area damage around the impact point.");
            MjolnirImpactRadius = Config.Bind("Mjolnir Impact", "Radius", 8f,
                new ConfigDescription(
                    "Area damage and lightning VFX radius, meters.",
                    new AcceptableValueRange<float>(1f, 15f)));
            MjolnirImpactDamageAmount = Config.Bind("Mjolnir Impact", "DamageAmount", 250f,
                new ConfigDescription(
                    "Flat lightning damage dealt to each enemy and structure in the radius.",
                    new AcceptableValueRange<float>(0f, 600f)));
            MjolnirImpactMinSpeed = Config.Bind("Mjolnir Impact", "MinImpactSpeed", 8f,
                new ConfigDescription(
                    "Minimum hammer speed at impact for the effect to trigger, m/s.",
                    new AcceptableValueRange<float>(0f, 80f)));
            MjolnirImpactStructures = Config.Bind("Mjolnir Impact", "DamageStructures", true,
                "Damage structures in the impact radius.");
            MjolnirImpactPlayerStructures = Config.Bind("Mjolnir Impact", "DamagePlayerStructures", true,
                "Allow damage to player-built pieces when the local ward access check permits it.");
            MjolnirImpactCooldown = Config.Bind("Mjolnir Impact", "Cooldown", 0.4f,
                new ConfigDescription(
                    "Minimum seconds between two impact effects.",
                    new AcceptableValueRange<float>(0.1f, 2f)));
            MjolnirHudRightMargin = Config.Bind("Mjolnir HUD", "RightMargin", 12f,
                new ConfigDescription(
                    "Distance in pixels between Mjolnir's ability panel and the right screen edge.",
                    new AcceptableValueRange<float>(0f, 200f)));
            MjolnirHudBottomMargin = Config.Bind("Mjolnir HUD", "BottomMargin", 72f,
                new ConfigDescription(
                    "Distance in pixels between Mjolnir's ability panel and the bottom screen edge.",
                    new AcceptableValueRange<float>(0f, 200f)));
            MjolnirMenuEnabled = Config.Bind("Mjolnir Menu", "Enabled", false,
                "Allow opening Mjolnir's in-game settings menu. Disabled by default.");
            MjolnirMenuOpenKey = Config.Bind("Mjolnir Menu", "OpenKey",
                new KeyboardShortcut(KeyCode.F10, KeyCode.LeftControl, KeyCode.LeftAlt),
                "Keyboard shortcut that opens Mjolnir's settings menu when Enabled is true. Mjolnir's F4 binding is ignored.");

            MigrateValalaMjolnirDefaults();
            MigrateSelectedElement();
            if (!IsHeadless)
                MigrateStaffFreeCastKey();
            RefreshMenuKeyCode();
            RefreshSelectedElement();
            MenuKey.SettingChanged += (_, _) => RefreshMenuKeyCode();
            SelectedElement.SettingChanged += (_, _) => RefreshSelectedElement();
            ValalaRings.Register();
            MjolnirRecipeOverride.Register();
            ValalaConsoleCommands.Register();
            ValidateElementRegistry();

            if (NorseFxCatalog.Count > 0)
            {
                EnableNorseFx = Config.Bind("Norse FX", "Enabled", true,
                    "Use the selected VFX/SFX from the local norsefx.bundle. Vanilla fallback when the bundle is missing.");
            }

            SetupConfigWatcher();
            if (!IsHeadless)
            {
                CreateElementMenu();
                CreateWindChargeHud();
            }

            _harmony = new Harmony(PluginGuid);
            try
            {
                _harmony.PatchAll(typeof(WindImpulsePatch));
                _harmony.PatchAll(typeof(ZNetAwakePatch));
                _harmony.PatchAll(typeof(LandingBoomPatch));
                MjolnirImpactPatch.Apply(_harmony);
                SecondTrinketSlot.Apply(_harmony);
                if (!IsHeadless)
                {
                    _harmony.PatchAll(typeof(CursorCapturePatch));
                    _harmony.PatchAll(typeof(CameraLookPatch));
                    StaffFreeWindImpulse.Initialize(_harmony);
                    MjolnirHudPosition.Apply(_harmony);
                    MjolnirMenuGuard.Apply(_harmony);
                }
                Log.LogInfo(PluginName + " loaded: patched WindImpulseVfx.Spawn (Mode=" + Mode.Value + ", rings + elemental VFX), landing effects" +
                    (IsHeadless
                        ? "; headless server mode (client input/UI disabled)."
                        : " and staff-free Wind Impulse (own charge/jump)."));
            }
            catch (Exception e)
            {
                Log.LogError("Could not install Harmony patches: " + e);
            }

            if (NorseFxCatalog.Count > 0)
            {
                Log.LogInfo("Norse FX module: " + NorseFxCatalog.Count + " selected effect(s), bundle " +
                    (NorseFxResolver.BundlePresent ? "found." : "missing (vanilla fallback)."));
            }
        }

        private static void CreateElementMenu()
        {
            try
            {
                GameObject host = new GameObject("Valala_ElementMenuHost");
                UnityEngine.Object.DontDestroyOnLoad(host);
                host.AddComponent<ElementSelectorUi>();
            }
            catch (Exception e)
            {
                Log.LogError("Could not create the element menu: " + e);
            }
        }

        private static void CreateWindChargeHud()
        {
            try
            {
                GameObject host = new GameObject("Valala_WindChargeHud");
                UnityEngine.Object.DontDestroyOnLoad(host);
                host.AddComponent<WindChargeHud>();
            }
            catch (Exception e)
            {
                Log.LogError("Could not create the Wind Impulse charge HUD: " + e);
            }
        }

        internal static bool IsMenuKeyDown()
        {
            return !IsHeadless && Input.GetKeyDown(MenuKeyCode);
        }

        private static void RefreshMenuKeyCode()
        {
            MenuKeyCode = MenuKey != null && Enum.TryParse(MenuKey.Value, true, out KeyCode parsed)
                ? parsed
                : KeyCode.Semicolon;
        }

        private static void RefreshSelectedElement()
        {
            ElementRegistry.Current = ElementRegistry.Parse(SelectedElement.Value);
        }

        private static void MigrateSelectedElement()
        {
            string value = SelectedElement.Value;
            if (!string.Equals(value, "Vent", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(value, "Wind", StringComparison.OrdinalIgnoreCase))
                return;

            SelectedElement.Value = "Roche";
            Log.LogInfo("Migrated SelectedElement from Vent/Wind to Roche.");
        }

        private static void ApplyBundledMjolnirDefaults(ConfigFile config)
        {
            if (config == null)
                return;

            bool changed = false;
            changed |= ApplyBundledMjolnirDefault(
                config, "Presentation", "CustomModelFlip", false);
            changed |= ApplyBundledMjolnirDefault(
                config, "TerrainImpact", "CratersEnabled", true);
            changed |= ApplyBundledMjolnirDefault(
                config, "TerrainImpact", "MaximumRadius", 10f);
            changed |= ApplyBundledMjolnirDefault(
                config, "TerrainImpact", "MaximumDepth", 6f);
            changed |= ApplyBundledMjolnirDefault(
                config, "TerrainImpact", "DistanceForMaximumContribution", 250f);
            changed |= ApplyBundledMjolnirDefault(
                config, "TerrainImpact", "FallAltitudeForMaximumContribution", 40f);
            changed |= ApplyBundledMjolnirDefault(
                config, "Throwing", "ImpactDamage", 250f);

            if (changed)
            {
                config.Save();
                Log.LogInfo("Applied Valala's shipped Mjolnir defaults.");
            }
        }

        private static bool ApplyBundledMjolnirDefault<T>(
            ConfigFile config, string section, string key, T value)
        {
            if (config == null)
                return false;

            if (!config.TryGetEntry<T>(section, key, out ConfigEntry<T> entry))
            {
                Log.LogWarning(
                    "Mjolnir config entry missing; default not applied: " +
                    section + "." + key);
                return false;
            }

            // Only touch untouched entries. This also works when a future bundled
            // Mjolnir version changes its own defaults.
            if (!object.Equals(entry.Value, entry.DefaultValue) ||
                object.Equals(entry.Value, value))
                return false;

            entry.Value = value;
            return true;
        }

        private static void MigrateValalaMjolnirDefaults()
        {
            bool changed = false;
            changed |= MigrateDefault(MjolnirImpactRadius, 5f, 8f);
            changed |= MigrateDefault(MjolnirImpactDamageAmount, 120f, 250f);
            changed |= MigrateDefault(MjolnirImpactMinSpeed, 12f, 8f);
            changed |= MigrateDefault(MjolnirHudBottomMargin, 24f, 72f);

            if (changed)
            {
                Instance.Config.Save();
                Log.LogInfo("Applied Valala's shipped Mjolnir impact and HUD defaults.");
            }
        }

        private static bool MigrateDefault<T>(
            ConfigEntry<T> entry, T previous, T current)
        {
            if (entry == null || !EqualityComparer<T>.Default.Equals(entry.Value, previous))
                return false;

            entry.Value = current;
            return true;
        }

        private void MigrateStaffFreeCastKey()
        {
            try
            {
                if (!EnableStaffFreeWindImpulse.Value)
                    return;

                if (!Enum.TryParse(StaffFreeCastKey.Value, true, out KeyCode key) || key == KeyCode.None)
                    return;

                bool conflicts = key == KeyCode.F ||
                    (MTKeys.Paso != null && key == MTKeys.Paso.Value);
                if (!conflicts)
                    return;

                StaffFreeCastKey.Value = "Alt+F";
                Config.Save();
                Log.LogWarning("StaffFreeCastKey " + key + " conflicts with MT Toki Step; migrated to Alt+F.");
            }
            catch (Exception e)
            {
                Log.LogWarning("Could not migrate StaffFreeCastKey: " + e.Message);
            }
        }

        private static void ValidateElementRegistry()
        {
            for (int i = 0; i < ElementRegistry.All.Length; i++)
            {
                if ((int)ElementRegistry.All[i].Id != i)
                    Log.LogError("Element registry order mismatch at index " + i + ": " + ElementRegistry.All[i].Id);
            }
        }

        // Watches the .cfg so edits made in Gale (or any editor) apply without restarting the game.
        private void SetupConfigWatcher()
        {
            try
            {
                string path = Config.ConfigFilePath;
                if (string.IsNullOrEmpty(path))
                    return;

                _configWatcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path))
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size |
                                   NotifyFilters.CreationTime | NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };
                _configWatcher.Changed += OnConfigFileChanged;
                _configWatcher.Created += OnConfigFileChanged;
                _configWatcher.Renamed += OnConfigFileChanged;
            }
            catch (Exception e)
            {
                Log.LogWarning("Config hot-reload unavailable: " + e.Message);
            }
        }

        private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            _pendingReload = true; // raised off-thread; applied on the main thread in Update
        }

        private void Update()
        {
            if (_pendingReload)
            {
                _pendingReload = false;
                _reloadDelay = 0.35f; // debounce so a partial write is not read
                return;
            }

            if (_reloadDelay <= 0f)
                return;

            _reloadDelay -= Time.unscaledDeltaTime;
            if (_reloadDelay > 0f)
                return;

            try
            {
                Config.Reload();
                RefreshMenuKeyCode();
                RefreshSelectedElement();
                Log.LogInfo("Config hot-reloaded (MenuKey=" + MenuKey.Value + ").");
            }
            catch (Exception e)
            {
                Log.LogWarning("Config reload failed: " + e.Message);
            }
        }

        private void OnDestroy()
        {
            MjolnirRecipeOverride.Unregister();
            NorseFxResolver.Unload();

            if (_configWatcher == null)
                return;

            _configWatcher.EnableRaisingEvents = false;
            _configWatcher.Dispose();
            _configWatcher = null;
        }

        internal static bool TrySpawnElementStrike(Player player)
        {
            if (player == null)
                return false;

            if (!Enabled.Value)
                return false;

            bool fromMk1Jump = IsFromMk1Jump();
            if (!AffectMk1Jump.Value && fromMk1Jump)
                return false;

            try
            {
                Element element = ElementRegistry.Current;
                ElementDef def = ElementRegistry.Get(element);
                if (!def.Implemented || !ValalaRings.IsEquipped(player, element))
                    return false;

                Color color = element == Element.Lightning ? ParseColor(ColorHex.Value) : def.Tint;

                RpcSync.Broadcast(element, player.transform.position, Duration.Value, Radius.Value, Height.Value, color);

                if (Damage.Value)
                {
                    bool castLandingPulse = !fromMk1Jump &&
                        ValalaRings.GetEquippedQuality(player, element) >= CastLandingDamageMinQuality;

                    if (castLandingPulse)
                    {
                        DamageEnemiesAt(
                            player,
                            player.transform.position,
                            LandingRadius.Value,
                            element);
                    }
                    else
                    {
                        DamageEnemies(player, Radius.Value, element);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Log.LogError("TrySpawnElementStrike failed: " + e);
                return false;
            }
        }

        private static void DamageEnemies(Player player, float radius, Element element)
        {
            DamageEnemiesAt(player, player.transform.position, radius, element);
        }

        internal static void DamageEnemiesAt(Player player, Vector3 center, float radius, Element element)
        {
            if (player == null || !ValalaRings.IsEquipped(player, element))
                return;

            ElementDef def = ElementRegistry.Get(element);
            float level = player.GetSkillLevel(Skills.SkillType.ElementalMagic);
            float damage = Mathf.Lerp(DamageMin.Value, DamageMax.Value, Mathf.Clamp01(level / 100f));
            damage *= ValalaRings.GetDamageMultiplier(player, element);
            DamageEnemiesAtCore(player, center, radius, def, damage, level);
        }

        // Flat-damage path used by the Mjolnir impact: no Valala ring required.
        internal static void DamageEnemiesWithLightning(
            Player player, Vector3 center, float radius, float damage)
        {
            if (player == null)
                return;

            float level = player.GetSkillLevel(Skills.SkillType.ElementalMagic);
            DamageEnemiesAtCore(
                player, center, radius, ElementRegistry.Get(Element.Lightning), damage, level);
        }

        private static void DamageEnemiesAtCore(
            Player player, Vector3 center, float radius, ElementDef def, float damage, float level)
        {
            float radiusSqr = radius * radius;

            // Snapshot: damage may be applied immediately on host/SP and mutate the live list.
            foreach (Character target in new List<Character>(Character.GetAllCharacters()))
            {
                if (target == null || target == player || target.IsDead())
                    continue;

                // Strict "enemies only": never the caster, other players, tames or NPCs.
                if (target.IsPlayer() || target.IsTamed())
                    continue;

                if (target.GetComponent<Trader>() != null)
                    continue;

                if (!BaseAI.IsEnemy(player, target))
                    continue;

                Vector3 offset = target.transform.position - center;
                if (offset.sqrMagnitude > radiusSqr)
                    continue;

                HitData hit = new HitData();
                switch (def.Damage)
                {
                    case ElementDamage.Blunt:
                        hit.m_damage.m_blunt = damage;
                        break;
                    case ElementDamage.Fire:
                        hit.m_damage.m_fire = damage;
                        break;
                    case ElementDamage.Poison:
                        hit.m_damage.m_poison = damage;
                        break;
                    case ElementDamage.Frost:
                        hit.m_damage.m_frost = damage;
                        break;
                    default:
                        hit.m_damage.m_lightning = damage;
                        break;
                }

                hit.m_staggerMultiplier = def.StaggerMultiplier;
                hit.m_point = target.GetCenterPoint();
                hit.m_dir = offset.normalized;
                hit.m_skill = Skills.SkillType.ElementalMagic;
                hit.m_skillLevel = level;
                hit.SetAttacker(player);
                target.Damage(hit);
            }
        }

        private static bool IsFromMk1Jump()
        {
            try
            {
                var st = new System.Diagnostics.StackTrace(false);
                for (int i = 0; i < st.FrameCount; i++)
                {
                    var dt = st.GetFrame(i)?.GetMethod()?.DeclaringType;
                    if (dt != null && dt.Name == "Mk1SaltoPatch")
                        return true;
                }
            }
            catch
            {
            }

            return false;
        }

        internal static Color ParseColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                hex = "59BFFF";
            hex = hex.TrimStart('#');

            try
            {
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                return new Color(r / 255f, g / 255f, b / 255f, 1f);
            }
            catch
            {
                return new Color(0.35f, 0.75f, 1f, 1f);
            }
        }
    }

    // One-shot vanilla elemental strike: bolt, spark burst, inert ground AOE and a light flash.
    // Prefabs are resolved at runtime from the element's candidate names (first match wins).
    internal static class ElementVfx
    {
        private static readonly HashSet<string> MissingWarned = new HashSet<string>();
        private static readonly string[] LandingWavePrefabs = { "vfx_sledge_iron_hit" };

        public static void Spawn(ElementDef def, Vector3 pos, float duration, float radius, float height, Color color)
        {
            if (!string.IsNullOrEmpty(def.NorseEffectId))
            {
                GameObject norse = NorseFxService.SpawnVfx(def.NorseEffectId, pos, Quaternion.identity, null, duration);
                if (norse != null)
                {
                    if (!string.IsNullOrEmpty(def.NorseSfxId))
                        NorseFxService.PlaySfx(def.NorseSfxId, pos);

                    return;
                }
            }

            float life = Mathf.Max(0.5f, duration);
            float scale = Mathf.Clamp(0.8f + radius * 0.6f + height * 0.4f, 0.8f, 4f);

            SpawnLocal(def.BoltPrefabs, pos, scale, life, color);
            SpawnLocal(def.SparkPrefabs, pos, 1f, Mathf.Min(life, 2.5f), color);
            SpawnLocal(def.AoePrefabs, pos, Mathf.Clamp(radius, 0.5f, 3f), Mathf.Min(life, 2.5f), color);
            SpawnFlash(pos, color, radius);
        }

        public static void SpawnLanding(ElementDef def, Vector3 pos, float duration, float radius, Color color)
        {
            float life = Mathf.Clamp(duration, 0.5f, 2.5f);
            float waveScale = Mathf.Clamp(radius / 4f, 0.9f, 1.6f);

            SpawnLocal(LandingWavePrefabs, pos, waveScale, 5f, color, true);
            SpawnLocal(def.LandingAoePrefabs, pos, Mathf.Clamp(radius, 0.5f, 3f), life, color, true);
            SpawnLocal(def.SparkPrefabs, pos, 1f, life, color, true);
        }

        private static void SpawnFlash(Vector3 pos, Color color, float radius)
        {
            GameObject go = new GameObject("Valala_ElementFlash");
            go.transform.position = pos + Vector3.up * 1.2f;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = Mathf.Max(6f, radius * 8f);
            light.intensity = 4f;
            UnityEngine.Object.Destroy(go, 0.25f);
        }

        private static GameObject SpawnLocal(string[] candidates, Vector3 pos, float scale, float lifetime, Color color, bool disableLights = false)
        {
            GameObject prefab = ResolvePrefab(candidates);
            if (prefab == null)
                return null;

            bool prev = ZNetView.m_forceDisableInit;
            ZNetView.m_forceDisableInit = true;
            GameObject go;
            try
            {
                go = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);
            }
            finally
            {
                ZNetView.m_forceDisableInit = prev;
            }

            go.transform.localScale *= scale;
            Neutralize(go);
            if (disableLights)
                DisableParticleLights(go);
            Tint(go, color);
            UnityEngine.Object.Destroy(go, lifetime);
            return go;
        }

        private static void DisableParticleLights(GameObject go)
        {
            ParticleSystem[] particles = go.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem.LightsModule lights = particles[i].lights;
                if (lights.enabled)
                    lights.enabled = false;
            }
        }

        private static GameObject ResolvePrefab(string[] candidates)
        {
            if (candidates == null || candidates.Length == 0 || ZNetScene.instance == null)
                return null;

            foreach (string candidate in candidates)
            {
                GameObject prefab = ZNetScene.instance.GetPrefab(candidate);
                if (prefab != null)
                    return prefab;
            }

            string key = string.Join(", ", candidates);
            if (MissingWarned.Add(key))
                ValalaModsPlugin.Log.LogWarning("Vanilla VFX not found: " + key);
            return null;
        }

        // Strip every script, collider and rigidbody so the vanilla prefab is a pure visual:
        // it can never damage or affect anyone.
        // WindImpulseVfx.Spawn runs inside an animation-event callback, where Unity forbids
        // DestroyImmediate ("Destroying components immediately is not permitted during ...
        // animation event callbacks. You must use Destroy instead."): the call is rejected and
        // the components survive, so the prefab's Aoe would still tick damage on the caster.
        // We therefore DISABLE first (stops Update/OnTriggerStay the very same frame) then
        // Destroy (deferred, always allowed).
        private static void Neutralize(GameObject go)
        {
            Behaviour[] behaviours = go.GetComponentsInChildren<Behaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                try
                {
                    behaviours[i].enabled = false;
                    UnityEngine.Object.Destroy(behaviours[i]);
                }
                catch (Exception e)
                {
                    LogNeutralizeOnce(e);
                }
            }

            Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                try
                {
                    colliders[i].enabled = false;
                    UnityEngine.Object.Destroy(colliders[i]);
                }
                catch (Exception e)
                {
                    LogNeutralizeOnce(e);
                }
            }

            Rigidbody[] bodies = go.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                try
                {
                    bodies[i].isKinematic = true;
                    UnityEngine.Object.Destroy(bodies[i]);
                }
                catch (Exception e)
                {
                    LogNeutralizeOnce(e);
                }
            }
        }

        private static bool _neutralizeErrorLogged;

        private static void LogNeutralizeOnce(Exception e)
        {
            if (_neutralizeErrorLogged)
                return;

            _neutralizeErrorLogged = true;
            ValalaModsPlugin.Log.LogWarning("Neutralize error (reported once): " + e.Message);
        }

        private static void Tint(GameObject go, Color color)
        {
            ParticleSystem[] particles = go.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem.MainModule main = particles[i].main;
                main.startColor = color;
            }

            TrailRenderer[] trails = go.GetComponentsInChildren<TrailRenderer>(true);
            for (int i = 0; i < trails.Length; i++)
                trails[i].startColor = color;

            LineRenderer[] lines = go.GetComponentsInChildren<LineRenderer>(true);
            for (int i = 0; i < lines.Length; i++)
                lines[i].startColor = color;

            Light[] lights = go.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
                lights[i].color = color;
        }
    }

    // Broadcasts the strike to every client through a vanilla routed RPC, so all players see it.
    // The RPC is global (no ZDO): each receiver spawns the same inert local VFX.
    internal static class RpcSync
    {
        private const string RpcSpawnVfx = "odin.valala.raja.mods.spawn";
        private const string RpcLanding = "odin.valala.raja.mods.landing";

        // Called on every ZNet.Awake: a fresh ZRoutedRpc is created per session, so the
        // handler must be (re)registered on all clients AND on the server (to avoid
        // "Failed to find rpc method" warnings while relaying).
        public static void EnsureRegistered()
        {
            if (ZRoutedRpc.instance == null)
                return;

            try
            {
                ZRoutedRpc.instance.Register<int, Vector3, Vector3, float, float, float>(RpcSpawnVfx, OnSpawnVfx);
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log.LogWarning("VFX RPC already registered: " + e.Message);
            }

            try
            {
                ZRoutedRpc.instance.Register<int, Vector3, float>(RpcLanding, OnLanding);
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log.LogWarning("Landing RPC already registered: " + e.Message);
            }
        }

        public static void Broadcast(Element element, Vector3 pos, float duration, float radius, float height, Color color)
        {
            if (!ValalaModsPlugin.SyncToOthers.Value || ZRoutedRpc.instance == null)
            {
                ElementFx.Spawn(element, pos, duration, radius, height, color);
                return;
            }

            try
            {
                // Everybody (0) = handle locally then relay to all peers through the server.
                ZRoutedRpc.instance.InvokeRoutedRPC(
                    ZRoutedRpc.Everybody, RpcSpawnVfx,
                    (int)element, pos, new Vector3(color.r, color.g, color.b), duration, radius, height);
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log.LogError("VFX RPC failed, spawning locally: " + e);
                ElementFx.Spawn(element, pos, duration, radius, height, color);
            }
        }

        private static void OnSpawnVfx(long sender, int element, Vector3 pos, Vector3 color, float duration, float radius, float height)
        {
            // Headless dedicated server: no renderer, nothing to draw.
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return;

            try
            {
                ElementFx.Spawn((Element)element, pos, duration, radius, height, new Color(color.x, color.y, color.z, 1f));
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log.LogError("OnSpawnVfx failed: " + e);
            }
        }

        public static void BroadcastLanding(Element element, Vector3 pos, float fallHeight)
        {
            if (!ValalaModsPlugin.SyncToOthers.Value || ZRoutedRpc.instance == null)
            {
                LandingEffects.Spawn(element, pos, fallHeight);
                return;
            }

            try
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(
                    ZRoutedRpc.Everybody, RpcLanding, (int)element, pos, fallHeight);
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log.LogError("Landing RPC failed, spawning locally: " + e);
                LandingEffects.Spawn(element, pos, fallHeight);
            }
        }

        private static void OnLanding(long sender, int element, Vector3 pos, float fallHeight)
        {
            // Headless dedicated server: no renderer, nothing to draw.
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return;

            try
            {
                LandingEffects.Spawn((Element)element, pos, fallHeight);
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log.LogError("OnLanding failed: " + e);
            }
        }
    }

    // Registers the VFX RPC whenever the network layer is (re)created.
    [HarmonyPatch(typeof(ZNet), "Awake")]
    internal static class ZNetAwakePatch
    {
        private static void Postfix()
        {
            RpcSync.EnsureRegistered();
        }
    }

    // While the element menu is open, keep the cursor visible/unlocked so the player can click
    // a class; the vanilla capture logic is skipped for that frame.
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateMouseCapture))]
    internal static class CursorCapturePatch
    {
        private static bool Prefix()
        {
            if (!ElementSelectorUi.IsMenuVisible)
                return true;

            try
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            catch
            {
                // Never break the game over the cursor.
            }

            return false;
        }
    }

    // While the element menu is open, freeze the mouse-look so the camera stays put
    // (the cursor is free for clicking, but must not rotate the view).
    [HarmonyPatch(typeof(PlayerController), "LateUpdate")]
    internal static class CameraLookPatch
    {
        private static bool Prefix()
        {
            if (!ElementSelectorUi.IsMenuVisible)
                return true;

            try
            {
                Player local = Player.m_localPlayer;
                if (local != null)
                    local.SetMouseLook(Vector2.zero);
            }
            catch
            {
                // Never break input handling.
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(WindImpulseVfx), nameof(WindImpulseVfx.Spawn))]
    internal static class WindImpulsePatch
    {
        // Replace mode: skip the original wind VFX and spawn lightning instead.
        private static bool Prefix(Player player)
        {
            try
            {
                if (!ValalaModsPlugin.Enabled.Value)
                    return true;

                if (ValalaModsPlugin.Mode.Value.Equals("Replace", StringComparison.OrdinalIgnoreCase))
                {
                    bool replaced = ValalaModsPlugin.TrySpawnElementStrike(player);
                    return !replaced;
                }
            }
            catch
            {
                // Fall through and let the original run.
            }

            return true;
        }

        // Add mode: keep the wind VFX and overlay lightning.
        private static void Postfix(Player player)
        {
            try
            {
                if (!ValalaModsPlugin.Enabled.Value)
                    return;

                if (!ValalaModsPlugin.Mode.Value.Equals("Replace", StringComparison.OrdinalIgnoreCase))
                    ValalaModsPlugin.TrySpawnElementStrike(player);
            }
            catch
            {
                // Never break the game because of the VFX.
            }
        }
    }
}
