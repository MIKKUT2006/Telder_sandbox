
#if UNITY_EDITOR

using System;
using System.Reflection;

using UnityEditor;
using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.SceneManagement;

using Game.World.Structures.EditorRuntime;

namespace Game.EditorTools
{
    public static class StructureEditorSceneWizard
    {
        [MenuItem(
            "Tools/Game/Create Structure Editor Scene"
        )]
        public static void CreateScene()
        {
            if (!EditorSceneManager
                .SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            string path =
                EditorUtility.SaveFilePanelInProject(
                    "Create Structure Editor Scene",
                    "StructureEditor",
                    "unity",
                    "Выбери путь для сцены редактора структур."
                );

            if (string.IsNullOrWhiteSpace(path))
                return;

            Scene scene =
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single
                );

            GameObject cameraObject =
                new GameObject(
                    "StructureEditorCamera"
                );

            Camera camera =
                cameraObject.AddComponent<Camera>();

            camera.clearFlags =
                CameraClearFlags.SolidColor;

            camera.backgroundColor =
                new Color(
                    0.02f,
                    0.02f,
                    0.025f
                );

            cameraObject.transform.position =
                new Vector3(
                    0f,
                    0f,
                    -10f
                );

            cameraObject.tag =
                "MainCamera";

            GameObject editor =
                new GameObject(
                    "StructureEditor"
                );

            editor.AddComponent<
                StructureEditorContentBootstrap
            >();

            editor.AddComponent<
                StructureEditorController
            >();

            // Do not add the normal GameBootstrap here.
            // StructureEditorContentBootstrap initializes only the
            // content registries required to preview block textures.

            EditorSceneManager.SaveScene(
                scene,
                path
            );

            Selection.activeGameObject =
                editor;

            EditorUtility.DisplayDialog(
                "Structure Editor",
                "Готово.\n\nСоздана отдельная сцена редактора структур.\nЗапусти её в Play Mode.\n\nСлева — блоки.\nЦентр — сетка.\nСправа — spawn rules, biome, height, free-space и loot сундука.\n\nF5 — сохранить JSON.",
                "OK"
            );
        }

        private static void TryAddGameBootstrap()
        {
            Type bootstrapType = null;

            Assembly[] assemblies =
                AppDomain.CurrentDomain
                    .GetAssemblies();

            for (int a = 0;
                 a < assemblies.Length &&
                 bootstrapType == null;
                 a++)
            {
                Type[] types;

                try
                {
                    types =
                        assemblies[a]
                            .GetTypes();
                }
                catch
                {
                    continue;
                }

                for (int i = 0;
                     i < types.Length;
                     i++)
                {
                    if (types[i].Name ==
                            "GameBootstrap" &&
                        typeof(MonoBehaviour)
                            .IsAssignableFrom(
                                types[i]))
                    {
                        bootstrapType =
                            types[i];

                        break;
                    }
                }
            }

            if (bootstrapType == null)
                return;

            GameObject bootstrap =
                new GameObject(
                    "GameBootstrap"
                );

            bootstrap.AddComponent(
                bootstrapType
            );
        }
    }
}

#endif
