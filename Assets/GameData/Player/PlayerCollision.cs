
using UnityEngine;
using Game.World.Collision;


public class PlayerCollision : MonoBehaviour
{

    [Header("Collision")]

    [SerializeField]
    private Vector2 colliderSize =
        new Vector2(
            0.8f,
            1.8f
        );


    [SerializeField]
    private float skin =
        0.02f;


    [Header("World")]

    [SerializeField]
    private float blockSize =
        1f;


    private WorldCollision worldCollision;


    public bool IsGrounded
    {
        get;
        private set;
    }


    // =====================================================
    // INITIALIZE
    // =====================================================

    public void Initialize(
        WorldCollision worldCollision
    )
    {

        this.worldCollision =
            worldCollision;

    }


    // =====================================================
    // MOVE
    // =====================================================

    public void Move(
    Vector2 movement
)
    {

        if (
            worldCollision == null
        )
        {
            return;
        }


        IsGrounded =
            false;


        // Проверяем землю
        // перед движением.

        CheckGround();


        // =================================================
        // X
        // =================================================

        if (
            movement.x != 0f
        )
        {

            MoveHorizontal(
                movement.x
            );

        }


        // =================================================
        // Y
        // =================================================

        if (
            movement.y != 0f
        )
        {

            MoveVertical(
                movement.y
            );

        }


        // Проверяем землю
        // после движения.

        CheckGround();

    }


    // =====================================================
    // HORIZONTAL
    // =====================================================

    private void MoveHorizontal(
        float movement
    )
    {

        float direction =
            Mathf.Sign(
                movement
            );


        float distance =
            Mathf.Abs(
                movement
            );


        float targetX =
            transform.position.x +
            movement;


        // Полностью свободное движение.

        if (
            !CheckCollisionAt(
                targetX,
                transform.position.y
            )
        )
        {

            transform.position =
                new Vector3(
                    targetX,
                    transform.position.y,
                    transform.position.z
                );


            return;

        }


        // =================================================
        // ДВИЖЕНИЕ ДО ПРЕПЯТСТВИЯ
        // =================================================

        float step =
            0.01f;


        float moved =
            0f;


        while (
            moved <
            distance
        )
        {

            float next =
                moved +
                step;


            if (
                next >
                distance
            )
            {

                next =
                    distance;

            }


            float testX =
                transform.position.x +
                direction *
                next;


            if (
                CheckCollisionAt(
                    testX,
                    transform.position.y
                )
            )
            {

                break;

            }


            moved =
                next;

        }


        transform.position =
            new Vector3(
                transform.position.x +
                direction *
                moved,
                transform.position.y,
                transform.position.z
            );

    }


    // =====================================================
    // VERTICAL
    // =====================================================

    private void MoveVertical(
        float movement
    )
    {

        float direction =
            Mathf.Sign(
                movement
            );


        float distance =
            Mathf.Abs(
                movement
            );


        float targetY =
            transform.position.y +
            movement;


        // Полностью свободное движение.

        if (
            !CheckCollisionAt(
                transform.position.x,
                targetY
            )
        )
        {

            transform.position =
                new Vector3(
                    transform.position.x,
                    targetY,
                    transform.position.z
                );


            return;

        }


        // =================================================
        // ДВИЖЕНИЕ ДО ПРЕПЯТСТВИЯ
        // =================================================

        float step =
            0.01f;


        float moved =
            0f;


        while (
            moved <
            distance
        )
        {

            float next =
                moved +
                step;


            if (
                next >
                distance
            )
            {

                next =
                    distance;

            }


            float testY =
                transform.position.y +
                direction *
                next;


            if (
                CheckCollisionAt(
                    transform.position.x,
                    testY
                )
            )
            {

                break;

            }


            moved =
                next;

        }


        transform.position =
            new Vector3(
                transform.position.x,
                transform.position.y +
                direction *
                moved,
                transform.position.z
            );


        // Если двигались вниз
        // и встретили блок,
        // считаем игрока стоящим на земле.

        if (
            direction < 0f
        )
        {

            IsGrounded =
                true;

        }

    }


    // =====================================================
    // GROUND CHECK
    // =====================================================

    private void CheckGround()
    {

        float bottom =
            transform.position.y -
            colliderSize.y *
            0.5f;


        float checkY =
            bottom -
            skin;


        if (
            CheckCollisionAt(
                transform.position.x,
                checkY
            )
        )
        {

            IsGrounded =
                true;

        }

    }


    // =====================================================
    // COLLISION CHECK
    // =====================================================

    private bool CheckCollisionAt(
        float centerX,
        float centerY
    )
    {

        // =================================================
        // HALF SIZE
        // =================================================

        float halfWidth =
            colliderSize.x *
            0.5f;


        float halfHeight =
            colliderSize.y *
            0.5f;


        // =================================================
        // BOUNDS
        // =================================================

        float minX =
            centerX -
            halfWidth +
            skin;


        float maxX =
            centerX +
            halfWidth -
            skin;


        float minY =
            centerY -
            halfHeight +
            skin;


        float maxY =
            centerY +
            halfHeight -
            skin;


        // =================================================
        // BLOCK COORDINATES
        // =================================================

        int minBlockX =
            Mathf.FloorToInt(
                minX /
                blockSize
            );


        int maxBlockX =
            Mathf.FloorToInt(
                maxX /
                blockSize
            );


        int minBlockY =
            Mathf.FloorToInt(
                minY /
                blockSize
            );


        int maxBlockY =
            Mathf.FloorToInt(
                maxY /
                blockSize
            );


        // =================================================
        // CHECK BLOCKS
        // =================================================

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
                    worldCollision.IsSolid(
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

