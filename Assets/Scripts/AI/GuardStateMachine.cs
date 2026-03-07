using UnityEngine;
using UnityEngine.AI;
using EchoThief.Core;

namespace EchoThief.AI
{
    /// <summary>
    /// Guard AI state machine with 4 states: Patrol, Suspicious, Alerted, Chasing.
    /// Phase 2: Added footstep sonar emission while moving.
    ///          Wired GuardPatrol component so guard walks waypoints in Patrol state.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class GuardStateMachine : MonoBehaviour
    {
        public enum GuardState { Patrol, Suspicious, Alerted, Chasing, Caught }

        [Header("State Timeouts")]
        [SerializeField] private float _suspiciousTimeout = 4f;
        [SerializeField] private float _alertedTimeout = 6f;

        [Header("Detection")]
        [SerializeField] private float _catchDistance = 1.5f;

        [Header("Movement Speeds")]
        [SerializeField] private float _patrolSpeed = 2f;
        [SerializeField] private float _suspiciousSpeed = 1.5f;
        [SerializeField] private float _alertedSpeed = 3f;
        [SerializeField] private float _chaseSpeed = 5f;

        [Header("Footstep Sonar (Phase 2)")]
        [SerializeField] private float _footstepSonarRadius = 4f;
        [SerializeField] private float _footstepInterval = 0.6f;
        [SerializeField] private Color _footstepColor = new Color(1f, 0.24f, 0f, 1f); // Red-orange

        private NavMeshAgent _agent;
        private GuardPatrol  _guardPatrol;           // Phase 2: wired patrol
        private Transform    _playerTransform;
        private GuardState   _currentState = GuardState.Patrol;
        private Vector3      _lastKnownPlayerPos;
        private float        _stateTimer;
        private float        _footstepTimer;

        public GuardState CurrentState  => _currentState;
        public bool       IsPatrolling  => _currentState == GuardState.Patrol;

        private void Awake()
        {
            _agent       = GetComponent<NavMeshAgent>();
            _guardPatrol = GetComponent<GuardPatrol>();

            _playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (_playerTransform == null)
                Debug.LogWarning("[GuardStateMachine] Player not found! Guard AI will not function.");

            if (_guardPatrol == null)
                Debug.LogWarning("[GuardStateMachine] No GuardPatrol component found. Guard will not walk waypoints.");
        }

        private void Start()
        {
            // Kick off initial patrol destination once NavMesh is ready
            if (_guardPatrol != null && _agent.isOnNavMesh)
                _guardPatrol.SetDestination(_agent);
        }

        private void Update()
        {
            if (_playerTransform == null) return;

            // Tick state timer
            _stateTimer -= Time.deltaTime;

            // Check catch
            float distToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
            if (distToPlayer < _catchDistance)
            {
                CatchPlayer();
                return;
            }

            // State timeout fallback → return to Patrol
            if (_stateTimer <= 0f)
            {
                switch (_currentState)
                {
                    case GuardState.Suspicious:
                        TransitionTo(GuardState.Patrol);
                        break;
                    case GuardState.Alerted:
                        TransitionTo(GuardState.Patrol);
                        break;
                }
            }

            ExecuteStateBehavior();
            EmitFootstepsIfMoving();
        }

        private void ExecuteStateBehavior()
        {
            switch (_currentState)
            {
                case GuardState.Patrol:
                    _agent.speed     = _patrolSpeed;
                    _agent.isStopped = false;
                    // Delegate waypoint logic to GuardPatrol
                    if (_guardPatrol != null)
                        _guardPatrol.UpdatePatrol(_agent);
                    break;

                case GuardState.Suspicious:
                    _agent.speed     = _suspiciousSpeed;
                    _agent.isStopped = true;
                    break;

                case GuardState.Alerted:
                    _agent.speed     = _alertedSpeed;
                    _agent.isStopped = false;
                    if (_agent.destination != _lastKnownPlayerPos)
                        _agent.SetDestination(_lastKnownPlayerPos);
                    break;

                case GuardState.Chasing:
                    _agent.speed     = _chaseSpeed;
                    _agent.isStopped = false;
                    _agent.SetDestination(_playerTransform.position);
                    break;

                case GuardState.Caught:
                    _agent.isStopped = true;
                    break;
            }
        }

        /// <summary>
        /// Phase 2: Emit red sonar pulses from guard footsteps while moving.
        /// Creates the "guards are visible via their footsteps" gameplay mechanic.
        /// </summary>
        private void EmitFootstepsIfMoving()
        {
            if (_agent.velocity.magnitude < 0.1f)
            {
                _footstepTimer = 0f;
                return;
            }

            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0f)
            {
                NoiseEventBus.EmitNoise(new NoiseEvent(
                    origin:      transform.position,
                    loudness:    0.3f,
                    sonarRadius: _footstepSonarRadius,
                    sonarColor:  _footstepColor,
                    source:      gameObject
                ));
                _footstepTimer = _footstepInterval;
            }
        }

        /// <summary>Called by GuardHearing when a noise is heard.</summary>
        public void OnNoiseHeard(Vector3 perceivedOrigin, float loudness)
        {
            _lastKnownPlayerPos = perceivedOrigin;

            switch (_currentState)
            {
                case GuardState.Patrol:
                    TransitionTo(GuardState.Suspicious);
                    _stateTimer = _suspiciousTimeout;
                    break;

                case GuardState.Suspicious:
                    // Second noise while suspicious → escalate
                    TransitionTo(GuardState.Alerted);
                    _stateTimer = _alertedTimeout;
                    break;

                case GuardState.Alerted:
                    // Already investigating — refresh target and timer
                    _stateTimer = _alertedTimeout;
                    break;

                case GuardState.Chasing:
                    // Don't downgrade from chase
                    break;
            }
        }

        private void TransitionTo(GuardState newState)
        {
            if (_currentState == newState) return;

            Debug.Log($"[Guard {name}] {_currentState} → {newState}");
            _currentState = newState;

            switch (newState)
            {
                case GuardState.Patrol:
                    _agent.isStopped = false;
                    // Resume patrol from wherever we are
                    if (_guardPatrol != null)
                    {
                        _guardPatrol.ResumePatrol();
                        _guardPatrol.SetDestination(_agent);
                    }
                    break;

                case GuardState.Suspicious:
                    _agent.isStopped = true;
                    break;

                case GuardState.Alerted:
                    _agent.isStopped = false;
                    break;

                case GuardState.Chasing:
                    _agent.isStopped = false;
                    break;
            }
        }

        private void CatchPlayer()
        {
            if (_currentState == GuardState.Caught) return;

            TransitionTo(GuardState.Caught);
            Debug.Log("[Guard] Player caught!");

            if (GameManager.Instance != null)
                GameManager.Instance.PlayerCaught();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Catch radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _catchDistance);

            // Footstep sonar radius
            Gizmos.color = _footstepColor;
            Gizmos.DrawWireSphere(transform.position, _footstepSonarRadius);

            // Last known player position
            if (_lastKnownPlayerPos != Vector3.zero)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, _lastKnownPlayerPos);
                Gizmos.DrawWireSphere(_lastKnownPlayerPos, 0.5f);
            }
        }
#endif
    }
}
