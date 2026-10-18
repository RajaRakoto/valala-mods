using System;
using UnityEngine;

namespace ValalaMods.NorseFx
{
    /// <summary>
    /// Small opt-in API around selected bundle assets. Every method is safe to call when the
    /// bundle is missing or the catalog is empty: it returns null/false and does not crash.
    /// </summary>
    internal static class NorseFxService
    {
        internal static bool IsAvailable =>
            (ValalaModsPlugin.EnableNorseFx?.Value ?? true) &&
            NorseFxCatalog.Count > 0 &&
            NorseFxResolver.IsAvailable;

        internal static bool HasEffect(string id)
        {
            return IsAvailable && NorseFxCatalog.TryGet(id, out _);
        }

        internal static GameObject SpawnVfx(
            string id,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null,
            float? lifetime = null,
            float scale = 1f)
        {
            if (!IsAvailable || !NorseFxCatalog.TryGet(id, out NorseFxEntry entry) || !entry.HasVfx)
                return null;

            GameObject prefab = NorseFxResolver.LoadPrefab(entry.PrefabPath);
            if (prefab == null)
                return null;

            try
            {
                GameObject go = parent != null
                    ? UnityEngine.Object.Instantiate(prefab, position, rotation, parent)
                    : UnityEngine.Object.Instantiate(prefab, position, rotation);

                if (!Mathf.Approximately(scale, 1f))
                    go.transform.localScale *= scale;

                float seconds = lifetime ?? entry.Lifetime;
                if (seconds > 0f)
                    UnityEngine.Object.Destroy(go, seconds);

                return go;
            }
            catch (Exception e)
            {
                NorseFxResolver.WarnOnce("spawn:" + id, "Norse VFX spawn failed (" + id + "): " + e.Message);
                return null;
            }
        }

        internal static GameObject PlaySfx(
            string id,
            Vector3 position,
            Transform parent = null)
        {
            if (!IsAvailable || !NorseFxCatalog.TryGet(id, out NorseFxEntry entry) || !entry.HasSfx)
                return null;

            AudioClip clip = NorseFxResolver.LoadClip(entry.SfxPath);
            if (clip == null)
                return null;

            try
            {
                GameObject go = new GameObject("Valala_NorseSfx_" + id);
                go.transform.SetParent(parent, false);
                go.transform.position = position;

                AudioSource source = go.AddComponent<AudioSource>();
                source.clip = clip;
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.minDistance = entry.MinDistance;
                source.maxDistance = entry.MaxDistance;
                source.volume = entry.Volume;
                source.loop = entry.Loop;
                source.playOnAwake = false;
                source.Play();

                if (entry.SfxStartTime > 0f && entry.SfxStartTime < clip.length)
                    source.time = entry.SfxStartTime;

                if (!entry.Loop)
                    UnityEngine.Object.Destroy(go, clip.length + 0.1f);

                return go;
            }
            catch (Exception e)
            {
                NorseFxResolver.WarnOnce("sfx:" + id, "Norse SFX spawn failed (" + id + "): " + e.Message);
                return null;
            }
        }

        internal static bool Stop(GameObject handle)
        {
            if (handle == null)
                return false;

            try
            {
                if (ZNetScene.instance != null)
                    ZNetScene.instance.Destroy(handle);
                else
                    UnityEngine.Object.Destroy(handle);

                return true;
            }
            catch (Exception e)
            {
                NorseFxResolver.WarnOnce("stop", "Norse effect stop failed: " + e.Message);
                return false;
            }
        }
    }
}
