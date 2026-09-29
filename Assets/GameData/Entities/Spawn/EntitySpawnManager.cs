using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Entities.AI;
using Game.World;
using Game.World.Biomes.Caves;
using Game.World.Lighting;

namespace Game.Entities.Spawn
{
    [DisallowMultipleComponent]
    public sealed class EntitySpawnManager : MonoBehaviour
    {
        private readonly List<EntityActor> alive = new List<EntityActor>(64);
        private Game.PlayerStats.PlayerStats playerStats;
        private Transform player;
        private WorldManager worldManager;
        private float nextSpawnTime;
        private float nextDiagnosticTime;

        private void Start()
        {
            // Always reload from disk when a world starts. Unity can keep static
            // fields between Play sessions when Domain Reload is disabled, which
            // otherwise leaves old Enabled/Spawn values cached in EntityRegistry.
            EntityRegistry.Reload();

            worldManager = WorldManager.Instance;
            ResolvePlayer();
            nextSpawnTime = Time.time + 1f;
            nextDiagnosticTime = Time.time + 4f;

            int total = 0;
            int enabled = 0;
            foreach (EntityDefinition definition in EntityRegistry.All)
            {
                total++;
                if (definition != null && definition.Enabled &&
                    definition.Spawn != null && definition.Spawn.Enabled)
                {
                    enabled++;
                }
            }

            Debug.Log(
                "ENTITIES SPAWN: manager started. Definitions=" + total +
                ", enabledForSpawn=" + enabled + ".");
        }

        private void Update()
        {
            if (worldManager == null)
                worldManager = WorldManager.Instance;

            if (player == null || playerStats == null)
                ResolvePlayer();

            CleanupAndDespawnFar();

            // Do not globally block all spawning on InitialLightingReady.
            // Definitions that accept every light level (0..1) can spawn safely
            // before lighting finishes. Light-restricted definitions are filtered
            // later inside PassesEnvironment until lighting is ready.
            if (worldManager == null || !worldManager.IsReady || player == null)
            {
                LogDiagnostic("waiting: worldManager=" + (worldManager != null) +
                    ", worldReady=" + (worldManager != null && worldManager.IsReady) +
                    ", player=" + (player != null));
                return;
            }

            if (Time.time < nextSpawnTime)
                return;

            EntitySpawnSettingsData settings = EntitySpawnSettings.Data;
            nextSpawnTime = Time.time + settings.SpawnInterval;

            if (alive.Count >= settings.GlobalMaxAlive)
                return;

            for (int i = 0; i < settings.AttemptsPerTick && alive.Count < settings.GlobalMaxAlive; i++)
            {
                EntityDefinition definition = PickDefinition();
                if (definition == null)
                {
                    LogDiagnostic("no enabled entity definitions are eligible for spawning");
                    return;
                }

                Vector2 spawnPosition;
                if (!TryFindSpawnPosition(definition, out spawnPosition))
                    continue;

                Spawn(definition, spawnPosition);
            }

            if (alive.Count == 0)
            {
                LogDiagnostic(
                    "spawn attempts found no valid position. lightingReady=" +
                    worldManager.InitialLightingReady +
                    ", playerPos=" + player.position);
            }
        }

        private void LogDiagnostic(string message)
        {
            if (Time.time < nextDiagnosticTime)
                return;

            nextDiagnosticTime = Time.time + 5f;
            Debug.LogWarning("ENTITIES SPAWN: " + message);
        }

        private void ResolvePlayer()
        {
            playerStats = UnityEngine.Object.FindObjectOfType<Game.PlayerStats.PlayerStats>();
            player = playerStats != null ? playerStats.transform : null;
        }

        private EntityDefinition PickDefinition()
        {
            List<EntityDefinition> candidates = new List<EntityDefinition>();
            float totalWeight = 0f;

            foreach (EntityDefinition definition in EntityRegistry.All)
            {
                if (definition == null || !definition.Enabled || definition.Spawn == null || !definition.Spawn.Enabled)
                    continue;

                if (definition.Spawn.MaxAlive <= 0)
                    continue;

                int current = CountAlive(definition.ID);
                if (current >= definition.Spawn.MaxAlive)
                    continue;

                candidates.Add(definition);
                totalWeight += Mathf.Max(0.01f, definition.Spawn.Weight);
            }

            if (candidates.Count == 0)
                return null;

            float roll = UnityEngine.Random.value * totalWeight;
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= Mathf.Max(0.01f, candidates[i].Spawn.Weight);
                if (roll <= 0f)
                    return candidates[i];
            }

            return candidates[candidates.Count - 1];
        }

        private int CountAlive(string id)
        {
            int count = 0;
            for (int i = 0; i < alive.Count; i++)
            {
                EntityActor actor = alive[i];
                if (actor != null && !actor.IsDead && string.Equals(actor.DefinitionId, id, StringComparison.OrdinalIgnoreCase))
                    count++;
            }
            return count;
        }

        private bool TryFindSpawnPosition(EntityDefinition definition, out Vector2 position)
        {
            position = Vector2.zero;
            if (player == null || worldManager == null || definition == null || definition.Spawn == null)
                return false;

            EntityMovementKind kind = definition.Movement.GetKind();
            return kind == EntityMovementKind.Flying
                ? TryFindFlyingSpawnPosition(definition, out position)
                : TryFindGroundSpawnPosition(definition, out position);
        }

        private bool TryFindGroundSpawnPosition(EntityDefinition definition, out Vector2 position)
        {
            position = Vector2.zero;

            float minDistance = Mathf.Max(
                EntitySpawnSettings.Data.SpawnSafetyRadius,
                definition.Spawn.MinDistanceFromPlayer);
            float maxDistance = Mathf.Max(minDistance + 1f, definition.Spawn.MaxDistanceFromPlayer);

            // Ground creatures should be selected primarily by horizontal distance.
            // The old code selected a random point on a 2D circle and rejected it
            // before looking for the ground. On surface worlds that random Y is often
            // an unloaded air/underground chunk, so valid ground spawns were discarded.
            int side = UnityEngine.Random.value < 0.5f ? -1 : 1;
            float horizontalDistance = UnityEngine.Random.Range(minDistance, maxDistance);
            int x = Mathf.FloorToInt(player.position.x + side * horizontalDistance);

            // Search around the player's current feet level. This is much more reliable
            // for surface terrain and still allows nearby cave ledges to be discovered.
            int playerFeetY = Mathf.FloorToInt(player.position.y - 0.05f);
            int configured = Mathf.Max(1, EntitySpawnSettings.Data.VerticalSearchCells);
            int vertical = Mathf.Max(configured, 40);

            EntityPathfinder probe = new EntityPathfinder(
                worldManager.GetWorldCollision(),
                definition);

            // First try the generated surface explicitly. This avoids missing the
            // terrain when the local height differs a lot from the player's Y
            // (mountains/valleys) and makes normal surface creatures deterministic.
            if (worldManager.GetGenerator() != null)
            {
                int surfaceFeetY = worldManager.GetGenerator().GetSurfaceHeight(x) + 1;
                if (TryUseGroundCell(definition, probe, x, surfaceFeetY, out position))
                    return true;
            }

            // Then search nearby standable cells too, so ground creatures may also
            // spawn on cave ledges and structures when their environment allows it.

            // Randomize whether we inspect lower or upper cells first so cave/surface
            // candidates do not always get the same priority.
            bool lowerFirst = UnityEngine.Random.value < 0.5f;

            for (int offset = 0; offset <= vertical; offset++)
            {
                int a = playerFeetY + (lowerFirst ? -offset : offset);
                int b = playerFeetY + (lowerFirst ? offset : -offset);

                if (TryUseGroundCell(definition, probe, x, a, out position))
                    return true;

                if (offset != 0 && b != a && TryUseGroundCell(definition, probe, x, b, out position))
                    return true;
            }

            return false;
        }

        private bool TryUseGroundCell(
            EntityDefinition definition,
            EntityPathfinder probe,
            int x,
            int feetY,
            out Vector2 position)
        {
            position = Vector2.zero;

            if (!IsLoadedCell(x, feetY))
                return false;

            if (!probe.CanStandAt(x, feetY))
                return false;

            Vector2 p = new Vector2(
                x + 0.5f,
                feetY + definition.ColliderHeight * 0.5f);

            if (!PassesEnvironment(definition, x, feetY, p))
                return false;

            position = p;
            return true;
        }

        private bool TryFindFlyingSpawnPosition(EntityDefinition definition, out Vector2 position)
        {
            position = Vector2.zero;

            float minDistance = Mathf.Max(
                EntitySpawnSettings.Data.SpawnSafetyRadius,
                definition.Spawn.MinDistanceFromPlayer);
            float maxDistance = Mathf.Max(minDistance + 1f, definition.Spawn.MaxDistanceFromPlayer);

            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float distance = UnityEngine.Random.Range(minDistance, maxDistance);
            Vector2 raw = (Vector2)player.position +
                          new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            int x = Mathf.FloorToInt(raw.x);
            int y = Mathf.FloorToInt(raw.y);
            int search = Mathf.Max(1, EntitySpawnSettings.Data.VerticalSearchCells);

            EntityPathfinder probe = new EntityPathfinder(
                worldManager.GetWorldCollision(),
                definition);

            for (int offset = 0; offset <= search; offset++)
            {
                int a = y + offset;
                int b = y - offset;

                if (TryUseFlyingCell(definition, probe, x, a, out position))
                    return true;

                if (offset != 0 && b != a && TryUseFlyingCell(definition, probe, x, b, out position))
                    return true;
            }

            return false;
        }

        private bool TryUseFlyingCell(
            EntityDefinition definition,
            EntityPathfinder probe,
            int x,
            int y,
            out Vector2 position)
        {
            position = Vector2.zero;

            if (!IsLoadedCell(x, y) || !probe.IsFlyingCellFree(x, y))
                return false;

            Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
            if (!PassesEnvironment(definition, x, y, p))
                return false;

            position = p;
            return true;
        }

        private bool PassesEnvironment(EntityDefinition definition, int x, int y, Vector2 position)
        {
            float distance = Vector2.Distance(player.position, position);
            if (distance < Mathf.Max(EntitySpawnSettings.Data.SpawnSafetyRadius, definition.Spawn.MinDistanceFromPlayer) ||
                distance > definition.Spawn.MaxDistanceFromPlayer + 1f)
            {
                return false;
            }

            bool acceptsAllLight =
                definition.Spawn.MinLight <= 0.001f &&
                definition.Spawn.MaxLight >= 0.999f;

            if (worldManager.InitialLightingReady)
            {
                float light = GetLight01(
                    x,
                    y + Mathf.Max(0, Mathf.FloorToInt(definition.ColliderHeight * 0.5f)));

                if (light < definition.Spawn.MinLight - 0.001f ||
                    light > definition.Spawn.MaxLight + 0.001f)
                {
                    return false;
                }
            }
            else if (!acceptsAllLight)
            {
                // Light-sensitive creatures wait for valid lighting, but a creature
                // configured for 0..1 must not be prevented from spawning at all.
                return false;
            }

            if (!PassesBiome(definition, x, y))
                return false;

            // Explicit JSON free-space requirement. This can be larger than
            // the actual collider, e.g. for a creature that needs room to spawn.
            float requiredWidth = Mathf.Max(definition.ColliderWidth, definition.Spawn.RequiredWidth);
            float requiredHeight = Mathf.Max(definition.ColliderHeight, definition.Spawn.RequiredHeight);
            float feetY = position.y - definition.ColliderHeight * 0.5f;
            Rect requiredBounds = new Rect(
                position.x - requiredWidth * 0.5f,
                feetY + 0.03f,
                requiredWidth,
                Mathf.Max(0.1f, requiredHeight - 0.06f));

            if (worldManager.GetWorldCollision().OverlapsAny(requiredBounds))
                return false;

            // Never spawn directly inside another creature/player.
            Collider2D[] overlaps = Physics2D.OverlapBoxAll(
                position,
                new Vector2(definition.ColliderWidth * 0.9f, definition.ColliderHeight * 0.9f),
                0f,
                Physics2D.DefaultRaycastLayers);

            for (int i = 0; i < overlaps.Length; i++)
            {
                if (overlaps[i] == null)
                    continue;
                if (overlaps[i].GetComponentInParent<EntityActor>() != null ||
                    overlaps[i].GetComponentInParent<Game.PlayerStats.PlayerStats>() != null)
                    return false;
            }

            return true;
        }

        private bool PassesBiome(EntityDefinition definition, int x, int y)
        {
            string[] allowed = definition.Spawn.Biomes;
            if (allowed == null || allowed.Length == 0)
                return true;

            string surfaceId = null;
            if (worldManager.GetGenerator() != null)
            {
                var biome = worldManager.GetGenerator().GetDominantBiome(x);
                if (biome != null)
                    surfaceId = biome.ID;
            }

            string caveId = null;
            try
            {
                caveId = CaveBiomeRegistry.GetBiomeIdAt(x, y, worldManager.GetSettings().Seed);
            }
            catch
            {
            }

            for (int i = 0; i < allowed.Length; i++)
            {
                string value = allowed[i];
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                if (value == "*")
                    return true;
                if (string.Equals(value, surfaceId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, caveId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private float GetLight01(int x, int y)
        {
            Game.World.World world = worldManager.GetWorld();
            if (world == null)
                return 1f;

            LightNode light = world.GetLight(x, y);
            int strongest = Math.Max(Math.Max((int)light.Sun, (int)light.R), Math.Max((int)light.G, (int)light.B));
            return strongest / 15f;
        }

        private bool IsLoadedCell(int worldX, int worldY)
        {
            if (worldManager.GetLoader() == null || worldManager.GetSettings() == null)
                return false;

            int size = Mathf.Max(1, worldManager.GetSettings().ChunkSize);
            int chunkX = FloorDiv(worldX, size);
            int chunkY = FloorDiv(worldY, size);
            return worldManager.GetLoader().IsChunkLoaded(chunkX, chunkY);
        }

        private void Spawn(EntityDefinition definition, Vector2 position)
        {
            GameObject go = new GameObject("Entity_" + definition.ID);
            go.transform.position = position;
            EntityActor actor = go.AddComponent<EntityActor>();
            int stars = definition.Stars.RollStars();
            actor.Initialize(definition, stars, playerStats);
            alive.Add(actor);
        }

        private void CleanupAndDespawnFar()
        {
            float maxDistance = EntitySpawnSettings.Data.DespawnDistance;
            float maxDistanceSqr = maxDistance * maxDistance;

            for (int i = alive.Count - 1; i >= 0; i--)
            {
                EntityActor actor = alive[i];
                if (actor == null)
                {
                    alive.RemoveAt(i);
                    continue;
                }

                if (player != null && !actor.IsDead && ((Vector2)actor.transform.position - (Vector2)player.position).sqrMagnitude > maxDistanceSqr)
                {
                    Destroy(actor.gameObject);
                    alive.RemoveAt(i);
                }
            }
        }

        private static int FloorDiv(int value, int divisor)
        {
            int result = value / divisor;
            int remainder = value % divisor;
            if (remainder != 0 && ((remainder < 0) != (divisor < 0)))
                result--;
            return result;
        }
    }
}
