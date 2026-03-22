using UnityEngine;

namespace Jis.LoadingSystems
{
    public sealed class ProgressSmoother
    {
        private readonly float _speed;

        public ProgressSmoother(float speed)
        {
            _speed = Mathf.Max(0.01f, speed);
        }

        public float Next(float current, float target, float deltaTime)
        {
            var t = 1f - Mathf.Exp(-_speed * Mathf.Max(0.0001f, deltaTime));
            return Mathf.Lerp(current, target, t);
        }
    }
}
