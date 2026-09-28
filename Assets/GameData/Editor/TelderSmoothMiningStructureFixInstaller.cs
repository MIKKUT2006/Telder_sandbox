#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;

using UnityEditor;
using UnityEngine;


public static class TelderSmoothMiningStructureFixInstaller
{
    [MenuItem("Tools/Game/Apply Smooth Mining + Structure Editor Fix")]
    public static void Apply()
    {
        try
        {
            string armPath = FindScriptPath("ArmMiningOverlayController");
            string structurePath = FindScriptPath("StructureEditorController");

            if (string.IsNullOrWhiteSpace(armPath))
            {
                Debug.LogError(
                    "TELDER FIX: ArmMiningOverlayController.cs not found."
                );
                return;
            }

            if (string.IsNullOrWhiteSpace(structurePath))
            {
                Debug.LogError(
                    "TELDER FIX: StructureEditorController.cs not found."
                );
                return;
            }

            BackupOnce(armPath);
            BackupOnce(structurePath);

            ApplyArmReplacement(armPath);
            ApplyStructureEditorPatch(structurePath);

            AssetDatabase.Refresh();

            Debug.Log(
                "TELDER FIX: applied successfully.\n" +
                "Arm: " + armPath + "\n" +
                "Structure Editor: " + structurePath
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "TELDER FIX FAILED:\n" + exception
            );
        }
    }

    private static void ApplyArmReplacement(string armPath)
    {
        string replacement = ReadPatch("ArmMiningOverlayController.txt");

        File.WriteAllText(
            ToAbsoluteProjectPath(armPath),
            replacement,
            Encoding.UTF8
        );
    }

    private static void ApplyStructureEditorPatch(string assetPath)
    {
        string absolutePath = ToAbsoluteProjectPath(assetPath);
        string source = File.ReadAllText(absolutePath, Encoding.UTF8);

        if (!source.Contains("private GUIStyle editableTextFieldStyle;"))
        {
            const string fieldMarker = "private GUIStyle cellIdStyle;";

            if (!source.Contains(fieldMarker))
            {
                throw new InvalidOperationException(
                    "Structure editor field marker not found: " +
                    fieldMarker
                );
            }

            source = source.Replace(
                fieldMarker,
                "private GUIStyle editableTextFieldStyle;\n\n        " +
                fieldMarker
            );
        }

        source = ReplaceMethod(
            source,
            "private void OnGUI()",
            ReadPatch("Structure_OnGUI.txt")
        );

        source = ReplaceMethod(
            source,
            "private void DrawBlockPalette()",
            ReadPatch("Structure_DrawBlockPalette.txt")
        );

        source = ReplaceMethod(
            source,
            "private string LabeledText(",
            ReadPatch("Structure_LabeledText.txt")
        );

        source = ReplaceMethod(
            source,
            "private int BufferedIntField(",
            ReadPatch("Structure_BufferedIntField.txt")
        );

        source = ReplaceMethod(
            source,
            "private float BufferedFloatField(",
            ReadPatch("Structure_BufferedFloatField.txt")
        );

        string editableStyle =
            ReadPatch("Structure_GetEditableTextFieldStyle.txt");

        if (source.Contains("private GUIStyle GetEditableTextFieldStyle()"))
        {
            source = ReplaceMethod(
                source,
                "private GUIStyle GetEditableTextFieldStyle()",
                editableStyle
            );
        }
        else
        {
            int insertion = source.IndexOf(
                "private string ShortId(",
                StringComparison.Ordinal
            );

            if (insertion < 0)
            {
                throw new InvalidOperationException(
                    "Could not find ShortId() insertion point."
                );
            }

            source = source.Insert(
                insertion,
                editableStyle + "\n\n        "
            );
        }

        File.WriteAllText(
            absolutePath,
            source,
            Encoding.UTF8
        );
    }

    private static string ReplaceMethod(
        string source,
        string signatureStart,
        string replacement
    )
    {
        int signatureIndex = source.IndexOf(
            signatureStart,
            StringComparison.Ordinal
        );

        if (signatureIndex < 0)
        {
            throw new InvalidOperationException(
                "Method not found: " + signatureStart
            );
        }

        int braceStart = source.IndexOf('{', signatureIndex);

        if (braceStart < 0)
        {
            throw new InvalidOperationException(
                "Opening brace not found: " + signatureStart
            );
        }

        int depth = 0;
        bool inString = false;
        bool inChar = false;
        bool escape = false;
        bool lineComment = false;
        bool blockComment = false;

        for (int i = braceStart; i < source.Length; i++)
        {
            char c = source[i];
            char next = i + 1 < source.Length ? source[i + 1] : '\0';

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
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;

                if (depth == 0)
                {
                    int length = i - signatureIndex + 1;

                    return source
                        .Remove(signatureIndex, length)
                        .Insert(signatureIndex, replacement);
                }
            }
        }

        throw new InvalidOperationException(
            "Closing brace not found: " + signatureStart
        );
    }

    private static string ReadPatch(string fileName)
    {
        string path = Path.Combine(
            Application.dataPath,
            "GameData",
            "Editor",
            "TelderFixData",
            fileName
        );

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                "TELDER patch data not found.",
                path
            );
        }

        return File.ReadAllText(path, Encoding.UTF8);
    }

    private static string FindScriptPath(string className)
    {
        string[] guids = AssetDatabase.FindAssets(
            className + " t:MonoScript"
        );

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);

            if (string.Equals(
                    Path.GetFileNameWithoutExtension(path),
                    className,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return path;
            }
        }

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string absolute = ToAbsoluteProjectPath(path);

            if (File.Exists(absolute) &&
                File.ReadAllText(absolute).Contains(
                    "class " + className
                ))
            {
                return path;
            }
        }

        return null;
    }

    private static string ToAbsoluteProjectPath(string assetPath)
    {
        string projectRoot = Directory.GetParent(
            Application.dataPath
        ).FullName;

        return Path.Combine(projectRoot, assetPath);
    }

    private static void BackupOnce(string assetPath)
    {
        string absolute = ToAbsoluteProjectPath(assetPath);
        string backup = absolute + ".telder_backup";

        if (!File.Exists(backup))
            File.Copy(absolute, backup);
    }
}

#endif
