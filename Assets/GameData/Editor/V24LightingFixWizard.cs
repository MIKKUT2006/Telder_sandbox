
#if UNITY_EDITOR

using System;
using System.IO;

using UnityEditor;
using UnityEngine;


namespace Game.EditorTools
{

    public static class V24LightingFixWizard
    {

        [MenuItem(
            "Tools/Game/Apply V24 Lighting Fix"
        )]
        public static void Apply()
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

                EditorUtility.DisplayDialog(
                    "V24 Lighting Fix",
                    "LightPropagationEngine.cs не найден.",
                    "OK"
                );


                return;

            }


            string source =
                File.ReadAllText(
                    path
                );


            string before =
                source;


            source =
                InsertUsing(
                    source,
                    "using Game.Blocks;",
                    "using Game.World.Furniture;"
                );


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


            bool oldZeroClampStillExists =
                source.Contains(
                    "0,\n                worldHeight - 1"
                )
                ||
                source.Contains(
                    "0,\r\n                worldHeight - 1"
                );


            if (
                source !=
                before
            )
            {

                File.WriteAllText(
                    path,
                    source
                );


                AssetDatabase.Refresh();

            }


            EditorUtility.DisplayDialog(
                "V24 Lighting Fix",
                "Готово.\n\n" +
                "RebuildAfterBlockChanged теперь использует реальные MinY/MaxY загруженных чанков.\n" +
                "Furniture-факелы включены в RGB emissive scan.\n\n" +
                (
                    oldZeroClampStillExists
                        ? "ВНИМАНИЕ: в файле всё ещё найден старый Y=0 clamp. Пришли LightPropagationEngine.cs."
                        : "Старый clamp Y=0 в RebuildAfterBlockChanged удалён."
                ) +
                "\n\nДождись повторной компиляции Unity.",
                "OK"
            );

        }


        // =====================================================
        // NEGATIVE-Y BLOCK UPDATE
        // =====================================================

        private static string GetBlockChangedMethod()
        {

            return
@"        public void RebuildAfterBlockChanged(
            int worldX,
            int worldY,
            int worldHeight
        )
        {
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


            // Do NOT clamp Y to 0.
            //
            // Negative chunk/world coordinates are valid in this
            // world implementation. Rebuild through the complete
            // currently loaded vertical span so a torch at
            // Y = -1, -50, -500... is treated exactly the same
            // as a torch above zero.
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


        // =====================================================
        // FOREGROUND + FURNITURE EMISSIVE BLOCKS
        // =====================================================

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
                    // FOREGROUND EMITTER
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
                    // FURNITURE EMITTER
                    // =========================================
                    //
                    // Furniture is not part of World.GetBlock(),
                    // so without this branch a Furniture torch
                    // could show particles but emit no light.

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
                    "V24: method not found: " +
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


            return
                -1;

        }

    }

}

#endif
