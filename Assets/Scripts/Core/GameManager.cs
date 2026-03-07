using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Collections;

namespace EchoThief.Core
{
    /// <summary>
    /// Possible game states.
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        LevelComplete
    }

    /// <summary>
    /// Singleton that manages high-level game state, level transitions, and win/lose flow.
    /// Persists across scenes via DontDestroyOnLoad.
    /// 
    /// Phase 2: Terminal states (GameOver, LevelComplete) now trigger automatic scene
    /// reloads after a short delay, so the test scene loops without needing a UI.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        /// <summary>Fired when the game state changes. UI and other systems can subscribe.</summary>
        public static event Action<GameState> OnGameStateChanged;

        [Header("Level Settings")]
        [Tooltip("Scene names for each level, in order.")]
        [SerializeField] private string[] _levelSceneNames;

        [Header("Restart Delays")]
        [Tooltip("Seconds to wait after being caught before reloading.")]
        [SerializeField] private float _gameOverRestartDelay = 3f;

        private GameState _currentState = GameState.MainMenu;
        public GameState CurrentState => _currentState;

        private int _currentLevelIndex = 0;
        public int CurrentLevelIndex => _currentLevelIndex;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // If the game starts directly in a level scene (e.g. TestRoom hit Play),
            // auto-transition to Playing so ScoreManager and other systems initialise.
            if (_currentState == GameState.MainMenu)
                SetState(GameState.Playing);
        }

        /// <summary>
        /// Transition to a new game state.
        /// </summary>
        public void SetState(GameState newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;
            Debug.Log($"[GameManager] State → {newState}");

            switch (newState)
            {
                case GameState.Playing:
                    Time.timeScale = 1f;
                    if (ScoreManager.Instance != null)
                        ScoreManager.Instance.StartLevel();
                    break;

                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;

                case GameState.GameOver:
                    Time.timeScale = 0f;
                    if (ScoreManager.Instance != null)
                        ScoreManager.Instance.StopLevel();
                    StartCoroutine(RestartAfterDelay(_gameOverRestartDelay));
                    break;

                case GameState.LevelComplete:
                    Time.timeScale = 0f;
                    if (ScoreManager.Instance != null)
                    {
                        ScoreManager.Instance.StopLevel();
                        int final = ScoreManager.Instance.ComputeFinalScore();
                        Debug.Log($"[GameManager] Level complete! Final score: {final}");
                    }
                    // No auto-reload — overlay stays until player quits or editor is stopped.
                    break;
            }

            OnGameStateChanged?.Invoke(newState);
        }

        /// <summary>Start playing a specific level by index.</summary>
        public void LoadLevel(int levelIndex)
        {
            if (_levelSceneNames == null || levelIndex < 0 || levelIndex >= _levelSceneNames.Length)
            {
                Debug.LogWarning($"[GameManager] Invalid level index {levelIndex} — reloading current scene.");
                NoiseEventBus.ClearAll();
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                _currentState = GameState.Playing;
                return;
            }

            _currentLevelIndex = levelIndex;
            NoiseEventBus.ClearAll();
            Time.timeScale = 1f;
            SceneManager.LoadScene(_levelSceneNames[levelIndex]);
            _currentState = GameState.Playing;
        }

        /// <summary>Restart the current level.</summary>
        public void RestartLevel()
        {
            NoiseEventBus.ClearAll();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            _currentState = GameState.Playing;
        }

        /// <summary>Advance to the next level, or restart the current one if there is no next level.</summary>
        public void NextLevel()
        {
            int next = _currentLevelIndex + 1;
            if (_levelSceneNames != null && next < _levelSceneNames.Length)
            {
                LoadLevel(next);
            }
            else
            {
                // No next level defined — loop back to start
                Debug.Log("[GameManager] No next level — restarting current level.");
                RestartLevel();
            }
        }

        /// <summary>Load the main menu scene.</summary>
        public void LoadMainMenu()
        {
            NoiseEventBus.ClearAll();
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
            _currentState = GameState.MainMenu;
        }

        /// <summary>Called when the player is caught by a guard.</summary>
        public void PlayerCaught()
        {
            SetState(GameState.GameOver);
        }

        /// <summary>Called when the player reaches the exit.</summary>
        public void LevelCompleted()
        {
            Debug.Log("[GameManager] LevelCompleted() called — freezing game and showing overlay.");
            Time.timeScale = 0f;
            ShowLevelCompleteUI();
            SetState(GameState.LevelComplete);
        }

        // ── UI Overlay ───────────────────────────────────────────────────

        /// <summary>
        /// Spawns a fullscreen "LEVEL COMPLETE" overlay at runtime.
        /// No scene canvas required — creates its own Canvas/CanvasScaler/Text.
        /// </summary>
        private void ShowLevelCompleteUI()
        {
            // Avoid duplicates if called twice
            if (GameObject.Find("LevelCompleteCanvas") != null) return;

            // Canvas
            var canvasGO = new GameObject("LevelCompleteCanvas");
            DontDestroyOnLoad(canvasGO);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Dark semi-transparent background panel
            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelImg = panelGO.AddComponent<Image>();
            panelImg.color = new Color(0f, 0f, 0f, 0.6f);
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

            // "LEVEL COMPLETE" text
            var textGO = new GameObject("LevelCompleteText");
            textGO.transform.SetParent(canvasGO.transform, false);
            var text = textGO.AddComponent<Text>();
            text.text = "LEVEL COMPLETE";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 96;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.2f, 1f, 0.4f, 1f);   // sonar-green

            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.4f);
            textRect.anchorMax = new Vector2(1f, 0.7f);
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;

            // Subtitle
            var subGO = new GameObject("SubText");
            subGO.transform.SetParent(canvasGO.transform, false);
            var sub = subGO.AddComponent<Text>();
            sub.text = "you escaped with the gem";
            sub.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sub.fontSize = 36;
            sub.alignment = TextAnchor.MiddleCenter;
            sub.color = new Color(0.7f, 0.9f, 0.7f, 0.8f);

            var subRect = subGO.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0.32f);
            subRect.anchorMax = new Vector2(1f, 0.42f);
            subRect.offsetMin = subRect.offsetMax = Vector2.zero;
        }

        /// <summary>Toggle pause state.</summary>
        public void TogglePause()
        {
            if (_currentState == GameState.Playing)
                SetState(GameState.Paused);
            else if (_currentState == GameState.Paused)
                SetState(GameState.Playing);
        }

        // ── Coroutines ──────────────────────────────────────────────────

        private IEnumerator RestartAfterDelay(float delay)
        {
            // Use unscaled time so this still ticks when timeScale = 0
            yield return new WaitForSecondsRealtime(delay);
            RestartLevel();
        }


    }
}