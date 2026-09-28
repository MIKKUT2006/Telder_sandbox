#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class TelderPolishFixInstaller
{
    private const string Marker = "TELDER_POLISH_FIX_PACK_V1";

    [MenuItem("Tools/Game/Apply Polish Fix Pack")]
    public static void Apply()
    {
        string gameData = Path.Combine(Application.dataPath, "GameData");

        if (!Directory.Exists(gameData))
        {
            Debug.LogError("POLISH FIX: Assets/GameData not found.");
            return;
        }

        var changed = new List<string>();
        var warnings = new List<string>();

        try
        {
            WriteRuntimeHelpers(gameData, changed);

            PatchChunkData(gameData, changed, warnings);
            PatchChunkLoader(gameData, changed, warnings);
            PatchWorldGenerator(gameData, changed, warnings);
            PatchChunkRenderer(gameData, changed, warnings);
            PatchLighting(gameData, changed, warnings);
            PatchBlockInteraction(gameData, changed, warnings);
            PatchWeaponController(gameData, changed, warnings);
            PatchDebrisAmbient(gameData, changed, warnings);

            AssetDatabase.Refresh();

            Debug.Log(
                "POLISH FIX: completed.\nChanged/created:\n" +
                string.Join("\n", changed.Select(x => "  • " + ToAssetPath(x))) +
                (warnings.Count > 0
                    ? "\n\nWarnings:\n" + string.Join("\n", warnings.Select(x => "  • " + x))
                    : "")
            );

            EditorUtility.DisplayDialog(
                "Telder Polish Fix Pack",
                "Patch applied.\n\n" +
                "Changed/created files: " + changed.Count + "\n" +
                "Warnings: " + warnings.Count + "\n\n" +
                "Unity will recompile scripts now. Backups use .polishfix.bak.",
                "OK"
            );
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog(
                "Telder Polish Fix Pack",
                "Patch stopped because of an exception.\nCheck Console.\n\n" + ex.Message,
                "OK"
            );
        }
    }

    // =========================================================
    // RUNTIME HELPERS
    // =========================================================

    private static void WriteRuntimeHelpers(string gameData, List<string> changed)
    {
        string dir = Path.Combine(gameData, "World", "PolishFixes");
        Directory.CreateDirectory(dir);

        WriteIfDifferent(
            Path.Combine(dir, "GeneratedFurnitureChunkBridge.cs"),
            GeneratedFurnitureBridgeSource(),
            changed
        );

        WriteIfDifferent(
            Path.Combine(dir, "SandPlacementFallRuntime.cs"),
            SandPlacementFallRuntimeSource(),
            changed
        );

        WriteIfDifferent(
            Path.Combine(gameData, "Combat", "ExplicitWeaponAttackGuard.cs"),
            ExplicitWeaponAttackGuardSource(),
            changed
        );
    }

    private static string GeneratedFurnitureBridgeSource()
    {
        return @"using Game.World.Furniture;

namespace Game.World.PolishFixes
{
    public static class GeneratedFurnitureChunkBridge
    {
        public static void Apply(
            Game.World.ChunkData data,
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

            int startX = chunkX * Game.World.Chunk.SizeX;
            int startY = chunkY * Game.World.Chunk.SizeY;

            for (int x = 0; x < Game.World.Chunk.SizeX; x++)
            {
                for (int y = 0; y < Game.World.Chunk.SizeY; y++)
                {
                    string blockId =
                        data.GetFurniture(x, y);

                    if (string.IsNullOrWhiteSpace(blockId))
                        continue;

                    manager.SetGeneratedFurniture(
                        startX + x,
                        startY + y,
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
            if (FurnitureLayerManager.Instance == null)
                return;

            FurnitureLayerManager.Instance
                .UnloadGeneratedFurnitureChunk(
                    chunkX,
                    chunkY
                );
        }
    }
}
";
    }

    private static string SandPlacementFallRuntimeSource()
    {
        return @"using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Game.Content;

namespace Game.World.PolishFixes
{
    public static class SandPlacementFallRuntime
    {
        private static Runner runner;

        private static readonly HashSet<long> active =
            new HashSet<long>();

        public static void NotifyPlaced(
            WorldManager worldManager,
            World world,
            int worldX,
            int worldY,
            ushort blockId
        )
        {
            if (worldManager == null ||
                world == null ||
                blockId == 0 ||
                !IsSand(blockId))
            {
                return;
            }

            if (world.GetBlock(worldX, worldY - 1) != 0)
                return;

            EnsureRunner();

            if (runner == null)
                return;

            long key = Pack(worldX, worldY);

            if (!active.Add(key))
                return;

            runner.StartCoroutine(
                Fall(
                    worldManager,
                    world,
                    worldX,
                    worldY,
                    blockId,
                    key
                )
            );
        }

        private static IEnumerator Fall(
            WorldManager worldManager,
            World world,
            int startX,
            int startY,
            ushort blockId,
            long activeKey
        )
        {
            int x = startX;
            int y = startY;

            // Small delay allows any existing falling-block system
            // to take ownership first. If it already moved the block,
            // this fallback exits without fighting it.
            yield return null;

            while (worldManager != null &&
                   world != null)
            {
                if (world.GetBlock(x, y) != blockId)
                    break;

                int belowY = y - 1;

                if (!world.IsLoaded(x, belowY))
                    break;

                if (world.GetBlock(x, belowY) != 0)
                    break;

                if (!worldManager.SetBlock(x, y, 0))
                    break;

                if (!worldManager.SetBlock(x, belowY, blockId))
                {
                    // Best effort rollback.
                    worldManager.SetBlock(x, y, blockId);
                    break;
                }

                y = belowY;

                yield return new WaitForSeconds(0.035f);
            }

            active.Remove(activeKey);
        }

        private static bool IsSand(ushort blockId)
        {
            try
            {
                string id =
                    BlockIDRegistry
                        .GetContentID(blockId)
                        .ToString();

                if (string.IsNullOrWhiteSpace(id))
                    return false;

                id = id.ToLowerInvariant();

                return
                    id == ""game:sand"" ||
                    id.EndsWith("":sand"") ||
                    id.Contains(""sand_"") ||
                    id.Contains(""_sand"");
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureRunner()
        {
            if (runner != null)
                return;

            GameObject obj =
                new GameObject(
                    ""[Runtime] Sand Placement Fall""
                );

            Object.DontDestroyOnLoad(obj);

            runner =
                obj.AddComponent<Runner>();
        }

        private static long Pack(int x, int y)
        {
            unchecked
            {
                return ((long)x << 32) ^ (uint)y;
            }
        }

        private sealed class Runner :
            MonoBehaviour
        {
        }
    }
}
";
    }

    private static string ExplicitWeaponAttackGuardSource()
    {
        return @"using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Combat
{
    public static class ExplicitWeaponAttackGuard
    {
        public static bool AllowsAttack(
            string itemId,
            WeaponKind kind
        )
        {
            if (kind == WeaponKind.None)
                return false;

            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            string itemsFolder =
                Path.Combine(
                    Application.dataPath,
                    ""GameData"",
                    ""Items""
                );

            if (!Directory.Exists(itemsFolder))
                return false;

            string json =
                TryReadItemJson(
                    itemsFolder,
                    itemId
                );

            if (string.IsNullOrWhiteSpace(json))
                return false;

            string kindName =
                kind.ToString();

            // A weapon attack must be explicitly declared in item JSON.
            // Supported keys cover current/older Telder metadata variants.
            string pattern =
                ""\""(Kind|WeaponKind|WeaponType|AttackType|Type)\""\\s*:\\s*\"""" +
                Regex.Escape(kindName) +
                ""\"""";

            return Regex.IsMatch(
                json,
                pattern,
                RegexOptions.IgnoreCase
            );
        }

        private static string TryReadItemJson(
            string folder,
            string itemId
        )
        {
            string shortId =
                itemId;

            int separator =
                shortId.LastIndexOf(':');

            if (separator >= 0 &&
                separator < shortId.Length - 1)
            {
                shortId =
                    shortId.Substring(
                        separator + 1
                    );
            }

            string direct =
                Path.Combine(
                    folder,
                    shortId + "".json""
                );

            if (File.Exists(direct))
            {
                try
                {
                    return File.ReadAllText(direct);
                }
                catch
                {
                }
            }

            string escapedId =
                Regex.Escape(itemId);

            string idPattern =
                ""\""ID\""\\s*:\\s*\"""" +
                escapedId +
                ""\"""";

            foreach (
                string file
                in Directory.GetFiles(
                    folder,
                    ""*.json"",
                    SearchOption.AllDirectories
                )
            )
            {
                try
                {
                    string json =
                        File.ReadAllText(file);

                    if (
                        Regex.IsMatch(
                            json,
                            idPattern,
                            RegexOptions.IgnoreCase
                        )
                    )
                    {
                        return json;
                    }
                }
                catch
                {
                }
            }

            return null;
        }
    }
}
";
    }

    // =========================================================
    // PATCH: CHUNK DATA / FURNITURE
    // =========================================================

    private static void PatchChunkData(
        string gameData,
        List<string> changed,
        List<string> warnings
    )
    {
        string path = FindFile(gameData, "ChunkData.cs");

        if (path == null)
        {
            warnings.Add("ChunkData.cs not found.");
            return;
        }

        string text = File.ReadAllText(path);

        if (text.Contains("string GetFurniture(") &&
            text.Contains("bool SetFurniture("))
        {
            return;
        }

        string snippet = @"

        // =====================================================
        // PROCEDURAL FURNITURE DATA
        // TELDER_POLISH_FIX_PACK_V1
        // =====================================================

        private readonly string[,] polishFurniture =
            new string[
                Chunk.SizeX,
                Chunk.SizeY
            ];

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

            return polishFurniture[
                localX,
                localY
            ];
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
                localY >= Chunk.SizeY
            )
            {
                return false;
            }

            string normalized =
                string.IsNullOrWhiteSpace(blockId)
                    ? null
                    : blockId;

            if (
                string.Equals(
                    polishFurniture[localX, localY],
                    normalized,
                    StringComparison.Ordinal
                )
            )
            {
                return false;
            }

            polishFurniture[
                localX,
                localY
            ] =
                normalized;

            return true;
        }
";

        if (!text.Contains("using System;"))
            text = "using System;\n" + text;

        text = InsertIntoType(text, "ChunkData", snippet);

        SavePatched(path, text, changed);
    }

    // =========================================================
    // PATCH: CHUNK LOADER
    // =========================================================

    private static void PatchChunkLoader(
        string gameData,
        List<string> changed,
        List<string> warnings
    )
    {
        string path = FindFile(gameData, "ChunkLoader.cs");

        if (path == null)
        {
            warnings.Add("ChunkLoader.cs not found.");
            return;
        }

        string text = File.ReadAllText(path);
        string original = text;

        if (!text.Contains("GeneratedFurnitureChunkBridge.Apply("))
        {
            text = Regex.Replace(
                text,
                @"chunk\.ApplyData\s*\(\s*data\s*\)\s*;",
                @"chunk.ApplyData(
                    data
                );

                // TELDER_POLISH_FIX_PACK_V1
                Game.World.PolishFixes.GeneratedFurnitureChunkBridge.Apply(
                    data,
                    position.x,
                    position.y
                );

                // Newly loaded/generated chunks must get light immediately,
                // not only during the initial WorldManager lighting pass.
                if (
                    world.GetLightEngine() !=
                    null
                )
                {
                    world.GetLightEngine()
                        .RebuildAfterChunkGenerated(
                            position.x,
                            position.y,
                            settings.WorldHeight
                        );
                }",
                RegexOptions.Singleline
            );
        }

        if (!text.Contains("GeneratedFurnitureChunkBridge.Unload("))
        {
            text = Regex.Replace(
                text,
                @"(\s*)renderer\.RemoveChunk\s*\(\s*position\.x\s*,\s*position\.y\s*\)\s*;",
                m => m.Value +
                     "\n\n" +
                     m.Groups[1].Value +
                     "Game.World.PolishFixes.GeneratedFurnitureChunkBridge.Unload(\n" +
                     m.Groups[1].Value + "    position.x,\n" +
                     m.Groups[1].Value + "    position.y\n" +
                     m.Groups[1].Value + ");",
                RegexOptions.Singleline
            );
        }

        if (text == original)
        {
            if (!text.Contains("GeneratedFurnitureChunkBridge"))
                warnings.Add("ChunkLoader.cs found, but expected patch points were not matched.");
            return;
        }

        SavePatched(path, text, changed);
    }

    // =========================================================
    // PATCH: WORLD GENERATION PIPELINE
    // =========================================================

    private static void PatchWorldGenerator(
        string gameData,
        List<string> changed,
        List<string> warnings
    )
    {
        string path = FindFile(gameData, "WorldGenerator.cs");

        if (path == null)
        {
            warnings.Add("WorldGenerator.cs not found.");
            return;
        }

        string text = File.ReadAllText(path);
        string method = ExtractMethod(text, "GenerateChunkData");

        if (method == null)
        {
            warnings.Add("WorldGenerator.GenerateChunkData was not found.");
            return;
        }

        var calls = new List<string>();

        if (!method.Contains("StructureGenerationRuntime"))
        {
            calls.Add(
@"            Game.World.Structures.StructureGenerationRuntime.ApplyToChunk(
                this,
                settings,
                data,
                chunkX,
                chunkY
            );"
            );
        }

        // Discover non-cave vegetation runtimes already present in the project.
        foreach (string runtime in DiscoverVegetationRuntimeTypes(gameData))
        {
            if (!method.Contains(runtime))
            {
                calls.Add(
$@"            {runtime}.ApplyToChunk(
                this,
                settings,
                data,
                chunkX,
                chunkY
            );"
                );
            }
        }

        if (!method.Contains("CaveVegetationGenerationRuntime"))
        {
            string caveFile =
                FindFile(
                    gameData,
                    "CaveVegetationGenerationRuntime.cs"
                );

            if (caveFile != null)
            {
                calls.Add(
@"            Game.World.Vegetation.Caves.CaveVegetationGenerationRuntime.ApplyToChunk(
                this,
                settings,
                data,
                chunkX,
                chunkY
            );"
                );
            }
        }

        if (calls.Count == 0)
            return;

        string insertion =
            "\n\n            // " + Marker + "\n" +
            string.Join("\n\n", calls) +
            "\n";

        string patched =
            InsertBeforeReturnInMethod(
                text,
                "GenerateChunkData",
                "return data;",
                insertion
            );

        if (patched == text)
        {
            warnings.Add("WorldGenerator.cs: could not insert generation stages before return data.");
            return;
        }

        SavePatched(path, patched, changed);
    }

    // =========================================================
    // PATCH: RENDERER AMBIENT
    // =========================================================

    private static void PatchChunkRenderer(
        string gameData,
        List<string> changed,
        List<string> warnings
    )
    {
        string path = FindFile(gameData, "ChunkRenderer.cs");

        if (path == null)
        {
            warnings.Add("ChunkRenderer.cs not found.");
            return;
        }

        string text = File.ReadAllText(path);
        string original = text;

        text = Regex.Replace(
            text,
            @"SetFloat\s*\(\s*""_Ambient""\s*,\s*[0-9]*\.?[0-9]+f\s*\)",
            "SetFloat(\"_Ambient\", 0f)"
        );

        if (text != original)
            SavePatched(path, text, changed);
    }

    // =========================================================
    // PATCH: LIGHTING
    // =========================================================

    private static void PatchLighting(
        string gameData,
        List<string> changed,
        List<string> warnings
    )
    {
        string path = FindFile(gameData, "LightPropagationEngine.cs");

        if (path == null)
        {
            warnings.Add("LightPropagationEngine.cs not found.");
            return;
        }

        string text = File.ReadAllText(path);
        string original = text;

        // Patch the int-bounds rebuild path.
        if (!text.Contains("TELDER_TWO_BLOCK_SUN_INT"))
        {
            text = RegexReplaceFirst(
                text,
                @"int\s+sunlight\s*=\s*MaxLight\s*;",
                "int sunlight = MaxLight;\n                int opaqueDepth = 0; // TELDER_TWO_BLOCK_SUN_INT"
            );

            text = RegexReplaceFirst(
                text,
                @"if\s*\(\s*opacity\s*>=\s*MaxLight\s*\)\s*\{\s*sunlight\s*=\s*0\s*;\s*continue\s*;\s*\}",
@"if (opacity >= MaxLight)
                    {
                        opaqueDepth++;

                        if (opaqueDepth > 2)
                        {
                            sunlight = 0;
                            break;
                        }

                        sunlight =
                            opaqueDepth == 1
                                ? 10
                                : 5;

                        world.SetLight(
                            x,
                            y,
                            (byte)sunlight,
                            0,
                            0,
                            0
                        );

                        queue.Enqueue(
                            new LightPosition(
                                x,
                                y,
                                (byte)sunlight,
                                0,
                                0,
                                0
                            )
                        );

                        continue;
                    }",
                RegexOptions.Singleline
            );

            text = RegexReplaceFirst(
                text,
                @"(\s*)sunlight\s*-=\s*Mathf\.Max\s*\(",
@"$1if (opaqueDepth > 0)
$1{
$1    sunlight = 0;
$1    break;
$1}

$1sunlight -= Mathf.Max("
            );
        }

        // Patch the LightBounds SeedSunlight path if this version contains it.
        if (text.Contains("private void SeedSunlight(") &&
            !text.Contains("TELDER_TWO_BLOCK_SUN_BOUNDS"))
        {
            string replacement =
@"        private void SeedSunlight(
            LightBounds bounds,
            int worldHeight
        )
        {
            int skyTop =
                worldHeight - 1;

            for (
                int x = bounds.MinX;
                x <= bounds.MaxX;
                x++
            )
            {
                byte directSun =
                    MaxLight;

                int opaqueDepth =
                    0; // TELDER_TWO_BLOCK_SUN_BOUNDS

                for (
                    int y = skyTop;
                    y >= bounds.MinY;
                    y--
                )
                {
                    if (directSun == 0)
                        break;

                    ushort blockID =
                        world.GetBlock(x, y);

                    int opacity =
                        GetOpacity(blockID);

                    bool insideRegion =
                        y <= bounds.MaxY;

                    if (blockID == 0)
                    {
                        // Once sunlight entered solid terrain, a cave/air
                        // cell below it must be completely dark.
                        if (opaqueDepth > 0)
                            break;

                        if (insideRegion &&
                            world.IsLoaded(x, y))
                        {
                            SetSunlightSource(
                                x,
                                y,
                                directSun,
                                true
                            );
                        }

                        continue;
                    }

                    if (opacity >= MaxLight)
                    {
                        opaqueDepth++;

                        if (opaqueDepth > 2)
                            break;

                        byte penetration =
                            opaqueDepth == 1
                                ? (byte)10
                                : (byte)5;

                        if (insideRegion &&
                            world.IsLoaded(x, y))
                        {
                            SetSunlightSource(
                                x,
                                y,
                                penetration,
                                false
                            );
                        }

                        continue;
                    }

                    // Do not let light reappear below the two solid
                    // surface cells through semi-transparent material.
                    if (opaqueDepth > 0)
                        break;

                    directSun =
                        SubtractLight(
                            directSun,
                            opacity
                        );

                    if (insideRegion &&
                        world.IsLoaded(x, y))
                    {
                        SetSunlightSource(
                            x,
                            y,
                            directSun,
                            true
                        );
                    }
                }
            }
        }
";

            string patched =
                ReplaceMethod(
                    text,
                    "SeedSunlight",
                    replacement
                );

            if (patched != text)
                text = patched;
            else
                warnings.Add("LightPropagationEngine.SeedSunlight could not be replaced.");
        }

        if (text != original)
            SavePatched(path, text, changed);
    }

    // =========================================================
    // PATCH: BLOCK INTERACTION / VFX / SAND
    // =========================================================

    private static void PatchBlockInteraction(
        string gameData,
        List<string> changed,
        List<string> warnings
    )
    {
        string path = FindFile(gameData, "BlockInteraction.cs");

        if (path == null)
        {
            warnings.Add("BlockInteraction.cs not found.");
            return;
        }

        string text = File.ReadAllText(path);
        string original = text;

        if (!text.Contains("BlockBreakDebrisSystem.Emit"))
        {
            text = ReplaceFirst(
                text,
@"                SpawnBlockDrop(
                    foregroundID,",
@"                Game.World.Effects.BlockBreakDebrisSystem.Emit(
                    world,
                    x,
                    y,
                    foregroundID,
                    false
                );

                SpawnBlockDrop(
                    foregroundID,"
            );

            text = ReplaceFirst(
                text,
@"            SpawnBlockDrop(
                backgroundID,",
@"            Game.World.Effects.BlockBreakDebrisSystem.Emit(
                world,
                x,
                y,
                backgroundID,
                true
            );

            SpawnBlockDrop(
                backgroundID,"
            );
        }

        if (!text.Contains("SandPlacementFallRuntime.NotifyPlaced"))
        {
            string needle =
@"        if (
            !placed
        )
        {

            return false;

        }";

            string replacement =
needle +
@"

        // TELDER_POLISH_FIX_PACK_V1
        Game.World.PolishFixes.SandPlacementFallRuntime.NotifyPlaced(
            worldManager,
            world,
            x,
            y,
            selectedBlockID
        );";

            text = text.Replace(
                needle,
                replacement
            );
        }

        if (text == original)
        {
            warnings.Add("BlockInteraction.cs found, but no matching patch points changed.");
            return;
        }

        SavePatched(path, text, changed);
    }

    // =========================================================
    // PATCH: EXPLICIT WEAPON ATTACK
    // =========================================================

    private static void PatchWeaponController(
        string gameData,
        List<string> changed,
        List<string> warnings
    )
    {
        string path = FindFile(gameData, "PlayerWeaponController.cs");

        if (path == null)
        {
            warnings.Add("PlayerWeaponController.cs not found.");
            return;
        }

        string text = File.ReadAllText(path);

        if (text.Contains("ExplicitWeaponAttackGuard.AllowsAttack"))
            return;

        string needle =
@"            if (selectedKind ==
                WeaponKind.None)
            {
                return;
            }";

        string replacement =
needle +
@"

            // Only items that explicitly declare a weapon kind in JSON
            // are allowed to attack. Missing metadata is NOT Sword.
            if (
                !ExplicitWeaponAttackGuard.AllowsAttack(
                    itemId,
                    selectedKind
                )
            )
            {
                return;
            }";

        if (!text.Contains(needle))
        {
            warnings.Add("PlayerWeaponController.cs: selectedKind guard was not matched.");
            return;
        }

        text = text.Replace(
            needle,
            replacement
        );

        SavePatched(path, text, changed);
    }

    // =========================================================
    // PATCH: DEBRIS FULL DARKNESS
    // =========================================================

    private static void PatchDebrisAmbient(
        string gameData,
        List<string> changed,
        List<string> warnings
    )
    {
        string path = FindFile(gameData, "BlockBreakDebrisSystem.cs");

        if (path == null)
            return;

        string text = File.ReadAllText(path);
        string original = text;

        text = Regex.Replace(
            text,
            @"const\s+float\s+ambient\s*=\s*[0-9]*\.?[0-9]+f\s*;",
            "const float ambient = 0f;"
        );

        if (text != original)
            SavePatched(path, text, changed);
    }

    // =========================================================
    // DISCOVERY
    // =========================================================

    private static IEnumerable<string> DiscoverVegetationRuntimeTypes(
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

            string text;

            try
            {
                text = File.ReadAllText(path);
            }
            catch
            {
                continue;
            }

            if (!text.Contains("ApplyToChunk("))
                continue;

            Match ns =
                Regex.Match(
                    text,
                    @"namespace\s+([A-Za-z0-9_\.]+)"
                );

            Match type =
                Regex.Match(
                    text,
                    @"(?:public\s+)?(?:static\s+)?class\s+([A-Za-z0-9_]+)"
                );

            if (!ns.Success || !type.Success)
                continue;

            string full =
                ns.Groups[1].Value +
                "." +
                type.Groups[1].Value;

            if (
                full.IndexOf(
                    ".Caves.",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
            )
            {
                continue;
            }

            result.Add(full);
        }

        return result;
    }

    // =========================================================
    // FILE / SOURCE HELPERS
    // =========================================================

    private static string FindFile(
        string root,
        string fileName
    )
    {
        return Directory
            .GetFiles(
                root,
                fileName,
                SearchOption.AllDirectories
            )
            .FirstOrDefault(
                x =>
                    !x.EndsWith(
                        ".polishfix.bak",
                        StringComparison.OrdinalIgnoreCase
                    )
            );
    }

    private static void SavePatched(
        string path,
        string text,
        List<string> changed
    )
    {
        Backup(path);

        File.WriteAllText(
            path,
            text,
            new UTF8Encoding(false)
        );

        changed.Add(path);
    }

    private static void Backup(string path)
    {
        string backup =
            path +
            ".polishfix.bak";

        if (!File.Exists(backup))
        {
            File.Copy(
                path,
                backup,
                false
            );
        }
    }

    private static void WriteIfDifferent(
        string path,
        string content,
        List<string> changed
    )
    {
        Directory.CreateDirectory(
            Path.GetDirectoryName(path)
        );

        if (
            File.Exists(path) &&
            File.ReadAllText(path) ==
            content
        )
        {
            return;
        }

        if (File.Exists(path))
            Backup(path);

        File.WriteAllText(
            path,
            content,
            new UTF8Encoding(false)
        );

        changed.Add(path);
    }

    private static string ReplaceFirst(
        string text,
        string oldValue,
        string newValue
    )
    {
        int index =
            text.IndexOf(
                oldValue,
                StringComparison.Ordinal
            );

        if (index < 0)
            return text;

        return
            text.Substring(0, index) +
            newValue +
            text.Substring(
                index + oldValue.Length
            );
    }

    private static string ToAssetPath(
        string absolute
    )
    {
        string data =
            Application.dataPath
                .Replace('\\', '/');

        string normalized =
            absolute.Replace('\\', '/');

        if (
            normalized.StartsWith(
                data,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return
                "Assets" +
                normalized.Substring(
                    data.Length
                );
        }

        return absolute;
    }

    private static string InsertIntoType(
        string text,
        string typeName,
        string snippet
    )
    {
        Match match =
            Regex.Match(
                text,
                @"\bclass\s+" +
                Regex.Escape(typeName) +
                @"\b"
            );

        if (!match.Success)
            throw new InvalidOperationException(
                "Type not found: " + typeName
            );

        int open =
            text.IndexOf(
                '{',
                match.Index + match.Length
            );

        if (open < 0)
            throw new InvalidOperationException(
                "Type brace not found: " + typeName
            );

        int close =
            FindMatchingBrace(
                text,
                open
            );

        if (close < 0)
            throw new InvalidOperationException(
                "Type closing brace not found: " + typeName
            );

        return
            text.Insert(
                close,
                snippet
            );
    }

    private static string ExtractMethod(
        string text,
        string methodName
    )
    {
        if (!TryFindMethodBounds(
            text,
            methodName,
            out int start,
            out int end))
        {
            return null;
        }

        return text.Substring(
            start,
            end - start + 1
        );
    }

    private static string InsertBeforeReturnInMethod(
        string text,
        string methodName,
        string returnToken,
        string insertion
    )
    {
        if (!TryFindMethodBounds(
            text,
            methodName,
            out int start,
            out int end))
        {
            return text;
        }

        int returnIndex =
            text.LastIndexOf(
                returnToken,
                end,
                end - start + 1,
                StringComparison.Ordinal
            );

        if (returnIndex < start)
            return text;

        return
            text.Insert(
                returnIndex,
                insertion
            );
    }

    private static string ReplaceMethod(
        string text,
        string methodName,
        string replacement
    )
    {
        if (!TryFindMethodBounds(
            text,
            methodName,
            out int start,
            out int end))
        {
            return text;
        }

        return
            text.Substring(0, start) +
            replacement +
            text.Substring(end + 1);
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
                @"(?:static\s+)?[A-Za-z0-9_\.<>\[\],]+\s+" +
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
            lineStart >= 0
                ? lineStart + 1
                : 0;

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

    private static string RegexReplaceFirst(
        string input,
        string pattern,
        string replacement,
        RegexOptions options = RegexOptions.None
    )
    {
        Regex regex =
            new Regex(
                pattern,
                options
            );

        return regex.Replace(
            input,
            replacement,
            1
        );
    }

    private static int FindMatchingBrace(
        string text,
        int openIndex
    )
    {
        int depth = 0;

        bool lineComment = false;
        bool blockComment = false;
        bool str = false;
        bool chr = false;
        bool verbatim = false;
        bool escape = false;

        for (
            int i = openIndex;
            i < text.Length;
            i++
        )
        {
            char c = text[i];
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
                if (c == '*' && n == '/')
                {
                    blockComment = false;
                    i++;
                }

                continue;
            }

            if (str)
            {
                if (verbatim)
                {
                    if (c == '"' && n == '"')
                    {
                        i++;
                        continue;
                    }

                    if (c == '"')
                    {
                        str = false;
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
                    str = false;

                continue;
            }

            if (chr)
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
                    chr = false;

                continue;
            }

            if (c == '/' && n == '/')
            {
                lineComment = true;
                i++;
                continue;
            }

            if (c == '/' && n == '*')
            {
                blockComment = true;
                i++;
                continue;
            }

            if (c == '@' && n == '"')
            {
                str = true;
                verbatim = true;
                i++;
                continue;
            }

            if (c == '"')
            {
                str = true;
                continue;
            }

            if (c == '\'')
            {
                chr = true;
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
