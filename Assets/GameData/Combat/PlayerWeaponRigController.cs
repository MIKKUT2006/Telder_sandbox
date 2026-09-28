using System.Collections.Generic;
using UnityEngine;


namespace Game.Combat
{
    [DefaultExecutionOrder(60000)]
    public sealed class PlayerWeaponRigController :
        MonoBehaviour
    {
        [Header("Auto-resolved")]

        [SerializeField]
        private Transform frontArmSource;


        [SerializeField]
        private Transform backArmSource;


        [SerializeField]
        private Transform sourceHandPoint;


        [SerializeField]
        private ArmMiningOverlayController miningProvider;


        [SerializeField]
        private PlayerCombatRigCalibration calibration;


        private Transform frontVisual;


        private Transform backVisual;


        private Transform runtimeHandPoint;


        private Transform runtimeHeldItem;


        private readonly Dictionary<
            Transform,
            Transform
        > sourceToClone =
            new Dictionary<
                Transform,
                Transform
            >();


        private readonly List<SpriteRenderer>
            hiddenSourceRenderers =
                new List<SpriteRenderer>();


        private bool built;


        private void Awake()
        {
            ResolveReferences();


            EnsureCalibration();


            BuildVisualRig();
        }


        private void OnDestroy()
        {
            for (
                int i = 0;
                i < hiddenSourceRenderers.Count;
                i++
            )
            {
                SpriteRenderer sr =
                    hiddenSourceRenderers[i];


                if (sr != null)
                {
                    sr.forceRenderingOff =
                        false;
                }
            }
        }


        private void LateUpdate()
        {
            ResolveReferences();


            EnsureCalibration();


            if (!built)
            {
                BuildVisualRig();


                if (!built)
                    return;
            }


            SyncVisualTree();


            ApplyRequestedPose();
        }


        private void EnsureCalibration()
        {
            if (calibration == null)
            {
                calibration =
                    GetComponent<
                        PlayerCombatRigCalibration
                    >();


                if (calibration == null)
                {
                    calibration =
                        gameObject.AddComponent<
                            PlayerCombatRigCalibration
                        >();
                }
            }


            if (
                !calibration.Configured &&
                frontArmSource != null &&
                backArmSource != null
            )
            {
                calibration.CaptureCurrentRig(
                    frontArmSource,
                    backArmSource,
                    sourceHandPoint
                );
            }
        }


        // =====================================================
        // BUILD VISUAL COPY
        // =====================================================

        private void BuildVisualRig()
        {
            if (built)
                return;


            if (
                frontArmSource == null ||
                backArmSource == null ||
                frontArmSource.parent == null ||
                backArmSource.parent == null
            )
            {
                return;
            }


            frontVisual =
                CreateVisualRoot(
                    frontArmSource,
                    "__WeaponFrontVisual"
                );


            backVisual =
                CreateVisualRoot(
                    backArmSource,
                    "__WeaponBackVisual"
                );


            if (
                frontVisual == null ||
                backVisual == null
            )
            {
                return;
            }


            sourceToClone.Clear();
            hiddenSourceRenderers.Clear();


            CloneChildrenRecursive(
                frontArmSource,
                frontVisual
            );


            CloneChildrenRecursive(
                backArmSource,
                backVisual
            );


            ResolveRuntimeHandPoint();


            built =
                true;
        }


        private static Transform CreateVisualRoot(
            Transform source,
            string name
        )
        {
            Transform old =
                source.parent.Find(
                    name
                );


            if (old != null)
            {
                Object.Destroy(
                    old.gameObject
                );
            }


            GameObject go =
                new GameObject(
                    name
                );


            go.layer =
                source.gameObject.layer;


            Transform t =
                go.transform;


            t.SetParent(
                source.parent,
                false
            );


            t.localPosition =
                source.localPosition;


            t.localRotation =
                source.localRotation;


            t.localScale =
                source.localScale;


            return t;
        }


        private void CloneChildrenRecursive(
            Transform sourceParent,
            Transform cloneParent
        )
        {
            for (
                int i = 0;
                i < sourceParent.childCount;
                i++
            )
            {
                Transform sourceChild =
                    sourceParent.GetChild(
                        i
                    );


                Transform clone =
                    CreateCloneNode(
                        sourceChild,
                        cloneParent
                    );


                CloneChildrenRecursive(
                    sourceChild,
                    clone
                );
            }
        }


        private Transform CreateCloneNode(
            Transform source,
            Transform parent
        )
        {
            GameObject go =
                new GameObject(
                    source.name
                );


            go.layer =
                source.gameObject.layer;


            go.SetActive(
                source.gameObject.activeSelf
            );


            Transform clone =
                go.transform;


            clone.SetParent(
                parent,
                false
            );


            CopyLocalTransform(
                source,
                clone
            );


            sourceToClone[
                source
            ] = clone;


            SpriteRenderer sourceRenderer =
                source.GetComponent<
                    SpriteRenderer
                >();


            if (sourceRenderer != null)
            {
                SpriteRenderer cloneRenderer =
                    go.AddComponent<
                        SpriteRenderer
                    >();


                CopyRenderer(
                    sourceRenderer,
                    cloneRenderer
                );


                sourceRenderer.forceRenderingOff =
                    true;


                hiddenSourceRenderers.Add(
                    sourceRenderer
                );
            }


            return clone;
        }


        // =====================================================
        // SYNC
        // =====================================================

        private void SyncVisualTree()
        {
            SyncVisualRoot(
                frontArmSource,
                frontVisual,
                calibration.FrontArmBaseLocalPosition
            );


            SyncVisualRoot(
                backArmSource,
                backVisual,
                calibration.BackArmBaseLocalPosition
            );


            EnsureCloneTree(
                frontArmSource,
                frontVisual
            );


            EnsureCloneTree(
                backArmSource,
                backVisual
            );


            foreach (
                KeyValuePair<
                    Transform,
                    Transform
                > pair
                in sourceToClone
            )
            {
                Transform source =
                    pair.Key;


                Transform clone =
                    pair.Value;


                if (
                    source == null ||
                    clone == null
                )
                {
                    continue;
                }


                clone.gameObject.SetActive(
                    source.gameObject.activeSelf
                );


                CopyLocalTransform(
                    source,
                    clone
                );


                SpriteRenderer sourceRenderer =
                    source.GetComponent<
                        SpriteRenderer
                    >();


                SpriteRenderer cloneRenderer =
                    clone.GetComponent<
                        SpriteRenderer
                    >();


                if (
                    sourceRenderer != null &&
                    cloneRenderer != null
                )
                {
                    CopyRenderer(
                        sourceRenderer,
                        cloneRenderer
                    );


                    sourceRenderer.forceRenderingOff =
                        true;
                }
            }


            ResolveRuntimeHandPoint();


            if (runtimeHandPoint != null)
            {
                Vector3 hp =
                    runtimeHandPoint.localPosition;


                hp.x =
                    calibration.HandPointLocalPosition.x;


                hp.y =
                    calibration.HandPointLocalPosition.y;


                runtimeHandPoint.localPosition =
                    hp;
            }


            runtimeHeldItem =
                FindHeldItem(
                    runtimeHandPoint
                );
        }


        private static void SyncVisualRoot(
            Transform source,
            Transform visual,
            Vector2 authoredXY
        )
        {
            if (
                source == null ||
                visual == null
            )
            {
                return;
            }


            Vector3 p =
                source.localPosition;


            p.x =
                authoredXY.x;


            p.y =
                authoredXY.y;


            visual.localPosition =
                p;


            visual.localRotation =
                source.localRotation;


            visual.localScale =
                source.localScale;
        }


        private void EnsureCloneTree(
            Transform sourceParent,
            Transform cloneParent
        )
        {
            for (
                int i = 0;
                i < sourceParent.childCount;
                i++
            )
            {
                Transform sourceChild =
                    sourceParent.GetChild(
                        i
                    );


                if (
                    !sourceToClone.TryGetValue(
                        sourceChild,
                        out Transform clone
                    )
                    ||
                    clone == null
                )
                {
                    clone =
                        CreateCloneNode(
                            sourceChild,
                            cloneParent
                        );
                }


                EnsureCloneTree(
                    sourceChild,
                    clone
                );
            }
        }


        private void ResolveRuntimeHandPoint()
        {
            if (
                sourceHandPoint != null &&
                sourceToClone.TryGetValue(
                    sourceHandPoint,
                    out Transform hp
                )
            )
            {
                runtimeHandPoint =
                    hp;


                return;
            }


            runtimeHandPoint =
                FindDeepChild(
                    frontVisual,
                    "HandPoint"
                );
        }


        private static void CopyLocalTransform(
            Transform source,
            Transform target
        )
        {
            if (
                source == null ||
                target == null
            )
            {
                return;
            }


            target.localPosition =
                source.localPosition;


            target.localRotation =
                source.localRotation;


            target.localScale =
                source.localScale;
        }


        private static void CopyRenderer(
            SpriteRenderer source,
            SpriteRenderer target
        )
        {
            target.sprite =
                source.sprite;


            target.color =
                source.color;


            target.flipX =
                source.flipX;


            target.flipY =
                source.flipY;


            target.drawMode =
                source.drawMode;


            target.size =
                source.size;


            target.maskInteraction =
                source.maskInteraction;


            target.sortingLayerID =
                source.sortingLayerID;


            target.sortingOrder =
                source.sortingOrder;


            target.sharedMaterial =
                source.sharedMaterial;


            target.enabled =
                source.enabled;
        }


        // =====================================================
        // POSE
        // =====================================================

        private void ApplyRequestedPose()
        {
            PlayerWeaponController weapon =
                GetComponent<
                    PlayerWeaponController
                >();


            PlayerWeaponPoseRequest request =
                default;


            bool hasWeapon =
                weapon != null &&
                weapon.TryGetArmPoseRequest(
                    out request
                );


            float miningFront =
                0f;


            float miningBack =
                0f;


            bool hasMining =
                false;


            if (
                !hasWeapon &&
                miningProvider != null
            )
            {
                hasMining =
                    miningProvider.TryGetMiningPose(
                        out miningFront,
                        out miningBack
                    );
            }


            if (hasWeapon)
            {
                ApplyWeaponPose(
                    request
                );


                return;
            }


            if (hasMining)
            {
                RotateVisualAroundShoulder(
                    frontVisual,
                    calibration.FrontShoulderParentLocal,
                    miningFront
                );


                RotateVisualAroundShoulder(
                    backVisual,
                    calibration.BackShoulderParentLocal,
                    miningBack
                );
            }
        }


        private void ApplyWeaponPose(
            PlayerWeaponPoseRequest request
        )
        {
            WeaponMetadata data =
                request.Weapon;


            if (data == null)
                return;


            Vector2 aim =
                request.AimDirection.sqrMagnitude >
                0.000001f
                    ? request.AimDirection.normalized
                    : Vector2.right;


            float aimAngle =
                DirectionAngle(
                    aim
                );


            if (
                request.Kind ==
                WeaponKind.Sword
            )
            {
                if (
                    request.SwordAttack ==
                    SwordAttackKind.Swing
                )
                {
                    float relative =
                        Mathf.LerpAngle(
                            data.SwordSwingStartAngle,
                            data.SwordSwingEndAngle,
                            Smooth01(
                                request.Phase
                            )
                        );


                    // Build the authored RIGHT-facing swing first,
                    // then horizontally mirror the RESULTING VECTOR when
                    // the player faces left.
                    //
                    // This guarantees:
                    // right-facing arc -> normal
                    // left-facing arc  -> exact horizontal mirror
                    //
                    // No 180/-180 angle-wrap ambiguity remains.
                    Vector2 swingDirection =
                        BuildFacingMirroredSwingDirection(
                            aim,
                            request.FacingRight,
                            relative
                        );


                    float swingWorldAngle =
                        DirectionAngle(
                            swingDirection
                        );


                    float frontOffset =
                        request.FacingRight
                            ? data.ArmAimOffset
                            : -data.ArmAimOffset;


                    float backOffset =
                        request.FacingRight
                            ? data.BackArmAimOffset
                            : -data.BackArmAimOffset;


                    AimFrontFist(
                        swingWorldAngle +
                        frontOffset
                    );


                    if (data.UseBackArm)
                    {
                        AimBackFist(
                            swingWorldAngle +
                            backOffset
                        );
                    }


                    return;
                }


                AimFrontFist(
                    aimAngle +
                    data.ArmAimOffset
                );


                if (data.UseBackArm)
                {
                    AimBackFist(
                        aimAngle +
                        data.BackArmAimOffset
                    );
                }


                ApplyHeldItemWorldOffset(
                    aim *
                    (
                        ThrustEnvelope(
                            request.Phase
                        )
                        *
                        data.ThrustDistance
                    )
                );


                return;
            }


            if (
                request.Kind ==
                WeaponKind.Spear
            )
            {
                AimFrontFist(
                    aimAngle +
                    data.ArmAimOffset
                );


                if (data.UseBackArm)
                {
                    AimBackFist(
                        aimAngle +
                        data.BackArmAimOffset
                    );
                }


                ApplyHeldItemWorldOffset(
                    aim *
                    (
                        ThrustEnvelope(
                            request.Phase
                        )
                        *
                        data.SpearThrustDistance
                    )
                );


                return;
            }


            if (
                request.Kind ==
                WeaponKind.Bow
            )
            {
                // BOTH bow arms use the exact same cursor-tracking rule.
                //
                // FRONT:
                // shoulder -> front fist -> cursor
                //
                // BACK:
                // shoulder -> back fist -> cursor
                //
                // Important: bow aiming ignores ArmAimOffset /
                // BackArmAimOffset. Those offsets are useful for other weapon
                // sprites, but would prevent the fists from looking exactly
                // at the cursor.
                AimFrontFist(
                    aimAngle
                );


                AimBackFist(
                    aimAngle
                );


                float draw =
                    Smooth01(
                        request.Phase
                    );


                // ONLY the draw/back arm moves backward.
                // The front/main arm remains at its shoulder position and
                // simply follows the cursor.
                TranslateVisualWorld(
                    backVisual,
                    -aim *
                    (
                        data.BowDrawDistance *
                        draw
                    )
                );


                if (
                    Mathf.Abs(
                        data.BowHeldItemFullDrawOffset
                    )
                    >
                    0.00001f
                )
                {
                    ApplyHeldItemWorldOffset(
                        aim *
                        (
                            data.BowHeldItemFullDrawOffset *
                            draw
                        )
                    );
                }


                return;
            }


            if (
                request.Kind ==
                WeaponKind.Gun
            )
            {
                AimFrontFist(
                    aimAngle +
                    data.ArmAimOffset
                );


                if (data.UseBackArm)
                {
                    AimBackFist(
                        aimAngle +
                        data.BackArmAimOffset
                    );
                }
            }
        }


        // =====================================================
        // FIST AIM
        // =====================================================

        private void AimFrontFist(
            float worldAngle
        )
        {
            AimVisualFist(
                frontVisual,
                calibration.FrontShoulderParentLocal,
                calibration.FrontFistArmLocal,
                DirectionFromAngle(
                    worldAngle
                )
            );
        }


        private void AimBackFist(
            float worldAngle
        )
        {
            AimVisualFist(
                backVisual,
                calibration.BackShoulderParentLocal,
                calibration.BackFistArmLocal,
                DirectionFromAngle(
                    worldAngle
                )
            );
        }


        private static void AimVisualFist(
            Transform visual,
            Vector2 shoulderParentLocal,
            Vector2 fistLocal,
            Vector2 desiredWorldDirection
        )
        {
            if (
                visual == null ||
                visual.parent == null
            )
            {
                return;
            }


            Vector3 fistWorld =
                visual.TransformPoint(
                    new Vector3(
                        fistLocal.x,
                        fistLocal.y,
                        0f
                    )
                );


            Vector3 fistParent =
                visual.parent.InverseTransformPoint(
                    fistWorld
                );


            Vector2 current =
                new Vector2(
                    fistParent.x -
                    shoulderParentLocal.x,
                    fistParent.y -
                    shoulderParentLocal.y
                );


            Vector3 desiredParent3 =
                visual.parent.InverseTransformVector(
                    desiredWorldDirection
                );


            Vector2 desired =
                new Vector2(
                    desiredParent3.x,
                    desiredParent3.y
                );


            if (
                current.sqrMagnitude <
                0.000001f ||
                desired.sqrMagnitude <
                0.000001f
            )
            {
                return;
            }


            float delta =
                Vector2.SignedAngle(
                    current,
                    desired
                );


            RotateVisualAroundShoulder(
                visual,
                shoulderParentLocal,
                delta
            );
        }


        private static void RotateVisualAroundShoulder(
            Transform visual,
            Vector2 shoulderParentLocal,
            float angle
        )
        {
            if (
                visual == null ||
                visual.parent == null
            )
            {
                return;
            }


            Quaternion delta =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                );


            Vector3 pivot =
                new Vector3(
                    shoulderParentLocal.x,
                    shoulderParentLocal.y,
                    visual.localPosition.z
                );


            visual.localPosition =
                pivot +
                delta *
                (
                    visual.localPosition -
                    pivot
                );


            visual.localRotation =
                delta *
                visual.localRotation;
        }


        private static void TranslateVisualWorld(
            Transform visual,
            Vector2 worldDelta
        )
        {
            if (
                visual == null ||
                visual.parent == null
            )
            {
                return;
            }


            visual.localPosition +=
                visual.parent.InverseTransformVector(
                    worldDelta
                );
        }


        private void ApplyHeldItemWorldOffset(
            Vector2 worldDelta
        )
        {
            if (
                runtimeHeldItem == null ||
                runtimeHeldItem.parent == null
            )
            {
                return;
            }


            runtimeHeldItem.localPosition +=
                runtimeHeldItem.parent.InverseTransformVector(
                    worldDelta
                );
        }


        // =====================================================
        // PUBLIC
        // =====================================================

        public Vector2 GetFrontShoulderWorldPosition()
        {
            if (
                frontVisual == null ||
                frontVisual.parent == null
            )
            {
                return transform.position;
            }


            Vector2 p =
                calibration.FrontShoulderParentLocal;


            return
                frontVisual.parent.TransformPoint(
                    new Vector3(
                        p.x,
                        p.y,
                        frontVisual.localPosition.z
                    )
                );
        }


        public Transform GetRuntimeHandPoint()
        {
            return runtimeHandPoint;
        }


        // =====================================================
        // REFERENCES
        // =====================================================

        private void ResolveReferences()
        {
            if (miningProvider == null)
            {
                miningProvider =
                    GetComponent<
                        ArmMiningOverlayController
                    >();


                if (miningProvider == null)
                {
                    miningProvider =
                        GetComponentInChildren<
                            ArmMiningOverlayController
                        >(
                            true
                        );
                }
            }


            if (calibration == null)
            {
                calibration =
                    GetComponent<
                        PlayerCombatRigCalibration
                    >();
            }


            if (frontArmSource == null)
            {
                frontArmSource =
                    FindDeepChild(
                        transform,
                        "FrontArmPivot"
                    );
            }


            if (backArmSource == null)
            {
                backArmSource =
                    FindDeepChild(
                        transform,
                        "BackArmPivot"
                    );
            }


            if (
                sourceHandPoint == null &&
                frontArmSource != null
            )
            {
                sourceHandPoint =
                    FindDeepChild(
                        frontArmSource,
                        "HandPoint"
                    );
            }
        }


        private static Transform FindDeepChild(
            Transform root,
            string name
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
                    all[i].name == name
                )
                {
                    return all[i];
                }
            }


            return null;
        }


        private static Transform FindHeldItem(
            Transform hp
        )
        {
            if (hp == null)
                return null;


            Transform direct =
                hp.Find(
                    "HeldItem"
                );


            if (direct != null)
                return direct;


            return
                hp.childCount > 0
                    ? hp.GetChild(0)
                    : null;
        }


        // =====================================================
        // MATH
        // =====================================================


private static Vector2 BuildFacingMirroredSwingDirection(
    Vector2 frontAim,
    bool facingRight,
    float relativeAngle
)
{
    float sign =
        facingRight
            ? 1f
            : -1f;


    // Convert the current front-facing aim into canonical
    // RIGHT-facing space.
    Vector2 canonicalAim =
        new Vector2(
            Mathf.Abs(
                frontAim.x
            ),
            frontAim.y
        );


    if (
        canonicalAim.sqrMagnitude <
        0.000001f
    )
    {
        canonicalAim =
            Vector2.right;
    }


    canonicalAim.Normalize();


    // Apply the editor-authored sword arc only once,
    // in canonical RIGHT-facing space.
    float radians =
        relativeAngle *
        Mathf.Deg2Rad;


    float c =
        Mathf.Cos(
            radians
        );


    float s =
        Mathf.Sin(
            radians
        );


    Vector2 canonicalSwing =
        new Vector2(
            canonicalAim.x * c -
            canonicalAim.y * s,

            canonicalAim.x * s +
            canonicalAim.y * c
        );


    // True horizontal mirror for left-facing:
    // X changes sign, Y does not.
    Vector2 worldSwing =
        new Vector2(
            canonicalSwing.x *
            sign,
            canonicalSwing.y
        );


    if (
        worldSwing.sqrMagnitude <
        0.000001f
    )
    {
        return
            facingRight
                ? Vector2.right
                : Vector2.left;
    }


    return
        worldSwing.normalized;
}


        private static float DirectionAngle(
            Vector2 direction
        )
        {
            return
                Mathf.Atan2(
                    direction.y,
                    direction.x
                )
                *
                Mathf.Rad2Deg;
        }


        private static Vector2 DirectionFromAngle(
            float degrees
        )
        {
            float r =
                degrees *
                Mathf.Deg2Rad;


            return
                new Vector2(
                    Mathf.Cos(r),
                    Mathf.Sin(r)
                );
        }


        private static float Smooth01(
            float t
        )
        {
            t =
                Mathf.Clamp01(t);


            return
                t *
                t *
                (
                    3f -
                    2f *
                    t
                );
        }


        private static float ThrustEnvelope(
            float t
        )
        {
            float s =
                Mathf.Sin(
                    Mathf.Clamp01(t) *
                    Mathf.PI
                );


            return s * s;
        }
    }
}
