using System.Collections.Generic;
using UnityEngine;

namespace EchoThief.Sonar
{
    public static class ReflectionSolver
    {
        public struct WallFace
        {
            public Vector2 A;
            public Vector2 B;
            public Vector2 Normal;

            public WallFace(Vector2 a, Vector2 b, Vector2 normal)
            {
                A = a;
                B = b;
                Normal = normal.normalized;
            }
        }

        public struct SoundArc
        {
            public Vector2 Center;
            public float Radius;
            public float StartAngle;
            public float EndAngle;
            public float Fade;
            public Color Color;

            public SoundArc(Vector2 center, float radius, float startAngle, float endAngle, float fade, Color color)
            {
                Center = center;
                Radius = radius;
                StartAngle = NormalizeAngle(startAngle);
                EndAngle = NormalizeAngle(endAngle);
                Fade = fade;
                Color = color;
            }
        }

        private struct ArcInterval
        {
            public float Start;
            public float End;

            public ArcInterval(float start, float end)
            {
                Start = NormalizeAngle(start);
                End = NormalizeAngle(end);
            }

            public ArcInterval(float start, float end, bool normalize)
            {
                Start = normalize ? NormalizeAngle(start) : start;
                End = normalize ? NormalizeAngle(end) : end;
            }

            public float Length
            {
                get
                {
                    float delta = End - Start;
                    if (delta < 0f) delta += TwoPi;
                    return delta;
                }
            }

            public bool IsEmpty => Mathf.Approximately(Length, 0f);

            public bool Wraps => End < Start;
        }

        private const float TwoPi = 6.28318530718f;
        private const float OcclusionEpsilon = 0.05f;
        private const float RangeMargin = 0.1f;

        public static void SolveWave(
            SoundWave wave,
            float previousRadius,
            IReadOnlyList<WallFace> wallFaces,
            float reflectionCoefficient,
            float loudnessThreshold,
            int maxReflections,
            List<SoundArc> outArcs,
            List<SoundWave> outReflections)
        {
            if (wave.Fade < loudnessThreshold)
            {
                return;
            }

            var visibleIntervals = new List<ArcInterval> { new ArcInterval(wave.ArcStart, wave.ArcEnd) };

            if (wallFaces != null)
            {
                foreach (var wall in wallFaces)
                {
                    if (!TryComputeWallOcclusion(wave, wall, out ArcInterval occlusion))
                    {
                        continue;
                    }

                    visibleIntervals = SubtractIntervals(visibleIntervals, occlusion);

                    if (wave.Depth < maxReflections && TryCreateReflection(wave, previousRadius, wall, reflectionCoefficient, loudnessThreshold, out SoundWave reflection))
                    {
                        outReflections.Add(reflection);
                    }
                }
            }

            foreach (var interval in visibleIntervals)
            {
                if (interval.IsEmpty)
                {
                    continue;
                }

                outArcs.Add(new SoundArc(wave.Origin, wave.Radius, interval.Start, interval.End, wave.Fade, wave.Color));
            }
        }

        private static bool TryComputeWallOcclusion(SoundWave wave, WallFace wall, out ArcInterval occlusion)
        {
            occlusion = default;

            Vector2 sourceToWall = wave.Origin - wall.A;
            if (Vector2.Dot(sourceToWall, wall.Normal) < 0f)
            {
                return false;
            }

            Vector2 direction = wall.B - wall.A;
            float length = direction.magnitude;
            if (length < 0.001f)
            {
                return false;
            }

            float crossDistance = Mathf.Abs(Cross(direction.normalized, wave.Origin - wall.A));
            if (crossDistance < OcclusionEpsilon)
            {
                return false;
            }

            if (DistancePointToSegment(wave.Origin, wall.A, wall.B) > wave.Radius + RangeMargin)
            {
                return false;
            }

            float angleA = Mathf.Atan2(wall.A.y - wave.Origin.y, wall.A.x - wave.Origin.x);
            float angleB = Mathf.Atan2(wall.B.y - wave.Origin.y, wall.B.x - wave.Origin.x);
            occlusion = SmallestSignedInterval(angleA, angleB);
            return occlusion.Length > 0.001f;
        }

        private static bool TryCreateReflection(
            SoundWave wave,
            float previousRadius,
            WallFace wall,
            float reflectionCoefficient,
            float loudnessThreshold,
            out SoundWave reflectionWave)
        {
            reflectionWave = default;

            float wallDistance = DistancePointToSegment(wave.Origin, wall.A, wall.B);
            if (previousRadius >= wallDistance || wave.Radius < wallDistance - 0.001f)
            {
                return false;
            }

            float reflectedLoudness = wave.InitialLoudness * reflectionCoefficient;
            if (reflectedLoudness < loudnessThreshold)
            {
                return false;
            }

            Vector2 reflectedOrigin = ReflectPointAcrossLine(wave.Origin, wall.A, wall.B);
            float angleA = Mathf.Atan2(wall.A.y - reflectedOrigin.y, wall.A.x - reflectedOrigin.x);
            float angleB = Mathf.Atan2(wall.B.y - reflectedOrigin.y, wall.B.x - reflectedOrigin.x);
            var arcInterval = SmallestSignedInterval(angleA, angleB);
            if (arcInterval.IsEmpty)
            {
                return false;
            }

            reflectionWave = new SoundWave(
                reflectedOrigin,
                wave.Radius,
                wave.Radius,
                wave.MaxRadius,
                reflectedLoudness,
                wave.Depth + 1,
                arcInterval.Start,
                arcInterval.End,
                wave.Color);

            return true;
        }

        private static ArcInterval SmallestSignedInterval(float a, float b)
        {
            float delta = SignedAngleDifference(a, b);
            if (delta >= 0f)
            {
                return new ArcInterval(a, a + delta);
            }

            return new ArcInterval(b, b - delta);
        }

        private static float SignedAngleDifference(float from, float to)
        {
            float delta = to - from;
            if (delta > Mathf.PI)
            {
                delta -= TwoPi;
            }
            else if (delta < -Mathf.PI)
            {
                delta += TwoPi;
            }
            return delta;
        }

        private static List<ArcInterval> SubtractIntervals(List<ArcInterval> sources, ArcInterval block)
        {
            if (block.IsEmpty)
            {
                return sources;
            }

            var result = new List<ArcInterval>(sources.Count);
            foreach (var source in sources)
            {
                foreach (var piece in SplitIfWrapped(source))
                {
                    foreach (var negativePiece in SplitIfWrapped(block))
                    {
                        AddSubtraction(result, piece, negativePiece);
                    }
                }
            }

            return result;
        }

        private static IEnumerable<ArcInterval> SplitIfWrapped(ArcInterval interval)
        {
            if (!interval.Wraps)
            {
                yield return interval;
                yield break;
            }

            yield return new ArcInterval(interval.Start, TwoPi, false);
            yield return new ArcInterval(0f, interval.End, false);
        }

        private static void AddSubtraction(List<ArcInterval> result, ArcInterval source, ArcInterval block)
        {
            if (block.End <= source.Start || block.Start >= source.End)
            {
                result.Add(source);
                return;
            }

            if (block.Start > source.Start)
            {
                result.Add(new ArcInterval(source.Start, block.Start));
            }

            if (block.End < source.End)
            {
                result.Add(new ArcInterval(block.End, source.End));
            }
        }

        private static float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float abSquared = Vector2.Dot(ab, ab);
            if (abSquared <= 0f)
            {
                return Vector2.Distance(point, a);
            }

            float t = Vector2.Dot(point - a, ab) / abSquared;
            t = Mathf.Clamp01(t);
            Vector2 projection = a + ab * t;
            return Vector2.Distance(point, projection);
        }

        private static Vector2 ReflectPointAcrossLine(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = (b - a).normalized;
            Vector2 ap = point - a;
            float projection = Vector2.Dot(ap, ab);
            Vector2 closest = a + ab * projection;
            Vector2 offset = closest - point;
            return point + 2f * offset;
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private static float NormalizeAngle(float angle)
        {
            float wrapped = angle % TwoPi;
            if (wrapped < 0f)
            {
                wrapped += TwoPi;
            }
            return wrapped;
        }
    }
}
