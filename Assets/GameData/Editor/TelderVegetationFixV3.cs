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

public static class TelderVegetationFixV3
{
    private const string ValidationKey =
        "TelderVegetationFixV3.ValidateAfterReload";

    [MenuItem("Tools/Game/Apply Vegetation Fix V3")]
    public static void Apply()
    {
        string assetsRoot =
            Application.dataPath;

        string gameData =
            Path.Combine(
                assetsRoot,
                "GameData"
            );

        var changed =
            new List<string>();

        var info =
            new List<string>();

        var warnings =
            new List<string>();

        try
        {
            string chunkDataPath =
                FindChunkDataSource(
                    assetsRoot
                );

            if (
                string.IsNullOrWhiteSpace(
                    chunkDataPath
                )
            )
            {
                throw new Exception(
                    "Game.World.ChunkData source was not found inside Assets."
                );
            }

            PatchChunkDataToUShortFurniture(
                chunkDataPath,
                changed,
                info
            );

            WriteV3Bridge(
                gameData,
                changed
            );

            RewriteOldBridgeFiles(
                gameData,
                changed,
                info
            );

            PatchChunkLoader(
                gameData,
                changed,
                info,
                warnings
            );

            PatchWorldGeneratorStages(
                gameData,
                changed,
                info,
                warnings
            );

            EditorPrefs.SetBool(
                ValidationKey,
                true
            );

            AssetDatabase.Refresh();

            Debug.Log(
                "VEGETATION FIX V3 WRITTEN.\n\n" +
                "Changed:\n" +
                string.Join(
                    "\n",
                    changed.Select(
                        x =>
                            "  • " +
                            ToAssetPath(x)
                    )
                ) +
                "\n\nInfo:\n" +
                string.Join(
                    "\n",
                    info.Select(
                        x => "  • " + x
                    )
                ) +
                (
                    warnings.Count > 0
                        ? "\n\nWarnings:\n" +
                          string.Join(
                              "\n",
                              warnings.Select(
                                  x => "  • " + x
                              )
                          )
                        : ""
                )
            );

            EditorUtility.DisplayDialog(
                "Vegetation Fix V3",
                "V3 written.\n\n" +
                "Wait until Unity finishes compiling.\n" +
                "After script reload V3 will validate the EXACT API expected by CaveVegetationGenerationRuntime:\n\n" +
                "GetFurniture(int,int) -> ushort\n" +
                "SetFurniture(int,int,ushort)\n\n" +
                "Do not enter Play Mode until compilation is finished.",
                "OK"
            );
        }
        catch (
            Exception exception
        )
        {
            Debug.LogException(
                exception
            );

            EditorUtility.DisplayDialog(
                "Vegetation Fix V3",
                "Patch failed:\n\n" +
                exception.Message,
                "OK"
            );
        }
    }


    [DidReloadScripts]
    private static void ValidateAfterReload()
    {
        if (
            !EditorPrefs.GetBool(
                ValidationKey,
                false
            )
        )
        {
            return;
        }

        EditorPrefs.DeleteKey(
            ValidationKey
        );

        Validate();
    }


    [MenuItem("Tools/Game/Validate Vegetation Fix V3")]
    public static void Validate()
    {
        Type type =
            typeof(
                Game.World.ChunkData
            );

        MethodInfo get =
            type.GetMethod(
                "GetFurniture",
                BindingFlags.Instance |
                BindingFlags.Public,
                null,
                new Type[]
                {
                    typeof(int),
                    typeof(int)
                },
                null
            );

        MethodInfo set =
            type.GetMethod(
                "SetFurniture",
                BindingFlags.Instance |
                BindingFlags.Public,
                null,
                new Type[]
                {
                    typeof(int),
                    typeof(int),
                    typeof(ushort)
                },
                null
            );

        bool getOk =
            get != null &&
            get.ReturnType ==
                typeof(ushort);

        bool setOk =
            set != null;

        if (
            !getOk ||
            !setOk
        )
        {
            string message =
                "VEGETATION FIX V3 VALIDATION FAILED.\n\n" +
                "Compiled type: " +
                type.AssemblyQualifiedName +
                "\n\n" +
                "GetFurniture(int,int): " +
                (
                    get == null
                        ? "MISSING"
                        : get.ToString()
                ) +
                "\n" +
                "SetFurniture(int,int,ushort): " +
                (
                    set == null
                        ? "MISSING"
                        : set.ToString()
                );

            Debug.LogError(
                message
            );

            EditorUtility.DisplayDialog(
                "Vegetation Fix V3",
                message,
                "OK"
            );

            return;
        }

        string ok =
            "VEGETATION FIX V3 VALIDATION OK\n\n" +
            get +
            "\n" +
            set +
            "\n\n" +
            "This is the exact ushort furniture API expected by the cave vegetation runtime.";

        Debug.Log(
            ok
        );

        EditorUtility.DisplayDialog(
            "Vegetation Fix V3",
            ok +
            "\n\nNow test in a new world or never-generated chunks.",
            "OK"
        );
    }


    // =====================================================
    // CHUNK DATA
    // =====================================================

    private static string FindChunkDataSource(
        string assetsRoot
    )
    {
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
                    File.ReadAllText(
                        path
                    );
            }
            catch
            {
                continue;
            }

            if (
                !Regex.IsMatch(
                    text,
                    @"namespace\s+Game\.World\b"
                )
            )
            {
                continue;
            }

            if (
                Regex.IsMatch(
                    text,
                    @"\b(?:class|struct)\s+ChunkData\b"
                )
            )
            {
                return path;
            }
        }

        return null;
    }


    private static void PatchChunkDataToUShortFurniture(
        string path,
        List<string> changed,
        List<string> info
    )
    {
        string text =
            File.ReadAllText(
                path
            );

        string original =
            text;

        // Remove any earlier V1/V2 GetFurniture methods.
        text =
            RemoveMethodsNamed(
                text,
                "ChunkData",
                "GetFurniture"
            );

        // Remove any earlier V1/V2 SetFurniture overloads.
        text =
            RemoveMethodsNamed(
                text,
                "ChunkData",
                "SetFurniture"
            );

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

        string block =
@"

        // =====================================================
        // PROCEDURAL FURNITURE GENERATION BUFFER
        // TELDER_VEGETATION_FIX_V3
        //
        // IMPORTANT:
        // CaveVegetationGenerationRuntime expects ushort IDs here.
        // The ID is converted to ContentID only after ChunkData is
        // applied to a live chunk.
        // =====================================================

        private readonly Dictionary<int, ushort>
            telderFurnitureV3 =
                new Dictionary<int, ushort>();


        private static int TelderFurnitureKeyV3(
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


        public ushort GetFurniture(
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
                return 0;
            }

            return
                telderFurnitureV3.TryGetValue(
                    TelderFurnitureKeyV3(
                        localX,
                        localY
                    ),
                    out ushort blockId
                )
                    ? blockId
                    : (ushort)0;
        }


        public bool SetFurniture(
            int localX,
            int localY,
            ushort blockId
        )
        {
            if (
                localX < 0 ||
                localX >= Chunk.SizeX ||
                localY < 0 ||
                localY >= Chunk.SizeY ||
                blockId == 0
            )
            {
                return false;
            }

            int key =
                TelderFurnitureKeyV3(
                    localX,
                    localY
                );

            if (
                telderFurnitureV3.TryGetValue(
                    key,
                    out ushort oldId
                )
                &&
                oldId == blockId
            )
            {
                return false;
            }

            telderFurnitureV3[
                key
            ] =
                blockId;

            return true;
        }
";

        text =
            InsertIntoType(
                text,
                "ChunkData",
                block
            );

        if (
            text != original
        )
        {
            Backup(
                path
            );

            File.WriteAllText(
                path,
                text,
                new UTF8Encoding(false)
            );

            changed.Add(
                path
            );
        }

        info.Add(
            "ChunkData now exposes ushort GetFurniture(int,int) and SetFurniture(int,int,ushort)."
        );
    }


    // =====================================================
    // V3 BRIDGE
    // =====================================================

    private static void WriteV3Bridge(
        string gameData,
        List<string> changed
    )
    {
        string directory =
            Path.Combine(
                gameData,
                "World",
                "Vegetation",
                "Runtime"
            );

        Directory.CreateDirectory(
            directory
        );

        string path =
            Path.Combine(
                directory,
                "GeneratedVegetationFurnitureBridgeV3.cs"
            );

        string source =
@"using Game.Content;
using Game.World.Furniture;

namespace Game.World.Vegetation.Runtime
{
    public static class GeneratedVegetationFurnitureBridgeV3
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
                    ushort blockId =
                        data.GetFurniture(
                            localX,
                            localY
                        );

                    if (blockId == 0)
                        continue;

                    string contentId;

                    try
                    {
                        contentId =
                            BlockIDRegistry
                                .GetContentID(
                                    blockId
                                )
                                .ToString();
                    }
                    catch
                    {
                        continue;
                    }

                    if (
                        string.IsNullOrWhiteSpace(
                            contentId
                        )
                    )
                    {
                        continue;
                    }

                    manager.SetGeneratedFurniture(
                        startX + localX,
                        startY + localY,
                        contentId
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


    // =====================================================
    // COMPATIBILITY WITH V1/V2 BRIDGES
    // =====================================================

    private static void RewriteOldBridgeFiles(
        string gameData,
        List<string> changed,
        List<string> info
    )
    {
        string v1 =
            FindFile(
                gameData,
                "GeneratedFurnitureChunkBridge.cs"
            );

        if (v1 != null)
        {
            string source =
@"namespace Game.World.PolishFixes
{
    public static class GeneratedFurnitureChunkBridge
    {
        public static void Apply(
            Game.World.ChunkData data,
            int chunkX,
            int chunkY
        )
        {
            Game.World.Vegetation.Runtime
                .GeneratedVegetationFurnitureBridgeV3
                .Apply(
                    data,
                    chunkX,
                    chunkY
                );
        }

        public static void Unload(
            int chunkX,
            int chunkY
        )
        {
            Game.World.Vegetation.Runtime
                .GeneratedVegetationFurnitureBridgeV3
                .Unload(
                    chunkX,
                    chunkY
                );
        }
    }
}
";

            WriteIfDifferent(
                v1,
                source,
                changed
            );

            info.Add(
                "V1 bridge redirected to ushort V3 bridge."
            );
        }

        string v2 =
            FindFile(
                gameData,
                "GeneratedVegetationFurnitureBridge.cs"
            );

        if (
            v2 != null &&
            !v2.EndsWith(
                "GeneratedVegetationFurnitureBridgeV3.cs",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            string source =
@"namespace Game.World.Vegetation.Runtime
{
    public static class GeneratedVegetationFurnitureBridge
    {
        public static void Apply(
            ChunkData data,
            int chunkX,
            int chunkY
        )
        {
            GeneratedVegetationFurnitureBridgeV3
                .Apply(
                    data,
                    chunkX,
                    chunkY
                );
        }

        public static void Unload(
            int chunkX,
            int chunkY
        )
        {
            GeneratedVegetationFurnitureBridgeV3
                .Unload(
                    chunkX,
                    chunkY
                );
        }
    }
}
";

            WriteIfDifferent(
                v2,
                source,
                changed
            );

            info.Add(
                "V2 bridge redirected to ushort V3 bridge."
            );
        }
    }


    // =====================================================
    // CHUNK LOADER
    // =====================================================

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
            File.ReadAllText(
                path
            );

        if (
            text.Contains(
                "GeneratedFurnitureChunkBridge.Apply"
            )
            ||
            text.Contains(
                "GeneratedVegetationFurnitureBridge.Apply"
            )
            ||
            text.Contains(
                "GeneratedVegetationFurnitureBridgeV3.Apply"
            )
        )
        {
            info.Add(
                "ChunkLoader already contains a generated-furniture bridge call."
            );

            return;
        }

        Match match =
            Regex.Match(
                text,
                @"chunk\.ApplyData\s*\(\s*data\s*\)\s*;",
                RegexOptions.Singleline
            );

        if (!match.Success)
        {
            warnings.Add(
                "ChunkLoader: chunk.ApplyData(data) was not found."
            );

            return;
        }

        string insertion =
match.Value +
@"

                // TELDER_VEGETATION_FIX_V3
                Game.World.Vegetation.Runtime
                    .GeneratedVegetationFurnitureBridgeV3
                    .Apply(
                        data,
                        position.x,
                        position.y
                    );";

        text =
            text.Remove(
                match.Index,
                match.Length
            )
            .Insert(
                match.Index,
                insertion
            );

        Backup(
            path
        );

        File.WriteAllText(
            path,
            text,
            new UTF8Encoding(false)
        );

        changed.Add(
            path
        );

        info.Add(
            "ChunkLoader now commits ushort procedural furniture after ChunkData.ApplyData."
        );
    }


    // =====================================================
    // GENERATION PIPELINE
    // =====================================================

    private static void PatchWorldGeneratorStages(
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
            File.ReadAllText(
                path
            );

        if (
            !TryFindMethodBounds(
                text,
                "GenerateChunkData",
                out int start,
                out int end
            )
        )
        {
            warnings.Add(
                "WorldGenerator.GenerateChunkData was not found."
            );

            return;
        }

        string method =
            text.Substring(
                start,
                end - start + 1
            );

        var missing =
            new List<string>();

        string structureFile =
            FindFile(
                gameData,
                "StructureGenerationRuntime.cs"
            );

        if (
            structureFile != null &&
            !method.Contains(
                "StructureGenerationRuntime.ApplyToChunk"
            )
        )
        {
            string structureSource =
                File.ReadAllText(
                    structureFile
                );

            string fullName =
                GetFirstFullTypeName(
                    structureSource,
                    "StructureGenerationRuntime"
                );

            if (
                !string.IsNullOrWhiteSpace(
                    fullName
                )
            )
            {
                missing.Add(
                    BuildApplyToChunkCall(
                        fullName
                    )
                );
            }
        }

        foreach (
            string runtime
            in DiscoverSurfaceVegetationRuntimes(
                gameData
            )
        )
        {
            string shortName =
                runtime.Substring(
                    runtime.LastIndexOf('.') + 1
                );

            if (
                method.Contains(
                    shortName +
                    ".ApplyToChunk"
                )
            )
            {
                continue;
            }

            missing.Add(
                BuildApplyToChunkCall(
                    runtime
                )
            );
        }

        if (missing.Count == 0)
        {
            info.Add(
                "No missing surface vegetation/structure generation stages were discovered."
            );

            return;
        }

        int caveCall =
            text.IndexOf(
                "CaveVegetationGenerationRuntime.ApplyToChunk",
                start,
                end - start + 1,
                StringComparison.Ordinal
            );

        int insertAt;

        if (caveCall >= 0)
        {
            insertAt =
                text.LastIndexOf(
                    '\n',
                    caveCall
                );

            if (insertAt < start)
                insertAt = caveCall;
            else
                insertAt++;
        }
        else
        {
            insertAt =
                text.LastIndexOf(
                    "return data;",
                    end,
                    end - start + 1,
                    StringComparison.Ordinal
                );
        }

        if (insertAt < start)
        {
            warnings.Add(
                "Could not find a safe generation-stage insertion point."
            );

            return;
        }

        string insertion =
            "\n            // TELDER_VEGETATION_FIX_V3: restored generation stages\n" +
            string.Join(
                "\n\n",
                missing
            ) +
            "\n\n";

        text =
            text.Insert(
                insertAt,
                insertion
            );

        Backup(
            path
        );

        File.WriteAllText(
            path,
            text,
            new UTF8Encoding(false)
        );

        changed.Add(
            path
        );

        info.Add(
            "Restored " +
            missing.Count +
            " missing structure/surface vegetation generation stage(s)."
        );
    }


    private static List<string> DiscoverSurfaceVegetationRuntimes(
        string gameData
    )
    {
        var result =
            new HashSet<string>(
                StringComparer.Ordinal
            );

        foreach (
            string path
            in Directory.GetFiles(
                gameData,
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
                ||
                fileName.IndexOf(
                    "Bridge",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
                ||
                fileName.IndexOf(
                    "Editor",
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

            if (
                !ns.Success ||
                !cl.Success
            )
            {
                continue;
            }

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


    private static string BuildApplyToChunkCall(
        string fullType
    )
    {
        return
@"            " +
fullType +
@".ApplyToChunk(
                this,
                settings,
                data,
                chunkX,
                chunkY
            );";
    }


    private static string GetFirstFullTypeName(
        string source,
        string typeName
    )
    {
        Match ns =
            Regex.Match(
                source,
                @"namespace\s+([A-Za-z_][A-Za-z0-9_\.]*)"
            );

        if (!ns.Success)
            return null;

        if (
            !Regex.IsMatch(
                source,
                @"\bclass\s+" +
                Regex.Escape(typeName) +
                @"\b"
            )
        )
        {
            return null;
        }

        return
            ns.Groups[1].Value +
            "." +
            typeName;
    }


    // =====================================================
    // SOURCE EDIT HELPERS
    // =====================================================

    private static string RemoveMethodsNamed(
        string text,
        string typeName,
        string methodName
    )
    {
        if (
            !TryFindTypeBounds(
                text,
                typeName,
                out int typeStart,
                out int typeEnd
            )
        )
        {
            throw new Exception(
                "Type not found: " +
                typeName
            );
        }

        while (true)
        {
            Match match =
                Regex.Match(
                    text.Substring(
                        typeStart,
                        typeEnd - typeStart + 1
                    ),
                    @"\b(?:public|private|protected|internal)\s+" +
                    @"(?:static\s+)?" +
                    @"[A-Za-z_][A-Za-z0-9_<>\[\],\.\?]*\s+" +
                    Regex.Escape(methodName) +
                    @"\s*\("
                );

            if (!match.Success)
                break;

            int absolute =
                typeStart +
                match.Index;

            int lineStart =
                text.LastIndexOf(
                    '\n',
                    absolute
                );

            lineStart =
                lineStart < 0
                    ? absolute
                    : lineStart + 1;

            int openBrace =
                text.IndexOf(
                    '{',
                    absolute + match.Length
                );

            if (
                openBrace < 0 ||
                openBrace > typeEnd
            )
            {
                break;
            }

            int closeBrace =
                FindMatchingBrace(
                    text,
                    openBrace
                );

            if (
                closeBrace < 0 ||
                closeBrace > typeEnd
            )
            {
                break;
            }

            int removeEnd =
                closeBrace + 1;

            while (
                removeEnd < text.Length &&
                (
                    text[removeEnd] == '\r' ||
                    text[removeEnd] == '\n'
                )
            )
            {
                removeEnd++;
            }

            text =
                text.Remove(
                    lineStart,
                    removeEnd - lineStart
                );

            if (
                !TryFindTypeBounds(
                    text,
                    typeName,
                    out typeStart,
                    out typeEnd
                )
            )
            {
                break;
            }
        }

        return text;
    }


    private static string InsertIntoType(
        string text,
        string typeName,
        string insertion
    )
    {
        if (
            !TryFindTypeBounds(
                text,
                typeName,
                out int start,
                out int end
            )
        )
        {
            throw new Exception(
                "Could not locate type " +
                typeName
            );
        }

        return
            text.Insert(
                end,
                insertion
            );
    }


    private static bool TryFindTypeBounds(
        string text,
        string typeName,
        out int start,
        out int end
    )
    {
        start = -1;
        end = -1;

        Match match =
            Regex.Match(
                text,
                @"\b(?:class|struct)\s+" +
                Regex.Escape(typeName) +
                @"\b"
            );

        if (!match.Success)
            return false;

        int open =
            text.IndexOf(
                '{',
                match.Index + match.Length
            );

        if (open < 0)
            return false;

        int close =
            FindMatchingBrace(
                text,
                open
            );

        if (close < 0)
            return false;

        start =
            match.Index;

        end =
            close;

        return true;
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

        Match match =
            Regex.Match(
                text,
                @"\b(?:public|private|protected|internal)\s+" +
                @"(?:static\s+)?" +
                @"[A-Za-z_][A-Za-z0-9_<>\[\],\.\?]*\s+" +
                Regex.Escape(methodName) +
                @"\s*\("
            );

        if (!match.Success)
            return false;

        int lineStart =
            text.LastIndexOf(
                '\n',
                match.Index
            );

        start =
            lineStart < 0
                ? match.Index
                : lineStart + 1;

        int open =
            text.IndexOf(
                '{',
                match.Index + match.Length
            );

        if (open < 0)
            return false;

        end =
            FindMatchingBrace(
                text,
                open
            );

        return end >= 0;
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
                    c == '*' &&
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
                        c == '"' &&
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
                c == '/' &&
                n == '/'
            )
            {
                lineComment = true;
                i++;
                continue;
            }

            if (
                c == '/' &&
                n == '*'
            )
            {
                blockComment = true;
                i++;
                continue;
            }

            if (
                c == '@' &&
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
                path =>
                    !path.EndsWith(
                        ".vegetationfixv3.bak",
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
            Path.GetDirectoryName(
                path
            )
        );

        if (
            File.Exists(path) &&
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

        changed.Add(
            path
        );
    }


    private static void Backup(
        string path
    )
    {
        string backup =
            path +
            ".vegetationfixv3.bak";

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
        string path
    )
    {
        string assets =
            Application.dataPath
                .Replace(
                    '\\',
                    '/'
                );

        string normalized =
            path.Replace(
                '\\',
                '/'
            );

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

        return path;
    }
}
#endif
