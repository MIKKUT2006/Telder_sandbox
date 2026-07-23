using UnityEngine;
using Game.Content;


public class GameBootstrap : MonoBehaviour
{

    private void Awake()
    {

        ContentManager.Initialize();

    }

}