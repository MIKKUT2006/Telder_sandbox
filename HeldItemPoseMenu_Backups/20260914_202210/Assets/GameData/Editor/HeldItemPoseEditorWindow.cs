
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using Game.Inventory;
using Game.Items.Visual;


public class HeldItemPoseEditorWindow :
    EditorWindow
{

    // =====================================================
    // SCENE PLAYER
    // =====================================================

    private GameObject player;

    private Animator sourceAnimator;

    private Transform sourceFrontArmPivot;

    private Transform sourceBackArmPivot;

    private Transform sourceHandPoint;


    // =====================================================
    // ITEM
    // =====================================================

    private string itemId =
        "game:wood_pickaxe";


    private string itemJsonPath;


    private float heldOffsetX;

    private float heldOffsetY;

    private float heldScale =
        1f;

    private float heldRotation;


    private Vector2 handLocalPosition =
        new Vector2(
            0.28f,
            0f
        );


    // =====================================================
    // ANIMATION
    // =====================================================

    private AnimationClip[] clips =
        Array.Empty<
            AnimationClip
        >();


    private string[] clipNames =
        Array.Empty<
            string
        >();


    private int selectedClipIndex;


    private bool playing;


    private bool loop =
        true;


    private float playbackSpeed =
        1f;


    private float animationTime;


    private double lastEditorTime;


    [Header("Mining Preview")]
    private bool previewMining;


    private bool autoMiningSwing =
        true;


    private float manualMiningPhase =
        0.5f;


    private float miningSwingSpeed =
        5.5f;


    private float frontArmBackAngle =
        52f;


    private float frontArmForwardAngle =
        -58f;


    private float backArmBackAngle =
        -12f;


    private float backArmForwardAngle =
        18f;


    // =====================================================
    // PREVIEW
    // =====================================================

    // [HELD-POSE-PREVIEW-V30]
    // V30:
    // We intentionally do NOT use PreviewRenderUtility for the 2D rig.
    // SpriteRenderer + custom/pixel materials can render as a blank preview
    // in PreviewRenderUtility depending on the project's render pipeline.
    //
    // Instead we sample the REAL cloned hierarchy, read every SpriteRenderer
    // transform, and draw the sprite textures directly in editor GUI.
    //
    // This keeps animation transforms but removes the render-pipeline dependency.


    private GameObject previewPlayer;

    private Animator previewAnimator;

    // V30:
    // Visual-only preview does not need a live Animator component.
    // This transform mirrors the real Animator root and is used as
    // AnimationClip.SampleAnimation root.
    private Transform previewAnimationRoot;

    private Transform previewFrontArmPivot;

    private Transform previewBackArmPivot;

    private Transform previewHandPoint;

    private SpriteRenderer previewHeldRenderer;


    private Sprite generatedItemSprite;


    private float previewZoom =
        1.15f;


    private Vector2 previewPan;


    private bool mirrorPreview;


    private Vector2 scroll;


    // =====================================================
    // MENU
    // =====================================================

    [MenuItem(
        "Tools/Game/Held Item Pose Editor"
    )]
    public static void Open()
    {

        HeldItemPoseEditorWindow window =
            GetWindow<
                HeldItemPoseEditorWindow
            >(
                "Held Item Pose"
            );


        window.minSize =
            new Vector2(
                900f,
                600f
            );

    }


    // =====================================================
    // UNITY EDITOR
    // =====================================================

    private void OnEnable()
    {

        EditorApplication.update +=
            EditorUpdate;


        TryUseSelection();

    }


    private void OnDisable()
    {

        EditorApplication.update -=
            EditorUpdate;


        CleanupPreview();

    }


    private void OnSelectionChange()
    {

        TryUseSelection();

        Repaint();

    }


    private void EditorUpdate()
    {

        double now =
            EditorApplication.timeSinceStartup;


        float delta =
            lastEditorTime >
            0.0
                ? (float)(
                    now -
                    lastEditorTime
                )
                : 0f;


        lastEditorTime =
            now;


        if (
            playing
            &&
            GetSelectedClip() !=
            null
        )
        {

            AnimationClip clip =
                GetSelectedClip();


            animationTime +=
                delta *
                playbackSpeed;


            if (
                clip.length >
                0f
            )
            {

                if (
                    loop
                )
                {

                    animationTime =
                        Mathf.Repeat(
                            animationTime,
                            clip.length
                        );

                }
                else
                {

                    animationTime =
                        Mathf.Clamp(
                            animationTime,
                            0f,
                            clip.length
                        );


                    if (
                        animationTime >=
                        clip.length
                    )
                    {

                        playing =
                            false;

                    }

                }

            }


            Repaint();

        }
        else if (
            previewMining
            &&
            autoMiningSwing
        )
        {

            Repaint();

        }

    }


    // =====================================================
    // PLAYER SELECTION
    // =====================================================

    private void TryUseSelection()
    {

        PlayerInventory inventory =
            null;


        GameObject selected =
            Selection.activeGameObject;


        if (
            selected !=
            null
        )
        {

            inventory =
                selected.GetComponent<
                    PlayerInventory
                >();


            if (
                inventory ==
                null
            )
            {

                inventory =
                    selected.GetComponentInParent<
                        PlayerInventory
                    >();

            }

        }


        // V30:
        // Do not leave the preview empty just because another object
        // is currently selected in the editor.
        if (
            inventory ==
            null
            &&
            player ==
            null
        )
        {

            inventory =
                FindScenePlayerInventory();

        }


        if (
            inventory ==
            null
        )
        {

            return;

        }


        if (
            player ==
            inventory.gameObject
        )
        {

            return;

        }


        SetPlayer(
            inventory.gameObject
        );

    }


    private static PlayerInventory FindScenePlayerInventory()
    {

        PlayerInventory[] inventories =
            Resources.FindObjectsOfTypeAll<
                PlayerInventory
            >();


        for (
            int i = 0;
            i < inventories.Length;
            i++
        )
        {

            PlayerInventory inventory =
                inventories[i];


            if (
                inventory ==
                null
                ||
                inventory.gameObject ==
                null
                ||
                !inventory.gameObject.scene.IsValid()
                ||
                EditorUtility.IsPersistent(
                    inventory.gameObject
                )
            )
            {

                continue;

            }


            return inventory;

        }


        return null;

    }


    private void SetPlayer(
        GameObject value
    )
    {

        player =
            value;


        sourceAnimator =
            null;


        sourceFrontArmPivot =
            null;


        sourceBackArmPivot =
            null;


        sourceHandPoint =
            null;


        if (
            player ==
            null
        )
        {

            clips =
                Array.Empty<
                    AnimationClip
                >();


            clipNames =
                Array.Empty<
                    string
                >();


            RebuildPreview();


            return;

        }


        sourceAnimator =
            player.GetComponentInChildren<
                Animator
            >(
                true
            );


        sourceFrontArmPivot =
            FindDeepChild(
                player.transform,
                "FrontArmPivot"
            );


        sourceBackArmPivot =
            FindDeepChild(
                player.transform,
                "BackArmPivot"
            );


        if (
            sourceFrontArmPivot !=
            null
        )
        {

            sourceHandPoint =
                FindDeepChild(
                    sourceFrontArmPivot,
                    "HandPoint"
                );

        }


        if (
            sourceHandPoint !=
            null
        )
        {

            handLocalPosition =
                sourceHandPoint.localPosition;

        }


        ReadMiningOverlaySettings();

        CollectAnimationClips();

        LoadItem();

        RebuildPreview();

    }


    private void ReadMiningOverlaySettings()
    {

        if (
            player ==
            null
        )
        {

            return;

        }


        ArmMiningOverlayController overlay =
            player.GetComponent<
                ArmMiningOverlayController
            >();


        if (
            overlay ==
            null
        )
        {

            return;

        }


        SerializedObject serialized =
            new SerializedObject(
                overlay
            );


        miningSwingSpeed =
            serialized
                .FindProperty(
                    "swingSpeed"
                )
                .floatValue;


        frontArmBackAngle =
            serialized
                .FindProperty(
                    "frontArmBackAngle"
                )
                .floatValue;


        frontArmForwardAngle =
            serialized
                .FindProperty(
                    "frontArmForwardAngle"
                )
                .floatValue;


        backArmBackAngle =
            serialized
                .FindProperty(
                    "backArmBackAngle"
                )
                .floatValue;


        backArmForwardAngle =
            serialized
                .FindProperty(
                    "backArmForwardAngle"
                )
                .floatValue;

    }


    // =====================================================
    // ANIMATION CLIPS
    // =====================================================

    private void CollectAnimationClips()
    {

        List<AnimationClip> found =
            new List<
                AnimationClip
            >();


        HashSet<int> used =
            new HashSet<
                int
            >();


        if (
            sourceAnimator !=
            null
            &&
            sourceAnimator.runtimeAnimatorController !=
            null
        )
        {

            AnimationClip[] controllerClips =
                sourceAnimator
                    .runtimeAnimatorController
                    .animationClips;


            for (
                int i = 0;
                i < controllerClips.Length;
                i++
            )
            {

                AnimationClip clip =
                    controllerClips[i];


                if (
                    clip ==
                    null
                    ||
                    !used.Add(
                        clip.GetInstanceID()
                    )
                )
                {

                    continue;

                }


                found.Add(
                    clip
                );

            }

        }


        clips =
            found.ToArray();


        clipNames =
            new string[
                clips.Length
            ];


        for (
            int i = 0;
            i < clips.Length;
            i++
        )
        {

            clipNames[i] =
                clips[i] !=
                null
                    ? clips[i].name
                    : "<null>";

        }


        selectedClipIndex =
            Mathf.Clamp(
                selectedClipIndex,
                0,
                Mathf.Max(
                    0,
                    clips.Length -
                    1
                )
            );


        animationTime =
            0f;

    }


    private AnimationClip GetSelectedClip()
    {

        if (
            clips ==
            null
            ||
            selectedClipIndex <
            0
            ||
            selectedClipIndex >=
            clips.Length
        )
        {

            return null;

        }


        return
            clips[
                selectedClipIndex
            ];

    }


    // =====================================================
    // GUI
    // =====================================================

    private void OnGUI()
    {

        DrawToolbar();


        EditorGUILayout.Space(
            6f
        );


        float availableWidth =
            Mathf.Max(
                320f,
                position.width -
                8f
            );


        float availableHeight =
            Mathf.Max(
                220f,
                position.height -
                62f
            );


        Rect full =
            GUILayoutUtility.GetRect(
                availableWidth,
                availableHeight,

                GUILayout.ExpandWidth(
                    true
                ),

                GUILayout.ExpandHeight(
                    true
                )
            );


        // On the first layout/repaint pass Unity can still return
        // a zero-sized rect for an EditorWindow that has just opened.
        // Clamp it here so PreviewRenderUtility never receives 0x0.
        full.width =
            Mathf.Max(
                1f,
                full.width
            );


        full.height =
            Mathf.Max(
                1f,
                full.height
            );


        float inspectorWidth =
            Mathf.Clamp(
                full.width *
                0.34f,
                310f,
                390f
            );


        Rect previewRect =
            new Rect(
                full.x,
                full.y,
                full.width -
                inspectorWidth -
                8f,
                full.height
            );


        Rect inspectorRect =
            new Rect(
                previewRect.xMax +
                8f,
                full.y,
                inspectorWidth,
                full.height
            );


        DrawAnimatedPreview(
            previewRect
        );


        GUILayout.BeginArea(
            inspectorRect
        );


        scroll =
            EditorGUILayout.BeginScrollView(
                scroll
            );


        DrawInspector();


        EditorGUILayout.EndScrollView();


        GUILayout.EndArea();

    }


    private void DrawToolbar()
    {

        EditorGUILayout.BeginHorizontal(
            EditorStyles.toolbar
        );


        GameObject newPlayer =
            EditorGUILayout.ObjectField(
                player,
                typeof(
                    GameObject
                ),
                true,
                GUILayout.Width(
                    260f
                )
            )
            as
            GameObject;


        if (
            newPlayer !=
            player
        )
        {

            SetPlayer(
                newPlayer
            );

        }


        GUILayout.Space(
            8f
        );


        GUILayout.Label(
            "Item",
            GUILayout.Width(
                30f
            )
        );


        string newItemId =
            GUILayout.TextField(
                itemId,
                GUILayout.Width(
                    185f
                )
            );


        if (
            newItemId !=
            itemId
        )
        {

            itemId =
                newItemId;

        }


        if (
            GUILayout.Button(
                "Загрузить",
                EditorStyles.toolbarButton,
                GUILayout.Width(
                    72f
                )
            )
        )
        {

            LoadItem();

            RebuildPreview();

        }


        GUILayout.FlexibleSpace();


        if (
            GUILayout.Button(
                "Сохранить",
                EditorStyles.toolbarButton,
                GUILayout.Width(
                    82f
                )
            )
        )
        {

            SaveAll();

        }


        EditorGUILayout.EndHorizontal();

    }


    private void DrawInspector()
    {

        EditorGUILayout.LabelField(
            "Риг игрока",
            EditorStyles.boldLabel
        );


        using (
            new EditorGUI.DisabledScope(
                true
            )
        )
        {

            EditorGUILayout.ObjectField(
                "Animator",
                sourceAnimator,
                typeof(
                    Animator
                ),
                true
            );


            EditorGUILayout.ObjectField(
                "Front Arm",
                sourceFrontArmPivot,
                typeof(
                    Transform
                ),
                true
            );


            EditorGUILayout.ObjectField(
                "Back Arm",
                sourceBackArmPivot,
                typeof(
                    Transform
                ),
                true
            );


            EditorGUILayout.ObjectField(
                "Hand Point",
                sourceHandPoint,
                typeof(
                    Transform
                ),
                true
            );

        }


        if (
            sourceFrontArmPivot ==
            null
        )
        {

            EditorGUILayout.HelpBox(
                "FrontArmPivot не найден. Выдели Player и сначала запусти Tools → Game → Setup Mining Arm Overlay.",
                MessageType.Error
            );


            return;

        }


        if (
            sourceHandPoint ==
            null
        )
        {

            EditorGUILayout.HelpBox(
                "HandPoint не найден. Запусти Setup Mining Arm Overlay.",
                MessageType.Warning
            );

        }


        EditorGUILayout.Space(
            12f
        );


        DrawAnimationControls();


        EditorGUILayout.Space(
            12f
        );


        EditorGUILayout.LabelField(
            "Точка ладони",
            EditorStyles.boldLabel
        );


        Vector2 newHandPosition =
            EditorGUILayout.Vector2Field(
                "Hand Local Position",
                handLocalPosition
            );


        if (
            newHandPosition !=
            handLocalPosition
        )
        {

            handLocalPosition =
                newHandPosition;


            UpdatePreviewHandPoint();

        }


        if (
            GUILayout.Button(
                "Применить HandPoint в сцену"
            )
        )
        {

            ApplyHandPoint();

        }


        EditorGUILayout.Space(
            12f
        );


        EditorGUILayout.LabelField(
            "Положение предмета",
            EditorStyles.boldLabel
        );


        EditorGUILayout.LabelField(
            itemJsonPath ??
            "JSON не найден",
            EditorStyles.miniLabel
        );


        EditorGUI.BeginChangeCheck();


        heldOffsetX =
            EditorGUILayout.Slider(
                "Offset X",
                heldOffsetX,
                -1.5f,
                1.5f
            );


        heldOffsetY =
            EditorGUILayout.Slider(
                "Offset Y",
                heldOffsetY,
                -1.5f,
                1.5f
            );


        heldScale =
            EditorGUILayout.Slider(
                "Scale",
                heldScale,
                0.05f,
                4f
            );


        heldRotation =
            EditorGUILayout.Slider(
                "Rotation",
                heldRotation,
                -180f,
                180f
            );


        if (
            EditorGUI.EndChangeCheck()
        )
        {

            UpdatePreviewHeldItemPose();

        }


        EditorGUILayout.Space(
            10f
        );


        EditorGUILayout.LabelField(
            "Камера предпросмотра",
            EditorStyles.boldLabel
        );


        previewZoom =
            EditorGUILayout.Slider(
                "Zoom",
                previewZoom,
                0.65f,
                2.5f
            );


        mirrorPreview =
            EditorGUILayout.Toggle(
                "Mirror X",
                mirrorPreview
            );


        if (
            GUILayout.Button(
                "Сбросить Preview"
            )
        )
        {

            previewPan =
                Vector2.zero;


            previewZoom =
                1.15f;


            animationTime =
                0f;


            mirrorPreview =
                false;

        }


        EditorGUILayout.Space(
            12f
        );


        if (
            GUILayout.Button(
                "Сохранить HandPoint + Item Pose",
                GUILayout.Height(
                    34f
                )
            )
        )
        {

            SaveAll();

        }


        EditorGUILayout.HelpBox(
            "Предпросмотр теперь является копией реального Player hierarchy. " +
            "Поэтому в нём видны Body, FrontArmPivot, BackArmPivot, ноги, голова и сам предмет. " +
            "Выбранный AnimationClip проигрывается прямо на этой копии.",
            MessageType.Info
        );

    }


    private void DrawAnimationControls()
    {

        EditorGUILayout.LabelField(
            "Анимация игрока",
            EditorStyles.boldLabel
        );


        if (
            clips ==
            null
            ||
            clips.Length ==
            0
        )
        {

            EditorGUILayout.HelpBox(
                "В RuntimeAnimatorController не найдено AnimationClip.",
                MessageType.Warning
            );


            return;

        }


        int newClip =
            EditorGUILayout.Popup(
                "Clip",
                selectedClipIndex,
                clipNames
            );


        if (
            newClip !=
            selectedClipIndex
        )
        {

            selectedClipIndex =
                newClip;


            animationTime =
                0f;


            playing =
                false;

        }


        AnimationClip clip =
            GetSelectedClip();


        EditorGUILayout.BeginHorizontal();


        if (
            GUILayout.Button(
                playing
                    ? "Пауза"
                    : "▶ Играть",
                GUILayout.Height(
                    26f
                )
            )
        )
        {

            playing =
                !playing;


            lastEditorTime =
                EditorApplication.timeSinceStartup;

        }


        if (
            GUILayout.Button(
                "■ Стоп",
                GUILayout.Height(
                    26f
                )
            )
        )
        {

            playing =
                false;


            animationTime =
                0f;

        }


        EditorGUILayout.EndHorizontal();


        loop =
            EditorGUILayout.Toggle(
                "Loop",
                loop
            );


        playbackSpeed =
            EditorGUILayout.Slider(
                "Speed",
                playbackSpeed,
                0.1f,
                3f
            );


        if (
            clip !=
            null
        )
        {

            float length =
                Mathf.Max(
                    0.0001f,
                    clip.length
                );


            animationTime =
                EditorGUILayout.Slider(
                    "Time",
                    animationTime,
                    0f,
                    length
                );


            EditorGUILayout.LabelField(
                "Length",
                clip.length.ToString(
                    "0.###"
                ) +
                " s"
            );

        }


        EditorGUILayout.Space(
            8f
        );


        previewMining =
            EditorGUILayout.Toggle(
                "Mining поверх клипа",
                previewMining
            );


        if (
            previewMining
        )
        {

            autoMiningSwing =
                EditorGUILayout.Toggle(
                    "Auto Swing",
                    autoMiningSwing
                );


            if (
                !autoMiningSwing
            )
            {

                manualMiningPhase =
                    EditorGUILayout.Slider(
                        "Swing Phase",
                        manualMiningPhase,
                        0f,
                        1f
                    );

            }


            EditorGUILayout.LabelField(
                "Mining overlay",
                "применяется после SampleAnimation"
            );

        }

    }


    // =====================================================
    // PREVIEW
    // =====================================================

    // =====================================================
    // PREVIEW LIFETIME
    // =====================================================

    private void CleanupPreview()
    {

        if (
            generatedItemSprite !=
            null
        )
        {

            DestroyImmediate(
                generatedItemSprite
            );


            generatedItemSprite =
                null;

        }


        if (
            previewPlayer !=
            null
        )
        {

            DestroyImmediate(
                previewPlayer
            );

        }


        previewPlayer =
            null;


        previewAnimator =
            null;


        previewAnimationRoot =
            null;


        previewFrontArmPivot =
            null;


        previewBackArmPivot =
            null;


        previewHandPoint =
            null;


        previewHeldRenderer =
            null;

    }


    private void RebuildPreview()
    {

        CleanupPreview();


        if (
            player ==
            null
        )
        {

            PlayerInventory inventory =
                FindScenePlayerInventory();


            if (
                inventory !=
                null
            )
            {

                player =
                    inventory.gameObject;

            }

        }


        if (
            player ==
            null
        )
        {

            return;

        }


        // V30:
        // The previous version instantiated the complete gameplay Player
        // and then DestroyImmediate()'d every MonoBehaviour in that copy.
        //
        // That is unsafe for preview because visual/runtime components may
        // change or clear the body hierarchy from OnDisable/OnDestroy.
        //
        // Build a clean visual hierarchy instead:
        // Transform + SpriteRenderer only.
        previewPlayer =
            CloneVisualPlayerHierarchy(
                player
            );


        if (
            previewPlayer ==
            null
        )
        {

            return;

        }


        previewPlayer.name =
            "HeldItemPosePreview_Player";


        previewPlayer.hideFlags =
            HideFlags.HideAndDontSave;


        previewPlayer.transform.position =
            Vector3.zero;


        previewPlayer.transform.rotation =
            Quaternion.identity;


        Vector3 sourceScale =
            player.transform.localScale;


        if (
            Mathf.Abs(
                sourceScale.x
            ) <
            0.0001f
        )
        {

            sourceScale.x =
                1f;

        }


        if (
            Mathf.Abs(
                sourceScale.y
            ) <
            0.0001f
        )
        {

            sourceScale.y =
                1f;

        }


        previewPlayer.transform.localScale =
            sourceScale;


        previewPlayer.SetActive(
            true
        );


        // No live Animator is needed for SampleAnimation.
        previewAnimator =
            null;


        previewAnimationRoot =
            previewPlayer.transform;


        if (
            sourceAnimator !=
            null
        )
        {

            string animatorPath =
                AnimationUtility.CalculateTransformPath(
                    sourceAnimator.transform,
                    player.transform
                );


            if (
                !string.IsNullOrEmpty(
                    animatorPath
                )
            )
            {

                Transform candidate =
                    previewPlayer.transform.Find(
                        animatorPath
                    );


                if (
                    candidate !=
                    null
                )
                {

                    previewAnimationRoot =
                        candidate;

                }

            }

        }


        previewFrontArmPivot =
            FindDeepChild(
                previewPlayer.transform,
                "FrontArmPivot"
            );


        previewBackArmPivot =
            FindDeepChild(
                previewPlayer.transform,
                "BackArmPivot"
            );


        if (
            previewFrontArmPivot !=
            null
        )
        {

            previewHandPoint =
                FindDeepChild(
                    previewFrontArmPivot,
                    "HandPoint"
                );


            if (
                previewHandPoint ==
                null
            )
            {

                GameObject hand =
                    new GameObject(
                        "HandPoint"
                    );


                hand.hideFlags =
                    HideFlags.HideAndDontSave;


                previewHandPoint =
                    hand.transform;


                previewHandPoint.SetParent(
                    previewFrontArmPivot,
                    false
                );

            }


            previewHandPoint.localPosition =
                new Vector3(
                    handLocalPosition.x,
                    handLocalPosition.y,
                    previewHandPoint.localPosition.z
                );

        }


        CreatePreviewHeldItem();


        Repaint();

    }


    private static GameObject CloneVisualPlayerHierarchy(
        GameObject source
    )
    {

        if (
            source ==
            null
        )
        {

            return null;

        }


        GameObject root =
            CloneVisualNode(
                source.transform,
                null,
                true
            );


        if (
            root !=
            null
        )
        {

            ApplyPreviewHideFlags(
                root.transform
            );

        }


        return root;

    }


    private static GameObject CloneVisualNode(
        Transform source,
        Transform parent,
        bool isRoot
    )
    {

        if (
            source ==
            null
        )
        {

            return null;

        }


        GameObject clone =
            new GameObject(
                source.name
            );


        clone.hideFlags =
            HideFlags.HideAndDontSave;


        clone.layer =
            source.gameObject.layer;


        if (
            parent !=
            null
        )
        {

            clone.transform.SetParent(
                parent,
                false
            );

        }


        if (
            isRoot
        )
        {

            clone.transform.localPosition =
                Vector3.zero;


            clone.transform.localRotation =
                Quaternion.identity;


            clone.transform.localScale =
                source.localScale;

        }
        else
        {

            clone.transform.localPosition =
                source.localPosition;


            clone.transform.localRotation =
                source.localRotation;


            clone.transform.localScale =
                source.localScale;

        }


        SpriteRenderer[] sourceRenderers =
            source.GetComponents<
                SpriteRenderer
            >();


        for (
            int i = 0;
            i < sourceRenderers.Length;
            i++
        )
        {

            SpriteRenderer sourceRenderer =
                sourceRenderers[i];


            if (
                sourceRenderer ==
                null
            )
            {

                continue;

            }


            SpriteRenderer renderer =
                clone.AddComponent<
                    SpriteRenderer
                >();


            CopySpriteRenderer(
                sourceRenderer,
                renderer
            );

        }


        for (
            int childIndex = 0;
            childIndex < source.childCount;
            childIndex++
        )
        {

            CloneVisualNode(
                source.GetChild(
                    childIndex
                ),
                clone.transform,
                false
            );

        }


        // Preview-only hierarchy: keep all nodes available.
        // Normal visibility is still preferred during DrawAnimatedPreview,
        // with a fallback if runtime scripts normally enable the body.
        clone.SetActive(
            true
        );


        return clone;

    }


    private static void CopySpriteRenderer(
        SpriteRenderer source,
        SpriteRenderer destination
    )
    {

        if (
            source ==
            null
            ||
            destination ==
            null
        )
        {

            return;

        }


        destination.sprite =
            source.sprite;


        destination.color =
            source.color;


        destination.flipX =
            source.flipX;


        destination.flipY =
            source.flipY;


        destination.sortingLayerID =
            source.sortingLayerID;


        destination.sortingOrder =
            source.sortingOrder;


        destination.maskInteraction =
            source.maskInteraction;


        destination.drawMode =
            source.drawMode;


        if (
            source.drawMode !=
            SpriteDrawMode.Simple
        )
        {

            destination.size =
                source.size;

        }


        destination.enabled =
            true;

    }


    private static void ApplyPreviewHideFlags(
        Transform root
    )
    {

        if (
            root ==
            null
        )
        {

            return;

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

            Transform current =
                all[i];


            if (
                current ==
                null
            )
            {

                continue;

            }


            current.gameObject.hideFlags =
                HideFlags.HideAndDontSave;

        }

    }


    private void CreatePreviewHeldItem()
    {

        if (
            previewHandPoint ==
            null
        )
        {

            return;

        }


        Transform old =
            previewHandPoint.Find(
                "HeldItem"
            );


        if (
            old !=
            null
        )
        {

            DestroyImmediate(
                old.gameObject
            );

        }


        GameObject item =
            new GameObject(
                "HeldItem"
            );


        item.hideFlags =
            HideFlags.HideAndDontSave;


        item.transform.SetParent(
            previewHandPoint,
            false
        );


        previewHeldRenderer =
            item.AddComponent<
                SpriteRenderer
            >();


        previewHeldRenderer.sprite =
            LoadPreviewItemSprite();


        SpriteRenderer body =
            FindNamedSpriteRenderer(
                previewPlayer,
                "Body"
            );


        if (
            body !=
            null
        )
        {

            previewHeldRenderer.sortingLayerID =
                body.sortingLayerID;


            previewHeldRenderer.sortingOrder =
                body.sortingOrder +
                10;

        }
        else
        {

            previewHeldRenderer.sortingOrder =
                100;

        }


        UpdatePreviewHeldItemPose();

    }


    private Sprite LoadPreviewItemSprite()
    {

        if (
            string.IsNullOrWhiteSpace(
                itemJsonPath
            )
        )
        {

            return null;

        }


        string json =
            File.ReadAllText(
                itemJsonPath
            );


        string texture =
            ReadString(
                json,
                "Texture"
            );


        if (
            string.IsNullOrWhiteSpace(
                texture
            )
        )
        {

            return null;

        }


        if (
            texture.EndsWith(
                ".png",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {

            texture =
                texture.Substring(
                    0,
                    texture.Length -
                    4
                );

        }


        string assetPath =
            "Assets/GameData/ResourcePacks/Default/textures/items/" +
            texture +
            ".png";


        Sprite importedSprite =
            AssetDatabase.LoadAssetAtPath<
                Sprite
            >(
                assetPath
            );


        if (
            importedSprite !=
            null
        )
        {

            return importedSprite;

        }


        Texture2D textureAsset =
            AssetDatabase.LoadAssetAtPath<
                Texture2D
            >(
                assetPath
            );


        if (
            textureAsset ==
            null
        )
        {

            return null;

        }


        generatedItemSprite =
            Sprite.Create(
                textureAsset,
                new Rect(
                    0f,
                    0f,
                    textureAsset.width,
                    textureAsset.height
                ),
                new Vector2(
                    0.5f,
                    0.5f
                ),
                16f,
                0,
                SpriteMeshType.FullRect
            );


        generatedItemSprite.name =
            "HeldItemPosePreview_" +
            itemId;


        return
            generatedItemSprite;

    }


    private void UpdatePreviewHandPoint()
    {

        if (
            previewHandPoint ==
            null
        )
        {

            return;

        }


        Vector3 local =
            previewHandPoint.localPosition;


        local.x =
            handLocalPosition.x;


        local.y =
            handLocalPosition.y;


        previewHandPoint.localPosition =
            local;


        Repaint();

    }


    private void UpdatePreviewHeldItemPose()
    {

        if (
            previewHeldRenderer ==
            null
        )
        {

            return;

        }


        Transform item =
            previewHeldRenderer.transform;


        item.localPosition =
            new Vector3(
                heldOffsetX,
                heldOffsetY,
                0f
            );


        item.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                heldRotation
            );


        Sprite sprite =
            previewHeldRenderer.sprite;


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


        const float heldWorldSize =
            0.58f;


        float scale =
            heldWorldSize /
            biggest *
            heldScale;


        item.localScale =
            Vector3.one *
            scale;


        Repaint();

    }


    private void SamplePreviewAnimation()
    {

        if (
            previewPlayer ==
            null
        )
        {

            return;

        }


        AnimationClip clip =
            GetSelectedClip();


        if (
            clip !=
            null
        )
        {

            GameObject sampleRoot =
                previewAnimationRoot !=
                null
                    ? previewAnimationRoot.gameObject
                    : previewPlayer;


            float time =
                clip.length >
                0f
                    ? Mathf.Clamp(
                        animationTime,
                        0f,
                        clip.length
                    )
                    : 0f;


            clip.SampleAnimation(
                sampleRoot,
                time
            );

        }


        // Re-apply values that belong to the held-item editor after
        // the locomotion clip has sampled the rig.
        UpdatePreviewHandPoint();

        UpdatePreviewHeldItemPose();


        if (
            mirrorPreview
        )
        {

            Vector3 scale =
                previewPlayer.transform.localScale;


            scale.x =
                -Mathf.Abs(
                    scale.x
                );


            previewPlayer.transform.localScale =
                scale;

        }
        else
        {

            Vector3 scale =
                previewPlayer.transform.localScale;


            scale.x =
                Mathf.Abs(
                    scale.x
                );


            previewPlayer.transform.localScale =
                scale;

        }


        ApplyPreviewMiningOverlay();

    }


    private void ApplyPreviewMiningOverlay()
    {

        if (
            !previewMining
            ||
            previewFrontArmPivot ==
            null
        )
        {

            return;

        }


        float phase;


        if (
            autoMiningSwing
        )
        {

            phase =
                Mathf.PingPong(
                    (float)
                    EditorApplication.timeSinceStartup *
                    miningSwingSpeed,
                    1f
                );

        }
        else
        {

            phase =
                manualMiningPhase;

        }


        phase =
            phase *
            phase *
            (
                3f -
                2f *
                phase
            );


        float front =
            Mathf.Lerp(
                frontArmBackAngle,
                frontArmForwardAngle,
                phase
            );


        float back =
            Mathf.Lerp(
                backArmBackAngle,
                backArmForwardAngle,
                phase
            );


        previewFrontArmPivot.localRotation =
            previewFrontArmPivot.localRotation *
            Quaternion.Euler(
                0f,
                0f,
                front
            );


        if (
            previewBackArmPivot !=
            null
        )
        {

            previewBackArmPivot.localRotation =
                previewBackArmPivot.localRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    back
                );

        }

    }


    private void DrawAnimatedPreview(
        Rect rect
    )
    {

        if (
            rect.width <=
            2f
            ||
            rect.height <=
            2f
        )
        {

            return;

        }


        EditorGUI.DrawRect(
            rect,
            new Color(
                0.055f,
                0.06f,
                0.075f,
                1f
            )
        );


        Rect inner =
            new Rect(
                rect.x +
                8f,
                rect.y +
                30f,
                rect.width -
                16f,
                rect.height -
                58f
            );


        EditorGUI.DrawRect(
            inner,
            new Color(
                0.085f,
                0.09f,
                0.11f,
                1f
            )
        );


        if (
            previewPlayer ==
            null
        )
        {

            GUI.Label(
                inner,
                "Выдели Player в Hierarchy",
                new GUIStyle(
                    EditorStyles.centeredGreyMiniLabel
                )
                {
                    alignment =
                        TextAnchor.MiddleCenter
                }
            );


            return;

        }


        HandlePreviewInput(
            inner
        );


        SamplePreviewAnimation();


        SpriteRenderer[] renderers =
            previewPlayer.GetComponentsInChildren<
                SpriteRenderer
            >(
                true
            );


        List<SpriteRenderer> visible =
            new List<
                SpriteRenderer
            >();


        // First use the renderers that would normally be visible.
        for (
            int i = 0;
            i < renderers.Length;
            i++
        )
        {

            SpriteRenderer renderer =
                renderers[i];


            if (
                renderer ==
                null
                ||
                renderer.sprite ==
                null
            )
            {

                continue;

            }


            if (
                renderer.enabled
                &&
                renderer.gameObject.activeInHierarchy
            )
            {

                visible.Add(
                    renderer
                );

            }

        }


        // Some Player rigs enable body parts from runtime scripts.
        // This preview intentionally contains no gameplay scripts.
        // If normal visibility produced nothing, draw every visual node
        // that has a real sprite instead of showing an empty preview.
        if (
            visible.Count ==
            0
        )
        {

            for (
                int i = 0;
                i < renderers.Length;
                i++
            )
            {

                SpriteRenderer renderer =
                    renderers[i];


                if (
                    renderer ==
                    null
                    ||
                    renderer.sprite ==
                    null
                )
                {

                    continue;

                }


                visible.Add(
                    renderer
                );

            }

        }


        if (
            visible.Count ==
            0
        )
        {

            GUI.Label(
                inner,
                "В preview-копии Player не найдено активных SpriteRenderer.",
                new GUIStyle(
                    EditorStyles.centeredGreyMiniLabel
                )
                {
                    alignment =
                        TextAnchor.MiddleCenter
                }
            );


            return;

        }


        visible.Sort(
            CompareSpriteRenderers
        );


        Bounds bounds =
            CalculateRendererBounds(
                visible
            );


        DrawSpriteRendererList(
            inner,
            visible,
            bounds
        );


        GUI.Label(
            new Rect(
                rect.x +
                10f,
                rect.y +
                7f,
                rect.width -
                20f,
                20f
            ),
            GetPreviewStatus(),
            EditorStyles.whiteMiniLabel
        );


        GUI.Label(
            new Rect(
                rect.x +
                10f,
                rect.yMax -
                22f,
                rect.width -
                20f,
                18f
            ),
            "Колесо — zoom, средняя кнопка — pan. Preview рисуется напрямую из SpriteRenderer.",
            EditorStyles.whiteMiniLabel
        );

    }


    private static int CompareSpriteRenderers(
        SpriteRenderer a,
        SpriteRenderer b
    )
    {

        int layerA =
            SortingLayer.GetLayerValueFromID(
                a.sortingLayerID
            );


        int layerB =
            SortingLayer.GetLayerValueFromID(
                b.sortingLayerID
            );


        int result =
            layerA.CompareTo(
                layerB
            );


        if (
            result !=
            0
        )
        {

            return result;

        }


        result =
            a.sortingOrder.CompareTo(
                b.sortingOrder
            );


        if (
            result !=
            0
        )
        {

            return result;

        }


        // In 2D projects lower Z is commonly closer to camera.
        return
            b.transform.position.z.CompareTo(
                a.transform.position.z
            );

    }


    private static Bounds CalculateRendererBounds(
        List<SpriteRenderer> renderers
    )
    {

        bool initialized =
            false;


        Bounds result =
            new Bounds();


        for (
            int i = 0;
            i < renderers.Count;
            i++
        )
        {

            SpriteRenderer renderer =
                renderers[i];


            if (
                renderer ==
                null
                ||
                renderer.sprite ==
                null
            )
            {

                continue;

            }


            Sprite sprite =
                renderer.sprite;


            float pixelsPerUnit =
                Mathf.Max(
                    0.0001f,
                    sprite.pixelsPerUnit
                );


            // Use the full sprite RECT and pivot, not sprite.bounds.
            //
            // sprite.bounds may be based on a tight mesh. Drawing the full
            // textureRect into those mesh bounds changes the apparent aspect
            // ratio and shifts body parts in the preview.
            float left =
                -sprite.pivot.x /
                pixelsPerUnit;


            float right =
                (
                    sprite.rect.width -
                    sprite.pivot.x
                )
                /
                pixelsPerUnit;


            float bottom =
                -sprite.pivot.y /
                pixelsPerUnit;


            float top =
                (
                    sprite.rect.height -
                    sprite.pivot.y
                )
                /
                pixelsPerUnit;


            Vector3[] corners =
            {
                new Vector3(
                    left,
                    bottom,
                    0f
                ),

                new Vector3(
                    left,
                    top,
                    0f
                ),

                new Vector3(
                    right,
                    bottom,
                    0f
                ),

                new Vector3(
                    right,
                    top,
                    0f
                )
            };


            for (
                int cornerIndex = 0;
                cornerIndex < corners.Length;
                cornerIndex++
            )
            {

                Vector3 world =
                    renderer.transform.TransformPoint(
                        corners[
                            cornerIndex
                        ]
                    );


                if (
                    !initialized
                )
                {

                    result =
                        new Bounds(
                            world,
                            Vector3.zero
                        );


                    initialized =
                        true;

                }
                else
                {

                    result.Encapsulate(
                        world
                    );

                }

            }

        }


        if (
            !initialized
        )
        {

            return
                new Bounds(
                    Vector3.zero,
                    new Vector3(
                        2f,
                        2.5f,
                        1f
                    )
                );

        }


        if (
            result.size.x <
            0.01f
        )
        {

            result.Expand(
                new Vector3(
                    1f,
                    0f,
                    0f
                )
            );

        }


        if (
            result.size.y <
            0.01f
        )
        {

            result.Expand(
                new Vector3(
                    0f,
                    1f,
                    0f
                )
            );

        }


        return result;

    }


    private void DrawSpriteRendererList(
        Rect rect,
        List<SpriteRenderer> renderers,
        Bounds bounds
    )
    {

        float paddedWidth =
            Mathf.Max(
                0.25f,
                bounds.size.x
            );


        float paddedHeight =
            Mathf.Max(
                0.25f,
                bounds.size.y
            );


        float fitX =
            rect.width /
            paddedWidth;


        float fitY =
            rect.height /
            paddedHeight;


        float pixelsPerWorldUnit =
            Mathf.Min(
                fitX,
                fitY
            )
            /
            Mathf.Max(
                0.05f,
                previewZoom
            )
            *
            0.82f;


        Vector2 worldCenter =
            new Vector2(
                bounds.center.x +
                previewPan.x,

                bounds.center.y +
                previewPan.y
            );


        Vector2 screenCenter =
            rect.center;


        for (
            int i = 0;
            i < renderers.Count;
            i++
        )
        {

            DrawOneSpriteRenderer(
                renderers[i],
                worldCenter,
                screenCenter,
                pixelsPerWorldUnit
            );

        }

    }


    private static void DrawOneSpriteRenderer(
        SpriteRenderer renderer,
        Vector2 worldCenter,
        Vector2 screenCenter,
        float pixelsPerWorldUnit
    )
    {

        Sprite sprite =
            renderer.sprite;


        if (
            sprite ==
            null
            ||
            sprite.texture ==
            null
        )
        {

            return;

        }


        float spritePixelsPerUnit =
            Mathf.Max(
                0.0001f,
                sprite.pixelsPerUnit
            );


        // Local rectangle of the actual sprite texture relative to its pivot.
        //
        // This is the important difference from the old preview:
        // we do NOT stretch textureRect to Sprite.bounds/AABB.
        float left =
            -sprite.pivot.x /
            spritePixelsPerUnit;


        float bottom =
            -sprite.pivot.y /
            spritePixelsPerUnit;


        float width =
            sprite.rect.width /
            spritePixelsPerUnit;


        float height =
            sprite.rect.height /
            spritePixelsPerUnit;


        Rect localRect =
            new Rect(
                left,
                bottom,
                width,
                height
            );


        Rect textureRect =
            sprite.textureRect;


        Rect uv =
            new Rect(
                textureRect.x /
                sprite.texture.width,

                textureRect.y /
                sprite.texture.height,

                textureRect.width /
                sprite.texture.width,

                textureRect.height /
                sprite.texture.height
            );


        bool flipX =
            renderer.flipX;


        bool flipY =
            renderer.flipY;


        if (
            flipX
        )
        {

            uv.x +=
                uv.width;


            uv.width =
                -uv.width;

        }


        if (
            flipY
        )
        {

            uv.y +=
                uv.height;


            uv.height =
                -uv.height;

        }


        // Build an exact local -> preview-screen affine transform from the
        // SpriteRenderer Transform. This preserves nested rotations,
        // non-uniform scales and even shear produced by scaled parents.
        Vector3 worldOrigin =
            renderer.transform.TransformPoint(
                Vector3.zero
            );


        Vector3 worldX =
            renderer.transform.TransformPoint(
                Vector3.right
            );


        Vector3 worldY =
            renderer.transform.TransformPoint(
                Vector3.up
            );


        Vector2 screenOrigin =
            WorldToPreviewPoint(
                worldOrigin,
                worldCenter,
                screenCenter,
                pixelsPerWorldUnit
            );


        Vector2 screenX =
            WorldToPreviewPoint(
                worldX,
                worldCenter,
                screenCenter,
                pixelsPerWorldUnit
            );


        Vector2 screenY =
            WorldToPreviewPoint(
                worldY,
                worldCenter,
                screenCenter,
                pixelsPerWorldUnit
            );


        Vector2 basisX =
            screenX -
            screenOrigin;


        Vector2 basisY =
            screenY -
            screenOrigin;


        Matrix4x4 localToScreen =
            Matrix4x4.identity;


        localToScreen.m00 =
            basisX.x;


        localToScreen.m01 =
            basisY.x;


        localToScreen.m03 =
            screenOrigin.x;


        localToScreen.m10 =
            basisX.y;


        localToScreen.m11 =
            basisY.y;


        localToScreen.m13 =
            screenOrigin.y;


        Matrix4x4 oldMatrix =
            GUI.matrix;


        Color oldColor =
            GUI.color;


        GUI.color =
            renderer.color;


        GUI.matrix =
            oldMatrix *
            localToScreen;


        GUI.DrawTextureWithTexCoords(
            localRect,
            sprite.texture,
            uv,
            true
        );


        GUI.matrix =
            oldMatrix;


        GUI.color =
            oldColor;

    }


    private static Vector2 WorldToPreviewPoint(
        Vector3 world,
        Vector2 worldCenter,
        Vector2 screenCenter,
        float pixelsPerWorldUnit
    )
    {

        return
            new Vector2(
                screenCenter.x +
                (
                    world.x -
                    worldCenter.x
                )
                *
                pixelsPerWorldUnit,

                screenCenter.y -
                (
                    world.y -
                    worldCenter.y
                )
                *
                pixelsPerWorldUnit
            );

    }


    private string GetPreviewStatus()
    {

        AnimationClip clip =
            GetSelectedClip();


        if (
            clip ==
            null
        )
        {

            return
                "Реальный Player rig — без выбранного clip";

        }


        return
            clip.name +
            "  " +
            animationTime.ToString(
                "0.00"
            ) +
            " / " +
            clip.length.ToString(
                "0.00"
            ) +
            (
                previewMining
                    ? "   + Mining Overlay"
                    : string.Empty
            );

    }


    private void HandlePreviewInput(
        Rect rect
    )
    {

        Event current =
            Event.current;


        if (
            !rect.Contains(
                current.mousePosition
            )
        )
        {

            return;

        }


        if (
            current.type ==
            EventType.ScrollWheel
        )
        {

            previewZoom =
                Mathf.Clamp(
                    previewZoom +
                    current.delta.y *
                    0.035f,
                    0.65f,
                    2.5f
                );


            current.Use();


            Repaint();

        }


        if (
            current.type ==
            EventType.MouseDrag
            &&
            current.button ==
            2
        )
        {

            // Approximate GUI pixels -> world units.
            float worldUnitsPerPixel =
                0.01f *
                previewZoom;


            previewPan +=
                new Vector2(
                    -current.delta.x *
                    worldUnitsPerPixel,

                    current.delta.y *
                    worldUnitsPerPixel
                );


            current.Use();


            Repaint();

        }

    }


    private static SpriteRenderer FindNamedSpriteRenderer(
        GameObject root,
        string name
    )
    {

        Transform target =
            FindDeepChild(
                root.transform,
                name
            );


        return
            target !=
            null
                ? target.GetComponent<
                    SpriteRenderer
                >()
                : null;

    }


    // =====================================================
    // APPLY / SAVE
    // =====================================================

    private void ApplyHandPoint()
    {

        if (
            sourceHandPoint ==
            null
        )
        {

            return;

        }


        Undo.RecordObject(
            sourceHandPoint,
            "Move HandPoint"
        );


        Vector3 local =
            sourceHandPoint.localPosition;


        local.x =
            handLocalPosition.x;


        local.y =
            handLocalPosition.y;


        sourceHandPoint.localPosition =
            local;


        EditorUtility.SetDirty(
            sourceHandPoint
        );


        if (
            sourceHandPoint.gameObject.scene.IsValid()
        )
        {

            EditorSceneManager.MarkSceneDirty(
                sourceHandPoint.gameObject.scene
            );

        }

    }


    private void SaveAll()
    {

        ApplyHandPoint();

        SaveItemJson();

    }


    // =====================================================
    // ITEM JSON
    // =====================================================

    private void LoadItem()
    {

        itemJsonPath =
            FindItemJson(
                itemId
            );


        heldOffsetX =
            0f;


        heldOffsetY =
            0f;


        heldScale =
            1f;


        heldRotation =
            0f;


        if (
            string.IsNullOrWhiteSpace(
                itemJsonPath
            )
        )
        {

            return;

        }


        string json =
            File.ReadAllText(
                itemJsonPath
            );


        heldOffsetX =
            ReadFloat(
                json,
                "HeldOffsetX",
                0f
            );


        heldOffsetY =
            ReadFloat(
                json,
                "HeldOffsetY",
                0f
            );


        heldScale =
            ReadFloat(
                json,
                "HeldScale",
                1f
            );


        heldRotation =
            ReadFloat(
                json,
                "HeldRotation",
                0f
            );

    }


    private void SaveItemJson()
    {

        if (
            string.IsNullOrWhiteSpace(
                itemJsonPath
            )
        )
        {

            EditorUtility.DisplayDialog(
                "Held Item Pose",
                "Не найден JSON для " +
                itemId,
                "OK"
            );


            return;

        }


        string json =
            File.ReadAllText(
                itemJsonPath
            );


        json =
            SetFloat(
                json,
                "HeldOffsetX",
                heldOffsetX
            );


        json =
            SetFloat(
                json,
                "HeldOffsetY",
                heldOffsetY
            );


        json =
            SetFloat(
                json,
                "HeldScale",
                heldScale
            );


        json =
            SetFloat(
                json,
                "HeldRotation",
                heldRotation
            );


        File.WriteAllText(
            itemJsonPath,
            json
        );


        AssetDatabase.Refresh();


        HeldItemPoseRegistry.Reload();


        EditorUtility.DisplayDialog(
            "Held Item Pose",
            "HandPoint и item pose сохранены.",
            "OK"
        );

    }


    private static string FindItemJson(
        string targetId
    )
    {

        if (
            string.IsNullOrWhiteSpace(
                targetId
            )
        )
        {

            return null;

        }


        string folder =
            Path.Combine(
                Application.dataPath,
                "GameData",
                "Items"
            );


        if (
            !Directory.Exists(
                folder
            )
        )
        {

            return null;

        }


        string[] files =
            Directory.GetFiles(
                folder,
                "*.json",
                SearchOption.AllDirectories
            );


        for (
            int i = 0;
            i < files.Length;
            i++
        )
        {

            string json =
                File.ReadAllText(
                    files[i]
                );


            if (
                string.Equals(
                    ReadString(
                        json,
                        "ID"
                    ),
                    targetId,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {

                return files[i];

            }

        }


        return null;

    }


    private static string ReadString(
        string json,
        string field
    )
    {

        Match match =
            Regex.Match(
                json,
                "\"" +
                Regex.Escape(
                    field
                ) +
                "\"\\s*:\\s*\"([^\"]*)\""
            );


        return
            match.Success
                ? match.Groups[
                    1
                ].Value
                : null;

    }


    private static float ReadFloat(
        string json,
        string field,
        float fallback
    )
    {

        Match match =
            Regex.Match(
                json,
                "\"" +
                Regex.Escape(
                    field
                ) +
                "\"\\s*:\\s*(-?[0-9]+(?:\\.[0-9]+)?)"
            );


        if (
            !match.Success
        )
        {

            return fallback;

        }


        if (
            float.TryParse(
                match.Groups[
                    1
                ].Value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out float value
            )
        )
        {

            return value;

        }


        return fallback;

    }


    private static string SetFloat(
        string json,
        string field,
        float value
    )
    {

        string number =
            value.ToString(
                "0.####",
                System.Globalization.CultureInfo.InvariantCulture
            );


        string pattern =
            "\"" +
            Regex.Escape(
                field
            ) +
            "\"\\s*:\\s*-?[0-9]+(?:\\.[0-9]+)?";


        string replacement =
            "\"" +
            field +
            "\": " +
            number;


        Regex regex =
            new Regex(
                pattern
            );


        if (
            regex.IsMatch(
                json
            )
        )
        {

            return
                regex.Replace(
                    json,
                    replacement,
                    1
                );

        }


        int lastBrace =
            json.LastIndexOf(
                '}'
            );


        if (
            lastBrace <
            0
        )
        {

            return json;

        }


        string before =
            json.Substring(
                0,
                lastBrace
            ).TrimEnd();


        if (
            before.Length >
            0
            &&
            before[
                before.Length -
                1
            ]
            !=
            ','
        )
        {

            before +=
                ",";

        }


        return
            before +
            "\n  " +
            replacement +
            "\n}";

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

}

#endif
