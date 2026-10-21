using UnityEngine;
using TMPro;

namespace Game.Networking
{
    /// <summary>Visual-only representation of another network participant.</summary>
    public sealed class RemotePlayerAvatar : MonoBehaviour
    {
        private SpriteRenderer sprite;
        private TextMesh label;
        private Vector3 targetPosition;
        private bool hasTarget;
        private string dimension = "surface";
        public string Dimension => dimension;

        public void Initialize(string nick)
        {
            var local = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            SpriteRenderer source = null;
            if (local.Length > 0)
                source = local[0].GetComponentInChildren<SpriteRenderer>(true);
            sprite = gameObject.AddComponent<SpriteRenderer>();
            if (source != null)
            {
                sprite.sprite = source.sprite;
                sprite.sortingLayerID = source.sortingLayerID;
                sprite.sortingOrder = source.sortingOrder + 1;
                sprite.flipX = source.flipX;
                sprite.color = source.color;
            }
            else
            {
                // Safe visible fallback until the local player sprite is ready.
                var texture = Texture2D.whiteTexture;
                sprite.sprite = Sprite.Create(texture, new Rect(0,0,1,1), new Vector2(.5f,0), 16);
                sprite.color = new Color(.35f,.8f,1f,1f);
            }
            var labelObject = new GameObject("Nickname");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0, 1.5f, 0);
            label = labelObject.AddComponent<TextMesh>();
            label.text = nick;
            label.anchor = TextAnchor.LowerCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 48;
            label.characterSize = .035f;
            label.color = Color.white;
            label.font = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
            var meshRenderer = labelObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = labelObject.AddComponent<MeshRenderer>();
            meshRenderer.sortingOrder = 500;
            targetPosition = transform.position;
        }

        public void SetNick(string nick) { if (label != null) label.text = nick; }
        public void SetTarget(Vector3 position, string dimensionName)
        {
            targetPosition = position;
            dimension = string.IsNullOrWhiteSpace(dimensionName) ? "surface" : dimensionName;
            hasTarget = true;
        }
        public void SetVisibleInDimension(bool visible)
        {
            if (sprite != null) sprite.enabled = visible;
            if (label != null) label.GetComponent<MeshRenderer>().enabled = visible;
        }
        private void Update()
        {
            if (hasTarget) transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Exp(-18f * Time.deltaTime));
            if (label != null && Camera.main != null)
            {
                Vector3 dir = label.transform.position - Camera.main.transform.position;
                label.transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }
}
