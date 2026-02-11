using UnityEngine;
using UnityEngine.AI;
using EchoThief.Core;

namespace EchoThief.AI
{
    /// <summary>
    /// Guard states for the finite state machine.
    /// </summary>
    public enum GuardState
    {
        Patrol,
        Suspicious,
        Alerted,
        Chasing
    }

    /// <summary>
    /// Core guard AI state machine. Manages transitions between states based on
    /// hearing events and player proximity.
    /// 
    /// Required Components: NavMeshAgent, GuardHearing, GuardPatrol.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(GuardHearing))]
    [RequireComponent(typeof(GuardPatrol))]
    public class GuardStateMachine : MonoBehaviour
    {
        [Header("State Settings")]
        [Tooltip("How long the guard stays suspicious before returning to patrol.")]
        [SerializeField] private float _suspiciousTimeout = 4f;

        [Tooltip("How long the guard stays alerted before escalating or returning.")]
        [SerializeField] private float _alertedTimeout = 6f;

        [Tooltip("Distance at which the guard catches the player.")]
        [SerializeField] private float _catchDistance = 1.5f;

        [Header("Speed")]
        [SerializeField] private float _patrolSpeed = 2f;
        [SerializeField] private float _suspiciousSpeed = 1.5f;
        [SerializeField] private float _alertedSpeed = 3f;
        [SerializeField] private float _chaseSpeed = 5f;

        [Header("Sonar (Guard's own footsteps)")]
        [SerializeField] private float _footstepSonarRadius = 4f;
        [SerializeField] private float _footstepInterval = 0.6f;
        [SerializeField] private Color _footstepColor = new Color(1f, 0.24f, 0f, 1f); // Red-orange

        private NavMeshAgent _agent;
        private GuardHearing _hearing;
        private GuardPatrol _patrol;

        private GuardState _currentState = GuardState.Patrol;
        public GuardState CurrentState => _currentState;

        private float _stateTimer;
        private float _footstepTimer;
        private Vector3 _lastHeardPosition;
        private Transform _playerTransform;

        public Vector3 LastHeardPosition => _lastHeardPosition;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _hearing = GetComponent<GuardHearing>();
            _patrol = GetComponent<GuardPatrol>();
        }

        private void Start()
        {
            // Try to find the player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _playerTransform = player.transform;

            SetState(GuardState.Patrol);
        }

        private void Update()
        {
            _stateTimer -= Time.deltaTime;

            switch (_currentState)
            {
                case GuardState.Patrol:
                    UpdatePatrol();
                    break;
                case GuardState.Suspicious:
                    UpdateSuspicious();
                    break;
                case GuardState.Alerted:
                    UpdateAlerted();
                    break;
                case GuardState.Chasing:
                    UpdateChasing();
                    break;
            }

            // Guard footstep sonar (guards reveal themselves too!)
            UpdateFootstepSonar();
        }

        /// <summary>
        /// Transition to a new state.
        /// </summary>
        public void SetState(GuardState newState)
        {
            _currentState = newState;

            switch (newState)
            {
                case GuardState.Patrol:
                    _agent.speed = _patrolSpeed;
                    _patrol.ResumePatrol();
                    break;

                case GuardState.Suspicious:
                    _agent.speed = _suspiciousSpeed;
                    _stateTimer = _suspiciousTimeout;
                    _agent.ResetPath(); // Stop and look around
                    break;

                case GuardState.Alerted:
                    _agent.speed = _alertedSpeed;
                    _stateTimer = _alertedTimeout;
                    _agent.SetDestination(_lastHeardPosition);
                    break;

                case GuardState.Chasing:
                    _agent.speed = _chaseSpeed;
                    break;
            }

            Debug.Log($"[Guard {name}] State → {newState}");
        }

        /// <summary>
        /// Called by GuardHearing when a noise is detected.
        /// </summary>
        public void OnNoiseHeard(Vector3 perceivedOrigin, float loudness)
        {
            _lastHeardPosition = perceivedOrigin;

            switch (_currentState)
            {
                case GuardState.Patrol:
                    SetState(GuardState.Suspicious);
                    break;
                case GuardState.Suspicious:
                    SetState(GuardState.Alerted);
                    break;
                case GuardState.Alerted:
                    // Update destination to new noise
                    _agent.SetDestination(_lastHeardPosition);
                    _stateTimer = _alertedTimeout; // Reset timer
                    break;
                case GuardState.Chasing:
                    // Already chasing — update target
                    break;
            }
        }

        private void UpdatePatrol()
        {
            // Patrol handles its own waypoint logic
            _patrol.UpdatePatrol(_agent);
        }

        private void UpdateSuspicious()
        {
            // Look toward the last heard position
            Vector3 lookDir = (_lastHeardPosition - transform.position).normalized;
            if (lookDir.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 3f);
            }

            if (_stateTimer <= 0f)
            {
                SetState(GuardState.Patrol);
            }
        }

        private void UpdateAlerted()
        {
            // Walk toward the noise source
            if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
            {
                // Arrived at noise source — look around briefly, then return
                if (_stateTimer <= 0f)
                {
                    SetState(GuardState.Patrol);
                }
            }

            // If we can see the player (close range), escalate to chasing
            if (_playerTransform != null)
            {
                float distToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
                if (distToPlayer < _catchDistance * 3f)
                {
                    SetState(GuardState.Chasing);
                }
            }
        }

        private void UpdateChasing()
        {
            if (_playerTransform == null) return;

            _agent.SetDestination(_playerTransform.position);

            float distToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
            if (distToPlayer <= _catchDistance)
            {
                CatchPlayer();
            }
        }

        private void CatchPlayer()
        {
            Debug.Log($"[Guard {name}] CAUGHT THE PLAYER!");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerCaught();
            }
        }

        /// <summary>
        /// Guards emit their own dim sonar pulses when walking — revealing themselves to the player.
        /// </summary>
        private void UpdateFootstepSonar()
        {
            if (_agent.velocity.magnitude < 0.1f) return;

            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0f)
            {
                _footstepTimer = _footstepInterval;

                // Guard footsteps are quiet — they don't alert other guards
                Sonar.SonarManager.Instance?.SpawnPulse(
                    transform.position,
                    _footstepSonarRadius,
                    _footstepColor
                );
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Catch range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _catchDistance);

            // Footstep sonar range
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _footstepSonarRadius);

            // Detection range (approximate for visualization)
            if (_hearing != null)
            {
                // We'd need _hearing properties, but can visualize current state
            }
            
            // Draw line to last heard position if suspicious/alerted
            if (_currentState == GuardState.Suspicious || _currentState == GuardState.Alerted)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, _lastHeardPosition);
                Gizmos.DrawWireSphere(_lastHeardPosition, 0.5f);
            }
        }
    }
}
