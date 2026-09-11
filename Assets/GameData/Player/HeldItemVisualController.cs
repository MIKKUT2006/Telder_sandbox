
using UnityEngine;

using Game.Inventory;
using Game.Inventory.UI;
using Game.Items.Visual;


[DefaultExecutionOrder(32000)]
[RequireComponent(
    typeof(
        PlayerInventory
    )
)]
public class HeldItemVisualController :
    MonoBehaviour
{

    // =====================================================
    // REFERENCES
    // =====================================================

    [Header("Existing Rig")]

    [SerializeField]
    private Transform frontArmPivot;


    [SerializeField]
    private Transform handPoint;


    [SerializeField]
    private SpriteRenderer bodyRenderer;


    // =====================================================
    // DEFAULT ITEM POSE
    // =====================================================

    [Header("Held Item")]

    [SerializeField]
    private Vector2 defaultHeldOffset =
        Vector2.zero;


    [SerializeField]
    private float heldWorldSize =
        0.58f;


    [SerializeField]
    private float defaultHeldRotation =
        0f;


    // =====================================================
    // RUNTIME
    // =====================================================

    private PlayerInventory inventory;


    private SpriteRenderer heldRenderer;


    private string currentItemId;


    private HeldItemPoseMetadata currentPose;


    // =====================================================
    // AUTO INSTALL
    // =====================================================

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.AfterSceneLoad
    )]
    private static void EnsureOnPlayer()
    {

        PlayerInventory[] inventories =
            Object.FindObjectsOfType<
                PlayerInventory
            >();


        for (
            int i = 0;
            i < inventories.Length;
            i++
        )
        {

            PlayerInventory playerInventory =
                inventories[i];


            if (
                playerInventory ==
                null
                ||
                playerInventory.GetComponent<
                    HeldItemVisualController
                >() !=
                null
            )
            {

                continue;

            }


            playerInventory.gameObject
                .AddComponent<
                    HeldItemVisualController
                >();

        }

    }


    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {

        inventory =
            GetComponent<
                PlayerInventory
            >();


        AutoFindRig();


        EnsureHeldRenderer();

    }


    private void OnEnable()
    {

        if (
            inventory !=
            null
        )
        {

            inventory.Changed +=
                RefreshHeldItem;

        }


        RefreshHeldItem();

    }


    private void OnDisable()
    {

        if (
            inventory !=
            null
        )
        {

            inventory.Changed -=
                RefreshHeldItem;

        }

    }


    private void LateUpdate()
    {

        if (
            handPoint ==
            null
        )
        {

            AutoFindRig();


            EnsureHeldRenderer();

        }


        if (
            heldRenderer ==
            null
        )
        {

            return;

        }


        UpdateSorting();

    }


    // =====================================================
    // RIG
    // =====================================================

    private void AutoFindRig()
    {

        if (
            frontArmPivot ==
            null
        )
        {

            frontArmPivot =
                FindDeepChild(
                    transform,
                    "FrontArmPivot"
                );

        }


        if (
            handPoint ==
            null
            &&
            frontArmPivot !=
            null
        )
        {

            handPoint =
                FindDeepChild(
                    frontArmPivot,
                    "HandPoint"
                );

        }


        if (
            bodyRenderer ==
            null
        )
        {

            Transform body =
                FindDeepChild(
                    transform,
                    "Body"
                );


            if (
                body !=
                null
            )
            {

                bodyRenderer =
                    body.GetComponent<
                        SpriteRenderer
                    >();

            }

        }

    }


    private static Transform FindDeepChild(
        Transform root,
        string targetName
    )
    {

        if (
            root ==
            null
        )
        {

            return null;

        }


        Transform[] all =
            root.GetComponentsInChildren<
                Transform
            >(
                true
            );


        for (
            int i = 0;
            i < all.Length;
            i++
        )
        {

            Transform candidate =
                all[i];


            if (
                candidate !=
                null
                &&
                candidate.name ==
                targetName
            )
            {

                return candidate;

            }

        }


        return null;

    }


    private void EnsureHeldRenderer()
    {

        if (
            handPoint ==
            null
        )
        {

            return;

        }


        Transform existing =
            handPoint.Find(
                "HeldItem"
            );


        if (
            existing !=
            null
        )
        {

            heldRenderer =
                existing.GetComponent<
                    SpriteRenderer
                >();


            if (
                heldRenderer ==
                null
            )
            {

                heldRenderer =
                    existing.gameObject
                        .AddComponent<
                            SpriteRenderer
                        >();

            }


            return;

        }


        GameObject itemObject =
            new GameObject(
                "HeldItem"
            );


        itemObject.transform.SetParent(
            handPoint,
            false
        );


        heldRenderer =
            itemObject.AddComponent<
                SpriteRenderer
            >();


        heldRenderer.color =
            Color.white;


        heldRenderer.enabled =
            false;


        UpdateSorting();

    }


    // =====================================================
    // ITEM
    // =====================================================

    private void RefreshHeldItem()
    {

        if (
            inventory ==
            null
        )
        {

            return;

        }


        AutoFindRig();

        EnsureHeldRenderer();


        if (
            heldRenderer ==
            null
        )
        {

            return;

        }


        string itemId =
            inventory.GetSelectedItemId();


        if (
            itemId ==
            currentItemId
            &&
            heldRenderer.sprite !=
            null
        )
        {

            return;

        }


        currentItemId =
            itemId;


        currentPose =
            null;


        if (
            string.IsNullOrWhiteSpace(
                itemId
            )
        )
        {

            heldRenderer.sprite =
                null;


            heldRenderer.enabled =
                false;


            return;

        }


        Sprite sprite =
            ItemIconProvider.GetIcon(
                itemId
            );


        heldRenderer.sprite =
            sprite;


        heldRenderer.enabled =
            sprite !=
            null;


        HeldItemPoseRegistry.TryGet(
            itemId,
            out currentPose
        );


        ApplyPose();

    }


    private void ApplyPose()
    {

        if (
            heldRenderer ==
            null
        )
        {

            return;

        }


        Vector2 offset =
            defaultHeldOffset;


        float rotation =
            defaultHeldRotation;


        float poseScale =
            1f;


        if (
            currentPose !=
            null
        )
        {

            offset.x +=
                currentPose.HeldOffsetX;


            offset.y +=
                currentPose.HeldOffsetY;


            rotation +=
                currentPose.HeldRotation;


            poseScale =
                Mathf.Max(
                    0.01f,
                    currentPose.HeldScale
                );

        }


        heldRenderer.transform.localPosition =
            new Vector3(
                offset.x,
                offset.y,
                0f
            );


        heldRenderer.transform.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                rotation
            );


        Sprite sprite =
            heldRenderer.sprite;


        if (
            sprite ==
            null
        )
        {

            return;

        }


        float biggest =
            Mathf.Max(
                sprite.bounds.size.x,
                sprite.bounds.size.y
            );


        if (
            biggest <=
            0.0001f
        )
        {

            biggest =
                1f;

        }


        float scale =
            heldWorldSize /
            biggest *
            poseScale;


        heldRenderer.transform.localScale =
            Vector3.one *
            scale;

    }


    // =====================================================
    // SORTING
    // =====================================================

    private void UpdateSorting()
    {

        if (
            heldRenderer ==
            null
        )
        {

            return;

        }


        if (
            bodyRenderer !=
            null
        )
        {

            heldRenderer.sortingLayerID =
                bodyRenderer.sortingLayerID;


            heldRenderer.sortingOrder =
                bodyRenderer.sortingOrder +
                10;

        }
        else
        {

            heldRenderer.sortingOrder =
                100;

        }

    }


    // =====================================================
    // EDITOR ACCESS
    // =====================================================

    public Transform GetFrontArmPivot()
    {

        return
            frontArmPivot;

    }


    public Transform GetHandPoint()
    {

        return
            handPoint;

    }


    public Vector2 GetDefaultHeldOffset()
    {

        return
            defaultHeldOffset;

    }


    public float GetHeldWorldSize()
    {

        return
            heldWorldSize;

    }


    public float GetDefaultHeldRotation()
    {

        return
            defaultHeldRotation;

    }

}
