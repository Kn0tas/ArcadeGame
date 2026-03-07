using UnityEngine;
using EchoThief.Core;

namespace EchoThief.Environment
{
    /// <summary>
    /// Marks the level exit. Emits a periodic green beacon sonar pulse so the player
    /// can locate the exit in the dark. Triggers LevelCompleted() when the player enters,
    /// optionally requiring a minimum gem count first.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ExitZone : MonoBehaviour
    {
        [Header("Gem Requirements")]
        [Tooltip("If true, player must collect ALL gems (uses _requiredGems count).")]
        [SerializeField] private bool _requiresAllGems = false;

        [Tooltip("Minimum gems needed before exit unlocks (0 = no requirement).")]
        [SerializeField] private int _requiredGems = 0;

        [Header("Beacon Sonar")]
        [Tooltip("If true, periodically emit a sonar pulse so the player can find the exit.")]
        [SerializeField] private bool _emitBeacon = true;

        [Tooltip("Seconds between beacon pulses.")]
        [SerializeField] private float _beaconInterval = 3f;

        [Tooltip("Color of the beacon sonar ring.")]
        [SerializeField] private Color _beaconColor = new Color(0f, 0.9f, 0.47f, 1f); // Green

        [Tooltip("Radius of the beacon sonar ring.")]
        [SerializeField] private float _beaconRadius = 8f;

        private float _beaconTimer;
        private bool _isUnlocked;

        private void Awake()
        {
            // Ensure the collider is a trigger
            Collider col = GetComponent<Collider>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning("[ExitZone] Collider was not a trigger — fixed automatically.");
            }

            // If no gem requirement, exit starts unlocked
            _isUnlocked = (_requiredGems <= 0 && !_requiresAllGems);
        }

        private void Start()
        {
            // Emit first beacon immediately so player can find exit right away
            if (_emitBeacon)
                EmitBeacon();
        }

        private void Update()
        {
            if (!_emitBeacon) return;

            _beaconTimer -= Time.deltaTime;
            if (_beaconTimer <= 0f)
            {
                EmitBeacon();
                _beaconTimer = _beaconInterval;
            }

            // Re-check unlock state each frame (gem count may change)
            UpdateUnlockState();
        }

        private void UpdateUnlockState()
        {
            if (_requiredGems <= 0 && !_requiresAllGems)
            {
                _isUnlocked = true;
                return;
            }

            if (ScoreManager.Instance == null) return;

            int collected = ScoreManager.Instance.GemCount;
            _isUnlocked = collected >= _requiredGems;
        }

        private void EmitBeacon()
        {
            // Use a slightly different color when locked to hint it's not reachable yet
            Color pulseColor = _isUnlocked
                ? _beaconColor
                : new Color(_beaconColor.r * 0.4f, _beaconColor.g * 0.4f, _beaconColor.b * 0.4f, 1f);

            NoiseEventBus.EmitNoise(new NoiseEvent(
                origin: transform.position,
                loudness: 0.05f,        // Very quiet — guards won't react
                sonarRadius: _beaconRadius,
                sonarColor: pulseColor,
                source: gameObject
            ));
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            // Check if exit is unlocked
            UpdateUnlockState();
            if (!_isUnlocked)
            {
                int collected = ScoreManager.Instance != null ? ScoreManager.Instance.GemCount : 0;
                Debug.Log($"[ExitZone] Exit locked! Need {_requiredGems} gems, have {collected}.");

                // Emit a "rejected" pulse — red flash
                NoiseEventBus.EmitNoise(new NoiseEvent(
                    origin: transform.position,
                    loudness: 0.05f,
                    sonarRadius: _beaconRadius * 0.5f,
                    sonarColor: new Color(1f, 0.2f, 0.2f, 1f),
                    source: gameObject
                ));
                return;
            }

            Debug.Log("[ExitZone] Player reached the exit — Level Complete!");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LevelCompleted();
            }
            else
            {
                // Fallback for test scenes without GameManager
                Debug.Log("[ExitZone] No GameManager found. Level would be complete.");
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _beaconColor;
            Gizmos.DrawWireSphere(transform.position, _beaconRadius);

            // Draw a smaller solid sphere to mark the exit position
            Gizmos.color = new Color(_beaconColor.r, _beaconColor.g, _beaconColor.b, 0.2f);
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
#endif
    }
}
