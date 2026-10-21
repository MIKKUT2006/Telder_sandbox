using System;
using System.Reflection;
using Game.Blocks;
using Game.World;
using Game.World.Collision;
using GameWorld = Game.World.World;
using UnityEngine;

namespace Game.BlockTransforms
{
    public static class BlockShapeCollision
    {
        private const BindingFlags Flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static FieldInfo worldField;


        public static bool Intersects(
            WorldCollision worldCollision,
            float minX,
            float minY,
            float maxX,
            float maxY)
        {
            if (worldCollision == null ||
                minX >= maxX ||
                minY >= maxY)
            {
                return false;
            }

            Rect bounds = Rect.MinMaxRect(
                minX,
                minY,
                maxX,
                maxY
            );

            return worldCollision.OverlapsAny(bounds);
        }


        public static bool IsCustomCell(
            WorldCollision worldCollision,
            int worldX,
            int worldY)
        {
            BlockDefinition block = GetBlockDefinition(worldCollision, worldX, worldY);
            return block != null && GetBool(block, "UseCustomCollision", false);
        }

        public static bool TryGetWorldRect(
            WorldCollision worldCollision,
            int worldX,
            int worldY,
            out Vector2 center,
            out Vector2 size)
        {
            center = new Vector2(worldX + 0.5f, worldY + 0.5f);
            size = Vector2.one;

            if (worldCollision == null || !worldCollision.IsSolid(worldX, worldY))
                return false;

            BlockDefinition block = GetBlockDefinition(worldCollision, worldX, worldY);
            if (block == null || !GetBool(block, "UseCustomCollision", false))
                return true;

            float ox = Mathf.Clamp01(GetFloat(block, "CollisionOffsetX", 0.5f));
            float oy = Mathf.Clamp01(GetFloat(block, "CollisionOffsetY", 0.5f));
            float width = Mathf.Clamp(GetFloat(block, "CollisionWidth", 1f), 0.001f, 1f);
            float height = Mathf.Clamp(GetFloat(block, "CollisionHeight", 1f), 0.001f, 1f);

            BlockTransformState state = BlockTransformRegistry.Get(worldX, worldY);
            Vector2 transformed = BlockTransformRenderBridge.TransformPoint01(
                new Vector2(ox, oy), state);

            if (state.Rotation == 1 || state.Rotation == 3)
            {
                float swap = width;
                width = height;
                height = swap;
            }

            center = new Vector2(worldX + transformed.x, worldY + transformed.y);
            size = new Vector2(width, height);
            return true;
        }

        private static BlockDefinition GetBlockDefinition(
            WorldCollision worldCollision,
            int worldX,
            int worldY)
        {
            GameWorld world = GetWorld(worldCollision);
            if (world == null)
                return null;

            ushort id = world.GetBlock(worldX, worldY);
            return id == 0 ? null : world.GetBlockDefinition(id);
        }

        private static GameWorld GetWorld(WorldCollision worldCollision)
        {
            if (worldCollision == null)
                return null;

            Type type = worldCollision.GetType();

            if (worldField == null || worldField.DeclaringType != type)
            {
                worldField = type.GetField("world", Flags);

                if (worldField == null)
                {
                    FieldInfo[] fields = type.GetFields(Flags);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (typeof(GameWorld).IsAssignableFrom(fields[i].FieldType))
                        {
                            worldField = fields[i];
                            break;
                        }
                    }
                }
            }

            return worldField != null
                ? worldField.GetValue(worldCollision) as GameWorld
                : null;
        }

        private static object GetMember(object target, string name)
        {
            if (target == null)
                return null;

            Type type = target.GetType();
            FieldInfo field = type.GetField(name, Flags);
            if (field != null)
                return field.GetValue(target);

            PropertyInfo property = type.GetProperty(name, Flags);
            if (property != null && property.GetIndexParameters().Length == 0)
                return property.GetValue(target, null);

            return null;
        }

        private static bool GetBool(object target, string name, bool fallback)
        {
            object value = GetMember(target, name);
            return value is bool ? (bool)value : fallback;
        }

        private static float GetFloat(object target, string name, float fallback)
        {
            object value = GetMember(target, name);
            if (value == null)
                return fallback;

            try { return Convert.ToSingle(value); }
            catch { return fallback; }
        }
    }
}
