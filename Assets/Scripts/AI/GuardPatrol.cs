using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace EchoThief.AI
{
    /// <summary>
    /// Handles waypoint-based patrol behavior for guards.
    /// Guards walk between waypoints, pausing briefly at each one.
    /// 
    /// Phase 2 addition: If _waypoints is not assigned in the Inspector, the component
    /// will attempt to auto-find GameObjects named "Waypoint_A", "Waypoint_B", etc.
    /// </summary>
    public class GuardPatrol : MonoBehaviour
    {
        [Header("Waypoints")]
        [Tooltip("Assign patrol waypoints in the Inspector. If left empty, auto-searches scene for Waypoint_A/B/C/...")]
        [SerializeField] private Transform[] _waypoints;

        [Header("Patrol Behavior")]
        [Tooltip("How long the guard pauses at each waypoint.")]
        [SerializeField] private float _waitTimeAtWaypoint = 2f;

        [Tooltip("If true, guard ping-pongs (A→B→C→B→A). If false, loops (A→B→C→A).")]
        [SerializeField] private bool _pingPong = false;

        private int   _currentWaypointIndex = 0;
        private float _waitTimer;
        private bool  _isWaiting;
        private int   _direction = 1; // 1 = forward, -1 = reverse (ping-pong)

        private void Awake()
        {
            // Auto-find waypoints when not assigned in Inspector
            if (_waypoints == null || _waypoints.Length == 0)
            {
                AutoFindWaypoints();
            }
        }

        /// <summary>
        /// Searches the scene for sequentially named waypoint objects (Waypoint_A, Waypoint_B, …).
        /// Falls back to any GameObject tagged "Waypoint".
        /// </summary>
        private void AutoFindWaypoints()
        {
            var found = new List<Transform>();

            // Try lettered naming convention: Waypoint_A through Waypoint_Z
            for (char c = 'A'; c <= 'Z'; c++)
            {
                GameObject go = GameObject.Find($"Waypoint_{c}");
                if (go != null)
                    found.Add(go.transform);
            }

            // Also try numbered convention: Waypoint_0 through Waypoint_19
            if (found.Count == 0)
            {
                for (int i = 0; i < 20; i++)
                {
                    GameObject go = GameObject.Find($"Waypoint_{i}");
                    if (go != null)
                        found.Add(go.transform);
                }
            }

            if (found.Count > 0)
            {
                _waypoints = found.ToArray();
                Debug.Log($"[GuardPatrol] Auto-found {_waypoints.Length} waypoints for {gameObject.name}.");
            }
            else
            {
                Debug.LogWarning($"[GuardPatrol] No waypoints found for {gameObject.name}. " +
                                 "Guard will stand still. Assign waypoints in the Inspector or name them Waypoint_A, Waypoint_B, etc.");
            }
        }

        /// <summary>
        /// Called every frame by GuardStateMachine while in Patrol state.
        /// </summary>
        public void UpdatePatrol(NavMeshAgent agent)
        {
            if (_waypoints == null || _waypoints.Length == 0) return;

            if (_isWaiting)
            {
                _waitTimer -= Time.deltaTime;
                if (_waitTimer <= 0f)
                {
                    _isWaiting = false;
                    AdvanceWaypoint();
                    SetDestination(agent);
                }
                return;
            }

            // Check if we've reached the current waypoint
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                _isWaiting = true;
                _waitTimer = _waitTimeAtWaypoint;
            }
        }

        /// <summary>
        /// Resume patrol from the current waypoint after returning from another state.
        /// </summary>
        public void ResumePatrol()
        {
            _isWaiting = false;
        }

        /// <summary>
        /// Set the NavMeshAgent destination to the current waypoint.
        /// </summary>
        public void SetDestination(NavMeshAgent agent)
        {
            if (_waypoints == null || _waypoints.Length == 0) return;
            if (_waypoints[_currentWaypointIndex] == null) return;

            agent.SetDestination(_waypoints[_currentWaypointIndex].position);
        }

        private void AdvanceWaypoint()
        {
            if (_pingPong)
            {
                _currentWaypointIndex += _direction;

                if (_currentWaypointIndex >= _waypoints.Length - 1)
                    _direction = -1;
                else if (_currentWaypointIndex <= 0)
                    _direction = 1;
            }
            else
            {
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
            }
        }

        /// <summary>
        /// Draw waypoint path gizmos in the editor.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (_waypoints == null || _waypoints.Length < 2) return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < _waypoints.Length - 1; i++)
            {
                if (_waypoints[i] != null && _waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(_waypoints[i].position, _waypoints[i + 1].position);
                    Gizmos.DrawSphere(_waypoints[i].position, 0.3f);
                }
            }

            if (_waypoints[_waypoints.Length - 1] != null)
            {
                Gizmos.DrawSphere(_waypoints[_waypoints.Length - 1].position, 0.3f);

                if (!_pingPong && _waypoints[0] != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(_waypoints[_waypoints.Length - 1].position, _waypoints[0].position);
                }
            }
        }
    }
}
