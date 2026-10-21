#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Game.EditorTools
{
    public static class SteamDeckInputSystemInstaller
    {
        [MenuItem("Tools/Game/Steam Deck/Install Unity Input System")]
        public static void Install()
        {
            if (!EditorUtility.DisplayDialog("Steam Deck Input",
                "Установить официальный пакет com.unity.inputsystem? После установки в Project Settings > Player > Other Settings выбери Active Input Handling = Both и перезапусти Unity.",
                "Установить", "Отмена")) return;
            Client.Add("com.unity.inputsystem");
            Debug.Log("STEAM DECK: Unity Input System installation requested. Set Active Input Handling to Both and restart Editor when asked.");
        }
    }
}
#endif
