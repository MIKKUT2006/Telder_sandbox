#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;

using UnityEditor;
using UnityEngine;


public static class TelderHeldItemPoseSaveCrashFixInstaller
{
    private const string MenuPath =
        "Tools/Game/Fix Held Item Pose Save Crash";

    private const string Marker =
        "// [TELDER-V39-DEFERRED-SAVE]";


    [MenuItem(MenuPath)]
    public static void Apply()
    {
        try
        {
            string path =
                FindScriptPath(
                    "HeldItemPoseEditorWindow"
                );

            if (string.IsNullOrWhiteSpace(path))
            {
                Debug.LogError(
                    "V39: HeldItemPoseEditorWindow.cs not found."
                );
                return;
            }

            string absolute =
                ToAbsolutePath(path);

            string source =
                File.ReadAllText(
                    absolute,
                    Encoding.UTF8
                );

            if (source.Contains(Marker))
            {
                EditorUtility.DisplayDialog(
                    "Held Item Pose",
                    "Safe Save уже установлен.",
                    "OK"
                );
                return;
            }

            string backup =
                absolute +
                ".save_crash_backup";

            if (!File.Exists(backup))
            {
                File.Copy(
                    absolute,
                    backup,
                    false
                );
            }

            int methodStart =
                source.IndexOf(
                    "private void SaveAll()",
                    StringComparison.Ordinal
                );

            if (methodStart < 0)
            {
                throw new InvalidOperationException(
                    "SaveAll() not found."
                );
            }

            int openBrace =
                source.IndexOf(
                    '{',
                    methodStart
                );

            int closeBrace =
                FindMatchingBrace(
                    source,
                    openBrace
                );

            string replacement =
@"private bool v39SaveQueued;

    private bool v39SaveRunning;

    private string v39SaveStatus =
        string.Empty;


    // [TELDER-V39-DEFERRED-SAVE]
    private void SaveAll()
    {
        if (
            v39SaveQueued
            ||
            v39SaveRunning
        )
        {
            return;
        }


        v39SaveQueued =
            true;


        v39SaveStatus =
            ""Сохранение..."";


        Repaint();


        // Do not modify hierarchy/assets from the current GUILayout event.
        EditorApplication.delayCall +=
            V39PerformDeferredSave;
    }


    private void V39PerformDeferredSave()
    {
        if (this == null)
            return;


        v39SaveQueued =
            false;


        if (v39SaveRunning)
            return;


        v39SaveRunning =
            true;


        try
        {
            playing =
                false;


            // Scene transforms are never persisted from Play Mode.
            if (
                !EditorApplication.isPlaying
                &&
                !EditorApplication.isPlayingOrWillChangePlaymode
            )
            {
                ApplyHandPoint();
            }


            V39SaveItemJsonSafely();


            v39SaveStatus =
                EditorApplication.isPlaying
                    ? ""JSON сохранён. Риг в Play Mode не сохраняется.""
                    : ""HandPoint + Item Pose сохранены."";
        }
        catch (Exception exception)
        {
            v39SaveStatus =
                ""Ошибка: "" +
                exception.Message;


            Debug.LogError(
                ""HELD ITEM POSE SAFE SAVE FAILED:\n"" +
                exception
            );
        }
        finally
        {
            v39SaveRunning =
                false;


            Repaint();
        }
    }


    private void V39SaveItemJsonSafely()
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
                ""JSON предмета не найден."",
                itemJsonPath
            );
        }


        string json =
            File.ReadAllText(
                itemJsonPath
            );


        json =
            SetFloat(
                json,
                ""HeldOffsetX"",
                heldOffsetX
            );


        json =
            SetFloat(
                json,
                ""HeldOffsetY"",
                heldOffsetY
            );


        json =
            SetFloat(
                json,
                ""HeldScale"",
                heldScale
            );


        json =
            SetFloat(
                json,
                ""HeldRotation"",
                heldRotation
            );


        File.WriteAllText(
            itemJsonPath,
            json
        );


        // Import only the changed JSON. Never do a global AssetDatabase.Refresh
        // as part of this save operation.
        string assetPath =
            V39AbsoluteToAssetPath(
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
    }


    private static string V39AbsoluteToAssetPath(
        string absolutePath
    )
    {
        if (
            string.IsNullOrWhiteSpace(
                absolutePath
            )
        )
        {
            return null;
        }


        string projectRoot =
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
                    absolutePath
                )
                .Replace(
                    '\\',
                    '/'
                );


        if (
            !normalized.StartsWith(
                projectRoot + ""/"",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return null;
        }


        return
            normalized.Substring(
                projectRoot.Length +
                1
            );
    }";

            source =
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

            File.WriteAllText(
                absolute,
                source,
                new UTF8Encoding(false)
            );

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Held Item Pose",
                "Safe deferred Save установлен.\n\n" +
                "После компиляции закрой и снова открой окно редактора.",
                "OK"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "V39 INSTALL FAILED:\n" +
                exception
            );

            EditorUtility.DisplayDialog(
                "V39 — ошибка",
                exception.ToString(),
                "OK"
            );
        }
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

        for (int i = openingBrace; i < source.Length; i++)
        {
            char c = source[i];

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
                if (c == '*' && next == '/')
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

            if (c == '/' && next == '/')
            {
                lineComment = true;
                i++;
                continue;
            }

            if (c == '/' && next == '*')
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
                depth++;
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

        for (int i = 0; i < guids.Length; i++)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[i]
                );

            if (
                string.Equals(
                    Path.GetFileNameWithoutExtension(path),
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
