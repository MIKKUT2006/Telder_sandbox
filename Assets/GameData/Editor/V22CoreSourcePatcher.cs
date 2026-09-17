#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    public static class V22CoreSourcePatcher
    {
        private const string ContentMarker =
            "_telderContentInitialized";

        [MenuItem("Tools/Game/Apply V22.1 Emergency Fixes")]
        public static void Apply()
        {
            int content =
                PatchContentManager();

            int light =
                PatchLighting();

            int renderer =
                PatchRenderer();

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Telder V22.1",
                "ContentManager patches: " + content +
                "\nLighting patches: " + light +
                "\nRenderer patches: " + renderer +
                "\n\nДождись повторной компиляции Unity и только потом запускай Play Mode.",
                "OK"
            );
        }


        // =========================================================
        // CONTENT INITIALIZATION
        // =========================================================

        private static int PatchContentManager()
        {
            string path =
                FindScript(
                    "ContentManager"
                );

            if (
                string.IsNullOrWhiteSpace(
                    path
                )
            )
            {
                Debug.LogError(
                    "V22.1: ContentManager.cs not found."
                );

                return 0;
            }

            string source =
                File.ReadAllText(
                    path
                );

            if (
                source.Contains(
                    ContentMarker
                )
            )
            {
                return 0;
            }

            int methodStart =
                source.IndexOf(
                    "public static void Initialize",
                    StringComparison.Ordinal
                );

            if (
                methodStart < 0
            )
            {
                Debug.LogError(
                    "V22.1: public static void Initialize() not found in ContentManager."
                );

                return 0;
            }

            int openBrace =
                source.IndexOf(
                    '{',
                    methodStart
                );

            if (
                openBrace < 0
            )
            {
                return 0;
            }

            int closeBrace =
                FindMatchingBrace(
                    source,
                    openBrace
                );

            if (
                closeBrace < 0
            )
            {
                Debug.LogError(
                    "V22.1: could not parse ContentManager.Initialize()."
                );

                return 0;
            }

            // Put the guard fields immediately before Initialize().
            string fields =
                "        // TELDER V22.1: Content loading must be idempotent.\\n" +
                "        private static bool _telderContentInitialized;\\n" +
                "        private static bool _telderContentInitializing;\\n\\n";

            source =
                source.Insert(
                    methodStart,
                    fields
                );

            // The insert changed positions.
            methodStart +=
                fields.Length;

            openBrace =
                source.IndexOf(
                    '{',
                    methodStart
                );

            closeBrace =
                FindMatchingBrace(
                    source,
                    openBrace
                );

            string originalBody =
                source.Substring(
                    openBrace + 1,
                    closeBrace - openBrace - 1
                );

            string wrappedBody =
                @"{
            if (
                _telderContentInitialized ||
                _telderContentInitializing
            )
            {
                return;
            }

            _telderContentInitializing =
                true;

            try
            {" +
                originalBody +
                @"

                _telderContentInitialized =
                    true;
            }
            catch
            {
                // Never leave the manager permanently locked after
                // a failed initialization attempt.
                _telderContentInitialized =
                    false;

                throw;
            }
            finally
            {
                _telderContentInitializing =
                    false;
            }
        }";

            source =
                source.Substring(
                    0,
                    openBrace
                ) +
                wrappedBody +
                source.Substring(
                    closeBrace + 1
                );

            File.WriteAllText(
                path,
                source
            );

            Debug.Log(
                "V22.1: ContentManager.Initialize is now idempotent: " +
                path
            );

            return 1;
        }


        // =========================================================
        // LIGHTING
        // =========================================================

        private static int PatchLighting()
        {
            string path =
                FindScript(
                    "LightPropagationEngine"
                );

            if (
                string.IsNullOrWhiteSpace(
                    path
                )
            )
            {
                Debug.LogError(
                    "V22.1: LightPropagationEngine.cs not found."
                );

                return 0;
            }

            string source =
                File.ReadAllText(
                    path
                );

            string before =
                source;

            if (
                !source.Contains(
                    "using Game.World.Furniture;"
                )
            )
            {
                source =
                    InsertUsingAfter(
                        source,
                        "using Game.Blocks;",
                        "using Game.World.Furniture;"
                    );
            }

            source =
                ReplaceMethod(
                    source,
                    "public void RebuildAfterBlockChanged",
                    GetRebuildAfterBlockChangedMethod()
                );

            source =
                ReplaceMethod(
                    source,
                    "private void SeedEmissiveBlocks",
                    GetSeedEmissiveBlocksMethod()
                );

            if (
                source ==
                before
            )
            {
                Debug.LogWarning(
                    "V22.1: lighting source was already patched or expected methods were not found."
                );

                return 0;
            }

            File.WriteAllText(
                path,
                source
            );

            Debug.Log(
                "V22.1: Lighting patched for negative Y and Furniture emitters: " +
                path
            );

            return 1;
        }


        private static string
            GetRebuildAfterBlockChangedMethod()
        {
            return
@"        public void RebuildAfterBlockChanged(
            int worldX,
            int worldY,
            int worldHeight
        )
        {
            // IMPORTANT:
            // World coordinates are not clamped to Y >= 0.
            // Deep chunks can have negative chunk/world Y.
            //
            // Always rebuild through the real bounds of the loaded world.
            if (
                !TryGetLoadedBounds(
                    out LightBounds loadedBounds
                )
            )
            {
                return;
            }

            int minX =
                Mathf.Max(
                    loadedBounds.MinX,
                    worldX -
                    LightSpreadRadius
                );

            int maxX =
                Mathf.Min(
                    loadedBounds.MaxX,
                    worldX +
                    LightSpreadRadius
                );

            if (
                minX >
                maxX
            )
            {
                return;
            }

            LightBounds bounds =
                new LightBounds(
                    minX,
                    loadedBounds.MinY,
                    maxX,
                    loadedBounds.MaxY
                );

            // Use the modern bounds-based rebuild path.
            // It preserves boundary light and scans emitters at negative Y.
            RebuildRegion(
                bounds,
                worldHeight
            );
        }";
        }


        private static string
            GetSeedEmissiveBlocksMethod()
        {
            return
@"        private void SeedEmissiveBlocks(
            LightBounds bounds
        )
        {
            for (
                int x = bounds.MinX;
                x <= bounds.MaxX;
                x++
            )
            {
                for (
                    int y = bounds.MinY;
                    y <= bounds.MaxY;
                    y++
                )
                {
                    if (
                        !world.IsLoaded(
                            x,
                            y
                        )
                    )
                    {
                        continue;
                    }

                    byte r = 0;
                    byte g = 0;
                    byte b = 0;


                    // =============================================
                    // FOREGROUND EMITTER
                    // =============================================

                    ushort blockID =
                        world.GetBlock(
                            x,
                            y
                        );

                    if (
                        blockID != 0
                    )
                    {
                        BlockDefinition definition =
                            world.GetBlockDefinition(
                                blockID
                            );

                        if (
                            definition != null
                        )
                        {
                            r =
                                ClampLight(
                                    definition
                                        .LightEmissionR
                                );

                            g =
                                ClampLight(
                                    definition
                                        .LightEmissionG
                                );

                            b =
                                ClampLight(
                                    definition
                                        .LightEmissionB
                                );
                        }
                    }


                    // =============================================
                    // FURNITURE EMITTER
                    // =============================================
                    //
                    // Furniture does not replace foreground light.
                    // If both layers emit, keep the strongest channel.

                    if (
                        FurnitureLayerManager.Instance !=
                        null
                        &&
                        FurnitureLayerManager.Instance
                            .TryGetDefinition(
                                x,
                                y,
                                out BlockDefinition
                                    furnitureDefinition
                            )
                        &&
                        furnitureDefinition !=
                        null
                    )
                    {
                        r =
                            Max(
                                r,
                                ClampLight(
                                    furnitureDefinition
                                        .LightEmissionR
                                )
                            );

                        g =
                            Max(
                                g,
                                ClampLight(
                                    furnitureDefinition
                                        .LightEmissionG
                                )
                            );

                        b =
                            Max(
                                b,
                                ClampLight(
                                    furnitureDefinition
                                        .LightEmissionB
                                )
                            );
                    }


                    if (
                        r == 0 &&
                        g == 0 &&
                        b == 0
                    )
                    {
                        continue;
                    }


                    LightNode existing =
                        world.GetLight(
                            x,
                            y
                        );

                    LightNode result =
                        new LightNode(
                            existing.Sun,

                            Max(
                                existing.R,
                                r
                            ),

                            Max(
                                existing.G,
                                g
                            ),

                            Max(
                                existing.B,
                                b
                            )
                        );


                    world.SetLight(
                        x,
                        y,
                        result.Sun,
                        result.R,
                        result.G,
                        result.B
                    );


                    propagationQueue.Enqueue(
                        new LightPoint(
                            x,
                            y
                        )
                    );
                }
            }
        }";
        }


        // =========================================================
        // RENDERER
        // =========================================================

        private static int PatchRenderer()
        {
            string path =
                FindScript(
                    "ChunkRenderer"
                );

            if (
                string.IsNullOrWhiteSpace(
                    path
                )
            )
            {
                return 0;
            }

            string source =
                File.ReadAllText(
                    path
                );

            string before =
                source;

            source =
                source.Replace(
                    "foregroundRenderer.sortingOrder =\n                1;",
                    "foregroundRenderer.sortingOrder =\n                2;"
                );

            source =
                source.Replace(
                    "\"_LayerBrightness\",\n                0.93f",
                    "\"_LayerBrightness\",\n                0.82f"
                );

            source =
                source.Replace(
                    "\"_LayerBrightness\",\n                0.68f",
                    "\"_LayerBrightness\",\n                0.82f"
                );

            if (
                source ==
                before
            )
            {
                return 0;
            }

            File.WriteAllText(
                path,
                source
            );

            return 1;
        }


        // =========================================================
        // SOURCE HELPERS
        // =========================================================

        private static string FindScript(
            string className
        )
        {
            string[] guids =
                AssetDatabase.FindAssets(
                    className +
                    " t:Script"
                );

            foreach (
                string guid
                in guids
            )
            {
                string path =
                    AssetDatabase
                        .GUIDToAssetPath(
                            guid
                        );

                if (
                    !File.Exists(
                        path
                    )
                )
                {
                    continue;
                }

                string text =
                    File.ReadAllText(
                        path
                    );

                if (
                    text.Contains(
                        "class " +
                        className
                    )
                )
                {
                    return path;
                }
            }

            return null;
        }


        private static string InsertUsingAfter(
            string source,
            string anchor,
            string usingLine
        )
        {
            if (
                source.Contains(
                    usingLine
                )
            )
            {
                return source;
            }

            int index =
                source.IndexOf(
                    anchor,
                    StringComparison.Ordinal
                );

            if (
                index < 0
            )
            {
                return
                    usingLine +
                    "\n" +
                    source;
            }

            int end =
                index +
                anchor.Length;

            return
                source.Insert(
                    end,
                    "\n" +
                    usingLine
                );
        }


        private static string ReplaceMethod(
            string source,
            string signatureStart,
            string replacement
        )
        {
            int start =
                source.IndexOf(
                    signatureStart,
                    StringComparison.Ordinal
                );

            if (
                start < 0
            )
            {
                Debug.LogError(
                    "V22.1: method not found: " +
                    signatureStart
                );

                return source;
            }

            // Include indentation before the signature so the replacement
            // cannot leave doubled leading spaces.
            int lineStart =
                source.LastIndexOf(
                    '\n',
                    start
                );

            lineStart =
                lineStart < 0
                    ? 0
                    : lineStart + 1;

            int openBrace =
                source.IndexOf(
                    '{',
                    start
                );

            if (
                openBrace < 0
            )
            {
                return source;
            }

            int closeBrace =
                FindMatchingBrace(
                    source,
                    openBrace
                );

            if (
                closeBrace < 0
            )
            {
                return source;
            }

            return
                source.Substring(
                    0,
                    lineStart
                ) +
                replacement +
                source.Substring(
                    closeBrace + 1
                );
        }


        // Brace matcher that ignores comments and C# string/char literals.
        private static int FindMatchingBrace(
            string source,
            int openBrace
        )
        {
            int depth = 0;

            bool lineComment = false;
            bool blockComment = false;
            bool normalString = false;
            bool verbatimString = false;
            bool charLiteral = false;

            for (
                int i = openBrace;
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

                if (
                    lineComment
                )
                {
                    if (
                        c == '\n'
                    )
                    {
                        lineComment =
                            false;
                    }

                    continue;
                }

                if (
                    blockComment
                )
                {
                    if (
                        c == '*' &&
                        next == '/'
                    )
                    {
                        blockComment =
                            false;

                        i++;
                    }

                    continue;
                }

                if (
                    normalString
                )
                {
                    if (
                        c == '\\'
                    )
                    {
                        i++;
                        continue;
                    }

                    if (
                        c == '"'
                    )
                    {
                        normalString =
                            false;
                    }

                    continue;
                }

                if (
                    verbatimString
                )
                {
                    if (
                        c == '"' &&
                        next == '"'
                    )
                    {
                        i++;
                        continue;
                    }

                    if (
                        c == '"'
                    )
                    {
                        verbatimString =
                            false;
                    }

                    continue;
                }

                if (
                    charLiteral
                )
                {
                    if (
                        c == '\\'
                    )
                    {
                        i++;
                        continue;
                    }

                    if (
                        c == '\''
                    )
                    {
                        charLiteral =
                            false;
                    }

                    continue;
                }

                if (
                    c == '/' &&
                    next == '/'
                )
                {
                    lineComment =
                        true;

                    i++;
                    continue;
                }

                if (
                    c == '/' &&
                    next == '*'
                )
                {
                    blockComment =
                        true;

                    i++;
                    continue;
                }

                if (
                    c == '@' &&
                    next == '"'
                )
                {
                    verbatimString =
                        true;

                    i++;
                    continue;
                }

                if (
                    c == '"'
                )
                {
                    normalString =
                        true;

                    continue;
                }

                if (
                    c == '\''
                )
                {
                    charLiteral =
                        true;

                    continue;
                }

                if (
                    c == '{'
                )
                {
                    depth++;
                    continue;
                }

                if (
                    c == '}'
                )
                {
                    depth--;

                    if (
                        depth == 0
                    )
                    {
                        return i;
                    }
                }
            }

            return -1;
        }
    }
}
#endif
