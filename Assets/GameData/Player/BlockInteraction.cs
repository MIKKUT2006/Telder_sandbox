using Game.Blocks;
using Game.Content;
using Game.Inventory;
using Game.Inventory.UI;
using Game.Mining;
using Game.World;
using Game.World.Effects;
using Game.World.Items;
using Game.World.Structures;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;


public class BlockInteraction :
    MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField]
    private float interactionDistance = 6f;

    [Header("Mining")]
    [SerializeField]
    private float baseMiningPower = 1f;

    [SerializeField]
    private float miningHitInterval = 0.12f;

    [SerializeField]
    private float minimumMiningHitInterval = 0.055f;

    [SerializeField]
    private float defaultBlockHardness = 1f;

    [SerializeField]
    private float minimumBlockHardness = 0.05f;

    [Header("Place")]
    [SerializeField]
    private float placeInterval = 0.08f;

    [Header("Camera")]
    [SerializeField]
    private Camera playerCamera;


    private WorldManager worldManager;
    private World world;
    private PlayerCollision playerCollision;
    private PlayerInventory inventory;

    private bool initialized;
    private float nextPlaceTime;

    private bool hasMiningTarget;
    private int miningX;
    private int miningY;
    private ushort miningBlockID;
    private bool miningBackground;
    private float miningProgress;
    private float currentMiningHardness;
    private float nextMiningHitTime;

    private readonly Dictionary<ushort, float>
        hardnessCache =
            new Dictionary<ushort, float>();

    [SerializeField]
    private Game.PlayerStats.PlayerStats playerStats;
    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null)
        {
            Debug.LogError(
                "BLOCK INTERACTION: Camera not found."
            );
            return;
        }

        worldManager =
            WorldManager.Instance;

        if (worldManager == null)
        {
            Debug.LogError(
                "BLOCK INTERACTION: WorldManager is null."
            );
            return;
        }

        world =
            worldManager.GetWorld();

        if (world == null)
        {
            Debug.LogError(
                "BLOCK INTERACTION: World is null."
            );
            return;
        }

        playerCollision =
            GetComponent<PlayerCollision>();

        inventory =
            GetComponent<PlayerInventory>();

        nextPlaceTime = 0f;

        ResetMining();

        MiningMetadataRegistry.Reload();

        if (playerStats == null)
        {
            playerStats =
                FindFirstObjectByType<Game.PlayerStats.PlayerStats>();
        }

        initialized = true;

        Debug.Log(
            "BLOCK INTERACTION: INITIALIZED."
        );

    }


    private void Update()
    {
        if (
            InventoryUI.Instance != null &&
            InventoryUI.Instance.IsOpen
        )
        {
            ResetMining();
            return;
        }

        if (!initialized)
            return;

        // [BT-AUTO-GAMEPLAY]
        if (
            Game.BlockTransforms
                .BlockTransformGameplayController
                .UpdateAndConsume(
                    worldManager,
                    world,
                    playerCamera,
                    transform,
                    interactionDistance
                )
        )
        {
            ResetMining();
            nextPlaceTime = 0f;
            return;
        }

        if (Input.GetMouseButton(0))
            ProcessMining();
        else
            ResetMining();

        if (Input.GetMouseButton(1))
        {
            if (Time.time >= nextPlaceTime)
            {
                PlaceBlock();
                nextPlaceTime =
                    Time.time +
                    placeInterval;
            }
        }
        else
        {
            nextPlaceTime = 0f;
        }
    }


    // =====================================================
    // MINING
    // =====================================================

    private void ProcessMining()
    {
        if (
            !TryGetMiningTarget(
                out UnityEngine.Vector2Int position,
                out ushort blockID,
                out bool background
            )
        )
        {
            ResetMining();
            return;
        }

        if (
            !MiningToolRules.CanBreak(
                blockID,
                inventory
            )
        )
        {
            ResetMining();
            return;
        }

        MiningVisualSignal.Pulse();

        bool targetChanged =
            !hasMiningTarget ||
            miningX != position.x ||
            miningY != position.y ||
            miningBlockID != blockID ||
            miningBackground != background;

        if (targetChanged)
        {
            BeginMiningTarget(
                position.x,
                position.y,
                blockID,
                background
            );
        }

        float toolMiningSpeed =
            MiningToolRules
                .GetSelectedMiningSpeed(
                    inventory
                );

        toolMiningSpeed =
            Mathf.Max(
                0.05f,
                toolMiningSpeed
            );

        float workPerSecond =
            Mathf.Max(
                0.01f,
                baseMiningPower
            ) *
            toolMiningSpeed;

        miningProgress +=
            workPerSecond *
            Time.deltaTime;

        EmitMiningHitIfNeeded(
            toolMiningSpeed
        );

        if (
            miningProgress <
            currentMiningHardness
        )
        {
            return;
        }

        CompleteMiningTarget();
    }


    private bool TryGetMiningTarget(
        out UnityEngine.Vector2Int position,
        out ushort blockID,
        out bool background
    )
    {
        position =
            UnityEngine.Vector2Int.zero;

        blockID = 0;
        background = false;

        if (!TryGetMouseCell(out position))
            return false;

        ushort foregroundID =
            world.GetBlock(
                position.x,
                position.y
            );

        if (foregroundID != 0)
        {
            blockID = foregroundID;
            background = false;
            return true;
        }

        ushort backgroundID =
            world.GetBackground(
                position.x,
                position.y
            );

        if (backgroundID == 0)
            return false;

        blockID = backgroundID;
        background = true;
        return true;
    }


    private void BeginMiningTarget(
        int x,
        int y,
        ushort blockID,
        bool background
    )
    {
        hasMiningTarget = true;

        miningX = x;
        miningY = y;
        miningBlockID = blockID;
        miningBackground = background;

        miningProgress = 0f;

        currentMiningHardness =
            GetBlockHardness(
                blockID
            );

        nextMiningHitTime = 0f;
    }


    private void ResetMining()
    {
        hasMiningTarget = false;

        miningX = 0;
        miningY = 0;
        miningBlockID = 0;
        miningBackground = false;

        miningProgress = 0f;
        currentMiningHardness = 0f;
        nextMiningHitTime = 0f;
    }


    private void EmitMiningHitIfNeeded(
        float toolMiningSpeed
    )
    {
        if (!hasMiningTarget)
            return;

        if (Time.time < nextMiningHitTime)
            return;

        BlockBreakDebrisSystem
            .EmitMiningHit(
                world,
                miningX,
                miningY,
                miningBlockID,
                miningBackground
            );

        float interval =
            miningHitInterval /
            Mathf.Max(
                0.25f,
                toolMiningSpeed
            );

        interval =
            Mathf.Max(
                minimumMiningHitInterval,
                interval
            );

        nextMiningHitTime =
            Time.time +
            interval;
    }


    private bool CompleteMiningTarget()
    {
        if (!hasMiningTarget)
            return false;

        int x = miningX;
        int y = miningY;
        ushort expectedID = miningBlockID;
        bool background = miningBackground;

        if (!background)
        {
            ushort currentID =
                world.GetBlock(
                    x,
                    y
                );

            if (
                currentID != expectedID ||
                currentID == 0
            )
            {
                ResetMining();
                return false;
            }

            if (
                StructureCascadeBreakRuntime
                    .TryBreakTreeFromTrunk(
                        worldManager,
                        world,
                        x,
                        y,
                        currentID
                    )
            )
            {
                ForcePlayerCollisionUpdate();
                ResetMining();
                return true;
            }

            bool foregroundChanged =
                worldManager.SetBlock(
                    x,
                    y,
                    0
                );

            if (foregroundChanged)
            {
                SpawnBlockDrop(
                    currentID,
                    x,
                    y
                );

                ForcePlayerCollisionUpdate();
            }

            ResetMining();
            return foregroundChanged;
        }

        ushort currentBackgroundID =
            world.GetBackground(
                x,
                y
            );

        if (
            currentBackgroundID != expectedID ||
            currentBackgroundID == 0
        )
        {
            ResetMining();
            return false;
        }

        bool backgroundChanged =
            worldManager.SetBackground(
                x,
                y,
                0
            );

        if (backgroundChanged)
        {
            SpawnBlockDrop(
                currentBackgroundID,
                x,
                y
            );
        }

        ResetMining();
        return backgroundChanged;
    }


    // =====================================================
    // HARDNESS
    // =====================================================

    private float GetBlockHardness(
        ushort blockID
    )
    {
        if (blockID == 0)
        {
            return Mathf.Max(
                minimumBlockHardness,
                defaultBlockHardness
            );
        }

        if (
            hardnessCache.TryGetValue(
                blockID,
                out float cached
            )
        )
        {
            return cached;
        }

        float hardness =
            defaultBlockHardness;

        BlockDefinition block =
            GetBlockDefinition(
                blockID
            );

        if (block != null)
        {
            if (
                !TryReadFloatMember(
                    block,
                    "Hardness",
                    out hardness
                ) &&
                !TryReadFloatMember(
                    block,
                    "MiningHardness",
                    out hardness
                ) &&
                !TryReadFloatMember(
                    block,
                    "Durability",
                    out hardness
                ) &&
                !TryReadFloatMember(
                    block,
                    "Strength",
                    out hardness
                )
            )
            {
                hardness =
                    defaultBlockHardness;
            }
        }

        if (
            float.IsNaN(hardness) ||
            float.IsInfinity(hardness)
        )
        {
            hardness =
                defaultBlockHardness;
        }

        hardness =
            Mathf.Max(
                minimumBlockHardness,
                hardness
            );

        hardnessCache[blockID] =
            hardness;

        return hardness;
    }


    private BlockDefinition GetBlockDefinition(
        ushort blockID
    )
    {
        if (blockID == 0)
            return null;

        try
        {
            ContentID contentID =
                BlockIDRegistry
                    .GetContentID(
                        blockID
                    );

            if (
                !BlockRegistry.Contains(
                    contentID
                )
            )
            {
                return null;
            }

            return
                BlockRegistry.Get(
                    contentID
                );
        }
        catch
        {
            return null;
        }
    }


    private bool TryReadFloatMember(
        object instance,
        string memberName,
        out float result
    )
    {
        result = 0f;

        if (
            instance == null ||
            string.IsNullOrWhiteSpace(
                memberName
            )
        )
        {
            return false;
        }

        Type type =
            instance.GetType();

        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.IgnoreCase;

        object raw = null;

        FieldInfo field =
            type.GetField(
                memberName,
                flags
            );

        if (field != null)
        {
            raw =
                field.GetValue(
                    instance
                );
        }
        else
        {
            PropertyInfo property =
                type.GetProperty(
                    memberName,
                    flags
                );

            if (
                property != null &&
                property.CanRead
            )
            {
                raw =
                    property.GetValue(
                        instance,
                        null
                    );
            }
        }

        if (raw == null)
            return false;

        if (raw is float floatValue)
        {
            result = floatValue;
            return true;
        }

        if (raw is double doubleValue)
        {
            result = (float)doubleValue;
            return true;
        }

        if (raw is int intValue)
        {
            result = intValue;
            return true;
        }

        return
            float.TryParse(
                raw.ToString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out result
            );
    }


    // =====================================================
    // BLOCK DROP
    // =====================================================

    private void SpawnBlockDrop(
        ushort blockID,
        int worldX,
        int worldY
    )
    {
        if (blockID == 0)
            return;

        if (ItemDropSpawner.Instance == null)
        {
            Debug.LogWarning(
                "MINING: ItemDropSpawner.Instance is null. " +
                "Block was removed but its item cannot be spawned."
            );
            return;
        }

        ContentID contentID;

        try
        {
            contentID =
                BlockIDRegistry.GetContentID(
                    blockID
                );
        }
        catch
        {
            return;
        }

        if (!BlockRegistry.Contains(contentID))
            return;

        BlockDefinition block =
            BlockRegistry.Get(contentID);

        if (block == null)
            return;

        int count =
            block.DropCount;

        if (count <= 0)
            return;

        string dropId = null;

        try
        {
            dropId =
                block.Drop.ToString();
        }
        catch
        {
        }

        if (string.IsNullOrWhiteSpace(dropId))
            dropId = contentID.ToString();

        ItemDropSpawner.Instance
            .SpawnFromBlock(
                dropId,
                count,
                new Vector2(
                    worldX + 0.5f,
                    worldY + 0.55f
                )
            );
    }


    // =====================================================
    // PLACE BLOCK
    // =====================================================

    private bool PlaceBlock()
    {
        if (
            !TryGetSelectedBlockID(
                out ushort selectedBlockID
            )
        )
        {
            return false;
        }

        if (
            !TryGetMouseCell(
                out UnityEngine.Vector2Int placePosition
            )
        )
        {
            return false;
        }

        int x = placePosition.x;
        int y = placePosition.y;

        ushort foregroundID =
            world.GetBlock(
                x,
                y
            );

        if (foregroundID != 0)
            return false;

        if (IsInsidePlayer(placePosition))
            return false;

        ushort backgroundID =
            world.GetBackground(
                x,
                y
            );

        bool canPlace =
            backgroundID != 0 ||
            HasAdjacentForegroundBlock(
                x,
                y
            );

        if (!canPlace)
            return false;

        bool placed =
            worldManager.SetBlock(
                x,
                y,
                selectedBlockID
            );

        if (!placed)
            return false;

        if (inventory != null)
        {
            bool consumed =
                inventory.TryConsumeSelected(
                    1
                );

            if (!consumed)
            {
                Debug.LogWarning(
                    "BLOCK INTERACTION: Block was placed, " +
                    "but selected inventory item could not be consumed."
                );
            }
        }

        return true;
    }


    private bool TryGetSelectedBlockID(
        out ushort blockID
    )
    {
        blockID = 0;

        if (inventory == null)
            return false;

        string selectedItemID =
            inventory.GetSelectedItemId();

        if (
            string.IsNullOrWhiteSpace(
                selectedItemID
            )
        )
        {
            return false;
        }

        ContentID contentID;

        try
        {
            contentID =
                ContentID.Parse(
                    selectedItemID
                );
        }
        catch
        {
            return false;
        }

        if (
            !BlockIDRegistry.Contains(
                contentID
            )
        )
        {
            return false;
        }

        blockID =
            BlockIDRegistry.GetID(
                contentID
            );

        return blockID != 0;
    }


    // =====================================================
    // MOUSE CELL
    // =====================================================

    private bool TryGetMouseCell(
        out UnityEngine.Vector2Int cell
    )
    {
        cell =
            UnityEngine.Vector2Int.zero;

        if (
            playerCamera == null ||
            world == null
        )
        {
            return false;
        }

        Vector2 mouseWorld =
            GetMouseWorldPosition();

        int x =
            Mathf.FloorToInt(
                mouseWorld.x
            );

        int y =
            Mathf.FloorToInt(
                mouseWorld.y
            );

        cell =
            new UnityEngine.Vector2Int(
                x,
                y
            );

        Vector2 cellCenter =
            new Vector2(
                x + 0.5f,
                y + 0.5f
            );

        Vector2 playerPosition =
            new Vector2(
                transform.position.x,
                transform.position.y
            );

        return
            Vector2.Distance(
                playerPosition,
                cellCenter
            ) <=
            interactionDistance;
    }


    private Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPosition =
            Input.mousePosition;

        mouseScreenPosition.z =
            Mathf.Abs(
                playerCamera
                    .transform
                    .position
                    .z
            );

        Vector3 mouseWorldPosition =
            playerCamera.ScreenToWorldPoint(
                mouseScreenPosition
            );

        return
            new Vector2(
                mouseWorldPosition.x,
                mouseWorldPosition.y
            );
    }


    // =====================================================
    // ADJACENCY
    // =====================================================

    private bool HasAdjacentForegroundBlock(
        int x,
        int y
    )
    {
        return
            world.GetBlock(x - 1, y) != 0 ||
            world.GetBlock(x + 1, y) != 0 ||
            world.GetBlock(x, y - 1) != 0 ||
            world.GetBlock(x, y + 1) != 0;
    }


    // =====================================================
    // PLAYER COLLISION
    // =====================================================

    private void ForcePlayerCollisionUpdate()
    {
        if (playerCollision == null)
            return;

        playerCollision.ResolveOverlaps();
        playerCollision.ForceGroundCheck();
    }


    private bool IsInsidePlayer(
        UnityEngine.Vector2Int blockPosition
    )
    {
        if (playerCollision == null)
            return false;

        Vector2 playerSize =
            playerCollision.GetColliderSize();

        Vector2 playerPosition =
            transform.position;

        float playerLeft =
            playerPosition.x -
            playerSize.x * 0.5f;

        float playerRight =
            playerPosition.x +
            playerSize.x * 0.5f;

        float playerBottom =
            playerPosition.y -
            playerSize.y * 0.5f;

        float playerTop =
            playerPosition.y +
            playerSize.y * 0.5f;

        float blockLeft =
            blockPosition.x;

        float blockRight =
            blockPosition.x + 1f;

        float blockBottom =
            blockPosition.y;

        float blockTop =
            blockPosition.y + 1f;

        return
            playerRight > blockLeft &&
            playerLeft < blockRight &&
            playerTop > blockBottom &&
            playerBottom < blockTop;
    }
}
