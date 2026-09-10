using System;
using System.IO;
using System.Linq;
using Game.Content;
using UnityEngine;

namespace Game.Items
{
    public static class ItemFolderLoader
    {
        [Serializable]
        private class ItemJson
        {
            public string ID;
            public string Name;
            public string Texture;
            public string Type;
            public int MaxStack;
        }

        public static void LoadDefaultFolder()
        {
            string folder = Path.Combine(Application.dataPath, "GameData", "Items");
            LoadFolder(folder);
        }

        public static void LoadFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                Debug.LogError("ITEM LOADER: Folder path is empty.");
                return;
            }

            if (!Directory.Exists(folder))
            {
                Debug.LogWarning("ITEM LOADER: Folder not found: " + folder);
                return;
            }

            string[] files = Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories);

            foreach (string file in files)
                LoadFile(file);

            Debug.Log("ITEM LOADER: Registry count = " + ItemRegistry.GetAll().Count());
        }

        private static void LoadFile(string file)
        {
            try
            {
                string json = File.ReadAllText(file);
                ItemJson data = JsonUtility.FromJson<ItemJson>(json);

                if (data == null || string.IsNullOrWhiteSpace(data.ID))
                {
                    Debug.LogWarning("ITEM LOADER: Invalid item JSON: " + file);
                    return;
                }

                ContentID contentID;
                try
                {
                    contentID = ContentID.Parse(data.ID);
                }
                catch
                {
                    Debug.LogWarning("ITEM LOADER: Invalid ContentID '" + data.ID + "' in " + file);
                    return;
                }

                ItemType itemType = default;

                if (!string.IsNullOrWhiteSpace(data.Type) &&
                    !Enum.TryParse(data.Type, true, out itemType))
                {
                    Debug.LogWarning("ITEM LOADER: Unknown ItemType '" + data.Type + "' for " + data.ID);
                }

                ItemDefinition definition = new ItemDefinition
                {
                    ID = contentID,
                    Name = string.IsNullOrWhiteSpace(data.Name) ? data.ID : data.Name,
                    Texture = data.Texture,
                    Type = itemType,
                    MaxStack = data.MaxStack > 0 ? data.MaxStack : 100
                };

                ItemRegistry.Register(definition);
            }
            catch (Exception exception)
            {
                Debug.LogError("ITEM LOADER: Failed to load " + file + "\n" + exception);
            }
        }
    }
}
