using UnityEngine;
using Game.World;

namespace Game.World.Effects {
    [DisallowMultipleComponent]
    public sealed class PlayerFootstepDust : MonoBehaviour {
        [SerializeField] private float spawnInterval = .055f;
        [SerializeField] private float minSpeed = 1.1f;
        [SerializeField] private float particleSize = .16f;
        private ParticleSystem effect;
        private Material dustMaterial;
        private PlayerCollision collision;
        private Vector3 previous;
        private float timer;

        private void Awake(){
            collision = GetComponent<PlayerCollision>();
            previous = transform.position;
            Create();
        }

        private void Create() { /* PixelImpactParticles owns the pooled renderers. */ }

        private void LateUpdate()
        {
            Vector3 current = transform.position;
            float dx = current.x-previous.x;
            float speed = Mathf.Abs(dx)/Mathf.Max(Time.deltaTime,.001f);
            previous = current;
            if (collision == null) collision = GetComponent<PlayerCollision>();
            if (collision == null || speed < minSpeed || !collision.IsGrounded) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = spawnInterval;
            float feetY = current.y-collision.GetColliderSize().y*.5f+.04f;
            PixelImpactParticles.EmitFootstep(new Vector2(current.x,feetY),
                SampleTerrainDustColor(current.x,feetY),Mathf.Sign(dx));
        }

        private static Color SampleTerrainDustColor(float worldX, float feetY) {
            var manager = WorldManager.Instance;
            if (manager == null || manager.GetWorld() == null)
                return new Color(.74f,.49f,.37f,.92f);
            var world = manager.GetWorld();
            int x = Mathf.FloorToInt(worldX);
            int y = Mathf.FloorToInt(feetY - .12f);
            ushort tile = world.GetBlock(x,y);
            if (tile == 0) return new Color(.74f,.49f,.37f,.92f);
            var block = world.GetBlockDefinition(tile);
            string key = block != null && !string.IsNullOrWhiteSpace(block.Texture) ? block.Texture.ToLowerInvariant() : string.Empty;
            if (key.Contains("snow") || key.Contains("ice")) return new Color(.84f,.89f,.96f,.9f);
            if (key.Contains("sand")) return new Color(.82f,.72f,.48f,.9f);
            if (key.Contains("stone") || key.Contains("rock") || key.Contains("ore")) return new Color(.57f,.58f,.61f,.92f);
            if (key.Contains("grass") || key.Contains("moss")) return new Color(.46f,.57f,.32f,.88f);
            if (key.Contains("wood") || key.Contains("plank")) return new Color(.67f,.46f,.29f,.9f);
            return new Color(.74f,.49f,.37f,.92f);
        }

        private void OnDestroy(){ if (dustMaterial != null) Destroy(dustMaterial); }
    }
}
