using UnityEngine;
using Game.World;
using Game.World.Loading;


public class PlayerController : MonoBehaviour
{

    [Header("Movement")]

    [SerializeField]
    private float moveSpeed = 5f;


    [SerializeField]
    private float jumpForce = 8f;


    [Header("Ground Check")]

    [SerializeField]
    private Transform groundCheck;


    [SerializeField]
    private float groundCheckRadius = 0.2f;


    [SerializeField]
    private LayerMask groundLayer;


    private Rigidbody2D rb;

    private ChunkLoader chunkLoader;

    private int currentChunkX;

    private int currentChunkY;


    private void Awake()
    {

        rb =
            GetComponent<Rigidbody2D>();

    }


    private void Start()
    {

        WorldManager worldManager =
            WorldManager.Instance;


        chunkLoader =
            worldManager.GetLoader();


        UpdateCurrentChunk(
            true
        );

    }


    private void Update()
    {

        HandleJump();

        UpdateCurrentChunk(
            false
        );

    }


    private void FixedUpdate()
    {

        HandleMovement();

    }


    private void HandleMovement()
    {

        float horizontal =
            Input.GetAxisRaw(
                "Horizontal"
            );


        rb.linearVelocity =
            new Vector2(
                horizontal *
                moveSpeed,

                rb.linearVelocity.y
            );

    }


    private void HandleJump()
    {

        if (
            !Input.GetKeyDown(
                KeyCode.Space
            )
        )
        {

            return;

        }


        if (
            !IsGrounded()
        )
        {

            return;

        }


        rb.linearVelocity =
            new Vector2(
                rb.linearVelocity.x,

                jumpForce
            );

    }


    private bool IsGrounded()
    {

        if (
            groundCheck == null
        )
        {

            return false;

        }


        return Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

    }


    private void UpdateCurrentChunk(
        bool forceUpdate
    )
    {

        int chunkX =
            Mathf.FloorToInt(
                transform.position.x /
                Chunk.SizeX
            );


        int chunkY =
            Mathf.FloorToInt(
                transform.position.y /
                Chunk.SizeY
            );


        if (
            !forceUpdate &&
            chunkX == currentChunkX &&
            chunkY == currentChunkY
        )
        {

            return;

        }


        currentChunkX =
            chunkX;


        currentChunkY =
            chunkY;

        if (
    WorldManager.Instance == null
)
        {
            return;
        }


        if (
            !WorldManager.Instance.IsReady
        )
        {
            return;
        }


        WorldManager.Instance.UpdatePlayerChunk(
            currentChunkX,
            currentChunkY
        );


    }


    private void OnDrawGizmosSelected()
    {

        if (
            groundCheck == null
        )
        {

            return;

        }


        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );

    }

}