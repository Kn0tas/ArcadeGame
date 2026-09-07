using UnityEngine;

namespace EchoThief.Sonar
{
    [System.Serializable]
    public struct SoundWave
    {
        public Vector2 Origin;
        public float Radius;
        public float InitialRadius;
        public float MaxRadius;
        public float InitialLoudness;
        public int Depth;
        public float ArcStart;
        public float ArcEnd;
        public Color Color;

        public SoundWave(Vector2 origin, float currentRadius, float initialRadius, float maxRadius, float initialLoudness, int depth, float arcStart, float arcEnd, Color color)
        {
            Origin = origin;
            Radius = currentRadius;
            InitialRadius = initialRadius;
            MaxRadius = maxRadius;
            InitialLoudness = initialLoudness;
            Depth = depth;
            ArcStart = arcStart;
            ArcEnd = arcEnd;
            Color = color;
        }

        public float Fade
        {
            get
            {
                float totalTravel = Mathf.Max(MaxRadius - InitialRadius, 0.001f);
                float travelled = Mathf.Clamp(Radius - InitialRadius, 0f, totalTravel);
                return Mathf.Clamp01(1f - travelled / totalTravel) * InitialLoudness;
            }
        }

        public float CurrentLoudness => Fade;

        public bool IsAlive(float loudnessThreshold)
        {
            return Radius < MaxRadius && Fade >= loudnessThreshold;
        }

        public void Advance(float deltaTime, float expansionSpeed)
        {
            Radius = Mathf.Min(Radius + expansionSpeed * deltaTime, MaxRadius);
        }
    }
}
