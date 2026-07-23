using Game.Blocks;
using Game.World;
using System.Linq;
using UnityEngine;


namespace Game.Content
{

    public static class BlockDatabaseLoader
    {

        public static void Load()
        {

            foreach (BlockDefinition block in BlockRegistry.GetAll())
            {

                ushort id =
                    BlockIDRegistry.GetID(
                        ContentID.Parse(
                            block.ID
                        )
                    );


                BlockDatabase.Register(
                    id,
                    block
                );

            }


            Debug.Log(
                "Blocks loaded: " +
                BlockRegistry.GetAll().Count()
            );

        }

    }

}