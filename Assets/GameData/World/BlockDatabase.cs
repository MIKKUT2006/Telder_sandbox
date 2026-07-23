using System.Collections.Generic;
using Game.Blocks;


namespace Game.World
{

    public static class BlockDatabase
    {

        private static Dictionary<ushort, BlockDefinition> blocks =
            new Dictionary<ushort, BlockDefinition>();



        public static void Register(
            ushort id,
            BlockDefinition block
        )
        {

            blocks[id] = block;

        }





        public static BlockDefinition Get(
            ushort id
        )
        {

            return blocks[id];

        }





        public static bool Contains(
            ushort id
        )
        {

            return blocks.ContainsKey(id);

        }

    }

}