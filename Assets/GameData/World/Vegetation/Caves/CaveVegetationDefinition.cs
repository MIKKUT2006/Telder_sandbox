using System;
using System.Collections.Generic;

namespace Game.World.Vegetation.Caves
{
    [Serializable]
    public sealed class CaveVegetationDefinition
    {
        public string ID = "game:cave_vegetation";

        public string SpawnType = "Cave";

        public string BlockId;

        public List<string> Biomes =
            new List<string>();

        public float Chance = 0.08f;

        public int MinY = -1000000;

        public int MaxY = 1000000;

        public bool Floor = true;

        public bool Ceiling = false;

        public bool Walls = false;
    }
}
