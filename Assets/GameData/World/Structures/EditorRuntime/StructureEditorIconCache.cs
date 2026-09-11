
using System.Collections.Generic;

using UnityEngine;

using Game.Content;
using Game.World.Rendering;


namespace Game.World.Structures.EditorRuntime
{

    public static class StructureEditorIconCache
    {

        private static readonly Dictionary<
            string,
            Texture2D
        > cache =
            new Dictionary<
                string,
                Texture2D
            >();


        public static Texture2D Get(
            string blockId
        )
        {

            if (
                string.IsNullOrWhiteSpace(
                    blockId
                )
            )
            {

                return null;

            }


            if (
                cache.TryGetValue(
                    blockId,
                    out Texture2D existing
                )
            )
            {

                return existing;

            }


            try
            {

                ContentID contentId =
                    ContentID.Parse(
                        blockId
                    );


                if (
                    !BlockIDRegistry.Contains(
                        contentId
                    )
                )
                {

                    return null;

                }


                ushort id =
                    BlockIDRegistry.GetID(
                        contentId
                    );


                int size =
                    Mathf.Max(
                        1,
                        BlockRenderer.BlockPixelSize
                    );


                Texture2D texture =
                    new Texture2D(
                        size,
                        size,
                        TextureFormat.RGBA32,
                        false
                    );


                texture.name =
                    "StructureEditor_" +
                    blockId;


                texture.filterMode =
                    FilterMode.Point;


                texture.wrapMode =
                    TextureWrapMode.Clamp;


                Color[] clear =
                    new Color[
                        size *
                        size
                    ];


                for (
                    int i = 0;
                    i < clear.Length;
                    i++
                )
                {

                    clear[i] =
                        Color.clear;

                }


                texture.SetPixels(
                    clear
                );


                BlockRenderer.DrawBlock(
                    texture,
                    0,
                    0,
                    id
                );


                texture.Apply(
                    false,
                    false
                );


                cache[
                    blockId
                ] =
                    texture;


                return texture;

            }
            catch
            {

                return null;

            }

        }


        public static void Clear()
        {

            foreach (
                Texture2D texture
                in cache.Values
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


            cache.Clear();

        }

    }

}
