using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.UI.Character
{
    // Keep existing numeric values for compatibility with already saved presets.
    // Append new categories only; existing saved preset category names remain compatible.
    public enum PartCategory { Hair, Body, Arms, Legs, Head, Boots, Sleeves, Eyes, Outerwears, Jewelry }

    [Serializable]
    public sealed class AppearancePart
    {
        public string Category;
        public string Sprite;
        public Color Tint = Color.white;
    }

    [Serializable]
    public sealed class AppearancePreset
    {
        public string Id;
        public string Name;
        public AppearancePart[] Parts = new AppearancePart[0];

        public AppearancePreset Clone()
        {
            PartCategory[] categories = (PartCategory[])Enum.GetValues(typeof(PartCategory));
            var result = new AppearancePreset
            {
                Id = Id,
                Name = Name,
                Parts = new AppearancePart[categories.Length]
            };
            for (int i = 0; i < categories.Length; i++)
            {
                PartCategory category = categories[i];
                AppearancePart old = Part(category);
                result.Parts[i] = new AppearancePart
                {
                    Category = category.ToString(),
                    Sprite = old != null ? old.Sprite : string.Empty,
                    Tint = old != null ? old.Tint : Color.white
                };
            }
            return result;
        }

        public AppearancePart Part(PartCategory part)
        {
            if (Parts != null)
                foreach (AppearancePart p in Parts)
                    if (p != null && string.Equals(p.Category, part.ToString(), StringComparison.OrdinalIgnoreCase))
                        return p;
            return null;
        }
    }

    [Serializable]
    public sealed class AppearanceSaveData
    {
        public string ActiveId = "";
        public List<AppearancePreset> Presets = new List<AppearancePreset>();
    }

    public static class PlayerAppearanceStorage
    {
        // Resource-pack PNGs are the single source of truth. In the Editor this resolves to
        // Assets/GameData/ResourcePacks; in a player build it resolves to <Game>_Data/GameData/ResourcePacks.
        private const string PackName = "Default";
        private const float FileChangePollSeconds = 0.5f;
        private const float DefaultPixelsPerUnit = 32f;
        private static readonly Dictionary<PartCategory, CategoryCache> cache =
            new Dictionary<PartCategory, CategoryCache>();
        private static readonly Regex PpuRegex = new Regex(
            @"spritePixelsToUnits:\s*([0-9]+(?:\.[0-9]+)?)", RegexOptions.Compiled);
        private static readonly Regex PivotRegex = new Regex(
            @"spritePivot:\s*\{x:\s*([-+]?[0-9]*\.?[0-9]+),\s*y:\s*([-+]?[0-9]*\.?[0-9]+)\}",
            RegexOptions.Compiled);

        private sealed class CategoryCache
        {
            public string Signature;
            public float NextValidationTime;
            public Sprite[] Sprites;
            public Texture2D[] Textures;
        }

        public static string Folder(PartCategory category)
        {
            switch (category)
            {
                case PartCategory.Hair: return "Hairs";
                case PartCategory.Body: return "Bodies";
                case PartCategory.Arms: return "Arms";
                case PartCategory.Legs: return "Legs";
                case PartCategory.Boots: return "Boots";
                case PartCategory.Sleeves: return "Sleeves";
                case PartCategory.Eyes: return "Eyes";
                case PartCategory.Outerwears: return "Outerwears";
                case PartCategory.Jewelry: return "Jewelry";
                default: return "Heads";
            }
        }

        private static string LegacyFile(PartCategory category)
        {
            switch (category)
            {
                case PartCategory.Hair: return "Hair.png";
                case PartCategory.Body: return "Body.png";
                case PartCategory.Arms: return "Arm.png";
                case PartCategory.Legs: return "Leg.png";
                case PartCategory.Boots: return "Boot.png";
                case PartCategory.Sleeves: return "Sleeve.png";
                case PartCategory.Eyes: return "Eyes.png";
                case PartCategory.Outerwears: return "Outerwear.png";
                case PartCategory.Jewelry: return "Jewelry.png";
                default: return "Head.png";
            }
        }

        private static string PlayerTexturesRoot
        {
            get
            {
                return Path.Combine(Game.Content.DataPaths.ResourcePacksFolder,
                    PackName, "textures", "Entity", "Player");
            }
        }

        public static void InvalidateCache()
        {
            foreach (CategoryCache entry in cache.Values) DestroyCache(entry);
            cache.Clear();
        }

        public static Sprite[] GetVariants(PartCategory category)
        {
            CategoryCache entry;
            float now = Time.realtimeSinceStartup;
            if (cache.TryGetValue(category, out entry) && now < entry.NextValidationTime)
                return entry.Sprites;

            string[] files = GetSourceFiles(category);
            string signature = BuildSignature(files);
            if (entry != null && entry.Signature == signature)
            {
                entry.NextValidationTime = now + FileChangePollSeconds;
                return entry.Sprites;
            }

            CategoryCache refreshed = LoadCategory(category, files, signature, now);
            if (entry != null) DestroyCache(entry);
            cache[category] = refreshed;
            return refreshed.Sprites;
        }

        public static Sprite GetSprite(PartCategory category, string sprite)
        {
            if (string.IsNullOrEmpty(sprite)) return null;
            Sprite[] items = GetVariants(category);
            for (int i = 0; i < items.Length; ++i)
                if (string.Equals(items[i].name, sprite, StringComparison.OrdinalIgnoreCase)) return items[i];
            return null;
        }

        private static string[] GetSourceFiles(PartCategory category)
        {
            string root = PlayerTexturesRoot;
            string categoryFolder = Path.Combine(root, Folder(category));
            string[] files = new string[0];
            try
            {
                if (Directory.Exists(categoryFolder))
                {
                    string[] candidates = Directory.GetFiles(categoryFolder, "*", SearchOption.TopDirectoryOnly);
                    List<string> pngFiles = new List<string>(candidates.Length);
                    for (int i = 0; i < candidates.Length; i++)
                        if (string.Equals(Path.GetExtension(candidates[i]), ".png", StringComparison.OrdinalIgnoreCase))
                            pngFiles.Add(candidates[i]);
                    files = pngFiles.ToArray();
                }
                if (files.Length == 0)
                {
                    // Backwards compatibility with older resource packs that had only
                    // Player/Head.png, Player/Body.png, etc. at the root.
                    string legacy = Path.Combine(root, LegacyFile(category));
                    if (File.Exists(legacy)) files = new[] { legacy };
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("APPEARANCE: cannot scan resource-pack folder '" + categoryFolder + "': " + ex.Message);
            }
            Array.Sort(files, (a, b) => string.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase));
            return files;
        }

        private static string BuildSignature(string[] files)
        {
            StringBuilder signature = new StringBuilder();
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    FileInfo info = new FileInfo(files[i]);
                    if (!info.Exists) continue;
                    signature.Append(Path.GetFileName(files[i])).Append('|')
                        .Append(info.Length).Append('|')
                        .Append(info.LastWriteTimeUtc.Ticks).Append(';');
                    string metaPath = files[i] + ".meta";
                    if (File.Exists(metaPath))
                    {
                        FileInfo meta = new FileInfo(metaPath);
                        signature.Append("meta|").Append(meta.Length).Append('|')
                            .Append(meta.LastWriteTimeUtc.Ticks).Append(';');
                    }
                }
                catch { }
            }
            return signature.ToString();
        }

        private static CategoryCache LoadCategory(PartCategory category, string[] files,
            string signature, float now)
        {
            List<Sprite> sprites = new List<Sprite>(files.Length);
            List<Texture2D> textures = new List<Texture2D>(files.Length);
            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];
                Texture2D texture = null;
                try
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                    texture.name = Path.GetFileNameWithoutExtension(path);
                    if (!texture.LoadImage(bytes, false))
                    {
                        DestroyObject(texture);
                        Debug.LogWarning("APPEARANCE: invalid PNG skipped: " + path);
                        continue;
                    }
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    texture.anisoLevel = 0;

                    float ppu = ReadPixelsPerUnit(path);
                    Vector2 pivot = ReadPivot(path);
                    Sprite sprite = Sprite.Create(texture,
                        new Rect(0, 0, texture.width, texture.height), pivot, ppu);
                    sprite.name = Path.GetFileNameWithoutExtension(path);
                    sprites.Add(sprite);
                    textures.Add(texture);
                }
                catch (Exception ex)
                {
                    if (texture != null) DestroyObject(texture);
                    Debug.LogWarning("APPEARANCE: failed to load player texture '" + path + "': " + ex.Message);
                }
            }

            return new CategoryCache
            {
                Signature = signature,
                NextValidationTime = now + FileChangePollSeconds,
                Sprites = sprites.ToArray(),
                Textures = textures.ToArray()
            };
        }

        private static float ReadPixelsPerUnit(string pngPath)
        {
            string metaPath = pngPath + ".meta";
            if (!File.Exists(metaPath)) return DefaultPixelsPerUnit;
            try
            {
                Match match = PpuRegex.Match(File.ReadAllText(metaPath));
                float value;
                if (match.Success && float.TryParse(match.Groups[1].Value,
                    NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value > 0.01f)
                    return value;
            }
            catch { }
            return DefaultPixelsPerUnit;
        }

        private static Vector2 ReadPivot(string pngPath)
        {
            string metaPath = pngPath + ".meta";
            if (!File.Exists(metaPath)) return new Vector2(0.5f, 0.5f);
            try
            {
                Match match = PivotRegex.Match(File.ReadAllText(metaPath));
                float x, y;
                if (match.Success &&
                    float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                    float.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                    return new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
            }
            catch { }
            return new Vector2(0.5f, 0.5f);
        }

        private static void DestroyCache(CategoryCache entry)
        {
            if (entry == null) return;
            if (entry.Sprites != null)
                for (int i = 0; i < entry.Sprites.Length; i++) DestroyObject(entry.Sprites[i]);
            if (entry.Textures != null)
                for (int i = 0; i < entry.Textures.Length; i++) DestroyObject(entry.Textures[i]);
        }

        private static void DestroyObject(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }

        public static string FilePath
        {
            get { return Path.Combine(Application.persistentDataPath, "TelderPlayerAppearance", "presets.json"); }
        }

        public static AppearanceSaveData Load()
        {
            if (!File.Exists(FilePath)) return new AppearanceSaveData();
            try
            {
                var data = JsonUtility.FromJson<AppearanceSaveData>(File.ReadAllText(FilePath));
                if (data != null)
                {
                    if (data.Presets == null) data.Presets = new List<AppearancePreset>();
                    return data;
                }
            }
            catch (Exception ex) { Debug.LogWarning("APPEARANCE: failed reading presets: " + ex.Message); }
            return new AppearanceSaveData();
        }

        public static bool Save(AppearanceSaveData data)
        {
            try
            {
                string path = FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                File.Copy(temp, path, true);
                File.Delete(temp);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("APPEARANCE: cannot save presets: " + ex);
                return false;
            }
        }

        public static AppearancePreset Active()
        {
            AppearanceSaveData file = Load();
            for (int i = 0; i < file.Presets.Count; ++i)
                if (file.Presets[i] != null && file.Presets[i].Id == file.ActiveId)
                    return file.Presets[i].Clone();
            return null;
        }
    }

    /// <summary>
    /// Applies appearance textures and tint data. The world-light receiver combines the selected
    /// appearance tint with the authored sprite color and sampled world lighting at render time.
    /// </summary>
    public sealed class AppearanceRig
    {
        private readonly Dictionary<PartCategory, List<SpriteRenderer>> renderers =
            new Dictionary<PartCategory, List<SpriteRenderer>>();
        private readonly Dictionary<SpriteRenderer, Sprite> originals = new Dictionary<SpriteRenderer, Sprite>();
        private readonly Dictionary<SpriteRenderer, Color> originalRendererColors = new Dictionary<SpriteRenderer, Color>();
        private readonly Dictionary<SpriteRenderer, AppearanceTintData> tintComponents =
            new Dictionary<SpriteRenderer, AppearanceTintData>();
        private readonly Dictionary<PartCategory, AppearancePart> selected = new Dictionary<PartCategory, AppearancePart>();
        private readonly Transform root;
        private readonly bool isPreview;
        private readonly Game.World.Lighting.WorldLightSpriteReceiver lightingReceiver;

        public AppearanceRig(Transform player)
        {
            root = player;
            isPreview = player != null && player.GetComponentInParent<AppearancePreviewMarker>() != null;
            lightingReceiver = player != null
                ? player.GetComponentInChildren<Game.World.Lighting.WorldLightSpriteReceiver>(true)
                : null;

            PartCategory[] categories = (PartCategory[])Enum.GetValues(typeof(PartCategory));
            for (int i = 0; i < categories.Length; i++) renderers[categories[i]] = new List<SpriteRenderer>();

            SpriteRenderer[] all = player != null ? player.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
            foreach (SpriteRenderer sr in all)
            {
                PartCategory group;
                if (!Detect(sr, out group)) continue;
                renderers[group].Add(sr);
                originals[sr] = sr.sprite;
                originalRendererColors[sr] = sr.color;

                // Store appearance tint as data instead of relying on a material's optional
                // _Color/_BaseColor property. Some arm/sleeve materials do not expose those
                // properties, which made those categories ignore the palette.
                AppearanceTintData tint = sr.GetComponent<AppearanceTintData>();
                if (tint == null) tint = sr.gameObject.AddComponent<AppearanceTintData>();
                tint.Tint = Color.white;
                tintComponents[sr] = tint;
            }
        }

        public bool HasParts { get { return originals.Count > 0; } }

        public string DefaultSprite(PartCategory category)
        {
            List<SpriteRenderer> list = renderers[category];
            return list.Count > 0 && originals[list[0]] != null ? originals[list[0]].name : string.Empty;
        }

        private static bool Detect(SpriteRenderer sr, out PartCategory group)
        {
            string s = sr.gameObject.name.ToLowerInvariant()
                .Replace("_", "").Replace(" ", "").Replace("-", "");

            // Test specific overlays before the broader limb/body names. The player's
            // SpriteRenderer objects are named exactly like the leaves in the rig hierarchy.
            if (s == "hair" || s.StartsWith("hair")) { group = PartCategory.Hair; return true; }
            if (s == "eyes" || s == "eye" || s.StartsWith("eyes") || s.StartsWith("eye")) { group = PartCategory.Eyes; return true; }
            if (s.Contains("jewelry") || s.Contains("jewerly") || s.Contains("jewellery"))
            { group = PartCategory.Jewelry; return true; }
            if (s.Contains("outerwear")) { group = PartCategory.Outerwears; return true; }
            if (s.Contains("sleeve")) { group = PartCategory.Sleeves; return true; }
            if (s == "head" || s == "face" || s.StartsWith("head")) { group = PartCategory.Head; return true; }
            if (s == "body" || s == "torso") { group = PartCategory.Body; return true; }
            if (s.Contains("boot")) { group = PartCategory.Boots; return true; }
            if (s.Contains("arm")) { group = PartCategory.Arms; return true; }
            if (s.Contains("leg")) { group = PartCategory.Legs; return true; }
            group = PartCategory.Body;
            return false;
        }

        public void Set(AppearancePreset preset)
        {
            selected.Clear();
            if (preset != null)
            {
                PartCategory[] categories = (PartCategory[])Enum.GetValues(typeof(PartCategory));
                for (int i = 0; i < categories.Length; i++)
                {
                    AppearancePart part = preset.Part(categories[i]);
                    if (part != null) selected[categories[i]] = part;
                }
            }
            Tick();
        }

        private static Color Multiply(Color a, Color b)
        {
            return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
        }

        private void ApplyTints()
        {
            foreach (KeyValuePair<PartCategory, List<SpriteRenderer>> pair in renderers)
            {
                AppearancePart part;
                Color tint = selected.TryGetValue(pair.Key, out part) && part != null ? part.Tint : Color.white;
                foreach (SpriteRenderer sr in pair.Value)
                {
                    if (sr == null) continue;
                    AppearanceTintData data;
                    if (!tintComponents.TryGetValue(sr, out data) || data == null)
                    {
                        data = sr.GetComponent<AppearanceTintData>();
                        if (data == null) data = sr.gameObject.AddComponent<AppearanceTintData>();
                        tintComponents[sr] = data;
                    }
                    data.Tint = tint;

                    // The isolated editor preview has no active world-light writer, so apply
                    // the tint to SpriteRenderer.color. In gameplay the world-light receiver
                    // combines this tint with the sampled light without overwriting either.
                    if (isPreview || lightingReceiver == null || !lightingReceiver.isActiveAndEnabled)
                    {
                        Color original;
                        if (originalRendererColors.TryGetValue(sr, out original))
                            sr.color = Multiply(original, tint);
                    }
                }
            }
        }

        public void Tick()
        {
            if (root == null) return;
            foreach (KeyValuePair<PartCategory, List<SpriteRenderer>> pair in renderers)
            {
                AppearancePart part;
                // Do not reset unselected parts to cached sprites. Old presets may not contain Boots,
                // and untouched renderers must remain under the control of their prefab/Animator.
                if (!selected.TryGetValue(pair.Key, out part) || part == null) continue;
                bool none = pair.Key == PartCategory.Hair && string.IsNullOrEmpty(part.Sprite);
                Sprite replacement = none ? null : PlayerAppearanceStorage.GetSprite(pair.Key, part.Sprite);
                foreach (SpriteRenderer sr in pair.Value)
                {
                    if (sr == null) continue;
                    Sprite target = replacement;
                    if (!none && target == null)
                    {
                        // A preset may refer to a texture that was removed from the resource pack.
                        // Fall back to this renderer's original prefab sprite instead of keeping a stale copy.
                        if (!originals.TryGetValue(sr, out target)) target = null;
                    }
                    if (sr.sprite != target) sr.sprite = target;
                }
            }
            // Reapply after Animator evaluation as some animation clips may key renderer colors.
            // In gameplay this updates only tint data; WorldLightSpriteReceiver owns renderer.color.
            ApplyTints();
        }

        public AppearancePreset Default(string name)
        {
            PartCategory[] categories = (PartCategory[])Enum.GetValues(typeof(PartCategory));
            AppearancePreset preset = new AppearancePreset
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                Parts = new AppearancePart[categories.Length]
            };
            for (int i = 0; i < categories.Length; i++)
            {
                PartCategory category = categories[i];
                string initial = DefaultSprite(category);
                if (string.IsNullOrEmpty(initial) && category != PartCategory.Hair)
                {
                    Sprite[] variants = PlayerAppearanceStorage.GetVariants(category);
                    if (variants.Length > 0) initial = variants[0].name;
                }
                preset.Parts[i] = new AppearancePart
                {
                    Category = category.ToString(),
                    Sprite = initial,
                    Tint = Color.white
                };
            }
            return preset;
        }
    }
}
