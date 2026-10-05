using System;
using System.Collections.Generic;

namespace Game.World.Rendering
{
    public enum BlockVisualLayer
    {
        Foreground = 0,
        Background = 1,
        Furniture = 2
    }

    /// <summary>
    /// Lightweight runtime state store for block visuals.
    /// Gameplay systems own the real state (furnace burn timer, door open state, etc.)
    /// and publish only a small string state here, for example "burning" or "open".
    /// </summary>
    public static class BlockVisualStateRuntime
    {
        private struct Key : IEquatable<Key>
        {
            public int X;
            public int Y;
            public BlockVisualLayer Layer;

            public bool Equals(Key other)
            {
                return X == other.X && Y == other.Y && Layer == other.Layer;
            }

            public override bool Equals(object obj)
            {
                return obj is Key other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + X;
                    hash = hash * 31 + Y;
                    hash = hash * 31 + (int)Layer;
                    return hash;
                }
            }
        }

        private static readonly Dictionary<Key, string> states =
            new Dictionary<Key, string>();

        private static string contextKey;

        public static event Action<int, int, BlockVisualLayer, string> StateChanged;

        /// <summary>
        /// Call once when the active save/dimension changes. A visual state is never
        /// allowed to leak from one dimension into another.
        /// </summary>
        public static void SetContext(string key)
        {
            key = key ?? string.Empty;
            if (string.Equals(contextKey, key, StringComparison.Ordinal))
                return;

            contextKey = key;
            states.Clear();
        }

        public static bool SetState(
            int x,
            int y,
            BlockVisualLayer layer,
            string state)
        {
            Key key = new Key { X = x, Y = y, Layer = layer };
            string normalized = string.IsNullOrWhiteSpace(state) ? null : state.Trim();

            if (states.TryGetValue(key, out string oldState))
            {
                if (string.Equals(oldState, normalized, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            else if (normalized == null)
            {
                return false;
            }

            if (normalized == null)
                states.Remove(key);
            else
                states[key] = normalized;

            StateChanged?.Invoke(x, y, layer, normalized);
            return true;
        }

        public static string GetState(
            int x,
            int y,
            BlockVisualLayer layer)
        {
            states.TryGetValue(
                new Key { X = x, Y = y, Layer = layer },
                out string state
            );
            return state;
        }

        public static bool ClearState(
            int x,
            int y,
            BlockVisualLayer layer)
        {
            return ClearState(x, y, layer, true);
        }

        public static bool ClearState(
            int x,
            int y,
            BlockVisualLayer layer,
            bool notify)
        {
            Key key = new Key { X = x, Y = y, Layer = layer };
            if (!states.Remove(key))
                return false;

            if (notify)
                StateChanged?.Invoke(x, y, layer, null);

            return true;
        }

        public static void ClearAll()
        {
            states.Clear();
        }

        public static void SetForegroundState(int x, int y, string state)
        {
            SetState(x, y, BlockVisualLayer.Foreground, state);
        }

        public static void SetBackgroundState(int x, int y, string state)
        {
            SetState(x, y, BlockVisualLayer.Background, state);
        }

        public static void SetFurnitureState(int x, int y, string state)
        {
            SetState(x, y, BlockVisualLayer.Furniture, state);
        }
    }
}
