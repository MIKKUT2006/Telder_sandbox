using UnityEngine;

namespace Game.World.Lighting
{
    public class ChunkLightData
    {
        private readonly byte[] sunlight;
        private readonly byte[] red;
        private readonly byte[] green;
        private readonly byte[] blue;

        private readonly int width;
        private readonly int height;


        public ChunkLightData(
            int width,
            int height
        )
        {
            this.width = width;
            this.height = height;

            int size =
                width *
                height;

            sunlight =
                new byte[size];

            red =
                new byte[size];

            green =
                new byte[size];

            blue =
                new byte[size];
        }


        // =====================================================
        // SUNLIGHT
        // =====================================================

        public byte GetSunlight(
            int x,
            int y
        )
        {
            return sunlight[
                y * width + x
            ];
        }


        public void SetSunlight(
            int x,
            int y,
            byte value
        )
        {
            sunlight[
                y * width + x
            ] = value;
        }


        // =====================================================
        // RED
        // =====================================================

        public byte GetRed(
            int x,
            int y
        )
        {
            return red[
                y * width + x
            ];
        }


        public void SetRed(
            int x,
            int y,
            byte value
        )
        {
            red[
                y * width + x
            ] = value;
        }


        // =====================================================
        // GREEN
        // =====================================================

        public byte GetGreen(
            int x,
            int y
        )
        {
            return green[
                y * width + x
            ];
        }


        public void SetGreen(
            int x,
            int y,
            byte value
        )
        {
            green[
                y * width + x
            ] = value;
        }


        // =====================================================
        // BLUE
        // =====================================================

        public byte GetBlue(
            int x,
            int y
        )
        {
            return blue[
                y * width + x
            ];
        }


        public void SetBlue(
            int x,
            int y,
            byte value
        )
        {
            blue[
                y * width + x
            ] = value;
        }


        // =====================================================
        // RGB
        // =====================================================

        public Color32 GetColor(
            int x,
            int y
        )
        {
            return new Color32(
                red[y * width + x],
                green[y * width + x],
                blue[y * width + x],
                255
            );
        }


        public void SetColor(
            int x,
            int y,
            byte r,
            byte g,
            byte b
        )
        {
            int index =
                y * width + x;

            red[index] = r;
            green[index] = g;
            blue[index] = b;
        }


        // =====================================================
        // CLEAR
        // =====================================================

        public void Clear()
        {
            System.Array.Clear(
                sunlight,
                0,
                sunlight.Length
            );

            System.Array.Clear(
                red,
                0,
                red.Length
            );

            System.Array.Clear(
                green,
                0,
                green.Length
            );

            System.Array.Clear(
                blue,
                0,
                blue.Length
            );
        }


        // =====================================================
        // CLEAR SUNLIGHT ONLY
        // =====================================================

        public void ClearSunlight()
        {
            System.Array.Clear(
                sunlight,
                0,
                sunlight.Length
            );
        }


        // =====================================================
        // CLEAR RGB ONLY
        // =====================================================

        public void ClearRGB()
        {
            System.Array.Clear(
                red,
                0,
                red.Length
            );

            System.Array.Clear(
                green,
                0,
                green.Length
            );

            System.Array.Clear(
                blue,
                0,
                blue.Length
            );
        }


        // =====================================================
        // SIZE
        // =====================================================

        public int Width
        {
            get
            {
                return width;
            }
        }


        public int Height
        {
            get
            {
                return height;
            }
        }
    }
}