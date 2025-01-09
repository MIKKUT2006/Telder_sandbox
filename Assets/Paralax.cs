using UnityEditor.Rendering;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    [SerializeField] Transform followingTarget;
    [SerializeField, Range(0f,1f)] float parallaxStrength = 0.1f;
    [SerializeField] bool disableVerticalParallax;
    Vector3 targetPreviousPosition;

    private void Start()
    {
        if (!followingTarget) 
        {
            followingTarget = Camera.main.transform;
        }

        targetPreviousPosition = followingTarget.position;
    }

    private void Update()
    {
        Vector3 delta = followingTarget.position - targetPreviousPosition;

        if (disableVerticalParallax)
        {
            delta.y = Camera.main.transform.position.y;
        }

        targetPreviousPosition = followingTarget.position;
        //transform.position = new Vector3 (transform.position.x + (delta.x * parallaxStrength), Camera.main.transform.position.y, 0);
        transform.position += delta * parallaxStrength;
    }
}
