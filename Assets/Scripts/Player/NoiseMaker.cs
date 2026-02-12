using UnityEngine;
using EchoThief.Core;

namespace EchoThief.Player
{
    /// <summary>
    /// Component for a thrown noise maker object.
    /// Emits a loud noise event upon collision and then destroys itself.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class NoiseMaker : MonoBehaviour
    {
        [Header("Noise Settings")]
        [Tooltip("Loudness of the impact (0-1).")]
        [SerializeField] private float _impactLoudness = 0.8f;

        [Tooltip("Radius of the sonar pulse created on impact.")]
        [SerializeField] private float _impactRadius = 15f;

        [Tooltip("Color of the impact sonar pulse.")]
        [SerializeField] private Color _impactColor = new Color(1f, 0f, 1f, 1f); // Magenta

        [Header("Lifetime")]
        [Tooltip("Time before the object is destroyed after impact.")]
        [SerializeField] private float _destroyDelay = 0.1f;

        private bool _hasImpacted = false;

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasImpacted) return;

            // Only trigger on significant impacts (optional threshold)
            if (collision.relativeVelocity.magnitude > 2f)
            {
                TriggerImpact();
            }
        }

        private void TriggerImpact()
        {
            _hasImpacted = true;

            // Emit the noise event
            NoiseEventBus.EmitNoise(new NoiseEvent(
                origin: transform.position,
                loudness: _impactLoudness,
                sonarRadius: _impactRadius,
                sonarColor: _impactColor,
                source: gameObject // The noise maker itself is the source
            ));

            Debug.Log($"[NoiseMaker] Impact at {transform.position}!");

            // Destroy the object shortly after impact
            Destroy(gameObject, _destroyDelay);
        }
    }
}
