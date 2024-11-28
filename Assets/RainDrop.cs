using UnityEngine;

public class RainDrop : MonoBehaviour
{
    public float fallSpeed = 5f; // Скорость падения капли
    public GameObject splashEffectPrefab; // Префаб эффекта разбрызгивания

    private void Update()
    {
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

        // Проверка на наличие коллизии с землёй
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.1f);
        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Ground"))
            {
                Instantiate(splashEffectPrefab, transform.position, Quaternion.identity);
                FindObjectOfType<RainPool>().ReturnRainDrop(gameObject);
            }
        }

        if (transform.position.y < -5)
        {
            FindObjectOfType<RainPool>().ReturnRainDrop(gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 3)
        {
            Instantiate(splashEffectPrefab, transform.position, Quaternion.identity);
            FindObjectOfType<RainPool>().ReturnRainDrop(gameObject);
        }
    }
}
