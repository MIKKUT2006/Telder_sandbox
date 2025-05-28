using UnityEditor.Rendering;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField, Range(0f, 1f)] private float horizontalParallaxMultiplier = 0.2f;
    [SerializeField, Range(0f, 1f)] private float verticalParallaxMultiplier = 0.2f;

    private Vector2 spriteHalfSizeWorld;

    private void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        spriteHalfSizeWorld = sr.bounds.size / 2f;
    }

    private void FixedUpdate()
    {
        Vector3 camPos = new Vector3(cameraTransform.position.x, cameraTransform.position.y, 0);
        Vector3 camSize = new Vector3(
            Camera.main.orthographicSize * Camera.main.aspect,
            Camera.main.orthographicSize,
            0f
        );

        Vector3 offset = new Vector3(
            Mathf.Clamp(camPos.x * horizontalParallaxMultiplier, -spriteHalfSizeWorld.x + camSize.x, spriteHalfSizeWorld.x - camSize.x),
            Mathf.Clamp(camPos.y * verticalParallaxMultiplier, -spriteHalfSizeWorld.y + camSize.y, spriteHalfSizeWorld.y - camSize.y),
            0f
        );

        transform.position = camPos + offset;
    }
}
