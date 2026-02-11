using UnityEngine;
using EchoThief.Core;

namespace EchoThief.AI
{
    /// <summary>
    /// Listens to the NoiseEventBus and determines if this guard can hear a noise
    /// based on distance and loudness. Feeds perceived location to GuardStateMachine.
    /// </summary>
    [RequireComponent(typeof(GuardStateMachine))]
    public class GuardHearing : MonoBehaviour
    {
        [Header("Hearing")]
        [Tooltip("Base hearing range (multiplied by noise loudness).")]
        [SerializeField] private float _baseHearingRange = 20f;

        [Tooltip("How much random offset to add to perceived position (simulates imprecise hearing at distance).")]
        [SerializeField] private float _maxPerceptionError = 3f;

        private GuardStateMachine _stateMachine;

        private void Awake()
        {
            _stateMachine = GetComponent<GuardStateMachine>();
        }

        private void OnEnable()
        {
            NoiseEventBus.OnNoise += HandleNoise;
        }

        private void OnDisable()
        {
            NoiseEventBus.OnNoise -= HandleNoise;
        }

        private void HandleNoise(NoiseEvent noise)
        {
            // Don't react to our own footsteps
            if (noise.Source == gameObject) return;

            float distance = Vector3.Distance(transform.position, noise.Origin);
            float hearingRange = _baseHearingRange * noise.Loudness;

            if (distance > hearingRange) return;

            // Accuracy decreases with distance
            float accuracy = 1f - (distance / hearingRange);
            Vector3 error = Random.insideUnitSphere * _maxPerceptionError * (1f - accuracy);
            error.y = 0f; // Keep on the same plane

            Vector3 perceivedOrigin = noise.Origin + error;

            Debug.Log($"[Guard {name}] Heard noise! Distance: {distance:F1}, Accuracy: {accuracy:P0}");

            _stateMachine.OnNoiseHeard(perceivedOrigin, noise.Loudness);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _baseHearingRange);
        }
    }
}
