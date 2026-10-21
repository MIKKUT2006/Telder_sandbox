using Game.InputSupport;
using System.Collections;

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

using Game.Save;
using Game.UI.MainMenu;
using Game.GameplaySystems.Respawn;


namespace Game.UI.Pause
{
    public class PauseMenuController :
        MonoBehaviour
    {
        [Header("Panels")]

        [SerializeField]
        private GameObject pausePanel;

        [SerializeField]
        private GameObject settingsPanel;


        [Header("Scenes")]

        [SerializeField]
        private string mainMenuSceneName =
            "MainMenu";


        [Header("Optional Pixel Font")]

        [SerializeField]
        private TMP_FontAsset pixelFont;


        public bool IsPaused
        {
            get;
            private set;
        }


        public GameObject PausePanel =>
            pausePanel;


        public TMP_FontAsset PixelFont =>
            pixelFont;


        public string MainMenuSceneName =>
            mainMenuSceneName;


        private void Start()
        {
            Time.timeScale =
                1f;

            if (
                pausePanel != null
            )
            {
                pausePanel.SetActive(
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

            ApplyPixelFont();
            Game.UI.Achievements.AchievementUIController.EnsureInstalled(this, pausePanel, pixelFont);
        }


        private void Update()
        {
            // Death screen owns input while the player is dead. Esc must not
            // open/close PausePanel behind it.
            if (DeathScreenUI.IsVisible)
            {
                return;
            }


            if (
                !(GameInput.Pressed(GameAction.Pause) || (IsPaused && GameInput.Pressed(GameAction.UiCancel)))
            )
            {
                return;
            }

            if (Game.UI.Achievements.AchievementUIController.CloseIfOpen())
            {
                return;
            }

            if (
                Game.UI.Settings.SettingsTabsController.IsOpen ||
                (settingsPanel != null && settingsPanel.activeSelf)
            )
            {
                if (GameInput.Pressed(GameAction.UiCancel)) GameInput.ConsumeUiCancel();
                CloseSettings();

                return;
            }

            if (
                IsPaused
            )
            {
                ContinueGame();
            }
            else
            {
                PauseGame();
            }
        }


        public void PauseGame()
        {
            IsPaused =
                true;

            if (
                pausePanel != null
            )
            {
                pausePanel.SetActive(
                    true
                );
            }

            Time.timeScale =
                0f;
        }


        public void ContinueGame()
        {
            Game.UI.Settings.SettingsTabsController.Close();

            IsPaused =
                false;

            Game.UI.Achievements.AchievementUIController.CloseIfOpen();

            if (
                settingsPanel != null
            )
            {
                settingsPanel.SetActive(
                    false
                );
            }

            if (
                pausePanel != null
            )
            {
                pausePanel.SetActive(
                    false
                );
            }

            Time.timeScale =
                1f;
        }


        public void OpenSettings()
        {
            Game.UI.Settings.SettingsTabsController.Open();
        }


        public void CloseSettings()
        {
            Game.UI.Settings.SettingsTabsController.Close();

            if (
                settingsPanel != null
            )
            {
                settingsPanel.SetActive(
                    false
                );
            }
        }


        public void ExitToMainMenu()
        {
            if (
                !gameObject.activeInHierarchy
            )
            {
                return;
            }

            StartCoroutine(
                ExitRoutine()
            );
        }


        private IEnumerator ExitRoutine()
        {
            SaveGameRuntime
                .SaveCurrentScene();

            if (
                pausePanel != null
            )
            {
                pausePanel.SetActive(
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

            yield return
                new WaitForEndOfFrame();

            if (
                SaveGameRuntime.HasActiveSave
            )
            {
                SavePreviewCapture
                    .CaptureCurrentScreen(
                        SaveGameRuntime
                            .CurrentSaveId
                    );
            }

            // =================================================
            // IMPORTANT:
            // отключаем persistent-визуал мира ДО загрузки меню.
            // =================================================

            MainMenuVisualCleanup
                .CleanupPersistentWorldVisuals();

            Time.timeScale =
                1f;

            IsPaused =
                false;

            SceneManager.LoadScene(
                mainMenuSceneName
            );
        }


        private void ApplyPixelFont()
        {
            if (
                pixelFont == null
            )
            {
                return;
            }

            TMP_Text[] texts =
                GetComponentsInChildren<
                    TMP_Text
                >(
                    true
                );

            for (
                int i = 0;
                i < texts.Length;
                i++
            )
            {
                texts[i].font =
                    pixelFont;
            }
        }
    }
}
