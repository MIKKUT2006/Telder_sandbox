
using System;

using Game.Content;
using Game.Inventory;


namespace Game.Mining
{

    public static class MiningToolRules
    {

        public static bool CanBreak(
            ushort blockNumericId,
            PlayerInventory inventory
        )
        {

            if (
                blockNumericId ==
                0
            )
            {

                return false;

            }


            string blockId;


            try
            {

                blockId =
                    BlockIDRegistry
                        .GetContentID(
                            blockNumericId
                        )
                        .ToString();

            }
            catch
            {

                return true;

            }


            if (
                !MiningMetadataRegistry
                    .TryGetBlock(
                        blockId,
                        out BlockMiningMetadata block
                    )
                ||
                block ==
                null
                ||
                string.IsNullOrWhiteSpace(
                    block.RequiredTool
                )
            )
            {

                return true;

            }


            if (
                inventory ==
                null
            )
            {

                return false;

            }


            string selectedItem =
                inventory.GetSelectedItemId();


            if (
                string.IsNullOrWhiteSpace(
                    selectedItem
                )
            )
            {

                return false;

            }


            if (
                !MiningMetadataRegistry
                    .TryGetTool(
                        selectedItem,
                        out ToolMetadata tool
                    )
                ||
                tool ==
                null
            )
            {

                return false;

            }


            if (
                !string.Equals(
                    tool.ToolType,
                    block.RequiredTool,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {

                return false;

            }


            return
                tool.ToolLevel >=
                block.RequiredToolLevel;

        }


        public static float GetSelectedMiningSpeed(
            PlayerInventory inventory
        )
        {

            if (
                inventory ==
                null
            )
            {

                return 1f;

            }


            string selectedItem =
                inventory.GetSelectedItemId();


            if (
                MiningMetadataRegistry.TryGetTool(
                    selectedItem,
                    out ToolMetadata tool
                )
                &&
                tool !=
                null
            )
            {

                return tool.MiningSpeed;

            }


            return 1f;

        }

    }

}
