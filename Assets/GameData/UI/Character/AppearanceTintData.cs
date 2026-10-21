using UnityEngine;

namespace Game.UI.Character
{
    /// <summary>
    /// Per-renderer appearance tint. Kept separate from SpriteRenderer.color so the world
    /// lighting system can combine appearance, authored sprite color, and sampled light.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AppearanceTintData : MonoBehaviour
    {
        [SerializeField] private Color tint = Color.white;

        public Color Tint
        {
            get { return tint; }
            set { tint = value; }
        }
    }
}
