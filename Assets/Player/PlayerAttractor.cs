using Unity.VisualScripting;
using UnityEngine;

public class PlayerAttractor : MonoBehaviour
{
    public float attractRadius;  // ������ ����������
    public float attractForce;    // ���� ����������
    public GameObject playerGameObject; // ������� ������ ������


    private void OnTriggerStay2D(Collider2D collision)
    {
        // ���������, �������� �� ������, �������� � �������, ������������� ��������
        if (collision.gameObject.tag == "Item")
        {
            // ����� ����������� �� ������� ������ � �������������� �������
            Vector3 direction = (playerGameObject.transform.position - collision.transform.position).normalized;

            // ��������� ���� ���������� � �������������� �������
            collision.GetComponent<Rigidbody2D>().AddForce(direction * attractForce, ForceMode2D.Force);
            collision.GetComponent<CircleCollider2D>().isTrigger = true;
            collision.GetComponent<Rigidbody2D>().gravityScale = 0;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Item")
        {
            // ����� ����������� �� ������� ������ � �������������� �������
            Vector3 direction = (playerGameObject.transform.position - collision.transform.position).normalized;
            collision.GetComponent<CircleCollider2D>().isTrigger = false;
            collision.GetComponent<Rigidbody2D>().gravityScale = 1;
        }
    }
}
