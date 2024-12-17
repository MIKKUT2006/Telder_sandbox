using UnityEngine;

public class PlayerDollCamera : MonoBehaviour
{
    public Transform player; // ���������� ������ ������ ���� � ����������
    //public Vector3 offset;  // �������� ������ �� ������

    void LateUpdate()
    {
        if (player != null)
        {
            //transform.position = player.position;
            transform.position = new Vector3(player.position.x, player.position.y, player.position.z - 1);
        }
    }
}
