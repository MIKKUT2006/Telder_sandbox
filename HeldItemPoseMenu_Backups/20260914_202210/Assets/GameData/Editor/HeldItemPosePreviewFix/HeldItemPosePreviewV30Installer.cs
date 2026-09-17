#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class HeldItemPosePreviewV30Installer
{
    private const string ClassToken =
        "class HeldItemPoseEditorWindow";

    private const string VersionMarker =
        "[HELD-POSE-PREVIEW-V30]";

    private const string TemplateAssetPath =
        "Assets/Editor/HeldItemPosePreviewFix/Templates/HeldItemPoseEditorWindow.cs.txt";


    [MenuItem(
        "Tools/Game/Held Item Pose Editor/Apply Player Preview Fix v30",
        priority = 260
    )]
    public static void Apply()
    {
        string target =
            FindTargetSource();

        if (
            string.IsNullOrEmpty(
                target
            )
        )
        {
            EditorUtility.DisplayDialog(
                "Held Item Pose Preview v30",
                "HeldItemPoseEditorWindow source was not found under Assets.",
                "OK"
            );

            return;
        }


        string oldSource =
            File.ReadAllText(
                target
            );


        if (
            oldSource.Contains(
                VersionMarker
            )
        )
        {
            EditorUtility.DisplayDialog(
                "Held Item Pose Preview v30",
                "Fix is already installed.\n\n" +
                target,
                "OK"
            );

            return;
        }


        string templateFullPath =
            Path.Combine(
                Directory.GetParent(
                    Application.dataPath
                ).FullName,
                TemplateAssetPath
            );


        if (
            !File.Exists(
                templateFullPath
            )
        )
        {
            EditorUtility.DisplayDialog(
                "Held Item Pose Preview v30",
                "Template was not found:\n" +
                templateFullPath,
                "OK"
            );

            return;
        }


        string newSource =
            File.ReadAllText(
                templateFullPath
            );


        if (
            !newSource.Contains(
                VersionMarker
            )
        )
        {
            EditorUtility.DisplayDialog(
                "Held Item Pose Preview v30",
                "Template validation failed.",
                "OK"
            );

            return;
        }


        string projectRoot =
            Directory.GetParent(
                Application.dataPath
            ).FullName;


        string backupRoot =
            Path.Combine(
                projectRoot,
                "HeldItemPosePreview_Backups",
                DateTime.Now.ToString(
                    "yyyyMMdd_HHmmss"
                )
            );


        string normalizedTarget =
            target.Replace(
                '\\',
                '/'
            );


        string normalizedAssets =
            Application.dataPath.Replace(
                '\\',
                '/'
            );


        string relative =
            normalizedTarget.StartsWith(
                normalizedAssets,
                StringComparison.OrdinalIgnoreCase
            )
                ? "Assets" +
                    normalizedTarget.Substring(
                        normalizedAssets.Length
                    )
                : Path.GetFileName(
                    target
                );


        string backupPath =
            Path.Combine(
                backupRoot,
                relative
            );


        Directory.CreateDirectory(
            Path.GetDirectoryName(
                backupPath
            )
        );


        File.Copy(
            target,
            backupPath,
            true
        );


        File.WriteAllText(
            target,
            newSource,
            new UTF8Encoding(
                false
            )
        );


        AssetDatabase.Refresh();


        Debug.Log(
            "HELD ITEM POSE PREVIEW v30 installed.\n" +
            "Target: " +
            target +
            "\nBackup: " +
            backupPath
        );


        EditorUtility.DisplayDialog(
            "Held Item Pose Preview v30",
            "Preview fix installed.\n\n" +
            "Unity will recompile the editor script.\n\n" +
            "Backup:\n" +
            backupPath,
            "OK"
        );
    }


    [MenuItem(
        "Tools/Game/Held Item Pose Editor/Check Player Preview Fix",
        priority = 261
    )]
    public static void Check()
    {
        string target =
            FindTargetSource();


        if (
            string.IsNullOrEmpty(
                target
            )
        )
        {
            EditorUtility.DisplayDialog(
                "Held Item Pose Preview",
                "HeldItemPoseEditorWindow source not found.",
                "OK"
            );

            return;
        }


        string source =
            File.ReadAllText(
                target
            );


        bool installed =
            source.Contains(
                VersionMarker
            );


        EditorUtility.DisplayDialog(
            "Held Item Pose Preview",
            installed
                ? "v30 is installed.\n\n" + target
                : "v30 is NOT installed.\n\n" + target,
            "OK"
        );
    }


    private static string FindTargetSource()
    {
        string[] files =
            Directory.GetFiles(
                Application.dataPath,
                "*.cs",
                SearchOption.AllDirectories
            );


        string best =
            null;

        int bestScore =
            int.MinValue;


        for (
            int i = 0;
            i < files.Length;
            i++
        )
        {
            string path =
                files[i];


            string normalized =
                path.Replace(
                    '\\',
                    '/'
                );


            if (
                normalized.Contains(
                    "/Assets/Editor/HeldItemPosePreviewFix/"
                )
            )
            {
                continue;
            }


            string source;

            try
            {
                source =
                    File.ReadAllText(
                        path
                    );
            }
            catch
            {
                continue;
            }


            if (
                !source.Contains(
                    ClassToken
                )
            )
            {
                continue;
            }


            int score =
                0;


            string fileName =
                Path.GetFileNameWithoutExtension(
                    path
                );


            if (
                string.Equals(
                    fileName,
                    "HeldItemPoseEditorWindow",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                score +=
                    100;
            }


            if (
                source.Contains(
                    "Tools/Game/Held Item Pose Editor"
                )
            )
            {
                score +=
                    40;
            }


            if (
                source.Contains(
                    "DrawAnimatedPreview"
                )
            )
            {
                score +=
                    20;
            }


            if (
                source.Contains(
                    "FrontArmPivot"
                )
            )
            {
                score +=
                    20;
            }


            if (
                score >
                bestScore
            )
            {
                bestScore =
                    score;

                best =
                    path;
            }
        }


        return best;
    }
}

#endif
