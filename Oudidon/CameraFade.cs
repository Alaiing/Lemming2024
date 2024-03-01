using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oudidon
{
    public static class CameraFade
    {
        private static Color _startingColor;
        private static Color _targetColor;
        private static Color _currentColor;
        public static Color Color => _currentColor;
        private static float _time;
        private static float _duration;
        private static bool _isFading;
        public static bool IsFading => _isFading;
        private static Action _onFadeDone;

        public static void FadeToBlack(float duration, Action OnFadeDone = null)
        {
            FadeTo(Color.Black, duration, OnFadeDone);
        }

        public static void FadeFromBlack(float duration, Action OnFadeDone = null)
        {
            FadeFrom(Color.Black, duration, OnFadeDone);
        }

        public static void FadeTo(Color targetColor, float duration, Action OnFadeDone = null)
        {
            Fade(Color.White, targetColor, duration, OnFadeDone);
        }

        public static void FadeFrom(Color startingColor, float duration, Action OnFadeDone = null)
        {
            Fade(startingColor, Color.White, duration, OnFadeDone);
        }

        public static void Fade(Color startingColor, Color targetColor, float duration, Action OnFadeDone = null)
        {
            _startingColor = startingColor;
            _targetColor = targetColor;
            _time = 0;
            _duration = duration;
            _isFading = true;
            _onFadeDone = OnFadeDone;
        }

        public static void Update(float deltaTime)
        {
            if (!_isFading)
                return;

            _currentColor = Color.Lerp(_startingColor, _targetColor, _time / _duration);
            _time += deltaTime;
            if (_time >= _duration) 
            {
                _currentColor = _targetColor;
                _isFading = false;
                Action action = _onFadeDone;
                _onFadeDone = null;
                action?.Invoke();
            }
        }
    }
}
