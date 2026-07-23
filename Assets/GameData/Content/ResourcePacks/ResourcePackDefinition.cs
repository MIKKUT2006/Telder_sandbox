using System;
using System.Collections.Generic;


namespace Game.ResourcePacks
{

    [Serializable]
    public class ResourcePackDefinition
    {

        public string ID;


        public string Name;


        public string Author;


        public string Version;


        public string Description;


        public List<string> SupportedVersions;



        public ResourcePackDefinition()
        {

            SupportedVersions = new List<string>();

        }

    }

}