using System;
using System.Diagnostics;


namespace Game.Content
{

    [Serializable]
    public struct ContentID
    {

        public string Namespace;

        public string Name;



        public ContentID(string namespaceID, string name)
        {
            Namespace = namespaceID;
            Name = name;
        }



        public override string ToString()
        {
            return Namespace + ":" + Name;
        }



        public static ContentID Parse(
     string value
 )
        {

            string[] parts =
                value.Split(':');


            if (parts.Length != 2)
            {
                return default;
            }


            return new ContentID(
                parts[0],
                parts[1]
            );
            

        }



        public override bool Equals(object obj)
        {

            if (obj is not ContentID)
                return false;


            ContentID other = (ContentID)obj;


            return
                Namespace == other.Namespace &&
                Name == other.Name;

        }



        public override int GetHashCode()
        {
            return
                (Namespace + ":" + Name)
                .GetHashCode();
        }



        public static bool operator ==(
            ContentID a,
            ContentID b)
        {
            return a.Equals(b);
        }



        public static bool operator !=(
            ContentID a,
            ContentID b)
        {
            return !a.Equals(b);
        }

    }

}