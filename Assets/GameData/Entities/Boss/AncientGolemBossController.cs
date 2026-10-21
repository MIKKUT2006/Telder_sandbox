using System.Collections;
using UnityEngine;
using Game.Combat;
using Game.World;
using Game.World.Items;
using Game.World.Effects;
using Game.World.Projectiles;
using Game.Audio;

namespace Game.Entities.Boss
{
    /// <summary>
    /// JSON-driven boss attack scheduler. Ordinary mobs keep their old AI.
    /// The boss is spawned by EntitySpawnManager just like other entities.
    /// All action names can be changed in its JSON; missing Animator states are safe.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AncientGolemBossController : MonoBehaviour
    {
        private EntityActor actor;
        private Animator bossAnimator;
        private EntityBossDefinition config;
        private bool acting;
        private float recoveryUntil;
        private Vector2 previousTarget, targetVelocity;
        private float targetSampleTime;
        private bool phaseTwoAnnounced;
        private float nextSlam;
        private float nextDash;
        private float nextBomb;
        private float nextOrb;
        private bool slamAnimationCompleted;
        private bool missingSlamReported;
        private bool fallbackSlamReported;

        private void Start()
        {
            actor = GetComponent<EntityActor>();
            if (actor == null || !actor.IsBoss) { enabled = false; return; }
            config = actor.BossSettings;
            bossAnimator = GetComponentInChildren<Animator>(true);
            nextSlam = Time.time + 2.2f;
            nextDash = Time.time + 5.0f;
            nextBomb = Time.time + 5.5f;
            nextOrb = Time.time + 8.0f;
        }

        private void Update()
        {
            if (actor == null || actor.IsDead || config == null || !actor.IsBossTerrainReady) return;
            var target = actor.BossTarget;
            if (target == null || target.IsDead) return;
            if (Vector2.Distance(transform.position, target.transform.position) > 33f) return;
            Vector2 targetNow = target.transform.position;
            float sampleDt = Time.time - targetSampleTime;
            if (sampleDt > .015f)
            {
                Vector2 velocity = (targetNow - previousTarget) / sampleDt;
                targetVelocity = velocity.sqrMagnitude < 225 ? Vector2.Lerp(targetVelocity, velocity, .35f) : Vector2.zero;
                previousTarget = targetNow; targetSampleTime = Time.time;
            }
            if (acting || Time.time < recoveryUntil) return;
            bool phaseTwo = actor.Health <= actor.MaxHealth * config.PhaseTwoHealth;
            if (phaseTwo && !phaseTwoAnnounced)
            {
                phaseTwoAnnounced = true;
                StartCoroutine(PhaseTransition());
            }
            else if (Time.time >= nextSlam &&
                (Mathf.Abs(targetNow.x-transform.position.x) < config.ShockwaveRange || Time.time-nextSlam>1.8f))
            {
                nextSlam = Time.time + Mathf.Max(1.5f, config.SlamCooldown) * (phaseTwo ? .78f : 1f);
                StartCoroutine(Slam());
            }
            else if (phaseTwo && Time.time >= nextOrb)
            {
                nextOrb = Time.time + Mathf.Max(2f, config.OrbCooldown);
                StartCoroutine(ThrowOrb());
            }
            else if (Time.time >= nextBomb)
            {
                nextBomb = Time.time + Mathf.Max(2f, config.BombCooldown) * (phaseTwo ? .8f : 1f);
                StartCoroutine(ThrowBombs());
            }
            else if (phaseTwo && Time.time >= nextDash)
            {
                nextDash = Time.time + Mathf.Max(2f, config.DashCooldown);
                StartCoroutine(Dash());
            }
            else if (Time.time >= nextSlam)
            {
                nextSlam = Time.time + Mathf.Max(1.5f, config.SlamCooldown);
                StartCoroutine(Slam());
            }
        }

        private bool IsOrbPhaseActive()
        {
            // The orb belongs to phase two, exactly like the dash. A boss at
            // full health is not eligible even if its cooldown has elapsed.
            return actor != null && config != null &&
                actor.Health <= actor.MaxHealth * config.PhaseTwoHealth;
        }

        private bool Ready()
        {
            return actor != null && !actor.IsDead && actor.IsBossTerrainReady &&
                actor.BossTarget != null && !actor.BossTarget.IsDead;
        }
        private void Begin(string animation)
        {
            acting = true;
            actor.SetBossBusy(true);
            actor.PlayBossAnimation(animation);
        }
        private void End()
        {
            acting = false;
            recoveryUntil = Time.time + .3f;
            if (actor != null) actor.SetBossBusy(false);
        }

        // Bomb and orb attacks do not choose or restart an Animator state.
        // The boss is free to keep playing its normal Idle/Walk animation.
        private void BeginWithoutAnimation()
        {
            acting = true;
        }
        private void EndWithoutAnimation()
        {
            acting = false;
            recoveryUntil = Time.time + .25f;
        }

        // Resolve the actual Animator STATE, not the animation clip file name.
        // Older boss JSON sets Boss.AnimationSlam = "Slam" while its Visual
        // section correctly names the real state "Ancient_golem_slam".
        private bool TryFindSlamState(string candidate, out string resolved)
        {
            resolved = null;
            if (bossAnimator == null || !bossAnimator.isActiveAndEnabled ||
                bossAnimator.runtimeAnimatorController == null ||
                string.IsNullOrWhiteSpace(candidate))
                return false;

            string trimmed = candidate.Trim();
            if (bossAnimator.HasState(0, Animator.StringToHash(trimmed)))
            {
                resolved = trimmed;
                return true;
            }

            // Unity state hashes may use a full path including the layer.
            if (!trimmed.StartsWith("Base Layer.", System.StringComparison.Ordinal))
            {
                string fullPath = "Base Layer." + trimmed;
                if (bossAnimator.HasState(0, Animator.StringToHash(fullPath)))
                {
                    resolved = fullPath;
                    return true;
                }
            }
            return false;
        }

        private string ResolveSlamState()
        {
            string stateName;
            if (TryFindSlamState(config.AnimationSlam, out stateName))
                return stateName;

            // Visual.AnimatorAttackState takes precedence over an invisible
            // fallback windup when Boss.AnimationSlam does not exist.
            if (TryFindSlamState(actor.BossVisualAttackState, out stateName))
            {
                if (!fallbackSlamReported)
                {
                    fallbackSlamReported = true;
                    Debug.LogWarning("ANCIENT GOLEM: Boss.AnimationSlam='" +
                        config.AnimationSlam + "' is not an Animator state. " +
                        "Using Visual.AnimatorAttackState='" + stateName +
                        "'. Make both values equal in the JSON.", this);
                }
                return stateName;
            }

            if (!missingSlamReported)
            {
                missingSlamReported = true;
                Debug.LogError("ANCIENT GOLEM: Slam animation not found. Boss.AnimationSlam='" +
                    config.AnimationSlam + "', Visual.AnimatorAttackState='" +
                    actor.BossVisualAttackState + "'. " +
                    "Check the Animator on the ancient_golem visual prefab; " +
                    "the JSON must contain its STATE name (or Base Layer.StateName). " +
                    "Shockwaves are disabled until the state exists.", this);
            }
            return null;
        }

        // The shockwaves are emitted only when this actual Animator playback
        // reaches its end. Timeouts do not silently turn into invisible attacks.
        private IEnumerator WaitForSlamAnimation(string stateName)
        {
            slamAnimationCompleted = false;
            if (bossAnimator == null || !bossAnimator.isActiveAndEnabled ||
                bossAnimator.runtimeAnimatorController == null)
                yield break;

            int fullHash = Animator.StringToHash(stateName);
            int dotIndex = stateName.LastIndexOf('.');
            string shortName = dotIndex >= 0 ? stateName.Substring(dotIndex + 1) : stateName;
            int shortHash = Animator.StringToHash(shortName);

            bool started = false;
            float expectedEnd = Time.time;
            // Animator.Play may not show up in StateInfo until its next update.
            for (int frame = 0; frame < 8 && Ready(); frame++)
            {
                AnimatorStateInfo current = bossAnimator.GetCurrentAnimatorStateInfo(0);
                if (current.shortNameHash == shortHash || current.fullPathHash == fullHash)
                {
                    started = true;
                    expectedEnd = Time.time + RemainingSlamSeconds(current);
                    break;
                }
                yield return null;
            }
            if (!started)
            {
                Debug.LogWarning("ANCIENT GOLEM: Animator did not enter Slam state '" +
                    stateName + "'. Check layer 0 and Animator transitions.", this);
                yield break;
            }

            float deadline = Time.time + 10f;
            while (Ready() && Time.time < deadline)
            {
                AnimatorStateInfo current = bossAnimator.GetCurrentAnimatorStateInfo(0);
                bool inSlam = current.shortNameHash == shortHash ||
                    current.fullPathHash == fullHash;
                if (inSlam)
                {
                    if (current.normalizedTime >= 1f)
                    {
                        slamAnimationCompleted = true;
                        yield break;
                    }
                    expectedEnd = Time.time + RemainingSlamSeconds(current);
                }
                else if (Time.time >= expectedEnd)
                {
                    // An exit transition may begin a little before normalizedTime=1.
                    // The timing still honors the complete authored clip length.
                    slamAnimationCompleted = true;
                    yield break;
                }
                yield return null;
            }
        }

        private float RemainingSlamSeconds(AnimatorStateInfo state)
        {
            float speed = Mathf.Max(0.01f, Mathf.Abs(bossAnimator.speed *
                state.speed * state.speedMultiplier));
            return Mathf.Max(0f, 1f - state.normalizedTime) *
                Mathf.Max(0.01f, state.length) / speed;
        }
        private IEnumerator PhaseTransition()
        {
            Begin(config.AnimationPhaseTwo);
            yield return new WaitForSeconds(.85f);
            End();
        }
        private IEnumerator Slam()
        {
            string slamState = ResolveSlamState();
            if (string.IsNullOrEmpty(slamState))
                yield break;

            Begin(slamState);
            // Windup starts together with the Slam animation.
            TelderAudioSystem.PlayAt("boss.ancient_golem.slam.windup",
                transform.position, GetInstanceID());
            yield return WaitForSlamAnimation(slamState);
            if (Ready() && slamAnimationCompleted)
            {
                Vector2 feet = (Vector2)transform.position - Vector2.up * Mathf.Max(.2f,
                    actor.GetDamageBounds().height * .5f);
                BossShockwave.Spawn(feet, -1f, config.ShockwaveRange,
                    config.ShockwaveSpeed, config.ShockwaveDamage, config.ShockwaveTexture);
                BossShockwave.Spawn(feet, 1f, config.ShockwaveRange,
                    config.ShockwaveSpeed, config.ShockwaveDamage, config.ShockwaveTexture);
                var target = actor.BossTarget;
                if (target != null && Mathf.Abs(target.transform.position.x - feet.x) < 2.4f &&
                    target.transform.position.y - .8f < feet.y + 1f)
                    target.TakeDamage(config.ShockwaveDamage);
                if (IsOrbPhaseActive() && target != null)
                {
                    Vector2 prediction = (Vector2)target.transform.position + targetVelocity * .35f;
                    for (int i=-1;i<=1;i++)
                        BossGroundSpike.Spawn(actor,prediction+Vector2.right*(i*1.5f),config.ShockwaveDamage,.8f);
                }
                // Ground shards appear at the SAME impact frame as the waves,
                // after the entire Slam animation has finished.
                BossSlamGroundParticles.Emit(feet, config);
                // Impact is sounded at the EXACT frame that the waves/shards spawn.
                TelderAudioSystem.PlayAt("boss.ancient_golem.slam.impact",
                    feet, GetInstanceID());
                // The wave objects draw their own pixel impact front.
            }
            if (Ready() && slamAnimationCompleted && IsOrbPhaseActive())
            {
                yield return new WaitForSeconds(.62f);
                if (Ready())
                {
                    Vector2 feet = (Vector2)transform.position - Vector2.up * actor.GetDamageBounds().height * .5f;
                    BossShockwave.Spawn(feet,-1,config.ShockwaveRange,config.ShockwaveSpeed*1.15f,config.ShockwaveDamage,config.ShockwaveTexture);
                    BossShockwave.Spawn(feet,1,config.ShockwaveRange,config.ShockwaveSpeed*1.15f,config.ShockwaveDamage,config.ShockwaveTexture);
                    BossSlamGroundParticles.Emit(feet,config);
                }
            }
            yield return new WaitForSeconds(.25f);
            End();
        }
        private IEnumerator ThrowBombs()
        {
            BeginWithoutAnimation();
            yield return new WaitForSeconds(.42f);
            if (Ready())
            {
                var weapon = new WeaponMetadata
                {
                    Kind = "Bomb", Damage = Mathf.Max(0f, config.BombDamage),
                    BombFuseTime = 1.6f, BombGravity = 16f, BombBounce = .28f,
                    BombExplosionWidth = 2, BombExplosionHeight = 2,
                    // Match regular game:bomb behaviour: destroy foreground
                    // and furniture, drop block loot and emit ExplosionPixelVfx.
                    // BombProjectile.ExplodeNow delegates to ExplosionSystem.ExplodeBox,
                    // which already emits one explosion burst + block debris.
                    BombDestroyBlocks = true, BombDestroyBackground = false,
                    BombDestroyFurniture = true, BombKnockback = 4f,
                    ProjectileItem = "game:bomb", ProjectileWorldSize = .55f,
                    ProjectileSortingLayer = "Default", ProjectileOrder = 140
                };
                int count = Random.Range(config.BombMinCount, config.BombMaxCount + 1);
                Vector2 origin = (Vector2)transform.position + Vector2.up * .7f;
                for (int i = 0; i < count; i++)
                {
                    float spread = count == 1 ? 0 : Mathf.Lerp(-3.5f, 3.5f, i / (float)(count - 1));
                    float flight = Mathf.Lerp(.8f, 1.25f, i / (float)Mathf.Max(1,count-1));
                    Vector2 destination = (Vector2)actor.BossTarget.transform.position + targetVelocity * flight * .65f + Vector2.right * spread;
                    Vector2 delta = destination - origin;
                    Vector2 velocity = new Vector2(Mathf.Clamp(delta.x / flight,-17,17),
                        Mathf.Clamp(delta.y / flight + .5f * weapon.BombGravity * flight,4,13));
                    BombProjectile.Spawn(origin + Vector2.up * .35f, velocity,
                        weapon, transform);
                }
            }
            yield return new WaitForSeconds(.4f);
            EndWithoutAnimation();
        }
        private IEnumerator ThrowOrb()
        {
            BeginWithoutAnimation();
            yield return new WaitForSeconds(.58f);
            // Check health AGAIN after the cast delay. A healed boss must not
            // release a void orb while in phase one.
            if (Ready() && IsOrbPhaseActive())
            {
                Vector2 direction = (Vector2)actor.BossTarget.transform.position - (Vector2)transform.position;
                if (direction.sqrMagnitude < .001f) direction = Vector2.right;
                direction.Normalize();
                BossVoidOrb.Spawn((Vector2)transform.position + direction * 1.1f + Vector2.up * .55f,
                    direction * Mathf.Max(1f, config.OrbSpeed),
                    Mathf.Max(.2f, config.OrbRadius), config.OrbHealthFraction,
                    actor.transform);
                for (int side=-1;side<=1;side+=2)
                {
                    Vector2 angled = (Vector2)(Quaternion.Euler(0,0,side*18)*direction);
                    BossVoidOrb.Spawn((Vector2)transform.position+angled*1.1f+Vector2.up*.55f,
                        angled*Mathf.Max(1,config.OrbSpeed)*.85f,Mathf.Max(.2f,config.OrbRadius)*.75f,
                        config.OrbHealthFraction*.65f,actor.transform);
                }
            }
            yield return new WaitForSeconds(.25f);
            EndWithoutAnimation();
        }
        private IEnumerator Dash()
        {
            Begin(config.AnimationDash);
            // Warning plays at the start of the dash windup, not after movement.
            TelderAudioSystem.PlayAt("boss.ancient_golem.dash.warning",
                transform.position, GetInstanceID());
            yield return new WaitForSeconds(Mathf.Max(.06f, config.DashWindup));
            if (!Ready()) { End(); yield break; }
            float sign = Mathf.Sign(actor.BossTarget.transform.position.x - transform.position.x);
            if (sign == 0) sign = 1;
            float travel = 0;
            bool didHit = false;
            var manager = WorldManager.Instance;
            if (manager == null || manager.GetWorld() == null) { End(); yield break; }
            float width = actor.GetDamageBounds().width;
            float height = actor.GetDamageBounds().height;
            while (Ready() && travel < Mathf.Max(1f, config.DashDistance))
            {
                float step = Mathf.Min(Mathf.Max(3f, config.DashSpeed) * Time.fixedDeltaTime,
                    config.DashDistance - travel);
                Vector2 current = actor.transform.position;
                Vector2 next = current + Vector2.right * (step * sign);
                int frontX = Mathf.FloorToInt(next.x + sign * (width * .5f + .1f));
                int chunkSize = Mathf.Max(1, manager.GetSettings().ChunkSize);
                int chunkX = Mathf.FloorToInt(frontX / (float)chunkSize);
                int chunkY = Mathf.FloorToInt(next.y / chunkSize);
                if (manager.GetLoader() == null || !manager.GetLoader().IsChunkLoaded(chunkX, chunkY))
                    break;
                int bottom = Mathf.FloorToInt(next.y - height * .5f + .12f);
                int top = Mathf.FloorToInt(next.y + height * .5f - .12f);
                for (int y = bottom; y <= top; y++)
                {
                    ushort blockId = manager.GetWorld().GetBlock(frontX, y);
                    if (blockId == 0) continue;
                    var definition = manager.GetWorld().GetBlockDefinition(blockId);
                    if (definition == null || definition.Hardness < 0f || definition.Hardness > 100f) continue;
                    if (manager.SetBlock(frontX, y, 0))
                    {
                        BlockLootDropper.SpawnDrops(definition, string.Empty,
                            new Vector2(frontX + .5f, y + .5f));
                    }
                }
                if (manager.GetWorldCollision().OverlapsAny(new Rect(next.x-width*.45f,next.y-height*.45f,width*.9f,height*.9f)))
                    break;
                actor.MoveBossDirect(next);
                travel += step;
                var target = actor.BossTarget;
                if (!didHit && target != null && !target.IsDead &&
                    Mathf.Abs(target.transform.position.x - next.x) < width * .5f + .35f &&
                    Mathf.Abs(target.transform.position.y - next.y) < height * .5f + .5f)
                {
                    target.TakeDamage(Mathf.Max(0f, config.DashDamage));
                    didHit = true;
                }
                yield return new WaitForFixedUpdate();
            }
            End();
        }
        private void OnDisable()
        {
            StopAllCoroutines();
            if (actor != null) actor.SetBossBusy(false);
            acting = false;
        }
    }
}
