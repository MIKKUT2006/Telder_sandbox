using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.World.Dimensions
{
    [Serializable]
    public sealed class PortalGunConfig
    {
        public string CrystalItemId = "game:crystal";
        public int CrystalCost = 1;
        public float BeamSpeed = 28f;
        public float BeamWidth = 5f;
        public float BeamDistortion = 6f;
        public float BeamStrength = 1f;
        public string BeamColor = "#62E9FF";
    }

    /// <summary>
    /// Reads only the optional PortalGun object from existing item JSON files.
    /// This keeps ItemDefinition unchanged and preserves the project's current item schema.
    /// </summary>
    public static class PortalGunConfigRegistry
    {
        [Serializable]
        private sealed class ItemRoot
        {
            public string ID;
            public string ToolType;
            public PortalGunConfig PortalGun;
        }

        private static readonly Dictionary<string, PortalGunConfig> Configs =
            new Dictionary<string, PortalGunConfig>(StringComparer.OrdinalIgnoreCase);

        private static bool loaded;

        public static bool TryGet(string itemId, out PortalGunConfig config)
        {
            EnsureLoaded();
            config = null;
            if (string.IsNullOrWhiteSpace(itemId))
                return false;
            return Configs.TryGetValue(itemId.Trim(), out config) && config != null;
        }

        public static void Reload()
        {
            loaded = true;
            Configs.Clear();

            string folder = Path.Combine(Game.Content.DataPaths.GameDataFolder, "Items");
            if (!Directory.Exists(folder))
            {
                Debug.LogWarning("PORTALGUN CONFIG: item JSON folder not found: " + folder);
                return;
            }

            string[] files = Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    ItemRoot root = JsonUtility.FromJson<ItemRoot>(File.ReadAllText(files[i]));
                    if (root == null || string.IsNullOrWhiteSpace(root.ID))
                        continue;

                    // PortalGun configuration alone must never grant portal-gun behavior.
                    // The item's explicit ToolType is the source of truth.
                    if (!string.Equals((root.ToolType ?? string.Empty).Trim(), "portal_gun",
                            StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (root.PortalGun == null)
                    {
                        Debug.LogWarning("PORTALGUN CONFIG: item '" + root.ID +
                            "' has ToolType=portal_gun but no PortalGun settings: " + Path.GetFileName(files[i]));
                        continue;
                    }

                    root.PortalGun.CrystalItemId = string.IsNullOrWhiteSpace(root.PortalGun.CrystalItemId)
                        ? "game:crystal"
                        : root.PortalGun.CrystalItemId.Trim();
                    root.PortalGun.CrystalCost = Mathf.Max(0, root.PortalGun.CrystalCost);
                    root.PortalGun.BeamSpeed = Mathf.Max(1f, root.PortalGun.BeamSpeed);
                    root.PortalGun.BeamWidth = Mathf.Max(1f, root.PortalGun.BeamWidth);
                    root.PortalGun.BeamDistortion = Mathf.Max(0f, root.PortalGun.BeamDistortion);
                    root.PortalGun.BeamStrength = Mathf.Max(0f, root.PortalGun.BeamStrength);
                    Configs[root.ID.Trim()] = root.PortalGun;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("PORTALGUN CONFIG: failed to read " + Path.GetFileName(files[i]) + ": " + e.Message);
                }
            }

            Debug.Log("PORTALGUN CONFIG: Loaded " + Configs.Count + " configured Portalgun item(s).");
        }

        private static void EnsureLoaded()
        {
            if (!loaded)
                Reload();
        }
    }
}
