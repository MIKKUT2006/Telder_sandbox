
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;


namespace Game.EditorTools
{

    public static class ArmMiningRigSetupWizard
    {

        [MenuItem(
            "Tools/Game/Setup Mining Arm Overlay"
        )]
        public static void Setup()
        {

            GameObject selected =
                Selection.activeGameObject;


            if (
                selected ==
                null
            )
            {

                EditorUtility.DisplayDialog(
                    "Mining Arm Overlay",
                    "Сначала выдели Player в Hierarchy.",
                    "OK"
                );


                return;

            }


            Transform player =
                selected.transform;


            Transform frontArm =
                FindDeepChild(
                    player,
                    "FrontArmPivot"
                );


            Transform backArm =
                FindDeepChild(
                    player,
                    "BackArmPivot"
                );


            if (
                frontArm ==
                null
            )
            {

                EditorUtility.DisplayDialog(
                    "Mining Arm Overlay",
                    "FrontArmPivot не найден внутри выбранного объекта.",
                    "OK"
                );


                return;

            }


            Transform handPoint =
                frontArm.Find(
                    "HandPoint"
                );


            if (
                handPoint ==
                null
            )
            {

                GameObject handObject =
                    new GameObject(
                        "HandPoint"
                    );


                Undo.RegisterCreatedObjectUndo(
                    handObject,
                    "Create HandPoint"
                );


                handPoint =
                    handObject.transform;


                handPoint.SetParent(
                    frontArm,
                    false
                );


                // Neutral starting point. Fine tune it in the
                // Held Item Pose Editor.
                handPoint.localPosition =
                    new Vector3(
                        0.28f,
                        0f,
                        0f
                    );

            }


            ArmMiningOverlayController controller =
                selected.GetComponent<
                    ArmMiningOverlayController
                >();


            if (
                controller ==
                null
            )
            {

                controller =
                    Undo.AddComponent<
                        ArmMiningOverlayController
                    >(
                        selected
                    );

            }


            SerializedObject serialized =
                new SerializedObject(
                    controller
                );


            serialized
                .FindProperty(
                    "frontArmPivot"
                )
                .objectReferenceValue =
                frontArm;


            serialized
                .FindProperty(
                    "backArmPivot"
                )
                .objectReferenceValue =
                backArm;


            serialized
                .FindProperty(
                    "handPoint"
                )
                .objectReferenceValue =
                handPoint;


            serialized.ApplyModifiedProperties();


            EditorUtility.SetDirty(
                controller
            );


            EditorUtility.DisplayDialog(
                "Mining Arm Overlay",
                "Готово.\n\n" +
                "FrontArmPivot найден.\n" +
                (
                    backArm != null
                        ? "BackArmPivot найден.\n"
                        : "BackArmPivot не найден — это допустимо.\n"
                ) +
                "HandPoint создан/найден.\n\n" +
                "Animator на Rig Root остаётся без изменений.\n" +
                "Mining overlay применяется в LateUpdate поверх текущей ходьбы.",
                "OK"
            );

        }


        private static Transform FindDeepChild(
            Transform root,
            string targetName
        )
        {

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

}

#endif
