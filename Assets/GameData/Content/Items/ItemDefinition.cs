using System;
using System.Collections.Generic;
using Game.Content;


namespace Game.Items
{

    [Serializable]
    public class ItemDefinition
    {

        public ContentID ID;


        public string Name;


        public string Texture;


        public int MaxStack;


        public ItemType Type;


        public int Durability;


        public int Damage;


        public List<string> Tags;



        public ItemDefinition()
        {

            MaxStack = 64;

            Tags = new List<string>();

        }

    }

}