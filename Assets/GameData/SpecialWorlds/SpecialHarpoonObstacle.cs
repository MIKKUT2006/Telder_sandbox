using System;
using UnityEngine;

namespace Game.SpecialWorlds
{
    /// <summary>Local hazards for the standalone harpoon challenge. Never touches survival HP.</summary>
    public sealed class SpecialHarpoonObstacle : MonoBehaviour
    {
        public enum Pattern { StaticSpikes, Pendulum, TimedGate }

        private Pattern pattern;
        private Rigidbody2D player;
        private Action onHit;
        private Collider2D hitbox;
        private SpriteRenderer visual;
        private LineRenderer chain;
        private Vector2 anchor;
        private float seedOffset;
        private float span;
        private float period;
        private float baseAlpha;
        private float disableUntil;
        private Rigidbody2D movingBody;

        public void Configure(Pattern kind, Rigidbody2D target, Action hitCallback,
            Vector2 anchorPosition, float swingSize, float timingOffset, float cycleSeconds)
        {
            pattern = kind;
            player = target;
            onHit = hitCallback;
            anchor = anchorPosition;
            span = swingSize;
            seedOffset = timingOffset;
            period = Mathf.Max(.8f, cycleSeconds);
            hitbox = GetComponent<Collider2D>();
            visual = GetComponent<SpriteRenderer>();
            if (visual != null) baseAlpha = visual.color.a;
            if (pattern == Pattern.Pendulum)
            {
                movingBody = gameObject.AddComponent<Rigidbody2D>();
                movingBody.bodyType = RigidbodyType2D.Kinematic;
                movingBody.simulated = true;
                movingBody.interpolation = RigidbodyInterpolation2D.Interpolate;

                var chainObject = new GameObject("PendulumCable");
                chainObject.transform.SetParent(transform, false);
                chain = chainObject.AddComponent<LineRenderer>();
                chain.useWorldSpace = true;
                chain.positionCount = 2;
                chain.startWidth = chain.endWidth = .055f;
                chain.material = new Material(Shader.Find("Sprites/Default"));
                chain.startColor = new Color(.86f, .91f, .96f, .8f);
                chain.endColor = new Color(.86f, .91f, .96f, .8f);
                chain.sortingOrder = 11;
            }
        }

        private void FixedUpdate()
        {
            if (pattern != Pattern.Pendulum || movingBody == null) return;
            float clock = Time.time * (2f * Mathf.PI / period) + seedOffset;
            float angle = Mathf.Sin(clock) * .95f;
            // The moving stone never rises above its ceiling anchor.
            Vector2 at = anchor + new Vector2(Mathf.Sin(angle) * span, -Mathf.Cos(angle) * span);
            movingBody.MovePosition(at);
        }

        private void Update()
        {
            if (pattern == Pattern.TimedGate)
            {
                // One window is safe for crossing. The fading warning precedes activation.
                float clock = Mathf.Repeat(Time.time + seedOffset, period) / period;
                bool lethal = clock > .54f;
                if (hitbox != null) hitbox.enabled = lethal;
                if (visual != null)
                {
                    Color c = visual.color;
                    c.a = lethal ? baseAlpha : (clock > .43f ? baseAlpha * .42f : baseAlpha * .10f);
                    visual.color = c;
                }
            }
            if (chain != null)
            {
                chain.SetPosition(0, anchor);
                chain.SetPosition(1, transform.position);
            }
        }

        private void OnTriggerEnter2D(Collider2D other) { TryHit(other); }
        private void OnTriggerStay2D(Collider2D other) { TryHit(other); }

        private void TryHit(Collider2D other)
        {
            if (Time.time < disableUntil || player == null || onHit == null || other == null)
                return;
            if (other.attachedRigidbody != player) return;
            if (pattern == Pattern.TimedGate && hitbox != null && !hitbox.enabled) return;
            disableUntil = Time.time + 1.25f;
            onHit();
        }

        private void OnDestroy()
        {
            if (chain != null && chain.material != null) Destroy(chain.material);
        }
    }

    /// <summary>Temporary foothold over an abyss. Disappears shortly after being touched.</summary>
    public sealed class SpecialHarpoonFragilePlatform : MonoBehaviour
    {
        private Rigidbody2D player;
        private BoxCollider2D solid;
        private SpriteRenderer graphic;
        private float crumbleAt = -1f;
        private bool broken;

        public void Configure(Rigidbody2D target)
        {
            player = target;
            solid = GetComponent<BoxCollider2D>();
            graphic = GetComponent<SpriteRenderer>();
        }

        private void OnCollisionEnter2D(Collision2D impact)
        {
            if (player == null || impact.rigidbody != player || broken || crumbleAt >= 0f) return;
            crumbleAt = Time.time + .85f;
        }

        private void Update()
        {
            if (crumbleAt < 0 || broken) return;
            float remaining = crumbleAt - Time.time;
            if (remaining <= 0f)
            {
                broken = true;
                if (solid != null) solid.enabled = false;
                if (graphic != null) graphic.enabled = false;
            }
            else if (graphic != null)
            {
                graphic.color = (Mathf.FloorToInt(remaining * 16f) & 1) == 0
                    ? Color.white : new Color(1f, .35f, .25f, 1f);
            }
        }
    }
}
