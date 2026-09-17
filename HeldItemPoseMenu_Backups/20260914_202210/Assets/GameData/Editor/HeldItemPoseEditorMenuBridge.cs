#if UNITY_EDITOR

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class HeldItemPoseEditorMenuBridge
{
    [MenuItem(
        "Tools/Game/Held Item Pose Editor",
        priority = 200
    )]
    public static void Open()
    {
        Type windowType =
            FindType(
                "HeldItemPoseEditorWindow"
            );

        if (
            windowType == null
        )
        {
            EditorUtility.DisplayDialog(
                "Held Item Pose Editor",
                "Класс HeldItemPoseEditorWindow не найден.\n\n" +
                "Проверь, что файл находится здесь:\n" +
                "Assets/GameData/Editor/HeldItemPoseEditorWindow.cs\n\n" +
                "И что в Console нет ошибок компиляции.",
                "OK"
            );

            return;
        }

        if (
            !typeof(EditorWindow)
                .IsAssignableFrom(
                    windowType
                )
        )
        {
            EditorUtility.DisplayDialog(
                "Held Item Pose Editor",
                "HeldItemPoseEditorWindow найден, но он не наследуется от EditorWindow.",
                "OK"
            );

            return;
        }

        EditorWindow window =
            EditorWindow.GetWindow(
                windowType,
                false,
                "Held Item Pose"
            );

        if (
            window != null
        )
        {
            window.minSize =
                new Vector2(
                    900f,
                    600f
                );

            window.Show();
            window.Focus();
        }
    }


    [MenuItem(
        "Tools/Game/Held Item Pose Editor",
        true
    )]
    private static bool ValidateOpen()
    {
        /*
         * Always keep the menu visible.
         *
         * If the actual window class is missing, Open()
         * will show a useful diagnostic instead of hiding
         * the menu completely.
         */
        return true;
    }


    private static Type FindType(
        string typeName
    )
    {
        Assembly[] assemblies =
            AppDomain.CurrentDomain.GetAssemblies();

        for (
            int i = 0;
            i < assemblies.Length;
            i++
        )
        {
            Type[] types;

            try
            {
                types =
                    assemblies[i].GetTypes();
            }
            catch (
                ReflectionTypeLoadException exception
            )
            {
                types =
                    exception.Types;
            }
            catch
            {
                continue;
            }

            if (types == null)
            {
                continue;
            }

            for (
                int j = 0;
                j < types.Length;
                j++
            )
            {
                Type type =
                    types[j];

                if (
                    type != null
                    &&
                    type.Name ==
                        typeName
                )
                {
                    return type;
                }
            }
        }

        return null;
    }
}

#endif
