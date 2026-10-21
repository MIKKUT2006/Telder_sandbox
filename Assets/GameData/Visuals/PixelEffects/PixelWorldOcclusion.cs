using System;
using System.Collections.Generic;
using Game.Blocks;
using Game.BlockTransforms;
using Game.Resources;
using Game.World;
using Game.World.Furniture;
using Game.World.Rendering;
using UnityEngine;
using GameWorld = Game.World.World;

namespace Game.Visuals.PixelEffects
{
    // CPU-side alpha sampling is cached per block and per texture, never per ray step.
    public sealed class PixelWorldOcclusion
    {
        private sealed class AlphaImage
        {
            public int Width, Height;
            public Color32[] Pixels;
        }
        private struct Cell
        {
            public BlockDefinition Definition;
            public AlphaImage Image;
            public byte Transform;
            public bool Foliage;
            public int X, Y;
        }

        private readonly GameWorld world;
        private readonly Dictionary<ushort, BlockDefinition> definitions = new Dictionary<ushort, BlockDefinition>();
        private readonly Dictionary<Texture2D, AlphaImage> images = new Dictionary<Texture2D, AlphaImage>();
        private readonly Dictionary<long, Cell> cells = new Dictionary<long, Cell>(8192);
        private readonly Dictionary<long, Cell> furnitureCells = new Dictionary<long, Cell>(2048);
        private float maskTime;

        public PixelWorldOcclusion(GameWorld world) { this.world = world; }
        public void BeginMask() { cells.Clear(); furnitureCells.Clear(); maskTime=Time.time; }

        public BlockDefinition Definition(ushort id)
        {
            if (id == 0) return null;
            if (!definitions.TryGetValue(id, out BlockDefinition definition))
            {
                definition = world.GetBlockDefinition(id);
                definitions[id] = definition;
            }
            return definition;
        }

        public static bool HasTag(BlockDefinition definition, string tag)
        {
            if (definition == null || definition.Tags == null) return false;
            for (int i = 0; i < definition.Tags.Count; i++)
                if (string.Equals(definition.Tags[i], tag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private Cell GetCell(int x, int y, bool furniture)
        {
            long key = ((long)x << 32) ^ (uint)y;
            Dictionary<long, Cell> cache = furniture ? furnitureCells : cells;
            if (cache.TryGetValue(key, out Cell cell)) return cell;
            BlockDefinition definition = null;
            byte transform = 0;
            if (furniture)
            {
                FurnitureLayerManager manager = FurnitureLayerManager.Instance;
                if (manager != null && manager.TryGetDefinition(x,y,out definition))
                    transform = manager.GetFurnitureTransform(x,y);
            }
            else
            {
                definition = Definition(world.GetBlock(x,y));
                transform = BlockTransformRegistry.GetRaw(x,y);
            }
            if (definition == null) { cache[key] = default; return default; }

            string id = definition.ID ?? string.Empty;
            bool foliage = HasTag(definition,"leaves") || HasTag(definition,"foliage") ||
                id.IndexOf("leav",StringComparison.OrdinalIgnoreCase) >= 0 ||
                id.IndexOf("leaf",StringComparison.OrdinalIgnoreCase) >= 0;
            string state = BlockVisualStateRuntime.GetState(x,y,
                furniture ? BlockVisualLayer.Furniture : BlockVisualLayer.Foreground);
            if (string.Equals(state,"open",StringComparison.OrdinalIgnoreCase))
            { cache[key] = default; return default; }

            cell = new Cell { Definition = definition, Transform = transform, Foliage = foliage, X=x, Y=y };
            string textureName = BlockVisualResolver.ResolveTextureName(definition,x,y,
                furniture ? BlockVisualLayer.Furniture : BlockVisualLayer.Foreground);
            if (TextureManager.TryGet(textureName, out Texture2D texture) && texture != null)
            {
                if (!images.TryGetValue(texture,out AlphaImage image))
                {
                    image = new AlphaImage { Width=texture.width, Height=texture.height };
                    try { image.Pixels = texture.GetPixels32(); }
                    catch (UnityException) { image.Pixels = null; }
                    images[texture] = image;
                }
                cell.Image = image;
            }
            cache[key] = cell;
            return cell;
        }

        public float Transmission(Vector2 position, out bool foliage)
        {
            int x = Mathf.FloorToInt(position.x), y = Mathf.FloorToInt(position.y);
            Vector2 local = new Vector2(position.x-x,position.y-y);
            Cell foreground = GetCell(x,y,false);
            float front = Sample(foreground,local,out bool frontLeaf);
            // Most foliage is terrain. Furniture foliage is supported as well.
            Cell furniture = GetCell(x,y,true);
            float back = Sample(furniture,local,out bool backLeaf);
            foliage = frontLeaf || backLeaf;
            return Mathf.Min(front,back);
        }

        private static Vector2 InverseTransform(Vector2 local, byte transform)
        {
            float x=local.x, y=local.y;
            switch (transform & 3)
            {
                case 1: local = new Vector2(1f-y,x); break;
                case 2: local = new Vector2(1f-x,1f-y); break;
                case 3: local = new Vector2(y,1f-x); break;
            }
            if ((transform & 4) != 0) local.x = 1f-local.x;
            return local;
        }

        private float Sample(Cell cell, Vector2 local, out bool foliage)
        {
            foliage = cell.Foliage;
            if (cell.Definition == null) return 1f;
            BlockDefinition definition = cell.Definition;
            if (!definition.BlocksLight && definition.LightOpacity == 0 && !foliage) return 1f;
            // Match the shader's integer wind displacement in leaf texture space.
            if (foliage && HasTag(definition,"wave"))
            {
                float phase = maskTime + cell.X * .56f + cell.Y * .22f;
                float gust = .72f + .28f * Mathf.Sin(maskTime * .39f + cell.X * .135f);
                float pixelY = local.y * 16f;
                float h = Mathf.Clamp01((pixelY - 2f) / 14f);
                float heightWeight = pixelY >= 2f ? Mathf.Lerp(.22f, 1f, Mathf.Pow(h, .78f)) : 0f;
                float sway = Mathf.Sin(phase * 1.1f) * gust
                    + .22f * Mathf.Sin(phase * 1.83f + h * 5.7f + maskTime * .31f)
                    + .10f * Mathf.Sin(phase * .49f - h * 12.9f);
                float offset = Mathf.Floor(sway * 2f * heightWeight + .5f) / 16f;
                local.x = Mathf.Clamp01(local.x-offset);
            }
            local = InverseTransform(local,cell.Transform);

            if (!foliage && !InsideShape(definition,local)) return 1f;
            float alpha = 1f;
            if (cell.Image != null && cell.Image.Pixels != null)
            {
                int px = Mathf.Clamp((int)(local.x*cell.Image.Width),0,cell.Image.Width-1);
                int py = Mathf.Clamp((int)(local.y*cell.Image.Height),0,cell.Image.Height-1);
                alpha = cell.Image.Pixels[px+py*cell.Image.Width].a/255f;
            }
            if (alpha < .15f) return 1f;
            if (foliage) return Mathf.Lerp(1f,.08f,alpha);
            float opacity = definition.BlocksLight ? 1f : definition.LightOpacity/15f;
            return Mathf.Clamp01(1f-opacity*alpha);
        }

        private static bool InsideShape(BlockDefinition definition, Vector2 p)
        {
            switch (definition.CollisionShape)
            {
                case "HalfBottom": return p.y <= .5f;
                case "HalfTop": return p.y >= .5f;
                case "StairUpRight": return p.y <= .5f || p.x >= .5f;
                case "StairUpLeft": return p.y <= .5f || p.x <= .5f;
                case "Custom":
                    if (definition.CollisionRects != null)
                        for (int i=0;i<definition.CollisionRects.Count;i++)
                        {
                            BlockCollisionRectDefinition r=definition.CollisionRects[i];
                            if (p.x>=r.X && p.x<=r.X+r.Width && p.y>=r.Y && p.y<=r.Y+r.Height) return true;
                        }
                    return false;
                default: return true;
            }
        }
    }
}

