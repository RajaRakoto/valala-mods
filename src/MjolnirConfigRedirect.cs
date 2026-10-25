using System;
using System.IO;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;

namespace ValalaMods
{
    /// <summary>
    /// Loaded before Mjolnir because its GUID sorts first in BepInEx's chainloader.
    /// It redirects Mjolnir's BepInEx config file before Mjolnir reads or creates it.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, ValalaModsPlugin.PluginVersion)]
    public sealed class ValalaConfigRedirectPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "__valala.raja.mods.config";
        public const string PluginName = "Valala Config Redirect";
        internal const string MjolnirGuid = "com.kewb.valheimmjolnir";
        internal const string NewFileName = "odin.valala.raja.mods.mjolnir.cfg";

        private const string OldFileName = MjolnirGuid + ".cfg";

        private Harmony _harmony;

        private void Awake()
        {
            try
            {
                if (Chainloader.PluginInfos.ContainsKey(MjolnirGuid))
                {
                    Logger.LogWarning(
                        "Mjolnir is already loaded; config path redirect skipped. " +
                        "Mjolnir keeps its previous config path.");
                    return;
                }

                string oldPath = Path.Combine(Paths.ConfigPath, OldFileName);
                string newPath = Path.Combine(Paths.ConfigPath, NewFileName);

                if (!PrepareNewConfig(oldPath, newPath, out string migration))
                {
                    Logger.LogWarning(
                        "Mjolnir config path redirect disabled; the plugin keeps its default path. " +
                        migration);
                    return;
                }

                _harmony = new Harmony(PluginGuid + ".harmony");
                _harmony.PatchAll(typeof(MjolnirConfigPathPatch));

                Logger.LogInfo(
                    "Mjolnir config path redirected to " + newPath +
                    (string.IsNullOrEmpty(migration) ? string.Empty : " (" + migration + ")"));

                ArchiveLegacyConfig(oldPath);
            }
            catch (Exception e)
            {
                Logger.LogError(
                    "Mjolnir config path redirect failed; the plugin keeps its default path: " + e);
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        private static bool PrepareNewConfig(string oldPath, string newPath, out string message)
        {
            message = string.Empty;

            try
            {
                if (File.Exists(newPath))
                {
                    if (!CanRead(newPath))
                    {
                        message = "new config exists but is not readable: " + newPath;
                        return false;
                    }

                    return true;
                }

                if (!File.Exists(oldPath))
                    return true;

                File.Copy(oldPath, newPath, false);
                message = "migrated legacy config";
                return true;
            }
            catch (Exception e)
            {
                message = "could not prepare " + newPath + ": " + e.Message;
                return false;
            }
        }

        private void ArchiveLegacyConfig(string oldPath)
        {
            if (!File.Exists(oldPath))
                return;

            string backupPath = UniqueBackupPath(oldPath);
            try
            {
                File.Move(oldPath, backupPath);
                Logger.LogInfo("Mjolnir legacy config archived at " + backupPath);
            }
            catch (Exception e)
            {
                Logger.LogWarning(
                    "Could not archive legacy Mjolnir config at " + oldPath + ": " + e.Message);
            }
        }

        private static string UniqueBackupPath(string path)
        {
            string candidate = path + ".old";
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
                return candidate;

            for (int i = 1; ; i++)
            {
                candidate = path + ".old." + i;
                if (!File.Exists(candidate) && !Directory.Exists(candidate))
                    return candidate;
            }
        }

        private static bool CanRead(string path)
        {
            try
            {
                using (File.OpenRead(path))
                {
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(ConfigFile), MethodType.Constructor, new Type[]
    {
        typeof(string), typeof(bool), typeof(BepInPlugin)
    })]
    internal static class MjolnirConfigPathPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(ref string configPath, BepInPlugin ownerMetadata)
        {
            if (ownerMetadata == null ||
                !string.Equals(ownerMetadata.GUID, ValalaConfigRedirectPlugin.MjolnirGuid,
                    StringComparison.Ordinal))
                return;

            configPath = Path.Combine(
                Paths.ConfigPath, ValalaConfigRedirectPlugin.NewFileName);
        }
    }
}
