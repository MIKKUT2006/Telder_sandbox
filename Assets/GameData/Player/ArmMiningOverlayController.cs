using UnityEngine;
using Game.Mining;


public class ArmMiningOverlayController :
    MonoBehaviour
{
    [Header("Rig")]
    [SerializeField]
    private Transform frontArmPivot;

    [SerializeField]
    private Transform backArmPivot;

    [SerializeField]
    private Transform handPoint;


    // These are intentionally in ARM.PARENT local space.
    // The old Held Item Pose Editor uses these exact serialized names.
    [Header("Shoulder Points")]
    [SerializeField]
    private Vector2 frontRotationPointLocal =
        Vector2.zero;

    [SerializeField]
    private Vector2 backRotationPointLocal =
        Vector2.zero;

    [SerializeField]
    private bool rotationPointsInitialized;


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


    [Header("Smoothing")]
    [SerializeField]
    private float blendInSpeed =
        12f;

    [SerializeField]
    private float blendOutSpeed =
        9f;

    [SerializeField]
    private float miningSignalGrace =
        0.16f;

    [Range(0f, 1f)]
    [SerializeField]
    private float motionSmoothing =
        0.75f;


    private float miningWeight;

    private float swingPhase;

    private float graceTimer;

    private bool wasActive;


    private void Awake()
    {
        ResolveReferences();

        if (!rotationPointsInitialized)
        {
            if (frontArmPivot != null)
            {
                frontRotationPointLocal =
                    new Vector2(
                        frontArmPivot.localPosition.x,
                        frontArmPivot.localPosition.y
                    );
            }

            if (backArmPivot != null)
            {
                backRotationPointLocal =
                    new Vector2(
                        backArmPivot.localPosition.x,
                        backArmPivot.localPosition.y
                    );
            }
        }
    }


    private void OnEnable()
    {
        miningWeight = 0f;
        swingPhase = 0f;
        graceTimer = 0f;
        wasActive = false;
    }


    private void Update()
    {
        ResolveReferences();

        float dt =
            Mathf.Min(
                Time.deltaTime,
                0.05f
            );

        bool signal =
            MiningVisualSignal.IsMining;

        if (signal)
        {
            graceTimer =
                Mathf.Max(
                    0f,
                    miningSignalGrace
                );
        }
        else
        {
            graceTimer =
                Mathf.Max(
                    0f,
                    graceTimer - dt
                );
        }

        bool active =
            signal ||
            graceTimer > 0f;

        if (
            active &&
            !wasActive &&
            miningWeight < 0.05f
        )
        {
            swingPhase = 0f;
        }

        float target =
            active
                ? 1f
                : 0f;

        float speed =
            active
                ? blendInSpeed
                : blendOutSpeed;

        miningWeight =
            ExpDamp(
                miningWeight,
                target,
                speed,
                dt
            );

        if (active)
        {
            float cyclesPerSecond =
                Mathf.Max(
                    0.01f,
                    swingSpeed
                )
                /
                (
                    Mathf.PI *
                    2f
                );

            swingPhase =
                Mathf.Repeat(
                    swingPhase +
                    cyclesPerSecond *
                    dt,
                    1f
                );
        }

        if (
            !active &&
            miningWeight < 0.0005f
        )
        {
            miningWeight = 0f;
        }

        wasActive = active;
    }


    public bool TryGetMiningPose(
        out float frontAngle,
        out float backAngle
    )
    {
        if (miningWeight <= 0.0005f)
        {
            frontAngle = 0f;
            backAngle = 0f;
            return false;
        }

        float motion =
            EvaluateSwing(
                swingPhase
            );

        frontAngle =
            Mathf.Lerp(
                frontArmBackAngle,
                frontArmForwardAngle,
                motion
            )
            *
            miningWeight;

        backAngle =
            Mathf.Lerp(
                backArmBackAngle,
                backArmForwardAngle,
                motion
            )
            *
            miningWeight;

        return true;
    }


    // Kept only because the old editor may still call it.
    // The new runtime does NOT use this helper.
    public static void RotateCurrentPoseAroundLocalPoint(
        Transform arm,
        Vector2 pointInParentLocal,
        float angle
    )
    {
        if (
            arm == null ||
            arm.parent == null
        )
        {
            return;
        }

        Vector3 pivot =
            new Vector3(
                pointInParentLocal.x,
                pointInParentLocal.y,
                arm.localPosition.z
            );

        Quaternion delta =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );

        arm.localPosition =
            pivot +
            delta *
            (
                arm.localPosition -
                pivot
            );

        arm.localRotation =
            delta *
            arm.localRotation;
    }


    private float EvaluateSwing(
        float phase
    )
    {
        float value =
            0.5f -
            0.5f *
            Mathf.Cos(
                phase *
                Mathf.PI *
                2f
            );

        float smoother =
            value *
            value *
            value *
            (
                value *
                (
                    value *
                    6f -
                    15f
                )
                +
                10f
            );

        return
            Mathf.Lerp(
                value,
                smoother,
                Mathf.Clamp01(
                    motionSmoothing
                )
            );
    }


    private static float ExpDamp(
        float current,
        float target,
        float speed,
        float dt
    )
    {
        speed =
            Mathf.Max(
                0.01f,
                speed
            );

        float t =
            1f -
            Mathf.Exp(
                -speed *
                dt
            );

        return
            Mathf.Lerp(
                current,
                target,
                t
            );
    }


    private void ResolveReferences()
    {
        if (frontArmPivot == null)
        {
            frontArmPivot =
                FindDeepChild(
                    transform,
                    "FrontArmPivot"
                );
        }

        if (backArmPivot == null)
        {
            backArmPivot =
                FindDeepChild(
                    transform,
                    "BackArmPivot"
                );
        }

        if (
            handPoint == null &&
            frontArmPivot != null
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
        string childName
    )
    {
        if (root == null)
            return null;

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
            if (
                all[i] != null &&
                all[i].name == childName
            )
            {
                return all[i];
            }
        }

        return null;
    }


    public Transform GetFrontArmPivot()
    {
        return frontArmPivot;
    }


    public Transform GetBackArmPivot()
    {
        return backArmPivot;
    }


    public Transform GetHandPoint()
    {
        return handPoint;
    }


    public Vector2 GetFrontRotationPointLocal()
    {
        return frontRotationPointLocal;
    }


    public Vector2 GetBackRotationPointLocal()
    {
        return backRotationPointLocal;
    }
}
