using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Combat
{
    public static class WeaponMetadataRegistry
    {
        [Serializable]
        private sealed class ItemRoot
        {
            public string ID;
            public WeaponMetadata Weapon;
        }

        private static readonly Dictionary<
            string,
            WeaponMetadata
        > Data =
            new Dictionary<
                string,
                WeaponMetadata
            >(
                StringComparer.OrdinalIgnoreCase
            );

        private static bool loaded;

        public static void Initialize()
        {
            if (!loaded)
                Reload();
        }

        public static void Reload()
        {
            loaded = true;
            Data.Clear();

            string folder =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "Items"
                );

            if (!Directory.Exists(folder))
                return;

            string[] files =
                Directory.GetFiles(
                    folder,
                    "*.json",
                    SearchOption.AllDirectories
                );

            for (int i = 0;
                 i < files.Length;
                 i++)
            {
                TryLoadFile(
                    files[i]
                );
            }

            Debug.Log(
                "WEAPON METADATA: Loaded " +
                Data.Count +
                " weapons."
            );
        }

        public static bool TryGet(
            string itemId,
            out WeaponMetadata weapon)
        {
            Initialize();

            if (string.IsNullOrWhiteSpace(
                    itemId))
            {
                weapon = null;
                return false;
            }

            return Data.TryGetValue(
                itemId.Trim(),
                out weapon
            );
        }

        public static bool IsWeapon(
            string itemId)
        {
            return TryGet(
                itemId,
                out _
            );
        }

        private static void TryLoadFile(
            string path)
        {
            try
            {
                string json =
                    File.ReadAllText(
                        path
                    );

                ItemRoot root =
                    JsonUtility.FromJson<
                        ItemRoot
                    >(
                        json
                    );

                if (root == null ||
                    root.Weapon == null ||
                    string.IsNullOrWhiteSpace(
                        root.ID))
                {
                    return;
                }

                root.Weapon.Normalize();

                Data[
                    root.ID.Trim()
                ] =
                    root.Weapon;
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    "WEAPON METADATA: Failed to read " +
                    Path.GetFileName(path) +
                    ": " +
                    e.Message
                );
            }
        }
    }
}
