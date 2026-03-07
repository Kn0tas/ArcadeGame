using UnityEngine;
using UnityEngine.UI;
using EchoThief.Player;
using EchoThief.Core;

namespace EchoThief.UI
{
    /// <summary>
    /// Minimal HUD. Uses UnityEvents from ScoreManager for gem count updates.
    /// Polls PlayerController for ping cooldown and noise maker count (frame-dependent data).
    /// All UI references except _playerController are optional — null refs are handled gracefully.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController _playerController;

        [Header("Ping Cooldown")]
        [Tooltip("Image with Fill Method = Radial 360. Filled 0→1 as cooldown expires.")]
        [SerializeField] private Image _pingCooldownRing;

        [Header("Gem Counter")]
        [SerializeField] private Text  _gemCounterText;
        [SerializeField] private CanvasGroup _gemCounterGroup;

        [Header("Noise Makers")]
        [SerializeField] private Text  _noiseMakerText;

        [Header("Alert Meter")]
        [SerializeField] private Image       _alertMeterFill;
        [SerializeField] private CanvasGroup _alertMeterGroup;

        private void OnEnable()
        {
            // Subscribe to ScoreManager UnityEvents
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnGemCountChanged.AddListener(UpdateGemCount);
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from ScoreManager UnityEvents
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnGemCountChanged.RemoveListener(UpdateGemCount);
            }
        }

        private void Update()
        {
            if (_playerController == null) return;

            // Poll frame-dependent data (cooldown changes every frame)
            if (_pingCooldownRing != null)
                _pingCooldownRing.fillAmount = _playerController.PingCooldownNormalized;

            // Noise maker count
            if (_noiseMakerText != null)
                _noiseMakerText.text = $"x{_playerController.NoiseMakerCount}";
        }

        /// <summary>Called by ScoreManager.OnGemCountChanged UnityEvent.</summary>
        private void UpdateGemCount(int count)
        {
            if (_gemCounterText != null)
                _gemCounterText.text = $"Gems: {count}";

            if (_gemCounterGroup != null)
                _gemCounterGroup.alpha = count > 0 ? 1f : 0.4f;
        }

        /// <summary>Public method for manual updates if needed.</summary>
        public void SetGemCount(int count)
        {
            UpdateGemCount(count);
        }

        /// <summary>0–1. Drives the alert meter fill (fed by GuardStateMachine in future milestones).</summary>
        public void SetAlertLevel(float normalized)
        {
            if (_alertMeterFill != null)
                _alertMeterFill.fillAmount = Mathf.Clamp01(normalized);

            if (_alertMeterGroup != null)
                _alertMeterGroup.alpha = normalized > 0.01f ? 1f : 0f;
        }
    }
}