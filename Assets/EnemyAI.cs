using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public ProceduralGeneration proceduralGeneration; // ваш генератор с public static int[,] map
    public LayerMask groundMask;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float jumpForce = 6f;
    public float blockSize = 1f;

    [Header("AI Ranges")]
    public float visionRange = 15f;
    public float attackRange = 1.5f;
    public float wanderRange = 5f;
    public float wanderCooldown = 3f;
    public float shootCooldown = 2f;
    private float shootTimer;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public float projectileSpeed = 8f;
    public float arcHeight = 2f;

    private Rigidbody2D rb;
    private float randomTimer;
    private Vector2 randomTarget;
    private float dir; // -1, 0 или +1
    public enum State { Wandering, Chasing, Attacking }
    public State state = State.Wandering;
    // Здоровье врага
    public int currentHealth;
    public int maxHealth;
    private float knockbackTimer = 0f;
    bool isKnockedBack = false;
    void Start()
    {
        player = HelperClass.playerGameObject.transform;

        rb = GetComponent<Rigidbody2D>();
        PickNewWanderTarget();
        // Устанавливаем значение здоровья по умолчанию
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage, Vector2 knockback)
    {
        currentHealth -= damage;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockback, ForceMode2D.Impulse);

        isKnockedBack = true;
        knockbackTimer = 0.3f;

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        Destroy(gameObject);
    }
    void Update()
    {
        // Состояния
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= attackRange)
            state = State.Attacking;
        else if (dist <= visionRange && CanSeePlayer())
            state = State.Chasing;
        else if (state != State.Wandering)
        {
            state = State.Wandering;
            PickNewWanderTarget();
        }

        // Направление
        if (state == State.Chasing)
            dir = Mathf.Sign(player.position.x - transform.position.x);
        else if (state == State.Wandering)
        {
            randomTimer -= Time.deltaTime;
            if (randomTimer <= 0f || Mathf.Abs(transform.position.x - randomTarget.x) < 0.5f)
                PickNewWanderTarget();
            dir = Mathf.Sign(randomTarget.x - transform.position.x);
        }
        else // Attacking
            dir = 0;

        if (dir != 0)
            transform.localScale = new Vector3(Mathf.Sign(dir), 1, 1);

        if (state == State.Attacking)
        {
            shootTimer -= Time.deltaTime;
            if (shootTimer <= 0f)
            {
                ShootAtPlayer();
                shootTimer = shootCooldown;
            }
        }
    }

    void FixedUpdate()
    {
        if (isKnockedBack)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            if (knockbackTimer <= 0f)
                isKnockedBack = false;
            return; // ← не двигаемся во время отталкивания
        }

        // Вычисляем, есть ли земля на глубине 1 и 2 блоков впереди
        bool ground1 = IsBlockAtOffset(dir, -1);  // блок на 1 ниже
        bool ground2 = IsBlockAtOffset(dir, -2);  // блок на 2 ниже

        // Препятствие: один блок на уровне ног
        bool obstacleAtFeet = IsBlockAtOffset(dir, 0);
        // Второй блок над ногами
        bool obstacleAbove = IsBlockAtOffset(dir, +1);

        // Проверяем «глубину ямы»:
        // если нет земли ни на 1-м, ни на 2-м блоке — слишком глубокая яма, стоп
        bool deepPit = false;
        if (state != State.Chasing)
        {
            deepPit = !ground1 && !ground2;
        }

        // Горизонтальная скорость: если глубокая яма — 0, иначе — движемся
        float vx = deepPit ? 0f : dir * moveSpeed;

        // Прыжок: только на ровном или мелкой яме, и лишь через один блок
        if (!deepPit && ground1 && obstacleAtFeet && !obstacleAbove)
        {
            rb.linearVelocity = new Vector2(vx, jumpForce);
        }
        else
        {
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        }
    }

    bool IsBlockAtOffset(float xDir, int yOffset)
    {
        //Vector3 p = transform.position;
        //int x = Mathf.FloorToInt((p.x + xDir * blockSize) / blockSize);
        //int y = Mathf.FloorToInt(p.y / blockSize) + yOffset;

        //var map = ProceduralGeneration.map;
        //if (x < 0 || y < 0 || x >= map.GetLength(0) || y >= map.GetLength(1))
        //    return false;

        //// Пусто, если == 0 или == 4
        //return !IsEmpty(map[x, y]);
        //Убрать при раскомментировании
        return true;
    }

    bool CanSeePlayer()
    {
        Vector2 a = transform.position;
        Vector2 b = player.position;
        int steps = Mathf.CeilToInt(Vector2.Distance(a, b) / blockSize);
        //var map = ProceduralGeneration.map;

        for (int i = 0; i <= steps; i++)
        {
            Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
            int x = Mathf.FloorToInt(p.x / blockSize);
            int y = Mathf.FloorToInt(p.y / blockSize);

            //if (x < 0 || y < 0 || x >= map.GetLength(0) || y >= map.GetLength(1))
            //    return false;
            //if (!IsEmpty(map[x, y]))
            //    return false;
        }

        return true;
    }

    void ShootAtPlayer()
    {
        if (projectilePrefab == null || shootPoint == null || player == null) return;

        Vector2 target = player.position;
        Vector2 startPos = shootPoint.position;

        if (Mathf.Abs(target.x - startPos.x) < 0.1f)
        {
            // Цель почти по вертикали, не стреляем
            return;
        }

        GameObject proj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
        Rigidbody2D projRb = proj.GetComponent<Rigidbody2D>();
        if (projRb != null)
        {
            Vector2 velocity = CalculateArcVelocity(startPos, target, arcHeight);

            if (!float.IsNaN(velocity.x) && !float.IsNaN(velocity.y))
            {
                projRb.linearVelocity = velocity;
            }
        }
    }
    Vector2 CalculateArcVelocity(Vector2 startPoint, Vector2 endPoint, float arcHeight)
    {
        float gravity = Mathf.Abs(Physics2D.gravity.y);

        // Расстояния
        float displacementY = endPoint.y - startPoint.y;
        float displacementX = endPoint.x - startPoint.x;

        if (Mathf.Abs(displacementX) < 0.1f)
        {
            // Очень маленькое горизонтальное расстояние - возвращаем прямо вверх
            return new Vector2(0, Mathf.Sqrt(2 * gravity * arcHeight));
        }

        // Время подъема до вершины дуги
        float timeUp = Mathf.Sqrt(2 * arcHeight / gravity);
        // Скорость по оси Y
        float velocityY = gravity * timeUp;

        // Время спуска от вершины дуги до цели
        float timeDown = Mathf.Sqrt(2 * (arcHeight - displacementY) / gravity);

        float totalTime = timeUp + timeDown;

        // Скорость по оси X
        float velocityX = displacementX / totalTime;

        return new Vector2(velocityX, velocityY);
    }
    void PickNewWanderTarget()
    {
        float offset = Random.Range(-wanderRange, wanderRange);
        randomTarget = new Vector2(transform.position.x + offset, transform.position.y);
        randomTimer = wanderCooldown;
    }

    // 0 и 4 считаются пустыми
    bool IsEmpty(int cellValue)
    {
        return cellValue == 0 || cellValue == 4;
    }

    void OnDrawGizmos()
    {
        if (player == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.gameObject.transform.position, attackRange);
    }
}
