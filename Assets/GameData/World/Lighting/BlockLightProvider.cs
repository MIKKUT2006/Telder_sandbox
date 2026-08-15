using Game.Blocks;
using Game.Content;

namespace Game.World.Lighting
{
    public static class BlockLightProvider
    {
        public static BlockLightInfo Get(
            ushort blockID
        )
        {
            // -------------------------------------------------
            // ID 0 = пустая клетка / воздух
            // -------------------------------------------------

            if (blockID == 0)
            {
                return new BlockLightInfo(
                    0,
                    0,
                    0,
                    0,
                    false
                );
            }


            // -------------------------------------------------
            // Получаем ContentID
            // -------------------------------------------------

            if (!TryGetContentID(
                blockID,
                out ContentID contentID
            ))
            {
                // Неизвестный блок считаем полностью
                // непрозрачным для безопасности.
                return new BlockLightInfo(
                    15,
                    0,
                    0,
                    0,
                    true
                );
            }


            // -------------------------------------------------
            // Получаем BlockDefinition
            // -------------------------------------------------

            if (!BlockRegistry.Contains(
                contentID
            ))
            {
                return new BlockLightInfo(
                    15,
                    0,
                    0,
                    0,
                    true
                );
            }


            BlockDefinition block =
                BlockRegistry.Get(
                    contentID
                );


            // -------------------------------------------------
            // Прозрачность
            // -------------------------------------------------

            byte opacity =
                block.LightOpacity;


            if (!block.BlocksLight)
            {
                opacity = 0;
            }


            // -------------------------------------------------
            // RGB emission
            // -------------------------------------------------

            return new BlockLightInfo(
                opacity,

                block.LightEmissionR,
                block.LightEmissionG,
                block.LightEmissionB,

                block.BlocksLight
            );
        }


        private static bool TryGetContentID(
            ushort blockID,
            out ContentID contentID
        )
        {
            contentID =
                default;


            if (blockID == 0)
            {
                return false;
            }


            try
            {
                contentID =
                    BlockIDRegistry.GetContentID(
                        blockID
                    );

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}