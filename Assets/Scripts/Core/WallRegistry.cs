using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using EchoThief.Sonar;

namespace EchoThief.Core
{
    [DisallowMultipleComponent]
    public class WallRegistry : MonoBehaviour
    {
        private static readonly List<ReflectionSolver.WallFace> _wallFaces = new List<ReflectionSolver.WallFace>();
        private static bool _initialized;

        public static IReadOnlyList<ReflectionSolver.WallFace> WallFaces
        {
            get
            {
                EnsureInitialized();
                return _wallFaces;
            }
        }

        [SerializeField]
        private bool _scanOnAwake = true;

        [SerializeField]
        private float _minimumFaceLength = 0.2f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeOnLoad()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _initialized = false;
            _wallFaces.Clear();
            BuildRegistryInternal(0.2f);
        }

        private void Awake()
        {
            if (_scanOnAwake)
            {
                BuildRegistry();
            }
        }

        public void BuildRegistry()
        {
            BuildRegistryInternal(_minimumFaceLength);
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            BuildRegistryInternal(0.2f);
        }

        private static void BuildRegistryInternal(float minimumFaceLength)
        {
            _wallFaces.Clear();
            var boxes = Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var box in boxes)
            {
                if (!box.enabled || box.isTrigger)
                {
                    continue;
                }

                RegisterBoxCollider(box, minimumFaceLength);
            }

            _initialized = true;
        }

        private static void RegisterBoxCollider(BoxCollider box, float minimumFaceLength)
        {
            Vector3 halfExtents = box.size * 0.5f;

            Vector3[] localCorners = new Vector3[4]
            {
                new Vector3(-halfExtents.x, 0f, -halfExtents.z),
                new Vector3(-halfExtents.x, 0f, halfExtents.z),
                new Vector3(halfExtents.x, 0f, halfExtents.z),
                new Vector3(halfExtents.x, 0f, -halfExtents.z)
            };

            Vector2[] corners = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 worldCorner = box.transform.TransformPoint(box.center + localCorners[i]);
                corners[i] = new Vector2(worldCorner.x, worldCorner.z);
            }

            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                Vector2 a = corners[i];
                Vector2 b = corners[next];
                Vector2 edge = b - a;
                if (edge.magnitude < minimumFaceLength)
                {
                    continue;
                }

                Vector2 normal = new Vector2(edge.y, -edge.x).normalized;
                _wallFaces.Add(new ReflectionSolver.WallFace(a, b, normal));
            }
        }
    }
}
