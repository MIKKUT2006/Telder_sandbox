using UnityEngine;
using Game.World.Collision;

namespace Game.Entities.Physics
{
    /// <summary>
    /// Lightweight kinematic motor for entity movement against the tile world.
    /// Transform position is always the CENTER of the entity collider.
    /// </summary>
    public sealed class CharacterMotor
    {
        private readonly WorldCollision collision;
        private readonly float width;
        private readonly float height;

        private const float Skin = 0.01f;
        private const float MaxStep = 0.25f;
        private const int ResolveIterations = 10;
        private const float GroundProbeDistance = 0.035f;

        public bool IsGrounded { get; private set; }
        public bool HitCeiling { get; private set; }
        public bool HitWall { get; private set; }

        public CharacterMotor(WorldCollision collision, float width, float height)
        {
            this.collision = collision;
            this.width = Mathf.Max(0.05f, width);
            this.height = Mathf.Max(0.05f, height);
        }

        public Vector2 Move(Vector2 position, ref Vector2 velocity, float deltaTime)
        {
            IsGrounded = false;
            HitCeiling = false;
            HitWall = false;

            if (collision == null || deltaTime <= 0f)
                return position + velocity * Mathf.Max(0f, deltaTime);

            position = MoveHorizontal(position, ref velocity, deltaTime);
            position = MoveVertical(position, ref velocity, deltaTime);

            // A body that is resting exactly on the floor can have an almost-zero
            // vertical movement on a frame. Probe slightly below so Ground entities
            // keep a stable grounded state instead of alternating grounded/airborne.
            if (!IsGrounded && velocity.y <= 0.001f && HasGroundImmediatelyBelow(position))
                IsGrounded = true;

            return position;
        }

        private Vector2 MoveHorizontal(Vector2 position, ref Vector2 velocity, float deltaTime)
        {
            float movement = velocity.x * deltaTime;
            if (Mathf.Abs(movement) <= Mathf.Epsilon)
                return position;

            float direction = Mathf.Sign(movement);
            float distance = Mathf.Abs(movement);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / MaxStep));
            float stepDistance = distance / steps;

            for (int i = 0; i < steps; i++)
            {
                Vector2 target = position;
                target.x += direction * stepDistance;

                if (IsColliding(target.x, target.y))
                {
                    position = ResolveBetween(position, target);
                    velocity.x = 0f;
                    HitWall = true;
                    break;
                }

                position = target;
            }

            return position;
        }

        private Vector2 MoveVertical(Vector2 position, ref Vector2 velocity, float deltaTime)
        {
            float movement = velocity.y * deltaTime;
            if (Mathf.Abs(movement) <= Mathf.Epsilon)
                return position;

            float direction = Mathf.Sign(movement);
            float distance = Mathf.Abs(movement);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / MaxStep));
            float stepDistance = distance / steps;

            for (int i = 0; i < steps; i++)
            {
                Vector2 target = position;
                target.y += direction * stepDistance;

                if (IsColliding(target.x, target.y))
                {
                    // IMPORTANT: resolve between the known-safe position and the
                    // actual blocked target. The old code derived a block index from
                    // the previous position, which could snap the entity one whole
                    // tile upward when landing on the floor.
                    position = ResolveBetween(position, target);
                    velocity.y = 0f;

                    if (direction < 0f)
                        IsGrounded = true;
                    else
                        HitCeiling = true;

                    break;
                }

                position = target;
            }

            return position;
        }

        private Vector2 ResolveBetween(Vector2 safePosition, Vector2 blockedPosition)
        {
            Vector2 safe = safePosition;
            Vector2 blocked = blockedPosition;

            for (int i = 0; i < ResolveIterations; i++)
            {
                Vector2 mid = (safe + blocked) * 0.5f;
                if (IsColliding(mid.x, mid.y))
                    blocked = mid;
                else
                    safe = mid;
            }

            return safe;
        }

        private bool HasGroundImmediatelyBelow(Vector2 position)
        {
            return IsColliding(position.x, position.y - GroundProbeDistance);
        }

        private bool IsColliding(float centerX, float centerY)
        {
            if (collision == null)
                return false;

            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;

            float minX = centerX - halfWidth + Skin;
            float maxX = centerX + halfWidth - Skin;
            float minY = centerY - halfHeight + Skin;
            float maxY = centerY + halfHeight - Skin;

            int minBlockX = Mathf.FloorToInt(minX);
            int maxBlockX = Mathf.FloorToInt(maxX);
            int minBlockY = Mathf.FloorToInt(minY);
            int maxBlockY = Mathf.FloorToInt(maxY);

            for (int x = minBlockX; x <= maxBlockX; x++)
            {
                for (int y = minBlockY; y <= maxBlockY; y++)
                {
                    if (collision.IsSolid(x, y))
                        return true;
                }
            }

            return false;
        }
    }
}
