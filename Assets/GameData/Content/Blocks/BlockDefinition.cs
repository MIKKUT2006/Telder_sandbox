using System;
using System.Collections.Generic;
using Game.Content;


namespace Game.Blocks
{

    [Serializable]
    public class BlockDefinition
    {

        public string ID;


        public string Name;


        public string Texture;



        // Физика блока

        public float Hardness;


        public float ExplosionResistance;



        // Свойства размещения

        public bool Solid;


        public bool Transparent;


        public bool BlocksLight;


        // Освещение 
        public byte LightOpacity;

        public byte LightEmissionR;
        public byte LightEmissionG;
        public byte LightEmissionB;



        // Материал

        public BlockMaterial Material;



        // Что выпадает

        public ContentID Drop;



        // Максимальный размер стака

        public int DropCount;



        // Звук

        public string PlaceSound;


        public string BreakSound;



        // Требуемый инструмент

        public string RequiredTool;



        // Дополнительные свойства

        public List<string> Tags;



        public BlockDefinition()
        {

            Hardness = 1;


            ExplosionResistance = 1;


            Solid = true;


            Transparent = false;


            BlocksLight = true;


            LightOpacity = 15;

            LightEmissionR = 0;
            LightEmissionG = 0;
            LightEmissionB = 0;


            DropCount = 1;


            Tags = new List<string>();

        }

    }

}