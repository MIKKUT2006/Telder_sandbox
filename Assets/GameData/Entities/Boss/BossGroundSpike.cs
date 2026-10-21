using Game.Visuals.PixelEffects;
using Game.World;
using UnityEngine;

namespace Game.Entities.Boss
{
    // A visible warning precedes the damaging stone pillar; owned by its boss.
    public sealed class BossGroundSpike : MonoBehaviour
    {
        private EntityActor owner;
        private Game.PlayerStats.PlayerStats target;
        private PixelEffectMesh draw;
        private Vector2 ground;
        private float born, warning, damage;
        private bool hit;
        public static void Spawn(EntityActor owner, Vector2 around, float damage, float warning)
        {
            var manager = WorldManager.Instance;
            if (manager == null || manager.GetWorldCollision() == null || owner == null) return;
            int x = Mathf.FloorToInt(around.x), start = Mathf.FloorToInt(around.y);
            bool found = false;
            for (int y = start + 2; y >= start - 7; y--)
            {
                if (!manager.GetWorld().IsLoaded(x,y)) continue;
                if (manager.GetWorldCollision().IsSolid(x,y-1) && !manager.GetWorldCollision().IsSolid(x,y))
                { around.y = y; found = true; break; }
            }
            if (!found) return;
            var spike = new GameObject("Golem Ground Eruption").AddComponent<BossGroundSpike>();
            spike.owner = owner; spike.target = owner.BossTarget; spike.ground = around;
            spike.damage = damage; spike.warning = Mathf.Max(.5f,warning); spike.born = Time.time;
            spike.draw = new PixelEffectMesh("Golem Eruption Pixels", null, 189);
        }
        private void Update()
        {
            if (owner == null || owner.IsDead || !owner.isActiveAndEnabled || draw == null)
            { Destroy(gameObject); return; }
            float age = Time.time - born;
            if (age > warning + .55f) { Destroy(gameObject); return; }
            draw.Begin();
            float x = Mathf.Floor(ground.x * 16) / 16;
            if (age < warning)
            {
                byte a = (byte)(Mathf.FloorToInt(age * 10) % 2 == 0 ? 210 : 100);
                for (int i=-6;i<=6;i++)
                    draw.Quad(x+i/16f,ground.y,1f/16,1f/16,new Color32(246,145,255,a));
            }
            else
            {
                float height = Mathf.Min(1.9f, (age-warning)*18);
                float fade = Mathf.Clamp01((warning+.55f-age)/.22f);
                for (int row=0;row<Mathf.CeilToInt(height*16);row++)
                {
                    float width = Mathf.Max(1, 9-row*.23f)/16f;
                    byte shade = (byte)(120+(row%3)*25);
                    draw.Quad(x-width*.5f,ground.y+row/16f,width,1f/16,new Color32(shade,(byte)(shade*.85f),shade,(byte)(fade*255)));
                }
                if (!hit && target != null && !target.IsDead &&
                    Mathf.Abs(target.transform.position.x-ground.x)<.65f &&
                    target.transform.position.y+.8f>ground.y && target.transform.position.y-.8f<ground.y+height)
                { target.TakeDamage(damage); hit = true; }
            }
            draw.Upload();
        }
        private void OnDestroy() { if (draw != null) draw.Dispose(); }
    }
}
