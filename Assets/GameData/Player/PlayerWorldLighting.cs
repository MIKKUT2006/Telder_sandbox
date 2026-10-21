using UnityEngine;
using Game.World.Lighting;

/// <summary>
/// Compatibility component retained for existing prefabs and saved scene refs.
/// Lighting is now applied exclusively by WorldLightSpriteReceiver to prevent
/// the same sprite being darkened two (or three) times each frame.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerWorldLighting : MonoBehaviour
{
    private void Awake()
    {
        if (GetComponent<WorldLightSpriteReceiver>() == null)
            gameObject.AddComponent<WorldLightSpriteReceiver>();
        enabled = false;
    }
}
