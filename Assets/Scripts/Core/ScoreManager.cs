using UnityEngine;
using UnityEngine.Events;

namespace EchoThief.Core
{
    /// <summary>
    /// Tracks scoring components per level. Singleton.
    /// Fires UnityEvents when counts change for UI updates.
    /// 
    /// Phase 2: Auto-starts the level timer on Start() so the HUD initialises
    /// correctly even when GameManager doesn't explicitly call StartLevel().
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Score Values")]
        [SerializeField] private int   _artifactPoints     = 1000;
        [SerializeField] private int   _gemPoints          = 100;
        [SerializeField] private int   _pingPenalty        = 10;
        [SerializeField] private int   _ghostBonus         = 500;
        [SerializeField] private int   _timeBonusPerSecond = 5;
        [SerializeField] private float _parTime            = 120f;

        [Header("Events (UnityEvents for UI)")]
        public UnityEvent<int> OnGemCountChanged      = new UnityEvent<int>();
        public UnityEvent<int> OnPingCountChanged     = new UnityEvent<int>();
        public UnityEvent<int> OnArtifactCountChanged = new UnityEvent<int>();
        public UnityEvent<int> OnScoreChanged         = new UnityEvent<int>();

        private int   _score;
        private int   _pingCount;
        private int   _gemsCollected;
        private int   _artifactsCollected;
        private float _levelTime;
        private bool  _isRunning;

        public int   Score             => _score;
        public int   PingCount         => _pingCount;
        public int   GemCount          => _gemsCollected;
        public int   ArtifactCount     => _artifactsCollected;
        public float LevelTime         => _levelTime;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            // Auto-start so the HUD and timer work even in standalone test scenes
            // that don't go through GameManager.LoadLevel().
            // GameManager.SetState(Playing) should call StartLevel() explicitly in
            // production; calling it here is safe because StartLevel() is idempotent.
            if (!_isRunning)
            {
                StartLevel();
            }
        }

        private void Update()
        {
            if (_isRunning) _levelTime += Time.deltaTime;
        }

        /// <summary>Reset all counters and begin the level timer.</summary>
        public void StartLevel()
        {
            _score              = 0;
            _pingCount          = 0;
            _gemsCollected      = 0;
            _artifactsCollected = 0;
            _levelTime          = 0f;
            _isRunning          = true;

            // Fire initial events so UI starts at correct "zero" state
            OnScoreChanged?.Invoke(_score);
            OnPingCountChanged?.Invoke(_pingCount);
            OnGemCountChanged?.Invoke(_gemsCollected);
            OnArtifactCountChanged?.Invoke(_artifactsCollected);
        }

        public void StopLevel() => _isRunning = false;

        /// <summary>Called by PlayerController each time the player pings.</summary>
        public void AddPing()
        {
            _pingCount++;
            _score -= _pingPenalty;
            OnPingCountChanged?.Invoke(_pingCount);
            OnScoreChanged?.Invoke(_score);
            Debug.Log($"[Score] Ping #{_pingCount}. Score: {_score} (-{_pingPenalty})");
        }

        public void AddGem()
        {
            _gemsCollected++;
            _score += _gemPoints;
            OnGemCountChanged?.Invoke(_gemsCollected);
            OnScoreChanged?.Invoke(_score);
            Debug.Log($"[Score] Gem collected #{_gemsCollected}. Score: {_score} (+{_gemPoints})");
        }

        public void AddArtifact()
        {
            _artifactsCollected++;
            _score += _artifactPoints;
            OnArtifactCountChanged?.Invoke(_artifactsCollected);
            OnScoreChanged?.Invoke(_score);
            Debug.Log($"[Score] Artifact collected #{_artifactsCollected}. Score: {_score} (+{_artifactPoints})");
        }

        public int ComputeFinalScore()
        {
            int final = _score;

            // Ghost bonus: never pinged
            if (_pingCount == 0) final += _ghostBonus;

            // Time bonus for finishing under par time
            float timeDelta = _parTime - _levelTime;
            if (timeDelta > 0) final += Mathf.FloorToInt(timeDelta * _timeBonusPerSecond);

            int finalScore = Mathf.Max(0, final);
            OnScoreChanged?.Invoke(finalScore);
            return finalScore;
        }

        public void ResetLevel()
        {
            _score              = 0;
            _pingCount          = 0;
            _gemsCollected      = 0;
            _artifactsCollected = 0;
            _levelTime          = 0f;
            _isRunning          = false;

            OnScoreChanged?.Invoke(0);
            OnPingCountChanged?.Invoke(0);
            OnGemCountChanged?.Invoke(0);
            OnArtifactCountChanged?.Invoke(0);
        }
    }
}
