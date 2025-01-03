using UnityEngine;

[ExecuteInEditMode]
public class WiggleScale : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Material _material;

    private void OnEnable()
    {
        UpdateScale();
    }

    private void OnValidate()
    {
        UpdateScale();
    }

    private void UpdateScale()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (_spriteRenderer == null)
        {
            return;
        }

        if (_material == null)
        {
            if (_spriteRenderer.sharedMaterial == null)
            {
                return;
            }
            _material = _spriteRenderer.sharedMaterial;
        }
        if (_material == null)
        {
            return;
        }
        _material.SetVector("_ObjectScale", new Vector4(_spriteRenderer.bounds.size.x, _spriteRenderer.bounds.size.y, 1, 1));
        // Set pivot offset in shader
        Vector3 spritePivot = new Vector3(_spriteRenderer.sprite.pivot.x / _spriteRenderer.sprite.rect.width, _spriteRenderer.sprite.pivot.y / _spriteRenderer.sprite.rect.height, 0);
        // Calculate offset using sprite size and position to compensate for the object's own position
        Vector3 offset = new Vector3(
             -_spriteRenderer.bounds.size.x * (spritePivot.x - 0.5f),
             -_spriteRenderer.bounds.size.y * (spritePivot.y - 0.5f),
              0
         );

        _material.SetVector("_PivotOffset", new Vector4(offset.x, offset.y, 0, 0));

    }
}

