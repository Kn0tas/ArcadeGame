using UnityEngine;
using System;
using EchoThief.Environment;

namespace EchoThief.Core
{
    /// <summary>
    /// Tracks score components during a level and computes the final score.
    /// 
    /// Formula:
    ///   FinalScore = ArtifactsStolen * 1000
    ///              + GemsCollected * 100
    ///              - PingsUsed * 10
    ///              + TimeBonus (if under par)
    ///              + GhostBonus (if 0 pings)
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        /// <summary>Fired when the score changes so UI can update.</summary>
        public static event Action<ScoreData> OnScoreChanged;

        [Header("Scoring Config")]
        [SerializeField] private int _artifactPoints = 1000;
        [SerializeField] private int _gemPoints = 100;
        [SerializeField] private int _pingPenalty = 10;
        [SerializeField] private int _ghostBonus = 500;
        [SerializeField] private int _timeBonusPerSecond = 5;
        [SerializeField] private float _parTime = 120f; // seconds

        private int _artifactsStolen;
        private int _gemsCollected;
        private int _totalGems;
        private int _pingsUsed;
        private float _levelStartTime;

        public int GemsCollected => _gemsCollected;
        public int TotalGems => _totalGems;
        public int PingsUsed => _pingsUsed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Auto-initialize level tracking based on scene objects
            // This fixes the bug where total gems started at 0
            var collectibles = FindObjectsOfType<Collectible>();
            int totalGemCount = 0;
            foreach (var c in collectibles)
            {
                if (c.Type == CollectibleType.Gem)
                {
                    totalGemCount++;
                }
            }

            InitLevel(totalGemCount, _parTime);
        }

        /// <summary>
        /// Call at level start to reset tracking.
        /// </summary>
        public void InitLevel(int totalGems, float parTime)
        {
            _artifactsStolen = 0;
            _gemsCollected = 0;
            _totalGems = totalGems;
            _pingsUsed = 0;
            _parTime = parTime;
            _levelStartTime = Time.time;

            NotifyChange();
        }

        public void AddArtifact()
        {
            _artifactsStolen++;
            NotifyChange();
        }

        public void AddGem()
        {
            _gemsCollected++;
            NotifyChange();
        }

        public void AddPing()
        {
            _pingsUsed++;
            NotifyChange();
        }

        /// <summary>
        /// Compute the final score for the level.
        /// </summary>
        public int ComputeFinalScore()
        {
            float elapsed = Time.time - _levelStartTime;
            int score = _artifactsStolen * _artifactPoints
                      + _gemsCollected * _gemPoints
                      - _pingsUsed * _pingPenalty;

            // Time bonus
            if (elapsed < _parTime)
            {
                int secondsUnder = Mathf.FloorToInt(_parTime - elapsed);
                score += secondsUnder * _timeBonusPerSecond;
            }

            // Ghost bonus (no pings at all)
            if (_pingsUsed == 0)
            {
                score += _ghostBonus;
            }

            return Mathf.Max(0, score);
        }

        private void NotifyChange()
        {
            OnScoreChanged?.Invoke(new ScoreData
            {
                ArtifactsStolen = _artifactsStolen,
                GemsCollected = _gemsCollected,
                TotalGems = _totalGems,
                PingsUsed = _pingsUsed,
                ElapsedTime = Time.time - _levelStartTime
            });
        }
    }

    /// <summary>
    /// Snapshot of current score state, passed to UI via events.
    /// </summary>
    public struct ScoreData
    {
        public int ArtifactsStolen;
        public int GemsCollected;
        public int TotalGems;
        public int PingsUsed;
        public float ElapsedTime;
    }
}
