using UnityEngine;


namespace Game.UI.MainMenu
{
    public class MainMenuController :
        MonoBehaviour
    {
        [SerializeField]
        private GameObject homePanel;


        [SerializeField]
        private GameObject saveSelectionPanel;


        [SerializeField]
        private GameObject settingsPanel;


        [SerializeField]
        private SaveSelectionController
            saveSelection;


        private void Start()
        {
            Time.timeScale =
                1f;


            ShowHome();
        }


        public void Play()
        {
            if (
                homePanel != null
            )
            {
                homePanel.SetActive(
                    false
                );
            }


            if (
                settingsPanel != null
            )
            {
                settingsPanel.SetActive(
                    false
                );
            }


            if (
                saveSelectionPanel != null
            )
            {
                saveSelectionPanel.SetActive(
                    true
                );
            }


            saveSelection?.Refresh();
        }


        public void OpenSettings()
        {
            if (
                homePanel != null
            )
            {
                homePanel.SetActive(
                    false
                );
            }


            if (
                saveSelectionPanel != null
            )
            {
                saveSelectionPanel.SetActive(
                    false
                );
            }


            if (
                settingsPanel != null
            )
            {
                settingsPanel.SetActive(
                    true
                );
            }
        }


        public void ShowHome()
        {
            if (
                homePanel != null
            )
            {
                homePanel.SetActive(
                    true
                );
            }


            if (
                saveSelectionPanel != null
            )
            {
                saveSelectionPanel.SetActive(
                    false
                );
            }


            if (
                settingsPanel != null
            )
            {
                settingsPanel.SetActive(
                    false
                );
            }
        }


        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication
                .isPlaying =
                false;
#else
            Application.Quit();
#endif
        }
    }
}
