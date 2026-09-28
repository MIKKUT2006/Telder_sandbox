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
using Game.Combat;


public partial class HeldItemPoseEditorWindow :
    EditorWindow
{
    private enum ArmSide
    {
        Front,
        Back
    }


    private enum ToolMode
    {
        ArmPosition,
        Pivot,
        HandPoint,
        FistPoint,
        Item,
        WeaponAim
    }


    // =====================================================
    // SOURCE
    // =====================================================

    private GameObject player;

    private Animator sourceAnimator;

    private Transform sourceFrontArm;

    private Transform sourceBackArm;

    private Transform sourceHandPoint;

    private ArmMiningOverlayController sourceOverlay;


    // =====================================================
    // ARM SCENE POSITIONS
    // =====================================================

    private Vector2 frontArmScenePosition;

    private Vector2 backArmScenePosition;


    // =====================================================
    // ROTATION POINTS
    // =====================================================

    private Vector2 frontPivotPoint;

    private Vector2 backPivotPoint;


    // =====================================================
    // SWING
    // =====================================================

    private bool previewSwing =
        true;


    private bool autoSwing;


    private float swingPhase =
        0.5f;


    private float swingSpeed =
        5.5f;


    private float frontBackAngle =
        52f;


    private float frontForwardAngle =
        -58f;


    private float backBackAngle =
        -12f;


    private float backForwardAngle =
        18f;


    // =====================================================
    // HAND / ITEM
    // =====================================================

    private Vector2 handLocalPosition =
        new Vector2(
            0.28f,
            0f
        );


    private string itemId =
        "game:wood_pickaxe";


    private string itemJsonPath;


    private float heldOffsetX;

    private float heldOffsetY;

    private float heldScale =
        1f;

    private float heldRotation;



    // =====================================================
    // WEAPON EDITOR
    // =====================================================

    private void DrawWeaponEditor()
    {
        EditorGUILayout.LabelField(
            "Оружие",
            EditorStyles.boldLabel
        );


        if (!weaponEnabled)
        {
            EditorGUILayout.HelpBox(
                "У этого предмета нет секции Weapon.",
                MessageType.None
            );


            if (
                GUILayout.Button(
                    "Сделать предмет оружием"
                )
            )
            {
                weapon =
                    new WeaponMetadata();


                InitializeWeaponFistPointsFromRig();


                weapon.Normalize();


                weaponEnabled =
                    true;


                weaponPreviewEnabled =
                    false;


                weaponPhase =
                    0f;
            }


            return;
        }


        WeaponKind kind =
            weapon.GetKind();


        kind =
            (WeaponKind)
            EditorGUILayout.EnumPopup(
                "Тип",
                kind
            );


        weapon.Kind =
            kind.ToString();


        weapon.Damage =
            EditorGUILayout.FloatField(
                "Урон",
                weapon.Damage
            );


        weapon.AttackSpeed =
            EditorGUILayout.FloatField(
                "Атак / сек",
                weapon.AttackSpeed
            );


        weapon.Knockback =
            EditorGUILayout.FloatField(
                "Отбрасывание",
                weapon.Knockback
            );


        weapon.Range =
            EditorGUILayout.FloatField(
                "Базовая дальность",
                weapon.Range
            );


        weapon.TrackCursorDuringAttack =
            EditorGUILayout.Toggle(
                "Следить за курсором",
                weapon.TrackCursorDuringAttack
            );


        EditorGUILayout.Space(
            5f
        );


        EditorGUILayout.LabelField(
            "Руки",
            EditorStyles.boldLabel
        );


        weapon.ArmAimOffset =
            EditorGUILayout.Slider(
                "Front Arm fine correction",
                weapon.ArmAimOffset,
                -180f,
                180f
            );


        weapon.UseBackArm =
            EditorGUILayout.Toggle(
                "Использовать Back Arm",
                weapon.UseBackArm
            );


        if (weapon.UseBackArm)
        {
            weapon.BackArmAimOffset =
                EditorGUILayout.Slider(
                    "Back Arm fine correction",
                    weapon.BackArmAimOffset,
                    -180f,
                    180f
                );
        }


        EditorGUILayout.Space(
            5f
        );


        EditorGUILayout.LabelField(
            "Точки кулаков",
            EditorStyles.boldLabel
        );


        EditorGUILayout.HelpBox(
            "FRONT/BACK → КУЛАК → ЛКМ прямо по центру видимого кулака. " +
            "При прицеливании линия ПЛЕЧО → КУЛАК совмещается с направлением на курсор.",
            MessageType.Info
        );


        Vector2 nextFrontFist =
            EditorGUILayout.Vector2Field(
                "Front Fist Local",
                weapon.FrontFistPointLocal
            );


        if (nextFrontFist != weapon.FrontFistPointLocal)
        {
            weapon.FrontFistPointLocal =
                nextFrontFist;

            weapon.FistPointsConfigured =
                true;
        }


        Vector2 nextBackFist =
            EditorGUILayout.Vector2Field(
                "Back Fist Local",
                weapon.BackFistPointLocal
            );


        if (nextBackFist != weapon.BackFistPointLocal)
        {
            weapon.BackFistPointLocal =
                nextBackFist;

            weapon.FistPointsConfigured =
                true;
        }


        EditorGUILayout.BeginHorizontal();


        if (
            GUILayout.Button(
                "FRONT = HandPoint"
            )
        )
        {
            SetFrontFistFromHandPoint();
        }


        if (
            GUILayout.Button(
                "BACK = FRONT"
            )
        )
        {
            weapon.BackFistPointLocal =
                weapon.FrontFistPointLocal;

            weapon.FistPointsConfigured =
                true;
        }


        EditorGUILayout.EndHorizontal();


        EditorGUILayout.Space(
            7f
        );


        switch (kind)
        {
            case WeaponKind.Sword:
                DrawSwordWeaponEditor();
                break;

            case WeaponKind.Spear:
                DrawSpearWeaponEditor();
                break;

            case WeaponKind.Bow:
                DrawBowWeaponEditor();
                DrawProjectileWeaponEditor();
                break;

            case WeaponKind.Gun:
                DrawGunWeaponEditor();
                DrawProjectileWeaponEditor();
                break;
        }


        EditorGUILayout.Space(
            8f
        );


        EditorGUILayout.LabelField(
            "Предпросмотр атаки",
            EditorStyles.boldLabel
        );


        weaponPreviewEnabled =
            EditorGUILayout.Toggle(
                "Показывать атаку",
                weaponPreviewEnabled
            );


        showWeaponGuides =
            EditorGUILayout.Toggle(
                "Направляющие",
                showWeaponGuides
            );


        weaponAimAngle =
            EditorGUILayout.Slider(
                "Курсор: угол",
                weaponAimAngle,
                -180f,
                180f
            );


        weaponAimDistance =
            EditorGUILayout.Slider(
                "Курсор: расстояние",
                weaponAimDistance,
                0.4f,
                8f
            );


        weaponAutoPreview =
            EditorGUILayout.Toggle(
                "Автопроигрывание",
                weaponAutoPreview
            );


        if (weaponAutoPreview)
        {
            weaponPreviewSpeed =
                EditorGUILayout.Slider(
                    "Preview Speed",
                    weaponPreviewSpeed,
                    0.1f,
                    4f
                );
        }
        else
        {
            weaponPhase =
                EditorGUILayout.Slider(
                    kind == WeaponKind.Bow
                        ? "Натяжение"
                        : "Фаза атаки",
                    weaponPhase,
                    0f,
                    1f
                );
        }


        if (kind == WeaponKind.Sword)
        {
            swordPreviewAttack =
                (SwordAttackKind)
                EditorGUILayout.EnumPopup(
                    "Атака preview",
                    swordPreviewAttack
                );
        }


        if (
            kind == WeaponKind.Bow
            ||
            kind == WeaponKind.Gun
        )
        {
            projectilePreviewTime =
                EditorGUILayout.Slider(
                    "Projectile time",
                    projectilePreviewTime,
                    0f,
                    1.2f
                );
        }


        EditorGUILayout.HelpBox(
            "Выбери сверху режим «АТАКА» и двигай красную точку ЛКМ. " +
            "Замах меча, выпад, копьё, лук и стрельба считаются относительно этой точки.",
            MessageType.Info
        );


        weapon.Normalize();
    }


    private void DrawSwordWeaponEditor()
    {
        EditorGUILayout.LabelField(
            "Меч",
            EditorStyles.boldLabel
        );


        string pattern =
            weapon.SwordPattern != null
            &&
            weapon.SwordPattern.Length > 0
                ? string.Join(
                    ", ",
                    weapon.SwordPattern
                )
                : "Swing, Thrust";


        string nextPattern =
            EditorGUILayout.TextField(
                "Порядок атак",
                pattern
            );


        if (nextPattern != pattern)
        {
            string[] values =
                nextPattern.Split(
                    new[]
                    {
                        ',',
                        ';'
                    },
                    StringSplitOptions.RemoveEmptyEntries
                );


            for (
                int i = 0;
                i < values.Length;
                i++
            )
            {
                values[i] =
                    values[i].Trim();
            }


            weapon.SwordPattern =
                values;
        }


        weapon.SwordSwingStartAngle =
            EditorGUILayout.Slider(
                "Замах Start",
                weapon.SwordSwingStartAngle,
                -180f,
                180f
            );


        weapon.SwordSwingEndAngle =
            EditorGUILayout.Slider(
                "Замах End",
                weapon.SwordSwingEndAngle,
                -180f,
                180f
            );


        weapon.SwingReach =
            EditorGUILayout.FloatField(
                "Swing Reach",
                weapon.SwingReach
            );


        weapon.SwingHitRadius =
            EditorGUILayout.FloatField(
                "Swing Hit Radius",
                weapon.SwingHitRadius
            );


        weapon.ThrustDistance =
            EditorGUILayout.FloatField(
                "Выпад: длина",
                weapon.ThrustDistance
            );


        weapon.ThrustHitRadius =
            EditorGUILayout.FloatField(
                "Выпад: Hit Radius",
                weapon.ThrustHitRadius
            );


        if (
            GUILayout.Button(
                "Нормальный замах к курсору"
            )
        )
        {
            weapon.SwordSwingStartAngle =
                100f;


            weapon.SwordSwingEndAngle =
                -30f;
        }


        EditorGUILayout.HelpBox(
            "Swing: углы считаются ОТ направления к красной точке. " +
            "Thrust: рука смотрит прямо на курсор, предмет выдвигается по этой линии.",
            MessageType.None
        );
    }


    private void DrawSpearWeaponEditor()
    {
        EditorGUILayout.LabelField(
            "Копьё",
            EditorStyles.boldLabel
        );


        weapon.SpearBaseReach =
            EditorGUILayout.FloatField(
                "Base Reach",
                weapon.SpearBaseReach
            );


        weapon.SpearThrustDistance =
            EditorGUILayout.FloatField(
                "Thrust Distance",
                weapon.SpearThrustDistance
            );


        weapon.SpearHitRadius =
            EditorGUILayout.FloatField(
                "Hit Radius",
                weapon.SpearHitRadius
            );
    }


private void DrawBowWeaponEditor()
{
    EditorGUILayout.LabelField(
        "Лук",
        EditorStyles.boldLabel
    );


    weapon.UseBackArm =
        true;


    weapon.BowChargeTime =
        EditorGUILayout.FloatField(
            "Полное натяжение, сек",
            weapon.BowChargeTime
        );


    weapon.BowMinPower =
        EditorGUILayout.Slider(
            "Минимальная сила",
            weapon.BowMinPower,
            0f,
            1f
        );


    weapon.BowDrawDistance =
        EditorGUILayout.Slider(
            "Отвод второй руки назад",
            weapon.BowDrawDistance,
            0f,
            2.5f
        );


    weapon.BowHeldItemFullDrawOffset =
        EditorGUILayout.FloatField(
            "Сдвиг лука при натяжении",
            weapon.BowHeldItemFullDrawOffset
        );


    if (
        GUILayout.Button(
            "Нормальное натяжение: 0.65 назад / 0.5 сек"
        )
    )
    {
        weapon.BowDrawDistance =
            0.65f;


        weapon.BowChargeTime =
            0.5f;


        weapon.BackArmAimOffset =
            0f;
    }


    EditorGUILayout.HelpBox(
        "Обе руки всё время направлены в сторону красной точки. " +
        "При натяжении задняя рука НЕ разворачивается — " +
        "весь Back Arm просто отъезжает назад по линии выстрела.",
        MessageType.Info
    );
}


    private void DrawGunWeaponEditor()
    {
        EditorGUILayout.LabelField(
            "Пушка",
            EditorStyles.boldLabel
        );


        weapon.FireNormalizedTime =
            EditorGUILayout.Slider(
                "Момент выстрела",
                weapon.FireNormalizedTime,
                0f,
                1f
            );
    }


    private void DrawProjectileWeaponEditor()
    {
        EditorGUILayout.Space(
            5f
        );


        EditorGUILayout.LabelField(
            "Снаряд",
            EditorStyles.boldLabel
        );


        weapon.AmmoItem =
            EditorGUILayout.TextField(
                "Ammo Item",
                weapon.AmmoItem ?? ""
            );


        weapon.AmmoPerShot =
            EditorGUILayout.IntField(
                "Расход ammo",
                weapon.AmmoPerShot
            );


        weapon.ProjectileItem =
            EditorGUILayout.TextField(
                "Projectile Item",
                weapon.ProjectileItem ?? ""
            );


        weapon.ProjectileCount =
            EditorGUILayout.IntField(
                "Количество",
                weapon.ProjectileCount
            );


        weapon.ProjectileSpeed =
            EditorGUILayout.FloatField(
                "Скорость",
                weapon.ProjectileSpeed
            );


        weapon.ProjectileGravity =
            EditorGUILayout.FloatField(
                "Гравитация",
                weapon.ProjectileGravity
            );


        weapon.ProjectileLifetime =
            EditorGUILayout.FloatField(
                "Lifetime",
                weapon.ProjectileLifetime
            );


        weapon.MuzzleOffset =
            EditorGUILayout.FloatField(
                "Muzzle Offset",
                weapon.MuzzleOffset
            );


        weapon.ProjectileWorldSize =
            EditorGUILayout.FloatField(
                "Размер снаряда",
                weapon.ProjectileWorldSize
            );


        weapon.ProjectileScale =
            EditorGUILayout.FloatField(
                "Доп. Scale",
                weapon.ProjectileScale
            );


        weapon.RotateProjectileToVelocity =
            EditorGUILayout.Toggle(
                "Наклонять по полёту",
                weapon.RotateProjectileToVelocity
            );


        weapon.SpreadDegrees =
            EditorGUILayout.Slider(
                "Разброс",
                weapon.SpreadDegrees,
                0f,
                180f
            );


        string[] spreadModes =
        {
            "Random",
            "Even"
        };


        int spreadIndex =
            string.Equals(
                weapon.SpreadMode,
                "Even",
                StringComparison.OrdinalIgnoreCase
            )
                ? 1
                : 0;


        spreadIndex =
            EditorGUILayout.Popup(
                "Spread Mode",
                spreadIndex,
                spreadModes
            );


        weapon.SpreadMode =
            spreadModes[
                spreadIndex
            ];


        weapon.ProjectileDamageMultiplier =
            EditorGUILayout.FloatField(
                "Damage Multiplier",
                weapon.ProjectileDamageMultiplier
            );
    }


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


    private int selectedClip;

    private float animationTime;



    // =====================================================
    // WEAPON
    // =====================================================

    [Serializable]
    private sealed class WeaponItemRoot
    {
        public string ID;
        public WeaponMetadata Weapon;
    }


    [Serializable]
    private sealed class JsonSyntaxProbe
    {
        public string ID;
    }


    private WeaponMetadata weapon =
        new WeaponMetadata();


    private bool weaponEnabled;


    // Off by default: simply loading an item must show its normal held pose.
    private bool weaponPreviewEnabled;


    private bool weaponAutoPreview;


    private float weaponPhase;


    private float weaponPreviewSpeed =
        1.4f;


    // Red point in preview. All sword/spear/ranged directions are relative to it.
    private float weaponAimAngle;


    private float weaponAimDistance =
        3f;


    private SwordAttackKind swordPreviewAttack =
        SwordAttackKind.Swing;


    private bool showWeaponGuides =
        true;


    private float projectilePreviewTime =
        0.22f;


    private GameObject previewProjectileObject;


    private SpriteRenderer previewProjectileRenderer;


    private Sprite generatedProjectileSprite;


    private string previewProjectileItemId;


    // =====================================================
    // PREVIEW
    // =====================================================

    private GameObject previewPlayer;

    private Animator previewAnimator;

    private Transform previewFrontArm;

    private Transform previewBackArm;

    private Transform previewHandPoint;

    private SpriteRenderer previewHeldRenderer;

    private Sprite generatedItemSprite;


    private float previewZoom =
        1.15f;


    private Vector2 previewPan;


    private ArmSide armSide =
        ArmSide.Front;


    private ToolMode toolMode =
        ToolMode.Pivot;


    private bool snapToPixel =
        true;


    private Vector2 inspectorScroll;


    private bool saveQueued;

    private string status;


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
                1000f,
                650f
            );
    }


    // =====================================================
    // UNITY
    // =====================================================

private void OnEnable()
{
    EditorApplication.update +=
        EditorTick;


    TryUseSelection();


    if (player == null)
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
                inventory == null
                ||
                !inventory.gameObject
                    .scene
                    .IsValid()
            )
            {
                continue;
            }


            SetPlayer(
                inventory.gameObject
            );


            break;
        }
    }
}


    private void OnDisable()
    {
        EditorApplication.update -=
            EditorTick;


        CleanupPreview();
    }


    private void OnSelectionChange()
    {
        TryUseSelection();

        Repaint();
    }


private void EditorTick()
{
    bool needsRepaint =
        false;


    if (
        previewSwing
        &&
        autoSwing
        &&
        !weaponPreviewEnabled
    )
    {
        swingPhase =
            Mathf.PingPong(
                (float)
                EditorApplication.timeSinceStartup
                *
                swingSpeed
                *
                0.18f,
                1f
            );


        needsRepaint =
            true;
    }


    if (
        weaponEnabled
        &&
        weaponPreviewEnabled
        &&
        weaponAutoPreview
    )
    {
        needsRepaint =
            true;
    }


    if (needsRepaint)
    {
        Repaint();
    }
}


    // =====================================================
    // PLAYER
    // =====================================================

    private void TryUseSelection()
    {
        GameObject selected =
            Selection.activeGameObject;


        if (selected == null)
            return;


        PlayerInventory inventory =
            selected.GetComponent<
                PlayerInventory
            >();


        if (inventory == null)
        {
            inventory =
                selected.GetComponentInParent<
                    PlayerInventory
                >();
        }


        if (inventory != null)
        {
            SetPlayer(
                inventory.gameObject
            );
        }
    }


    private void SetPlayer(
        GameObject value
    )
    {
        if (player == value)
            return;


        player =
            value;


        ResolveSource();

        ReadOverlay();

        ReadHandPoint();

        CollectClips();

        LoadItem();

        QueueRebuildPreview();
    }


    private void ResolveSource()
    {
        sourceAnimator =
            null;


        sourceFrontArm =
            null;


        sourceBackArm =
            null;


        sourceHandPoint =
            null;


        sourceOverlay =
            null;


        if (player == null)
            return;


        sourceAnimator =
            player.GetComponentInChildren<
                Animator
            >(
                true
            );


        sourceFrontArm =
            FindDeepChild(
                player.transform,
                "FrontArmPivot"
            );


        sourceBackArm =
            FindDeepChild(
                player.transform,
                "BackArmPivot"
            );


        if (sourceFrontArm != null)
        {
            sourceHandPoint =
                FindDeepChild(
                    sourceFrontArm,
                    "HandPoint"
                );
        }


        sourceOverlay =
            player.GetComponent<
                ArmMiningOverlayController
            >();
    }


    private void ReadOverlay()
    {
        if (sourceFrontArm != null)
        {
            frontArmScenePosition =
                XY(
                    sourceFrontArm.localPosition
                );


            frontPivotPoint =
                frontArmScenePosition;
        }


        if (sourceBackArm != null)
        {
            backArmScenePosition =
                XY(
                    sourceBackArm.localPosition
                );


            backPivotPoint =
                backArmScenePosition;
        }


        if (sourceOverlay == null)
            return;


        SerializedObject so =
            new SerializedObject(
                sourceOverlay
            );


        frontPivotPoint =
            ReadVector2(
                so,
                "frontRotationPointLocal",
                frontPivotPoint
            );


        backPivotPoint =
            ReadVector2(
                so,
                "backRotationPointLocal",
                backPivotPoint
            );


        swingSpeed =
            ReadFloat(
                so,
                "swingSpeed",
                swingSpeed
            );


        frontBackAngle =
            ReadFloat(
                so,
                "frontArmBackAngle",
                frontBackAngle
            );


        frontForwardAngle =
            ReadFloat(
                so,
                "frontArmForwardAngle",
                frontForwardAngle
            );


        backBackAngle =
            ReadFloat(
                so,
                "backArmBackAngle",
                backBackAngle
            );


        backForwardAngle =
            ReadFloat(
                so,
                "backArmForwardAngle",
                backForwardAngle
            );
    }


    private void ReadHandPoint()
    {
        if (sourceHandPoint == null)
            return;


        handLocalPosition =
            XY(
                sourceHandPoint.localPosition
            );
    }


    // =====================================================
    // GUI
    // =====================================================

    private void OnGUI()
    {
        DrawToolbar();


        Rect body =
            new Rect(
                0f,
                22f,
                position.width,
                position.height -
                22f
            );


        float inspectorWidth =
            Mathf.Clamp(
                position.width *
                0.34f,
                360f,
                430f
            );


        Rect previewRect =
            new Rect(
                6f,
                body.y +
                6f,
                body.width -
                inspectorWidth -
                18f,
                body.height -
                12f
            );


        Rect inspectorRect =
            new Rect(
                previewRect.xMax +
                6f,
                body.y +
                6f,
                inspectorWidth,
                body.height -
                12f
            );


        DrawPreview(
            previewRect
        );


        GUILayout.BeginArea(
            inspectorRect
        );


        inspectorScroll =
            EditorGUILayout.BeginScrollView(
                inspectorScroll
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


        GameObject nextPlayer =
            EditorGUILayout.ObjectField(
                player,
                typeof(GameObject),
                true,
                GUILayout.Width(240f)
            )
            as GameObject;


        if (nextPlayer != player)
        {
            SetPlayer(
                nextPlayer
            );
        }


        GUILayout.Space(
            8f
        );


        GUILayout.Label(
            "Item",
            GUILayout.Width(30f)
        );


        itemId =
            GUILayout.TextField(
                itemId,
                GUILayout.Width(175f)
            );


        if (
            GUILayout.Button(
                "Загрузить",
                EditorStyles.toolbarButton,
                GUILayout.Width(70f)
            )
        )
        {
            LoadItem();

            RefreshItemSprite();
        }


        GUILayout.Space(
            12f
        );


        DrawArmButton(
            "FRONT",
            ArmSide.Front
        );


        DrawArmButton(
            "BACK",
            ArmSide.Back
        );


        GUILayout.Space(
            8f
        );


        DrawToolButton(
            "ПОЛОЖЕНИЕ РУКИ",
            ToolMode.ArmPosition,
            118f
        );


        DrawToolButton(
            "ТОЧКА ВРАЩЕНИЯ",
            ToolMode.Pivot,
            120f
        );


        DrawToolButton(
            "ТОЧКА РУКИ",
            ToolMode.HandPoint,
            92f
        );


        DrawToolButton(
            "КУЛАК",
            ToolMode.FistPoint,
            64f
        );


        DrawToolButton(
            "ПРЕДМЕТ",
            ToolMode.Item,
            75f
        );


        DrawToolButton(
            "АТАКА",
            ToolMode.WeaponAim,
            70f
        );


        GUILayout.FlexibleSpace();


        if (
            GUILayout.Button(
                saveQueued
                    ? "..."
                    : "СОХРАНИТЬ",
                EditorStyles.toolbarButton,
                GUILayout.Width(95f)
            )
        )
        {
            QueueSave();
        }


        EditorGUILayout.EndHorizontal();
    }


    private void DrawArmButton(
        string label,
        ArmSide side
    )
    {
        bool selected =
            armSide == side;


        bool next =
            GUILayout.Toggle(
                selected,
                label,
                EditorStyles.toolbarButton,
                GUILayout.Width(58f)
            );


        if (next && !selected)
        {
            armSide =
                side;


            Repaint();
        }
    }


    private void DrawToolButton(
        string label,
        ToolMode mode,
        float width
    )
    {
        bool selected =
            toolMode == mode;


        bool next =
            GUILayout.Toggle(
                selected,
                label,
                EditorStyles.toolbarButton,
                GUILayout.Width(width)
            );


        if (next && !selected)
        {
            toolMode =
                mode;


            Repaint();
        }
    }


    private void DrawInspector()
    {
        DrawArmPositionEditor();


        EditorGUILayout.Space(
            12f
        );


        DrawPivotEditor();


        EditorGUILayout.Space(
            12f
        );


        DrawSwingEditor();


        EditorGUILayout.Space(
            12f
        );


        DrawDefaultItemEditor();


        EditorGUILayout.Space(
            12f
        );


        DrawHandPointEditor();


        EditorGUILayout.Space(
            12f
        );


        DrawWeaponEditor();


        EditorGUILayout.Space(
            12f
        );


        DrawAnimationEditor();


        EditorGUILayout.Space(
            12f
        );


        EditorGUILayout.LabelField(
            "Preview",
            EditorStyles.boldLabel
        );


        snapToPixel =
            EditorGUILayout.Toggle(
                "Snap 1/16",
                snapToPixel
            );


        previewZoom =
            EditorGUILayout.Slider(
                "Zoom",
                previewZoom,
                0.5f,
                3f
            );


        if (
            GUILayout.Button(
                "Центрировать Preview"
            )
        )
        {
            previewPan =
                Vector2.zero;
        }


        EditorGUILayout.Space(
            12f
        );


        if (
            GUILayout.Button(
                "СОХРАНИТЬ ВСЁ",
                GUILayout.Height(36f)
            )
        )
        {
            QueueSave();
        }


        if (
            !string.IsNullOrWhiteSpace(
                status
            )
        )
        {
            EditorGUILayout.HelpBox(
                status,
                status.StartsWith(
                    "Ошибка",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? MessageType.Error
                    : MessageType.Info
            );
        }
    }


    // =====================================================
    // ARM POSITION EDITOR
    // =====================================================

    private void DrawArmPositionEditor()
    {
        EditorGUILayout.LabelField(
            armSide ==
            ArmSide.Front
                ? "Положение руки — передняя"
                : "Положение руки — задняя",
            EditorStyles.boldLabel
        );


        EditorGUILayout.HelpBox(
            "Это реальное localPosition выбранной руки в риге. " +
            "В режиме «ПОЛОЖЕНИЕ РУКИ» ЛКМ двигает саму руку и её текстуру. " +
            "Точка вращения при этом НЕ меняется.",
            MessageType.Info
        );


        Vector2 value =
            GetCurrentArmScenePosition();


        Vector2 next =
            EditorGUILayout.Vector2Field(
                "Arm Local Position",
                value
            );


        if (next != value)
        {
            SetCurrentArmScenePosition(
                Snap(
                    next
                )
            );
        }


        EditorGUILayout.BeginHorizontal();


        if (
            GUILayout.Button(
                "Считать из сцены"
            )
        )
        {
            ReadCurrentArmScenePosition();
        }


        if (
            GUILayout.Button(
                "Pivot к руке"
            )
        )
        {
            SetCurrentPivot(
                GetCurrentArmScenePosition()
            );
        }


        EditorGUILayout.EndHorizontal();
    }


    // =====================================================
    // PIVOT EDITOR
    // =====================================================

    private void DrawPivotEditor()
    {
        EditorGUILayout.LabelField(
            armSide ==
            ArmSide.Front
                ? "Точка вращения — передняя рука"
                : "Точка вращения — задняя рука",
            EditorStyles.boldLabel
        );


        EditorGUILayout.HelpBox(
            "Это отдельная математическая точка. " +
            "Её изменение НЕ меняет Transform руки и НЕ двигает текстуру. " +
            "В режиме «ТОЧКА ВРАЩЕНИЯ» просто тащи жёлтый/оранжевый маркер мышью.",
            MessageType.Info
        );


        Vector2 value =
            GetCurrentPivot();


        Vector2 next =
            EditorGUILayout.Vector2Field(
                "Pivot",
                value
            );


        if (next != value)
        {
            SetCurrentPivot(
                Snap(
                    next
                )
            );
        }


        EditorGUILayout.BeginHorizontal();


        if (
            GUILayout.Button(
                "АВТО К ПЛЕЧУ"
            )
        )
        {
            AutoPivot(
                armSide
            );
        }


        if (
            GUILayout.Button(
                "АВТО ОБЕ РУКИ"
            )
        )
        {
            AutoPivot(
                ArmSide.Front
            );


            AutoPivot(
                ArmSide.Back
            );
        }


        EditorGUILayout.EndHorizontal();
    }


    // =====================================================
    // SWING EDITOR
    // =====================================================

    private void DrawSwingEditor()
    {
        EditorGUILayout.LabelField(
            "Размах при копании",
            EditorStyles.boldLabel
        );


        previewSwing =
            EditorGUILayout.Toggle(
                "Показывать",
                previewSwing
            );


        autoSwing =
            EditorGUILayout.Toggle(
                "Автоматически",
                autoSwing
            );


        if (!autoSwing)
        {
            swingPhase =
                EditorGUILayout.Slider(
                    "Фаза",
                    swingPhase,
                    0f,
                    1f
                );
        }


        swingSpeed =
            EditorGUILayout.Slider(
                "Скорость",
                swingSpeed,
                0.5f,
                12f
            );


        EditorGUILayout.Space(
            4f
        );


        if (
            armSide ==
            ArmSide.Front
        )
        {
            frontBackAngle =
                EditorGUILayout.Slider(
                    "Передняя — назад",
                    frontBackAngle,
                    -180f,
                    180f
                );


            frontForwardAngle =
                EditorGUILayout.Slider(
                    "Передняя — вперёд",
                    frontForwardAngle,
                    -180f,
                    180f
                );
        }
        else
        {
            backBackAngle =
                EditorGUILayout.Slider(
                    "Задняя — назад",
                    backBackAngle,
                    -180f,
                    180f
                );


            backForwardAngle =
                EditorGUILayout.Slider(
                    "Задняя — вперёд",
                    backForwardAngle,
                    -180f,
                    180f
                );
        }


        EditorGUILayout.BeginHorizontal();


        if (
            GUILayout.Button(
                "Показать заднюю точку"
            )
        )
        {
            swingPhase =
                0f;


            autoSwing =
                false;


            previewSwing =
                true;
        }


        if (
            GUILayout.Button(
                "Показать переднюю точку"
            )
        )
        {
            swingPhase =
                0.5f;


            autoSwing =
                false;


            previewSwing =
                true;
        }


        EditorGUILayout.EndHorizontal();
    }


    // =====================================================
    // ITEM EDITOR
    // =====================================================

    private void DrawDefaultItemEditor()
    {
        EditorGUILayout.LabelField(
            "Предмет — положение по умолчанию",
            EditorStyles.boldLabel
        );


        EditorGUILayout.HelpBox(
            "Выбери сверху режим «ПРЕДМЕТ» и тащи розовый маркер прямо в preview. " +
            "Это обычная поза предмета в руке, не анимация атаки.",
            MessageType.None
        );


        Vector2 offset =
            new Vector2(
                heldOffsetX,
                heldOffsetY
            );


        Vector2 nextOffset =
            EditorGUILayout.Vector2Field(
                "Offset",
                offset
            );


        if (nextOffset != offset)
        {
            nextOffset =
                Snap(
                    nextOffset
                );


            heldOffsetX =
                nextOffset.x;


            heldOffsetY =
                nextOffset.y;
        }


        heldRotation =
            EditorGUILayout.Slider(
                "Rotation",
                heldRotation,
                -180f,
                180f
            );


        heldScale =
            EditorGUILayout.Slider(
                "Scale",
                heldScale,
                0.05f,
                4f
            );


        EditorGUILayout.BeginHorizontal();


        if (
            GUILayout.Button(
                "В HandPoint"
            )
        )
        {
            heldOffsetX =
                0f;


            heldOffsetY =
                0f;
        }


        if (
            GUILayout.Button(
                "Сбросить"
            )
        )
        {
            heldOffsetX =
                0f;


            heldOffsetY =
                0f;


            heldRotation =
                0f;


            heldScale =
                1f;
        }


        EditorGUILayout.EndHorizontal();
    }


    private void DrawHandPointEditor()
    {
        EditorGUILayout.LabelField(
            "HandPoint",
            EditorStyles.boldLabel
        );


        EditorGUILayout.HelpBox(
            "ЗЕЛЁНАЯ точка — HandPoint на передней руке. " +
            "Её можно таскать мышью в режиме «ТОЧКА РУКИ». " +
            "Она двигает только сам HandPoint и НЕ двигает текстуру руки. " +
            "Точка вращения руки настраивается отдельно.",
            MessageType.Info
        );


        handLocalPosition =
            EditorGUILayout.Vector2Field(
                "Local Position",
                handLocalPosition
            );
    }


    // =====================================================
    // ANIMATION
    // =====================================================

    private void DrawAnimationEditor()
    {
        EditorGUILayout.LabelField(
            "Базовая анимация",
            EditorStyles.boldLabel
        );


        if (
            clips == null
            ||
            clips.Length == 0
        )
        {
            EditorGUILayout.LabelField(
                "AnimationClip не найден."
            );


            return;
        }


        selectedClip =
            EditorGUILayout.Popup(
                "Clip",
                selectedClip,
                clipNames
            );


        AnimationClip clip =
            GetClip();


        if (clip != null)
        {
            animationTime =
                EditorGUILayout.Slider(
                    "Time",
                    animationTime,
                    0f,
                    Mathf.Max(
                        0.001f,
                        clip.length
                    )
                );
        }
    }


    // =====================================================
    // PREVIEW BUILD
    // =====================================================

    private void QueueRebuildPreview()
    {
        EditorApplication.delayCall +=
            RebuildPreview;
    }


    private void RebuildPreview()
    {
        if (this == null)
            return;


        CleanupPreview();


        if (player == null)
        {
            Repaint();

            return;
        }


        previewPlayer =
            Instantiate(
                player
            );


        previewPlayer.name =
            "HeldItemPosePreview";


        previewPlayer.hideFlags =
            HideFlags.HideAndDontSave;


        previewPlayer.transform.position =
            Vector3.zero;


        previewPlayer.transform.rotation =
            Quaternion.identity;


        previewPlayer.transform.localScale =
            player.transform.localScale;


        StripRuntimeComponents(
            previewPlayer
        );


        previewAnimator =
            previewPlayer.GetComponentInChildren<
                Animator
            >(
                true
            );


        previewFrontArm =
            FindDeepChild(
                previewPlayer.transform,
                "FrontArmPivot"
            );


        previewBackArm =
            FindDeepChild(
                previewPlayer.transform,
                "BackArmPivot"
            );


        if (previewFrontArm != null)
        {
            previewHandPoint =
                FindDeepChild(
                    previewFrontArm,
                    "HandPoint"
                );
        }


        CreatePreviewItem();


        SpriteRenderer[] renderers =
            previewPlayer.GetComponentsInChildren<
                SpriteRenderer
            >(
                true
            );


        for (
            int i = 0;
            i < renderers.Length;
            i++
        )
        {
            if (renderers[i] != null)
            {
                renderers[i].forceRenderingOff =
                    true;
            }
        }


        Repaint();
    }


    private static void StripRuntimeComponents(
        GameObject root
    )
    {
        MonoBehaviour[] scripts =
            root.GetComponentsInChildren<
                MonoBehaviour
            >(
                true
            );


        for (
            int i = scripts.Length - 1;
            i >= 0;
            i--
        )
        {
            if (scripts[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    scripts[i]
                );
            }
        }


        Rigidbody2D[] bodies =
            root.GetComponentsInChildren<
                Rigidbody2D
            >(
                true
            );


        for (
            int i = bodies.Length - 1;
            i >= 0;
            i--
        )
        {
            if (bodies[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    bodies[i]
                );
            }
        }


        Collider2D[] colliders =
            root.GetComponentsInChildren<
                Collider2D
            >(
                true
            );


        for (
            int i = colliders.Length - 1;
            i >= 0;
            i--
        )
        {
            if (colliders[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    colliders[i]
                );
            }
        }
    }


private void CleanupPreview()
{
    if (generatedItemSprite != null)
    {
        UnityEngine.Object.DestroyImmediate(
            generatedItemSprite
        );


        generatedItemSprite =
            null;
    }


    if (generatedProjectileSprite != null)
    {
        UnityEngine.Object.DestroyImmediate(
            generatedProjectileSprite
        );


        generatedProjectileSprite =
            null;
    }


    if (previewPlayer != null)
    {
        UnityEngine.Object.DestroyImmediate(
            previewPlayer
        );
    }


    previewPlayer =
        null;


    previewAnimator =
        null;


    previewFrontArm =
        null;


    previewBackArm =
        null;


    previewHandPoint =
        null;


    previewHeldRenderer =
        null;


    previewProjectileObject =
        null;


    previewProjectileRenderer =
        null;


    previewProjectileItemId =
        null;
}


    // =====================================================
    // BASE POSE + SWING
    // =====================================================

private void PreparePreviewPose()
{
    if (previewPlayer == null)
        return;


    AnimationClip clip =
        GetClip();


    if (clip != null)
    {
        GameObject animationRoot =
            previewAnimator != null
                ? previewAnimator.gameObject
                : previewPlayer;


        clip.SampleAnimation(
            animationRoot,
            Mathf.Clamp(
                animationTime,
                0f,
                clip.length
            )
        );
    }


    SetLocalXY(
        previewFrontArm,
        frontArmScenePosition
    );


    SetLocalXY(
        previewBackArm,
        backArmScenePosition
    );


    if (previewHandPoint != null)
    {
        SetLocalXY(
            previewHandPoint,
            handLocalPosition
        );
    }


    UpdatePreviewItem();


    // Weapon preview is a separate overlay and never accumulates:
    // every frame above was rebuilt from the AnimationClip/base pose.
    if (
        weaponEnabled
        &&
        weaponPreviewEnabled
    )
    {
        ApplyWeaponPreviewPose();

        return;
    }


    if (!previewSwing)
        return;


    float smoothPhase =
        swingPhase
        *
        swingPhase
        *
        (
            3f
            -
            2f
            *
            swingPhase
        );


    ArmMiningOverlayController
        .RotateCurrentPoseAroundLocalPoint(
            previewFrontArm,
            frontPivotPoint,
            Mathf.Lerp(
                frontBackAngle,
                frontForwardAngle,
                smoothPhase
            )
        );


    ArmMiningOverlayController
        .RotateCurrentPoseAroundLocalPoint(
            previewBackArm,
            backPivotPoint,
            Mathf.Lerp(
                backBackAngle,
                backForwardAngle,
                smoothPhase
            )
        );
}



    // =====================================================
    // WEAPON PREVIEW
    // =====================================================

private void ApplyWeaponPreviewPose()
{
    if (
        weapon == null
        ||
        previewFrontArm == null
    )
    {
        SetPreviewProjectileVisible(
            false
        );


        return;
    }


    weapon.Normalize();


    float phase =
        GetWeaponPreviewPhase();


    WeaponKind kind =
        weapon.GetKind();


    Vector2 aim =
        DirectionFromAngle(
            weaponAimAngle
        );


    if (kind == WeaponKind.Sword)
    {
        if (
            swordPreviewAttack ==
            SwordAttackKind.Swing
        )
        {
            float relative =
                Mathf.LerpAngle(
                    weapon.SwordSwingStartAngle,
                    weapon.SwordSwingEndAngle,
                    Smooth01(
                        phase
                    )
                );


            RotatePreviewFrontArmToWorldDirection(
                weaponAimAngle
                +
                relative
                +
                weapon.ArmAimOffset
            );


            if (
                weapon.UseBackArm
                &&
                previewBackArm != null
            )
            {
                RotatePreviewBackArmToWorldDirection(
                    weaponAimAngle
                    +
                    relative
                    +
                    weapon.BackArmAimOffset
                );
            }
        }
        else
        {
            RotatePreviewFrontArmToWorldDirection(
                weaponAimAngle
                +
                weapon.ArmAimOffset
            );


            if (
                weapon.UseBackArm
                &&
                previewBackArm != null
            )
            {
                RotatePreviewBackArmToWorldDirection(
                    weaponAimAngle
                    +
                    weapon.BackArmAimOffset
                );
            }


            ApplyPreviewHeldWorldOffset(
                aim
                *
                (
                    ThrustEnvelope(
                        phase
                    )
                    *
                    weapon.ThrustDistance
                )
            );
        }


        SetPreviewProjectileVisible(
            false
        );


        return;
    }


    if (kind == WeaponKind.Spear)
    {
        RotatePreviewFrontArmToWorldDirection(
            weaponAimAngle
            +
            weapon.ArmAimOffset
        );


        ApplyPreviewHeldWorldOffset(
            aim
            *
            (
                ThrustEnvelope(
                    phase
                )
                *
                weapon.SpearThrustDistance
            )
        );


        SetPreviewProjectileVisible(
            false
        );


        return;
    }


if (kind == WeaponKind.Bow)
        {
            float draw01 =
                Smooth01(
                    phase
                );


            Vector2 bowAim =
                aim.sqrMagnitude >
                0.000001f
                    ? aim.normalized
                    : Vector2.right;


            float aimAngle =
                Mathf.Atan2(
                    bowAim.y,
                    bowAim.x
                )
                *
                Mathf.Rad2Deg;


            RotatePreviewFrontArmToWorldDirection(
                aimAngle +
                weapon.ArmAimOffset
            );


            if (previewBackArm != null)
            {
                RotatePreviewBackArmToWorldDirection(
                    aimAngle +
                    weapon.BackArmAimOffset
                );


                ApplyPreviewBackArmWorldOffset(
                    -bowAim *
                    (
                        weapon.BowDrawDistance *
                        draw01
                    )
                );
            }


            ApplyPreviewHeldWorldOffset(
                bowAim *
                (
                    weapon.BowHeldItemFullDrawOffset *
                    draw01
                )
            );


            UpdatePreviewProjectile(
                bowAim,
                Mathf.Lerp(
                    weapon.BowMinPower,
                    1f,
                    phase
                )
            );


            return;
        }


if (kind == WeaponKind.Gun)
    {
        RotatePreviewFrontArmToWorldDirection(
            weaponAimAngle
            +
            weapon.ArmAimOffset
        );


        if (
            weapon.UseBackArm
            &&
            previewBackArm != null
        )
        {
            RotatePreviewBackArmToWorldDirection(
                weaponAimAngle
                +
                weapon.BackArmAimOffset
            );
        }


        UpdatePreviewProjectile(
            aim,
            1f
        );


        return;
    }


    SetPreviewProjectileVisible(
        false
    );
}


private void SetPreviewFrontArmWorldAngle(
    float degrees
)
{
    RotatePreviewFrontArmToWorldDirection(
        degrees
    );
}


private void SetPreviewBackArmWorldAngle(
    float degrees
)
{
    RotatePreviewBackArmToWorldDirection(
        degrees
    );
}




private Vector2 GetPreviewFrontPivotWorld()
{
    if (
        previewFrontArm == null
        ||
        previewFrontArm.parent == null
    )
    {
        return
            previewFrontArm != null
                ? (Vector2)previewFrontArm.position
                : Vector2.zero;
    }


    return
        previewFrontArm.parent.TransformPoint(
            new Vector3(
                frontPivotPoint.x,
                frontPivotPoint.y,
                previewFrontArm.localPosition.z
            )
        );
}


private Vector2 GetPreviewBackPivotWorld()
{
    if (
        previewBackArm == null
        ||
        previewBackArm.parent == null
    )
    {
        return
            previewBackArm != null
                ? (Vector2)previewBackArm.position
                : Vector2.zero;
    }


    return
        previewBackArm.parent.TransformPoint(
            new Vector3(
                backPivotPoint.x,
                backPivotPoint.y,
                previewBackArm.localPosition.z
            )
        );
}


private void RotatePreviewFrontArmToWorldDirection(
    float desiredWorldAngle
)
{
    if (previewFrontArm == null)
        return;


    Vector2 pivotWorld =
        GetPreviewFrontPivotWorld();


    Vector2 fistWorld =
        GetPreviewFrontFistWorld();


    Vector2 currentDirection =
        fistWorld -
        pivotWorld;


    if (
        currentDirection.sqrMagnitude <
        0.000001f
    )
    {
        currentDirection =
            previewFrontArm.right;
    }


    float currentWorldAngle =
        Mathf.Atan2(
            currentDirection.y,
            currentDirection.x
        )
        *
        Mathf.Rad2Deg;


    float delta =
        Mathf.DeltaAngle(
            currentWorldAngle,
            desiredWorldAngle
        );


    ArmMiningOverlayController
        .RotateCurrentPoseAroundLocalPoint(
            previewFrontArm,
            frontPivotPoint,
            delta
        );
}


private void RotatePreviewBackArmToWorldDirection(
    float desiredWorldAngle
)
{
    if (previewBackArm == null)
        return;


    Vector2 pivotWorld =
        GetPreviewBackPivotWorld();


    Vector2 fistWorld =
        GetPreviewBackFistWorld();


    Vector2 currentDirection =
        fistWorld -
        pivotWorld;


    if (
        currentDirection.sqrMagnitude <
        0.000001f
    )
    {
        currentDirection =
            previewBackArm.right;
    }


    float currentWorldAngle =
        Mathf.Atan2(
            currentDirection.y,
            currentDirection.x
        )
        *
        Mathf.Rad2Deg;


    float delta =
        Mathf.DeltaAngle(
            currentWorldAngle,
            desiredWorldAngle
        );


    ArmMiningOverlayController
        .RotateCurrentPoseAroundLocalPoint(
            previewBackArm,
            backPivotPoint,
            delta
        );
}



private float RotatePreviewBowFrontArmToward(
    Vector2 desiredDirection
)
{
    if (previewFrontArm == null)
        return 0f;


    Vector2 pivotWorld =
        GetPreviewFrontPivotWorld();


    Vector2 referenceWorld =
        previewHandPoint != null
            ? (Vector2)previewHandPoint.position
            : (
                previewHeldRenderer != null
                    ? (Vector2)previewHeldRenderer.transform.position
                    : (Vector2)previewFrontArm.position
            );


    Vector2 currentDirection =
        referenceWorld
        -
        pivotWorld;


    if (
        currentDirection.sqrMagnitude <
        0.000001f
    )
    {
        currentDirection =
            previewFrontArm.right;
    }


    float currentAngle =
        Mathf.Atan2(
            currentDirection.y,
            currentDirection.x
        )
        *
        Mathf.Rad2Deg;


    float desiredAngle =
        Mathf.Atan2(
            desiredDirection.y,
            desiredDirection.x
        )
        *
        Mathf.Rad2Deg;


    float delta =
        Mathf.DeltaAngle(
            currentAngle,
            desiredAngle
        );


    ArmMiningOverlayController
        .RotateCurrentPoseAroundLocalPoint(
            previewFrontArm,
            frontPivotPoint,
            delta
        );


    return delta;
}


private void RotatePreviewBowBackArmByDelta(
    float delta
)
{
    if (previewBackArm == null)
        return;


    ArmMiningOverlayController
        .RotateCurrentPoseAroundLocalPoint(
            previewBackArm,
            backPivotPoint,
            delta
        );
}



private Vector2 GetPreviewFrontFistWorld()
{
    if (previewFrontArm == null)
        return Vector2.zero;


    if (
        weapon != null
        &&
        weapon.FistPointsConfigured
    )
    {
        return
            previewFrontArm.TransformPoint(
                weapon.FrontFistPointLocal
            );
    }


    if (previewHandPoint != null)
        return previewHandPoint.position;


    return
        previewFrontArm.TransformPoint(
            new Vector2(
                0.28f,
                0f
            )
        );
}


private Vector2 GetPreviewBackFistWorld()
{
    if (previewBackArm == null)
        return Vector2.zero;


    Vector2 local =
        weapon != null
        &&
        weapon.FistPointsConfigured
            ? weapon.BackFistPointLocal
            : new Vector2(
                0.28f,
                0f
            );


    return
        previewBackArm.TransformPoint(
            local
        );
}


private void InitializeWeaponFistPointsFromRig()
{
    if (weapon == null)
        return;


    if (
        previewFrontArm != null
        &&
        previewHandPoint != null
    )
    {
        Vector3 local =
            previewFrontArm.InverseTransformPoint(
                previewHandPoint.position
            );


        weapon.FrontFistPointLocal =
            new Vector2(
                local.x,
                local.y
            );
    }
    else
    {
        weapon.FrontFistPointLocal =
            new Vector2(
                0.28f,
                0f
            );
    }


    weapon.BackFistPointLocal =
        weapon.FrontFistPointLocal;


    weapon.FistPointsConfigured =
        true;
}


private void SetFrontFistFromHandPoint()
{
    if (
        weapon == null
        ||
        previewFrontArm == null
        ||
        previewHandPoint == null
    )
    {
        return;
    }


    Vector3 local =
        previewFrontArm.InverseTransformPoint(
            previewHandPoint.position
        );


    weapon.FrontFistPointLocal =
        new Vector2(
            local.x,
            local.y
        );


    weapon.FistPointsConfigured =
        true;
}

    private float GetWeaponPreviewPhase()
    {
        if (!weaponAutoPreview)
        {
            return
                Mathf.Clamp01(
                    weaponPhase
                );
        }


        float speed =
            weaponPreviewSpeed;


        if (
            weapon != null
            &&
            weapon.GetKind() ==
            WeaponKind.Bow
        )
        {
            speed =
                1f
                /
                Mathf.Max(
                    0.01f,
                    weapon.BowChargeTime
                );
        }


        return
            Mathf.PingPong(
                (float)
                EditorApplication.timeSinceStartup
                *
                Mathf.Max(
                    0.01f,
                    speed
                ),
                1f
            );
    }



private void ApplyPreviewBackArmWorldOffset(
    Vector2 worldDelta
)
{
    if (
        previewBackArm == null
        ||
        previewBackArm.parent == null
    )
    {
        return;
    }


    Vector3 localDelta =
        previewBackArm.parent.InverseTransformVector(
            worldDelta
        );


    // PreparePreviewPose rebuilds the base pose before every frame,
    // so this additive translation never accumulates.
    previewBackArm.localPosition +=
        localDelta;
}


    private void ApplyPreviewHeldWorldOffset(
        Vector2 worldDelta
    )
    {
        if (
            previewHeldRenderer == null
            ||
            previewHeldRenderer.transform.parent ==
            null
        )
        {
            return;
        }


        Transform item =
            previewHeldRenderer.transform;


        Vector3 localDelta =
            item.parent.InverseTransformVector(
                worldDelta
            );


        item.localPosition +=
            localDelta;
    }


    private void UpdatePreviewProjectile(
        Vector2 aim,
        float power
    )
    {
        if (
            previewPlayer == null
            ||
            previewHandPoint == null
        )
        {
            SetPreviewProjectileVisible(
                false
            );


            return;
        }


        string projectileId =
            string.IsNullOrWhiteSpace(
                weapon.ProjectileItem
            )
                ? weapon.AmmoItem
                : weapon.ProjectileItem;


        if (
            string.IsNullOrWhiteSpace(
                projectileId
            )
        )
        {
            SetPreviewProjectileVisible(
                false
            );


            return;
        }


        EnsurePreviewProjectile(
            projectileId
        );


        if (
            previewProjectileRenderer == null
            ||
            previewProjectileRenderer.sprite == null
        )
        {
            return;
        }


        float t =
            projectilePreviewTime;


        Vector2 start =
            (Vector2)
            previewHandPoint.position
            +
            aim
            *
            weapon.MuzzleOffset;


        Vector2 velocity =
            aim
            *
            weapon.ProjectileSpeed
            *
            Mathf.Max(
                0.01f,
                power
            );


        Vector2 position =
            start
            +
            velocity
            *
            t
            +
            Vector2.down
            *
            (
                0.5f
                *
                weapon.ProjectileGravity
                *
                t
                *
                t
            );


        Vector2 currentVelocity =
            velocity
            +
            Vector2.down
            *
            (
                weapon.ProjectileGravity
                *
                t
            );


        previewProjectileObject
            .transform
            .position =
            position;


        if (
            weapon.RotateProjectileToVelocity
            &&
            currentVelocity.sqrMagnitude >
            0.0001f
        )
        {
            float angle =
                Mathf.Atan2(
                    currentVelocity.y,
                    currentVelocity.x
                )
                *
                Mathf.Rad2Deg;


            previewProjectileObject
                .transform
                .rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                );
        }


        Sprite sprite =
            previewProjectileRenderer.sprite;


        float longest =
            Mathf.Max(
                sprite.bounds.size.x,
                sprite.bounds.size.y
            );


        if (longest <= 0.0001f)
            longest = 1f;


        float scale =
            weapon.ProjectileWorldSize
            /
            longest
            *
            weapon.ProjectileScale;


        previewProjectileObject
            .transform
            .localScale =
            Vector3.one
            *
            scale;


        previewProjectileObject.SetActive(
            true
        );
    }


    private void EnsurePreviewProjectile(
        string projectileId
    )
    {
        if (previewProjectileObject == null)
        {
            previewProjectileObject =
                new GameObject(
                    "WeaponProjectilePreview"
                );


            previewProjectileObject.hideFlags =
                HideFlags.HideAndDontSave;


            previewProjectileObject
                .transform
                .SetParent(
                    previewPlayer.transform,
                    true
                );


            previewProjectileRenderer =
                previewProjectileObject
                    .AddComponent<
                        SpriteRenderer
                    >();


            previewProjectileRenderer.sortingOrder =
                1100;


            previewProjectileRenderer.forceRenderingOff =
                true;
        }


        if (
            string.Equals(
                previewProjectileItemId,
                projectileId,
                StringComparison.OrdinalIgnoreCase
            )
            &&
            previewProjectileRenderer.sprite != null
        )
        {
            return;
        }


        previewProjectileItemId =
            projectileId;


        if (generatedProjectileSprite != null)
        {
            UnityEngine.Object.DestroyImmediate(
                generatedProjectileSprite
            );


            generatedProjectileSprite =
                null;
        }


        previewProjectileRenderer.sprite =
            LoadSpriteForItem(
                projectileId,
                out generatedProjectileSprite
            );
    }


    private void SetPreviewProjectileVisible(
        bool visible
    )
    {
        if (previewProjectileObject != null)
        {
            previewProjectileObject.SetActive(
                visible
            );
        }
    }


    // =====================================================
    // AUTO PIVOT
    // =====================================================

    private void AutoPivot(
        ArmSide side
    )
    {
        PrepareBasePoseWithoutSwing();


        Transform arm =
            side ==
            ArmSide.Front
                ? previewFrontArm
                : previewBackArm;


        if (
            arm == null
            ||
            arm.parent == null
        )
        {
            return;
        }


        SpriteRenderer armRenderer =
            FindBestArmRenderer(
                arm
            );


        SpriteRenderer bodyRenderer =
            FindBodyRenderer();


        if (
            armRenderer == null
            ||
            bodyRenderer == null
        )
        {
            return;
        }


        Bounds armBounds =
            armRenderer.bounds;


        Vector3 from =
            armBounds.center;


        Vector3 to =
            bodyRenderer.bounds.center;


        Vector3 direction =
            to -
            from;


        if (
            direction.sqrMagnitude <
            0.000001f
        )
        {
            return;
        }


        Vector3 extents =
            armBounds.extents;


        float tx =
            Mathf.Abs(
                direction.x
            )
            >
            0.00001f
                ? Mathf.Abs(
                    extents.x
                    /
                    direction.x
                )
                : float.MaxValue;


        float ty =
            Mathf.Abs(
                direction.y
            )
            >
            0.00001f
                ? Mathf.Abs(
                    extents.y
                    /
                    direction.y
                )
                : float.MaxValue;


        float t =
            Mathf.Min(
                tx,
                ty
            );


        Vector3 edgePoint =
            from
            +
            direction
            *
            t;


        Vector3 local =
            arm.parent.InverseTransformPoint(
                edgePoint
            );


        Vector2 pivot =
            Snap(
                new Vector2(
                    local.x,
                    local.y
                )
            );


        if (
            side ==
            ArmSide.Front
        )
        {
            frontPivotPoint =
                pivot;
        }
        else
        {
            backPivotPoint =
                pivot;
        }


        status =
            side ==
            ArmSide.Front
                ? "Передняя точка поставлена к плечу."
                : "Задняя точка поставлена к плечу.";
    }


    private void PrepareBasePoseWithoutSwing()
    {
        if (previewPlayer == null)
            return;


        AnimationClip clip =
            GetClip();


        if (clip != null)
        {
            GameObject animationRoot =
                previewAnimator != null
                    ? previewAnimator.gameObject
                    : previewPlayer;


            clip.SampleAnimation(
                animationRoot,
                Mathf.Clamp(
                    animationTime,
                    0f,
                    clip.length
                )
            );
        }


        SetLocalXY(
            previewFrontArm,
            frontArmScenePosition
        );


        SetLocalXY(
            previewBackArm,
            backArmScenePosition
        );


        if (previewHandPoint != null)
        {
            SetLocalXY(
                previewHandPoint,
                handLocalPosition
            );
        }


        UpdatePreviewItem();
    }


    private static SpriteRenderer FindBestArmRenderer(
        Transform arm
    )
    {
        if (arm == null)
            return null;


        SpriteRenderer direct =
            arm.GetComponent<
                SpriteRenderer
            >();


        if (
            direct != null
            &&
            direct.sprite != null
        )
        {
            return direct;
        }


        SpriteRenderer[] all =
            arm.GetComponentsInChildren<
                SpriteRenderer
            >(
                true
            );


        SpriteRenderer best =
            null;


        float bestArea =
            0f;


        for (
            int i = 0;
            i < all.Length;
            i++
        )
        {
            if (
                all[i] == null
                ||
                all[i].sprite == null
            )
            {
                continue;
            }


            float area =
                all[i].bounds.size.x
                *
                all[i].bounds.size.y;


            if (area > bestArea)
            {
                bestArea =
                    area;


                best =
                    all[i];
            }
        }


        return best;
    }


    private SpriteRenderer FindBodyRenderer()
    {
        if (previewPlayer == null)
            return null;


        string[] preferred =
        {
            "Body",
            "Torso",
            "Chest"
        };


        for (
            int n = 0;
            n < preferred.Length;
            n++
        )
        {
            Transform t =
                FindDeepChild(
                    previewPlayer.transform,
                    preferred[n]
                );


            if (t == null)
                continue;


            SpriteRenderer renderer =
                t.GetComponent<
                    SpriteRenderer
                >();


            if (
                renderer != null
                &&
                renderer.sprite != null
            )
            {
                return renderer;
            }
        }


        SpriteRenderer[] all =
            previewPlayer.GetComponentsInChildren<
                SpriteRenderer
            >(
                true
            );


        SpriteRenderer best =
            null;


        float bestArea =
            0f;


        for (
            int i = 0;
            i < all.Length;
            i++
        )
        {
            SpriteRenderer renderer =
                all[i];


            if (
                renderer == null
                ||
                renderer.sprite == null
            )
            {
                continue;
            }


            if (
                previewFrontArm != null
                &&
                renderer.transform.IsChildOf(
                    previewFrontArm
                )
            )
            {
                continue;
            }


            if (
                previewBackArm != null
                &&
                renderer.transform.IsChildOf(
                    previewBackArm
                )
            )
            {
                continue;
            }


            float area =
                renderer.bounds.size.x
                *
                renderer.bounds.size.y;


            if (area > bestArea)
            {
                bestArea =
                    area;


                best =
                    renderer;
            }
        }


        return best;
    }


    // =====================================================
    // PREVIEW ITEM
    // =====================================================

    private void CreatePreviewItem()
    {
        if (previewHandPoint == null)
            return;


        GameObject go =
            new GameObject(
                "HeldItem"
            );


        go.hideFlags =
            HideFlags.HideAndDontSave;


        go.transform.SetParent(
            previewHandPoint,
            false
        );


        previewHeldRenderer =
            go.AddComponent<
                SpriteRenderer
            >();


        previewHeldRenderer.sprite =
            LoadItemSprite();


        previewHeldRenderer.sortingOrder =
            1000;


        previewHeldRenderer.forceRenderingOff =
            true;


        UpdatePreviewItem();
    }


    private void RefreshItemSprite()
    {
        if (previewHeldRenderer == null)
            return;


        if (generatedItemSprite != null)
        {
            UnityEngine.Object.DestroyImmediate(
                generatedItemSprite
            );


            generatedItemSprite =
                null;
        }


        previewHeldRenderer.sprite =
            LoadItemSprite();


        Repaint();
    }


    private void UpdatePreviewItem()
    {
        if (previewHeldRenderer == null)
            return;


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


        if (sprite == null)
            return;


        float biggest =
            Mathf.Max(
                0.0001f,
                Mathf.Max(
                    sprite.bounds.size.x,
                    sprite.bounds.size.y
                )
            );


        item.localScale =
            Vector3.one
            *
            (
                0.58f
                /
                biggest
                *
                heldScale
            );
    }


    // =====================================================
    // PREVIEW DRAWING
    // =====================================================

    private void DrawPreview(
        Rect rect
    )
    {
        EditorGUI.DrawRect(
            rect,
            new Color(
                0.045f,
                0.048f,
                0.06f,
                1f
            )
        );


        Rect inner =
            new Rect(
                rect.x +
                10f,
                rect.y +
                42f,
                rect.width -
                20f,
                rect.height -
                70f
            );


        EditorGUI.DrawRect(
            inner,
            new Color(
                0.075f,
                0.078f,
                0.092f,
                1f
            )
        );


        DrawPreviewHeader(
            rect
        );


        if (previewPlayer == null)
        {
            GUI.Label(
                inner,
                "Выбери Player",
                EditorStyles.centeredGreyMiniLabel
            );


            return;
        }


        HandlePanZoom(
            inner
        );


        // CAMERA FRAMING IS BASED ON THE UNSWUNG POSE.
        PrepareBasePoseWithoutSwing();


        List<SpriteRenderer> renderers =
            CollectRenderers();


        Bounds stableBounds =
            CalculateBounds(
                renderers,
                true
            );


        PreparePreviewPose();


        float fit =
            Mathf.Min(
                inner.width
                /
                Mathf.Max(
                    0.25f,
                    stableBounds.size.x
                ),

                inner.height
                /
                Mathf.Max(
                    0.25f,
                    stableBounds.size.y
                )
            );


        float pixelsPerWorld =
            fit
            *
            0.72f
            *
            previewZoom;


        Vector2 center =
            new Vector2(
                stableBounds.center.x,
                stableBounds.center.y
            )
            +
            previewPan;


        renderers.Sort(
            CompareRenderers
        );


        for (
            int i = 0;
            i < renderers.Count;
            i++
        )
        {
            DrawSprite(
                renderers[i],
                inner,
                center,
                pixelsPerWorld
            );
        }


        DrawArmPositionMarkers(
            inner,
            center,
            pixelsPerWorld
        );


        DrawPivotMarkers(
            inner,
            center,
            pixelsPerWorld
        );


        DrawHandPointMarker(
            inner,
            center,
            pixelsPerWorld
        );


        DrawFistMarkers(
            inner,
            center,
            pixelsPerWorld
        );


        DrawItemMarker(
            inner,
            center,
            pixelsPerWorld
        );


        DrawWeaponMarkers(
            inner,
            center,
            pixelsPerWorld
        );


        HandleEditInput(
            inner,
            center,
            pixelsPerWorld
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
            GetToolHint(),
            EditorStyles.whiteMiniLabel
        );
    }


    private void DrawPreviewHeader(
        Rect rect
    )
    {
        Rect area =
            new Rect(
                rect.x +
                10f,
                rect.y +
                9f,
                rect.width -
                20f,
                26f
            );


        GUILayout.BeginArea(
            area
        );


        EditorGUILayout.BeginHorizontal();


        GUILayout.Label(
            armSide ==
            ArmSide.Front
                ? "FRONT ARM"
                : "BACK ARM",
            EditorStyles.boldLabel,
            GUILayout.Width(90f)
        );


        GUILayout.Label(
            GetToolTitle(),
            GUILayout.Width(150f)
        );


        GUILayout.FlexibleSpace();


        if (
            GUILayout.Button(
                "Авто к плечу",
                GUILayout.Width(95f)
            )
        )
        {
            AutoPivot(
                armSide
            );
        }


        if (
            GUILayout.Button(
                "Центр",
                GUILayout.Width(60f)
            )
        )
        {
            previewPan =
                Vector2.zero;
        }


        EditorGUILayout.EndHorizontal();

        GUILayout.EndArea();
    }


    private List<SpriteRenderer> CollectRenderers()
    {
        List<SpriteRenderer> result =
            new List<SpriteRenderer>();


        SpriteRenderer[] all =
            previewPlayer.GetComponentsInChildren<
                SpriteRenderer
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
                all[i] == null
                ||
                !all[i].enabled
                ||
                all[i].sprite == null
                ||
                !all[i].gameObject.activeInHierarchy
            )
            {
                continue;
            }


            result.Add(
                all[i]
            );
        }


        return result;
    }


    private Bounds CalculateBounds(
        List<SpriteRenderer> renderers,
        bool ignoreHeldItem
    )
    {
        bool initialized =
            false;


        Bounds bounds =
            default;


        for (
            int i = 0;
            i < renderers.Count;
            i++
        )
        {
            SpriteRenderer renderer =
                renderers[i];


            if (
                ignoreHeldItem
                &&
                previewHeldRenderer !=
                null
                &&
                renderer ==
                previewHeldRenderer
            )
            {
                continue;
            }


            if (
                ignoreHeldItem
                &&
                previewProjectileRenderer !=
                null
                &&
                renderer ==
                previewProjectileRenderer
            )
            {
                continue;
            }


            EncapsulateSprite(
                renderer,
                ref bounds,
                ref initialized
            );
        }


        if (!initialized)
        {
            bounds =
                new Bounds(
                    Vector3.zero,
                    new Vector3(
                        2f,
                        3f,
                        0f
                    )
                );
        }


        return bounds;
    }


    private static void EncapsulateSprite(
        SpriteRenderer renderer,
        ref Bounds bounds,
        ref bool initialized
    )
    {
        Sprite sprite =
            renderer.sprite;


        if (sprite == null)
            return;


        float ppu =
            Mathf.Max(
                0.0001f,
                sprite.pixelsPerUnit
            );


        float left =
            -sprite.pivot.x
            /
            ppu;


        float right =
            (
                sprite.rect.width
                -
                sprite.pivot.x
            )
            /
            ppu;


        float bottom =
            -sprite.pivot.y
            /
            ppu;


        float top =
            (
                sprite.rect.height
                -
                sprite.pivot.y
            )
            /
            ppu;


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
            int i = 0;
            i < corners.Length;
            i++
        )
        {
            Vector3 world =
                renderer.transform.TransformPoint(
                    corners[i]
                );


            if (!initialized)
            {
                bounds =
                    new Bounds(
                        world,
                        Vector3.zero
                    );


                initialized =
                    true;
            }
            else
            {
                bounds.Encapsulate(
                    world
                );
            }
        }
    }


    private static int CompareRenderers(
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


        if (result != 0)
            return result;


        result =
            a.sortingOrder.CompareTo(
                b.sortingOrder
            );


        if (result != 0)
            return result;


        return
            b.transform.position.z.CompareTo(
                a.transform.position.z
            );
    }


    private static void DrawSprite(
        SpriteRenderer renderer,
        Rect previewRect,
        Vector2 worldCenter,
        float pixelsPerWorld
    )
    {
        Sprite sprite =
            renderer.sprite;


        if (sprite == null)
            return;


        Texture2D texture =
            sprite.texture;


        if (texture == null)
            return;


        float spritePPU =
            Mathf.Max(
                0.0001f,
                sprite.pixelsPerUnit
            );


        Vector3 localCenter =
            new Vector3(
                (
                    sprite.rect.width
                    *
                    0.5f
                    -
                    sprite.pivot.x
                )
                /
                spritePPU,

                (
                    sprite.rect.height
                    *
                    0.5f
                    -
                    sprite.pivot.y
                )
                /
                spritePPU,

                0f
            );


        Vector3 world =
            renderer.transform.TransformPoint(
                localCenter
            );


        Vector3 scale =
            renderer.transform.lossyScale;


        float width =
            sprite.rect.width
            /
            spritePPU
            *
            Mathf.Abs(
                scale.x
            );


        float height =
            sprite.rect.height
            /
            spritePPU
            *
            Mathf.Abs(
                scale.y
            );


        Vector2 screen =
            WorldToScreen(
                world,
                previewRect,
                worldCenter,
                pixelsPerWorld
            );


        Rect drawRect =
            new Rect(
                screen.x
                -
                width
                *
                pixelsPerWorld
                *
                0.5f,

                screen.y
                -
                height
                *
                pixelsPerWorld
                *
                0.5f,

                width
                *
                pixelsPerWorld,

                height
                *
                pixelsPerWorld
            );


        Rect textureRect =
            sprite.textureRect;


        Rect uv =
            new Rect(
                textureRect.x
                /
                texture.width,

                textureRect.y
                /
                texture.height,

                textureRect.width
                /
                texture.width,

                textureRect.height
                /
                texture.height
            );


        bool flipX =
            renderer.flipX
            ^
            (
                scale.x <
                0f
            );


        bool flipY =
            renderer.flipY
            ^
            (
                scale.y <
                0f
            );


        if (flipX)
        {
            uv.x +=
                uv.width;


            uv.width =
                -uv.width;
        }


        if (flipY)
        {
            uv.y +=
                uv.height;


            uv.height =
                -uv.height;
        }


        Matrix4x4 oldMatrix =
            GUI.matrix;


        GUIUtility.RotateAroundPivot(
            -renderer.transform.eulerAngles.z,
            screen
        );


        Color oldColor =
            GUI.color;


        GUI.color =
            renderer.color;


        GUI.DrawTextureWithTexCoords(
            drawRect,
            texture,
            uv,
            true
        );


        GUI.color =
            oldColor;


        GUI.matrix =
            oldMatrix;
    }


    // =====================================================
    // MARKERS
    // =====================================================

    private void DrawArmPositionMarkers(
        Rect rect,
        Vector2 center,
        float ppu
    )
    {
        DrawSingleArmPositionMarker(
            previewFrontArm,
            rect,
            center,
            ppu,
            armSide ==
            ArmSide.Front
                ? new Color(
                    0.1f,
                    0.75f,
                    1f,
                    toolMode ==
                    ToolMode.ArmPosition
                        ? 1f
                        : 0.35f
                )
                : new Color(
                    0.1f,
                    0.75f,
                    1f,
                    0.2f
                )
        );


        DrawSingleArmPositionMarker(
            previewBackArm,
            rect,
            center,
            ppu,
            armSide ==
            ArmSide.Back
                ? new Color(
                    0.1f,
                    1f,
                    0.72f,
                    toolMode ==
                    ToolMode.ArmPosition
                        ? 1f
                        : 0.35f
                )
                : new Color(
                    0.1f,
                    1f,
                    0.72f,
                    0.2f
                )
        );
    }


    private static void DrawSingleArmPositionMarker(
        Transform arm,
        Rect rect,
        Vector2 center,
        float ppu,
        Color color
    )
    {
        if (arm == null)
            return;


        Vector2 screen =
            WorldToScreen(
                arm.position,
                rect,
                center,
                ppu
            );


        float size =
            9f;


        EditorGUI.DrawRect(
            new Rect(
                screen.x -
                size *
                0.5f,
                screen.y -
                size *
                0.5f,
                size,
                size
            ),
            color
        );
    }


    private void DrawPivotMarkers(
        Rect rect,
        Vector2 center,
        float ppu
    )
    {
        DrawSinglePivotMarker(
            previewFrontArm,
            frontPivotPoint,
            rect,
            center,
            ppu,
            armSide ==
            ArmSide.Front
                ? new Color(
                    1f,
                    0.82f,
                    0.05f,
                    1f
                )
                : new Color(
                    1f,
                    0.82f,
                    0.05f,
                    0.35f
                )
        );


        DrawSinglePivotMarker(
            previewBackArm,
            backPivotPoint,
            rect,
            center,
            ppu,
            armSide ==
            ArmSide.Back
                ? new Color(
                    1f,
                    0.35f,
                    0.08f,
                    1f
                )
                : new Color(
                    1f,
                    0.35f,
                    0.08f,
                    0.35f
                )
        );
    }


    private static void DrawSinglePivotMarker(
        Transform arm,
        Vector2 pivot,
        Rect rect,
        Vector2 center,
        float ppu,
        Color color
    )
    {
        if (
            arm == null
            ||
            arm.parent == null
        )
        {
            return;
        }


        Vector3 world =
            arm.parent.TransformPoint(
                new Vector3(
                    pivot.x,
                    pivot.y,
                    arm.localPosition.z
                )
            );


        Vector2 screen =
            WorldToScreen(
                world,
                rect,
                center,
                ppu
            );


        float size =
            15f;


        Rect outer =
            new Rect(
                screen.x
                -
                size
                *
                0.5f,

                screen.y
                -
                size
                *
                0.5f,

                size,
                size
            );


        const float t =
            2f;


        EditorGUI.DrawRect(
            new Rect(
                outer.x,
                outer.y,
                outer.width,
                t
            ),
            color
        );


        EditorGUI.DrawRect(
            new Rect(
                outer.x,
                outer.yMax -
                t,
                outer.width,
                t
            ),
            color
        );


        EditorGUI.DrawRect(
            new Rect(
                outer.x,
                outer.y,
                t,
                outer.height
            ),
            color
        );


        EditorGUI.DrawRect(
            new Rect(
                outer.xMax -
                t,
                outer.y,
                t,
                outer.height
            ),
            color
        );


        EditorGUI.DrawRect(
            new Rect(
                screen.x -
                2f,
                screen.y -
                2f,
                4f,
                4f
            ),
            color
        );
    }


    private void DrawHandPointMarker(
        Rect rect,
        Vector2 center,
        float ppu
    )
    {
        if (
            previewHandPoint == null
        )
        {
            return;
        }


        Vector2 screen =
            WorldToScreen(
                previewHandPoint.position,
                rect,
                center,
                ppu
            );


        Color color =
            toolMode ==
            ToolMode.HandPoint
                ? new Color(
                    0.12f,
                    1f,
                    0.38f,
                    1f
                )
                : new Color(
                    0.12f,
                    1f,
                    0.38f,
                    0.35f
                );


        float size =
            toolMode ==
            ToolMode.HandPoint
                ? 13f
                : 9f;


        float thickness =
            toolMode ==
            ToolMode.HandPoint
                ? 3f
                : 2f;


        EditorGUI.DrawRect(
            new Rect(
                screen.x -
                size *
                0.5f,
                screen.y -
                thickness *
                0.5f,
                size,
                thickness
            ),
            color
        );


        EditorGUI.DrawRect(
            new Rect(
                screen.x -
                thickness *
                0.5f,
                screen.y -
                size *
                0.5f,
                thickness,
                size
            ),
            color
        );


        EditorGUI.DrawRect(
            new Rect(
                screen.x -
                2f,
                screen.y -
                2f,
                4f,
                4f
            ),
            color
        );
    }



private void DrawFistMarkers(
    Rect rect,
    Vector2 center,
    float ppu
)
{
    if (
        !weaponEnabled
        ||
        weapon == null
    )
    {
        return;
    }


    DrawSingleFistMarker(
        previewFrontArm,
        weapon.FrontFistPointLocal,
        rect,
        center,
        ppu,
        armSide == ArmSide.Front
            ? new Color(
                0.1f,
                1f,
                1f,
                toolMode == ToolMode.FistPoint
                    ? 1f
                    : 0.42f
            )
            : new Color(
                0.1f,
                1f,
                1f,
                0.2f
            )
    );


    DrawSingleFistMarker(
        previewBackArm,
        weapon.BackFistPointLocal,
        rect,
        center,
        ppu,
        armSide == ArmSide.Back
            ? new Color(
                1f,
                0.2f,
                0.85f,
                toolMode == ToolMode.FistPoint
                    ? 1f
                    : 0.42f
            )
            : new Color(
                1f,
                0.2f,
                0.85f,
                0.2f
            )
    );
}


private static void DrawSingleFistMarker(
    Transform arm,
    Vector2 fistLocal,
    Rect rect,
    Vector2 center,
    float ppu,
    Color color
)
{
    if (arm == null)
        return;


    Vector2 screen =
        WorldToScreen(
            arm.TransformPoint(
                fistLocal
            ),
            rect,
            center,
            ppu
        );


    const float size =
        12f;


    EditorGUI.DrawRect(
        new Rect(
            screen.x -
            size * 0.5f,
            screen.y -
            1.5f,
            size,
            3f
        ),
        color
    );


    EditorGUI.DrawRect(
        new Rect(
            screen.x -
            1.5f,
            screen.y -
            size * 0.5f,
            3f,
            size
        ),
        color
    );
}

    private void DrawItemMarker(
        Rect rect,
        Vector2 center,
        float ppu
    )
    {
        if (
            previewHeldRenderer == null
            ||
            previewHeldRenderer.sprite ==
            null
        )
        {
            return;
        }


        Vector2 screen =
            WorldToScreen(
                previewHeldRenderer
                    .transform
                    .position,
                rect,
                center,
                ppu
            );


        Color color =
            toolMode ==
            ToolMode.Item
                ? new Color(
                    1f,
                    0.2f,
                    0.9f,
                    1f
                )
                : new Color(
                    1f,
                    0.2f,
                    0.9f,
                    0.35f
                );


        Matrix4x4 old =
            GUI.matrix;


        GUIUtility.RotateAroundPivot(
            45f,
            screen
        );


        EditorGUI.DrawRect(
            new Rect(
                screen.x -
                5f,
                screen.y -
                5f,
                10f,
                10f
            ),
            color
        );


        GUI.matrix =
            old;
    }



    private void DrawWeaponMarkers(
        Rect rect,
        Vector2 center,
        float ppu
    )
    {
        if (
            !weaponEnabled
            ||
            previewFrontArm == null
        )
        {
            return;
        }


        Vector2 shoulder =
            GetPreviewFrontPivotWorld();


        Vector2 cursorWorld =
            shoulder
            +
            DirectionFromAngle(
                weaponAimAngle
            )
            *
            weaponAimDistance;


        Vector2 shoulderScreen =
            WorldToScreen(
                shoulder,
                rect,
                center,
                ppu
            );


        Vector2 cursorScreen =
            WorldToScreen(
                cursorWorld,
                rect,
                center,
                ppu
            );


        Handles.BeginGUI();


        Handles.color =
            new Color(
                1f,
                0.15f,
                0.1f,
                0.75f
            );


        Handles.DrawAAPolyLine(
            2f,
            shoulderScreen,
            cursorScreen
        );


        Handles.EndGUI();


        DrawCross(
            cursorScreen,
            toolMode ==
            ToolMode.WeaponAim
                ? 8f
                : 6f,
            new Color(
                1f,
                0.08f,
                0.05f,
                1f
            )
        );


        if (!showWeaponGuides)
            return;


        WeaponKind kind =
            weapon.GetKind();


        if (
            kind == WeaponKind.Sword
            &&
            swordPreviewAttack ==
            SwordAttackKind.Swing
        )
        {
            const int count =
                30;


            Vector3[] points =
                new Vector3[
                    count
                ];


            for (
                int i = 0;
                i < count;
                i++
            )
            {
                float t =
                    i
                    /
                    (float)
                    (
                        count -
                        1
                    );


                float relative =
                    Mathf.LerpAngle(
                        weapon.SwordSwingStartAngle,
                        weapon.SwordSwingEndAngle,
                        Smooth01(t)
                    );


                Vector2 direction =
                    DirectionFromAngle(
                        weaponAimAngle
                        +
                        relative
                    );


                Vector2 world =
                    shoulder
                    +
                    direction
                    *
                    weapon.SwingReach;


                points[i] =
                    WorldToScreen(
                        world,
                        rect,
                        center,
                        ppu
                    );
            }


            Handles.BeginGUI();


            Handles.color =
                new Color(
                    1f,
                    0.55f,
                    0.12f,
                    0.9f
                );


            Handles.DrawAAPolyLine(
                2.5f,
                points
            );


            Handles.EndGUI();
        }


        if (
            kind == WeaponKind.Bow
            ||
            kind == WeaponKind.Gun
        )
        {
            DrawProjectileTrajectory(
                rect,
                center,
                ppu
            );
        }
    }


    private void DrawProjectileTrajectory(
        Rect rect,
        Vector2 center,
        float ppu
    )
    {
        if (previewHandPoint == null)
            return;


        Vector2 aim =
            DirectionFromAngle(
                weaponAimAngle
            );


        float power =
            weapon.GetKind() ==
            WeaponKind.Bow
                ? Mathf.Lerp(
                    weapon.BowMinPower,
                    1f,
                    GetWeaponPreviewPhase()
                )
                : 1f;


        Vector2 start =
            (Vector2)
            previewHandPoint.position
            +
            aim
            *
            weapon.MuzzleOffset;


        Vector2 velocity =
            aim
            *
            weapon.ProjectileSpeed
            *
            Mathf.Max(
                0.01f,
                power
            );


        const int count =
            32;


        Vector3[] points =
            new Vector3[
                count
            ];


        float totalTime =
            Mathf.Min(
                1.25f,
                weapon.ProjectileLifetime
            );


        for (
            int i = 0;
            i < count;
            i++
        )
        {
            float t =
                totalTime
                *
                i
                /
                (float)
                (
                    count -
                    1
                );


            Vector2 world =
                start
                +
                velocity
                *
                t
                +
                Vector2.down
                *
                (
                    0.5f
                    *
                    weapon.ProjectileGravity
                    *
                    t
                    *
                    t
                );


            points[i] =
                WorldToScreen(
                    world,
                    rect,
                    center,
                    ppu
                );
        }


        Handles.BeginGUI();


        Handles.color =
            new Color(
                0.2f,
                0.8f,
                1f,
                0.8f
            );


        Handles.DrawAAPolyLine(
            2f,
            points
        );


        Handles.EndGUI();
    }


    private static void DrawCross(
        Vector2 center,
        float size,
        Color color
    )
    {
        EditorGUI.DrawRect(
            new Rect(
                center.x -
                size,
                center.y -
                1f,
                size *
                2f,
                2f
            ),
            color
        );


        EditorGUI.DrawRect(
            new Rect(
                center.x -
                1f,
                center.y -
                size,
                2f,
                size *
                2f
            ),
            color
        );
    }


    // =====================================================
    // EDIT INPUT
    // =====================================================

    private void HandleEditInput(
        Rect rect,
        Vector2 center,
        float ppu
    )
    {
        Event e =
            Event.current;


        if (
            e == null
            ||
            e.button !=
            0
            ||
            !rect.Contains(
                e.mousePosition
            )
        )
        {
            return;
        }


        if (
            e.type !=
            EventType.MouseDown
            &&
            e.type !=
            EventType.MouseDrag
        )
        {
            return;
        }


        Vector3 world =
            ScreenToWorld(
                e.mousePosition,
                rect,
                center,
                ppu
            );


        if (
            toolMode ==
            ToolMode.ArmPosition
        )
        {
            SetArmPositionFromWorld(
                armSide,
                world
            );
        }
        else if (
            toolMode ==
            ToolMode.Pivot
        )
        {
            SetPivotFromWorld(
                armSide,
                world
            );
        }
        else if (
            toolMode ==
            ToolMode.HandPoint
        )
        {
            SetHandPointFromWorld(
                world
            );
        }
        else if (
            toolMode ==
            ToolMode.FistPoint
        )
        {
            SetFistPointFromWorld(
                armSide,
                world
            );
        }
        else if (
            toolMode ==
            ToolMode.Item
        )
        {
            SetItemFromWorld(
                world
            );
        }
        else if (
            toolMode ==
            ToolMode.WeaponAim
        )
        {
            SetWeaponAimFromWorld(
                world
            );
        }


        e.Use();

        Repaint();
    }


    private void SetArmPositionFromWorld(
        ArmSide side,
        Vector3 world
    )
    {
        Transform arm =
            side ==
            ArmSide.Front
                ? previewFrontArm
                : previewBackArm;


        if (
            arm == null
            ||
            arm.parent == null
        )
        {
            return;
        }


        Vector3 local =
            arm.parent.InverseTransformPoint(
                world
            );


        Vector2 next =
            Snap(
                new Vector2(
                    local.x,
                    local.y
                )
            );


        // Move ONLY the selected arm transform.
        // The independent rotation point remains exactly where the user put it.
        if (
            side ==
            ArmSide.Front
        )
        {
            frontArmScenePosition =
                next;
        }
        else
        {
            backArmScenePosition =
                next;
        }


        SetLocalXY(
            arm,
            next
        );
    }


    private void SetPivotFromWorld(
        ArmSide side,
        Vector3 world
    )
    {
        Transform arm =
            side ==
            ArmSide.Front
                ? previewFrontArm
                : previewBackArm;


        if (
            arm == null
            ||
            arm.parent == null
        )
        {
            return;
        }


        Vector3 local =
            arm.parent.InverseTransformPoint(
                world
            );


        Vector2 next =
            Snap(
                new Vector2(
                    local.x,
                    local.y
                )
            );


        // THIS IS THE ONLY CHANGE.
        // previewFrontArm/previewBackArm Transform is untouched.
        if (
            side ==
            ArmSide.Front
        )
        {
            frontPivotPoint =
                next;
        }
        else
        {
            backPivotPoint =
                next;
        }
    }


    private void SetHandPointFromWorld(
        Vector3 world
    )
    {
        if (
            previewHandPoint == null
            ||
            previewHandPoint.parent == null
        )
        {
            return;
        }


        Vector3 local =
            previewHandPoint.parent.InverseTransformPoint(
                world
            );


        handLocalPosition =
            Snap(
                new Vector2(
                    local.x,
                    local.y
                )
            );


        // Only HandPoint moves.
        // FrontArmPivot / BackArmPivot and their SpriteRenderers are untouched.
        SetLocalXY(
            previewHandPoint,
            handLocalPosition
        );
    }


    private void SetItemFromWorld(
        Vector3 world
    )
    {
        if (
            previewHeldRenderer == null
            ||
            previewHeldRenderer.transform.parent ==
            null
        )
        {
            return;
        }


        Vector3 local =
            previewHeldRenderer
                .transform
                .parent
                .InverseTransformPoint(
                    world
                );


        Vector2 offset =
            Snap(
                new Vector2(
                    local.x,
                    local.y
                )
            );


        heldOffsetX =
            offset.x;


        heldOffsetY =
            offset.y;
    }




private void SetFistPointFromWorld(
    ArmSide side,
    Vector3 world
)
{
    if (
        !weaponEnabled
        ||
        weapon == null
    )
    {
        return;
    }


    Transform arm =
        side == ArmSide.Front
            ? previewFrontArm
            : previewBackArm;


    if (arm == null)
        return;


    Vector3 local =
        arm.InverseTransformPoint(
            world
        );


    Vector2 point =
        Snap(
            new Vector2(
                local.x,
                local.y
            )
        );


    if (side == ArmSide.Front)
    {
        weapon.FrontFistPointLocal =
            point;
    }
    else
    {
        weapon.BackFistPointLocal =
            point;
    }


    weapon.FistPointsConfigured =
        true;
}

private void SetWeaponAimFromWorld(
    Vector3 world
)
{
    if (previewFrontArm == null)
        return;


    Vector2 shoulder =
        GetPreviewFrontPivotWorld();


    Vector2 delta =
        (Vector2)world
        -
        shoulder;


    if (
        delta.sqrMagnitude <
        0.0001f
    )
    {
        return;
    }


    weaponAimAngle =
        Mathf.Atan2(
            delta.y,
            delta.x
        )
        *
        Mathf.Rad2Deg;


    weaponAimDistance =
        Mathf.Clamp(
            delta.magnitude,
            0.4f,
            8f
        );
}


    // =====================================================
    // PAN / ZOOM
    // =====================================================

    private void HandlePanZoom(
        Rect rect
    )
    {
        Event e =
            Event.current;


        if (
            e == null
            ||
            !rect.Contains(
                e.mousePosition
            )
        )
        {
            return;
        }


        if (
            e.type ==
            EventType.ScrollWheel
        )
        {
            previewZoom =
                Mathf.Clamp(
                    previewZoom
                    *
                    (
                        1f
                        -
                        e.delta.y
                        *
                        0.05f
                    ),
                    0.5f,
                    3f
                );


            e.Use();

            Repaint();
        }


        if (
            e.type ==
            EventType.MouseDrag
            &&
            e.button ==
            2
        )
        {
            // Pan is approximate; it is intentionally independent from editing.
            previewPan.x -=
                e.delta.x
                /
                100f;


            previewPan.y +=
                e.delta.y
                /
                100f;


            e.Use();

            Repaint();
        }
    }


    // =====================================================
    // SAVE
    // =====================================================

    private void QueueSave()
    {
        if (saveQueued)
            return;


        saveQueued =
            true;


        status =
            "Сохранение...";


        EditorApplication.delayCall +=
            SaveDeferred;
    }


    private void SaveDeferred()
    {
        saveQueued =
            false;


        try
        {
            ApplyArmPositions();

            ApplyOverlayToPlayer();

            ApplyHandPoint();

            SaveItemJson();


            if (
                !EditorApplication.isPlaying
                &&
                player !=
                null
                &&
                player.scene.IsValid()
            )
            {
                EditorSceneManager.MarkSceneDirty(
                    player.scene
                );
            }


            status =
                EditorApplication.isPlaying
                    ? "Runtime Player и item pose обновлены."
                    : "Положение рук, точки вращения, размах и item pose сохранены.";
        }
        catch (Exception exception)
        {
            status =
                "Ошибка: "
                +
                exception.Message;


            Debug.LogError(
                "HELD ITEM POSE SAVE FAILED:\n"
                +
                exception
            );
        }


        Repaint();
    }


    private void ApplyArmPositions()
    {
        ApplySingleArmPosition(
            sourceFrontArm,
            frontArmScenePosition,
            "Front Arm Position"
        );


        ApplySingleArmPosition(
            sourceBackArm,
            backArmScenePosition,
            "Back Arm Position"
        );
    }


    private static void ApplySingleArmPosition(
        Transform arm,
        Vector2 localPosition,
        string undoName
    )
    {
        if (arm == null)
            return;


        if (!EditorApplication.isPlaying)
        {
            Undo.RecordObject(
                arm,
                undoName
            );
        }


        SetLocalXY(
            arm,
            localPosition
        );


        if (!EditorApplication.isPlaying)
        {
            EditorUtility.SetDirty(
                arm
            );


            PrefabUtility
                .RecordPrefabInstancePropertyModifications(
                    arm
                );
        }
    }


    private void ApplyOverlayToPlayer()
    {
        if (player == null)
            return;


        if (sourceOverlay == null)
        {
            if (EditorApplication.isPlaying)
            {
                sourceOverlay =
                    player.AddComponent<
                        ArmMiningOverlayController
                    >();
            }
            else
            {
                sourceOverlay =
                    Undo.AddComponent<
                        ArmMiningOverlayController
                    >(
                        player
                    );
            }
        }


        if (sourceOverlay == null)
            return;


        if (!EditorApplication.isPlaying)
        {
            Undo.RecordObject(
                sourceOverlay,
                "Held Item Pose"
            );
        }


        SerializedObject so =
            new SerializedObject(
                sourceOverlay
            );


        WriteVector2(
            so,
            "frontRotationPointLocal",
            frontPivotPoint
        );


        WriteVector2(
            so,
            "backRotationPointLocal",
            backPivotPoint
        );


        WriteBool(
            so,
            "rotationPointsInitialized",
            true
        );


        WriteFloat(
            so,
            "swingSpeed",
            swingSpeed
        );


        WriteFloat(
            so,
            "frontArmBackAngle",
            frontBackAngle
        );


        WriteFloat(
            so,
            "frontArmForwardAngle",
            frontForwardAngle
        );


        WriteFloat(
            so,
            "backArmBackAngle",
            backBackAngle
        );


        WriteFloat(
            so,
            "backArmForwardAngle",
            backForwardAngle
        );


        so.ApplyModifiedProperties();


        if (!EditorApplication.isPlaying)
        {
            EditorUtility.SetDirty(
                sourceOverlay
            );


            PrefabUtility
                .RecordPrefabInstancePropertyModifications(
                    sourceOverlay
                );
        }
    }


    private void ApplyHandPoint()
    {
        if (sourceHandPoint == null)
            return;


        if (!EditorApplication.isPlaying)
        {
            Undo.RecordObject(
                sourceHandPoint,
                "Hand Point"
            );
        }


        SetLocalXY(
            sourceHandPoint,
            handLocalPosition
        );


        if (!EditorApplication.isPlaying)
        {
            EditorUtility.SetDirty(
                sourceHandPoint
            );


            PrefabUtility
                .RecordPrefabInstancePropertyModifications(
                    sourceHandPoint
                );
        }
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
            ||
            !File.Exists(
                itemJsonPath
            )
        )
        {
            status =
                "JSON не найден: "
                +
                itemId;


            return;
        }


        string json =
            File.ReadAllText(
                itemJsonPath
            );


        heldOffsetX =
            ReadJsonFloat(
                json,
                "HeldOffsetX",
                0f
            );


        heldOffsetY =
            ReadJsonFloat(
                json,
                "HeldOffsetY",
                0f
            );


        heldScale =
            ReadJsonFloat(
                json,
                "HeldScale",
                1f
            );


        heldRotation =
            ReadJsonFloat(
                json,
                "HeldRotation",
                0f
            );


        bool repairedPose =
            false;


        if (
            float.IsNaN(heldOffsetX)
            ||
            float.IsInfinity(heldOffsetX)
            ||
            Mathf.Abs(heldOffsetX) >
            5f
        )
        {
            heldOffsetX =
                0f;


            repairedPose =
                true;
        }


        if (
            float.IsNaN(heldOffsetY)
            ||
            float.IsInfinity(heldOffsetY)
            ||
            Mathf.Abs(heldOffsetY) >
            5f
        )
        {
            heldOffsetY =
                0f;


            repairedPose =
                true;
        }


        if (
            float.IsNaN(heldScale)
            ||
            float.IsInfinity(heldScale)
            ||
            heldScale <= 0f
            ||
            heldScale >
            12f
        )
        {
            heldScale =
                1f;


            repairedPose =
                true;
        }


        if (
            float.IsNaN(heldRotation)
            ||
            float.IsInfinity(heldRotation)
        )
        {
            heldRotation =
                0f;


            repairedPose =
                true;
        }


        LoadWeaponFromJson(
            json
        );


        status =
            repairedPose
                ? "Загружен " + itemId + ". Невозможные HeldItem значения прошлой версии сброшены только в редакторе."
                : "Загружен " + itemId;
    }


private void SaveItemJson()
{
    if (
        string.IsNullOrWhiteSpace(
            itemJsonPath
        )
        ||
        !File.Exists(
            itemJsonPath
        )
    )
    {
        throw new FileNotFoundException(
            "Item JSON не найден.",
            itemJsonPath
        );
    }


    string original =
        File.ReadAllText(
            itemJsonPath
        );


    string json =
        RepairCommonJsonCommas(
            original
        );


    if (!IsValidJson(json))
    {
        throw new InvalidDataException(
            "Item JSON повреждён. Файл не перезаписан."
        );
    }


    json =
        SetJsonFloat(
            json,
            "HeldOffsetX",
            heldOffsetX
        );


    json =
        SetJsonFloat(
            json,
            "HeldOffsetY",
            heldOffsetY
        );


    json =
        SetJsonFloat(
            json,
            "HeldScale",
            heldScale
        );


    json =
        SetJsonFloat(
            json,
            "HeldRotation",
            heldRotation
        );


    if (weaponEnabled)
    {
        weapon.Normalize();


        string weaponJson =
            JsonUtility.ToJson(
                weapon,
                true
            );


        json =
            ReplaceOrInsertJsonObject(
                json,
                "Weapon",
                weaponJson
            );
    }


    json =
        RepairCommonJsonCommas(
            json
        );


    if (!IsValidJson(json))
    {
        throw new InvalidDataException(
            "После сохранения получился невалидный JSON. Исходный файл не перезаписан."
        );
    }


    string backup =
        itemJsonPath
        +
        ".before_weapon_editor.bak";


    if (!File.Exists(backup))
    {
        File.WriteAllText(
            backup,
            original,
            new System.Text.UTF8Encoding(
                false
            )
        );
    }


    File.WriteAllText(
        itemJsonPath,
        json,
        new System.Text.UTF8Encoding(
            false
        )
    );


    string assetPath =
        AbsoluteToAssetPath(
            itemJsonPath
        );


    if (
        !string.IsNullOrWhiteSpace(
            assetPath
        )
    )
    {
        AssetDatabase.ImportAsset(
            assetPath,
            ImportAssetOptions.ForceUpdate
        );
    }


    HeldItemPoseRegistry.Reload();


    WeaponMetadataRegistry.Reload();


    ItemSpriteResolver.ClearCache();
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


        if (!Directory.Exists(folder))
            return null;


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
            try
            {
                string json =
                    File.ReadAllText(
                        files[i]
                    );


                string id =
                    ReadJsonString(
                        json,
                        "ID"
                    );


                if (
                    string.Equals(
                        id,
                        targetId,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return
                        files[i];
                }
            }
            catch
            {
            }
        }


        return null;
    }


    private Sprite LoadItemSprite()
    {
        if (
            string.IsNullOrWhiteSpace(
                itemJsonPath
            )
            ||
            !File.Exists(
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


        string textureName =
            ReadJsonString(
                json,
                "Texture"
            );


        if (
            string.IsNullOrWhiteSpace(
                textureName
            )
        )
        {
            return null;
        }


        if (
            textureName.EndsWith(
                ".png",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            textureName =
                textureName.Substring(
                    0,
                    textureName.Length -
                    4
                );
        }


        string assetPath =
            "Assets/GameData/ResourcePacks/Default/textures/items/"
            +
            textureName
            +
            ".png";


        Sprite sprite =
            AssetDatabase.LoadAssetAtPath<
                Sprite
            >(
                assetPath
            );


        if (sprite != null)
            return sprite;


        Texture2D texture =
            AssetDatabase.LoadAssetAtPath<
                Texture2D
            >(
                assetPath
            );


        if (texture == null)
            return null;


        generatedItemSprite =
            Sprite.Create(
                texture,
                new Rect(
                    0f,
                    0f,
                    texture.width,
                    texture.height
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
            "HeldItemPose_"
            +
            itemId;


        return
            generatedItemSprite;
    }



    private void LoadWeaponFromJson(
        string json
    )
    {
        weaponEnabled =
            false;


        weapon =
            new WeaponMetadata();


        weaponPreviewEnabled =
            false;


        weaponAutoPreview =
            false;


        weaponPhase =
            0f;


        string repaired =
            RepairCommonJsonCommas(
                json
            );


        try
        {
            WeaponItemRoot root =
                JsonUtility.FromJson<
                    WeaponItemRoot
                >(
                    repaired
                );


            if (
                root != null
                &&
                root.Weapon != null
            )
            {
                weapon =
                    root.Weapon;


                weapon.Normalize();


                if (
                    Mathf.Abs(
                        weapon.ArmAimOffset
                        +
                        90f
                    )
                    <
                    0.001f
                )
                {
                    weapon.ArmAimOffset =
                        0f;
                }


                if (
                    Mathf.Abs(
                        weapon.BackArmAimOffset
                        +
                        90f
                    )
                    <
                    0.001f
                )
                {
                    weapon.BackArmAimOffset =
                        0f;
                }


                if (!weapon.FistPointsConfigured)
                {
                    InitializeWeaponFistPointsFromRig();
                }


                weaponEnabled =
                    true;
            }
        }
        catch (Exception exception)
        {
            status =
                "Ошибка Weapon JSON: "
                +
                exception.Message;
        }
    }


    private static bool IsValidJson(
        string json
    )
    {
        try
        {
            JsonUtility.FromJson<
                JsonSyntaxProbe
            >(
                json
            );


            return true;
        }
        catch
        {
            return false;
        }
    }


    private static string ReplaceOrInsertJsonObject(
        string json,
        string property,
        string objectJson
    )
    {
        int propertyIndex =
            FindTopLevelJsonProperty(
                json,
                property
            );


        if (propertyIndex >= 0)
        {
            int colon =
                json.IndexOf(
                    ':',
                    propertyIndex
                );


            int objectStart =
                SkipJsonWhitespace(
                    json,
                    colon +
                    1
                );


            if (
                objectStart <
                json.Length
                &&
                json[objectStart] ==
                '{'
            )
            {
                int objectEnd =
                    FindMatchingJsonBrace(
                        json,
                        objectStart
                    );


                if (objectEnd >= 0)
                {
                    return
                        json.Substring(
                            0,
                            objectStart
                        )
                        +
                        objectJson
                        +
                        json.Substring(
                            objectEnd +
                            1
                        );
                }
            }
        }


        int rootEnd =
            json.LastIndexOf(
                '}'
            );


        if (rootEnd < 0)
            return json;


        int previous =
            rootEnd -
            1;


        while (
            previous >= 0
            &&
            char.IsWhiteSpace(
                json[previous]
            )
        )
        {
            previous--;
        }


        bool needComma =
            previous >= 0
            &&
            json[previous] !=
            '{'
            &&
            json[previous] !=
            ',';


        string insertion =
            (
                needComma
                    ? ","
                    : ""
            )
            +
            Environment.NewLine
            +
            "  \""
            +
            property
            +
            "\": "
            +
            objectJson.Replace(
                Environment.NewLine,
                Environment.NewLine +
                "  "
            )
            +
            Environment.NewLine;


        return
            json.Insert(
                rootEnd,
                insertion
            );
    }


    private static int FindTopLevelJsonProperty(
        string json,
        string property
    )
    {
        string token =
            "\""
            +
            property
            +
            "\"";


        int depth =
            0;


        bool inString =
            false;


        bool escaped =
            false;


        for (
            int i = 0;
            i <=
            json.Length -
            token.Length;
            i++
        )
        {
            char c =
                json[i];


            if (inString)
            {
                if (escaped)
                {
                    escaped =
                        false;
                }
                else if (c == '\\')
                {
                    escaped =
                        true;
                }
                else if (c == '"')
                {
                    inString =
                        false;
                }


                continue;
            }


            if (c == '"')
            {
                if (
                    depth == 1
                    &&
                    string.CompareOrdinal(
                        json,
                        i,
                        token,
                        0,
                        token.Length
                    ) ==
                    0
                )
                {
                    return i;
                }


                inString =
                    true;


                continue;
            }


            if (c == '{')
                depth++;
            else if (c == '}')
                depth--;
        }


        return -1;
    }


    private static int SkipJsonWhitespace(
        string json,
        int index
    )
    {
        while (
            index <
            json.Length
            &&
            char.IsWhiteSpace(
                json[index]
            )
        )
        {
            index++;
        }


        return index;
    }


    private static int FindMatchingJsonBrace(
        string json,
        int open
    )
    {
        int depth =
            0;


        bool inString =
            false;


        bool escaped =
            false;


        for (
            int i = open;
            i < json.Length;
            i++
        )
        {
            char c =
                json[i];


            if (inString)
            {
                if (escaped)
                {
                    escaped =
                        false;
                }
                else if (c == '\\')
                {
                    escaped =
                        true;
                }
                else if (c == '"')
                {
                    inString =
                        false;
                }


                continue;
            }


            if (c == '"')
            {
                inString =
                    true;


                continue;
            }


            if (c == '{')
                depth++;
            else if (c == '}')
            {
                depth--;


                if (depth == 0)
                    return i;
            }
        }


        return -1;
    }


    private static string RepairCommonJsonCommas(
        string json
    )
    {
        System.Text.StringBuilder output =
            new System.Text.StringBuilder(
                json.Length
            );


        bool inString =
            false;


        bool escaped =
            false;


        for (
            int i = 0;
            i < json.Length;
            i++
        )
        {
            char c =
                json[i];


            if (inString)
            {
                output.Append(c);


                if (escaped)
                {
                    escaped =
                        false;
                }
                else if (c == '\\')
                {
                    escaped =
                        true;
                }
                else if (c == '"')
                {
                    inString =
                        false;
                }


                continue;
            }


            if (c == '"')
            {
                inString =
                    true;


                output.Append(c);


                continue;
            }


            if (c != ',')
            {
                output.Append(c);


                continue;
            }


            char previous =
                '\0';


            for (
                int p = output.Length - 1;
                p >= 0;
                p--
            )
            {
                if (
                    !char.IsWhiteSpace(
                        output[p]
                    )
                )
                {
                    previous =
                        output[p];


                    break;
                }
            }


            char next =
                '\0';


            for (
                int n = i + 1;
                n < json.Length;
                n++
            )
            {
                if (
                    !char.IsWhiteSpace(
                        json[n]
                    )
                )
                {
                    next =
                        json[n];


                    break;
                }
            }


            bool invalid =
                previous == '{'
                ||
                previous == '['
                ||
                previous == ','
                ||
                next == '}'
                ||
                next == ']'
                ||
                next == ',';


            if (!invalid)
            {
                output.Append(c);
            }
        }


        return
            output.ToString();
    }


    private Sprite LoadSpriteForItem(
        string targetId,
        out Sprite generated
    )
    {
        generated =
            null;


        string path =
            FindItemJson(
                targetId
            );


        if (
            string.IsNullOrWhiteSpace(
                path
            )
            ||
            !File.Exists(
                path
            )
        )
        {
            return null;
        }


        string json =
            File.ReadAllText(
                path
            );


        string textureName =
            ReadJsonString(
                json,
                "Texture"
            );


        if (
            string.IsNullOrWhiteSpace(
                textureName
            )
        )
        {
            return null;
        }


        if (
            textureName.EndsWith(
                ".png",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            textureName =
                textureName.Substring(
                    0,
                    textureName.Length -
                    4
                );
        }


        string assetPath =
            "Assets/GameData/ResourcePacks/Default/textures/items/"
            +
            textureName
            +
            ".png";


        Sprite sprite =
            AssetDatabase.LoadAssetAtPath<
                Sprite
            >(
                assetPath
            );


        if (sprite != null)
            return sprite;


        Texture2D texture =
            AssetDatabase.LoadAssetAtPath<
                Texture2D
            >(
                assetPath
            );


        if (texture == null)
            return null;


        generated =
            Sprite.Create(
                texture,
                new Rect(
                    0f,
                    0f,
                    texture.width,
                    texture.height
                ),
                new Vector2(
                    0.5f,
                    0.5f
                ),
                16f,
                0,
                SpriteMeshType.FullRect
            );


        return generated;
    }


    private static Vector2 DirectionFromAngle(
        float degrees
    )
    {
        float radians =
            degrees
            *
            Mathf.Deg2Rad;


        return
            new Vector2(
                Mathf.Cos(
                    radians
                ),
                Mathf.Sin(
                    radians
                )
            );
    }


    private static float Smooth01(
        float value
    )
    {
        value =
            Mathf.Clamp01(
                value
            );


        return
            value
            *
            value
            *
            (
                3f
                -
                2f
                *
                value
            );
    }


    private static float ThrustEnvelope(
        float value
    )
    {
        float s =
            Mathf.Sin(
                Mathf.Clamp01(
                    value
                )
                *
                Mathf.PI
            );


        return
            s
            *
            s;
    }


    // =====================================================
    // CLIPS
    // =====================================================

private void CollectClips()
{
    List<AnimationClip> result =
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
        AnimationClip[] found =
            sourceAnimator
                .runtimeAnimatorController
                .animationClips;


        for (
            int i = 0;
            i < found.Length;
            i++
        )
        {
            if (
                found[i] !=
                null
                &&
                used.Add(
                    found[i].GetInstanceID()
                )
            )
            {
                result.Add(
                    found[i]
                );
            }
        }
    }


    clips =
        result.ToArray();


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
            clips[i].name;
    }


    selectedClip =
        0;


    for (
        int i = 0;
        i < clips.Length;
        i++
    )
    {
        if (
            clips[i] != null
            &&
            clips[i].name.IndexOf(
                "idle",
                StringComparison.OrdinalIgnoreCase
            )
            >=
            0
        )
        {
            selectedClip =
                i;


            break;
        }
    }


    selectedClip =
        Mathf.Clamp(
            selectedClip,
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


    private AnimationClip GetClip()
    {
        if (
            clips == null
            ||
            selectedClip <
            0
            ||
            selectedClip >=
            clips.Length
        )
        {
            return null;
        }


        return
            clips[
                selectedClip
            ];
    }


    // =====================================================
    // SERIALIZED HELPERS
    // =====================================================

    private static Vector2 ReadVector2(
        SerializedObject so,
        string name,
        Vector2 fallback
    )
    {
        SerializedProperty p =
            so.FindProperty(
                name
            );


        return
            p != null
                ? p.vector2Value
                : fallback;
    }


    private static float ReadFloat(
        SerializedObject so,
        string name,
        float fallback
    )
    {
        SerializedProperty p =
            so.FindProperty(
                name
            );


        return
            p != null
                ? p.floatValue
                : fallback;
    }


    private static void WriteVector2(
        SerializedObject so,
        string name,
        Vector2 value
    )
    {
        SerializedProperty p =
            so.FindProperty(
                name
            );


        if (p != null)
        {
            p.vector2Value =
                value;
        }
    }


    private static void WriteFloat(
        SerializedObject so,
        string name,
        float value
    )
    {
        SerializedProperty p =
            so.FindProperty(
                name
            );


        if (p != null)
        {
            p.floatValue =
                value;
        }
    }


    private static void WriteBool(
        SerializedObject so,
        string name,
        bool value
    )
    {
        SerializedProperty p =
            so.FindProperty(
                name
            );


        if (p != null)
        {
            p.boolValue =
                value;
        }
    }


    // =====================================================
    // JSON HELPERS
    // =====================================================

    private static string ReadJsonString(
        string json,
        string field
    )
    {
        Match match =
            Regex.Match(
                json,
                "\""
                +
                Regex.Escape(
                    field
                )
                +
                "\"\\s*:\\s*\"([^\"]*)\""
            );


        return
            match.Success
                ? match.Groups[1].Value
                : null;
    }


    private static float ReadJsonFloat(
        string json,
        string field,
        float fallback
    )
    {
        Match match =
            Regex.Match(
                json,
                "\""
                +
                Regex.Escape(
                    field
                )
                +
                "\"\\s*:\\s*(-?[0-9]+(?:\\.[0-9]+)?)"
            );


        if (!match.Success)
            return fallback;


        if (
            float.TryParse(
                match.Groups[1].Value,
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


private static string SetJsonFloat(
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
        "\""
        +
        Regex.Escape(
            field
        )
        +
        "\"\\s*:\\s*-?[0-9]+(?:\\.[0-9]+)?";


    Regex regex =
        new Regex(
            pattern
        );


    string replacement =
        "\""
        +
        field
        +
        "\": "
        +
        number;


    if (regex.IsMatch(json))
    {
        return
            regex.Replace(
                json,
                replacement,
                1
            );
    }


    int closing =
        json.LastIndexOf(
            '}'
        );


    if (closing < 0)
        return json;


    int previous =
        closing -
        1;


    while (
        previous >= 0
        &&
        char.IsWhiteSpace(
            json[previous]
        )
    )
    {
        previous--;
    }


    bool comma =
        previous >= 0
        &&
        json[previous] != '{'
        &&
        json[previous] != ',';


    string insertion =
        (
            comma
                ? ","
                : ""
        )
        +
        Environment.NewLine
        +
        "  \""
        +
        field
        +
        "\": "
        +
        number
        +
        Environment.NewLine;


    return
        json.Insert(
            closing,
            insertion
        );
}


    private string GetToolTitle()
    {
        switch (toolMode)
        {
            case ToolMode.ArmPosition:
                return
                    "ПОЛОЖЕНИЕ РУКИ";

            case ToolMode.Pivot:
                return
                    "ТОЧКА ВРАЩЕНИЯ";

            case ToolMode.HandPoint:
                return
                    "ТОЧКА РУКИ";

            case ToolMode.FistPoint:
                return
                    "КУЛАК";

            case ToolMode.Item:
                return
                    "ПРЕДМЕТ";

            case ToolMode.WeaponAim:
                return
                    "АТАКА";

            default:
                return
                    string.Empty;
        }
    }


    private string GetToolHint()
    {
        switch (toolMode)
        {
            case ToolMode.ArmPosition:
                return
                    "ЛКМ: двигает САМУ выбранную руку по сцене. Pivot остаётся на месте.";

            case ToolMode.Pivot:
                return
                    "ЛКМ: двигает ТОЛЬКО точку вращения. Текстура руки не меняется.";

            case ToolMode.HandPoint:
                return
                    "ЛКМ: двигает ЗЕЛЁНЫЙ HandPoint. Текстура руки и pivot не меняются.";

            case ToolMode.FistPoint:
                return
                    "ЛКМ: поставь крестик прямо на видимый кулак выбранной FRONT/BACK руки. " +
                    "Плечо → этот крестик будет направляться точно в курсор.";

            case ToolMode.Item:
                return
                    "ЛКМ: положение предмета по умолчанию. Колесо: zoom. СКМ: pan.";

            case ToolMode.WeaponAim:
                return
                    "ЛКМ: двигать красную точку курсора. Все оружейные атаки направлены относительно неё.";

            default:
                return
                    string.Empty;
        }
    }


    // =====================================================
    // COMMON HELPERS
    // =====================================================

    private Vector2 GetCurrentArmScenePosition()
    {
        return
            armSide ==
            ArmSide.Front
                ? frontArmScenePosition
                : backArmScenePosition;
    }


    private void SetCurrentArmScenePosition(
        Vector2 value
    )
    {
        if (
            armSide ==
            ArmSide.Front
        )
        {
            frontArmScenePosition =
                value;


            SetLocalXY(
                previewFrontArm,
                value
            );
        }
        else
        {
            backArmScenePosition =
                value;


            SetLocalXY(
                previewBackArm,
                value
            );
        }
    }


    private void ReadCurrentArmScenePosition()
    {
        Transform source =
            armSide ==
            ArmSide.Front
                ? sourceFrontArm
                : sourceBackArm;


        if (source == null)
            return;


        SetCurrentArmScenePosition(
            XY(
                source.localPosition
            )
        );
    }


    private Vector2 GetCurrentPivot()
    {
        return
            armSide ==
            ArmSide.Front
                ? frontPivotPoint
                : backPivotPoint;
    }


    private void SetCurrentPivot(
        Vector2 value
    )
    {
        if (
            armSide ==
            ArmSide.Front
        )
        {
            frontPivotPoint =
                value;
        }
        else
        {
            backPivotPoint =
                value;
        }
    }


    private Vector2 Snap(
        Vector2 value
    )
    {
        if (!snapToPixel)
            return value;


        const float step =
            1f /
            16f;


        return
            new Vector2(
                Mathf.Round(
                    value.x
                    /
                    step
                )
                *
                step,

                Mathf.Round(
                    value.y
                    /
                    step
                )
                *
                step
            );
    }


    private static Vector2 XY(
        Vector3 value
    )
    {
        return
            new Vector2(
                value.x,
                value.y
            );
    }


    private static void SetLocalXY(
        Transform target,
        Vector2 value
    )
    {
        if (target == null)
            return;


        Vector3 local =
            target.localPosition;


        local.x =
            value.x;


        local.y =
            value.y;


        target.localPosition =
            local;
    }


    private static Transform FindDeepChild(
        Transform root,
        string targetName
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
                all[i] !=
                null
                &&
                all[i].name ==
                targetName
            )
            {
                return
                    all[i];
            }
        }


        return null;
    }


    private static Vector2 WorldToScreen(
        Vector3 world,
        Rect rect,
        Vector2 center,
        float pixelsPerWorld
    )
    {
        return
            new Vector2(
                rect.center.x
                +
                (
                    world.x
                    -
                    center.x
                )
                *
                pixelsPerWorld,

                rect.center.y
                -
                (
                    world.y
                    -
                    center.y
                )
                *
                pixelsPerWorld
            );
    }


    private static Vector3 ScreenToWorld(
        Vector2 screen,
        Rect rect,
        Vector2 center,
        float pixelsPerWorld
    )
    {
        return
            new Vector3(
                center.x
                +
                (
                    screen.x
                    -
                    rect.center.x
                )
                /
                pixelsPerWorld,

                center.y
                -
                (
                    screen.y
                    -
                    rect.center.y
                )
                /
                pixelsPerWorld,

                0f
            );
    }


    private static string AbsoluteToAssetPath(
        string absolute
    )
    {
        if (
            string.IsNullOrWhiteSpace(
                absolute
            )
        )
        {
            return null;
        }


        string project =
            Directory
                .GetParent(
                    Application.dataPath
                )
                .FullName
                .Replace(
                    '\\',
                    '/'
                );


        string normalized =
            Path
                .GetFullPath(
                    absolute
                )
                .Replace(
                    '\\',
                    '/'
                );


        if (
            !normalized.StartsWith(
                project
                +
                "/",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return null;
        }


        return
            normalized.Substring(
                project.Length
                +
                1
            );
    }
}

#endif

