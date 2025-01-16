using UnityEditor.Rendering;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    private float _startingPos; //This is starting position of the sprites.
    private float _lengthOfSprite;    //This is the length of the sprites.
    public float AmountOfParallax;  //This is amount of parallax scroll. 
    public float zPosition;  //This is amount of parallax scroll. 
    public Camera MainCamera;   //Reference of the camera.

    private void Start()
    {
        //Getting the starting X position of sprite.
        _startingPos = transform.position.x;
        //Getting the length of the sprites.
        _lengthOfSprite = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    private void FixedUpdate()
    {
        Vector3 Position = MainCamera.transform.position;
        float Temp = Position.x * (1 - AmountOfParallax);
        float Distance = Position.x * AmountOfParallax;

        Vector3 NewPosition = new Vector3(_startingPos + Distance, MainCamera.transform.position.y, zPosition);

        transform.position = NewPosition;

        if (Temp > _startingPos + (_lengthOfSprite / 2))
        {
            _startingPos += _lengthOfSprite;
        }
        else if (Temp < _startingPos - (_lengthOfSprite / 2))
        {
            _startingPos -= _lengthOfSprite;
        }
    }
}
