
using UnityEngine;

using Game.Mining;


[DefaultExecutionOrder(31000)]
public class ArmMiningOverlayController :
    MonoBehaviour
{

    // =====================================================
    // REFERENCES
    // =====================================================

    [Header("Rig")]

    [SerializeField]
    private Transform frontArmPivot;


    [SerializeField]
    private Transform backArmPivot;


    [Tooltip(
        "Optional. If assigned, the held item is expected to be a child of this point."
    )]
    [SerializeField]
    private Transform handPoint;


    [SerializeField]
    private Camera playerCamera;


    // =====================================================
    // MINING MOTION
    // =====================================================

    [Header("Mining Motion")]

    [SerializeField]
    private float swingSpeed =
        5.5f;


    [SerializeField]
    private float frontArmBackAngle =
        52f;


    [SerializeField]
    private float frontArmForwardAngle =
        -58f;


    [SerializeField]
    private float backArmBackAngle =
        -12f;


    [SerializeField]
    private float backArmForwardAngle =
        18f;


    [SerializeField]
    private float blendInSpeed =
        14f;


    [SerializeField]
    private float blendOutSpeed =
        18f;


    // =====================================================
    // OPTIONAL AIM
    // =====================================================

    [Header("Aim")]

    [SerializeField]
    private bool mirrorSwingWhenAimingLeft =
        true;


    [SerializeField]
    private bool useMouseDirection =
        true;


    // =====================================================
    // RUNTIME
    // =====================================================

    private float miningWeight;


    private float frontAppliedOffset;

    private float backAppliedOffset;


    private bool initialized;


    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {

        if (
            playerCamera ==
            null
        )
        {

            playerCamera =
                Camera.main;

        }


        AutoFindRig();


        initialized =
            frontArmPivot !=
            null;


        if (
            !initialized
        )
        {

            Debug.LogError(
                "ARM MINING: FrontArmPivot not found."
            );

        }

    }


    private void LateUpdate()
    {

        if (
            !initialized
        )
        {

            AutoFindRig();


            initialized =
                frontArmPivot !=
                null;


            if (
                !initialized
            )
            {

                return;

            }

        }


        // Animator has already evaluated this frame.
        // Remove only the offset that THIS script applied previously.
        // This protects us if a locomotion state does not key the arm
        // on a particular frame/state.
        RemovePreviousOffsets();


        bool mining =
            MiningVisualSignal.IsMining;


        float targetWeight =
            mining
                ? 1f
                : 0f;


        float blendSpeed =
            mining
                ? blendInSpeed
                : blendOutSpeed;


        miningWeight =
            Mathf.MoveTowards(
                miningWeight,
                targetWeight,
                blendSpeed *
                Time.deltaTime
            );


        if (
            miningWeight <=
            0.0001f
        )
        {

            frontAppliedOffset =
                0f;


            backAppliedOffset =
                0f;


            return;

        }


        float phase =
            Mathf.PingPong(
                Time.time *
                swingSpeed,
                1f
            );


        // SmoothStep without allocating an AnimationCurve.
        phase =
            phase *
            phase *
            (
                3f -
                2f *
                phase
            );


        float frontOffset =
            Mathf.Lerp(
                frontArmBackAngle,
                frontArmForwardAngle,
                phase
            );


        float backOffset =
            Mathf.Lerp(
                backArmBackAngle,
                backArmForwardAngle,
                phase
            );


        if (
            mirrorSwingWhenAimingLeft
            &&
            IsAimingLeft()
        )
        {

            frontOffset =
                -frontOffset;


            backOffset =
                -backOffset;

        }


        frontAppliedOffset =
            frontOffset *
            miningWeight;


        backAppliedOffset =
            backOffset *
            miningWeight;


        ApplyLocalZOffset(
            frontArmPivot,
            frontAppliedOffset
        );


        if (
            backArmPivot !=
            null
        )
        {

            ApplyLocalZOffset(
                backArmPivot,
                backAppliedOffset
            );

        }

    }


    private void OnDisable()
    {

        RemovePreviousOffsets();


        miningWeight =
            0f;


        frontAppliedOffset =
            0f;


        backAppliedOffset =
            0f;

    }


    // =====================================================
    // RIG SEARCH
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
            backArmPivot ==
            null
        )
        {

            backArmPivot =
                FindDeepChild(
                    transform,
                    "BackArmPivot"
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


    // =====================================================
    // OVERLAY
    // =====================================================

    private void RemovePreviousOffsets()
    {

        if (
            frontArmPivot !=
            null
            &&
            Mathf.Abs(
                frontAppliedOffset
            )
            >
            0.0001f
        )
        {

            ApplyLocalZOffset(
                frontArmPivot,
                -frontAppliedOffset
            );

        }


        if (
            backArmPivot !=
            null
            &&
            Mathf.Abs(
                backAppliedOffset
            )
            >
            0.0001f
        )
        {

            ApplyLocalZOffset(
                backArmPivot,
                -backAppliedOffset
            );

        }


        frontAppliedOffset =
            0f;


        backAppliedOffset =
            0f;

    }


    private static void ApplyLocalZOffset(
        Transform target,
        float degrees
    )
    {

        if (
            target ==
            null
        )
        {

            return;

        }


        target.localRotation =
            target.localRotation *
            Quaternion.Euler(
                0f,
                0f,
                degrees
            );

    }


    // =====================================================
    // DIRECTION
    // =====================================================

    private bool IsAimingLeft()
    {

        if (
            !useMouseDirection
        )
        {

            return false;

        }


        if (
            playerCamera ==
            null
        )
        {

            playerCamera =
                Camera.main;

        }


        if (
            playerCamera ==
            null
        )
        {

            return false;

        }


        Vector3 mouse =
            playerCamera.ScreenToWorldPoint(
                Input.mousePosition
            );


        return
            mouse.x <
            transform.position.x;

    }


    // =====================================================
    // PUBLIC ACCESS
    // =====================================================

    public Transform GetFrontArmPivot()
    {

        return
            frontArmPivot;

    }


    public Transform GetBackArmPivot()
    {

        return
            backArmPivot;

    }


    public Transform GetHandPoint()
    {

        return
            handPoint;

    }

}
