using System.Collections.Generic;
using System.IO;

using UnityEngine;

using Game.Content;
using Game.Items;
using Game.World.Rendering;


namespace Game.Inventory.UI
{

    public static class ItemIconProvider
    {

        private static readonly Dictionary<
            string,
            Sprite
        > spriteCache =
            new Dictionary<
                string,
                Sprite
            >();


        private static readonly Dictionary<
            string,
            Texture2D
        > textureCache =
            new Dictionary<
                string,
                Texture2D
            >();


        private const float ItemPixelsPerUnit =
            16f;


        // =====================================================
        // PUBLIC
        // =====================================================

        public static Sprite GetIcon(
            string itemID
        )
        {

            if (
                string.IsNullOrWhiteSpace(
                    itemID
                )
            )
            {

                return null;

            }


            if (
                spriteCache.TryGetValue(
                    itemID,
                    out Sprite cachedSprite
                )
            )
            {

                return cachedSprite;

            }


            if (
                !ItemRegistry.TryGet(
                    itemID,
                    out ItemDefinition item
                )
                ||
                item ==
                null
            )
            {

                Debug.LogWarning(
                    "ITEM ICON: Item not found: " +
                    itemID
                );


                return null;

            }


            // =================================================
            // BLOCK ITEM
            // =================================================
            //
            // Blocks continue to use BlockRenderer and the current
            // resource-pack block texture system.
            //
            // =================================================

            if (
                item.Type ==
                ItemType.Block
            )
            {

                Sprite blockSprite =
                    CreateBlockSprite(
                        itemID
                    );


                if (
                    blockSprite !=
                    null
                )
                {

                    spriteCache[
                        itemID
                    ] =
                        blockSprite;


                    return blockSprite;

                }

            }


            // =================================================
            // NORMAL ITEM
            // =================================================
            //
            // Item JSON:
            //
            // "Texture": "wood_pickaxe"
            //
            // File:
            //
            // Assets/GameData/ResourcePacks/Default/
            // textures/items/wood_pickaxe.png
            //
            // =================================================

            if (
                !string.IsNullOrWhiteSpace(
                    item.Texture
                )
            )
            {

                Sprite itemSprite =
                    LoadItemSpriteFromDefaultPack(
                        itemID,
                        item.Texture
                    );


                if (
                    itemSprite !=
                    null
                )
                {

                    spriteCache[
                        itemID
                    ] =
                        itemSprite;


                    return itemSprite;

                }

            }


            Debug.LogWarning(
                "ITEM ICON: Sprite not found for " +
                itemID
            );


            return null;

        }


        // =====================================================
        // ITEM TEXTURE FROM RESOURCE PACK
        // =====================================================

        private static Sprite LoadItemSpriteFromDefaultPack(
            string itemID,
            string textureName
        )
        {

            string cleanTextureName =
                textureName.Trim();


            if (
                cleanTextureName.EndsWith(
                    ".png",
                    System.StringComparison.OrdinalIgnoreCase
                )
            )
            {

                cleanTextureName =
                    cleanTextureName.Substring(
                        0,
                        cleanTextureName.Length -
                        4
                    );

            }


            string texturePath =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "ResourcePacks",
                    "Default",
                    "textures",
                    "items",
                    cleanTextureName +
                    ".png"
                );


            if (
                !File.Exists(
                    texturePath
                )
            )
            {

                Debug.LogWarning(
                    "ITEM ICON: Item texture file not found:\n" +
                    texturePath
                );


                return null;

            }


            byte[] bytes;


            try
            {

                bytes =
                    File.ReadAllBytes(
                        texturePath
                    );

            }
            catch (
                System.Exception exception
            )
            {

                Debug.LogError(
                    "ITEM ICON: Failed to read texture:\n" +
                    texturePath +
                    "\n" +
                    exception
                );


                return null;

            }


            Texture2D texture =
                new Texture2D(
                    2,
                    2,
                    TextureFormat.RGBA32,
                    false
                );


            texture.name =
                "ItemTexture_" +
                itemID;


            texture.filterMode =
                FilterMode.Point;


            texture.wrapMode =
                TextureWrapMode.Clamp;


            if (
                !texture.LoadImage(
                    bytes,
                    false
                )
            )
            {

                Object.Destroy(
                    texture
                );


                Debug.LogError(
                    "ITEM ICON: LoadImage failed:\n" +
                    texturePath
                );


                return null;

            }


            texture.filterMode =
                FilterMode.Point;


            texture.wrapMode =
                TextureWrapMode.Clamp;


            Sprite sprite =
                Sprite.Create(
                    texture,

                    new Rect(
                        0f,
                        0f,
                        texture.width,
                        texture.height
                    ),

                    new Vector2(
                        0.5f,
                        0.5f
                    ),

                    ItemPixelsPerUnit,

                    0,

                    SpriteMeshType.FullRect
                );


            sprite.name =
                "ItemSprite_" +
                itemID;


            textureCache[
                itemID
            ] =
                texture;


            Debug.Log(
                "ITEM ICON: Loaded item texture from resource pack: " +
                itemID +
                " -> " +
                texturePath
            );


            return sprite;

        }


        // =====================================================
        // BLOCK ITEM ICON
        // =====================================================

        private static Sprite CreateBlockSprite(
            string itemID
        )
        {

            ContentID contentID;


            try
            {

                contentID =
                    ContentID.Parse(
                        itemID
                    );

            }
            catch
            {

                Debug.LogWarning(
                    "ITEM ICON: Invalid ContentID: " +
                    itemID
                );


                return null;

            }


            if (
                !BlockIDRegistry.Contains(
                    contentID
                )
            )
            {

                Debug.LogWarning(
                    "ITEM ICON: Block ID not registered: " +
                    itemID
                );


                return null;

            }


            ushort blockID =
                BlockIDRegistry.GetID(
                    contentID
                );


            if (
                blockID ==
                0
            )
            {

                Debug.LogWarning(
                    "ITEM ICON: Numeric block ID is 0: " +
                    itemID
                );


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
                "ItemTexture_" +
                itemID;


            texture.filterMode =
                FilterMode.Point;


            texture.wrapMode =
                TextureWrapMode.Clamp;


            texture.SetPixels32(
                new Color32[
                    size *
                    size
                ]
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
                "ItemSprite_" +
                itemID;


            textureCache[
                itemID
            ] =
                texture;


            return sprite;

        }


        // =====================================================
        // CACHE
        // =====================================================

        public static void ClearCache()
        {

            foreach (
                Sprite sprite
                in spriteCache.Values
            )
            {

                if (
                    sprite !=
                    null
                )
                {

                    Object.Destroy(
                        sprite
                    );

                }

            }


            foreach (
                Texture2D texture
                in textureCache.Values
            )
            {

                if (
                    texture !=
                    null
                )
                {

                    Object.Destroy(
                        texture
                    );

                }

            }


            spriteCache.Clear();

            textureCache.Clear();

        }

    }

}
