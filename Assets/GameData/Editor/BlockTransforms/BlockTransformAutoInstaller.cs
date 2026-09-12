#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Game.BlockTransforms.Editor
{
    public static class BlockTransformAutoInstaller
    {
        private const string GameplayMarker = "// [BT-AUTO-GAMEPLAY]";
        private const string RenderMarker = "// [BT-AUTO-RENDER]";
        private const string WorldMarker = "// [BT-AUTO-WORLD]";
        private const string DefinitionMarker = "// [BT-AUTO-COLLISION-FIELDS]";
        private const string PlayerMarker = "// [BT-AUTO-PLAYER-COLLISION]";
        private const string ChunkMarker = "// [BT-AUTO-CHUNK-COLLISION]";
        private const string StructureMarker = "// [BT-AUTO-STRUCTURE-DATA]";

        private static string backupRoot;

        [MenuItem("Tools/Game/Apply Block Rotation System", priority = 220)]
        public static void Apply()
        {
            backupRoot = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                "BlockTransform_Backups",
                DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            Directory.CreateDirectory(backupRoot);

            List<string> report = new List<string>();

            PatchClass("BlockInteraction", PatchBlockInteraction, report);
            PatchClass("ChunkRenderer", PatchChunkRenderer, report);
            PatchClass("WorldManager", PatchWorldManager, report);
            PatchClass("BlockDefinition", PatchBlockDefinition, report);
            PatchClass("PlayerCollision", PatchPlayerCollision, report);
            PatchClass("ChunkCollision", PatchChunkCollision, report);
            PatchStructureData(report);

            AssetDatabase.Refresh();

            StringBuilder text = new StringBuilder();
            text.AppendLine("Block Rotation System: source patch finished.");
            text.AppendLine();

            for (int i = 0; i < report.Count; i++)
                text.AppendLine(report[i]);

            text.AppendLine();
            text.AppendLine("Backup: " + backupRoot);
            text.AppendLine("После второй компиляции: Play Mode -> Z.");

            Debug.Log(text.ToString());
            EditorUtility.DisplayDialog("Block Rotation System", text.ToString(), "OK");
        }

        [MenuItem("Tools/Game/Block Rotation/Check Installation", priority = 221)]
        public static void CheckInstallation()
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("Block Rotation diagnostic");
            text.AppendLine();

            AppendStatus(text, "BlockInteraction", GameplayMarker);
            AppendStatus(text, "ChunkRenderer", RenderMarker);
            AppendStatus(text, "WorldManager", WorldMarker);
            AppendStatus(text, "BlockDefinition", DefinitionMarker);
            AppendStatus(text, "PlayerCollision", PlayerMarker);
            AppendStatus(text, "ChunkCollision", ChunkMarker);

            string structureController = FindSourceByClass("StructureEditorController");
            text.AppendLine("StructureEditorController: " +
                (string.IsNullOrEmpty(structureController) ? "not found" : structureController));

            Debug.Log(text.ToString());
            EditorUtility.DisplayDialog("Block Rotation Diagnostic", text.ToString(), "OK");
        }

        private delegate PatchResult PatchDelegate(string path, string source);

        private static void PatchClass(
            string className,
            PatchDelegate patch,
            List<string> report)
        {
            string path = FindSourceByClass(className);

            if (string.IsNullOrEmpty(path))
            {
                report.Add("⚠ " + className + ": source not found");
                return;
            }

            string source = File.ReadAllText(path);
            PatchResult result = patch(path, source);

            if (!result.Changed)
            {
                report.Add((result.Success ? "✓ " : "⚠ ") +
                    className + ": " + result.Message);
                return;
            }

            Backup(path);
            File.WriteAllText(path, result.Source, new UTF8Encoding(false));
            report.Add("✓ " + className + ": " + result.Message);
        }

        private static PatchResult PatchBlockInteraction(string path, string source)
        {
            if (source.Contains(GameplayMarker))
                return PatchResult.Ok(source, false, "already installed");

            MethodSpan update = FindMethod(source, "private void Update");
            if (!update.Valid)
                return PatchResult.Fail(source, "Update() not found");

            int mouse = source.IndexOf(
                "Input.GetMouseButton",
                update.BodyStart,
                update.BodyEnd - update.BodyStart,
                StringComparison.Ordinal);

            if (mouse < 0)
                return PatchResult.Fail(source, "mouse input not found in Update()");

            int insert = LineStart(source, mouse);

            // Current Telder formatting puts Input.GetMouseButton on the line
            // directly after an outer `if (`. Insert before that outer if,
            // otherwise the source would be patched inside its condition.
            int previousLineEnd = insert - 1;
            if (previousLineEnd >= update.BodyStart)
            {
                int previousLineStart = LineStart(source, previousLineEnd);
                string previousLine = source.Substring(
                    previousLineStart,
                    previousLineEnd - previousLineStart + 1).Trim();

                if (previousLine == "if (" || previousLine == "if(")
                    insert = previousLineStart;
            }

            string indent = GetIndent(source, insert);

            bool hasBreakTimer = source.Contains("nextBreakTime");
            bool hasPlaceTimer = source.Contains("nextPlaceTime");

            StringBuilder code = new StringBuilder();
            code.AppendLine(indent + GameplayMarker);
            code.AppendLine(indent + "if (Game.BlockTransforms.BlockTransformGameplayController.UpdateAndConsume(");
            code.AppendLine(indent + "        worldManager,");
            code.AppendLine(indent + "        world,");
            code.AppendLine(indent + "        playerCamera,");
            code.AppendLine(indent + "        transform,");
            code.AppendLine(indent + "        interactionDistance))");
            code.AppendLine(indent + "{");
            if (hasBreakTimer)
                code.AppendLine(indent + "    nextBreakTime = 0f;");
            if (hasPlaceTimer)
                code.AppendLine(indent + "    nextPlaceTime = 0f;");
            code.AppendLine(indent + "    return;");
            code.AppendLine(indent + "}");
            code.AppendLine();

            source = source.Insert(insert, code.ToString());
            return PatchResult.Ok(source, true, "Z mode hook inserted");
        }

        private static PatchResult PatchChunkRenderer(string path, string source)
        {
            if (source.Contains(RenderMarker))
                return PatchResult.Ok(source, false, "already installed");

            int patched = 0;

            MethodSpan drawBlocks = FindMethod(source, "private void DrawBlocks");
            if (drawBlocks.Valid)
            {
                int fg = source.IndexOf(
                    "data.ForegroundTexture",
                    drawBlocks.BodyStart,
                    drawBlocks.BodyEnd - drawBlocks.BodyStart,
                    StringComparison.Ordinal);

                if (fg >= 0)
                {
                    int call = source.LastIndexOf(
                        "BlockRenderer.DrawBlock",
                        fg,
                        fg - drawBlocks.BodyStart + 1,
                        StringComparison.Ordinal);

                    if (call >= drawBlocks.BodyStart)
                    {
                        int semicolon = FindStatementSemicolon(source, call, drawBlocks.BodyEnd);

                        if (semicolon >= 0)
                        {
                            string indent = GetIndent(source, call);

                            string code =
                                Environment.NewLine +
                                indent + RenderMarker + " WHOLE_CHUNK" + Environment.NewLine +
                                indent + "Game.BlockTransforms.BlockTransformRenderBridge.ApplyToCell(" + Environment.NewLine +
                                indent + "    data.ForegroundTexture," + Environment.NewLine +
                                indent + "    x," + Environment.NewLine +
                                indent + "    y," + Environment.NewLine +
                                indent + "    BlockRenderer.BlockPixelSize," + Environment.NewLine +
                                indent + "    chunk.X * Chunk.SizeX + x," + Environment.NewLine +
                                indent + "    chunk.Y * Chunk.SizeY + y);" + Environment.NewLine;

                            source = source.Insert(semicolon + 1, code);
                            patched++;
                        }
                    }
                }
            }

            MethodSpan updateBlock = FindMethod(source, "public void UpdateBlock");
            if (updateBlock.Valid)
            {
                int fg = source.IndexOf(
                    "data.ForegroundTexture",
                    updateBlock.BodyStart,
                    updateBlock.BodyEnd - updateBlock.BodyStart,
                    StringComparison.Ordinal);

                if (fg >= 0)
                {
                    int call = source.LastIndexOf(
                        "BlockRenderer.DrawBlock",
                        fg,
                        fg - updateBlock.BodyStart + 1,
                        StringComparison.Ordinal);

                    if (call >= updateBlock.BodyStart)
                    {
                        int semicolon = FindStatementSemicolon(source, call, updateBlock.BodyEnd);

                        if (semicolon >= 0)
                        {
                            string indent = GetIndent(source, call);

                            string code =
                                Environment.NewLine +
                                indent + RenderMarker + " SINGLE_BLOCK" + Environment.NewLine +
                                indent + "Game.BlockTransforms.BlockTransformRenderBridge.ApplyToCell(" + Environment.NewLine +
                                indent + "    data.ForegroundTexture," + Environment.NewLine +
                                indent + "    localX," + Environment.NewLine +
                                indent + "    localY," + Environment.NewLine +
                                indent + "    BlockRenderer.BlockPixelSize," + Environment.NewLine +
                                indent + "    chunk.X * Chunk.SizeX + localX," + Environment.NewLine +
                                indent + "    chunk.Y * Chunk.SizeY + localY);" + Environment.NewLine;

                            source = source.Insert(semicolon + 1, code);
                            patched++;
                        }
                    }
                }
            }

            if (patched == 0)
                return PatchResult.Fail(
                    source,
                    "foreground texture anchors not found in DrawBlocks()/UpdateBlock()");

            return PatchResult.Ok(
                source,
                true,
                patched + " current Telder renderer path(s) patched");
        }

        private static PatchResult PatchWorldManager(string path, string source)
        {
            if (source.Contains(WorldMarker))
                return PatchResult.Ok(source, false, "already installed");

            MethodSpan method = FindMethod(source, "public bool SetBlock");
            if (!method.Valid)
                return PatchResult.Fail(source, "public bool SetBlock() not found");

            // Prefer the comment that already separates mutation from rendering.
            // This is safe for both braced and single-line renderer guards.
            int anchor = source.IndexOf(
                "// Обновляем внешний вид блока.",
                method.BodyStart,
                method.BodyEnd - method.BodyStart,
                StringComparison.Ordinal);

            if (anchor < 0)
            {
                anchor = source.IndexOf(
                    "if (renderer != null)",
                    method.BodyStart,
                    method.BodyEnd - method.BodyStart,
                    StringComparison.Ordinal);
            }

            if (anchor < 0)
            {
                anchor = source.IndexOf(
                    "renderer.UpdateBlock",
                    method.BodyStart,
                    method.BodyEnd - method.BodyStart,
                    StringComparison.Ordinal);

                if (anchor >= 0)
                {
                    // If the renderer call is the body of a split `if (` guard,
                    // walk back to that guard so we do not change its meaning.
                    int scan = LineStart(source, anchor) - 1;
                    for (int lines = 0; lines < 5 && scan >= method.BodyStart; lines++)
                    {
                        int lineStart = LineStart(source, scan);
                        string line = source.Substring(
                            lineStart,
                            scan - lineStart + 1).Trim();

                        if (line == "if (" || line == "if(" || line.StartsWith("if (renderer"))
                        {
                            anchor = lineStart;
                            break;
                        }

                        scan = lineStart - 1;
                    }
                }
            }

            if (anchor < 0)
                return PatchResult.Fail(source, "renderer update anchor not found in SetBlock()");

            int insert = LineStart(source, anchor);
            string indent = GetIndent(source, insert);
            string code =
                indent + WorldMarker + Environment.NewLine +
                indent + "Game.BlockTransforms.BlockTransformRegistry.Clear(worldX, worldY);" +
                Environment.NewLine + Environment.NewLine;

            source = source.Insert(insert, code);
            return PatchResult.Ok(source, true, "transform reset added after block replacement");
        }

        private static PatchResult PatchBlockDefinition(string path, string source)
        {
            if (source.Contains(DefinitionMarker))
                return PatchResult.Ok(source, false, "already installed");

            ClassSpan span = FindClass(source, "BlockDefinition");
            if (!span.Valid)
                return PatchResult.Fail(source, "BlockDefinition class body not found");

            string fields =
                Environment.NewLine +
                "        " + DefinitionMarker + Environment.NewLine +
                "        // Normalized rectangle inside one tile (0..1)." + Environment.NewLine +
                "        public bool UseCustomCollision = false;" + Environment.NewLine +
                "        public float CollisionOffsetX = 0.5f;" + Environment.NewLine +
                "        public float CollisionOffsetY = 0.5f;" + Environment.NewLine +
                "        public float CollisionWidth = 1f;" + Environment.NewLine +
                "        public float CollisionHeight = 1f;" + Environment.NewLine;

            source = source.Insert(span.CloseBrace, fields);
            return PatchResult.Ok(source, true, "partial-collision JSON fields added");
        }

        private static PatchResult PatchPlayerCollision(string path, string source)
        {
            if (source.Contains(PlayerMarker))
                return PatchResult.Ok(source, false, "already installed");

            string checkGround =
@"    private void CheckGround()
    {
        if (worldCollision == null)
        {
            IsGrounded = false;
            return;
        }

        float halfWidth = colliderSize.x * 0.5f;
        float bottom = transform.position.y - colliderSize.y * 0.5f;
        float minX = transform.position.x - halfWidth + skin;
        float maxX = transform.position.x + halfWidth - skin;

        IsGrounded = Game.BlockTransforms.BlockShapeCollision.Intersects(
            worldCollision,
            minX,
            bottom - skin - 0.03f,
            maxX,
            bottom + 0.01f);
    }";

            string checkCollision =
@"    // [BT-AUTO-PLAYER-COLLISION]
    private bool CheckCollisionAt(
        float centerX,
        float centerY)
    {
        if (worldCollision == null)
            return false;

        float halfWidth = colliderSize.x * 0.5f;
        float halfHeight = colliderSize.y * 0.5f;

        float minX = centerX - halfWidth + skin;
        float maxX = centerX + halfWidth - skin;
        float minY = centerY - halfHeight + skin;
        float maxY = centerY + halfHeight - skin;

        return Game.BlockTransforms.BlockShapeCollision.Intersects(
            worldCollision,
            minX,
            minY,
            maxX,
            maxY);
    }";

            if (!ReplaceMethod(ref source, "private void CheckGround", checkGround))
                return PatchResult.Fail(source, "CheckGround() could not be replaced");

            if (!ReplaceMethod(ref source, "private bool CheckCollisionAt", checkCollision))
                return PatchResult.Fail(source, "CheckCollisionAt() could not be replaced");

            return PatchResult.Ok(source, true, "shape-aware manual player collision installed");
        }

        private static PatchResult PatchChunkCollision(string path, string source)
        {
            if (source.Contains(ChunkMarker))
                return PatchResult.Ok(source, false, "already installed");

            MethodSpan build = FindMethod(source, "public void BuildChunkCollision");
            if (!build.Valid)
                return PatchResult.Fail(source, "BuildChunkCollision() not found");

            int solidAssign = source.IndexOf(
                "solid[x, y]",
                build.BodyStart,
                build.BodyEnd - build.BodyStart,
                StringComparison.Ordinal);

            if (solidAssign < 0)
                return PatchResult.Fail(source, "solid[x, y] assignment not found");

            int assignSemicolon = FindStatementSemicolon(
                source,
                solidAssign,
                build.BodyEnd);

            if (assignSemicolon < 0)
                return PatchResult.Fail(source, "solid assignment semicolon not found");

            string indent = GetIndent(source, solidAssign);

            string customCheck =
                Environment.NewLine +
                indent + ChunkMarker + Environment.NewLine +
                indent + "if (solid[x, y])" + Environment.NewLine +
                indent + "{" + Environment.NewLine +
                indent + "    int worldX = chunk.X * Chunk.SizeX + x;" + Environment.NewLine +
                indent + "    int worldY = chunk.Y * Chunk.SizeY + y;" + Environment.NewLine +
                Environment.NewLine +
                indent + "    if (Game.BlockTransforms.BlockShapeCollision.IsCustomCell(" + Environment.NewLine +
                indent + "            worldCollision," + Environment.NewLine +
                indent + "            worldX," + Environment.NewLine +
                indent + "            worldY))" + Environment.NewLine +
                indent + "    {" + Environment.NewLine +
                indent + "        solid[x, y] = false;" + Environment.NewLine +
                indent + "        CreateTransformedCustomCollider(" + Environment.NewLine +
                indent + "            chunkObject," + Environment.NewLine +
                indent + "            chunk," + Environment.NewLine +
                indent + "            x," + Environment.NewLine +
                indent + "            y," + Environment.NewLine +
                indent + "            worldX," + Environment.NewLine +
                indent + "            worldY);" + Environment.NewLine +
                indent + "    }" + Environment.NewLine +
                indent + "}" + Environment.NewLine;

            source = source.Insert(assignSemicolon + 1, customCheck);

            ClassSpan classSpan = FindClass(source, "ChunkCollision");
            if (!classSpan.Valid)
                return PatchResult.Fail(source, "ChunkCollision class body not found after patch");

            string method =
@"
        private void CreateTransformedCustomCollider(
            GameObject parent,
            Chunk chunk,
            int localX,
            int localY,
            int worldX,
            int worldY)
        {
            if (!Game.BlockTransforms.BlockShapeCollision.TryGetWorldRect(
                    worldCollision,
                    worldX,
                    worldY,
                    out Vector2 center,
                    out Vector2 size))
            {
                return;
            }

            GameObject colliderObject =
                new GameObject(
                    ""CustomCollider_"" +
                    localX +
                    ""_"" +
                    localY);

            int groundLayer =
                LayerMask.NameToLayer(
                    ""Ground"");

            if (groundLayer != -1)
                colliderObject.layer = groundLayer;

            colliderObject.transform.SetParent(
                parent.transform,
                false);

            colliderObject.transform.localPosition =
                new Vector3(
                    center.x - chunk.X * Chunk.SizeX,
                    center.y - chunk.Y * Chunk.SizeY,
                    0f);

            BoxCollider2D collider =
                colliderObject.AddComponent<BoxCollider2D>();

            collider.size = size;
        }

";

            source = source.Insert(classSpan.CloseBrace, method);

            return PatchResult.Ok(
                source,
                true,
                "patched in-place; no external template is required");
        }

        private static void PatchStructureData(List<string> report)
        {
            string controller = FindSourceByClass("StructureEditorController");

            List<string> preferredTypes = new List<string>
            {
                "StructureBlock",
                "StructureCell",
                "PlacedBlock",
                "StructureTile",
                "StructureBlockData",
                "StructureCellData"
            };

            for (int i = 0; i < preferredTypes.Count; i++)
            {
                string preferredPath = FindSourceByClass(preferredTypes[i]);

                if (!string.IsNullOrEmpty(preferredPath))
                {
                    if (TryAddTransformFieldToClass(
                        preferredPath,
                        preferredTypes[i],
                        out string message))
                    {
                        report.Add("✓ Structure data: " + message);
                        report.Add(string.IsNullOrEmpty(controller)
                            ? "⚠ StructureEditorController: source not found"
                            : "✓ StructureEditorController found: " + controller);
                        return;
                    }
                }
            }

            // V22-compatible fallback:
            // infer the element type from fields such as
            // List<SomeType> Blocks / Foreground / Cells.
            string[] files = Directory.GetFiles(
                Application.dataPath,
                "*.cs",
                SearchOption.AllDirectories);

            Regex listRegex = new Regex(
                @"(?:List|IList|IReadOnlyList)\s*<\s*(?<type>[A-Za-z_][A-Za-z0-9_]*)\s*>\s+(?<name>Blocks|Foreground|Cells|Tiles|PlacedBlocks)\b",
                RegexOptions.IgnoreCase);

            for (int i = 0; i < files.Length; i++)
            {
                string normalized = files[i].Replace('\\', '/');

                if (!normalized.Contains("/Structures/"))
                    continue;

                if (normalized.Contains("/Assets/GameData/BlockTransforms/") ||
                    normalized.Contains("/Assets/Editor/BlockTransforms/"))
                    continue;

                string source;
                try { source = File.ReadAllText(files[i]); }
                catch { continue; }

                Match match = listRegex.Match(source);
                if (!match.Success)
                    continue;

                string elementType = match.Groups["type"].Value;
                string elementPath = FindSourceByClass(elementType);

                if (string.IsNullOrEmpty(elementPath))
                    elementPath = files[i];

                if (TryAddTransformFieldToClass(
                    elementPath,
                    elementType,
                    out string message))
                {
                    report.Add("✓ Structure data: " + message);
                    report.Add(string.IsNullOrEmpty(controller)
                        ? "⚠ StructureEditorController: source not found"
                        : "✓ StructureEditorController found: " + controller);
                    return;
                }
            }

            // Last conservative fallback: score classes only inside /Structures/.
            string bestPath = null;
            string bestClass = null;
            int bestScore = 0;

            for (int i = 0; i < files.Length; i++)
            {
                string normalized = files[i].Replace('\\', '/');

                if (!normalized.Contains("/Structures/"))
                    continue;

                if (normalized.Contains("/Assets/GameData/BlockTransforms/") ||
                    normalized.Contains("/Assets/Editor/BlockTransforms/"))
                    continue;

                string source;
                try { source = File.ReadAllText(files[i]); }
                catch { continue; }

                MatchCollection classes =
                    Regex.Matches(
                        source,
                        @"\bclass\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)");

                for (int c = 0; c < classes.Count; c++)
                {
                    string className = classes[c].Groups["name"].Value;
                    int score = 0;

                    if (className.IndexOf("Block", StringComparison.OrdinalIgnoreCase) >= 0) score += 4;
                    if (className.IndexOf("Cell", StringComparison.OrdinalIgnoreCase) >= 0) score += 3;
                    if (className.IndexOf("Tile", StringComparison.OrdinalIgnoreCase) >= 0) score += 2;

                    if (source.IndexOf("BlockId", StringComparison.OrdinalIgnoreCase) >= 0) score += 4;
                    if (source.IndexOf("ContentID", StringComparison.OrdinalIgnoreCase) >= 0) score += 3;
                    if (source.IndexOf("Vector2Int", StringComparison.Ordinal) >= 0) score += 2;
                    if (source.IndexOf("LocalX", StringComparison.OrdinalIgnoreCase) >= 0) score += 2;
                    if (source.IndexOf("LocalY", StringComparison.OrdinalIgnoreCase) >= 0) score += 2;
                    if (source.IndexOf("Position", StringComparison.OrdinalIgnoreCase) >= 0) score += 1;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestPath = files[i];
                        bestClass = className;
                    }
                }
            }

            if (bestScore >= 7 &&
                TryAddTransformFieldToClass(
                    bestPath,
                    bestClass,
                    out string fallbackMessage))
            {
                report.Add("✓ Structure data: " + fallbackMessage +
                    " (heuristic score " + bestScore + ")");
            }
            else
            {
                report.Add(
                    "⚠ Structure data: model not safely identifiable. " +
                    "Controller was found, but no data class was modified.");
            }

            report.Add(string.IsNullOrEmpty(controller)
                ? "⚠ StructureEditorController: source not found"
                : "ℹ StructureEditorController found: " + controller);
        }

        private static bool TryAddTransformFieldToClass(
            string path,
            string className,
            out string message)
        {
            message = null;

            if (string.IsNullOrEmpty(path) ||
                string.IsNullOrEmpty(className) ||
                !File.Exists(path))
            {
                return false;
            }

            string source;
            try { source = File.ReadAllText(path); }
            catch { return false; }

            ClassSpan span = FindClass(source, className);
            if (!span.Valid)
                return false;

            string classBody = source.Substring(
                source.IndexOf('{', source.IndexOf("class " + className, StringComparison.Ordinal)),
                span.CloseBrace - source.IndexOf('{', source.IndexOf("class " + className, StringComparison.Ordinal)));

            if (classBody.Contains("public byte Transform") ||
                classBody.Contains("byte Transform;"))
            {
                message = className + " already has Transform";
                return true;
            }

            Backup(path);

            string field =
                Environment.NewLine +
                "        " + StructureMarker + Environment.NewLine +
                "        public byte Transform;" + Environment.NewLine;

            source = source.Insert(span.CloseBrace, field);
            File.WriteAllText(path, source, new UTF8Encoding(false));

            message =
                "Transform byte added to " +
                className +
                " (" +
                path +
                ")";

            return true;
        }

        private static int FindStatementSemicolon(
            string source,
            int start,
            int limit)
        {
            int paren = 0;
            int bracket = 0;
            int brace = 0;
            bool inString = false;
            bool inChar = false;
            bool escaped = false;
            bool lineComment = false;
            bool blockComment = false;

            for (int i = start; i < source.Length && i <= limit; i++)
            {
                char c = source[i];
                char next = i + 1 < source.Length ? source[i + 1] : '\0';

                if (lineComment)
                {
                    if (c == '\n') lineComment = false;
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
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"') inString = false;
                    continue;
                }

                if (inChar)
                {
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '\'') inChar = false;
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

                if (c == '"') { inString = true; continue; }
                if (c == '\'') { inChar = true; continue; }

                if (c == '(') paren++;
                else if (c == ')') paren--;
                else if (c == '[') bracket++;
                else if (c == ']') bracket--;
                else if (c == '{') brace++;
                else if (c == '}') brace--;
                else if (c == ';' &&
                         paren == 0 &&
                         bracket == 0 &&
                         brace == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool ReplaceMethod(ref string source, string signature, string replacement)
        {
            MethodSpan span = FindMethod(source, signature);
            if (!span.Valid)
                return false;

            int start = LineStart(source, span.SignatureIndex);
            source = source.Remove(start, span.BodyEnd - start + 1);
            source = source.Insert(start, replacement);
            return true;
        }

        private static MethodSpan FindMethod(string source, string signature)
        {
            int index = source.IndexOf(signature, StringComparison.Ordinal);
            if (index < 0)
                return new MethodSpan();

            int open = source.IndexOf('{', index);
            if (open < 0 || open - index > 1000)
                return new MethodSpan();

            int close = FindMatching(source, open, '{', '}');
            if (close < 0)
                return new MethodSpan();

            return new MethodSpan
            {
                Valid = true,
                SignatureIndex = index,
                BodyStart = open,
                BodyEnd = close
            };
        }

        private static ClassSpan FindClass(string source, string className)
        {
            int index = source.IndexOf("class " + className, StringComparison.Ordinal);
            if (index < 0)
                return new ClassSpan();

            int open = source.IndexOf('{', index);
            if (open < 0)
                return new ClassSpan();

            int close = FindMatching(source, open, '{', '}');
            if (close < 0)
                return new ClassSpan();

            return new ClassSpan { Valid = true, CloseBrace = close };
        }

        private static int FindMatching(string text, int openIndex, char open, char close)
        {
            int depth = 0;
            bool inString = false;
            bool inChar = false;
            bool escaped = false;
            bool lineComment = false;
            bool blockComment = false;

            for (int i = openIndex; i < text.Length; i++)
            {
                char c = text[i];
                char next = i + 1 < text.Length ? text[i + 1] : '\0';

                if (lineComment)
                {
                    if (c == '\n') lineComment = false;
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
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"') inString = false;
                    continue;
                }

                if (inChar)
                {
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '\'') inChar = false;
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

                if (c == '"') { inString = true; continue; }
                if (c == '\'') { inChar = true; continue; }

                if (c == open) depth++;
                else if (c == close)
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }

            return -1;
        }

        private static List<string> SplitArguments(string text)
        {
            List<string> result = new List<string>();
            int start = 0;
            int paren = 0;
            int bracket = 0;
            int brace = 0;
            bool inString = false;
            bool escaped = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (inString)
                {
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"') inString = false;
                    continue;
                }

                if (c == '"') { inString = true; continue; }
                if (c == '(') paren++;
                else if (c == ')') paren--;
                else if (c == '[') bracket++;
                else if (c == ']') bracket--;
                else if (c == '{') brace++;
                else if (c == '}') brace--;
                else if (c == ',' && paren == 0 && bracket == 0 && brace == 0)
                {
                    result.Add(text.Substring(start, i - start));
                    start = i + 1;
                }
            }

            result.Add(text.Substring(start));
            return result;
        }

        private static string FindSourceByClass(string className)
        {
            string[] files = Directory.GetFiles(
                Application.dataPath, "*.cs", SearchOption.AllDirectories);
            string token = "class " + className;

            for (int i = 0; i < files.Length; i++)
            {
                string normalized = files[i].Replace('\\', '/');
                if (normalized.Contains("/Assets/GameData/BlockTransforms/") ||
                    normalized.Contains("/Assets/Editor/BlockTransforms/"))
                    continue;

                try
                {
                    string source = File.ReadAllText(files[i]);
                    if (source.Contains(token))
                        return files[i];
                }
                catch { }
            }

            return null;
        }

        private static void AppendStatus(StringBuilder text, string className, string marker)
        {
            string path = FindSourceByClass(className);
            if (string.IsNullOrEmpty(path))
            {
                text.AppendLine(className + ": SOURCE NOT FOUND");
                return;
            }

            string source = File.ReadAllText(path);
            text.AppendLine(className + ": " +
                (source.Contains(marker) ? "OK" : "NOT PATCHED"));
        }

        private static void Backup(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            string assetsRoot = Application.dataPath.Replace('\\', '/');
            string normalized = path.Replace('\\', '/');
            string relative = normalized.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase)
                ? "Assets" + normalized.Substring(assetsRoot.Length)
                : Path.GetFileName(path);

            string destination = Path.Combine(backupRoot, relative);
            string directory = Path.GetDirectoryName(destination);
            Directory.CreateDirectory(directory);
            File.Copy(path, destination, true);
        }

        private static int LineStart(string text, int index)
        {
            int line = text.LastIndexOf('\n', Math.Max(0, index - 1));
            return line < 0 ? 0 : line + 1;
        }

        private static string GetIndent(string text, int index)
        {
            int start = LineStart(text, index);
            int end = start;

            while (end < text.Length && (text[end] == ' ' || text[end] == '\t'))
                end++;

            return text.Substring(start, end - start);
        }

        private struct PatchResult
        {
            public bool Success;
            public bool Changed;
            public string Source;
            public string Message;

            public static PatchResult Ok(string source, bool changed, string message)
            {
                return new PatchResult
                {
                    Success = true,
                    Changed = changed,
                    Source = source,
                    Message = message
                };
            }

            public static PatchResult Fail(string source, string message)
            {
                return new PatchResult
                {
                    Success = false,
                    Changed = false,
                    Source = source,
                    Message = message
                };
            }
        }

        private struct MethodSpan
        {
            public bool Valid;
            public int SignatureIndex;
            public int BodyStart;
            public int BodyEnd;
        }

        private struct ClassSpan
        {
            public bool Valid;
            public int CloseBrace;
        }
    }
}
#endif
