using Game.Content;
using UnityEngine;

namespace Game.World.Collision
{
    public class WorldCollision
    {
        private readonly World world;

        public WorldCollision(
            World world
        )
        {
            this.world =
                world;
        }


        // =====================================================
        // ПРОВЕРКА БЛОКА
        // =====================================================

        public bool IsSolid(
            int worldX,
            int worldY
        )
        {
            ushort blockID =
                GetBlock(
                    worldX,
                    worldY
                );


            // ID 0 = воздух
            if (blockID == 0)
            {
                return false;
            }


            ContentID contentID;

            try
            {
                contentID =
                    BlockIDRegistry.GetContentID(
                        blockID
                    );
            }
            catch
            {
                return false;
            }


            if (
                !BlockRegistry.Contains(
                    contentID
                )
            )
            {
                return false;
            }


            var block =
                BlockRegistry.Get(
                    contentID
                );


            if (block == null)
            {
                return false;
            }


            return block.Solid;
        }


        // =====================================================
        // ПОЛУЧИТЬ БЛОК
        // =====================================================

        public ushort GetBlock(
    int worldX,
    int worldY
)
        {
            return world.GetBlock(
                worldX,
                worldY
            );
        }

    }
}