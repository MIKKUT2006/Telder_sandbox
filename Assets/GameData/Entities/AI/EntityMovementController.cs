using System.Collections.Generic;
using UnityEngine;
using Game.Entities.Physics;
using Game.World.Collision;

namespace Game.Entities.AI
{
    public sealed class EntityMovementController
    {
        private readonly EntityDefinition definition;
        private readonly WorldCollision collision;
        private readonly CharacterMotor motor;
        private readonly EntityPathfinder pathfinder;
        private readonly List<Vector2> path = new List<Vector2>(64);

        private Vector2 velocity;
        private int pathIndex;
        private float nextPathTime;
        private float nextHopTime;
        private float nextGroundJumpTime;
        private float externalImpulseUntil;
        private Vector2 lastPathTarget;

        public Vector2 Velocity { get { return velocity; } }
        public bool HasPath { get { return pathIndex < path.Count; } }

        public EntityMovementController(EntityDefinition definition, WorldCollision collision)
        {
            this.definition = definition;
            this.collision = collision;
            motor = new CharacterMotor(collision, definition.ColliderWidth, definition.ColliderHeight);
            pathfinder = new EntityPathfinder(collision, definition);
        }

        public void ClearPath()
        {
            path.Clear();
            pathIndex = 0;
        }

        public void AddImpulse(Vector2 impulse)
        {
            if (impulse.sqrMagnitude <= 0.0001f)
                return;

            // AI steering used to immediately cancel horizontal knockback on the
            // following physics tick. Give external hits a short control window
            // so the reaction is visible, then smoothly return control to AI.
            velocity += impulse;
            externalImpulseUntil = Mathf.Max(
                externalImpulseUntil,
                Time.time + 0.16f + Mathf.Min(0.12f, impulse.magnitude * 0.015f));

            // Prevent a pathfinding jump from firing in the same instant as a hit.
            nextGroundJumpTime = Mathf.Max(nextGroundJumpTime, externalImpulseUntil);
        }

        public void SetTarget(Vector2 currentPosition, Vector2 target, bool force = false)
        {
            float refresh = definition.Movement.GetKind() == EntityMovementKind.Flying ? 0.45f : 0.65f;
            if (!force && Time.time < nextPathTime && (target - lastPathTarget).sqrMagnitude < 2.25f)
                return;

            nextPathTime = Time.time + refresh + Random.Range(0f, 0.18f);
            lastPathTarget = target;
            pathIndex = 0;
            pathfinder.FindPath(currentPosition, target, path);
        }

        public Vector2 Tick(Vector2 position, float dt)
        {
            EntityMovementKind kind = definition.Movement.GetKind();
            if (kind == EntityMovementKind.Flying)
                return TickFlying(position, dt);

            return TickGround(position, dt, kind == EntityMovementKind.Jumping);
        }

        private Vector2 TickFlying(Vector2 position, float dt)
        {
            Vector2 desired = Vector2.zero;
            Vector2 waypoint;
            if (TryGetWaypoint(position, out waypoint))
            {
                Vector2 d = waypoint - position;
                if (d.sqrMagnitude > 0.0001f)
                    desired = d.normalized * definition.Movement.Speed;
            }

            float steering =
                Time.time < externalImpulseUntil
                    ? Mathf.Max(0.1f, definition.Movement.Acceleration) * 0.08f
                    : Mathf.Max(0.1f, definition.Movement.Acceleration);

            velocity = Vector2.MoveTowards(
                velocity,
                desired,
                steering * dt);

            Vector2 next = position + velocity * dt;
            Rect bounds = new Rect(
                next.x - definition.ColliderWidth * 0.5f,
                next.y - definition.ColliderHeight * 0.5f,
                definition.ColliderWidth,
                definition.ColliderHeight);

            if (collision.OverlapsAny(bounds))
            {
                velocity *= 0.2f;
                return position;
            }

            return next;
        }

        private Vector2 TickGround(Vector2 position, float dt, bool jumpingOnly)
        {
            float desiredX = 0f;
            Vector2 waypoint;
            bool hasWaypoint = TryGetWaypoint(position, out waypoint);
            float waypointDx = 0f;

            if (hasWaypoint)
            {
                waypointDx = waypoint.x - position.x;
                if (Mathf.Abs(waypointDx) > 0.08f)
                    desiredX = Mathf.Sign(waypointDx) * definition.Movement.Speed;
            }

            bool externalImpulseActive = Time.time < externalImpulseUntil;

            velocity.x = Mathf.MoveTowards(
                velocity.x,
                externalImpulseActive ? 0f : desiredX,
                Mathf.Max(0.1f, definition.Movement.Acceleration) *
                (externalImpulseActive ? 0.10f : 1f) * dt);

            velocity.y -= Mathf.Max(0f, definition.Movement.Gravity) * dt;
            velocity.y = Mathf.Max(velocity.y, -Mathf.Max(0.1f, definition.Movement.MaxFallSpeed));

            bool wantsHorizontalMove = Mathf.Abs(desiredX) > 0.01f;
            bool higherWaypoint =
                hasWaypoint &&
                Mathf.Abs(waypointDx) > 0.14f &&
                waypoint.y > position.y + 0.55f;

            // Do not use CharacterMotor.HitWall here: that flag belongs to the
            // previous Move() call and made Ground enemies repeatedly jump in place.
            // Probe the space immediately in front of the current body instead.
            bool obstacleAhead =
                wantsHorizontalMove &&
                IsObstacleAhead(position, Mathf.Sign(desiredX));

            bool needsJump = higherWaypoint || obstacleAhead;

            if (jumpingOnly)
            {
                if (motor.IsGrounded && hasWaypoint && Time.time >= nextHopTime)
                {
                    needsJump = true;
                    nextHopTime = Time.time + Mathf.Max(0.1f, definition.Movement.JumpingHopInterval);
                }

                if (motor.IsGrounded && !needsJump)
                    velocity.x *= 0.35f;
            }

            if (
                needsJump &&
                !externalImpulseActive &&
                motor.IsGrounded &&
                Time.time >= nextGroundJumpTime)
            {
                // Keep horizontal intent while jumping so a Ground enemy climbs
                // toward the next path node instead of making a vertical hop.
                if (wantsHorizontalMove)
                    velocity.x = Mathf.Sign(desiredX) * Mathf.Max(Mathf.Abs(velocity.x), definition.Movement.Speed * 0.75f);

                velocity.y = Mathf.Max(0.1f, definition.Movement.JumpForce);
                nextGroundJumpTime = Time.time + 0.22f;
            }

            return motor.Move(position, ref velocity, dt);
        }

        private bool IsObstacleAhead(Vector2 position, float direction)
        {
            if (collision == null || Mathf.Abs(direction) < 0.01f)
                return false;

            float width = Mathf.Max(0.1f, definition.ColliderWidth);
            float height = Mathf.Max(0.2f, definition.ColliderHeight);
            float probeWidth = 0.10f;
            float feetY = position.y - height * 0.5f;
            float probeHeight = Mathf.Max(0.16f, height - 0.24f);
            float probeCenterX =
                position.x +
                direction * (width * 0.5f + probeWidth * 0.5f + 0.025f);

            Rect probe = new Rect(
                probeCenterX - probeWidth * 0.5f,
                feetY + 0.12f,
                probeWidth,
                probeHeight);

            return collision.OverlapsAny(probe);
        }

        private bool TryGetWaypoint(Vector2 position, out Vector2 waypoint)
        {
            while (pathIndex < path.Count)
            {
                Vector2 p = path[pathIndex];
                float threshold = definition.Movement.GetKind() == EntityMovementKind.Flying ? 0.4f : 0.38f;
                if ((p - position).sqrMagnitude <= threshold * threshold)
                {
                    pathIndex++;
                    continue;
                }

                waypoint = p;
                return true;
            }

            waypoint = position;
            return false;
        }
    }
}
