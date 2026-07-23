using System;
using System.Collections.Generic;


namespace Game.Mods
{

    [Serializable]
    public class ModDefinition
    {

        public string ID;


        public string Name;


        public string Author;


        public string Version;


        public string Description;


        public List<string> Dependencies;



        public ModDefinition()
        {

            Dependencies = new List<string>();

        }


    }

}