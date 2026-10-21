#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Game.InputSupport.Mobile.Editor
{
    public static class MobileGameplayPreviewMenu
    {
        private const string Key = "TELDER_TEST_MOBILE_UI";
        private const string MenuPath = "Tools/Game/Mobile/Preview Touch Controls (Editor)";

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            PlayerPrefs.SetInt(Key, PlayerPrefs.GetInt(Key, 0) == 0 ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log("MOBILE CONTROLS: Editor preview " +
                (PlayerPrefs.GetInt(Key) == 1 ? "enabled" : "disabled") +
                ". Restart Play Mode to apply.");
        }

        [MenuItem(MenuPath, true)]
        private static bool Checked()
        {
            UnityEditor.Menu.SetChecked(MenuPath, PlayerPrefs.GetInt(Key, 0) == 1);
            return true;
        }
    }
}
#endif
