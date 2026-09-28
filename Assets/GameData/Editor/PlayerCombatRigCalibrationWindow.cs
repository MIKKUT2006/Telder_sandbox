#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using Game.Combat;


public sealed class PlayerCombatRigCalibrationWindow :
    EditorWindow
{
    private enum Side
    {
        Front,
        Back
    }


    private enum EditPoint
    {
        Shoulder,
        Fist
    }


    private GameObject player;


    private PlayerCombatRigCalibration calibration;


    private Transform frontArm;


    private Transform backArm;


    private Transform handPoint;


    private Side side =
        Side.Front;


    private EditPoint editPoint =
        EditPoint.Shoulder;


    [MenuItem(
        "Tools/Game/Combat Rig Calibration"
    )]
    public static void Open()
    {
        PlayerCombatRigCalibrationWindow window =
            GetWindow<
                PlayerCombatRigCalibrationWindow
            >(
                "Combat Rig"
            );


        window.minSize =
            new Vector2(
                420f,
                390f
            );
    }


    private void OnEnable()
    {
        SceneView.duringSceneGui +=
            OnSceneGUI;


        TryUseSelection();
    }


    private void OnDisable()
    {
        SceneView.duringSceneGui -=
            OnSceneGUI;
    }


    private void OnSelectionChange()
    {
        TryUseSelection();

        Repaint();
    }


    private void TryUseSelection()
    {
        GameObject selected =
            Selection.activeGameObject;


        if (selected == null)
            return;


        PlayerController pc =
            selected.GetComponentInParent<
                PlayerController
            >();


        if (pc != null)
        {
            SetPlayer(
                pc.gameObject
            );
        }
    }


    private void SetPlayer(
        GameObject value
    )
    {
        player =
            value;


        Resolve();


        Repaint();

        SceneView.RepaintAll();
    }


    private void Resolve()
    {
        calibration =
            null;


        frontArm =
            null;


        backArm =
            null;


        handPoint =
            null;


        if (player == null)
            return;


        calibration =
            player.GetComponent<
                PlayerCombatRigCalibration
            >();


        frontArm =
            FindDeepChild(
                player.transform,
                "FrontArmPivot"
            );


        backArm =
            FindDeepChild(
                player.transform,
                "BackArmPivot"
            );


        if (frontArm != null)
        {
            handPoint =
                FindDeepChild(
                    frontArm,
                    "HandPoint"
                );
        }
    }


    private void OnGUI()
    {
        EditorGUILayout.LabelField(
            "Настройка рига оружия — ОДИН РАЗ",
            EditorStyles.boldLabel
        );


        EditorGUILayout.Space(
            6f
        );


        GameObject next =
            EditorGUILayout.ObjectField(
                "Player",
                player,
                typeof(GameObject),
                true
            )
            as GameObject;


        if (next != player)
        {
            SetPlayer(
                next
            );
        }


        if (player == null)
        {
            EditorGUILayout.HelpBox(
                "Выбери Player в Hierarchy.",
                MessageType.Info
            );


            return;
        }


        if (calibration == null)
        {
            if (
                GUILayout.Button(
                    "Создать PlayerCombatRigCalibration",
                    GUILayout.Height(32f)
                )
            )
            {
                calibration =
                    Undo.AddComponent<
                        PlayerCombatRigCalibration
                    >(
                        player
                    );


                CaptureCurrent();


                Save();
            }


            return;
        }


        EditorGUILayout.BeginHorizontal();


        side =
            (Side)
            GUILayout.Toolbar(
                (int)side,
                new[]
                {
                    "FRONT",
                    "BACK"
                }
            );


        EditorGUILayout.EndHorizontal();


        editPoint =
            (EditPoint)
            GUILayout.Toolbar(
                (int)editPoint,
                new[]
                {
                    "ПЛЕЧО",
                    "КУЛАК"
                }
            );


        EditorGUILayout.Space(
            8f
        );


        EditorGUILayout.HelpBox(
            editPoint == EditPoint.Shoulder
                ? "В Scene View перетащи маркер точно в место плеча, вокруг которого должна вращаться рука."
                : "В Scene View перетащи маркер точно в центр видимого кулака. Именно линия ПЛЕЧО → КУЛАК будет направляться на цель.",
            MessageType.Info
        );


        EditorGUILayout.Space(
            8f
        );


        DrawValues();


        EditorGUILayout.Space(
            10f
        );


        if (
            GUILayout.Button(
                "АВТО: считать текущий риг",
                GUILayout.Height(30f)
            )
        )
        {
            CaptureCurrent();

            SceneView.RepaintAll();
        }


        if (
            GUILayout.Button(
                "FRONT кулак = HandPoint",
                GUILayout.Height(26f)
            )
        )
        {
            SetFrontFistFromHandPoint();

            SceneView.RepaintAll();
        }


        EditorGUILayout.Space(
            8f
        );


        if (
            GUILayout.Button(
                "СОХРАНИТЬ КАЛИБРОВКУ",
                GUILayout.Height(38f)
            )
        )
        {
            Save();
        }


        EditorGUILayout.Space(
            8f
        );


        EditorGUILayout.HelpBox(
            "После этой настройки плечи, кулаки, базовые позиции рук и HandPoint являются ГЛОБАЛЬНЫМИ для игрока. " +
            "Их больше не нужно настраивать отдельно для каждого меча/лука.",
            MessageType.None
        );
    }


    private void DrawValues()
    {
        Undo.RecordObject(
            calibration,
            "Edit Combat Rig Calibration"
        );


        Vector2 frontBase =
            EditorGUILayout.Vector2Field(
                "Front Base",
                calibration.FrontArmBaseLocalPosition
            );


        if (
            frontBase !=
            calibration.FrontArmBaseLocalPosition
        )
        {
            calibration.SetFrontArmBase(
                frontBase
            );
        }


        Vector2 backBase =
            EditorGUILayout.Vector2Field(
                "Back Base",
                calibration.BackArmBaseLocalPosition
            );


        if (
            backBase !=
            calibration.BackArmBaseLocalPosition
        )
        {
            calibration.SetBackArmBase(
                backBase
            );
        }


        Vector2 hand =
            EditorGUILayout.Vector2Field(
                "HandPoint",
                calibration.HandPointLocalPosition
            );


        if (
            hand !=
            calibration.HandPointLocalPosition
        )
        {
            calibration.SetHandPoint(
                hand
            );
        }


        EditorGUILayout.Space(
            6f
        );


        Vector2 shoulder =
            side == Side.Front
                ? calibration.FrontShoulderParentLocal
                : calibration.BackShoulderParentLocal;


        Vector2 nextShoulder =
            EditorGUILayout.Vector2Field(
                side == Side.Front
                    ? "Front Shoulder"
                    : "Back Shoulder",
                shoulder
            );


        if (nextShoulder != shoulder)
        {
            if (side == Side.Front)
            {
                calibration.SetFrontShoulder(
                    nextShoulder
                );
            }
            else
            {
                calibration.SetBackShoulder(
                    nextShoulder
                );
            }
        }


        Vector2 fist =
            side == Side.Front
                ? calibration.FrontFistArmLocal
                : calibration.BackFistArmLocal;


        Vector2 nextFist =
            EditorGUILayout.Vector2Field(
                side == Side.Front
                    ? "Front Fist"
                    : "Back Fist",
                fist
            );


        if (nextFist != fist)
        {
            if (side == Side.Front)
            {
                calibration.SetFrontFist(
                    nextFist
                );
            }
            else
            {
                calibration.SetBackFist(
                    nextFist
                );
            }
        }


        EditorUtility.SetDirty(
            calibration
        );
    }


    private void CaptureCurrent()
    {
        if (
            calibration == null ||
            frontArm == null ||
            backArm == null
        )
        {
            return;
        }


        Undo.RecordObject(
            calibration,
            "Capture Combat Rig"
        );


        calibration.CaptureCurrentRig(
            frontArm,
            backArm,
            handPoint
        );


        EditorUtility.SetDirty(
            calibration
        );
    }


    private void SetFrontFistFromHandPoint()
    {
        if (
            calibration == null ||
            frontArm == null ||
            handPoint == null
        )
        {
            return;
        }


        Undo.RecordObject(
            calibration,
            "Set Front Fist"
        );


        Vector3 local =
            frontArm.InverseTransformPoint(
                handPoint.position
            );


        calibration.SetFrontFist(
            new Vector2(
                local.x,
                local.y
            )
        );


        EditorUtility.SetDirty(
            calibration
        );
    }


    private void OnSceneGUI(
        SceneView sceneView
    )
    {
        if (
            calibration == null ||
            player == null
        )
        {
            return;
        }


        Transform arm =
            side == Side.Front
                ? frontArm
                : backArm;


        if (
            arm == null ||
            arm.parent == null
        )
        {
            return;
        }


        if (
            editPoint ==
            EditPoint.Shoulder
        )
        {
            Vector2 local =
                side == Side.Front
                    ? calibration.FrontShoulderParentLocal
                    : calibration.BackShoulderParentLocal;


            Vector3 world =
                arm.parent.TransformPoint(
                    new Vector3(
                        local.x,
                        local.y,
                        arm.localPosition.z
                    )
                );


            Handles.Label(
                world +
                Vector3.up *
                0.15f,
                side == Side.Front
                    ? "FRONT SHOULDER"
                    : "BACK SHOULDER"
            );


            EditorGUI.BeginChangeCheck();


            Vector3 nextWorld =
                Handles.PositionHandle(
                    world,
                    Quaternion.identity
                );


            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(
                    calibration,
                    "Move Shoulder"
                );


                Vector3 nextLocal =
                    arm.parent.InverseTransformPoint(
                        nextWorld
                    );


                Vector2 value =
                    new Vector2(
                        nextLocal.x,
                        nextLocal.y
                    );


                if (side == Side.Front)
                {
                    calibration.SetFrontShoulder(
                        value
                    );
                }
                else
                {
                    calibration.SetBackShoulder(
                        value
                    );
                }


                EditorUtility.SetDirty(
                    calibration
                );
            }
        }
        else
        {
            Vector2 local =
                side == Side.Front
                    ? calibration.FrontFistArmLocal
                    : calibration.BackFistArmLocal;


            Vector3 world =
                arm.TransformPoint(
                    new Vector3(
                        local.x,
                        local.y,
                        0f
                    )
                );


            Handles.Label(
                world +
                Vector3.up *
                0.15f,
                side == Side.Front
                    ? "FRONT FIST"
                    : "BACK FIST"
            );


            EditorGUI.BeginChangeCheck();


            Vector3 nextWorld =
                Handles.PositionHandle(
                    world,
                    Quaternion.identity
                );


            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(
                    calibration,
                    "Move Fist"
                );


                Vector3 nextLocal =
                    arm.InverseTransformPoint(
                        nextWorld
                    );


                Vector2 value =
                    new Vector2(
                        nextLocal.x,
                        nextLocal.y
                    );


                if (side == Side.Front)
                {
                    calibration.SetFrontFist(
                        value
                    );
                }
                else
                {
                    calibration.SetBackFist(
                        value
                    );
                }


                EditorUtility.SetDirty(
                    calibration
                );
            }
        }
    }


    private void Save()
    {
        if (calibration == null)
            return;


        EditorUtility.SetDirty(
            calibration
        );


        if (
            calibration.gameObject.scene.IsValid()
        )
        {
            EditorSceneManager.MarkSceneDirty(
                calibration.gameObject.scene
            );
        }


        AssetDatabase.SaveAssets();


        Debug.Log(
            "COMBAT RIG: calibration saved."
        );
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
}

#endif
