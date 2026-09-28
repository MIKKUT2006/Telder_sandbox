#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public static class TelderVegetationFixV2
{
    private const string Marker = "TELDER_VEGETATION_FIX_V2";

    [MenuItem("Tools/Game/Apply Vegetation Fix V2")]
    public static void Apply()
    {
        string assetsRoot = Application.dataPath;
        string gameData = Path.Combine(assetsRoot, "GameData");

        var changed = new List<string>();
        var info = new List<string>();
        var warnings = new List<string>();

        try
        {
            string chunkDataPath = FindChunkData(assetsRoot);

            if (chunkDataPath == null)
                throw new Exception(
                    "Could not find the source file that declares Game.World.ChunkData."
                );

            PatchChunkData(
                chunkDataPath,
                changed,
                info
            );

            WriteFurnitureBridge(
                gameData,
                changed
            );

            PatchChunkLoader(
                gameData,
                changed,
                info,
                warnings
            );

            PatchWorldGenerator(
                gameData,
                changed,
                info,
                warnings
            );

            EditorPrefs.SetBool(
                "TelderVegetationFixV2.ValidateAfterReload",
                true
            );

            AssetDatabase.Refresh();

            Debug.Log(
                "VEGETATION FIX V2: source patch completed.\n\n" +
                "Changed:\n" +
                string.Join(
                    "\n",
                    changed.Select(
                        p => "  • " + ToAssetPath(p)
                    )
                ) +
                "\n\nInfo:\n" +
                string.Join(
                    "\n",
                    info.Select(
                        x => "  • " + x
                    )
                ) +
                (warnings.Count > 0
                    ? "\n\nWarnings:\n" +
                      string.Join(
                          "\n",
                          warnings.Select(
                              x => "  • " + x
                          )
                      )
                    : "")
            );

            EditorUtility.DisplayDialog(
                "Vegetation Fix V2",
                "Patch written.\n\n" +
                "Now wait for Unity to finish recompiling scripts.\n" +
                "After reload the patch validates the REAL compiled ChunkData API automatically.\n\n" +
                "Then create a NEW world or walk into never-generated chunks.",
                "OK"
            );
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            EditorUtility.DisplayDialog(
                "Vegetation Fix V2",
                ex.Message,
                "OK"
            );
        }
    }

    [DidReloadScripts]
    private static void AfterScriptsReloaded()
    {
        if (
            !EditorPrefs.GetBool(
                "TelderVegetationFixV2.ValidateAfterReload",
                false
            )
        )
        {
            return;
        }

        EditorPrefs.DeleteKey(
            "TelderVegetationFixV2.ValidateAfterReload"
        );

        ValidateCompiledApi();
    }

    [MenuItem("Tools/Game/Validate Vegetation Fix V2")]
    public static void ValidateCompiledApi()
    {
        Type type =
            typeof(Game.World.ChunkData);

        MethodInfo get =
            type.GetMethod(
                "GetFurniture",
                BindingFlags.Public |
                BindingFlags.Instance,
                null,
                new[]
                {
                    typeof(int),
                    typeof(int)
                },
                null
            );

        MethodInfo set =
            type.GetMethod(
                "SetFurniture",
                BindingFlags.Public |
                BindingFlags.Instance,
                null,
                new[]
                {
                    typeof(int),
                    typeof(int),
                    typeof(string)
                },
                null
            );

        if (get == null || set == null)
        {
            Debug.LogError(
                "VEGETATION FIX V2 VALIDATION FAILED: " +
                "the COMPILED Game.World.ChunkData still does not expose " +
                "GetFurniture(int,int) and SetFurniture(int,int,string).\n" +
                "This means another ChunkData source/assembly is being compiled."
            );

            EditorUtility.DisplayDialog(
                "Vegetation Fix V2",
                "FAILED: compiled ChunkData still has no furniture API.\n" +
                "Check Console. Another ChunkData source or asmdef is overriding the patched file.",
                "OK"
            );

            return;
        }

        Debug.Log(
            "VEGETATION FIX V2 VALIDATION OK:\n" +
            type.FullName + "\n" +
            get + "\n" +
            set + "\n\n" +
            "The old CAVE VEGETATION furniture API warning must now disappear."
        );

        EditorUtility.DisplayDialog(
            "Vegetation Fix V2",
            "OK: the real compiled Game.World.ChunkData now contains both furniture methods.\n\n" +
            "Test in a NEW world / new chunks.",
            "OK"
        );
    }

    // =========================================================
    // CHUNK DATA
    // =========================================================

    private static string FindChunkData(
        string assetsRoot
    )
    {
        string[] candidates =
            Directory.GetFiles(
                assetsRoot,
                "ChunkData.cs",
                SearchOption.AllDirectories
            );

        foreach (string path in candidates)
        {
            string text;

            try
            {
                text =
                    File.ReadAllText(path);
            }
            catch
            {
                continue;
            }

            if (
                Regex.IsMatch(
                    text,
                    @"namespace\s+Game\.World\b"
                )
                &&
                Regex.IsMatch(
                    text,
                    @"\bclass\s+ChunkData\b"
                )
            )
            {
                return path;
            }
        }

        // Fallback: class may live in a file with a different name.
        foreach (
            string path
            in Directory.GetFiles(
                assetsRoot,
                "*.cs",
                SearchOption.AllDirectories
            )
        )
        {
            string text;

            try
            {
                text =
                    File.ReadAllText(path);
            }
            catch
            {
                continue;
            }

            if (
                Regex.IsMatch(
                    text,
                    @"namespace\s+Game\.World\b"
                )
                &&
                Regex.IsMatch(
                    text,
                    @"\bclass\s+ChunkData\b"
                )
            )
            {
                return path;
            }
        }

        return null;
    }

    private static void PatchChunkData(
        string path,
        List<string> changed,
        List<string> info
    )
    {
        string text =
            File.ReadAllText(path);

        bool hasGet =
            Regex.IsMatch(
                text,
                @"public\s+string\s+GetFurniture\s*\(\s*int\s+\w+\s*,\s*int\s+\w+\s*\)"
            );

        bool hasSet =
            Regex.IsMatch(
                text,
                @"public\s+bool\s+SetFurniture\s*\(\s*int\s+\w+\s*,\s*int\s+\w+\s*,\s*string\s+\w+\s*\)"
            )
            ||
            Regex.IsMatch(
                text,
                @"public\s+void\s+SetFurniture\s*\(\s*int\s+\w+\s*,\s*int\s+\w+\s*,\s*string\s+\w+\s*\)"
            );

        if (hasGet && hasSet)
        {
            info.Add(
                "ChunkData already has the exact public furniture API in source."
            );

            return;
        }

        string block = @"

        // =====================================================
        // GENERATED FURNITURE BUFFER
        // TELDER_VEGETATION_FIX_V2
        // =====================================================

        private readonly Dictionary<int, string>
            telderGeneratedFurniture =
                new Dictionary<int, string>();


        private static int TelderFurnitureKey(
            int localX,
            int localY
        )
        {
            unchecked
            {
                return
                    (localX & 0xFFFF)
                    |
                    (localY << 16);
            }
        }


        public string GetFurniture(
            int localX,
            int localY
        )
        {
            if (
                localX < 0 ||
                localX >= Chunk.SizeX ||
                localY < 0 ||
                localY >= Chunk.SizeY
            )
            {
                return null;
            }

            return
                telderGeneratedFurniture.TryGetValue(
                    TelderFurnitureKey(
                        localX,
                        localY
                    ),
                    out string blockId
                )
                    ? blockId
                    : null;
        }


        public bool SetFurniture(
            int localX,
            int localY,
            string blockId
        )
        {
            if (
                localX < 0 ||
                localX >= Chunk.SizeX ||
                localY < 0 ||
                localY >= Chunk.SizeY ||
                string.IsNullOrWhiteSpace(
                    blockId
                )
            )
            {
                return false;
            }

            int key =
                TelderFurnitureKey(
                    localX,
                    localY
                );

            if (
                telderGeneratedFurniture.TryGetValue(
                    key,
                    out string old
                )
                &&
                string.Equals(
                    old,
                    blockId,
                    StringComparison.Ordinal
                )
            )
            {
                return false;
            }

            telderGeneratedFurniture[
                key
            ] =
                blockId;

            return true;
        }
";

        if (
            !Regex.IsMatch(
                text,
                @"using\s+System\s*;"
            )
        )
        {
            text =
                "using System;\n" +
                text;
        }

        if (
            !Regex.IsMatch(
                text,
                @"using\s+System\.Collections\.Generic\s*;"
            )
        )
        {
            text =
                "using System.Collections.Generic;\n" +
                text;
        }

        text =
            InsertIntoNamedClass(
                text,
                "ChunkData",
                block
            );

        Backup(path);

        File.WriteAllText(
            path,
            text,
            new UTF8Encoding(false)
        );

        changed.Add(path);

        info.Add(
            "Injected exact public GetFurniture(int,int) / SetFurniture(int,int,string) into Game.World.ChunkData."
        );
    }

    // =========================================================
    // RUNTIME BRIDGE
    // =========================================================

    private static void WriteFurnitureBridge(
        string gameData,
        List<string> changed
    )
    {
        string dir =
            Path.Combine(
                gameData,
                "World",
                "Vegetation",
                "Runtime"
            );

        Directory.CreateDirectory(dir);

        string path =
            Path.Combine(
                dir,
                "GeneratedVegetationFurnitureBridge.cs"
            );

        string source =
@"using Game.World.Furniture;

namespace Game.World.Vegetation.Runtime
{
    public static class GeneratedVegetationFurnitureBridge
    {
        public static void Apply(
            ChunkData data,
            int chunkX,
            int chunkY
        )
        {
            if (data == null)
                return;

            FurnitureLayerManager manager =
                FurnitureLayerManager.EnsureInstance();

            if (manager == null)
                return;

            int startX =
                chunkX *
                Chunk.SizeX;

            int startY =
                chunkY *
                Chunk.SizeY;

            for (
                int localX = 0;
                localX < Chunk.SizeX;
                localX++
            )
            {
                for (
                    int localY = 0;
                    localY < Chunk.SizeY;
                    localY++
                )
                {
                    string blockId =
                        data.GetFurniture(
                            localX,
                            localY
                        );

                    if (
                        string.IsNullOrWhiteSpace(
                            blockId
                        )
                    )
                    {
                        continue;
                    }

                    manager.SetGeneratedFurniture(
                        startX + localX,
                        startY + localY,
                        blockId
                    );
                }
            }
        }


        public static void Unload(
            int chunkX,
            int chunkY
        )
        {
            if (
                FurnitureLayerManager.Instance ==
                null
            )
            {
                return;
            }

            FurnitureLayerManager.Instance
                .UnloadGeneratedFurnitureChunk(
                    chunkX,
                    chunkY
                );
        }
    }
}
";

        WriteIfDifferent(
            path,
            source,
            changed
        );
    }

    // =========================================================
    // CHUNK LOADER
    // =========================================================

    private static void PatchChunkLoader(
        string gameData,
        List<string> changed,
        List<string> info,
        List<string> warnings
    )
    {
        string path =
            FindFile(
                gameData,
                "ChunkLoader.cs"
            );

        if (path == null)
        {
            warnings.Add(
                "ChunkLoader.cs not found."
            );

            return;
        }

        string text =
            File.ReadAllText(path);

        string original =
            text;

        if (
            !text.Contains(
                "GeneratedVegetationFurnitureBridge.Apply"
            )
        )
        {
            Match m =
                Regex.Match(
                    text,
                    @"chunk\.ApplyData\s*\(\s*data\s*\)\s*;",
                    RegexOptions.Singleline
                );

            if (m.Success)
            {
                string insert =
m.Value +
@"

                // TELDER_VEGETATION_FIX_V2
                Game.World.Vegetation.Runtime
                    .GeneratedVegetationFurnitureBridge
                    .Apply(
                        data,
                        position.x,
                        position.y
                    );";

                text =
                    text.Remove(
                        m.Index,
                        m.Length
                    )
                    .Insert(
                        m.Index,
                        insert
                    );

                info.Add(
                    "ChunkLoader now transfers generated vegetation from ChunkData into FurnitureLayerManager."
                );
            }
            else
            {
                warnings.Add(
                    "ChunkLoader: chunk.ApplyData(data) patch point not found."
                );
            }
        }

        if (
            !text.Contains(
                "GeneratedVegetationFurnitureBridge.Unload"
            )
        )
        {
            Match m =
                Regex.Match(
                    text,
                    @"renderer\.RemoveChunk\s*\(\s*position\.x\s*,\s*position\.y\s*\)\s*;",
                    RegexOptions.Singleline
                );

            if (m.Success)
            {
                string insert =
m.Value +
@"

                Game.World.Vegetation.Runtime
                    .GeneratedVegetationFurnitureBridge
                    .Unload(
                        position.x,
                        position.y
                    );";

                text =
                    text.Remove(
                        m.Index,
                        m.Length
                    )
                    .Insert(
                        m.Index,
                        insert
                    );

                info.Add(
                    "ChunkLoader now removes procedural vegetation visuals when a chunk unloads."
                );
            }
        }

        if (text != original)
        {
            Backup(path);

            File.WriteAllText(
                path,
                text,
                new UTF8Encoding(false)
            );

            changed.Add(path);
        }
    }

    // =========================================================
    // WORLD GENERATOR
    // =========================================================

    private static void PatchWorldGenerator(
        string gameData,
        List<string> changed,
        List<string> info,
        List<string> warnings
    )
    {
        string path =
            FindFile(
                gameData,
                "WorldGenerator.cs"
            );

        if (path == null)
        {
            warnings.Add(
                "WorldGenerator.cs not found."
            );

            return;
        }

        string text =
            File.ReadAllText(path);

        string method =
            ExtractMethod(
                text,
                "GenerateChunkData"
            );

        if (method == null)
        {
            warnings.Add(
                "WorldGenerator.GenerateChunkData not found."
            );

            return;
        }

        List<string> runtimeTypes =
            DiscoverSurfaceVegetationRuntimes(
                gameData
            );

        if (runtimeTypes.Count == 0)
        {
            warnings.Add(
                "No separate surface vegetation GenerationRuntime was discovered. " +
                "If surface grass is a configured biome-furniture system, send that runtime/config next."
            );
        }

        var missingCalls =
            new List<string>();

        foreach (
            string runtime
            in runtimeTypes
        )
        {
            if (
                method.Contains(
                    runtime
                )
            )
            {
                info.Add(
                    "Surface vegetation runtime already present: " +
                    runtime
                );

                continue;
            }

            missingCalls.Add(
@"            " +
runtime +
@".ApplyToChunk(
                this,
                settings,
                data,
                chunkX,
                chunkY
            );"
            );
        }

        string structureRuntime =
            "Game.World.Structures.StructureGenerationRuntime";

        string structureFile =
            FindFile(
                gameData,
                "StructureGenerationRuntime.cs"
            );

        if (
            structureFile != null
            &&
            !method.Contains(
                "StructureGenerationRuntime.ApplyToChunk"
            )
        )
        {
            missingCalls.Insert(
                0,
@"            " +
structureRuntime +
@".ApplyToChunk(
                this,
                settings,
                data,
                chunkX,
                chunkY
            );"
            );

            info.Add(
                "Structure generation call was missing and will be restored."
            );
        }

        if (missingCalls.Count == 0)
            return;

        string insertion =
            "\n\n            // " +
            Marker +
            "\n" +
            string.Join(
                "\n\n",
                missingCalls
            ) +
            "\n";

        int methodStart;
        int methodEnd;

        if (
            !TryFindMethodBounds(
                text,
                "GenerateChunkData",
                out methodStart,
                out methodEnd
            )
        )
        {
            warnings.Add(
                "Could not locate GenerateChunkData method bounds."
            );

            return;
        }

        // Prefer placing surface vegetation before cave vegetation.
        int caveIndex =
            text.IndexOf(
                "CaveVegetationGenerationRuntime.ApplyToChunk",
                methodStart,
                methodEnd - methodStart + 1,
                StringComparison.Ordinal
            );

        int insertIndex = -1;

        if (caveIndex >= 0)
        {
            int lineStart =
                text.LastIndexOf(
                    '\n',
                    caveIndex
                );

            insertIndex =
                lineStart >= methodStart
                    ? lineStart + 1
                    : caveIndex;
        }
        else
        {
            insertIndex =
                text.LastIndexOf(
                    "return data;",
                    methodEnd,
                    methodEnd - methodStart + 1,
                    StringComparison.Ordinal
                );
        }

        if (insertIndex < methodStart)
        {
            warnings.Add(
                "Could not find insertion point for vegetation generation calls."
            );

            return;
        }

        text =
            text.Insert(
                insertIndex,
                insertion
            );

        Backup(path);

        File.WriteAllText(
            path,
            text,
            new UTF8Encoding(false)
        );

        changed.Add(path);

        foreach (
            string runtime
            in missingCalls
        )
        {
            info.Add(
                "Restored generation stage in WorldGenerator."
            );
        }
    }

    private static List<string>
        DiscoverSurfaceVegetationRuntimes(
            string gameData
        )
    {
        var result =
            new HashSet<string>(
                StringComparer.Ordinal
            );

        string worldDir =
            Path.Combine(
                gameData,
                "World"
            );

        if (!Directory.Exists(worldDir))
            return result.ToList();

        foreach (
            string path
            in Directory.GetFiles(
                worldDir,
                "*.cs",
                SearchOption.AllDirectories
            )
        )
        {
            string fileName =
                Path.GetFileName(path);

            if (
                fileName.IndexOf(
                    "Vegetation",
                    StringComparison.OrdinalIgnoreCase
                ) < 0
                &&
                fileName.IndexOf(
                    "Flora",
                    StringComparison.OrdinalIgnoreCase
                ) < 0
            )
            {
                continue;
            }

            if (
                fileName.IndexOf(
                    "Cave",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
            )
            {
                continue;
            }

            string source;

            try
            {
                source =
                    File.ReadAllText(path);
            }
            catch
            {
                continue;
            }

            if (
                !source.Contains(
                    "ApplyToChunk"
                )
            )
            {
                continue;
            }

            Match ns =
                Regex.Match(
                    source,
                    @"namespace\s+([A-Za-z_][A-Za-z0-9_\.]*)"
                );

            Match cl =
                Regex.Match(
                    source,
                    @"\b(?:public\s+)?(?:static\s+)?class\s+([A-Za-z_][A-Za-z0-9_]*)"
                );

            if (!ns.Success || !cl.Success)
                continue;

            string className =
                cl.Groups[1].Value;

            if (
                className.IndexOf(
                    "Runtime",
                    StringComparison.OrdinalIgnoreCase
                ) < 0
            )
            {
                continue;
            }

            result.Add(
                ns.Groups[1].Value +
                "." +
                className
            );
        }

        return
            result
                .OrderBy(x => x)
                .ToList();
    }

    // =========================================================
    // SOURCE HELPERS
    // =========================================================

    private static string FindFile(
        string root,
        string fileName
    )
    {
        if (!Directory.Exists(root))
            return null;

        return
            Directory.GetFiles(
                root,
                fileName,
                SearchOption.AllDirectories
            )
            .FirstOrDefault(
                p =>
                    !p.EndsWith(
                        ".vegetationfixv2.bak",
                        StringComparison.OrdinalIgnoreCase
                    )
            );
    }

    private static void WriteIfDifferent(
        string path,
        string source,
        List<string> changed
    )
    {
        Directory.CreateDirectory(
            Path.GetDirectoryName(path)
        );

        if (
            File.Exists(path)
            &&
            File.ReadAllText(path) ==
            source
        )
        {
            return;
        }

        if (File.Exists(path))
            Backup(path);

        File.WriteAllText(
            path,
            source,
            new UTF8Encoding(false)
        );

        changed.Add(path);
    }

    private static void Backup(
        string path
    )
    {
        string backup =
            path +
            ".vegetationfixv2.bak";

        if (!File.Exists(backup))
        {
            File.Copy(
                path,
                backup,
                false
            );
        }
    }

    private static string ToAssetPath(
        string absolute
    )
    {
        string assets =
            Application.dataPath
                .Replace('\\', '/');

        string normalized =
            absolute.Replace('\\', '/');

        if (
            normalized.StartsWith(
                assets,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return
                "Assets" +
                normalized.Substring(
                    assets.Length
                );
        }

        return absolute;
    }

    private static string InsertIntoNamedClass(
        string text,
        string className,
        string insertion
    )
    {
        Match match =
            Regex.Match(
                text,
                @"\bclass\s+" +
                Regex.Escape(className) +
                @"\b"
            );

        if (!match.Success)
            throw new Exception(
                "Class not found: " +
                className
            );

        int open =
            text.IndexOf(
                '{',
                match.Index + match.Length
            );

        if (open < 0)
            throw new Exception(
                "Opening brace not found for " +
                className
            );

        int close =
            FindMatchingBrace(
                text,
                open
            );

        if (close < 0)
            throw new Exception(
                "Closing brace not found for " +
                className
            );

        return
            text.Insert(
                close,
                insertion
            );
    }

    private static string ExtractMethod(
        string text,
        string methodName
    )
    {
        if (
            !TryFindMethodBounds(
                text,
                methodName,
                out int start,
                out int end
            )
        )
        {
            return null;
        }

        return
            text.Substring(
                start,
                end - start + 1
            );
    }

    private static bool TryFindMethodBounds(
        string text,
        string methodName,
        out int start,
        out int end
    )
    {
        start = -1;
        end = -1;

        MatchCollection matches =
            Regex.Matches(
                text,
                @"\b" +
                Regex.Escape(methodName) +
                @"\s*\("
            );

        foreach (Match match in matches)
        {
            int lineStart =
                text.LastIndexOf(
                    '\n',
                    match.Index
                );

            lineStart =
                lineStart < 0
                    ? 0
                    : lineStart + 1;

            string prefix =
                text.Substring(
                    lineStart,
                    match.Index - lineStart
                );

            // Method declaration should have a return type before its name.
            if (
                prefix.IndexOf(
                    ".",
                    StringComparison.Ordinal
                ) >= 0
                &&
                prefix.TrimStart().Length == 0
            )
            {
                continue;
            }

            int open =
                text.IndexOf(
                    '{',
                    match.Index + match.Length
                );

            if (open < 0)
                continue;

            // Avoid accidentally treating a call as a declaration:
            // there must be a ')' before the opening body brace.
            int closeParen =
                text.IndexOf(
                    ')',
                    match.Index + match.Length
                );

            if (
                closeParen < 0
                ||
                closeParen > open
            )
            {
                continue;
            }

            int close =
                FindMatchingBrace(
                    text,
                    open
                );

            if (close < 0)
                continue;

            // Declaration line usually contains visibility or a return type.
            string decl =
                text.Substring(
                    lineStart,
                    Math.Min(
                        open - lineStart,
                        300
                    )
                );

            if (
                decl.Contains(
                    ";"
                )
                &&
                !decl.Contains(
                    "public "
                )
                &&
                !decl.Contains(
                    "private "
                )
                &&
                !decl.Contains(
                    "protected "
                )
                &&
                !decl.Contains(
                    "internal "
                )
            )
            {
                continue;
            }

            start =
                lineStart;

            end =
                close;

            return true;
        }

        return false;
    }

    private static int FindMatchingBrace(
        string text,
        int openIndex
    )
    {
        int depth = 0;

        bool lineComment = false;
        bool blockComment = false;
        bool inString = false;
        bool verbatim = false;
        bool inChar = false;
        bool escape = false;

        for (
            int i = openIndex;
            i < text.Length;
            i++
        )
        {
            char c =
                text[i];

            char n =
                i + 1 < text.Length
                    ? text[i + 1]
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
                    n == '/'
                )
                {
                    blockComment = false;
                    i++;
                }

                continue;
            }

            if (inString)
            {
                if (verbatim)
                {
                    if (
                        c == '"'
                        &&
                        n == '"'
                    )
                    {
                        i++;
                        continue;
                    }

                    if (c == '"')
                    {
                        inString = false;
                        verbatim = false;
                    }

                    continue;
                }

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
                n == '/'
            )
            {
                lineComment = true;
                i++;

                continue;
            }

            if (
                c == '/'
                &&
                n == '*'
            )
            {
                blockComment = true;
                i++;

                continue;
            }

            if (
                c == '@'
                &&
                n == '"'
            )
            {
                inString = true;
                verbatim = true;
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

        return -1;
    }
}
#endif
