using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.World.Structures.EditorRuntime
{
    /// <summary>
    /// Runtime IMGUI resource editor for JSON content in Assets/GameData.
    /// Existing JSON is edited as a JObject so unknown fields are preserved.
    /// Texture previews are read directly from the Default resource pack.
    /// </summary>
    public sealed class TEditorContentEditorController : MonoBehaviour
    {
        private sealed class CatalogEntry
        {
            public string Path;
            public string Id;
            public string Name;
            public string Texture;
        }

        private enum ResourceCategory
        {
            Blocks,
            Items
        }

        private readonly List<CatalogEntry> entries = new List<CatalogEntry>();
        private readonly List<CatalogEntry> filteredEntries = new List<CatalogEntry>();
        private readonly Dictionary<string, List<string>> texturePathsByName = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> primitiveBuffers = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> complexBuffers = new Dictionary<string, string>(StringComparer.Ordinal);

        private bool showMobs;
        private ResourceCategory category = ResourceCategory.Blocks;
        private CatalogEntry selectedEntry;
        private JObject currentObject;
        private string currentFilePath;
        private string search = string.Empty;
        private string status = string.Empty;
        private string linkedText = string.Empty;
        private bool addFieldPopup;
        private string pendingFieldName = "NewField";
        private int pendingFieldType;
        private bool linkedFields;
        private bool dirty;
        private Vector2 catalogScroll;
        private Vector2 propertyScroll;

        private Texture2D backgroundTexture;
        private Texture2D panelTexture;
        private Texture2D rowTexture;
        private Texture2D selectedTexture;
        private Texture2D buttonTexture;
        private Texture2D hoverTexture;
        private Texture2D accentTexture;
        private GUIStyle panelStyle;
        private GUIStyle rowStyle;
        private GUIStyle selectedStyle;
        private GUIStyle buttonStyle;
        private GUIStyle secondaryButtonStyle;
        private GUIStyle textFieldStyle;
        private GUIStyle textAreaStyle;
        private GUIStyle smallLabelStyle;
        private GUIStyle tileNameStyle;
        private GUIStyle tileIdStyle;
        private GUIStyle toggleStyle;

        private string GameDataPath
        {
            get { return Game.Content.DataPaths.GameDataFolder; }
        }

        private string TextureRoot
        {
            get { return Path.Combine(GameDataPath, "ResourcePacks", "Default", "textures"); }
        }

        private void OnEnable()
        {
            if (Application.isEditor)
                RefreshCatalog(null);
            else
                status = "T-Editor предназначен для запуска внутри Unity Editor.";
        }

        public void OpenCatalog(bool mobs)
        {
            showMobs = mobs;
            category = ResourceCategory.Blocks;
            addFieldPopup = false;
            search = string.Empty;
            filteredEntries.Clear();
            selectedEntry = null;
            currentObject = null;
            currentFilePath = null;
            dirty = false;
            bool wasEnabled = enabled;
            if (!wasEnabled)
                enabled = true; // OnEnable refreshes the selected catalog.
            else if (Application.isEditor)
                RefreshCatalog(null);
        }

        private void OnGUI()
        {
            if (!enabled)
                return;

            EnsureStyles();
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backgroundTexture);
            bool previousGuiEnabled = GUI.enabled;
            if (addFieldPopup)
                GUI.enabled = false;
            DrawTopBar();
            DrawCatalogPanel();
            DrawPropertiesPanel();
            GUI.enabled = previousGuiEnabled;
            DrawAddFieldPopup();
        }

        private void DrawTopBar()
        {
            GUI.Box(new Rect(0, 0, Screen.width, 62), GUIContent.none, panelStyle);
            if (GUI.Button(new Rect(18, 13, 118, 36), "←  МЕНЮ", secondaryButtonStyle))
            {
                if (TEditorMenuController.Instance != null)
                    TEditorMenuController.Instance.ReturnHome();
                else
                    enabled = false;
                return;
            }

            float right = Screen.width - 18f;
            string newLabel = showMobs ? "+ НОВЫЙ МОБ" : (category == ResourceCategory.Blocks ? "+ НОВЫЙ БЛОК" : "+ НОВЫЙ ПРЕДМЕТ");
            float newWidth = showMobs ? 142f : 160f;
            float newX = right - 138f - newWidth;
            string title = showMobs ? "T-EDITOR  /  МОБЫ" : "T-EDITOR  /  РЕДАКТОР РЕСУРСОВ";
            float titleWidth = Mathf.Max(120f, newX - 166f);
            GUI.Label(new Rect(154, 10, titleWidth, 42), title, TEditorMenuController.MakeLabelStyle(22, Color.white, TextAnchor.MiddleLeft));

            if (GUI.Button(new Rect(right - 128f, 13, 110, 36), "ОБНОВИТЬ", secondaryButtonStyle))
            {
                if (dirty)
                    status = "Сначала сохраните изменения или нажмите «Отменить правки».";
                else
                    RefreshCatalog(currentFilePath);
            }

            if (GUI.Button(new Rect(newX, 13, newWidth, 36), newLabel, buttonStyle))
                CreateNewResource();
        }

        private void DrawCatalogPanel()
        {
            float leftWidth = Mathf.Clamp(Screen.width * 0.34f, 230f, 420f);
            Rect panel = new Rect(18, 76, leftWidth, Screen.height - 94);
            GUI.Box(panel, GUIContent.none, panelStyle);

            float top = panel.y + 12f;
            if (showMobs)
            {
                GUI.Label(new Rect(panel.x + 14, top, panel.width - 28, 28), "МОБЫ", TEditorMenuController.MakeLabelStyle(16, Color.white, TextAnchor.MiddleLeft));
                top += 36f;
            }
            else
            {
                float tabWidth = (panel.width - 36f) * 0.5f;
                if (GUI.Button(new Rect(panel.x + 12, top, tabWidth, 34), "БЛОКИ", category == ResourceCategory.Blocks ? buttonStyle : secondaryButtonStyle))
                    SwitchCategory(ResourceCategory.Blocks);
                if (GUI.Button(new Rect(panel.x + 20 + tabWidth, top, tabWidth, 34), "ПРЕДМЕТЫ", category == ResourceCategory.Items ? buttonStyle : secondaryButtonStyle))
                    SwitchCategory(ResourceCategory.Items);
                top += 42f;
            }

            string nextSearch = GUI.TextField(new Rect(panel.x + 12, top, panel.width - 24, 30), search, textFieldStyle);
            if (!string.Equals(nextSearch, search, StringComparison.Ordinal))
            {
                search = nextSearch;
                catalogScroll = Vector2.zero;
                RebuildFilteredEntries();
            }
            top += 38f;

            GUI.Label(new Rect(panel.x + 12, top, panel.width - 24, 20), entries.Count + " записей  •  выбери ресурс для редактирования", smallLabelStyle);
            top += 24f;

            Rect viewport = new Rect(panel.x + 8, top, panel.width - 16, panel.yMax - top - 10);
            int columns = panel.width < 310 ? 2 : 3;
            float gap = 6f;
            float tileWidth = (viewport.width - 18f - gap * (columns - 1)) / columns;
            float tileHeight = 86f;
            List<CatalogEntry> filtered = filteredEntries;
            int rows = Mathf.CeilToInt(filtered.Count / (float)columns);
            float contentHeight = Mathf.Max(viewport.height, rows * (tileHeight + gap) + 4f);
            catalogScroll = GUI.BeginScrollView(viewport, catalogScroll, new Rect(0, 0, viewport.width - 16f, contentHeight));

            int firstRow = Mathf.Max(0, Mathf.FloorToInt(catalogScroll.y / (tileHeight + gap)) - 1);
            int lastRow = Mathf.Min(Mathf.Max(0, rows - 1), Mathf.CeilToInt((catalogScroll.y + viewport.height) / (tileHeight + gap)) + 1);
            for (int row = firstRow; row <= lastRow; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int i = row * columns + column;
                    if (i < 0 || i >= filtered.Count)
                        continue;

                    Rect tile = new Rect(column * (tileWidth + gap), row * (tileHeight + gap), tileWidth, tileHeight);
                    CatalogEntry entry = filtered[i];
                    bool selected = selectedEntry != null && string.Equals(selectedEntry.Path, entry.Path, StringComparison.OrdinalIgnoreCase);
                    if (GUI.Button(tile, GUIContent.none, selected ? selectedStyle : rowStyle))
                        SelectEntry(entry);

                    Texture2D icon = GetIcon(entry);
                    if (icon != null)
                    {
                        float iconSize = Mathf.Min(44f, tileWidth - 10f);
                        GUI.DrawTexture(new Rect(tile.x + (tile.width - iconSize) * 0.5f, tile.y + 5f, iconSize, iconSize), icon, ScaleMode.ScaleToFit, true);
                    }
                    string label = string.IsNullOrEmpty(entry.Name) ? entry.Id : entry.Name;
                    GUI.Label(new Rect(tile.x + 3f, tile.y + 51f, tile.width - 6f, 16f), Clip(label, 15), tileNameStyle);
                    GUI.Label(new Rect(tile.x + 3f, tile.y + 66f, tile.width - 6f, 15f), Clip(entry.Id, 20), tileIdStyle);
                }
            }

            GUI.EndScrollView();
        }

        private void DrawPropertiesPanel()
        {
            float leftWidth = Mathf.Clamp(Screen.width * 0.34f, 230f, 420f);
            float x = 18f + leftWidth + 16f;
            Rect panel = new Rect(x, 76, Mathf.Max(200f, Screen.width - x - 18f), Screen.height - 94f);
            GUI.Box(panel, GUIContent.none, panelStyle);

            GUI.Label(new Rect(panel.x + 16, panel.y + 12, panel.width - 32, 28), "JSON  /  ПАРАМЕТРЫ", TEditorMenuController.MakeLabelStyle(17, Color.white, TextAnchor.MiddleLeft));
            string pathLabel = string.IsNullOrEmpty(currentFilePath) ? "Новый ресурс — ещё не сохранён" : MakeProjectRelativePath(currentFilePath);
            GUI.Label(new Rect(panel.x + 16, panel.y + 39, panel.width - 32, 20), pathLabel, smallLabelStyle);

            float toolbarY = panel.y + 66f;
            if (currentObject != null)
            {
                if (!showMobs)
                {
                    string linkLabel = linkedFields ? "СВЯЗЬ ID / ИМЯ / ТЕКСТУРА: ВКЛ" : "СВЯЗАТЬ ID / ИМЯ / ТЕКСТУРУ";
                    if (GUI.Button(new Rect(panel.x + 14, toolbarY, Mathf.Min(240f, panel.width - 28f), 30), linkLabel, linkedFields ? buttonStyle : secondaryButtonStyle))
                    {
                        linkedFields = !linkedFields;
                        linkedText = GetString(currentObject, "Name", GetString(currentObject, "ID", string.Empty));
                        status = linkedFields ? "Изменение общего имени обновит Name, ID и Texture." : "Поля снова редактируются независимо.";
                    }
                }
                else
                {
                    GUI.Label(new Rect(panel.x + 14, toolbarY, panel.width - 28f, 30), "Поля JSON можно редактировать напрямую", smallLabelStyle);
                }

                float buttonsY = toolbarY + 36f;
                float innerWidth = Mathf.Max(160f, panel.width - 28f);
                float addWidth = Mathf.Min(118f, innerWidth * 0.32f);
                float saveWidth = Mathf.Min(104f, innerWidth * 0.28f);
                float cancelWidth = Mathf.Min(124f, innerWidth - addWidth - saveWidth - 12f);
                float addX = panel.x + 14f;
                float saveX = addX + addWidth + 6f;
                float cancelX = panel.xMax - 14f - cancelWidth;
                if (GUI.Button(new Rect(addX, buttonsY, addWidth, 30), "+ ПОЛЕ JSON", secondaryButtonStyle))
                {
                    addFieldPopup = true;
                    pendingFieldName = "NewField";
                    pendingFieldType = 0;
                }
                if (GUI.Button(new Rect(saveX, buttonsY, saveWidth, 30), "СОХРАНИТЬ", buttonStyle))
                    SaveCurrent();
                string cancelLabel = cancelWidth < 100f ? "ОТМЕНА" : "ОТМЕНИТЬ ПРАВКИ";
                if (GUI.Button(new Rect(cancelX, buttonsY, cancelWidth, 30), cancelLabel, secondaryButtonStyle))
                    DiscardChanges();
            }

            if (currentObject == null)
            {
                GUI.Label(new Rect(panel.x + 24, panel.y + 150, panel.width - 48, 80), "Выбери блок или предмет слева либо создай новый ресурс.", TEditorMenuController.MakeLabelStyle(16, new Color(0.62f, 0.67f, 0.74f), TextAnchor.UpperLeft));
                DrawStatus(panel);
                return;
            }

            float fieldsY = panel.y + 144f;
            Rect viewport = new Rect(panel.x + 10, fieldsY, panel.width - 20, panel.yMax - fieldsY - 44f);
            float contentWidth = viewport.width - 18f;
            List<string> keys = currentObject.Properties().Select(p => p.Name).ToList();
            float contentHeight = 8f;
            if (linkedFields && !showMobs)
                contentHeight += 74f;
            for (int i = 0; i < keys.Count; i++)
            {
                if (linkedFields && !showMobs && IsLinkedKey(keys[i]))
                    continue;
                JToken token = currentObject[keys[i]];
                contentHeight += IsComplex(token) ? 132f : 42f;
            }
            contentHeight = Mathf.Max(viewport.height, contentHeight + 10f);
            propertyScroll = GUI.BeginScrollView(viewport, propertyScroll, new Rect(0, 0, contentWidth, contentHeight));

            float y = 6f;
            if (linkedFields && !showMobs)
            {
                GUI.Box(new Rect(0, y, contentWidth, 66f), GUIContent.none, rowStyle);
                GUI.Label(new Rect(10, y + 6, 160, 22), "Общее имя ресурса", TEditorMenuController.MakeLabelStyle(12, new Color(0.72f, 0.79f, 0.88f), TextAnchor.MiddleLeft));
                string next = GUI.TextField(new Rect(172, y + 7, contentWidth - 184, 28), linkedText, textFieldStyle);
                if (!string.Equals(next, linkedText, StringComparison.Ordinal))
                {
                    linkedText = next;
                    ApplyLinkedFields(linkedText);
                    dirty = true;
                }
                GUI.Label(new Rect(10, y + 37, contentWidth - 20, 20), "Name = текст · Texture = имя_файла · ID = namespace:имя_файла", smallLabelStyle);
                y += 74f;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                if (linkedFields && !showMobs && IsLinkedKey(key))
                    continue;
                if (currentObject.Property(key) == null)
                    continue;
                JToken token = currentObject[key];
                y += DrawPropertyRow(key, token, y, contentWidth);
            }

            GUI.EndScrollView();
            DrawStatus(panel);
        }

        private float DrawPropertyRow(string key, JToken token, float y, float width)
        {
            bool complex = IsComplex(token);
            float height = complex ? 126f : 36f;
            GUI.Box(new Rect(0, y, width, height), GUIContent.none, rowStyle);
            float keyWidth = Mathf.Clamp(width * 0.27f, 110f, 190f);
            string nextKey = GUI.TextField(new Rect(8, y + 5, keyWidth - 14, 26), key, textFieldStyle);
            if (!string.Equals(nextKey, key, StringComparison.Ordinal))
            {
                RenameProperty(key, nextKey);
                return height + 6f;
            }

            float removeX = width - 34f;
            if (GUI.Button(new Rect(removeX, y + 5, 26, 26), "×", secondaryButtonStyle))
            {
                currentObject.Property(key).Remove();
                primitiveBuffers.Remove(key);
                complexBuffers.Remove(key);
                dirty = true;
                status = "Поле удалено. Сохраните изменения.";
                return height + 6f;
            }

            Rect valueRect = new Rect(keyWidth, y + 5, Mathf.Max(60f, width - keyWidth - 44f), complex ? 82f : 26f);
            if (token == null || token.Type == JTokenType.Null)
            {
                string value = GetPrimitiveBuffer(key, string.Empty);
                string next = GUI.TextField(valueRect, value, textFieldStyle);
                if (!string.Equals(value, next, StringComparison.Ordinal))
                {
                    primitiveBuffers[key] = next;
                    currentObject[key] = next;
                    dirty = true;
                }
            }
            else if (token.Type == JTokenType.Boolean)
            {
                bool oldValue = token.Value<bool>();
                bool nextValue = GUI.Toggle(valueRect, oldValue, oldValue ? "true" : "false", toggleStyle);
                if (nextValue != oldValue)
                {
                    currentObject[key] = nextValue;
                    dirty = true;
                }
            }
            else if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
            {
                string value = GetPrimitiveBuffer(key, token.ToString(Formatting.None));
                string next = GUI.TextField(valueRect, value, textFieldStyle);
                if (!string.Equals(value, next, StringComparison.Ordinal))
                {
                    primitiveBuffers[key] = next;
                    JToken parsed;
                    if (TryParseNumber(next, token.Type, out parsed))
                    {
                        currentObject[key] = parsed;
                        status = string.Empty;
                    }
                    else
                    {
                        status = "Некорректное число в поле «" + key + "». Исправь значение перед сохранением.";
                    }
                    dirty = true;
                }
            }
            else if (token.Type == JTokenType.String)
            {
                string value = GetPrimitiveBuffer(key, token.Value<string>() ?? string.Empty);
                string next = GUI.TextField(valueRect, value, textFieldStyle);
                if (!string.Equals(value, next, StringComparison.Ordinal))
                {
                    primitiveBuffers[key] = next;
                    currentObject[key] = next;
                    dirty = true;
                }
            }
            else
            {
                string buffer;
                if (!complexBuffers.TryGetValue(key, out buffer))
                {
                    buffer = token.ToString(Formatting.Indented);
                    complexBuffers[key] = buffer;
                }
                string next = GUI.TextArea(valueRect, buffer, textAreaStyle);
                if (!string.Equals(buffer, next, StringComparison.Ordinal))
                {
                    complexBuffers[key] = next;
                    dirty = true;
                }
                if (GUI.Button(new Rect(keyWidth, y + 91, 118, 26), "ПРИМЕНИТЬ JSON", secondaryButtonStyle))
                {
                    try
                    {
                        JToken parsed = JToken.Parse(complexBuffers[key]);
                        currentObject[key] = parsed;
                        complexBuffers.Remove(key);
                        status = "JSON поля «" + key + "» применён.";
                        dirty = true;
                    }
                    catch (Exception exception)
                    {
                        status = "Ошибка JSON в «" + key + "»: " + exception.Message;
                    }
                }
            }

            return height + 6f;
        }

        private void DrawStatus(Rect panel)
        {
            string dirtyLabel = dirty ? "● ЕСТЬ НЕСОХРАНЁННЫЕ ИЗМЕНЕНИЯ" : "● ВСЁ СОХРАНЕНО";
            Color statusColor = dirty ? new Color(1f, 0.72f, 0.34f) : new Color(0.42f, 0.75f, 0.57f);
            GUI.Label(new Rect(panel.x + 14, panel.yMax - 35, panel.width - 28, 18), dirtyLabel + (string.IsNullOrEmpty(status) ? string.Empty : "   |   " + status), TEditorMenuController.MakeLabelStyle(10, statusColor, TextAnchor.MiddleLeft));
        }

        private void SwitchCategory(ResourceCategory next)
        {
            if (category == next)
                return;
            if (dirty)
            {
                status = "Сначала сохраните изменения или нажмите «Отменить правки».";
                return;
            }
            category = next;
            search = string.Empty;
            selectedEntry = null;
            currentObject = null;
            currentFilePath = null;
            RefreshCatalog(null);
        }

        private void SelectEntry(CatalogEntry entry)
        {
            if (selectedEntry != null && string.Equals(selectedEntry.Path, entry.Path, StringComparison.OrdinalIgnoreCase))
                return;
            if (dirty)
            {
                status = "Есть несохранённые изменения. Сохрани их или нажми «Отменить правки».";
                return;
            }
            LoadEntry(entry);
        }

        private void LoadEntry(CatalogEntry entry)
        {
            try
            {
                currentObject = JObject.Parse(File.ReadAllText(entry.Path));
                currentFilePath = entry.Path;
                selectedEntry = entry;
                linkedFields = false;
                linkedText = GetString(currentObject, "Name", GetString(currentObject, "ID", string.Empty));
                primitiveBuffers.Clear();
                complexBuffers.Clear();
                propertyScroll = Vector2.zero;
                dirty = false;
                status = "Загружено: " + entry.Id;
            }
            catch (Exception exception)
            {
                currentObject = null;
                currentFilePath = null;
                selectedEntry = null;
                status = "Не удалось открыть JSON: " + exception.Message;
            }
        }

        private void RefreshCatalog(string preferredPath)
        {
            entries.Clear();
            BuildTextureIndex();
            string folder = GetCurrentDirectory();
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string[] files = Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    JObject json = JObject.Parse(File.ReadAllText(files[i]));
                    CatalogEntry entry = new CatalogEntry
                    {
                        Path = files[i],
                        Id = GetString(json, "ID", Path.GetFileNameWithoutExtension(files[i])),
                        Name = GetString(json, "Name", GetString(json, "ID", Path.GetFileNameWithoutExtension(files[i]))),
                        Texture = GetString(json, "Texture", string.Empty)
                    };
                    entries.Add(entry);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("T-EDITOR: Пропущен JSON " + files[i] + " — " + exception.Message);
                }
            }
            entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            RebuildFilteredEntries();

            string target = preferredPath;
            if (string.IsNullOrEmpty(target) && currentFilePath != null)
                target = currentFilePath;
            CatalogEntry match = entries.FirstOrDefault(e => !string.IsNullOrEmpty(target) && string.Equals(e.Path, target, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                LoadEntry(match);
            else if (!string.IsNullOrEmpty(target))
            {
                // The selected JSON may have been deleted outside the editor.
                // Drop the stale in-memory entry and select a currently existing file.
                currentObject = null;
                currentFilePath = null;
                selectedEntry = null;
                dirty = false;
                if (entries.Count > 0)
                    LoadEntry(entries[0]);
                else
                    status = "Выбранный JSON удалён. В этой категории больше нет записей.";
            }
            else if (entries.Count > 0 && currentObject == null)
                LoadEntry(entries[0]);
            else if (entries.Count == 0)
            {
                currentObject = null;
                currentFilePath = null;
                selectedEntry = null;
                dirty = false;
                status = "В этой категории пока нет JSON-файлов.";
            }
        }

        private void BuildTextureIndex()
        {
            foreach (Texture2D texture in textureCache.Values)
                if (texture != null) Destroy(texture);
            textureCache.Clear();
            texturePathsByName.Clear();
            if (!Directory.Exists(TextureRoot))
                return;

            string[] files = Directory.GetFiles(TextureRoot, "*.png", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string name = Path.GetFileNameWithoutExtension(files[i]);
                List<string> paths;
                if (!texturePathsByName.TryGetValue(name, out paths))
                {
                    paths = new List<string>();
                    texturePathsByName.Add(name, paths);
                }
                paths.Add(files[i]);
            }
        }

        private void RebuildFilteredEntries()
        {
            filteredEntries.Clear();
            string query = (search ?? string.Empty).Trim();
            for (int i = 0; i < entries.Count; i++)
            {
                CatalogEntry entry = entries[i];
                if (string.IsNullOrEmpty(query) ||
                    (!string.IsNullOrEmpty(entry.Name) && entry.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(entry.Id) && entry.Id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrEmpty(entry.Texture) && entry.Texture.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    filteredEntries.Add(entry);
                }
            }
        }

        private Texture2D GetIcon(CatalogEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Texture))
                return null;
            string path = ResolveTexturePath(entry.Texture);
            if (string.IsNullOrEmpty(path))
                return null;
            Texture2D texture;
            if (textureCache.TryGetValue(path, out texture))
                return texture;
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                {
                    Destroy(texture);
                    textureCache[path] = null;
                    return null;
                }
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.hideFlags = HideFlags.HideAndDontSave;
                textureCache[path] = texture;
                return texture;
            }
            catch
            {
                textureCache[path] = null;
                return null;
            }
        }

        private string ResolveTexturePath(string textureName)
        {
            string key = textureName.Trim().Replace('\\', '/');
            if (key.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                key = key.Substring(0, key.Length - 4);

            if (Path.IsPathRooted(key) && File.Exists(key))
                return key;

            string relative = key;
            if (relative.StartsWith("textures/", StringComparison.OrdinalIgnoreCase))
                relative = relative.Substring("textures/".Length);
            string direct = Path.Combine(TextureRoot, relative.Replace('/', Path.DirectorySeparatorChar) + ".png");
            if (File.Exists(direct))
                return direct;

            string stem = Path.GetFileNameWithoutExtension(key);
            List<string> candidates;
            if (!texturePathsByName.TryGetValue(stem, out candidates) || candidates.Count == 0)
                return null;

            if (showMobs)
                return candidates.FirstOrDefault(p => p.IndexOf(Path.DirectorySeparatorChar + "Entity" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) >= 0) ?? candidates[0];
            if (category == ResourceCategory.Blocks)
                return candidates.FirstOrDefault(p => p.IndexOf(Path.DirectorySeparatorChar + "blocks" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) >= 0) ?? candidates[0];
            return candidates.FirstOrDefault(p => p.IndexOf(Path.DirectorySeparatorChar + "items" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) >= 0)
                ?? candidates.FirstOrDefault(p => p.IndexOf(Path.DirectorySeparatorChar + "ItemTextures" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) >= 0)
                ?? candidates.FirstOrDefault(p => p.IndexOf(Path.DirectorySeparatorChar + "blocks" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) >= 0)
                ?? candidates[0];
        }

        private void CreateNewResource()
        {
            if (dirty)
            {
                status = "Сначала сохраните текущий ресурс или отмените правки.";
                return;
            }

            currentFilePath = null;
            selectedEntry = null;
            primitiveBuffers.Clear();
            complexBuffers.Clear();
            linkedFields = false;
            propertyScroll = Vector2.zero;
            dirty = true;

            if (showMobs)
            {
                currentObject = JObject.Parse(@"{
  'ID': 'game:new_mob',
  'Name': 'New Mob',
  'Enabled': true,
  'Behavior': 'Aggressive',
  'MaxHealth': 20.0,
  'ColliderWidth': 0.8,
  'ColliderHeight': 1.8,
  'WanderRadius': 8.0,
  'Movement': { 'Type': 'Ground', 'Speed': 2.5, 'Acceleration': 18.0, 'Gravity': 25.0, 'MaxFallSpeed': 20.0, 'JumpForce': 8.0 },
  'Combat': { 'VisionRange': 12.0, 'LoseTargetRange': 18.0, 'BaseDamage': 5.0, 'AttackRange': 1.5, 'AttackCooldown': 1.0 },
  'Spawn': { 'Enabled': true, 'MaxAlive': 4, 'Weight': 1.0, 'MinLight': 0.0, 'MaxLight': 1.0, 'MinDistanceFromPlayer': 12.0, 'MaxDistanceFromPlayer': 38.0 },
  'Visual': { 'PrefabResource': '', 'PrefabScale': 1.0, 'AnimatorIdleState': 'Idle', 'AnimatorWalkState': 'Walk' },
  'Loot': []
}");
            }
            else if (category == ResourceCategory.Blocks)
            {
                currentObject = JObject.Parse(@"{
  'ID': 'game:new_block',
  'Name': 'New Block',
  'Texture': 'new_block',
  'Hardness': 1.0,
  'ExplosionResistance': 1.0,
  'Solid': true,
  'Transparent': false,
  'BlocksLight': true,
  'LightOpacity': 15,
  'LightEmissionR': 0,
  'LightEmissionG': 0,
  'LightEmissionB': 0,
  'Material': 0,
  'Drop': 'game:new_block',
  'DropCount': 1,
  'PlaceSound': '',
  'BreakSound': '',
  'RequiredTool': '',
  'Tags': [],
  'CollisionShape': 'Full',
  'CollisionRects': [],
  'CraftIngredients': [],
  'CraftWithoutWorkbench': false,
  'CraftResultCount': 1,
  'RequiredToolLevel': 1
}");
            }
            else
            {
                currentObject = JObject.Parse(@"{
  'ID': 'game:new_item',
  'Name': 'New Item',
  'Texture': 'new_item',
  'Type': 'Material',
  'MaxStack': 100,
  'Tags': [],
  'CraftIngredients': [],
  'CraftWithoutWorkbench': false,
  'CraftResultCount': 1
}");
            }

            linkedText = GetString(currentObject, "Name", string.Empty);
            status = "Создан новый шаблон. Заполни поля и нажми «Сохранить».";
        }

        private void DrawAddFieldPopup()
        {
            if (!addFieldPopup || currentObject == null)
                return;

            Rect rect = new Rect(Screen.width * 0.5f - 230f, Screen.height * 0.5f - 135f, 460f, 270f);
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, panelStyle);
            GUI.Box(rect, GUIContent.none, selectedStyle);
            GUI.Label(new Rect(rect.x + 18, rect.y + 14, rect.width - 36, 30), "ДОБАВИТЬ JSON-ПОЛЕ", TEditorMenuController.MakeLabelStyle(18, Color.white, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(rect.x + 18, rect.y + 54, rect.width - 36, 20), "Название поля", smallLabelStyle);
            pendingFieldName = GUI.TextField(new Rect(rect.x + 18, rect.y + 78, rect.width - 36, 30), pendingFieldName, textFieldStyle);
            GUI.Label(new Rect(rect.x + 18, rect.y + 116, rect.width - 36, 20), "Тип значения", smallLabelStyle);
            pendingFieldType = GUI.SelectionGrid(
                new Rect(rect.x + 18, rect.y + 140, rect.width - 36, 60),
                pendingFieldType,
                new[] { "Строка", "Число", "Bool", "Массив []", "Объект {}" },
                3,
                secondaryButtonStyle
            );

            if (GUI.Button(new Rect(rect.x + 18, rect.yMax - 48, 180, 32), "СОЗДАТЬ ПОЛЕ", buttonStyle))
            {
                AddField(pendingFieldName, pendingFieldType);
                addFieldPopup = false;
            }
            if (GUI.Button(new Rect(rect.xMax - 158, rect.yMax - 48, 140, 32), "ОТМЕНА", secondaryButtonStyle))
                addFieldPopup = false;
        }

        private void AddField(string requestedName, int type)
        {
            if (currentObject == null)
                return;
            string name = string.IsNullOrWhiteSpace(requestedName) ? "NewField" : requestedName.Trim();
            string candidate = name;
            int suffix = 2;
            while (currentObject.Property(candidate) != null)
                candidate = name + suffix++;

            switch (type)
            {
                case 1:
                    currentObject[candidate] = new JValue(0.0);
                    break;
                case 2:
                    currentObject[candidate] = new JValue(false);
                    break;
                case 3:
                    currentObject[candidate] = new JArray();
                    break;
                case 4:
                    currentObject[candidate] = new JObject();
                    break;
                default:
                    currentObject[candidate] = new JValue(string.Empty);
                    break;
            }

            dirty = true;
            status = "Добавлено поле «" + candidate + "». Название и значение можно изменить в строке.";
        }

        private void RenameProperty(string oldKey, string newKey)
        {
            newKey = (newKey ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(newKey) || string.Equals(oldKey, newKey, StringComparison.Ordinal))
                return;
            if (currentObject.Property(newKey) != null)
            {
                status = "Поле «" + newKey + "» уже существует.";
                return;
            }
            JProperty oldProperty = currentObject.Property(oldKey);
            if (oldProperty == null)
                return;
            JToken value = oldProperty.Value.DeepClone();
            oldProperty.Remove();
            currentObject[newKey] = value;
            MoveBuffer(primitiveBuffers, oldKey, newKey);
            MoveBuffer(complexBuffers, oldKey, newKey);
            dirty = true;
        }

        private static void MoveBuffer(Dictionary<string, string> buffers, string oldKey, string newKey)
        {
            string value;
            if (buffers.TryGetValue(oldKey, out value))
            {
                buffers.Remove(oldKey);
                buffers[newKey] = value;
            }
        }

        private void ApplyLinkedFields(string value)
        {
            if (currentObject == null)
                return;
            string slug = MakeSlug(value);
            string oldId = GetString(currentObject, "ID", "game:new_resource");
            string namespaceId = "game";
            int separator = oldId.IndexOf(':');
            if (separator > 0)
                namespaceId = oldId.Substring(0, separator);
            currentObject["Name"] = value;
            currentObject["Texture"] = slug;
            currentObject["ID"] = namespaceId + ":" + slug;
        }

        private void SaveCurrent()
        {
            if (currentObject == null)
                return;
            if (!Application.isEditor)
            {
                status = "Сохранение разрешено только внутри Unity Editor.";
                return;
            }

            foreach (KeyValuePair<string, string> pair in complexBuffers.ToList())
            {
                if (currentObject.Property(pair.Key) == null)
                    continue;
                try
                {
                    currentObject[pair.Key] = JToken.Parse(pair.Value);
                }
                catch (Exception exception)
                {
                    status = "Некорректный JSON в поле «" + pair.Key + "»: " + exception.Message;
                    return;
                }
            }
            complexBuffers.Clear();

            foreach (KeyValuePair<string, string> pair in primitiveBuffers)
            {
                if (currentObject.Property(pair.Key) == null)
                    continue;
                JToken token = currentObject[pair.Key];
                if (token != null && (token.Type == JTokenType.Integer || token.Type == JTokenType.Float))
                {
                    JToken parsed;
                    if (!TryParseNumber(pair.Value, token.Type, out parsed))
                    {
                        status = "Сначала исправь некорректное число в поле «" + pair.Key + "».";
                        return;
                    }
                    currentObject[pair.Key] = parsed;
                }
            }

            string id = GetString(currentObject, "ID", string.Empty).Trim();
            int namespaceSeparator = id.IndexOf(':');
            if (string.IsNullOrEmpty(id) || namespaceSeparator <= 0 || namespaceSeparator >= id.Length - 1)
            {
                status = "Поле ID должно иметь формат namespace:name, например game:stone.";
                return;
            }
            if (string.IsNullOrWhiteSpace(GetString(currentObject, "Name", string.Empty)))
            {
                status = "Поле Name не может быть пустым.";
                return;
            }

            string folder = GetCurrentDirectory();
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string[] existingFiles = Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories);
            for (int i = 0; i < existingFiles.Length; i++)
            {
                if (!string.IsNullOrEmpty(currentFilePath) && string.Equals(existingFiles[i], currentFilePath, StringComparison.OrdinalIgnoreCase))
                    continue;
                try
                {
                    JObject other = JObject.Parse(File.ReadAllText(existingFiles[i]));
                    if (string.Equals(GetString(other, "ID", string.Empty), id, StringComparison.OrdinalIgnoreCase))
                    {
                        status = "ID «" + id + "» уже используется в " + Path.GetFileName(existingFiles[i]) + ".";
                        return;
                    }
                }
                catch { }
            }

            string destination = currentFilePath;
            if (string.IsNullOrEmpty(destination))
            {
                string fileName = MakeSlug(id.Substring(id.IndexOf(':') + 1)) + ".json";
                destination = Path.Combine(folder, fileName);
                int suffix = 2;
                while (File.Exists(destination))
                {
                    destination = Path.Combine(folder, MakeSlug(id.Substring(id.IndexOf(':') + 1)) + "_" + suffix++ + ".json");
                }
            }

            try
            {
                File.WriteAllText(destination, currentObject.ToString(Formatting.Indented), new UTF8Encoding(false));
#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
#endif
                currentFilePath = destination;
                dirty = false;
                primitiveBuffers.Clear();
                complexBuffers.Clear();
                status = "Сохранено: " + MakeProjectRelativePath(destination);
                RefreshCatalog(destination);
            }
            catch (Exception exception)
            {
                status = "Не удалось сохранить JSON: " + exception.Message;
            }
        }

        private void DiscardChanges()
        {
            if (!string.IsNullOrEmpty(currentFilePath) && File.Exists(currentFilePath))
            {
                CatalogEntry entry = new CatalogEntry
                {
                    Path = currentFilePath,
                    Id = GetString(currentObject, "ID", Path.GetFileNameWithoutExtension(currentFilePath)),
                    Name = GetString(currentObject, "Name", Path.GetFileNameWithoutExtension(currentFilePath)),
                    Texture = GetString(currentObject, "Texture", string.Empty)
                };
                LoadEntry(entry);
                return;
            }
            currentObject = null;
            currentFilePath = null;
            selectedEntry = null;
            dirty = false;
            primitiveBuffers.Clear();
            complexBuffers.Clear();
            RefreshCatalog(null);
            status = "Несохранённые изменения отменены.";
        }

        private string GetCurrentDirectory()
        {
            if (showMobs)
                return Path.Combine(GameDataPath, "Entities", "Definitions");
            return Path.Combine(GameDataPath, category == ResourceCategory.Blocks ? "Blocks" : "Items");
        }

        private static bool IsLinkedKey(string key)
        {
            return string.Equals(key, "ID", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(key, "Texture", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsComplex(JToken token)
        {
            return token != null && (token.Type == JTokenType.Array || token.Type == JTokenType.Object);
        }

        private string GetPrimitiveBuffer(string key, string fallback)
        {
            string value;
            if (primitiveBuffers.TryGetValue(key, out value))
                return value;
            primitiveBuffers[key] = fallback ?? string.Empty;
            return primitiveBuffers[key];
        }

        private static bool TryParseNumber(string value, JTokenType originalType, out JToken token)
        {
            token = null;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            JToken parsed;
            try { parsed = JToken.Parse(value.Trim()); }
            catch { return false; }
            if (parsed.Type != JTokenType.Integer && parsed.Type != JTokenType.Float)
                return false;
            if (originalType == JTokenType.Integer && parsed.Type != JTokenType.Integer)
                return false;
            token = parsed;
            return true;
        }

        private static string GetString(JObject obj, string key, string fallback)
        {
            if (obj == null || obj[key] == null || obj[key].Type == JTokenType.Null)
                return fallback;
            return obj[key].ToString();
        }

        private static string MakeSlug(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "new_resource";

            StringBuilder builder = new StringBuilder();
            bool previousUnderscore = false;
            string lower = value.Trim().ToLowerInvariant();
            for (int i = 0; i < lower.Length; i++)
            {
                char c = lower[i];
                string transliterated = TransliterateCyrillic(c);
                if (transliterated != null)
                {
                    builder.Append(transliterated);
                    previousUnderscore = false;
                    continue;
                }

                bool asciiLetterOrDigit = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
                if (asciiLetterOrDigit || c == '-')
                {
                    builder.Append(c);
                    previousUnderscore = false;
                }
                else if (c == '_' || char.IsWhiteSpace(c) || c == '.' || c == '/')
                {
                    if (!previousUnderscore && builder.Length > 0)
                        builder.Append('_');
                    previousUnderscore = true;
                }
            }
            string result = builder.ToString().Trim('_', '-');
            return string.IsNullOrEmpty(result) ? "new_resource" : result;
        }

        private static string TransliterateCyrillic(char c)
        {
            switch (c)
            {
                case 'а': return "a"; case 'б': return "b"; case 'в': return "v";
                case 'г': return "g"; case 'д': return "d"; case 'е': return "e";
                case 'ё': return "yo"; case 'ж': return "zh"; case 'з': return "z";
                case 'и': return "i"; case 'й': return "y"; case 'к': return "k";
                case 'л': return "l"; case 'м': return "m"; case 'н': return "n";
                case 'о': return "o"; case 'п': return "p"; case 'р': return "r";
                case 'с': return "s"; case 'т': return "t"; case 'у': return "u";
                case 'ф': return "f"; case 'х': return "h"; case 'ц': return "ts";
                case 'ч': return "ch"; case 'ш': return "sh"; case 'щ': return "sch";
                case 'ы': return "y"; case 'э': return "e"; case 'ю': return "yu";
                case 'я': return "ya"; case 'ъ': return string.Empty; case 'ь': return string.Empty;
                default: return null;
            }
        }

        private static string Clip(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max)
                return value ?? string.Empty;
            return value.Substring(0, Mathf.Max(1, max - 1)) + "…";
        }

        private static string MakeProjectRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            string root = Game.Content.DataPaths.GameDataFolder.Replace('\\', '/') + "/";
            string normalized = path.Replace('\\', '/');
            if (normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return "Assets/" + normalized.Substring(root.Length);
            return normalized;
        }

        private void EnsureStyles()
        {
            if (backgroundTexture != null)
                return;
            backgroundTexture = MakeTexture(new Color(0.055f, 0.063f, 0.078f));
            panelTexture = MakeTexture(new Color(0.085f, 0.098f, 0.12f));
            rowTexture = MakeTexture(new Color(0.105f, 0.12f, 0.145f));
            selectedTexture = MakeTexture(new Color(0.15f, 0.29f, 0.37f));
            buttonTexture = MakeTexture(new Color(0.13f, 0.15f, 0.18f));
            hoverTexture = MakeTexture(new Color(0.20f, 0.34f, 0.43f));
            accentTexture = MakeTexture(new Color(0.19f, 0.39f, 0.51f));

            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = panelTexture;
            panelStyle.border = new RectOffset(1, 1, 1, 1);

            rowStyle = new GUIStyle(GUI.skin.box);
            rowStyle.normal.background = rowTexture;
            rowStyle.border = new RectOffset(1, 1, 1, 1);

            selectedStyle = new GUIStyle(GUI.skin.button);
            selectedStyle.normal.background = selectedTexture;
            selectedStyle.hover.background = hoverTexture;
            selectedStyle.active.background = accentTexture;
            selectedStyle.normal.textColor = Color.white;

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 12;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.normal.background = accentTexture;
            buttonStyle.hover.background = hoverTexture;
            buttonStyle.active.background = selectedTexture;
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.alignment = TextAnchor.MiddleCenter;

            secondaryButtonStyle = new GUIStyle(buttonStyle);
            secondaryButtonStyle.normal.background = buttonTexture;
            secondaryButtonStyle.fontSize = 11;

            textFieldStyle = new GUIStyle(GUI.skin.textField);
            textFieldStyle.fontSize = 12;
            textFieldStyle.padding = new RectOffset(7, 7, 4, 4);
            textFieldStyle.normal.textColor = Color.white;
            textFieldStyle.normal.background = buttonTexture;
            textFieldStyle.focused.background = selectedTexture;

            textAreaStyle = new GUIStyle(GUI.skin.textArea);
            textAreaStyle.fontSize = 11;
            textAreaStyle.wordWrap = false;
            textAreaStyle.normal.textColor = Color.white;
            textAreaStyle.normal.background = buttonTexture;
            textAreaStyle.focused.background = selectedTexture;

            smallLabelStyle = TEditorMenuController.MakeLabelStyle(10, new Color(0.59f, 0.64f, 0.72f), TextAnchor.MiddleLeft);
            tileNameStyle = TEditorMenuController.MakeLabelStyle(10, Color.white, TextAnchor.MiddleCenter);
            tileIdStyle = TEditorMenuController.MakeLabelStyle(8, new Color(0.58f, 0.63f, 0.70f), TextAnchor.MiddleCenter);
            toggleStyle = new GUIStyle(GUI.skin.toggle);
            toggleStyle.normal.textColor = Color.white;
            toggleStyle.onNormal.textColor = Color.white;
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        private void OnDestroy()
        {
            foreach (Texture2D texture in textureCache.Values)
                if (texture != null) Destroy(texture);
            textureCache.Clear();
            DestroyTexture(backgroundTexture);
            DestroyTexture(panelTexture);
            DestroyTexture(rowTexture);
            DestroyTexture(selectedTexture);
            DestroyTexture(buttonTexture);
            DestroyTexture(hoverTexture);
            DestroyTexture(accentTexture);
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture != null)
                Destroy(texture);
        }
    }
}
