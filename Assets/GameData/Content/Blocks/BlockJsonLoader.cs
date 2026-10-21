using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Game.Blocks;


namespace Game.Content
{

    public static class BlockJsonLoader
    {

        /// <summary>
        /// Unity's inline serialization is not a reliable presence check: absent
        /// Plant/Cauldron fields can become default instances. Read these optional
        /// sections from the source JSON and retain null when they are absent.
        /// </summary>
        internal static void ApplyGameplaySections(BlockDefinition block, string json)
        {
            if (block == null) return;
            ApplyGameplaySections(block, JObject.Parse(json));
        }

        private static void ApplyGameplaySections(BlockDefinition block, JObject root)
        {
            // An empty object opts in with defaults; null, scalars and arrays do not.
            // Deserialize only these sections through JSON.NET so their field
            // initializers (20 minutes, stage textures, loot chance) stay intact.
            JObject plant = root.GetValue("Plant", System.StringComparison.OrdinalIgnoreCase) as JObject;
            JObject cauldron = root.GetValue("Cauldron", System.StringComparison.OrdinalIgnoreCase) as JObject;
            block.Plant = plant != null
                ? plant.ToObject<Game.Gameplay.Plants.PlantDefinition>()
                : null;
            block.Cauldron = cauldron != null
                ? cauldron.ToObject<Game.Gameplay.Cooking.CauldronDefinition>()
                : null;
        }

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

            if (block != null)
            {
                try
                {
                    JObject root = JObject.Parse(json);
                    block.HasLootTable = root.Property("Loot") != null;
                    ApplyGameplaySections(block, root);
                }
                catch
                {
                    block.HasLootTable = block.Loot != null && block.Loot.Count > 0;
                }
            }

            return block;

        }

    }

}