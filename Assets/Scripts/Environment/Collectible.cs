using UnityEngine;
using EchoThief.Core;
using EchoThief.Player;

namespace EchoThief.Environment
{
    /// <summary>
    /// Collectible items: gems, artifacts, noise makers.
    /// Phase 2: Gems emit proximity glow pulses when player is nearby.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Collectible : MonoBehaviour
    {
        public enum CollectibleType { Gem, Artifact, NoiseMaker }

        [Header("Type")]
        [SerializeField] private CollectibleType _type = CollectibleType.Gem;

        [Header("Proximity Glow (Gems only)")]
        [SerializeField] private float _proximityGlowRadius = 2f;
        [SerializeField] private Color _glowColor = new Color(1f, 0.92f, 0.23f, 1f); // Yellow
        [SerializeField] private float _glowInterval = 1.5f;

        [Header("Audio")]
        [SerializeField] private AudioClip _collectSound;

        private Transform _playerTransform;
        private float _glowTimer;

        public CollectibleType Type => _type;

        private void Awake()
        {
            // Ensure collider is trigger
            Collider col = GetComponent<Collider>();
            if (!col.isTrigger)
            {
                Debug.LogWarning($"[Collectible] {gameObject.name} collider is not a trigger. Setting isTrigger = true.");
                col.isTrigger = true;
            }

            // Cache player reference
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _playerTransform = player.transform;
        }

        private void Update()
        {
            // Phase 2: Proximity glow for gems
            if (_type == CollectibleType.Gem && _playerTransform != null)
            {
                EmitProximityGlow();
            }
        }

        /// <summary>
        /// Phase 2 Addition: Emit yellow sonar pulses when player is nearby.
        /// Helps player discover gems without spamming pings.
        /// </summary>
        private void EmitProximityGlow()
        {
            float dist = Vector3.Distance(transform.position, _playerTransform.position);

            if (dist < _proximityGlowRadius)
            {
                _glowTimer -= Time.deltaTime;
                if (_glowTimer <= 0f)
                {
                    NoiseEventBus.EmitNoise(new NoiseEvent(
                        origin: transform.position,
                        loudness: 0.1f,  // Very quiet — shouldn't alert guards
                        sonarRadius: 3f,
                        sonarColor: _glowColor,
                        source: gameObject
                    ));
                    _glowTimer = _glowInterval;
                }
            }
            else
            {
                _glowTimer = 0f;  // Reset when player leaves proximity
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            // Process collection based on type
            switch (_type)
            {
                case CollectibleType.Gem:
                    if (ScoreManager.Instance != null)
                        ScoreManager.Instance.AddGem();
                    break;

                case CollectibleType.Artifact:
                    if (ScoreManager.Instance != null)
                        ScoreManager.Instance.AddArtifact();
                    break;

                case CollectibleType.NoiseMaker:
                    PlayerController player = other.GetComponent<PlayerController>();
                    if (player != null)
                        player.AddNoiseMaker(1);
                    break;
            }

            // Play collection sound
            if (_collectSound != null)
            {
                AudioSource.PlayClipAtPoint(_collectSound, transform.position);
            }

            // Destroy collectible
            Destroy(gameObject);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw proximity glow radius (for gems)
            if (_type == CollectibleType.Gem)
            {
                Gizmos.color = new Color(_glowColor.r, _glowColor.g, _glowColor.b, 0.3f);
                Gizmos.DrawWireSphere(transform.position, _proximityGlowRadius);
            }
        }
#endif
    }
}