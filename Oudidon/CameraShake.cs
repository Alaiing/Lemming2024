using Microsoft.Xna.Framework;
using System;

namespace Oudidon
{
    public static class CameraShake
    {
        public static bool Enabled;
        private static bool _isScreenShaking;
        private static float _shakeTime;
        private static float _shakeIntensity;
        private static float _shakeAmplitude;
        private static float _shakeDuration;
        private static Vector2 _shakeDirection;
        private static Vector2 _shakeOffset = Vector2.Zero;
        public static Vector2 ShakeOffset => _shakeOffset;

        public static void Shake(Vector2 direction, float amplitude, float intensity, float duration)
        {
            if (!_isScreenShaking && Enabled)
            {
                _shakeTime = 0;
                _isScreenShaking = true;
                _shakeIntensity = intensity;
                _shakeDuration = duration;
                _shakeAmplitude = amplitude;
                _shakeDirection = direction; // TODO: normalize components
            }
        }

        public static void Update(float deltaTime)
        {
            if (_isScreenShaking && Enabled)
            {
                _shakeTime += deltaTime;
                float intensityTime = _shakeTime * _shakeIntensity;
                if (_shakeDuration < 0 || _shakeTime < _shakeDuration)
                {
                    _shakeOffset = new Vector2(MathF.Cos(intensityTime) * _shakeDirection.X, MathF.Sin(intensityTime) * _shakeDirection.Y) * _shakeAmplitude;
                }
                else
                {
                    _isScreenShaking = false;
                    _shakeOffset = Vector2.Zero;
                }
            }
        }

        public static void StopShake()
        {
            _isScreenShaking = false;
            _shakeOffset = Vector2.Zero;
        }

    }
}
