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

            RepairInvalidStructurePatches(report);

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
                report.Add("вљ  " + className + ": source not found");
                return;
            }

            string source = File.ReadAllText(path);
            PatchResult result = patch(path, source);

            if (!result.Changed)
            {
                report.Add((result.Success ? "вњ“ " : "вљ  ") +
                    className + ": " + result.Message);
                return;
            }

            Backup(path);
            File.WriteAllText(path, result.Source, new UTF8Encoding(false));
            report.Add("вњ“ " + className + ": " + result.Message);
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

            // v1.4:
            // Match the actual Telder DrawBlock calls independent of whitespace/newlines.
            Regex foregroundDraw = new Regex(
                @"BlockRenderer\s*\.\s*DrawBlock\s*\(\s*" +
                @"data\s*\.\s*ForegroundTexture\s*,\s*" +
                @"(?<x>[A-Za-z_][A-Za-z0-9_]*)\s*,\s*" +
                @"(?<y>[A-Za-z_][A-Za-z0-9_]*)\s*,\s*" +
                @"chunk\s*\.\s*GetBlock\s*\(\s*" +
                @"\k<x>\s*,\s*\k<y>\s*\)\s*\)\s*;",
                RegexOptions.Multiline);

            MatchCollection matches = foregroundDraw.Matches(source);

            if (matches.Count == 0)
            {
                return PatchResult.Fail(
                    source,
                    "actual foreground BlockRenderer.DrawBlock(...) call not found");
            }

            StringBuilder result = new StringBuilder();
            int cursor = 0;
            int patched = 0;

            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];

                result.Append(
                    source,
                    cursor,
                    match.Index - cursor);

                result.Append(match.Value);

                string x = match.Groups["x"].Value;
                string y = match.Groups["y"].Value;
                string indent = GetIndent(source, match.Index);

                result.Append(Environment.NewLine);
                result.Append(indent);
                result.Append(RenderMarker);
                result.Append(" EXACT_V14");
                result.Append(Environment.NewLine);

                result.Append(indent);
                result.Append(
                    "Game.BlockTransforms.BlockTransformRenderBridge.ApplyToCell(");
                result.Append(Environment.NewLine);

                result.Append(indent);
                result.Append("    data.ForegroundTexture,");
                result.Append(Environment.NewLine);

                result.Append(indent);
                result.Append("    ");
                result.Append(x);
                result.Append(",");
                result.Append(Environment.NewLine);

                result.Append(indent);
                result.Append("    ");
                result.Append(y);
                result.Append(",");
                result.Append(Environment.NewLine);

                result.Append(indent);
                result.Append("    BlockRenderer.BlockPixelSize,");
                result.Append(Environment.NewLine);

                result.Append(indent);
                result.Append("    chunk.X * Chunk.SizeX + ");
                result.Append(x);
                result.Append(",");
                result.Append(Environment.NewLine);

                result.Append(indent);
                result.Append("    chunk.Y * Chunk.SizeY + ");
                result.Append(y);
                result.Append(");");

                cursor = match.Index + match.Length;
                patched++;
            }

            result.Append(
                source,
                cursor,
                source.Length - cursor);

            return PatchResult.Ok(
                result.ToString(),
                true,
                patched + " exact Telder foreground draw call(s) patched");
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

            // v1.4:
            // Match the actual Telder solid map assignment independent of formatting.
            Regex solidAssignment = new Regex(
                @"solid\s*\[\s*(?<x>[A-Za-z_][A-Za-z0-9_]*)\s*,\s*" +
                @"(?<y>[A-Za-z_][A-Za-z0-9_]*)\s*\]\s*=\s*" +
                @"IsSolidBlock\s*\(\s*(?<id>[A-Za-z_][A-Za-z0-9_]*)\s*\)\s*;",
                RegexOptions.Multiline);

            Match match = solidAssignment.Match(source);

            if (!match.Success)
            {
                return PatchResult.Fail(
                    source,
                    "actual solid[x,y] = IsSolidBlock(...) assignment not found");
            }

            string x = match.Groups["x"].Value;
            string y = match.Groups["y"].Value;
            string indent = GetIndent(source, match.Index);

            string customCheck =
                Environment.NewLine +
                indent + ChunkMarker + " EXACT_V14" + Environment.NewLine +
                indent + "if (solid[" + x + ", " + y + "])" + Environment.NewLine +
                indent + "{" + Environment.NewLine +
                indent + "    int worldX = chunk.X * Chunk.SizeX + " + x + ";" + Environment.NewLine +
                indent + "    int worldY = chunk.Y * Chunk.SizeY + " + y + ";" + Environment.NewLine +
                Environment.NewLine +
                indent + "    if (Game.BlockTransforms.BlockShapeCollision.IsCustomCell(" + Environment.NewLine +
                indent + "            worldCollision," + Environment.NewLine +
                indent + "            worldX," + Environment.NewLine +
                indent + "            worldY))" + Environment.NewLine +
                indent + "    {" + Environment.NewLine +
                indent + "        solid[" + x + ", " + y + "] = false;" + Environment.NewLine +
                indent + "        CreateTransformedCustomCollider(" + Environment.NewLine +
                indent + "            chunkObject," + Environment.NewLine +
                indent + "            chunk," + Environment.NewLine +
                indent + "            " + x + "," + Environment.NewLine +
                indent + "            " + y + "," + Environment.NewLine +
                indent + "            worldX," + Environment.NewLine +
                indent + "            worldY);" + Environment.NewLine +
                indent + "    }" + Environment.NewLine +
                indent + "}";

            source =
                source.Insert(
                    match.Index + match.Length,
                    customCheck);

            ClassSpan classSpan =
                FindClass(
                    source,
                    "ChunkCollision");

            if (!classSpan.Valid)
            {
                return PatchResult.Fail(
                    source,
                    "ChunkCollision class end not found");
            }

            string helper =
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

            source =
                source.Insert(
                    classSpan.CloseBrace,
                    helper);

            return PatchResult.Ok(
                source,
                true,
                "exact Telder solid-map collision path patched");
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
                        report.Add("вњ“ Structure data: " + message);
                        report.Add(string.IsNullOrEmpty(controller)
                            ? "вљ  StructureEditorController: source not found"
                            : "вњ“ StructureEditorController found: " + controller);
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
                    report.Add("вњ“ Structure data: " + message);
                    report.Add(string.IsNullOrEmpty(controller)
                        ? "вљ  StructureEditorController: source not found"
                        : "вњ“ StructureEditorController found: " + controller);
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

                    if (IsUnsafeStructureDataClassName(className))
                        continue;

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
                report.Add("вњ“ Structure data: " + fallbackMessage +
                    " (heuristic score " + bestScore + ")");
            }
            else
            {
                report.Add(
                    "вљ  Structure data: model not safely identifiable. " +
                    "Controller was found, but no data class was modified.");
            }

            report.Add(string.IsNullOrEmpty(controller)
                ? "вљ  StructureEditorController: source not found"
                : "в„№ StructureEditorController found: " + controller);
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

            int classIndex =
                source.IndexOf(
                    "class " + className,
                    StringComparison.Ordinal);

            int declarationStart =
                source.LastIndexOf(
                    '\n',
                    classIndex);

            declarationStart =
                declarationStart < 0
                    ? 0
                    : declarationStart + 1;

            int openBrace =
                source.IndexOf(
                    '{',
                    classIndex);

            if (openBrace < 0 ||
                openBrace >= span.CloseBrace)
            {
                return false;
            }

            string declaration =
                source.Substring(
                    declarationStart,
                    openBrace - declarationStart);

            // Never inject per-instance block data into a static/service/runtime class.
            if (Regex.IsMatch(
                    declaration,
                    @"\bstatic\s+class\b",
                    RegexOptions.IgnoreCase) ||
                IsUnsafeStructureDataClassName(className))
            {
                message =
                    className +
                    " skipped: runtime/static/service class";
                return false;
            }

            string classBody =
                source.Substring(
                    openBrace,
                    span.CloseBrace - openBrace);

            if (classBody.Contains("public byte Transform") ||
                classBody.Contains("byte Transform;"))
            {
                message = className + " already has Transform";
                return true;
            }

            if (!LooksLikeSerializableStructureRecord(
                    source,
                    declarationStart,
                    classIndex,
                    classBody))
            {
                message =
                    className +
                    " skipped: not a structure block/cell record";
                return false;
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

        private static bool IsUnsafeStructureDataClassName(
            string className)
        {
            if (string.IsNullOrEmpty(className))
                return true;

            string[] forbidden =
            {
                "Runtime",
                "Generation",
                "Generator",
                "Controller",
                "Manager",
                "Scanner",
                "Cache",
                "Registry",
                "Loader",
                "Service",
                "Utility",
                "Helper",
                "EditorWindow",
                "Database"
            };

            for (int i = 0; i < forbidden.Length; i++)
            {
                if (className.IndexOf(
                        forbidden[i],
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool LooksLikeSerializableStructureRecord(
            string source,
            int declarationStart,
            int classIndex,
            string classBody)
        {
            int attributeSearchStart =
                Math.Max(
                    0,
                    declarationStart - 300);

            string prefix =
                source.Substring(
                    attributeSearchStart,
                    classIndex - attributeSearchStart);

            bool serializable =
                prefix.IndexOf(
                    "[Serializable",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                prefix.IndexOf(
                    "[System.Serializable",
                    StringComparison.OrdinalIgnoreCase) >= 0;

            bool hasBlockId =
                Regex.IsMatch(
                    classBody,
                    @"\b(BlockId|BlockID|blockId|blockID|ContentID|BlockContentID)\b",
                    RegexOptions.IgnoreCase);

            bool hasPosition =
                Regex.IsMatch(
                    classBody,
                    @"\b(Vector2Int|Position|LocalPosition|LocalX|LocalY|CellX|CellY|OffsetX|OffsetY)\b",
                    RegexOptions.IgnoreCase) ||
                (
                    Regex.IsMatch(
                        classBody,
                        @"\b(int|short|byte)\s+[Xx]\b") &&
                    Regex.IsMatch(
                        classBody,
                        @"\b(int|short|byte)\s+[Yy]\b")
                );

            return
                serializable &&
                hasBlockId &&
                hasPosition;
        }

        private static void RepairInvalidStructurePatches(
            List<string> report)
        {
            string[] files =
                Directory.GetFiles(
                    Application.dataPath,
                    "*.cs",
                    SearchOption.AllDirectories);

            int repaired = 0;

            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i];

                if (IsOurFile(path))
                    continue;

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

                if (!source.Contains(StructureMarker))
                    continue;

                bool unsafeFile =
                    source.IndexOf(
                        "static class",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    Path.GetFileNameWithoutExtension(path)
                        .IndexOf(
                            "Runtime",
                            StringComparison.OrdinalIgnoreCase) >= 0 ||
                    Path.GetFileNameWithoutExtension(path)
                        .IndexOf(
                            "Generation",
                            StringComparison.OrdinalIgnoreCase) >= 0 ||
                    Path.GetFileNameWithoutExtension(path)
                        .IndexOf(
                            "Generator",
                            StringComparison.OrdinalIgnoreCase) >= 0;

                if (!unsafeFile)
                    continue;

                string cleaned =
                    Regex.Replace(
                        source,
                        @"[ \t]*// \[BT-AUTO-STRUCTURE-DATA\][ \t]*\r?\n[ \t]*public byte Transform;[ \t]*\r?\n?",
                        string.Empty);

                if (cleaned == source)
                    continue;

                Backup(path);

                File.WriteAllText(
                    path,
                    cleaned,
                    new UTF8Encoding(false));

                repaired++;
            }

            if (repaired > 0)
            {
                report.Add(
                    "вњ“ Structure repair: removed invalid Transform field from " +
                    repaired +
                    " runtime/static class(es).");
            }
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
                Application.dataPath,
                "*.cs",
                SearchOption.AllDirectories);

            string token = "class " + className;

            string bestPath = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < files.Length; i++)
            {
                string normalized =
                    files[i].Replace('\\', '/');

                if (normalized.Contains("/Assets/GameData/BlockTransforms/") ||
                    normalized.Contains("/Assets/Editor/BlockTransforms/") ||
                    normalized.Contains("/BlockTransform_Backups/"))
                {
                    continue;
                }

                string fileName =
                    Path.GetFileNameWithoutExtension(
                        files[i]);

                string candidate;

                try
                {
                    candidate =
                        File.ReadAllText(
                            files[i]);
                }
                catch
                {
                    continue;
                }

                if (!candidate.Contains(token))
                    continue;

                int score = 0;

                if (string.Equals(
                        fileName,
                        className,
                        StringComparison.Ordinal))
                {
                    score += 100;
                }

                if (className == "ChunkRenderer")
                {
                    if (normalized.Contains("/GameData/World/Rendering/"))
                        score += 100;

                    if (candidate.Contains("data.ForegroundTexture"))
                        score += 30;

                    if (candidate.Contains("BlockRenderer.DrawBlock"))
                        score += 30;

                    if (candidate.Contains("public void UpdateBlock"))
                        score += 20;
                }
                else if (className == "ChunkCollision")
                {
                    if (normalized.Contains("/GameData/World/Collision/"))
                        score += 100;

                    if (candidate.Contains("BuildChunkCollision"))
                        score += 30;

                    if (candidate.Contains("IsSolidBlock"))
                        score += 30;

                    if (candidate.Contains("bool[,]"))
                        score += 20;
                }
                else if (className == "StructureEditorController")
                {
                    if (normalized.Contains("/Structures/EditorRuntime/"))
                        score += 100;
                }
                else if (className == "BlockInteraction")
                {
                    if (normalized.Contains("/GameData/"))
                        score += 20;
                }

                // Avoid generated/backup/sample sources if they somehow live under Assets.
                if (normalized.IndexOf(
                        "/Backup",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    normalized.IndexOf(
                        "/Samples/",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score -= 200;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPath = files[i];
                }
            }

            return bestPath;
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

        private static bool IsOurFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string normalized =
                path.Replace('\\', '/');

            return
                normalized.Contains("/Assets/GameData/BlockTransforms/")
                ||
                normalized.Contains("/Assets/Editor/BlockTransforms/")
                ||
                normalized.Contains("/Assets/GameData/Editor/BlockTransforms/");
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
