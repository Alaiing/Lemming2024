using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Oudidon
{
    public class Character : OudidonGameComponent
    {
        public string name;
        protected SpriteSheet _spriteSheet;
        public SpriteSheet SpriteSheet => _spriteSheet;
        protected Vector2 _position;
        public Vector2 Position => _position;
        private Point _pivotOffset;
        public int PixelPositionX => (int)MathF.Floor(_position.X);
        public int PixelPositionY => (int)MathF.Floor(_position.Y);
        private float _currentFrame;
        public virtual int CurrentFrame => (int)MathF.Floor(_currentFrame);
        protected Color[] _colors;
        public Color[] Colors => _colors;
        protected Color _mainColor;
        public Color Color => _mainColor;
        private Color[] _finalColors;
        public Vector2 MoveDirection;
        protected float _currentRotation;
        public float CurrentRotation => _currentRotation;
        protected Vector2 _currentScale;
        public Vector2 CurrentScale => _currentScale;
        protected float _baseSpeed;
        protected float _speedMultiplier;
        public float CurrentSpeed => _baseSpeed * _speedMultiplier;
        protected float _animationSpeedMultiplier;
        private string _currentAnimationName;
        private SpriteSheet.Animation _currentAnimation;

        private Action _onAnimationEnd;
        protected Action<int> _onAnimationFrame;

        public Character(SpriteSheet spriteSheet, Game game) : base(game)
        {
            _spriteSheet = spriteSheet;
            _colors = new Color[_spriteSheet.LayerCount];
            _finalColors = new Color[_colors.Length];
            Reset();
            Enabled = true;
            Visible = true;
        }

        public virtual Rectangle GetBounds()
        {
            return new Rectangle(PixelPositionX - SpriteSheet.LeftMargin, PixelPositionY - SpriteSheet.TopMargin, SpriteSheet.FrameWidth, SpriteSheet.FrameHeight);
        }

        public void Activate()
        {
            Enabled = true;
            Visible = true;
        }

        public void Deactivate()
        {
            Enabled = false;
            Visible = false;
        }

        public virtual void Reset()
        {
            _currentScale = Vector2.One;
            _currentFrame = 0;
            ResetColors();
            LookTo(new Vector2(1, 0));
            MoveDirection = Vector2.Zero;
            _animationSpeedMultiplier = 1f;
            _speedMultiplier = 1f;
        }

        public virtual void ResetColors()
        {
            for (int i = 0; i < _colors.Length; i++)
            {
                _colors[i] = Color.White;
            }
            _mainColor = Color.White;
            UpdateFinalColors();
        }

        public void SetMainColor(Color color)
        {
            _mainColor = color;
            UpdateFinalColors();
        }

        public void SetLayerColor(Color color, int layer)
        {
            _colors[layer] = color;
            UpdateFinalColors();
        }

        public void SetColors(Color[] colors)
        {
            _colors = colors;
            UpdateFinalColors();
        }

        protected void UpdateFinalColors()
        {
            for (int i = 0; i < _colors.Length; i++)
            {
                _finalColors[i] = new Color(_colors[i].ToVector4() * _mainColor.ToVector4());
            }
        }

        public void SetBaseSpeed(float speed)
        {
            _baseSpeed = speed;
        }

        public void SetSpeedMultiplier(float speedMutilplier)
        {
            _speedMultiplier = speedMutilplier;
        }

        public void SetAnimationSpeedMultiplier(float animationSpeedMultiplier)
        {
            _animationSpeedMultiplier = animationSpeedMultiplier;
        }

        public void SetFrame(int frameIndex)
        {
            if (!string.IsNullOrEmpty(_currentAnimationName) && frameIndex > 0 && frameIndex < _currentAnimation.FrameCount)
            {
                _currentFrame = frameIndex;
            }
        }

        public void SetAnimation(string animationName, Action onAnimationEnd = null, Action<int> onAnimationFrame = null)
        {
            if (_currentAnimationName != animationName)
            {
                if (_spriteSheet.TryGetAnimation(animationName, out _currentAnimation))
                {
                    _currentAnimationName = animationName;

                    _currentFrame = 0;

                    _onAnimationEnd = onAnimationEnd;
                    _onAnimationFrame = onAnimationFrame;
                }
                else
                {
                    _currentAnimationName = null;
                    _onAnimationEnd = null;
                    _onAnimationFrame = null;
                }
            }
        }

        public void SetScale(Vector2 scale)
        {
            _currentScale = scale;
        }

        public void MoveTo(Vector2 position)
        {
            _position = position;
        }

        public void MoveBy(Vector2 translation)
        {
            _position += translation;
        }

        public void LookTo(Vector2 direction)
        {
            direction.Normalize();
            _currentRotation = MathF.Sign(direction.Y) * MathF.Acos(direction.X);
        }

        public void SetPivotOffset(Point offset)
        {
            _pivotOffset = offset;
        }

        public void ResetPivotOffset()
        {
            _pivotOffset = Point.Zero;
        }

        public virtual void Move(float deltaTime)
        {
            _position += MoveDirection * CurrentSpeed * deltaTime;
        }

        public void Animate(float deltaTime)
        {
            if (string.IsNullOrEmpty(_currentAnimationName))
                return;

            int previousFrame = (int)MathF.Floor(_currentFrame);
            _currentFrame += deltaTime * _currentAnimation.speed * _animationSpeedMultiplier;
            if (_currentFrame > _currentAnimation.FrameCount)
            {
                _currentFrame = 0;
                if (_onAnimationEnd != null)
                {
                    _onAnimationEnd?.Invoke();
                }
            }
            int newFrame = (int)MathF.Floor(_currentFrame);
            if (previousFrame != newFrame)
            {
                _onAnimationFrame?.Invoke(newFrame);
            }
        }

        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Animate(deltaTime);
            Move(deltaTime);
        }

        public override void Draw(GameTime gameTime)
        {
            if (string.IsNullOrEmpty(_currentAnimationName))
                return;
            _spriteSheet.DrawAnimationFrame(_currentAnimationName, CurrentFrame, SpriteBatch, new Vector2(PixelPositionX + _pivotOffset.X, PixelPositionY + _pivotOffset.Y), _currentRotation, _currentScale, _finalColors);
        }

        public Color GetPixel(int x, int y, int layer = 0)
        {
            if (string.IsNullOrEmpty(_currentAnimationName))
                return new Color(0, 0, 0, 0);

            int scaledX;
            if (_currentScale.X < 0)
                scaledX = SpriteSheet.FrameWidth - x - 1;
            else
                scaledX = x;
            scaledX = (int)MathF.Floor(scaledX * MathF.Abs(_currentScale.X));

            int scaledY;
            if (_currentScale.Y < 0)
                scaledY = SpriteSheet.FrameHeight - y - 1;
            else
                scaledY = y;
            scaledY = (int)MathF.Floor(scaledY * MathF.Abs(_currentScale.Y));

            // TODO: take rotation into account

            return SpriteSheet.GetPixel(_spriteSheet.GetAbsoluteFrameIndex(_currentAnimationName, CurrentFrame), scaledX, scaledY, layer);
        }
    }
}
