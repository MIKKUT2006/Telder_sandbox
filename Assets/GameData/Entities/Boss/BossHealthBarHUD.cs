using System.Collections.Generic;
using UnityEngine;

namespace Game.Entities.Boss
{
    /// <summary>Overlay bar for all living creatures carrying the boss tag.</summary>
    [DisallowMultipleComponent]
    public sealed class BossHealthBarHUD : MonoBehaviour
    {
        private static BossHealthBarHUD instance;
        private GUIStyle titleStyle;
        public static void Ensure()
        {
            if (instance != null) return;
            var root = new GameObject("[UI] Boss health bars");
            instance = root.AddComponent<BossHealthBarHUD>();
        }
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
        }
        private void OnDestroy() { if (instance == this) instance = null; }
        private void OnGUI()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.alignment = TextAnchor.MiddleCenter;
                titleStyle.fontSize = 17;
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.normal.textColor = Color.white;
            }
            IReadOnlyList<EntityActor> actors = EntityActor.ActiveActors;
            float width = Mathf.Min(560f, Screen.width * .65f);
            float x = (Screen.width - width) * .5f;
            int shown = 0;
            for (int i = 0; i < actors.Count; i++)
            {
                var actor = actors[i];
                if (actor == null || !actor.IsBoss || actor.IsDead || actor.MaxHealth <= 0f ||
                    !actor.IsBossTerrainReady) continue;
                if (shown >= 3) break;
                float y = 24f + shown * 58f;
                string name = actor.DefinitionId;
                if (Game.Entities.EntityRegistry.TryGet(actor.DefinitionId, out EntityDefinition definition))
                    name = string.IsNullOrWhiteSpace(definition.Name) ? name : definition.Name;
                Color tint = new Color(.48f, .35f, .85f, 1f);
                if (actor.BossSettings != null &&
                    ColorUtility.TryParseHtmlString(actor.BossSettings.HealthBarColor, out Color parsed))
                    tint = parsed;
                GUI.color = Color.white;
                GUI.Label(new Rect(x, y, width, 26), name, titleStyle);
                GUI.color = new Color(.08f, .07f, .12f, .94f);
                GUI.DrawTexture(new Rect(x, y + 30, width, 18), Texture2D.whiteTexture);
                GUI.color = tint;
                GUI.DrawTexture(new Rect(x + 3, y + 33,
                    Mathf.Max(0f, width - 6f) * Mathf.Clamp01(actor.Health / actor.MaxHealth), 12),
                    Texture2D.whiteTexture);
                GUI.color = Color.white;
                shown++;
            }
        }
    }
}
