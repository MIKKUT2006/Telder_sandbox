using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    [Header ("Основные характеристики противника")]
    public float speed = 2f;
    public float jumpForce = 5f;
    public float attackRange = 1f;
    public int damage = 10;
    public float detectionRange = 5f;
    public LayerMask groundLayer;
    public Transform feetPosition;
    public float obstacleCheckDistance = 1.5f;
    public float groundCheckDistance = 0.1f;
    public float jumpCooldown = 0.5f;
    public float jumpHeightCheck = 0.5f;
    public LayerMask playerLayer; // Новый слой для игрока

    private Rigidbody2D rb;
    private Animator animator;
    private Transform player;
    private bool isGrounded;
    private float lastJumpTime;
    private bool isJumping = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        if (feetPosition == null)
        {
            Debug.LogError("Assign feet position to Zombie");
            enabled = false;
            return;
        }
        rb.gravityScale = 1f;
    }


}
