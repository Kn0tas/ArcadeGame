using System.Collections.Generic;
using UnityEngine;
using EchoThief.Core;

namespace EchoThief.Sonar
{
    /// <summary>
    /// Manages active sonar waves, resolves wall occlusion/reflection, and pushes arc data to the shader.
    /// </summary>
    public class SonarManager : MonoBehaviour
    {
        public static SonarManager Instance { get; private set; }

        [Header("Wave Settings")]
        [Tooltip("How fast the sonar wave expands (units/sec).")]
        [SerializeField] private float _defaultExpansionSpeed = 12f;

        [Tooltip("Width of the visible sonar ring band.")]
        [SerializeField] private float _defaultArcThickness = 1.5f;

        [Tooltip("Sound energy retention for reflections.")]
        [SerializeField] [Range(0.1f, 1f)] private float _reflectionCoefficient = 0.65f;

        [Tooltip("Minimum loudness at which sonar arcs remain visible.")]
        [SerializeField] private float _loudnessThreshold = 0.02f;

        [Tooltip("Maximum number of reflection bounces.")]
        [SerializeField] private int _maxReflections = 1;

        [Header("Limits")]
        [Tooltip("Maximum simultaneous sonar waves kept in memory.")]
        [SerializeField] private int _maxWaves = 32;

        [Tooltip("Maximum arcs that can be uploaded to the shader.")]
        [SerializeField] private int _maxArcs = 64;

        private readonly List<SoundWave> _activeWaves = new List<SoundWave>();
        private readonly List<ReflectionSolver.SoundArc> _activeArcs = new List<ReflectionSolver.SoundArc>();
        private readonly List<SoundWave> _newReflections = new List<SoundWave>();

        private static readonly int ArcCountId = Shader.PropertyToID("_SonarArcCount");
        private static readonly int ArcOriginsId = Shader.PropertyToID("_SonarArcOrigins");
        private static readonly int ArcRadiiId = Shader.PropertyToID("_SonarArcRadii");
        private static readonly int ArcAnglesId = Shader.PropertyToID("_SonarArcAngles");
        private static readonly int ArcFadesId = Shader.PropertyToID("_SonarArcFades");
        private static readonly int ArcColorsId = Shader.PropertyToID("_SonarArcColors");
        private static readonly int ArcThicknessId = Shader.PropertyToID("_SonarArcThickness");

        private Vector4[] _arcOrigins;
        private float[] _arcRadii;
        private Vector4[] _arcAngles;
        private float[] _arcFades;
        private Vector4[] _arcColors;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _maxArcs = Mathf.Max(1, _maxArcs);
            _maxWaves = Mathf.Max(1, _maxWaves);

            _arcOrigins = new Vector4[_maxArcs];
            _arcRadii = new float[_maxArcs];
            _arcAngles = new Vector4[_maxArcs];
            _arcFades = new float[_maxArcs];
            _arcColors = new Vector4[_maxArcs];
        }

        private void OnEnable()
        {
            NoiseEventBus.OnNoise += HandleNoiseEvent;
        }

        private void OnDisable()
        {
            NoiseEventBus.OnNoise -= HandleNoiseEvent;
        }

        private void Update()
        {
            _activeArcs.Clear();
            _newReflections.Clear();

            for (int i = _activeWaves.Count - 1; i >= 0; i--)
            {
                var wave = _activeWaves[i];
                float previousRadius = wave.Radius;
                wave.Advance(Time.deltaTime, _defaultExpansionSpeed);

                if (!wave.IsAlive(_loudnessThreshold))
                {
                    _activeWaves.RemoveAt(i);
                    continue;
                }

                ReflectionSolver.SolveWave(
                    wave,
                    previousRadius,
                    WallRegistry.WallFaces,
                    _reflectionCoefficient,
                    _loudnessThreshold,
                    _maxReflections,
                    _activeArcs,
                    _newReflections);

                _activeWaves[i] = wave;
            }

            foreach (var reflection in _newReflections)
            {
                if (_activeWaves.Count >= _maxWaves)
                {
                    _activeWaves.RemoveAt(0);
                }

                _activeWaves.Add(reflection);
            }

            PushToShader();
        }

        private void HandleNoiseEvent(NoiseEvent noise)
        {
            SpawnWave(noise.Origin, noise.SonarRadius, noise.SonarColor, noise.Loudness);
        }

        public void SpawnWave(Vector3 origin, float maxRadius, Color color, float loudness = 1f)
        {
            if (_activeWaves.Count >= _maxWaves)
            {
                _activeWaves.RemoveAt(0);
            }

            var wave = new SoundWave(
                new Vector2(origin.x, origin.z),
                0f,
                0f,
                maxRadius,
                Mathf.Clamp01(loudness),
                0,
                0f,
                Mathf.PI * 2f,
                color);

            _activeWaves.Add(wave);
        }

        private void PushToShader()
        {
            int count = Mathf.Min(_activeArcs.Count, _maxArcs);

            for (int i = 0; i < count; i++)
            {
                var arc = _activeArcs[i];
                _arcOrigins[i] = new Vector4(arc.Center.x, 0f, arc.Center.y, 0f);
                _arcRadii[i] = arc.Radius;
                _arcAngles[i] = new Vector4(arc.StartAngle, arc.EndAngle, 0f, 0f);
                _arcFades[i] = arc.Fade;
                _arcColors[i] = new Vector4(arc.Color.r, arc.Color.g, arc.Color.b, arc.Color.a);
            }

            for (int i = count; i < _maxArcs; i++)
            {
                _arcOrigins[i] = Vector4.zero;
                _arcRadii[i] = 0f;
                _arcAngles[i] = Vector4.zero;
                _arcFades[i] = 0f;
                _arcColors[i] = Vector4.zero;
            }

            Shader.SetGlobalInt(ArcCountId, count);
            Shader.SetGlobalVectorArray(ArcOriginsId, _arcOrigins);
            Shader.SetGlobalFloatArray(ArcRadiiId, _arcRadii);
            Shader.SetGlobalVectorArray(ArcAnglesId, _arcAngles);
            Shader.SetGlobalFloatArray(ArcFadesId, _arcFades);
            Shader.SetGlobalVectorArray(ArcColorsId, _arcColors);
            Shader.SetGlobalFloat(ArcThicknessId, _defaultArcThickness);
        }
    }
}
