using UnityEngine;
using static UnityEngine.ParticleSystem;

public class ProjectileScript : MonoBehaviour
{
    public int damage = 10;
    public float lifeTime = 5f;
    public LayerMask groundMask;
    public ParticleSystem particles;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerData.SetDamage(damage);
            Destroy(gameObject);
        }

        // Если попал в землю или игрока — уничтожаем
        if (((1 << collision.gameObject.layer) & groundMask) != 0 || collision.gameObject.CompareTag("Player"))
        {
            Instantiate(particles, this.gameObject.transform);
            Destroy(gameObject);
        }
    }
}
