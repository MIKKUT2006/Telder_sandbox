using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Game.Fluids;
using Game.Save;
using Game.World.Dimensions;

namespace Game.World.Fluids
{
    [DefaultExecutionOrder(1200)]
    public sealed class LiquidRuntime : MonoBehaviour
    {
        [Serializable] private class CellSave { public int X; public int Y; public string Fluid; public int Amount; }
        [Serializable] private class LiquidSaveFile { public List<CellSave> Cells = new List<CellSave>(); }

        private static LiquidRuntime instance;
        private World world;
        private readonly Queue<long> active = new Queue<long>();
        private readonly HashSet<long> activeSet = new HashSet<long>();
        private readonly Dictionary<Vector2Int, GameObject> visuals = new Dictionary<Vector2Int, GameObject>();
        private readonly Dictionary<long, CellSave> saved = new Dictionary<long, CellSave>();
        private readonly HashSet<Vector2Int> seenChunks = new HashSet<Vector2Int>();
        private float tickTimer, saveTimer, hazardTimer;
        private string loadedDimensionKey = "";
        private Material material;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) return;
            GameObject go = new GameObject("LiquidRuntime");
            instance = go.AddComponent<LiquidRuntime>();
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            Shader sh = Shader.Find("Sprites/Default");
            if (sh != null) material = new Material(sh);
        }

        private void Update()
        {
            WorldManager wm = WorldManager.Instance;
            if (wm == null || !wm.IsReady) return;
            world = wm.GetWorld();
            EnsureDimensionLoaded();
            DiscoverChunks();

            tickTimer += Time.deltaTime;
            if (tickTimer >= 0.08f) { tickTimer = 0f; Simulate(700); }

            hazardTimer += Time.deltaTime;
            if (hazardTimer >= 0.12f) { hazardTimer = 0f; UpdatePlayerHazard(); }

            saveTimer += Time.deltaTime;
            if (saveTimer >= 5f) { saveTimer = 0f; Flush(); }
        }

        private void EnsureDimensionLoaded()
        {
            DimensionDefinition d = DimensionTravelRuntime.Current;
            string key = d == null ? "" : d.Name + "_" + d.Seed;
            if (key == loadedDimensionKey) return;

            Flush();
            loadedDimensionKey = key;
            saved.Clear(); seenChunks.Clear(); ClearVisuals(); active.Clear(); activeSet.Clear();

            if (!SaveGameRuntime.HasActiveSave || string.IsNullOrWhiteSpace(key)) return;
            string path = SavePath();
            if (!File.Exists(path)) return;

            try
            {
                LiquidSaveFile f = JsonUtility.FromJson<LiquidSaveFile>(File.ReadAllText(path));
                if (f != null && f.Cells != null)
                    for (int i = 0; i < f.Cells.Count; i++)
                    {
                        CellSave c = f.Cells[i];
                        if (c != null) saved[Pack(c.X, c.Y)] = c;
                    }
            }
            catch (Exception e) { Debug.LogWarning("LIQUID: save load failed: " + e.Message); }
        }

        private void DiscoverChunks()
        {
            foreach (var pair in world.GetLoadedChunks())
            {
                Vector2Int p = new Vector2Int(pair.Key.x, pair.Key.y);
                if (!seenChunks.Add(p)) continue;
                Chunk c = pair.Value;
                int sx = c.X * Chunk.SizeX, sy = c.Y * Chunk.SizeY;

                for (int x = 0; x < Chunk.SizeX; x++)
                    for (int y = 0; y < Chunk.SizeY; y++)
                    {
                        int wx = sx + x, wy = sy + y;
                        long k = Pack(wx, wy);
                        if (saved.TryGetValue(k, out CellSave sv))
                        {
                            ushort fid = string.IsNullOrWhiteSpace(sv.Fluid) ? (ushort)0 : FluidIDRegistry.GetOrRegister(sv.Fluid);
                            c.SetLiquid(x, y, fid, (byte)Mathf.Clamp(sv.Amount, 0, 8));
                        }
                        if (c.GetLiquidAmount(x, y) > 0) Activate(wx, wy);
                    }

                RebuildChunkVisual(c.X, c.Y);
            }

            List<Vector2Int> remove = null;
            foreach (var kv in visuals)
            {
                if (world.GetChunk(kv.Key.x, kv.Key.y) == null)
                {
                    if (remove == null) remove = new List<Vector2Int>();
                    remove.Add(kv.Key);
                }
            }
            if (remove != null)
                for (int i = 0; i < remove.Count; i++)
                {
                    Destroy(visuals[remove[i]]);
                    visuals.Remove(remove[i]);
                    seenChunks.Remove(remove[i]);
                }
        }

        private void Simulate(int budget)
        {
            HashSet<Vector2Int> dirty = new HashSet<Vector2Int>();
            while (budget-- > 0 && active.Count > 0)
            {
                long key = active.Dequeue();
                activeSet.Remove(key);
                Unpack(key, out int x, out int y);

                byte amount = world.GetLiquidAmount(x, y);
                ushort id = world.GetLiquidID(x, y);
                if (amount == 0 || id == 0) continue;

                if (world.GetBlock(x, y) != 0)
                {
                    Set(x, y, 0, 0, dirty);
                    continue;
                }

                if (CanOccupy(x, y - 1, id))
                {
                    byte below = world.GetLiquidAmount(x, y - 1);
                    int move = Mathf.Min(amount, 8 - below);
                    if (move > 0)
                    {
                        Set(x, y, id, (byte)(amount - move), dirty);
                        Set(x, y - 1, id, (byte)(below + move), dirty);
                        WakeAround(x, y); WakeAround(x, y - 1);
                        continue;
                    }
                }

                int first = ((x + y) & 1) == 0 ? -1 : 1;
                FlowSide(x, y, id, first, dirty);
                FlowSide(x, y, id, -first, dirty);
            }

            foreach (Vector2Int p in dirty) RebuildChunkVisual(p.x, p.y);
        }

        private void FlowSide(int x, int y, ushort id, int dx, HashSet<Vector2Int> dirty)
        {
            byte a = world.GetLiquidAmount(x, y);
            if (a <= 1 || !CanOccupy(x + dx, y, id)) return;
            byte b = world.GetLiquidAmount(x + dx, y);
            if (a <= b + 1) return;

            int move = Mathf.Max(1, (a - b) / 2);
            move = Mathf.Min(move, a - 1);
            move = Mathf.Min(move, 8 - b);
            if (move <= 0) return;

            Set(x, y, id, (byte)(a - move), dirty);
            Set(x + dx, y, id, (byte)(b + move), dirty);
            WakeAround(x, y); WakeAround(x + dx, y);
        }

        private bool CanOccupy(int x, int y, ushort id)
        {
            Chunk c = GetChunkFor(x, y);
            if (c == null || world.GetBlock(x, y) != 0) return false;
            ushort other = world.GetLiquidID(x, y);
            return other == 0 || other == id;
        }

        private Chunk GetChunkFor(int x, int y)
        {
            int cx = FloorDiv(x, Chunk.SizeX), cy = FloorDiv(y, Chunk.SizeY);
            return world.GetChunk(cx, cy);
        }

        private void Set(int x, int y, ushort id, byte amount, HashSet<Vector2Int> dirty)
        {
            if (!world.SetLiquid(x, y, id, amount)) return;
            int cx = FloorDiv(x, Chunk.SizeX), cy = FloorDiv(y, Chunk.SizeY);
            dirty.Add(new Vector2Int(cx, cy));
            saved[Pack(x, y)] = new CellSave
            {
                X = x,
                Y = y,
                Fluid = amount == 0 ? "" : FluidIDRegistry.GetString(id),
                Amount = amount
            };
        }

        private void Activate(int x, int y)
        {
            long k = Pack(x, y);
            if (activeSet.Add(k)) active.Enqueue(k);
        }

        private void WakeAround(int x, int y)
        {
            Activate(x, y); Activate(x, y - 1); Activate(x, y + 1); Activate(x - 1, y); Activate(x + 1, y);
        }

        public static void NotifyCellChanged(int x, int y)
        {
            if (instance == null) return;
            instance.WakeAround(x, y);
            if (instance.world != null)
                instance.RebuildChunkVisual(FloorDiv(x, Chunk.SizeX), FloorDiv(y, Chunk.SizeY));
        }

        private void UpdatePlayerHazard()
        {
            Game.PlayerStats.PlayerStats ps = FindFirstObjectByType<Game.PlayerStats.PlayerStats>();
            if (ps == null) return;
            Vector3 p = ps.transform.position;
            int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y);
            byte amount = world.GetLiquidAmount(x, y);
            ushort id = world.GetLiquidID(x, y);
            if (amount == 0 || id == 0) return;

            string sid = FluidIDRegistry.GetString(id);
            if (!FluidRegistry.TryGet(sid, out FluidDefinition def) || def.Damage <= 0f) return;
            float interval = Mathf.Max(0.05f, def.DamageInterval);
            ps.TakeDamage(def.Damage * (0.12f / interval));
        }

        private void RebuildChunkVisual(int cx, int cy)
        {
            if (world == null) return;
            Chunk c = world.GetChunk(cx, cy);
            if (c == null) return;

            Vector2Int key = new Vector2Int(cx, cy);
            if (visuals.TryGetValue(key, out GameObject old))
            {
                Destroy(old); visuals.Remove(key);
            }

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Color> colors = new List<Color>();

            for (int x = 0; x < Chunk.SizeX; x++)
                for (int y = 0; y < Chunk.SizeY; y++)
                {
                    byte amount = c.GetLiquidAmount(x, y);
                    ushort id = c.GetLiquidID(x, y);
                    if (amount == 0 || id == 0) continue;

                    string sid = FluidIDRegistry.GetString(id);
                    Color col = new Color(0.2f, 0.55f, 1f, 0.72f);
                    if (FluidRegistry.TryGet(sid, out FluidDefinition def))
                        ColorUtility.TryParseHtmlString(def.Color, out col);

                    float h = Mathf.Clamp01(amount / 8f);
                    int n = verts.Count;
                    verts.Add(new Vector3(x, y, 0));
                    verts.Add(new Vector3(x + 1, y, 0));
                    verts.Add(new Vector3(x + 1, y + h, 0));
                    verts.Add(new Vector3(x, y + h, 0));
                    colors.Add(col); colors.Add(col); colors.Add(col); colors.Add(col);
                    tris.Add(n); tris.Add(n + 2); tris.Add(n + 1);
                    tris.Add(n); tris.Add(n + 3); tris.Add(n + 2);
                }

            if (verts.Count == 0) return;

            GameObject go = new GameObject("Liquids_" + cx + "_" + cy);
            go.transform.position = new Vector3(cx * Chunk.SizeX, cy * Chunk.SizeY, -0.15f);
            Mesh m = new Mesh();
            m.name = "LiquidMesh";
            m.SetVertices(verts); m.SetTriangles(tris, 0); m.SetColors(colors); m.RecalculateBounds();
            MeshFilter mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = m;
            MeshRenderer mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = material; mr.sortingOrder = 5;
            visuals[key] = go;
        }

        public void Flush()
        {
            if (!SaveGameRuntime.HasActiveSave || saved.Count == 0 || string.IsNullOrWhiteSpace(loadedDimensionKey)) return;
            try
            {
                Directory.CreateDirectory(SavePaths.GetSaveFolder(SaveGameRuntime.CurrentSaveId));
                LiquidSaveFile f = new LiquidSaveFile();
                foreach (CellSave c in saved.Values) f.Cells.Add(c);
                File.WriteAllText(SavePath(), JsonUtility.ToJson(f, true));
            }
            catch (Exception e) { Debug.LogWarning("LIQUID: save failed: " + e.Message); }
        }

        private string SavePath()
        {
            string safe = loadedDimensionKey.Replace('/', '_').Replace('\\', '_').Replace(':', '_');
            return Path.Combine(SavePaths.GetSaveFolder(SaveGameRuntime.CurrentSaveId), "liquids_" + safe + ".json");
        }

        private void OnApplicationQuit() { Flush(); }
        private void ClearVisuals() { foreach (var g in visuals.Values) if (g != null) Destroy(g); visuals.Clear(); }
        private static long Pack(int x, int y) { unchecked { return ((long)x << 32) ^ (uint)y; } }
        private static void Unpack(long k, out int x, out int y) { x = (int)(k >> 32); y = (int)(uint)k; }
        private static int FloorDiv(int a, int b) { int q = a / b, r = a % b; if (r != 0 && ((r < 0) != (b < 0))) q--; return q; }
    }
}
