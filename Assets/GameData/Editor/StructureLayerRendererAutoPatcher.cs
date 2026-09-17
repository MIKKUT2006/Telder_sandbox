#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;


[InitializeOnLoad]
public static class StructureLayerRendererAutoPatcher
{
    private const string BackgroundMarker =
        "// [STRUCTURE-LAYERS-BACKGROUND-TRANSFORM-V1]";


    private const string WorldManagerMarker =
        "// [STRUCTURE-LAYERS-CLEAR-BACKGROUND-TRANSFORM-V1]";


    static StructureLayerRendererAutoPatcher()
    {
        EditorApplication.delayCall +=
            ApplyAutomatically;
    }


    [MenuItem(
        "Tools/Game/Structure Editor/Apply Layer Runtime Patch",
        priority = 245
    )]
    public static void ApplyFromMenu()
    {
        Apply(
            true
        );
    }


    private static void ApplyAutomatically()
    {
        if (
            EditorApplication.isPlayingOrWillChangePlaymode
        )
        {
            return;
        }


        Apply(
            false
        );
    }


    private static void Apply(
        bool showDialog
    )
    {
        bool changed =
            false;


        changed |=
            PatchChunkRenderer();


        changed |=
            PatchWorldManager();


        if (changed)
        {
            AssetDatabase.Refresh();
        }


        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Structure Layers",
                changed
                    ? "Runtime patch применён. Дождись перекомпиляции."
                    : "Runtime patch уже установлен.",
                "OK"
            );
        }
    }


    private static bool PatchChunkRenderer()
    {
        string path =
            FindClassFile(
                "ChunkRenderer"
            );


        if (
            string.IsNullOrEmpty(
                path
            )
        )
        {
            return false;
        }


        string source =
            File.ReadAllText(
                path
            );


        if (
            source.Contains(
                BackgroundMarker
            )
        )
        {
            return false;
        }


        Regex draw =
            new Regex(
                @"BlockRenderer\s*\.\s*DrawBlock\s*\(\s*" +
                @"(?<texture>[A-Za-z_][A-Za-z0-9_]*\.BackgroundTexture)\s*,\s*" +
                @"(?<x>[A-Za-z_][A-Za-z0-9_]*)\s*,\s*" +
                @"(?<y>[A-Za-z_][A-Za-z0-9_]*)\s*,\s*" +
                @"chunk\s*\.\s*GetBackground\s*\(\s*\k<x>\s*,\s*\k<y>\s*\)\s*\)\s*;",
                RegexOptions.Multiline
            );


        MatchCollection matches =
            draw.Matches(
                source
            );


        if (
            matches.Count ==
            0
        )
        {
            return false;
        }


        StringBuilder builder =
            new StringBuilder();


        int cursor =
            0;


        for (
            int i = 0;
            i < matches.Count;
            i++
        )
        {
            Match match =
                matches[i];


            builder.Append(
                source,
                cursor,
                match.Index -
                cursor
            );


            builder.Append(
                match.Value
            );


            string indent =
                GetIndent(
                    source,
                    match.Index
                );


            string texture =
                match.Groups[
                    "texture"
                ].Value;


            string x =
                match.Groups[
                    "x"
                ].Value;


            string y =
                match.Groups[
                    "y"
                ].Value;


            builder.AppendLine();

            builder.Append(
                indent
            );

            builder.AppendLine(
                BackgroundMarker
            );

            builder.Append(
                indent
            );

            builder.AppendLine(
                "Game.BlockTransforms.BackgroundBlockTransformRegistry.ApplyToCell("
            );

            builder.Append(
                indent
            );

            builder.AppendLine(
                "    " +
                texture +
                ","
            );

            builder.Append(
                indent
            );

            builder.AppendLine(
                "    " +
                x +
                ","
            );

            builder.Append(
                indent
            );

            builder.AppendLine(
                "    " +
                y +
                ","
            );

            builder.Append(
                indent
            );

            builder.AppendLine(
                "    BlockRenderer.BlockPixelSize,"
            );

            builder.Append(
                indent
            );

            builder.AppendLine(
                "    chunk.X * Chunk.SizeX + " +
                x +
                ","
            );

            builder.Append(
                indent
            );

            builder.AppendLine(
                "    chunk.Y * Chunk.SizeY + " +
                y +
                ");"
            );


            cursor =
                match.Index +
                match.Length;
        }


        builder.Append(
            source,
            cursor,
            source.Length -
            cursor
        );


        Backup(
            path
        );


        File.WriteAllText(
            path,
            builder.ToString(),
            new UTF8Encoding(
                true
            )
        );


        return true;
    }


    private static bool PatchWorldManager()
    {
        string path =
            FindClassFile(
                "WorldManager"
            );


        if (
            string.IsNullOrEmpty(
                path
            )
        )
        {
            return false;
        }


        string source =
            File.ReadAllText(
                path
            );


        if (
            source.Contains(
                WorldManagerMarker
            )
        )
        {
            return false;
        }


        int setBackground =
            source.IndexOf(
                "SetBackground",
                StringComparison.Ordinal
            );


        if (setBackground < 0)
            return false;


        int changedCheck =
            source.IndexOf(
                "if (!changed)",
                setBackground,
                StringComparison.Ordinal
            );


        if (changedCheck < 0)
            return false;


        int returnFalse =
            source.IndexOf(
                "return false;",
                changedCheck,
                StringComparison.Ordinal
            );


        if (returnFalse < 0)
            return false;


        int semicolon =
            source.IndexOf(
                ';',
                returnFalse
            );


        if (semicolon < 0)
            return false;


        string indent =
            GetIndent(
                source,
                changedCheck
            );


        string injection =
            Environment.NewLine +
            Environment.NewLine +
            indent +
            WorldManagerMarker +
            Environment.NewLine +
            indent +
            "Game.BlockTransforms.BackgroundBlockTransformRegistry.Clear(worldX, worldY);" +
            Environment.NewLine;


        source =
            source.Insert(
                semicolon +
                1,
                injection
            );


        Backup(
            path
        );


        File.WriteAllText(
            path,
            source,
            new UTF8Encoding(
                true
            )
        );


        return true;
    }


    private static string FindClassFile(
        string className
    )
    {
        string[] files =
            Directory.GetFiles(
                Application.dataPath,
                "*.cs",
                SearchOption.AllDirectories
            );


        string token =
            "class " +
            className;


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
                    "/GameData/Editor/"
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
                    token
                )
            )
            {
                continue;
            }


            int score =
                0;


            if (
                string.Equals(
                    Path.GetFileNameWithoutExtension(
                        path
                    ),
                    className,
                    StringComparison.Ordinal
                )
            )
            {
                score +=
                    100;
            }


            if (
                className ==
                "ChunkRenderer"
                &&
                normalized.Contains(
                    "/World/Rendering/"
                )
            )
            {
                score +=
                    100;
            }


            if (
                className ==
                "WorldManager"
                &&
                normalized.Contains(
                    "/World/"
                )
            )
            {
                score +=
                    100;
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


    private static string GetIndent(
        string source,
        int index
    )
    {
        int start =
            source.LastIndexOf(
                '\n',
                index
            );


        start =
            start <
            0
                ? 0
                : start +
                1;


        int end =
            start;


        while (
            end <
            source.Length
            &&
            (
                source[end] ==
                ' '
                ||
                source[end] ==
                '\t'
            )
        )
        {
            end++;
        }


        return
            source.Substring(
                start,
                end -
                start
            );
    }


    private static void Backup(
        string path
    )
    {
        string projectRoot =
            Directory.GetParent(
                Application.dataPath
            ).FullName;


        string relative =
            path
                .Replace(
                    '\\',
                    '/'
                )
                .Substring(
                    Application.dataPath
                        .Replace(
                            '\\',
                            '/'
                        )
                        .Length
                )
                .TrimStart(
                    '/'
                );


        string backup =
            Path.Combine(
                projectRoot,
                "StructureLayer_Backups",
                DateTime.Now.ToString(
                    "yyyyMMdd_HHmmss"
                ),
                "Assets",
                relative
            );


        Directory.CreateDirectory(
            Path.GetDirectoryName(
                backup
            )
        );


        File.Copy(
            path,
            backup,
            true
        );
    }
}

#endif
