using UnityEngine;

public class PlayerDollCamera : MonoBehaviour
{
    public Transform player; // Прикрепите объект игрока сюда в инспекторе
    //public Vector3 offset;  // Смещение камеры от игрока

    void LateUpdate()
    {
        if (player != null)
        {
            //transform.position = player.position;
            transform.position = new Vector3(player.position.x, player.position.y, player.position.z - 1);
        }
    }
}
