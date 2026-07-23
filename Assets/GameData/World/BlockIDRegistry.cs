//using System.Collections.Generic;
//using Game.Content;


//namespace Game.World
//{

//    public static class BlockIDRegistry
//    {

//        private static Dictionary<ContentID, ushort> idByBlock =
//            new Dictionary<ContentID, ushort>();


//        private static Dictionary<ushort, ContentID> blockByID =
//            new Dictionary<ushort, ContentID>();



//        private static ushort nextID = 1;



//        public static void Register(
//            ContentID blockID
//        )
//        {

//            if (idByBlock.ContainsKey(blockID))
//                return;



//            ushort numericID = nextID;


//            nextID++;



//            idByBlock.Add(
//                blockID,
//                numericID
//            );



//            blockByID.Add(
//                numericID,
//                blockID
//            );

//        }





//        public static ushort GetID(
//            ContentID blockID
//        )
//        {

//            return idByBlock[blockID];

//        }





//        public static ContentID GetContentID(
//            ushort id
//        )
//        {

//            return blockByID[id];

//        }





//        public static bool Contains(
//            ContentID blockID
//        )
//        {

//            return idByBlock.ContainsKey(
//                blockID
//            );

//        }





//        public static void Clear()
//        {

//            idByBlock.Clear();

//            blockByID.Clear();


//            nextID = 1;

//        }


//    }

//}