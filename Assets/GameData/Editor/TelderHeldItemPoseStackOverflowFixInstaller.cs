#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;

using UnityEditor;
using UnityEngine;


public static class TelderHeldItemPoseStackOverflowFixInstaller
{
    private const string MenuPath =
        "Tools/Game/Fix Held Item Pose Stack Overflow";


    [MenuItem(MenuPath)]
    public static void Apply()
    {
        try
        {
            string path =
                FindScriptPath(
                    "HeldItemPoseEditorWindow"
                );


            if (
                string.IsNullOrWhiteSpace(
                    path
                )
            )
            {
                Debug.LogError(
                    "STACK OVERFLOW FIX: HeldItemPoseEditorWindow.cs not found."
                );

                return;
            }


            string absolute =
                ToAbsolutePath(
                    path
                );


            string source =
                File.ReadAllText(
                    absolute,
                    Encoding.UTF8
                );


            string backup =
                absolute +
                ".stack_overflow_backup";


            if (
                !File.Exists(
                    backup
                )
            )
            {
                File.Copy(
                    absolute,
                    backup,
                    false
                );
            }


            source =
                ReplaceV38Helper(
                    source
                );


            File.WriteAllText(
                absolute,
                source,
                new UTF8Encoding(
                    false
                )
            );


            AssetDatabase.Refresh();


            Debug.Log(
                "HELD ITEM POSE: V38 recursive helper fixed."
            );


            EditorUtility.DisplayDialog(
                "Held Item Pose",
                "StackOverflow исправлен.\n\n" +
                "V38MarkSceneDirtySafe больше не вызывает сам себя.\n" +
                "После компиляции закрой и снова открой Held Item Pose Editor.",
                "OK"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "STACK OVERFLOW FIX FAILED:\n" +
                exception
            );


            EditorUtility.DisplayDialog(
                "Held Item Pose — ошибка",
                exception.ToString(),
                "OK"
            );
        }
    }


    private static string ReplaceV38Helper(
        string source
    )
    {
        int methodStart =
            source.IndexOf(
                "private static void V38MarkSceneDirtySafe(",
                StringComparison.Ordinal
            );


        if (methodStart < 0)
        {
            // Some intermediate patch may have omitted "static".
            methodStart =
                source.IndexOf(
                    "private void V38MarkSceneDirtySafe(",
                    StringComparison.Ordinal
                );
        }


        if (methodStart < 0)
        {
            throw new InvalidOperationException(
                "V38MarkSceneDirtySafe() not found in HeldItemPoseEditorWindow.cs"
            );
        }


        int openBrace =
            source.IndexOf(
                '{',
                methodStart
            );


        if (openBrace < 0)
        {
            throw new InvalidOperationException(
                "V38MarkSceneDirtySafe opening brace not found."
            );
        }


        int closeBrace =
            FindMatchingBrace(
                source,
                openBrace
            );


        string replacement =
@"private static void V38MarkSceneDirtySafe(
        UnityEngine.SceneManagement.Scene scene
    )
    {
        HeldItemPoseSceneDirtyBridge.Mark(
            scene
        );
    }";


        return
            source.Remove(
                methodStart,
                closeBrace -
                methodStart +
                1
            )
            .Insert(
                methodStart,
                replacement
            );
    }


    private static int FindMatchingBrace(
        string source,
        int openingBrace
    )
    {
        int depth = 0;

        bool inString = false;
        bool inChar = false;
        bool escape = false;
        bool lineComment = false;
        bool blockComment = false;


        for (
            int i = openingBrace;
            i < source.Length;
            i++
        )
        {
            char c =
                source[i];


            char next =
                i + 1 < source.Length
                    ? source[i + 1]
                    : '\0';


            if (lineComment)
            {
                if (c == '\n')
                    lineComment = false;

                continue;
            }


            if (blockComment)
            {
                if (
                    c == '*'
                    &&
                    next == '/'
                )
                {
                    blockComment = false;
                    i++;
                }

                continue;
            }


            if (inString)
            {
                if (escape)
                {
                    escape = false;
                    continue;
                }


                if (c == '\\')
                {
                    escape = true;
                    continue;
                }


                if (c == '"')
                    inString = false;

                continue;
            }


            if (inChar)
            {
                if (escape)
                {
                    escape = false;
                    continue;
                }


                if (c == '\\')
                {
                    escape = true;
                    continue;
                }


                if (c == '\'')
                    inChar = false;

                continue;
            }


            if (
                c == '/'
                &&
                next == '/'
            )
            {
                lineComment = true;
                i++;
                continue;
            }


            if (
                c == '/'
                &&
                next == '*'
            )
            {
                blockComment = true;
                i++;
                continue;
            }


            if (c == '"')
            {
                inString = true;
                continue;
            }


            if (c == '\'')
            {
                inChar = true;
                continue;
            }


            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;

                if (depth == 0)
                    return i;
            }
        }


        throw new InvalidOperationException(
            "Matching brace not found."
        );
    }


    private static string FindScriptPath(
        string className
    )
    {
        string[] guids =
            AssetDatabase.FindAssets(
                className +
                " t:MonoScript"
            );


        for (
            int i = 0;
            i < guids.Length;
            i++
        )
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[i]
                );


            if (
                string.Equals(
                    Path.GetFileNameWithoutExtension(
                        path
                    ),
                    className,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return path;
            }
        }


        return null;
    }


    private static string ToAbsolutePath(
        string assetPath
    )
    {
        string projectRoot =
            Directory
                .GetParent(
                    Application.dataPath
                )
                .FullName;


        return
            Path.Combine(
                projectRoot,
                assetPath
            );
    }
}

#endif
