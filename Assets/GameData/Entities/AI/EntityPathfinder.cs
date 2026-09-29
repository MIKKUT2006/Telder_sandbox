using System;
using System.Collections.Generic;
using UnityEngine;
using Game.World.Collision;

namespace Game.Entities.AI
{
    /// <summary>
    /// Small local A* for Terraria-like movement. It never searches the whole world.
    /// Ground nodes represent safe standing positions; flying nodes represent empty cells.
    /// </summary>
    public sealed class EntityPathfinder
    {
        private sealed class Node
        {
            public int X;
            public int Y;
            public float G;
            public float F;
            public Node Parent;
        }

        private readonly WorldCollision collision;
        private readonly EntityDefinition definition;
        private readonly List<Node> open = new List<Node>(256);
        private readonly Dictionary<long, Node> best = new Dictionary<long, Node>(512);
        private readonly HashSet<long> closed = new HashSet<long>();
        private readonly List<Node> nodePool = new List<Node>(700);
        private int nodePoolUsed;

        public EntityPathfinder(WorldCollision collision, EntityDefinition definition)
        {
            this.collision = collision;
            this.definition = definition;
        }

        public bool FindPath(Vector2 startWorld, Vector2 targetWorld, List<Vector2> result)
        {
            result.Clear();
            if (collision == null || definition == null)
                return false;

            EntityMovementKind kind = definition.Movement.GetKind();
            int startX = Mathf.FloorToInt(startWorld.x);
            int startY = kind == EntityMovementKind.Flying
                ? Mathf.FloorToInt(startWorld.y)
                : Mathf.FloorToInt(startWorld.y - definition.ColliderHeight * 0.5f + 0.05f);

            int goalX = Mathf.FloorToInt(targetWorld.x);
            int goalY = kind == EntityMovementKind.Flying
                ? Mathf.FloorToInt(targetWorld.y)
                : Mathf.FloorToInt(targetWorld.y - definition.ColliderHeight * 0.5f + 0.05f);

            if (kind != EntityMovementKind.Flying)
            {
                if (!FindNearestStandable(goalX, goalY, 5, out goalX, out goalY))
                    return false;

                if (!CanStandAt(startX, startY))
                {
                    if (!FindNearestStandable(startX, startY, 3, out startX, out startY))
                        return false;
                }
            }
            else
            {
                if (!IsFlyingCellFree(goalX, goalY))
                    FindNearestFlyingFree(goalX, goalY, 4, out goalX, out goalY);
            }

            open.Clear();
            best.Clear();
            closed.Clear();
            nodePoolUsed = 0;

            Node start = AcquireNode();
            start.X = startX;
            start.Y = startY;
            start.G = 0f;
            start.F = Heuristic(startX, startY, goalX, goalY);
            start.Parent = null;

            open.Add(start);
            best[Key(startX, startY)] = start;

            int maxExpanded = kind == EntityMovementKind.Flying ? 700 : 550;
            int expanded = 0;
            Node reached = null;

            while (open.Count > 0 && expanded < maxExpanded)
            {
                int bestIndex = 0;
                float bestF = open[0].F;
                for (int i = 1; i < open.Count; i++)
                {
                    if (open[i].F < bestF)
                    {
                        bestF = open[i].F;
                        bestIndex = i;
                    }
                }

                Node current = open[bestIndex];
                open.RemoveAt(bestIndex);
                long currentKey = Key(current.X, current.Y);
                if (closed.Contains(currentKey))
                    continue;

                closed.Add(currentKey);
                expanded++;

                if (Mathf.Abs(current.X - goalX) <= 0 && Mathf.Abs(current.Y - goalY) <= 0)
                {
                    reached = current;
                    break;
                }

                if (kind == EntityMovementKind.Flying)
                    ExpandFlying(current, goalX, goalY);
                else
                    ExpandGround(current, goalX, goalY);
            }

            if (reached == null)
                return false;

            BuildPath(reached, kind, result);
            return result.Count > 0;
        }

        private void ExpandFlying(Node current, int goalX, int goalY)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;

                    int nx = current.X + dx;
                    int ny = current.Y + dy;
                    if (!IsFlyingCellFree(nx, ny))
                        continue;

                    float cost = dx != 0 && dy != 0 ? 1.4142f : 1f;
                    AddNode(current, nx, ny, cost, goalX, goalY);
                }
            }
        }

        private void ExpandGround(Node current, int goalX, int goalY)
        {
            int maxStep = Mathf.Max(0, definition.Movement.MaxStepUpCells);
            int maxJump = Mathf.Max(maxStep, definition.Movement.MaxJumpUpCells);
            int maxDrop = Mathf.Max(0, definition.Movement.MaxDropCells);

            for (int dir = -1; dir <= 1; dir += 2)
            {
                int nx = current.X + dir;

                // Walk on the same level.
                if (CanStandAt(nx, current.Y))
                    AddNode(current, nx, current.Y, 1f, goalX, goalY);

                // Step / jump upward.
                for (int up = 1; up <= maxJump; up++)
                {
                    int ny = current.Y + up;
                    if (!CanStandAt(nx, ny))
                        continue;

                    if (HasJumpClearance(current.X, current.Y, nx, ny))
                    {
                        float extra = up <= maxStep ? 0.2f : 0.75f + up * 0.18f;
                        AddNode(current, nx, ny, 1f + extra, goalX, goalY);
                    }
                    break;
                }

                // Drop to a lower ledge.
                for (int down = 1; down <= maxDrop; down++)
                {
                    int ny = current.Y - down;
                    if (!CanStandAt(nx, ny))
                        continue;

                    AddNode(current, nx, ny, 1f + down * 0.12f, goalX, goalY);
                    break;
                }

                // Jump across a one-cell gap.
                int jumpX = current.X + dir * 2;
                for (int up = 0; up <= maxJump; up++)
                {
                    int ny = current.Y + up;
                    if (!CanStandAt(jumpX, ny))
                        continue;

                    if (HasJumpClearance(current.X, current.Y, jumpX, ny))
                        AddNode(current, jumpX, ny, 2.35f + up * 0.2f, goalX, goalY);
                    break;
                }
            }
        }

        private void AddNode(Node parent, int x, int y, float moveCost, int goalX, int goalY)
        {
            long key = Key(x, y);
            if (closed.Contains(key))
                return;

            float g = parent.G + moveCost;
            Node node;
            if (best.TryGetValue(key, out node))
            {
                if (g >= node.G)
                    return;

                node.G = g;
                node.F = g + Heuristic(x, y, goalX, goalY);
                node.Parent = parent;
                if (!open.Contains(node))
                    open.Add(node);
                return;
            }

            node = AcquireNode();
            node.X = x;
            node.Y = y;
            node.G = g;
            node.F = g + Heuristic(x, y, goalX, goalY);
            node.Parent = parent;
            best[key] = node;
            open.Add(node);
        }

        private Node AcquireNode()
        {
            Node node;
            if (nodePoolUsed < nodePool.Count)
            {
                node = nodePool[nodePoolUsed];
            }
            else
            {
                node = new Node();
                nodePool.Add(node);
            }

            nodePoolUsed++;
            return node;
        }

        private void BuildPath(Node reached, EntityMovementKind kind, List<Vector2> result)
        {
            Node n = reached;
            while (n != null)
            {
                float y = kind == EntityMovementKind.Flying
                    ? n.Y + 0.5f
                    : n.Y + definition.ColliderHeight * 0.5f;
                result.Add(new Vector2(n.X + 0.5f, y));
                n = n.Parent;
            }

            result.Reverse();
            if (result.Count > 0)
                result.RemoveAt(0);
        }

        public bool CanStandAt(int x, int feetY)
        {
            Rect bounds = new Rect(
                x + 0.5f - definition.ColliderWidth * 0.5f,
                feetY + 0.02f,
                definition.ColliderWidth,
                Mathf.Max(0.1f, definition.ColliderHeight - 0.04f));

            if (collision.OverlapsAny(bounds))
                return false;

            Rect feet = new Rect(
                x + 0.5f - definition.ColliderWidth * 0.42f,
                feetY - 0.08f,
                definition.ColliderWidth * 0.84f,
                0.11f);

            return collision.OverlapsAny(feet);
        }

        public bool IsFlyingCellFree(int x, int y)
        {
            Rect bounds = new Rect(
                x + 0.5f - definition.ColliderWidth * 0.5f,
                y + 0.5f - definition.ColliderHeight * 0.5f,
                definition.ColliderWidth,
                definition.ColliderHeight);

            return !collision.OverlapsAny(bounds);
        }

        private bool FindNearestStandable(int x, int y, int radius, out int foundX, out int foundY)
        {
            for (int r = 0; r <= radius; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        int px = x + dx;
                        int py = y + dy;
                        if (CanStandAt(px, py))
                        {
                            foundX = px;
                            foundY = py;
                            return true;
                        }
                    }
                }
            }

            foundX = x;
            foundY = y;
            return false;
        }

        private bool FindNearestFlyingFree(int x, int y, int radius, out int foundX, out int foundY)
        {
            for (int r = 0; r <= radius; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        int px = x + dx;
                        int py = y + dy;
                        if (IsFlyingCellFree(px, py))
                        {
                            foundX = px;
                            foundY = py;
                            return true;
                        }
                    }
                }
            }

            foundX = x;
            foundY = y;
            return false;
        }

        private bool HasJumpClearance(int fromX, int fromY, int toX, int toY)
        {
            int steps = Mathf.Max(Mathf.Abs(toX - fromX), Mathf.Abs(toY - fromY)) * 2 + 2;
            Vector2 start = new Vector2(fromX + 0.5f, fromY + definition.ColliderHeight * 0.5f);
            Vector2 end = new Vector2(toX + 0.5f, toY + definition.ColliderHeight * 0.5f);

            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;
                Vector2 p = Vector2.Lerp(start, end, t);
                p.y += Mathf.Sin(t * Mathf.PI) * 0.45f;

                Rect bounds = new Rect(
                    p.x - definition.ColliderWidth * 0.45f,
                    p.y - definition.ColliderHeight * 0.45f,
                    definition.ColliderWidth * 0.9f,
                    definition.ColliderHeight * 0.9f);

                if (collision.OverlapsAny(bounds))
                    return false;
            }

            return true;
        }

        private static float Heuristic(int x, int y, int gx, int gy)
        {
            return Mathf.Abs(gx - x) + Mathf.Abs(gy - y) * 1.15f;
        }

        private static long Key(int x, int y)
        {
            unchecked
            {
                return ((long)x << 32) ^ (uint)y;
            }
        }
    }
}
