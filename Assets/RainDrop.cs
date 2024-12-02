using UnityEngine;

public class RainDrop : MonoBehaviour
{
    public GameObject splashEffectPrefab; // Префаб эффекта разбрызгивания
    public bool isSnow = false;

    private void Update()
    {
        transform.Translate(Vector3.down * HelperClass.weatherFallSpeed * Time.deltaTime);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 3)
        {
            if (!isSnow)
            {
                Instantiate(splashEffectPrefab, transform.position, Quaternion.identity);
            }
            FindObjectOfType<RainPool>().ReturnRainDrop(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.layer == 3) 
        {
            if (!isSnow)
            {
                Instantiate(splashEffectPrefab, transform.position, Quaternion.identity);
            }
            FindObjectOfType<RainPool>().ReturnRainDrop(gameObject);
        }
    }
}
