using UnityEngine;

namespace Game.Audio
{
    /// <summary>Lightweight animation-free movement sounds for the custom 2D motor.</summary>
    [DisallowMultipleComponent]
    public sealed class TelderPlayerMovementAudio : MonoBehaviour
    {
        private PlayerCollision collision;
        private PlayerController controller;
        private Game.Harpoon.HarpoonController2D harpoon;
        private Vector3 last;
        private float traveled;
        private bool wasGrounded;
        private bool ready;

        private void Awake()
        {
            collision = GetComponent<PlayerCollision>();
            controller = GetComponent<PlayerController>();
            harpoon = GetComponent<Game.Harpoon.HarpoonController2D>();
            last = transform.position;
        }

        private void LateUpdate()
        {
            if (collision == null || controller == null) return;
            var current = transform.position;
            if (!ready)
            {
                ready = true;
                wasGrounded = collision.IsGrounded;
                last = current;
                return;
            }
            Vector3 change = current - last;
            last = current;
            if (change.sqrMagnitude > 20f || Time.deltaTime > .5f)
            {
                // Teleport/spawn correction is not a step, jump or landing.
                traveled = 0f;
                wasGrounded = collision.IsGrounded;
                return;
            }
            bool grapple = harpoon != null && harpoon.IsAttached;
            bool grounded = collision.IsGrounded;
            bool liquid = controller.IsInLiquid;
            if (grapple || liquid)
            {
                wasGrounded = grounded;
                traveled = 0f;
                return;
            }
            if (grounded && !wasGrounded && change.y < .07f)
                TelderAudioSystem.PlayAt("player.land", current, GetInstanceID());
            else if (!grounded && wasGrounded && change.y > .018f)
                TelderAudioSystem.PlayAt("player.jump", current, GetInstanceID());

            if (grounded && Mathf.Abs(change.x) < .4f)
            {
                traveled += Mathf.Abs(change.x);
                if (traveled >= .92f)
                {
                    traveled -= .92f;
                    TelderAudioSystem.PlayAt("player.step", current, GetInstanceID());
                }
            }
            else if (!grounded) traveled = 0f;
            wasGrounded = grounded;
        }
    }
}
