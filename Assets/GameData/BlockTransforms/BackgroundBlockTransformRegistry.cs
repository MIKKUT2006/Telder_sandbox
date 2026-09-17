using System.Collections.Generic;
using UnityEngine;

namespace Game.BlockTransforms
{
    public static class BackgroundBlockTransformRegistry
    {
        private static readonly object Sync =
            new object();


        private static readonly Dictionary<long, byte> Data =
            new Dictionary<long, byte>();


        public static byte GetRaw(
            int x,
            int y
        )
        {
            lock (Sync)
            {
                return
                    Data.TryGetValue(
                        Key(
                            x,
                            y
                        ),
                        out byte value
                    )
                        ? value
                        : (byte)0;
            }
        }


        public static void SetRaw(
            int x,
            int y,
            byte value
        )
        {
            lock (Sync)
            {
                long key =
                    Key(
                        x,
                        y
                    );


                if (value == 0)
                {
                    Data.Remove(
                        key
                    );

                    return;
                }


                Data[
                    key
                ] =
                    value;
            }
        }


        public static void Clear(
            int x,
            int y
        )
        {
            lock (Sync)
            {
                Data.Remove(
                    Key(
                        x,
                        y
                    )
                );
            }
        }


        public static void ClearAll()
        {
            lock (Sync)
            {
                Data.Clear();
            }
        }


        public static void ApplyToCell(
            Texture2D texture,
            int cellX,
            int cellY,
            int pixelSize,
            int worldX,
            int worldY
        )
        {
            byte transform =
                GetRaw(
                    worldX,
                    worldY
                );


            if (
                transform ==
                0
                ||
                texture ==
                null
                ||
                pixelSize <=
                0
            )
            {
                return;
            }


            int startX =
                cellX *
                pixelSize;


            int startY =
                cellY *
                pixelSize;


            if (
                startX <
                0
                ||
                startY <
                0
                ||
                startX +
                pixelSize >
                texture.width
                ||
                startY +
                pixelSize >
                texture.height
            )
            {
                return;
            }


            Color[] source =
                texture.GetPixels(
                    startX,
                    startY,
                    pixelSize,
                    pixelSize
                );


            Color[] result =
                new Color[
                    source.Length
                ];


            int rotation =
                transform &
                0x03;


            bool mirrored =
                (
                    transform &
                    0x04
                )
                !=
                0;


            int max =
                pixelSize -
                1;


            for (
                int y = 0;
                y < pixelSize;
                y++
            )
            {
                for (
                    int x = 0;
                    x < pixelSize;
                    x++
                )
                {
                    int sourceX;
                    int sourceY;


                    switch (rotation)
                    {
                        default:
                        case 0:
                            sourceX =
                                x;

                            sourceY =
                                y;
                            break;


                        case 1:
                            sourceX =
                                max -
                                y;

                            sourceY =
                                x;
                            break;


                        case 2:
                            sourceX =
                                max -
                                x;

                            sourceY =
                                max -
                                y;
                            break;


                        case 3:
                            sourceX =
                                y;

                            sourceY =
                                max -
                                x;
                            break;
                    }


                    if (mirrored)
                    {
                        sourceX =
                            max -
                            sourceX;
                    }


                    result[
                        x +
                        y *
                        pixelSize
                    ] =
                        source[
                            sourceX +
                            sourceY *
                            pixelSize
                        ];
                }
            }


            texture.SetPixels(
                startX,
                startY,
                pixelSize,
                pixelSize,
                result
            );
        }


        private static long Key(
            int x,
            int y
        )
        {
            unchecked
            {
                return
                    (
                        (long)x <<
                        32
                    )
                    ^
                    (uint)y;
            }
        }
    }
}
