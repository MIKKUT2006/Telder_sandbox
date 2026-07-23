using System.Collections.Generic;
using Game.Blocks;
using UnityEngine;


namespace Game.Content
{

    public static class BlockRegistry
    {

        private static Dictionary<ContentID, BlockDefinition> blocks =
            new Dictionary<ContentID, BlockDefinition>();



        public static void Register(BlockDefinition block)
        {
            Debug.Log(
                "BLOCK REGISTRY REGISTER: " + block.ID
            );


            ContentID id =
                ContentID.Parse(
                    block.ID
                );


            Debug.Log(
                "PARSED CONTENT ID: " + id
            );


            blocks[id] = block;


            Debug.Log(
                "CALLING BLOCK ID REGISTRY"
            );


            BlockIDRegistry.Register(
                id
            );

        }





        public static BlockDefinition Get(
            ContentID id
        )
        {

            return blocks[id];

        }





        public static bool Contains(
            ContentID id
        )
        {

            return blocks.ContainsKey(id);

        }





        public static IEnumerable<BlockDefinition> GetAll()
        {

            return blocks.Values;

        }

    }

}