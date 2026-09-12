using System;
using System.Reflection;
using Game.World;
using GameWorld = Game.World.World;
using UnityEngine;

namespace Game.BlockTransforms
{
    public static class BlockTransformGameplayController
    {
        public static BlockTransformMode Mode { get; private set; }
        public static event Action<BlockTransformMode> ModeChanged;

        public static bool UpdateAndConsume(
            WorldManager worldManager,
            GameWorld world,
            Camera playerCamera,
            Transform player,
            float interactionDistance)
        {
            BlockTransformRuntimeBootstrap.Ensure();

            if (Input.GetKeyDown(KeyCode.Z))
                CycleMode();

            if (Mode == BlockTransformMode.None)
                return false;

            if (Input.GetMouseButtonDown(0))
            {
                TryTransform(
                    worldManager,
                    world,
                    playerCamera,
                    player,
                    interactionDistance);
            }

            return true;
        }

        public static void SetMode(BlockTransformMode mode)
        {
            if (Mode == mode)
                return;

            Mode = mode;
            Action<BlockTransformMode> handler = ModeChanged;
            if (handler != null)
                handler(mode);
        }

        private static void CycleMode()
        {
            if (Mode == BlockTransformMode.None)
                SetMode(BlockTransformMode.Rotate);
            else if (Mode == BlockTransformMode.Rotate)
                SetMode(BlockTransformMode.Mirror);
            else
                SetMode(BlockTransformMode.None);
        }

        private static void TryTransform(
            WorldManager manager,
            GameWorld world,
            Camera camera,
            Transform player,
            float interactionDistance)
        {
            if (manager == null || world == null || camera == null || player == null)
                return;

            Vector3 mouse = camera.ScreenToWorldPoint(Input.mousePosition);
            int x = Mathf.FloorToInt(mouse.x);
            int y = Mathf.FloorToInt(mouse.y);

            Vector2 cellCenter = new Vector2(x + 0.5f, y + 0.5f);
            if (Vector2.Distance(player.position, cellCenter) > interactionDistance)
                return;

            if (world.GetBlock(x, y) == 0)
                return;

            if (Mode == BlockTransformMode.Rotate)
                BlockTransformRegistry.Rotate(x, y);
            else if (Mode == BlockTransformMode.Mirror)
                BlockTransformRegistry.ToggleMirror(x, y);
            else
                return;

            RefreshCell(manager, world, x, y);
            RefreshPlayer(player);
        }

        private static void RefreshCell(
            WorldManager manager,
            GameWorld world,
            int worldX,
            int worldY)
        {
            int chunkX = Mathf.FloorToInt((float)worldX / Chunk.SizeX);
            int chunkY = Mathf.FloorToInt((float)worldY / Chunk.SizeY);
            int localX = worldX - chunkX * Chunk.SizeX;
            int localY = worldY - chunkY * Chunk.SizeY;

            if (localX < 0) localX += Chunk.SizeX;
            if (localY < 0) localY += Chunk.SizeY;

            Chunk chunk = world.GetChunk(chunkX, chunkY);
            if (chunk == null)
                return;

            object renderer = GetMember(manager, "renderer");
            Invoke(renderer, "UpdateBlock", new object[] { chunk, localX, localY });

            object collision = GetMember(manager, "chunkCollision");
            Invoke(collision, "BuildChunkCollision", new object[] { chunk });
        }

        private static void RefreshPlayer(Transform player)
        {
            Component[] components = player.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null || component.GetType().Name != "PlayerCollision")
                    continue;

                Invoke(component, "ResolveOverlaps", null);
                Invoke(component, "ForceGroundCheck", null);
                break;
            }
        }

        private static object GetMember(object target, string name)
        {
            if (target == null)
                return null;

            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            FieldInfo field = target.GetType().GetField(name, flags);
            if (field != null)
                return field.GetValue(target);

            PropertyInfo property = target.GetType().GetProperty(name, flags);
            if (property != null && property.GetIndexParameters().Length == 0)
                return property.GetValue(target, null);

            return null;
        }

        private static void Invoke(object target, string methodName, object[] args)
        {
            if (target == null)
                return;

            try
            {
                MethodInfo[] methods = target.GetType().GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                int count = args == null ? 0 : args.Length;

                for (int i = 0; i < methods.Length; i++)
                {
                    if (methods[i].Name != methodName || methods[i].GetParameters().Length != count)
                        continue;

                    methods[i].Invoke(target, args);
                    return;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("BLOCK TRANSFORMS: refresh failed: " + exception.Message);
            }
        }
    }

    public sealed class BlockTransformRuntimeBootstrap : MonoBehaviour
    {
        private static BlockTransformRuntimeBootstrap instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            Ensure();
        }

        public static void Ensure()
        {
            BlockTransformPersistence.EnsureInstance();

            if (instance != null)
                return;

            GameObject go = new GameObject("BlockTransformHUD");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<BlockTransformRuntimeBootstrap>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnGUI()
        {
            BlockTransformMode mode = BlockTransformGameplayController.Mode;
            if (mode == BlockTransformMode.None)
                return;

            string text = mode == BlockTransformMode.Rotate
                ? "↻  ПОВОРОТ БЛОКОВ   [Z]"
                : "⇆  ЗЕРКАЛИРОВАНИЕ БЛОКОВ   [Z]";

            float width = Mathf.Min(460f, Screen.width - 20f);
            Rect rect = new Rect(
                (Screen.width - width) * 0.5f,
                Screen.height - 112f,
                width,
                34f);

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;
            style.fontSize = Mathf.Clamp(Screen.height / 48, 14, 24);

            GUI.Box(rect, GUIContent.none);
            GUI.Label(rect, text, style);
        }
    }
}
