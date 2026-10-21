using Game.Combat;
using Game.Items;
using Game.World.Lighting;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Entities.Visual
{
    public enum EntityVisualState
    {
        Idle,
        Walk,
        Attack,
        Hurt,
        Death
    }

    /// <summary>
    /// Presentation-only layer for an entity.
    /// Supports either classic frame sprites OR a prefab with Animator / 2D bones.
    /// AI, health, collision and combat never depend on the chosen visual mode.
    /// </summary>
    public sealed class EntityVisualController
    {
        private readonly EntityDefinition definition;
        private readonly Transform root;

        private SpriteRenderer bodyRenderer;
        private SpriteRenderer heldRenderer;
        private Transform heldTransform;
        private Transform visualTransform;
        private TextMesh starsText;
        private Animator animator;

        private bool prefabMode;
        private Vector3 baseVisualScale = Vector3.one;

        private readonly Dictionary<EntityVisualState, Sprite[]> animationCache =
            new Dictionary<EntityVisualState, Sprite[]>();

        private static readonly Dictionary<string, Sprite> FileSpriteCache =
            new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        private EntityVisualState state;
        private float stateStartTime;
        private Sprite fallbackSprite;
        private float facing = 1f;
        private float heldBaseRotation;


        public EntityVisualController(Transform root, EntityDefinition definition, int stars)
        {
            this.root = root;
            this.definition = definition;

            prefabMode = TryCreatePrefabVisual();
            if (!prefabMode)
                CreateSpriteVisual();

            CreateHeldItemVisual();
            CreateStars(stars);
            SetState(EntityVisualState.Idle, true);
        }

        // =====================================================
        // CREATION
        // =====================================================

        private bool TryCreatePrefabVisual()
        {
            if (definition == null || definition.Visual == null ||
                string.IsNullOrWhiteSpace(definition.Visual.PrefabResource))
            {
                return false;
            }

            GameObject prefab = UnityEngine.Resources.Load<GameObject>(definition.Visual.PrefabResource.Trim());
            if (prefab == null)
            {
                Debug.LogWarning(
                    "ENTITY VISUAL: prefab resource not found: " +
                    definition.Visual.PrefabResource +
                    ". Expected a prefab under a Resources folder, for example: " +
                    "Assets/GameData/Resources/" + definition.Visual.PrefabResource + ".prefab. " +
                    "Falling back to sprite mode.");
                return false;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab, root, false);
            instance.name = "VisualPrefab";
            visualTransform = instance.transform;
            visualTransform.localPosition = new Vector3(
                definition.Visual.OffsetX,
                definition.Visual.OffsetY,
                0f);

            float scale = Mathf.Max(0.01f, definition.Visual.PrefabScale);
            visualTransform.localScale = visualTransform.localScale * scale;
            baseVisualScale = visualTransform.localScale;

            animator = instance.GetComponentInChildren<Animator>(true);
            return true;
        }

        private void CreateSpriteVisual()
        {
            GameObject body = new GameObject("Visual");
            body.transform.SetParent(root, false);
            body.transform.localPosition = new Vector3(
                definition.Visual.OffsetX,
                definition.Visual.OffsetY,
                0f);

            visualTransform = body.transform;
            bodyRenderer = body.AddComponent<SpriteRenderer>();
            bodyRenderer.sortingLayerName = definition.Visual.SortingLayer;
            bodyRenderer.sortingOrder = definition.Visual.SortingOrder;

            fallbackSprite = ResolveBaseSprite();
            bodyRenderer.sprite = fallbackSprite;
            ApplyTint();
            ApplyBodyScale();
            baseVisualScale = visualTransform.localScale;
        }

        private void CreateHeldItemVisual()
        {
            Transform parent = root;

            if (prefabMode && visualTransform != null)
            {
                parent = ResolveHeldItemAnchor();
                if (parent == null)
                    parent = visualTransform;
            }

            GameObject held = new GameObject("HeldItem");
            held.transform.SetParent(parent, false);
            heldTransform = held.transform;
            heldRenderer = held.AddComponent<SpriteRenderer>();
            heldRenderer.sortingLayerName = definition.Visual.SortingLayer;
            heldRenderer.sortingOrder = definition.Visual.SortingOrder + 2;
            heldTransform.localPosition = new Vector3(
                definition.Visual.HeldItemOffsetX,
                definition.Visual.HeldItemOffsetY,
                0f);
        }

        private Transform ResolveHeldItemAnchor()
        {
            if (visualTransform == null)
                return null;

            // 1) Явный путь из JSON (если указан).
            if (!string.IsNullOrWhiteSpace(definition.Visual.HeldItemAnchorPath))
            {
                Transform byPath = visualTransform.Find(definition.Visual.HeldItemAnchorPath.Trim());
                if (byPath != null)
                    return byPath;
            }

            // 2) Общие имена анкоров в риг-префабах.
            //    Порядок важен: сначала специфичное, потом универсальное.
            string[] candidates =
            {
        "HeldItemAnchor",
        "HandPoint",
        "Hand",
        "ItemAnchor"
    };

            for (int i = 0; i < candidates.Length; i++)
            {
                Transform found = FindRecursiveByName(visualTransform, candidates[i]);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static Transform FindRecursiveByName(Transform parent, string wantedName)
        {
            if (parent == null)
                return null;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name, wantedName, StringComparison.OrdinalIgnoreCase))
                    return child;

                Transform nested = FindRecursiveByName(child, wantedName);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private void CreateStars(int stars)
        {
            Debug.Log($"[Stars] entity={definition.ID} stars={stars}");

            int count = Mathf.Max(0, stars);
            if (count == 0) return;

            // Если звёзд нет — вообще не создаём объект.
            if (count == 0)
                return;

            GameObject starGo = new GameObject("Stars");
            starGo.transform.SetParent(root, false);
            starGo.transform.localPosition = new Vector3(
                0f,
                definition.ColliderHeight * 0.62f + 0.25f,
                0f);

            starsText = starGo.AddComponent<TextMesh>();
            starsText.text = new string('▪', count);
            starsText.anchor = TextAnchor.MiddleCenter;
            starsText.alignment = TextAlignment.Center;
            starsText.fontSize = 45;
            starsText.characterSize = 0.065f;
            starsText.color = new Color(1f, 0.87f, 0.28f, 1f);

            MeshRenderer starRenderer = starGo.GetComponent<MeshRenderer>();
            if (starRenderer != null)
            {
                starRenderer.sortingLayerName = definition.Visual.SortingLayer;
                starRenderer.sortingOrder = definition.Visual.SortingOrder + 5;
            }
        }

        // =====================================================
        // PUBLIC VISUAL API
        // =====================================================

        public void SetHeldItem(string itemId)
        {
            if (heldRenderer == null)
                return;

            heldRenderer.sprite = string.IsNullOrWhiteSpace(itemId)
                ? null
                : ItemSpriteResolver.Resolve(itemId);

            if (heldRenderer.sprite == null)
                return;

            // ---- Значения по умолчанию — из EntityDefinition ----
            float itemScale = 1f;
            float itemRotationDeg = definition.Visual.HeldItemRotation;
            float itemOffsetX = definition.Visual.HeldItemOffsetX;
            float itemOffsetY = definition.Visual.HeldItemOffsetY;

            // ---- Переопределения из JSON предмета ----
            if (definition.Visual.UseItemHeldPose && ItemRegistry.TryGet(itemId, out ItemDefinition item) && item != null)
            {
                if (item.HeldScale > 0.0001f)
                    itemScale = item.HeldScale;

                itemRotationDeg = item.HeldRotation;

                // Смещения в руке: если предмет что-то задал — берём его,
                // иначе остаётся значение из EntityDefinition.
                if (Mathf.Abs(item.HeldOffsetX) > 0.0001f)
                    itemOffsetX = item.HeldOffsetX;

                if (Mathf.Abs(item.HeldOffsetY) > 0.0001f)
                    itemOffsetY = item.HeldOffsetY;
            }

            // ---- Размер: fit по длинной стороне спрайта ----
            float longest = Mathf.Max(
                heldRenderer.sprite.bounds.size.x,
                heldRenderer.sprite.bounds.size.y);

            if (longest <= 0.0001f)
                longest = 1f;

            float desiredWorldSize =
                Mathf.Max(0.01f, definition.Visual.HeldItemWorldSize) * itemScale;

            // В префаб-режиме рука имеет накопленный масштаб (PrefabScale и
            // цепочка костей). Компенсируем его, чтобы предмет в мире был
            // ровно desiredWorldSize, а не «раздутым» на этот множитель.
            Vector3 parentLossy = heldTransform.parent != null
                ? heldTransform.parent.lossyScale
                : Vector3.one;

            float parentScaleFactor = Mathf.Max(
                0.0001f,
                Mathf.Max(Mathf.Abs(parentLossy.x), Mathf.Abs(parentLossy.y)));

            float localScale = (desiredWorldSize / longest) / parentScaleFactor;
            heldTransform.localScale = Vector3.one * localScale;

            // Локальный поворот: pickup садится в руку под нужным углом.
            // Ranged-оружие потом переопределит world rotation в AimHeldItem.
            heldBaseRotation = itemRotationDeg;
            heldTransform.localRotation = Quaternion.Euler(0f, 0f, itemRotationDeg);
            heldTransform.localPosition = new Vector3(itemOffsetX, itemOffsetY, 0f);
        }

        public void SetFacing(float horizontalDirection)
        {
            if (Mathf.Abs(horizontalDirection) > 0.01f)
                facing = Mathf.Sign(horizontalDirection);

            if (prefabMode && visualTransform != null)
            {
                float authoredDirection = definition.Visual.PrefabFacesRight ? 1f : -1f;
                Vector3 scale = baseVisualScale;
                scale.x = Mathf.Abs(baseVisualScale.x) * facing * authoredDirection;
                visualTransform.localScale = scale;
            }
            else if (bodyRenderer != null)
            {
                bodyRenderer.flipX = facing < 0f;

                if (heldTransform != null && heldTransform.parent == root)
                {
                    Vector3 heldPosition = heldTransform.localPosition;
                    heldPosition.x = Mathf.Abs(definition.Visual.HeldItemOffsetX) * facing;
                    heldTransform.localPosition = heldPosition;
                }
            }
        }

        public void AimHeldItem(Vector2 worldDirection, float attackPhase, WeaponMetadata weapon = null)
        {
            if (heldRenderer == null || heldRenderer.sprite == null ||
                worldDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            float angle = Mathf.Atan2(worldDirection.y, worldDirection.x) * Mathf.Rad2Deg;
            if (state == EntityVisualState.Attack && weapon != null && weapon.GetKind() == WeaponKind.Sword)
            {
                float phase = attackPhase;
                float relative = MeleeAttackMotion.SwingAngle(weapon, phase);
                Vector2 direction = PixelSwordTrail.SwingDirection(worldDirection, facing >= 0f, relative);
                angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                PixelSwordTrail.Record(root, heldTransform.position, direction,
                    Mathf.Max(.4f, weapon.SwingReach), phase, false,
                    definition.Visual.SortingLayer, definition.Visual.SortingOrder + 1);
            }
            else if (state == EntityVisualState.Attack)
            {
                float kick = Mathf.Sin(Mathf.Clamp01(attackPhase) * Mathf.PI) * 16f;
                angle -= kick;
            }

            // World rotation is intentional here: with a skeletal prefab the held
            // item may live under a mirrored hand-bone hierarchy. Assigning world
            // rotation keeps the item aiming at the player in both facing directions.
            heldTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void SetState(EntityVisualState newState, bool force = false)
        {
            if (!force && state == newState)
                return;

            if (state == EntityVisualState.Attack && newState != EntityVisualState.Attack && heldTransform != null)
                heldTransform.localRotation = Quaternion.Euler(0f, 0f, heldBaseRotation);

            state = newState;
            stateStartTime = Time.time;

            if (prefabMode)
                PlayAnimatorState(newState, force);
        }

        public void Tick()
        {

            if (prefabMode)
                return;

            if (bodyRenderer == null)
                return;

            Sprite[] frames = GetFrames(state);
            if (frames == null || frames.Length == 0)
            {
                bodyRenderer.sprite = fallbackSprite;
                return;
            }

            float fps = Mathf.Max(0.01f, definition.Visual.FramesPerSecond);
            int frame = Mathf.FloorToInt((Time.time - stateStartTime) * fps);

            if (state == EntityVisualState.Death)
                frame = Mathf.Min(frame, frames.Length - 1);
            else
                frame %= frames.Length;

            bodyRenderer.sprite = frames[Mathf.Clamp(frame, 0, frames.Length - 1)];
        }

        // =====================================================
        // PREFAB / ANIMATOR
        // =====================================================

        private void PlayAnimatorState(EntityVisualState visualState, bool force)
        {
            if (animator == null || !animator.isActiveAndEnabled)
                return;

            string stateName = GetAnimatorStateName(visualState);
            if (string.IsNullOrWhiteSpace(stateName))
                return;

            int hash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, hash))
            {
                // Also allow full state paths such as "Base Layer.Attack".
                hash = Animator.StringToHash(stateName.Trim());
                if (!animator.HasState(0, hash))
                    return;
            }

            float fade = Mathf.Max(0f, definition.Visual.AnimatorCrossFade);
            if (force || fade <= 0.0001f)
                animator.Play(hash, 0, 0f);
            else
                animator.CrossFade(hash, fade, 0);
        }

        private string GetAnimatorStateName(EntityVisualState visualState)
        {
            switch (visualState)
            {
                case EntityVisualState.Walk:
                    return definition.Visual.AnimatorWalkState;
                case EntityVisualState.Attack:
                    return definition.Visual.AnimatorAttackState;
                case EntityVisualState.Hurt:
                    return definition.Visual.AnimatorHurtState;
                case EntityVisualState.Death:
                    return definition.Visual.AnimatorDeathState;
                default:
                    return definition.Visual.AnimatorIdleState;
            }
        }

        // =====================================================
        // SPRITE-FRAME MODE
        // =====================================================

        private Sprite[] GetFrames(EntityVisualState requested)
        {
            Sprite[] cached;
            if (animationCache.TryGetValue(requested, out cached))
                return cached;

            string[] paths = null;
            switch (requested)
            {
                case EntityVisualState.Idle: paths = definition.Visual.IdleSprites; break;
                case EntityVisualState.Walk: paths = definition.Visual.WalkSprites; break;
                case EntityVisualState.Attack: paths = definition.Visual.AttackSprites; break;
                case EntityVisualState.Hurt: paths = definition.Visual.HurtSprites; break;
                case EntityVisualState.Death: paths = definition.Visual.DeathSprites; break;
            }

            if (paths == null || paths.Length == 0)
            {
                cached = fallbackSprite == null ? new Sprite[0] : new[] { fallbackSprite };
                animationCache[requested] = cached;
                return cached;
            }

            List<Sprite> result = new List<Sprite>();
            for (int i = 0; i < paths.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(paths[i]))
                    continue;

                Sprite sprite = LoadVisualSprite(paths[i]);
                if (sprite != null)
                    result.Add(sprite);
            }

            if (result.Count == 0 && fallbackSprite != null)
                result.Add(fallbackSprite);

            cached = result.ToArray();
            animationCache[requested] = cached;
            return cached;
        }

        private Sprite ResolveBaseSprite()
        {
            if (!string.IsNullOrWhiteSpace(definition.Visual.SpriteFile))
            {
                Sprite fileSprite = LoadVisualSprite(definition.Visual.SpriteFile);
                if (fileSprite != null)
                    return fileSprite;
            }

            if (!string.IsNullOrWhiteSpace(definition.Visual.SpriteResource))
            {
                Sprite resource = LoadVisualSprite(definition.Visual.SpriteResource);
                if (resource != null)
                    return resource;
            }

            if (!string.IsNullOrWhiteSpace(definition.Visual.SpriteItem))
            {
                Sprite item = ItemSpriteResolver.Resolve(definition.Visual.SpriteItem);
                if (item != null)
                    return item;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "EntityFallbackPixel";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
        }

        private static Sprite LoadVisualSprite(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            string clean = path.Trim();
            bool fileLike = clean.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                            clean.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            clean.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);

            if (!fileLike)
                return UnityEngine.Resources.Load<Sprite>(clean);

            string fullPath = Path.IsPathRooted(clean)
                ? clean
                : Path.Combine(
                    Application.dataPath,
                    "GameData",
                    clean.Replace('/', Path.DirectorySeparatorChar));

            Sprite cached;
            if (FileSpriteCache.TryGetValue(fullPath, out cached))
                return cached;

            if (!File.Exists(fullPath))
                return null;

            try
            {
                byte[] bytes = File.ReadAllBytes(fullPath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.name = Path.GetFileNameWithoutExtension(fullPath);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                if (!texture.LoadImage(bytes, false))
                {
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    16f);

                FileSpriteCache[fullPath] = sprite;
                return sprite;
            }
            catch
            {
                return null;
            }
        }

        private void ApplyTint()
        {
            if (bodyRenderer == null)
                return;

            Color color;
            if (!ColorUtility.TryParseHtmlString(definition.Visual.Color, out color))
                color = Color.white;
            bodyRenderer.color = color;
        }

        private void ApplyBodyScale()
        {
            if (bodyRenderer == null || bodyRenderer.sprite == null)
                return;

            float width = Mathf.Max(0.0001f, bodyRenderer.sprite.bounds.size.x);
            float scale = Mathf.Max(0.01f, definition.Visual.WorldWidth) / width;
            bodyRenderer.transform.localScale = Vector3.one * scale;
        }
    }
}
