using UnityEngine;
using UnityEngine.UI;
// using TMPro; // Commented out to fix compilation errors
using EchoThief.Core;
using EchoThief.Player;

namespace EchoThief.UI
{
    /// <summary>
    /// Drives the minimal in-game HUD:
    /// - Ping cooldown ring
    /// - Gem counter
    /// - Noise maker count
    /// - Alert meter (only visible when guards are suspicious+)
    /// 
    /// All elements fade in/out contextually to keep the screen clean.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the player controller.")]
        [SerializeField] private PlayerController _playerController;

        [Header("Ping Cooldown")]
        [Tooltip("Image component using 'Filled' type for the radial cooldown ring.")]
        [SerializeField] private Image _pingCooldownRing;

        [Header("Gem Counter")]
        [SerializeField] private Text _gemCounterText;
        [SerializeField] private CanvasGroup _gemCounterGroup;
        private float _gemDisplayTimer;
        private const float GemDisplayDuration = 3f;

        [Header("Noise Maker Count")]
        [SerializeField] private Text _noiseMakerText;

        [Header("Alert Meter")]
        [Tooltip("Fill image for the alert meter (red bar).")]
        [SerializeField] private Image _alertMeterFill;
        [SerializeField] private CanvasGroup _alertMeterGroup;

        private void OnEnable()
        {
            ScoreManager.OnScoreChanged += HandleScoreChanged;
        }

        private void OnDisable()
        {
            ScoreManager.OnScoreChanged -= HandleScoreChanged;
        }

        private void Update()
        {
            UpdatePingCooldown();
            UpdateNoiseMakerCount();
            UpdateGemFade();
        }

        private void UpdatePingCooldown()
        {
            if (_pingCooldownRing == null || _playerController == null) return;

            // Fill amount: 1 when ready, 0 when on cooldown
            _pingCooldownRing.fillAmount = 1f - _playerController.PingCooldownNormalized;

            // Change color when ready
            _pingCooldownRing.color = _playerController.CanPing
                ? new Color(0f, 0.9f, 1f, 0.8f)   // Cyan when ready
                : new Color(0.3f, 0.3f, 0.3f, 0.4f); // Dim grey when on cooldown
        }

        private void UpdateNoiseMakerCount()
        {
            if (_noiseMakerText == null || _playerController == null) return;
            _noiseMakerText.text = $"×{_playerController.NoiseMakerCount}";
        }

        private void HandleScoreChanged(ScoreData data)
        {
            // Update gem counter
            if (_gemCounterText != null)
            {
                _gemCounterText.text = $"{data.GemsCollected} / {data.TotalGems}";
                _gemDisplayTimer = GemDisplayDuration;
            }
        }

        private void UpdateGemFade()
        {
            if (_gemCounterGroup == null) return;

            _gemDisplayTimer -= Time.deltaTime;
            float targetAlpha = _gemDisplayTimer > 0 ? 1f : 0f;
            _gemCounterGroup.alpha = Mathf.MoveTowards(_gemCounterGroup.alpha, targetAlpha, Time.deltaTime * 2f);
        }

        /// <summary>
        /// Call this from the guard alert system to update the alert meter.
        /// Value from 0 (safe) to 1 (danger).
        /// </summary>
        public void SetAlertLevel(float level)
        {
            if (_alertMeterFill != null)
                _alertMeterFill.fillAmount = level;

            if (_alertMeterGroup != null)
            {
                float targetAlpha = level > 0.01f ? 1f : 0f;
                _alertMeterGroup.alpha = Mathf.MoveTowards(_alertMeterGroup.alpha, targetAlpha, Time.deltaTime * 3f);
            }
        }
    }
}
