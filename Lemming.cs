using Microsoft.Xna.Framework;
using Oudidon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lemmings2024
{
    public class Lemming : Character
    {
        public const string EVENT_DIE = "LemmingDie";
        public const string EVENT_EXPLODE = "LemmingExplode";
        public const string EVENT_START_DIG_LINE = "LemmingStartDigLine";
        public const string EVENT_DIG_LINE = "LemmingDigLine";
        public const string EVENT_SAVED = "LemmingSaved";

        public const string STATE_WALK = "Walk";
        public const string STATE_FALL = "Fall";
        public const string STATE_DIE_FALL = "DieFall";
        public const string STATE_EXPLODE = "Explode";
        public const string STATE_DIG_DOWN = "DigDown";
        public const string STATE_SAVED = "Saved";

        public const string ANIMATION_WALK = "Walk";
        public const string ANIMATION_FALL = "Fall";
        public const string ANIMATION_DIE_FALL = "DieFall";
        public const string ANIMATION_OHNO = "Ohno";
        public const string ANIMATION_EXPLODE = "Explode";
        public const string ANIMATION_DIG_DOWN = "DigDown";
        public const string ANIMATION_SAVED = "Saved";

        public const int WALL_HEIGHT = 5;
        public const int DEATH_HEIGHT = 60;

        public const float BASE_SPEED = 15;

        private Color[] _currentLevelData;
        private SimpleStateMachine _simpleStateMachine;

        private Character _countDown;

        public Lemming(SpriteSheet spriteSheet, Game game) : base(spriteSheet, game)
        {
            InitStateMachine();

            SetBaseSpeed(BASE_SPEED);

            SpriteSheet countDownSheet = new SpriteSheet(game.Content, "countdown", 5, 5, new Point(3, 5));
            countDownSheet.RegisterAnimation("Countdown", 0, 4, 1f);
            _countDown = new Character(countDownSheet, game);
            _countDown.SetAnimation("Countdown", onAnimationEnd: OnCountDownDone);
            _countDown.Deactivate();
            game.Components.Add(_countDown);
        }

        public void InitStateMachine()
        {
            _simpleStateMachine = new SimpleStateMachine();
            _simpleStateMachine.AddState(STATE_WALK, OnEnter: WalkEnter, OnUpdate: WalkUpdate);
            _simpleStateMachine.AddState(STATE_FALL, OnEnter: FallEnter, OnUpdate: FallUpdate);
            _simpleStateMachine.AddState(STATE_DIE_FALL, OnEnter: DieFallEnter);
            _simpleStateMachine.AddState(STATE_EXPLODE, OnEnter: ExplodeEnter, OnUpdate: ExplodeUpdate);
            _simpleStateMachine.AddState(STATE_DIG_DOWN, OnEnter: DigDownEnter, OnExit: DigDownExit);
            _simpleStateMachine.AddState(STATE_SAVED, OnEnter: SavedEnter);

            _simpleStateMachine.SetState(STATE_WALK);
        }

        public override Rectangle GetBounds()
        {
            return new Rectangle(PixelPositionX - 5, PixelPositionY - SpriteSheet.TopMargin, SpriteSheet.FrameWidth / 2, SpriteSheet.FrameHeight / 2);
        }

        public void SetState(string state)
        {
            _simpleStateMachine.SetState(state);
        }

        public void SetDirection(int direction)
        {
            SetScale(new Vector2(direction, 1));
            MoveDirection = new Vector2(direction, 0);
        }

        public void SetCurrentLevel(Color[] currentLevelData)
        {
            _currentLevelData = currentLevelData;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (_countDown.Enabled)
            {
                _countDown.MoveTo(Position + new Vector2(0, -10));
            }
            _simpleStateMachine.Update(gameTime);
            //_currentLevelData[IndexInLevelData(PixelPositionX, PixelPositionY)] = Color.Red;
        }

        public static int IndexInLevelData(int x, int y)
        {
            return y * Lemmings2024.PLAYGROUND_WIDTH + x;
        }

        public void DigDown()
        {
            SetState(STATE_DIG_DOWN);
        }

        public void Explode()
        {
            _countDown.Activate();
            _countDown.SetFrame(0);
        }

        public void Save()
        {
            SetState(STATE_SAVED);
        }

        private void OnCountDownDone()
        {
            SetState(STATE_EXPLODE);
        }

        public void Kill()
        {
            Game.Components.Remove(_countDown);
            EventsManager.FireEvent(EVENT_DIE, this);
        }

        public void Destroy()
        {
            EventsManager.FireEvent(EVENT_EXPLODE, this);
            Kill();
        }

        private bool IsWalkable(Color color)
        {
            return color.A > 0;
        }

        #region States
        private void WalkEnter()
        {
            SetAnimation(ANIMATION_WALK);
            SetSpeedMultiplier(1f);
            MoveDirection = new Vector2(CurrentScale.X, 0);
        }

        private void WalkUpdate(GameTime time, float arg2)
        {
            Color groundColor;

            int offsetX = PixelPositionX;
            int indexInLevelTexture = IndexInLevelData(offsetX, PixelPositionY);
            groundColor = _currentLevelData[indexInLevelTexture];
            if (IsWalkable(groundColor))
            {
                int positionChange = FindUpwardPosition(offsetX, PixelPositionY);
                if (positionChange <= WALL_HEIGHT)
                {
                    MoveBy(new Vector2(0, -positionChange));
                }
                else
                {
                    TurnAround();
                }
            }
            else
            {
                int positionChange = FindDownwardPosition(offsetX, PixelPositionY);
                if (positionChange < WALL_HEIGHT)
                {
                    MoveBy(new Vector2(0, positionChange));
                }
                else
                {
                    SetState(ANIMATION_FALL);
                }
            }
        }

        private void TurnAround()
        {
            MoveDirection = -MoveDirection;
            SetScale(new Vector2(-CurrentScale.X, CurrentScale.Y));
            MoveBy(new Vector2(MoveDirection.X, 0));
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            //SpriteBatch.DrawRectangle(Position, Vector2.Zero, Color.Red);
        }

        private int FindUpwardPosition(int x, int y)
        {
            int indexAtPosition = IndexInLevelData(x, y - 1);
            int delta = 0;
            while (indexAtPosition >= 0 && IsWalkable(_currentLevelData[indexAtPosition]) && delta <= WALL_HEIGHT + 1)
            {
                delta++;
                indexAtPosition -= Lemmings2024.PLAYGROUND_WIDTH;
            }

            return delta;
        }

        private int FindDownwardPosition(int x, int y)
        {
            int indexAtPosition = IndexInLevelData(x, y);
            int delta = 0;
            while (indexAtPosition < _currentLevelData.Length && !IsWalkable(_currentLevelData[indexAtPosition]) && delta <= WALL_HEIGHT)
            {
                delta++;
                indexAtPosition += Lemmings2024.PLAYGROUND_WIDTH;
            }
            if (indexAtPosition > _currentLevelData.Length)
            {
                return WALL_HEIGHT + 1;
            }
            return delta;
        }


        private int _fallStartHeight;
        private void FallEnter()
        {
            SetAnimation(ANIMATION_FALL);
            SetSpeedMultiplier(2f);
            MoveDirection = new Vector2(0, 1);
            _fallStartHeight = PixelPositionY;
        }

        private void FallUpdate(GameTime time, float arg2)
        {
            Fall(onLand: () => SetState(STATE_WALK), onLandDie: () => SetState(STATE_DIE_FALL));
        }

        private void Fall(Action onLand, Action onLandDie)
        {
            int indexInLevelTexture = IndexInLevelData(PixelPositionX, PixelPositionY);
            if (PixelPositionY > Lemmings2024.PLAYGROUND_HEIGHT + 5)
            {
                Kill();
                return;
            }

            if (indexInLevelTexture >= _currentLevelData.Length)
            {
                return;
            }
            Color groundColor = _currentLevelData[indexInLevelTexture];
            if (groundColor.A > 0)
            {
                if (PixelPositionY - _fallStartHeight < DEATH_HEIGHT)
                {
                    onLand?.Invoke();
                }
                else
                {
                    onLandDie?.Invoke();
                }
            }
        }

        private void DieFallEnter()
        {
            SetSpeedMultiplier(0);
            SetAnimation(ANIMATION_DIE_FALL, onAnimationEnd: Kill);
        }

        private void ExplodeEnter()
        {
            // TODO: add countdown
            _countDown.Deactivate();
            SetSpeedMultiplier(0f);
            SetAnimation(ANIMATION_OHNO, onAnimationEnd:
                () =>
                {
                    DrawOrder = 99;
                    SetAnimation(ANIMATION_EXPLODE, onAnimationEnd: Destroy);
                });
        }

        private void ExplodeUpdate(GameTime gameTime, float stateTime)
        {
            WalkUpdate(gameTime, stateTime);
        }

        private Vector2 _scaleOnDigDown;
        private void DigDownEnter()
        {
            SetAnimation(ANIMATION_DIG_DOWN, onAnimationFrame: DigLine);
            SetSpeedMultiplier(0f);
            _scaleOnDigDown = CurrentScale;
            SetScale(new Vector2(1, 1));
            EventsManager.FireEvent(EVENT_START_DIG_LINE, this);
        }

        private void DigLine(int frameIndex)
        {
            if (frameIndex == _spriteSheet.GetAnimationFrameCount(ANIMATION_DIG_DOWN) - 1)
            {
                MoveBy(new Vector2(0, 1));
                EventsManager.FireEvent(EVENT_DIG_LINE, this);

                if (PixelPositionY >= Lemmings2024.PLAYGROUND_HEIGHT - 1)
                {
                    SetState(STATE_WALK);
                    return;
                }

                bool canDig = false;
                int endPosition = Math.Min(Lemmings2024.PLAYGROUND_WIDTH, PixelPositionX + Lemmings2024.LINE_DIG_LENGTH);
                int length = endPosition - PixelPositionX;
                int startIndex = (PixelPositionY + 1) * Lemmings2024.PLAYGROUND_WIDTH + PixelPositionX;
                for (int i = 0; i < length; i++)
                {
                    if (_currentLevelData[startIndex + i].A > 0)
                    {
                        canDig = true;
                        break;
                    }
                }

                if (!canDig)
                {
                    SetState(STATE_WALK);
                }
            }
            else if (frameIndex == 0)
            {
                SetScale(new Vector2(-CurrentScale.X, CurrentScale.Y));
                if (CurrentScale.X < 0)
                {
                    SetPivotOffset(new Point(-3, 0));
                }
                else
                {
                    ResetPivotOffset();
                }
            }
        }

        private void DigDownExit()
        {
            SetScale(_scaleOnDigDown);
        }

        private void SavedEnter()
        {
            SetSpeedMultiplier(0f);
            SetAnimation(ANIMATION_SAVED, onAnimationEnd: () => EventsManager.FireEvent(EVENT_SAVED, this));
        }
        #endregion
    }
}
