
#if UNITY_EDITOR

using System;
using System.IO;
using System.Text.RegularExpressions;

using UnityEditor;

using UnityEngine;


namespace Game.EditorTools
{

    public static class V22_2CriticalFixWizard
    {

        [MenuItem(
            "Tools/Game/Apply V22.2 Critical Fixes"
        )]
        public static void Apply()
        {

            int worldManager =
                PatchWorldManager();


            int lighting =
                PatchLighting();


            AssetDatabase.Refresh();


            EditorUtility.DisplayDialog(
                "Telder V22.2",
                "Готово.\n\n" +
                "WorldManager patches: " +
                worldManager +
                "\nLighting patches: " +
                lighting +
                "\n\nВАЖНО: дождись повторной компиляции Unity до запуска Play Mode.",
                "OK"
            );

        }


        // =====================================================
        // WORLD MANAGER
        // =====================================================

        private static int PatchWorldManager()
        {

            string path =
                FindScript(
                    "WorldManager"
                );


            if (
                string.IsNullOrWhiteSpace(
                    path
                )
            )
            {

                Debug.LogError(
                    "V22.2: WorldManager.cs not found."
                );


                return 0;

            }


            string source =
                File.ReadAllText(
                    path
                );


            string before =
                source;


            // Content initialization is now owned by GameBootstrap.
            //
            // Calling ContentManager.Initialize directly from
            // WorldManager + GameBootstrap + menu was the source
            // of duplicate game:water registrations.
            source =
                Regex.Replace(
                    source,
                    @"\bContentManager\s*\.\s*Initialize\s*\(\s*\)\s*;",
                    "global::GameBootstrap.EnsureContentInitialized();"
                );


            // Current World already owns its LightPropagationEngine.
            // Avoid creating a second engine in WorldManager.
            source =
                Regex.Replace(
                    source,
                    @"lighting\s*=\s*new\s+LightPropagationEngine\s*\(\s*world\s*\)\s*;",
                    "lighting = world.GetLightEngine();"
                );


            // Make sure World knows the configured height.
            if (
                source.Contains(
                    "world =\n                new World();"
                )
                &&
                !source.Contains(
                    "world.SetWorldHeight("
                )
            )
            {

                source =
                    source.Replace(
                        "world =\n                new World();",
                        "world =\n                new World();\n\n\n            world.SetWorldHeight(\n                settings.WorldHeight\n            );"
                    );

            }
            else if (
                source.Contains(
                    "world = new World();"
                )
                &&
                !source.Contains(
                    "world.SetWorldHeight("
                )
            )
            {

                source =
                    source.Replace(
                        "world = new World();",
                        "world = new World();\n\n            world.SetWorldHeight(settings.WorldHeight);"
                    );

            }


            if (
                source ==
                before
            )
            {

                Debug.Log(
                    "V22.2: WorldManager already patched."
                );


                return 0;

            }


            File.WriteAllText(
                path,
                source
            );


            Debug.Log(
                "V22.2: patched WorldManager: " +
                path
            );


            return 1;

        }


        // =====================================================
        // LIGHT
        // =====================================================

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
                    "V22.2: LightPropagationEngine.cs not found."
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
                    InsertUsing(
                        source,
                        "using Game.Blocks;",
                        "using Game.World.Furniture;"
                    );

            }


            source =
                ReplaceMethod(
                    source,
                    "public void RebuildAfterBlockChanged",
                    GetBlockChangedMethod()
                );


            source =
                ReplaceMethod(
                    source,
                    "private void SeedEmissiveBlocks",
                    GetEmissiveMethod()
                );


            if (
                source ==
                before
            )
            {

                Debug.Log(
                    "V22.2: LightPropagationEngine already patched."
                );


                return 0;

            }


            File.WriteAllText(
                path,
                source
            );


            Debug.Log(
                "V22.2: patched negative-Y/furniture lighting: " +
                path
            );


            return 1;

        }


        private static string GetBlockChangedMethod()
        {

            return
@"        public void RebuildAfterBlockChanged(
            int worldX,
            int worldY,
            int worldHeight
        )
        {
            // Never clamp lighting to Y >= 0.
            // Loaded chunks are the actual source of valid world bounds.
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


            RebuildRegion(
                bounds,
                worldHeight
            );
        }";

        }


        private static string GetEmissiveMethod()
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


                    byte r =
                        0;


                    byte g =
                        0;


                    byte b =
                        0;


                    // =========================================
                    // FOREGROUND
                    // =========================================

                    ushort blockID =
                        world.GetBlock(
                            x,
                            y
                        );


                    if (
                        blockID !=
                        0
                    )
                    {
                        BlockDefinition definition =
                            world.GetBlockDefinition(
                                blockID
                            );


                        if (
                            definition !=
                            null
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


                    // =========================================
                    // FURNITURE
                    // =========================================
                    //
                    // Torch/workbench/decor can live in the
                    // furniture layer. Furniture is non-colliding,
                    // but it can still emit light.

                    if (
                        FurnitureLayerManager.Instance !=
                        null
                        &&
                        FurnitureLayerManager.Instance
                            .TryGetDefinition(
                                x,
                                y,
                                out BlockDefinition
                                    furniture
                            )
                        &&
                        furniture !=
                        null
                    )
                    {
                        r =
                            Max(
                                r,
                                ClampLight(
                                    furniture
                                        .LightEmissionR
                                )
                            );


                        g =
                            Max(
                                g,
                                ClampLight(
                                    furniture
                                        .LightEmissionG
                                )
                            );


                        b =
                            Max(
                                b,
                                ClampLight(
                                    furniture
                                        .LightEmissionB
                                )
                            );
                    }


                    if (
                        r ==
                        0
                        &&
                        g ==
                        0
                        &&
                        b ==
                        0
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


        // =====================================================
        // SOURCE HELPERS
        // =====================================================

        private static string FindScript(
            string className
        )
        {

            string[] guids =
                AssetDatabase.FindAssets(
                    className +
                    " t:Script"
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
                    !File.Exists(
                        path
                    )
                )
                {
                    continue;
                }


                string source =
                    File.ReadAllText(
                        path
                    );


                if (
                    source.Contains(
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


        private static string InsertUsing(
            string source,
            string anchor,
            string newUsing
        )
        {

            if (
                source.Contains(
                    newUsing
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
                index <
                0
            )
            {

                return
                    newUsing +
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
                    newUsing
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
                start <
                0
            )
            {

                Debug.LogError(
                    "V22.2: method not found: " +
                    signatureStart
                );


                return source;

            }


            int lineStart =
                source.LastIndexOf(
                    '\n',
                    start
                );


            lineStart =
                lineStart <
                0
                    ? 0
                    : lineStart +
                      1;


            int openBrace =
                source.IndexOf(
                    '{',
                    start
                );


            if (
                openBrace <
                0
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
                closeBrace <
                0
            )
            {
                return source;
            }


            return
                source.Substring(
                    0,
                    lineStart
                )
                +
                replacement
                +
                source.Substring(
                    closeBrace +
                    1
                );

        }


        private static int FindMatchingBrace(
            string source,
            int openBrace
        )
        {

            int depth =
                0;


            bool lineComment =
                false;


            bool blockComment =
                false;


            bool normalString =
                false;


            bool verbatimString =
                false;


            bool charLiteral =
                false;


            for (
                int i = openBrace;
                i < source.Length;
                i++
            )
            {

                char current =
                    source[i];


                char next =
                    i +
                    1 <
                    source.Length
                        ? source[
                            i +
                            1
                        ]
                        : '\0';


                if (
                    lineComment
                )
                {

                    if (
                        current ==
                        '\n'
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
                        current ==
                        '*'
                        &&
                        next ==
                        '/'
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
                        current ==
                        '\\'
                    )
                    {

                        i++;


                        continue;

                    }


                    if (
                        current ==
                        '"'
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
                        current ==
                        '"'
                        &&
                        next ==
                        '"'
                    )
                    {

                        i++;


                        continue;

                    }


                    if (
                        current ==
                        '"'
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
                        current ==
                        '\\'
                    )
                    {

                        i++;


                        continue;

                    }


                    if (
                        current ==
                        '\''
                    )
                    {
                        charLiteral =
                            false;
                    }


                    continue;

                }


                if (
                    current ==
                    '/'
                    &&
                    next ==
                    '/'
                )
                {

                    lineComment =
                        true;


                    i++;


                    continue;

                }


                if (
                    current ==
                    '/'
                    &&
                    next ==
                    '*'
                )
                {

                    blockComment =
                        true;


                    i++;


                    continue;

                }


                if (
                    current ==
                    '@'
                    &&
                    next ==
                    '"'
                )
                {

                    verbatimString =
                        true;


                    i++;


                    continue;

                }


                if (
                    current ==
                    '"'
                )
                {

                    normalString =
                        true;


                    continue;

                }


                if (
                    current ==
                    '\''
                )
                {

                    charLiteral =
                        true;


                    continue;

                }


                if (
                    current ==
                    '{'
                )
                {
                    depth++;
                }
                else if (
                    current ==
                    '}'
                )
                {

                    depth--;


                    if (
                        depth ==
                        0
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
