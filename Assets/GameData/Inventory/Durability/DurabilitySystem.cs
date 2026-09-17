using System;

using UnityEngine;

using Game.Inventory;


namespace Game.Items.Durability
{
    public static class DurabilitySystem
    {
        public const string ToolArtifactTag =
            "tool_artifact";


        public static bool HasDurability(
            ItemStack stack
        )
        {
            return
                stack !=
                null
                &&
                !stack.IsEmpty
                &&
                ItemDurabilityMetadata
                    .GetDurability(
                        stack.ItemId
                    ) >
                0;
        }


        public static int GetBaseMaxDurability(
            ItemStack stack
        )
        {
            if (
                stack ==
                null
                ||
                stack.IsEmpty
            )
            {
                return 0;
            }


            return
                ItemDurabilityMetadata
                    .GetDurability(
                        stack.ItemId
                    );
        }


        public static int GetMaxDurability(
            ItemStack stack
        )
        {
            int baseValue =
                GetBaseMaxDurability(
                    stack
                );


            if (
                baseValue <=
                0
            )
            {
                return 0;
            }


            return
                Mathf.Max(
                    1,
                    baseValue +
                    Mathf.Max(
                        0,
                        stack.BonusMaxDurability
                    )
                );
        }


        public static void EnsureInitialized(
            ItemStack stack
        )
        {
            if (
                stack ==
                null
                ||
                stack.IsEmpty
            )
            {
                return;
            }


            int max =
                GetMaxDurability(
                    stack
                );


            if (
                max <=
                0
            )
            {
                return;
            }


            if (
                stack.CurrentDurability <
                0
            )
            {
                stack.CurrentDurability =
                    max;
            }


            stack.CurrentDurability =
                Mathf.Clamp(
                    stack.CurrentDurability,
                    0,
                    max
                );
        }


        public static int GetCurrentDurability(
            ItemStack stack
        )
        {
            EnsureInitialized(
                stack
            );


            return
                stack !=
                null
                    ? Mathf.Max(
                        0,
                        stack.CurrentDurability
                    )
                    : 0;
        }


        public static float GetNormalized(
            ItemStack stack
        )
        {
            int max =
                GetMaxDurability(
                    stack
                );


            if (
                max <=
                0
            )
            {
                return 1f;
            }


            EnsureInitialized(
                stack
            );


            return
                Mathf.Clamp01(
                    stack.CurrentDurability /
                    (float)max
                );
        }


        public static bool IsBroken(
            ItemStack stack
        )
        {
            if (
                !HasDurability(
                    stack
                )
            )
            {
                return false;
            }


            EnsureInitialized(
                stack
            );


            return
                stack.CurrentDurability <=
                0;
        }


        public static bool Damage(
            ItemStack stack,
            int amount = 1
        )
        {
            if (
                !HasDurability(
                    stack
                )
                ||
                amount <=
                0
            )
            {
                return false;
            }


            EnsureInitialized(
                stack
            );


            stack.CurrentDurability =
                Mathf.Max(
                    0,
                    stack.CurrentDurability -
                    amount
                );


            return
                stack.CurrentDurability <=
                0;
        }


        public static bool RepairFully(
            ItemStack stack
        )
        {
            int max =
                GetMaxDurability(
                    stack
                );


            if (
                max <=
                0
            )
            {
                return false;
            }


            stack.CurrentDurability =
                max;


            return true;
        }


        public static bool TryGetRepairRecipe(
            ItemStack tool,
            out string repairItemId,
            out int fullRepairCost
        )
        {
            repairItemId =
                "";


            fullRepairCost =
                0;


            if (
                tool ==
                null
                ||
                tool.IsEmpty
                ||
                !ItemDurabilityMetadata.TryGet(
                    tool.ItemId,
                    out ItemDurabilityMetadataData data
                )
            )
            {
                return false;
            }


            repairItemId =
                data.RepairItem;


            fullRepairCost =
                Mathf.Max(
                    0,
                    data.RepairItemCount
                );


            return
                !string.IsNullOrWhiteSpace(
                    repairItemId
                )
                &&
                fullRepairCost >
                0;
        }


        public static int GetRepairCost(
            ItemStack tool
        )
        {
            if (
                !HasDurability(
                    tool
                )
            )
            {
                return 0;
            }


            EnsureInitialized(
                tool
            );


            int max =
                GetMaxDurability(
                    tool
                );


            if (
                max <=
                0
                ||
                tool.CurrentDurability >=
                max
            )
            {
                return 0;
            }


            if (
                !TryGetRepairRecipe(
                    tool,
                    out string repairItem,
                    out int fullRepairCost
                )
            )
            {
                return 0;
            }


            float missingFraction =
                1f -
                GetNormalized(
                    tool
                );


            return
                Mathf.Clamp(
                    Mathf.CeilToInt(
                        fullRepairCost *
                        missingFraction
                    ),
                    1,
                    fullRepairCost
                );
        }


        public static bool IsToolArtifact(
            ItemStack stack
        )
        {
            if (
                stack ==
                null
                ||
                stack.IsEmpty
            )
            {
                return false;
            }


            if (
                ItemDurabilityMetadata
                    .HasTag(
                        stack.ItemId,
                        ToolArtifactTag
                    )
            )
            {
                return true;
            }


            return
                ItemDurabilityMetadata.TryGet(
                    stack.ItemId,
                    out ItemDurabilityMetadataData data
                )
                &&
                !string.IsNullOrWhiteSpace(
                    data.UpgradeType
                );
        }


        public static bool CanApplyUpgrade(
            ItemStack tool,
            ItemStack artifact,
            out string reason
        )
        {
            reason =
                "";


            if (
                !HasDurability(
                    tool
                )
            )
            {
                reason =
                    "В левом слоте должен быть предмет с прочностью.";

                return false;
            }


            if (
                !IsToolArtifact(
                    artifact
                )
            )
            {
                reason =
                    "Во втором слоте нужен артефакт улучшения.";

                return false;
            }


            if (
                !string.IsNullOrWhiteSpace(
                    tool.InstalledUpgradeId
                )
            )
            {
                reason =
                    "У этого предмета уже установлено улучшение.";

                return false;
            }


            if (
                !ItemDurabilityMetadata.TryGet(
                    artifact.ItemId,
                    out ItemDurabilityMetadataData data
                )
            )
            {
                reason =
                    "Данные артефакта не найдены.";

                return false;
            }


            if (
                !string.Equals(
                    data.UpgradeType,
                    "DurabilityFlat",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                reason =
                    "Пока поддерживается UpgradeType = DurabilityFlat.";

                return false;
            }


            if (
                data.UpgradeValue <=
                0
            )
            {
                reason =
                    "UpgradeValue должен быть больше 0.";

                return false;
            }


            return true;
        }


        public static bool ApplyUpgrade(
            ItemStack tool,
            ItemStack artifact,
            out string reason
        )
        {
            if (
                !CanApplyUpgrade(
                    tool,
                    artifact,
                    out reason
                )
            )
            {
                return false;
            }


            ItemDurabilityMetadata.TryGet(
                artifact.ItemId,
                out ItemDurabilityMetadataData data
            );


            EnsureInitialized(
                tool
            );


            int value =
                Mathf.Max(
                    0,
                    data.UpgradeValue
                );


            tool.BonusMaxDurability +=
                value;


            tool.CurrentDurability +=
                value;


            tool.InstalledUpgradeId =
                artifact.ItemId;


            artifact.Count -=
                1;


            reason =
                "Улучшение установлено.";


            return true;
        }
    }
}
