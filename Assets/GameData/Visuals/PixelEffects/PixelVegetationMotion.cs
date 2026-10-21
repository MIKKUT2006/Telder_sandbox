using System.Collections.Generic;
using Game.Entities;
using UnityEngine;

namespace Game.Visuals.PixelEffects
{
    // One small global contact buffer drives every grass/vine material.
    // Positions are retained for a damped recovery after the actor has passed.
    public sealed class PixelVegetationMotion
    {
        private const int Capacity = 24;
        private readonly Vector4[] contacts = new Vector4[Capacity];
        private readonly Vector4[] uniforms = new Vector4[Capacity];
        private readonly Dictionary<int, Vector2> previous = new Dictionary<int, Vector2>(128);
        private PlayerController player;
        private float nextPlayerSearch, nextSample;
        private int cursor;

        public void Clear()
        {
            previous.Clear(); cursor = 0; player = null; nextPlayerSearch = nextSample = 0f;
            for (int i = 0; i < Capacity; i++) { contacts[i] = new Vector4(0, 0, 0, -10000); uniforms[i] = new Vector4(); }
            Shader.SetGlobalVectorArray("_PlantContacts", uniforms);
        }

        public void Tick(Camera camera)
        {
            if (Time.time < nextSample) return;
            nextSample = Time.time + .06f;
            if (player == null && Time.unscaledTime >= nextPlayerSearch)
            { player = Object.FindObjectOfType<PlayerController>(); nextPlayerSearch = Time.unscaledTime + .5f; }
            var actors = EntityActor.ActiveActors;
            Vector2 center = camera.transform.position;
            float radius = camera.orthographicSize * (camera.aspect + 1f) + 5f;
            for (int i = 0, count = 0; i < actors.Count && count < 10; i++)
            {
                EntityActor actor = actors[i];
                if (actor == null || !actor.IsDamageTargetActive ||
                    ((Vector2)actor.transform.position - center).sqrMagnitude > radius * radius) continue;
                Sample(actor.transform, actor.GetDamageBounds().height); count++;
            }
            if (player != null && player.isActiveAndEnabled)
                Sample(player.transform, player.GetPlayerCollision() != null ?
                    player.GetPlayerCollision().GetColliderSize().y : 1.8f);
            // A changing mob population must not grow this cache indefinitely.
            if (previous.Count > 256) previous.Clear();
            for (int i = 0; i < Capacity; i++)
            {
                Vector4 contact = contacts[i];
                float age = Time.time - contact.w;
                contact.z *= age < 0 || age > 1.5f ? 0 : Mathf.Exp(-age * 4.8f) * Mathf.Cos(age * 10f);
                uniforms[i] = contact;
            }
            Shader.SetGlobalVectorArray("_PlantContacts", uniforms);
        }

        private void Sample(Transform mover, float height)
        {
            int id = mover.GetInstanceID();
            Vector2 p = mover.position;
            bool known = previous.TryGetValue(id, out Vector2 old);
            previous[id] = p;
            if (!known) return;
            Vector2 delta = p - old;
            if (delta.sqrMagnitude < .0004f || delta.sqrMagnitude > 16f) return;
            float strength = Mathf.Clamp(delta.x * 4f, -1.5f, 1.5f);
            if (Mathf.Abs(strength) < .12f) strength = Mathf.Sign(delta.y) * .35f;
            Vector2 feet = new Vector2(p.x, p.y - height * .5f + .25f);
            Add(feet, strength);
            if (height > 1f) Add(p, strength * .8f);
        }

        private void Add(Vector2 p, float strength)
        {
            contacts[cursor] = new Vector4(p.x, p.y, strength, Time.time);
            cursor = (cursor + 1) % Capacity;
        }
    }
}
