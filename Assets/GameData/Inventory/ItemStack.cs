using System;
using UnityEngine;


namespace Game.Inventory
{

    [Serializable]
    public class ItemStack
    {

        [SerializeField]
        private string itemId;


        [SerializeField]
        private int count;


        public string ItemId =>
            itemId;


        public int Count
        {
            get =>
                count;

            set
            {

                count =
                    Mathf.Max(
                        0,
                        value
                    );


                if (
                    count <= 0
                )
                {

                    Clear();

                }

            }
        }


        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(
                itemId
            )
            ||
            count <= 0;


        // =====================================================
        // SET
        // =====================================================

        public void Set(
            string id,
            int amount
        )
        {

            if (
                string.IsNullOrWhiteSpace(
                    id
                )
                ||
                amount <= 0
            )
            {

                Clear();

                return;

            }


            itemId =
                id;


            count =
                amount;

        }


        public void Set(
            ItemStack other
        )
        {

            if (
                other == null
                ||
                other.IsEmpty
            )
            {

                Clear();

                return;

            }


            Set(
                other.ItemId,
                other.Count
            );

        }


        // =====================================================
        // CLEAR
        // =====================================================

        public void Clear()
        {

            itemId =
                null;


            count =
                0;

        }

    }

}
