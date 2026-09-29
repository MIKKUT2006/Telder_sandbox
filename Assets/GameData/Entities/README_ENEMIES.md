# Enemy / Creature system

Definitions live in `Assets/GameData/Entities/Definitions/*.json`.
The four `example_*.json` definitions are intentionally disabled (`"Enabled": false`) so the game does not start spawning placeholder-colored creatures before real art is assigned. Set one to `true` to test immediately.

## Behaviour

`Behavior`:
- `Aggressive` - attacks after seeing the player.
- `Neutral` - wanders until the player damages it, then retaliates for `AggroAfterHitSeconds`.
- `Passive` - never attacks; after taking damage it flees for a short time.

`Movement.Type`:
- `Ground` - local A* over walk/step/jump/drop links.
- `Flying` - local 8-direction A* through free cells.
- `Jumping` - ground pathfinder, movement performed as repeated hops.

Pathfinding is local and rate-limited; it does not search the infinite world every frame.

## Stars (1..5)

Stars are rolled on spawn from `MinStars`, `MaxStars`, and `StarWeights`.
Set `MinStars` equal to `MaxStars` for a fixed tier.

Default multipliers:
- HP: `1 + (Stars - 1) * HealthPerExtraStar` (default +35% per extra star)
- Damage: `1 + (Stars - 1) * DamagePerExtraStar` (default +20% per extra star)
- Loot amount: `1 + (Stars - 1) * LootPerExtraStar` (default +25% per extra star)

With defaults:
- 1 star: HP x1.00 / Damage x1.00 / Loot x1.00
- 2 stars: HP x1.35 / Damage x1.20 / Loot x1.25
- 3 stars: HP x1.70 / Damage x1.40 / Loot x1.50
- 4 stars: HP x2.05 / Damage x1.60 / Loot x1.75
- 5 stars: HP x2.40 / Damage x1.80 / Loot x2.00

Held-weapon damage is included in the star multiplier. Example: BaseDamage 8 + sword Damage 12 at 3 stars and DamagePerExtraStar=0.20 => (8+12)*1.40 = 28 damage.

## Spawn

Global settings: `Assets/GameData/Entities/entity_spawn_settings.json`.
Each entity additionally has:
- `MaxAlive` and `Weight`
- `MinLight` / `MaxLight` (0..1)
- min/max distance from player
- required free width/height
- optional biome IDs (`["*"]` for any)

The manager only accepts spawn cells in loaded chunks and despawns creatures that get too far from the player.

## Equipment

`Equipment` contains item IDs and chances. The first successful entry becomes the held item.
If the item has `Weapon` metadata, AI uses the same weapon definition as the player.

`Weapon.AIUseMode` can be:
- `Auto`
- `Melee`
- `Ranged`
- `Throwable`

This lets future weapons be equipped by enemies without creating an enemy-specific item type. Current Sword/Spear/Bow/Gun/Bomb kinds are handled automatically.

## Bomb

`Assets/GameData/Items/bomb.json` is a new `Bomb` item type and `WeaponKind.Bomb`.
It is consumed when the player throws it. Enemies can equip it as any other item.

Important fields:
- `BombFuseTime` (default/sample: 3 seconds)
- `BombExplosionWidth`, `BombExplosionHeight` (sample: exact 2x2 cells)
- `BombThrowSpeed`, `BombGravity`, `BombBounce`
- block/background/furniture destruction toggles
- `Damage` and `BombKnockback`

The bomb uses the existing world block-change/explosion pipeline, so lighting, chunk visuals and falling blocks are updated through the normal world APIs.

## Visuals and animations

A creature can use one of:
- `Visual.SpriteItem`: icon of an existing item/block
- `Visual.SpriteResource`: Unity Resources sprite path
- `Visual.SpriteFile`: path relative to `Assets/GameData`, including `.png`

Animation arrays (`IdleSprites`, `WalkSprites`, `AttackSprites`, `HurtSprites`, `DeathSprites`) accept Resources paths or `.png` file paths. If no art is assigned, a colored pixel placeholder from `Visual.Color` is used.

The star count is displayed above the creature in world space.


## Prefab / skeletal animation mode

Entities can now use a Unity prefab instead of frame-by-frame sprite arrays.
This is intended for Unity 2D Animation / Sprite Skin / bone rigs.

1. Put the prefab below any `Resources` folder. Recommended path:
   `Assets/GameData/Resources/Entities/Skeleton.prefab`
2. The prefab can contain bones, SpriteSkin components and an Animator.
3. In the entity JSON set:

```json
"Visual": {
  "PrefabResource": "Entities/Skeleton",
  "PrefabScale": 1.0,
  "PrefabFacesRight": true,
  "OffsetX": 0.0,
  "OffsetY": 0.0,

  "AnimatorIdleState": "Idle",
  "AnimatorWalkState": "Walk",
  "AnimatorAttackState": "Attack",
  "AnimatorHurtState": "Hurt",
  "AnimatorDeathState": "Death",
  "AnimatorCrossFade": 0.06,
  "DeathDespawnDelay": 0.9
}
```

The Animator states are driven by the existing entity AI. No enemy-specific C# script is required.

For a held weapon, create a child transform named `HeldItemAnchor` under the hand bone,
or specify its exact relative path with `HeldItemAnchorPath`. The runtime weapon sprite
will be parented there, so it follows the skeletal hand animation.

`OffsetY` is visual-only. Use it to line up art/prefab feet with the entity collider without
changing physics, spawning or pathfinding.

See `Definitions/example_skeletal_prefab_enemy.json`.
