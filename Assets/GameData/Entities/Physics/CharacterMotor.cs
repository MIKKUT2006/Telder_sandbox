using UnityEngine;
using Game.World.Collision;

namespace Game.Entities.Physics
{
    public class CharacterMotor
    {
        private readonly WorldCollision collision;

        private readonly float width;
        private readonly float height;

        private const float Skin = 0.01f;

        private const float MaxStep = 0.25f;


        public bool IsGrounded
        {
            get;
            private set;
        }


        public bool HitCeiling
        {
            get;
            private set;
        }


        public bool HitWall
        {
            get;
            private set;
        }


        public CharacterMotor(
            WorldCollision collision,
            float width,
            float height
        )
        {
            this.collision =
                collision;

            this.width =
                width;

            this.height =
                height;
        }


        // =====================================================
        // Œ—ÕŒ¬ÕŒ≈ ƒ¬»∆≈Õ»≈
        // =====================================================

        public Vector2 Move(
            Vector2 position,
            ref Vector2 velocity,
            float deltaTime
        )
        {
            IsGrounded =
                false;

            HitCeiling =
                false;

            HitWall =
                false;


            position =
                MoveHorizontal(
                    position,
                    ref velocity,
                    deltaTime
                );


            position =
                MoveVertical(
                    position,
                    ref velocity,
                    deltaTime
                );


            return position;
        }


        // =====================================================
        // ƒ¬»∆≈Õ»≈ œŒ X
        // =====================================================

        private Vector2 MoveHorizontal(
            Vector2 position,
            ref Vector2 velocity,
            float deltaTime
        )
        {
            float movement =
                velocity.x *
                deltaTime;


            if (
                Mathf.Abs(
                    movement
                ) <=
                Mathf.Epsilon
            )
            {
                return position;
            }


            float direction =
                Mathf.Sign(
                    movement
                );


            float distance =
                Mathf.Abs(
                    movement
                );


            int steps =
                Mathf.CeilToInt(
                    distance /
                    MaxStep
                );


            steps =
                Mathf.Max(
                    steps,
                    1
                );


            float stepDistance =
                distance /
                steps;


            for (
                int i = 0;
                i < steps;
                i++
            )
            {
                float step =
                    direction *
                    stepDistance;


                float targetX =
                    position.x +
                    step;


                if (
                    IsColliding(
                        targetX,
                        position.y
                    )
                )
                {
                    position.x =
                        GetSafeHorizontalPosition(
                            position,
                            direction
                        );


                    velocity.x =
                        0f;


                    HitWall =
                        true;


                    break;
                }


                position.x =
                    targetX;
            }


            return position;
        }


        // =====================================================
        // ƒ¬»∆≈Õ»≈ œŒ Y
        // =====================================================

        private Vector2 MoveVertical(
            Vector2 position,
            ref Vector2 velocity,
            float deltaTime
        )
        {
            float movement =
                velocity.y *
                deltaTime;


            if (
                Mathf.Abs(
                    movement
                ) <=
                Mathf.Epsilon
            )
            {
                return position;
            }


            float direction =
                Mathf.Sign(
                    movement
                );


            float distance =
                Mathf.Abs(
                    movement
                );


            int steps =
                Mathf.CeilToInt(
                    distance /
                    MaxStep
                );


            steps =
                Mathf.Max(
                    steps,
                    1
                );


            float stepDistance =
                distance /
                steps;


            for (
                int i = 0;
                i < steps;
                i++
            )
            {
                float step =
                    direction *
                    stepDistance;


                float targetY =
                    position.y +
                    step;


                if (
                    IsColliding(
                        position.x,
                        targetY
                    )
                )
                {
                    position.y =
                        GetSafeVerticalPosition(
                            position,
                            direction
                        );


                    velocity.y =
                        0f;


                    if (
                        direction < 0f
                    )
                    {
                        IsGrounded =
                            true;
                    }
                    else
                    {
                        HitCeiling =
                            true;
                    }


                    break;
                }


                position.y =
                    targetY;
            }


            return position;
        }


        // =====================================================
        // “Œ◊Õ¿ﬂ œŒ«»÷»ﬂ œ–» —“ŒÀ ÕŒ¬≈Õ»» œŒ X
        // =====================================================

        private float GetSafeHorizontalPosition(
            Vector2 position,
            float direction
        )
        {
            float halfWidth =
                width *
                0.5f;


            if (
                direction > 0f
            )
            {
                int blockX =
                    Mathf.FloorToInt(
                        position.x +
                        halfWidth
                    );


                float blockLeft =
                    blockX;


                return
                    blockLeft -
                    halfWidth -
                    Skin;
            }


            int leftBlockX =
                Mathf.FloorToInt(
                    position.x -
                    halfWidth
                );


            float blockRight =
                leftBlockX +
                1f;


            return
                blockRight +
                halfWidth +
                Skin;
        }


        // =====================================================
        // “Œ◊Õ¿ﬂ œŒ«»÷»ﬂ œ–» —“ŒÀ ÕŒ¬≈Õ»» œŒ Y
        // =====================================================

        private float GetSafeVerticalPosition(
            Vector2 position,
            float direction
        )
        {
            float halfHeight =
                height *
                0.5f;


            if (
                direction > 0f
            )
            {
                int blockY =
                    Mathf.FloorToInt(
                        position.y +
                        halfHeight
                    );


                float blockBottom =
                    blockY;


                return
                    blockBottom -
                    halfHeight -
                    Skin;
            }


            int bottomBlockY =
                Mathf.FloorToInt(
                    position.y -
                    halfHeight
                );


            float blockTop =
                bottomBlockY +
                1f;


            return
                blockTop +
                halfHeight +
                Skin;
        }


        // =====================================================
        // œ–Œ¬≈– ¿ œ≈–≈—≈◊≈Õ»ﬂ — Ã»–ŒÃ
        // =====================================================

        private bool IsColliding(
            float centerX,
            float centerY
        )
        {
            float halfWidth =
                width *
                0.5f;


            float halfHeight =
                height *
                0.5f;


            float minX =
                centerX -
                halfWidth +
                Skin;


            float maxX =
                centerX +
                halfWidth -
                Skin;


            float minY =
                centerY -
                halfHeight +
                Skin;


            float maxY =
                centerY +
                halfHeight -
                Skin;


            int minBlockX =
                Mathf.FloorToInt(
                    minX
                );


            int maxBlockX =
                Mathf.FloorToInt(
                    maxX
                );


            int minBlockY =
                Mathf.FloorToInt(
                    minY
                );


            int maxBlockY =
                Mathf.FloorToInt(
                    maxY
                );


            for (
                int x = minBlockX;
                x <= maxBlockX;
                x++
            )
            {
                for (
                    int y = minBlockY;
                    y <= maxBlockY;
                    y++
                )
                {
                    if (
                        collision.IsSolid(
                            x,
                            y
                        )
                    )
                    {
                        return true;
                    }
                }
            }


            return false;
        }
    }
}