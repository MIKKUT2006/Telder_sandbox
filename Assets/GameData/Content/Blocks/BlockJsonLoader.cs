using System.IO;
using Newtonsoft.Json;
using Game.Blocks;


namespace Game.Content
{

    public static class BlockJsonLoader
    {

        public static BlockDefinition Load(
            string path
        )
        {

            if (!File.Exists(path))
            {
                return null;
            }


            string json =
                File.ReadAllText(path);



            BlockDefinition block =
                JsonConvert.DeserializeObject<BlockDefinition>(
                    json
                );



            return block;

        }

    }

}