using System.Collections.Generic;
using UnityEngine;


namespace Game.Content
{

    public static class BlockIDRegistry
    {

        private static Dictionary<ContentID, ushort> idByBlock =
            new Dictionary<ContentID, ushort>();


        private static Dictionary<ushort, ContentID> blockByID =
            new Dictionary<ushort, ContentID>();



        private static ushort nextID = 1;





        public static ushort Register(
            ContentID id
        )
        {
            Debug.Log(
    "BLOCK ID REGISTRY INSTANCE: " +
    typeof(BlockIDRegistry).FullName
);
            Debug.Log(
        "TRY REGISTER ID: " + id
    );

            if (idByBlock.ContainsKey(id))
            {
                return idByBlock[id];
            }



            ushort newID =
                nextID;



            nextID++;

            

            idByBlock.Add(
                id,
                newID
            );


            blockByID.Add(
                newID,
                id
            );

            Debug.Log(
    "Registered ID: " + id + " = " + newID
);
            return newID;

        }





        public static ushort GetID(ContentID id)
        {

            //Debug.Log(
            //    "GET ID REQUEST: " + id
            //);


            //Debug.Log(
            //    "REGISTERED IDS COUNT: " +
            //    idByBlock.Count
            //);


            if (!idByBlock.ContainsKey(id))
            {

                Debug.LogError(
                    "BLOCK ID NOT FOUND: " + id
                );


                foreach (
                    var pair in idByBlock
                )
                {

                    Debug.Log(
                        "REGISTERED: " +
                        pair.Key +
                        " = " +
                        pair.Value
                    );

                }


                throw new KeyNotFoundException(
                    "Block ID not found: " + id
                );

            }


            return idByBlock[id];

        }





        public static ContentID GetContentID(
            ushort id
        )
        {

            return blockByID[id];

        }





        public static bool Contains(
    ContentID id
)
        {
            return idByBlock.ContainsKey(id);
        }

        public static bool Contains(
    ushort id
)
        {
            return blockByID.ContainsKey(
                id
            );
        }



        public static void Clear()
        {

            idByBlock.Clear();

            blockByID.Clear();

            nextID = 1;

        }

    }

}