using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ValalaMods.NorseFx
{
    /// <summary>
    /// Loads the local <c>norsefx.bundle</c> that sits next to ValalaMods.dll. The original
    /// NorseDemigods plugin is not required: only the extracted AssetBundle is read.
    /// </summary>
    internal static class NorseFxResolver
    {
        private const string BundleFileName = "norsefx.bundle";

        private static readonly Dictionary<string, GameObject> PrefabCache =
            new Dictionary<string, GameObject>();

        private static readonly Dictionary<string, AudioClip> ClipCache =
            new Dictionary<string, AudioClip>();

        private static readonly HashSet<string> Warnings = new HashSet<string>();

        private static AssetBundle _bundle;
        private static bool _loadAttempted;

        internal static string BundlePath =>
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".", BundleFileName);

        internal static bool BundlePresent => File.Exists(BundlePath);

        internal static bool IsAvailable => Bundle != null;

        internal static GameObject LoadPrefab(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            if (PrefabCache.TryGetValue(assetPath, out GameObject cached) && cached != null)
                return cached;

            AssetBundle bundle = Bundle;
            if (bundle == null)
                return null;

            try
            {
                GameObject prefab = bundle.LoadAsset<GameObject>(assetPath);
                if (prefab == null)
                {
                    WarnOnce("prefab:" + assetPath, "Norse bundle asset not found: " + assetPath);
                    return null;
                }

                PrefabCache[assetPath] = prefab;
                return prefab;
            }
            catch (Exception e)
            {
                WarnOnce("prefab:" + assetPath, "Norse bundle prefab failed (" + assetPath + "): " + e.Message);
                return null;
            }
        }

        internal static AudioClip LoadClip(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            if (ClipCache.TryGetValue(assetPath, out AudioClip cached) && cached != null)
                return cached;

            AssetBundle bundle = Bundle;
            if (bundle == null)
                return null;

            try
            {
                AudioClip clip = bundle.LoadAsset<AudioClip>(assetPath);
                if (clip == null)
                {
                    WarnOnce("clip:" + assetPath, "Norse bundle audio not found: " + assetPath);
                    return null;
                }

                ClipCache[assetPath] = clip;
                return clip;
            }
            catch (Exception e)
            {
                WarnOnce("clip:" + assetPath, "Norse bundle audio failed (" + assetPath + "): " + e.Message);
                return null;
            }
        }

        internal static void Unload()
        {
            try
            {
                _bundle?.Unload(false);
            }
            catch (Exception e)
            {
                ValalaModsPlugin.Log?.LogWarning("Norse bundle unload failed: " + e.Message);
            }

            _bundle = null;
            _loadAttempted = false;
            PrefabCache.Clear();
            ClipCache.Clear();
        }

        internal static void WarnOnce(string key, string message)
        {
            if (Warnings.Add(key))
                ValalaModsPlugin.Log?.LogWarning(message);
        }

        private static AssetBundle Bundle
        {
            get
            {
                if (_loadAttempted)
                    return _bundle;

                _loadAttempted = true;
                string path = BundlePath;

                if (!File.Exists(path))
                {
                    WarnOnce("bundle:missing", "Norse FX bundle missing: " + path);
                    return null;
                }

                try
                {
                    _bundle = AssetBundle.LoadFromFile(path);
                    if (_bundle == null)
                        WarnOnce("bundle:load", "Norse FX bundle could not be loaded: " + path);
                    else
                        ValalaModsPlugin.Log?.LogInfo("Norse FX bundle loaded: " + path);
                }
                catch (Exception e)
                {
                    WarnOnce("bundle:load", "Norse FX bundle load failed (" + path + "): " + e.Message);
                }

                return _bundle;
            }
        }
    }
}
