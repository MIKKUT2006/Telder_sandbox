using Unity.VisualScripting;
using UnityEngine;

public class PlayerAttractor : MonoBehaviour
{
    public float attractRadius;  // Радиус притяжения
    public float attractForce;    // Сила притяжения
    public GameObject playerGameObject; // Игровой объект игрока


    private void OnTriggerStay2D(Collider2D collision)
    {
        // Проверить, является ли объект, вошедший в триггер, притягиваемым объектом
        if (collision.gameObject.tag == "Item")
        {
            // Найти направление от объекта игрока к притягиваемому объекту
            Vector3 direction = (playerGameObject.transform.position - collision.transform.position).normalized;

            // Применить силу притяжения к притягиваемому объекту
            collision.GetComponent<Rigidbody2D>().AddForce(direction * attractForce, ForceMode2D.Force);
            collision.GetComponent<CircleCollider2D>().isTrigger = true;
            collision.GetComponent<Rigidbody2D>().gravityScale = 0;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Item")
        {
            // Найти направление от объекта игрока к притягиваемому объекту
            Vector3 direction = (playerGameObject.transform.position - collision.transform.position).normalized;
            collision.GetComponent<CircleCollider2D>().isTrigger = false;
            collision.GetComponent<Rigidbody2D>().gravityScale = 1;
        }
    }
}
