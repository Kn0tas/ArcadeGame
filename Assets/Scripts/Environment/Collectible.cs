using UnityEngine;
using EchoThief.Core;

namespace EchoThief.Environment
{
    /// <summary>
    /// Types of collectible items.
    /// </summary>
    public enum CollectibleType
    {
        Gem,
        Artifact,
        KeyCard,
        NoiseMaker,
        SoftShoes,
        EchoBomb
    }

    /// <summary>
    /// Collectible item component. When the player enters the trigger collider,
    /// the item is collected and the appropriate game event fires.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Collectible : MonoBehaviour
    {
        [Header("Collectible Settings")]
        [SerializeField] private CollectibleType _type = CollectibleType.Gem;

        [Tooltip("Small sonar glow when the player is near (free hint).")]
        [SerializeField] private float _proximityGlowRadius = 2f;
        [SerializeField] private Color _glowColor = new Color(1f, 1f, 0.5f, 1f); // Warm yellow

        [Header("Audio")]
        [SerializeField] private AudioClip _collectSound;

        public CollectibleType Type => _type;

        private bool _collected;

        private void OnTriggerEnter(Collider other)
        {
            if (_collected) return;

            if (other.CompareTag("Player"))
            {
                Collect(other.gameObject);
            }
        }

        private void Collect(GameObject player)
        {
            _collected = true;
            Debug.Log($"[Collectible] Player collected: {_type}");

            // Tiny sonar glow on pickup
            NoiseEventBus.EmitNoise(new NoiseEvent(
                origin: transform.position,
                loudness: 0.01f, // Nearly silent — guards shouldn't hear this
                sonarRadius: _proximityGlowRadius,
                sonarColor: _glowColor,
                source: gameObject
            ));

            // Notify game systems based on type
            switch (_type)
            {
                case CollectibleType.Gem:
                    ScoreManager.Instance?.AddGem();
                    break;

                case CollectibleType.Artifact:
                    ScoreManager.Instance?.AddArtifact();
                    break;

                case CollectibleType.NoiseMaker:
                    var playerController = player.GetComponent<EchoThief.Player.PlayerController>();
                    if (playerController != null)
                    {
                        playerController.AddNoiseMaker(1);
                    }
                    break;

                case CollectibleType.KeyCard:
                    // TODO: Add to player's key inventory
                    break;

                case CollectibleType.SoftShoes:
                    // TODO: Apply temporary silent-running buff
                    break;

                case CollectibleType.EchoBomb:
                    // TODO: Add to player's special inventory
                    break;
            }

            // Play collect sound
            if (_collectSound != null)
            {
                AudioSource.PlayClipAtPoint(_collectSound, transform.position);
            }

            // Destroy the collectible
            Destroy(gameObject);
        }

        /// <summary>
        /// Draw the proximity glow radius in the editor.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _glowColor;
            Gizmos.DrawWireSphere(transform.position, _proximityGlowRadius);
        }
    }
}
