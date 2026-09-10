using System.Collections.Generic;
using UnityEngine;
using Game.Content;
using Game.Items;
using Game.World.Rendering;

namespace Game.Inventory.UI
{
    public static class ItemIconProvider
    {
        private static readonly Dictionary<string, Sprite> spriteCache =
            new Dictionary<string, Sprite>();

        private static readonly Dictionary<string, Texture2D> textureCache =
            new Dictionary<string, Texture2D>();

        public static Sprite GetIcon(string itemID)
        {
            if (string.IsNullOrWhiteSpace(itemID))
                return null;

            if (spriteCache.TryGetValue(itemID, out Sprite cachedSprite))
                return cachedSprite;

            if (!ItemRegistry.TryGet(itemID, out ItemDefinition item))
            {
                Debug.LogWarning("ITEM ICON: Item not found: " + itemID);
                return null;
            }

            // Для блоков используем тот же BlockRenderer,
            // что и мир. Никаких дубликатов texture в Resources.
            if (item.Type == ItemType.Block)
            {
                Sprite blockSprite = CreateBlockSprite(itemID);

                if (blockSprite != null)
                {
                    spriteCache[itemID] = blockSprite;
                    return blockSprite;
                }
            }

            // Fallback для будущих НЕ-блоковых предметов.
            if (!string.IsNullOrWhiteSpace(item.Texture))
            {
                Sprite resourceSprite =
                    UnityEngine.Resources.Load<Sprite>(
                        "ItemTextures/" + item.Texture
                    );

                if (resourceSprite != null)
                {
                    spriteCache[itemID] = resourceSprite;
                    return resourceSprite;
                }
            }

            Debug.LogWarning("ITEM ICON: Sprite not found for " + itemID);
            return null;
        }

        private static Sprite CreateBlockSprite(string itemID)
        {
            ContentID contentID;

            try
            {
                contentID = ContentID.Parse(itemID);
            }
            catch
            {
                Debug.LogWarning("ITEM ICON: Invalid ContentID: " + itemID);
                return null;
            }

            if (!BlockIDRegistry.Contains(contentID))
            {
                Debug.LogWarning("ITEM ICON: Block ID not registered: " + itemID);
                return null;
            }

            ushort blockID =
                BlockIDRegistry.GetID(contentID);

            if (blockID == 0)
            {
                Debug.LogWarning("ITEM ICON: Numeric block ID is 0: " + itemID);
                return null;
            }

            int size =
                BlockRenderer.BlockPixelSize;

            Texture2D texture =
                new Texture2D(
                    size,
                    size,
                    TextureFormat.RGBA32,
                    false
                );

            texture.name =
                "ItemTexture_" + itemID;

            texture.filterMode =
                FilterMode.Point;

            texture.wrapMode =
                TextureWrapMode.Clamp;

            texture.SetPixels32(
                new Color32[size * size]
            );

            BlockRenderer.DrawBlock(
                texture,
                0,
                0,
                blockID
            );

            texture.Apply(
                false,
                false
            );

            Sprite sprite =
                Sprite.Create(
                    texture,
                    new Rect(
                        0,
                        0,
                        size,
                        size
                    ),
                    new Vector2(
                        0.5f,
                        0.5f
                    ),
                    size
                );

            sprite.name =
                "ItemSprite_" + itemID;

            textureCache[itemID] =
                texture;

            Debug.Log(
                "ITEM ICON: Created block sprite: " +
                itemID
            );

            return sprite;
        }

        public static void ClearCache()
        {
            foreach (Sprite sprite in spriteCache.Values)
            {
                if (sprite != null)
                    Object.Destroy(sprite);
            }

            foreach (Texture2D texture in textureCache.Values)
            {
                if (texture != null)
                    Object.Destroy(texture);
            }

            spriteCache.Clear();
            textureCache.Clear();
        }
    }
}
