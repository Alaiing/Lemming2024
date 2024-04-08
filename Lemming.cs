using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Oudidon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public const string EVENT_BASH = "LemmingBash";
        public const string EVENT_MINE = "LemmingMine";
        public const string EVENT_SAVED = "LemmingSaved";

        public const string STATE_WALK = "Walk";
        public const string STATE_FALL = "Fall";
        public const string STATE_DIE_FALL = "DieFall";
        public const string STATE_DIE_DROWN = "DieDrown";
        public const string STATE_EXPLODE = "Explode";
        public const string STATE_DIG_DOWN = "DigDown";
        public const string STATE_STOP = "Stop";
        public const string STATE_CLIMB = "Climb";
        public const string STATE_END_CLIMB = "EndClimb";
        public const string STATE_FLOATER_OPEN = "FloaterOpen";
        public const string STATE_FLOATER = "Floater";
        public const string STATE_BUILDER = "Builder";
        public const string STATE_BASHER = "Basher";
        public const string STATE_MINER = "Miner";
        public const string STATE_SHRUG = "Shrug";
        public const string STATE_SAVED = "Saved";

        public const string ANIMATION_WALK = "Walk";
        public const string ANIMATION_FALL = "Fall";
        public const string ANIMATION_DIE_FALL = "DieFall";
        public const string ANIMATION_DIE_DROWN = "DieDrown";
        public const string ANIMATION_OHNO = "Ohno";
        public const string ANIMATION_EXPLODE = "Explode";
        public const string ANIMATION_DIG_DOWN = "DigDown";
        public const string ANIMATION_STOP = "Stop";
        public const string ANIMATION_CLIMB = "Climb";
        public const string ANIMATION_END_CLIMB = "EndClimb";
        public const string ANIMATION_FLOATER_OPEN = "FloaterOpen";
        public const string ANIMATION_FLOATER = "Floater";
        public const string ANIMATION_BUILDER = "Builder";
        public const string ANIMATION_BASHER = "Basher";
        public const string ANIMATION_MINER = "Miner";
        public const string ANIMATION_SHRUG = "Shrug";
        public const string ANIMATION_SAVED = "Saved";

        public const int WALL_HEIGHT = 5;
        public const int DEATH_HEIGHT = 60;
        public const int FLOATER_HEIGHT = 20;
        public const int BLOCKER_DISTANCE = 6;
        public const int CLIMBER_TEST = 7;
        public const int BUILD_WIDTH = 6;

        public const float BASE_SPEED = 50f / 3f;
        public const float FALL_SPEED = 2f;
        private const float WATER_DROWN_HEIGHT = 10;

        private Level _currentLevel;
        private SimpleStateMachine _simpleStateMachine;

        private Character _countDown;

        public bool IsFalling => _simpleStateMachine.CurrentState == STATE_FALL;
        public bool IsBlocker => _simpleStateMachine.CurrentState == STATE_STOP;
        public bool IsBuilder => _simpleStateMachine.CurrentState == STATE_BUILDER;
        public bool IsBasher => _simpleStateMachine.CurrentState == STATE_BASHER;
        public bool IsMiner => _simpleStateMachine.CurrentState == STATE_MINER;
        public bool IsSaved => _simpleStateMachine.CurrentState == STATE_SAVED;

        public bool IsClimber { get; set; }
        public bool IsFloater { get; set; }

        private bool _inWater;

        private static SoundEffect _popSound;
        private static SoundEffectInstance _popSoundInstance;

        private static SoundEffect _splotchSound;
        private static SoundEffectInstance _splotchSoundInstance;
        private static SoundEffect _ploufSound;
        private static SoundEffectInstance _ploufSoundInstance;
        private static SoundEffect _glouglouSound;
        private static SoundEffectInstance _glouglouSoundInstance;
        private static SoundEffect _lemmingDieSound;
        private static SoundEffectInstance _lemmingDieSoundInstance;
        private static SoundEffect _buildEndSound;
        private static SoundEffectInstance _buildEndSoundInstance;

        private string _typeName;
        public string TypeName => _typeName;

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

            if (_popSound == null)
            {
                _popSound = game.Content.Load<SoundEffect>("pop");
                _popSoundInstance = _popSound.CreateInstance();
            }

            if (_splotchSound == null)
            {
                _splotchSound = game.Content.Load<SoundEffect>("zboui");
                _splotchSoundInstance = _splotchSound.CreateInstance();
            }

            if (_lemmingDieSound == null)
            {
                _lemmingDieSound = game.Content.Load<SoundEffect>("aaah");
                _lemmingDieSoundInstance = _lemmingDieSound.CreateInstance();
            }

            if (_buildEndSound == null)
            {
                _buildEndSound = game.Content.Load<SoundEffect>("tuk");
                _buildEndSoundInstance = _buildEndSound.CreateInstance();
            }

            if (_ploufSound == null)
            {
                _ploufSound = game.Content.Load<SoundEffect>("plouf");
                _ploufSoundInstance = _ploufSound.CreateInstance();
            }
            if (_glouglouSound == null)
            {
                _glouglouSound = game.Content.Load<SoundEffect>("glouglou");
                _glouglouSoundInstance = _glouglouSound.CreateInstance();
            }

            _inWater = false;
        }

        public void InitStateMachine()
        {
            _simpleStateMachine = new SimpleStateMachine();
            _simpleStateMachine.AddState(STATE_WALK, OnEnter: WalkEnter, OnUpdate: WalkUpdate);
            _simpleStateMachine.AddState(STATE_FALL, OnEnter: FallEnter, OnUpdate: FallUpdate);
            _simpleStateMachine.AddState(STATE_DIE_FALL, OnEnter: DieFallEnter);
            _simpleStateMachine.AddState(STATE_DIE_DROWN, OnEnter: DieDrownEnter);
            _simpleStateMachine.AddState(STATE_EXPLODE, OnEnter: ExplodeEnter, OnUpdate: ExplodeUpdate);
            _simpleStateMachine.AddState(STATE_DIG_DOWN, OnEnter: DigDownEnter, OnExit: DigDownExit);
            _simpleStateMachine.AddState(STATE_STOP, OnEnter: StopEnter, OnUpdate: StopUpdate, OnExit: StopExit);
            _simpleStateMachine.AddState(STATE_CLIMB, OnEnter: ClimbEnter, OnUpdate: ClimbUpdate);
            _simpleStateMachine.AddState(STATE_END_CLIMB, OnEnter: EndClimbEnter);
            _simpleStateMachine.AddState(STATE_FLOATER_OPEN, OnEnter: FloaterOpenEnter);
            _simpleStateMachine.AddState(STATE_FLOATER, OnEnter: FloaterEnter, OnUpdate: FloaterUpdate);
            _simpleStateMachine.AddState(STATE_BUILDER, OnEnter: BuilderEnter, OnUpdate: BuilderUpdate);
            _simpleStateMachine.AddState(STATE_BASHER, OnEnter: BasherEnter, OnUpdate: BasherUpdate);
            _simpleStateMachine.AddState(STATE_MINER, OnEnter: MinerEnter);
            _simpleStateMachine.AddState(STATE_SHRUG, OnEnter: ShrugEnter);
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

        public void SetCurrentLevel(Level currentLevel)
        {
            _currentLevel = currentLevel;
            SetLayerColor(_currentLevel.DirtColor, 1);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (_countDown.Enabled)
            {
                _countDown.MoveTo(Position + new Vector2(0, -10));
            }
            _simpleStateMachine.Update(gameTime);
        }

        public void Walk()
        {
            SetState(STATE_WALK);
        }

        public bool IsDigging => _simpleStateMachine.CurrentState == STATE_DIG_DOWN;
        public bool DigDown()
        {
            if (IsDigging || IsFalling || IsBlocker || !CanDigDown())
                return false;

            SetState(STATE_DIG_DOWN);
            return true;
        }

        public bool Stop()
        {
            if (IsFalling || IsBlocker)
                return false;

            SetState(STATE_STOP);

            return true;
        }

        public bool Climb()
        {
            if (IsBlocker || IsClimber)
                return false;
            IsClimber = true;

            return true;
        }

        public bool Float()
        {
            if (IsBlocker || IsFloater)
                return false;
            IsFloater = true;

            return true;
        }

        public bool Build()
        {
            if (IsBlocker || IsFalling || IsBuilder)
                return false;

            SetState(STATE_BUILDER);
            return true;
        }

        public bool Bash()
        {
            if (IsBlocker || IsFalling || IsBasher)
                return false;

            SetState(STATE_BASHER);
            return true;
        }

        public bool Mine()
        {
            if (IsBlocker || IsFalling || IsMiner)
                return false;

            SetState(STATE_MINER);
            return true;
        }

        private bool _isExploding;
        public bool IsExploding => _isExploding;
        public bool Explode(float delay = 0f)
        {
            if (_isExploding)
                return false;

            DelayAction(() =>
            {
                _isExploding = true;
                _countDown.Activate();
                _countDown.SetFrame(0);
            }, delay);

            return true;
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
            ExplosionParticles(Position);
            EventsManager.FireEvent(EVENT_EXPLODE, this);
            Kill();
        }

        public static void ExplosionParticles(Vector2 position)
        {
            for (int i = 0; i < 20; i++)
            {
                Vector2 velocity = new Vector2((CommonRandom.Random.NextSingle() * 2 - 1) / 2f, -CommonRandom.Random.NextSingle() * 1.5f);
                Color color = new Color(CommonRandom.Random.Next(0, 256), CommonRandom.Random.Next(0, 256), CommonRandom.Random.Next(0, 256), 255);
                Particles.SpawnParticle(color, position, velocity * 20, 2f, useGravity: true, timeScale: 5f);
            }
        }

        private bool IsOnWalkableGround()
        {
            return IsOnWalkableGround(Point.Zero);
        }

        private bool IsOnWalkableGround(Point offset)
        {
            int indexInLevelTexture = _currentLevel.IndexInLevelData(PixelPositionX + offset.X, PixelPositionY + offset.Y);
            if (indexInLevelTexture >= _currentLevel.MaskTextureData.Length)
                return false;

            Color groundColor = _currentLevel.MaskTextureData[indexInLevelTexture];
            return groundColor.IsWalkable();
        }

        #region States
        private void WalkEnter()
        {
            SetAnimation(ANIMATION_WALK);
            SetSpeedMultiplier(1f);
            MoveDirection = new Vector2(CurrentScale.X, 0);
            _typeName = IsFloater ? "FLOATER" : IsClimber ? "CLIMBER" : "WALKER";
        }

        private void WalkUpdate(GameTime time, float arg2)
        {
            UpdateWalk();
        }

        private void UpdateWalk()
        {
            if (IsOnWalkableGround(new Point((int)CurrentScale.X, 0)) || IsOnWalkableGround(new Point((int)CurrentScale.X, 1)))
            {
                int positionChange = FindUpwardPosition(PixelPositionX, PixelPositionY);
                if (positionChange <= WALL_HEIGHT)
                {
                    MoveBy(new Vector2(0, -positionChange));
                }
                else
                {
                    if (IsClimber && CanClimb())
                    {
                        SetState(STATE_CLIMB);
                    }
                    else
                    {
                        TurnAround();
                    }
                }
            }
            else
            {
                int positionChange = FindDownwardPosition(PixelPositionX, PixelPositionY);
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
            DebugDraw();
        }

        private int FindUpwardPosition(int x, int y)
        {
            int indexAtPosition = _currentLevel.IndexInLevelData(x, y - 1);
            int delta = 0;
            while (indexAtPosition >= 0 && _currentLevel.MaskTextureData[indexAtPosition].IsWalkable() && delta <= WALL_HEIGHT + 1)
            {
                delta++;
                indexAtPosition -= _currentLevel.Width;
            }

            return delta;
        }

        private int FindDownwardPosition(int x, int y)
        {
            int indexAtPosition = _currentLevel.IndexInLevelData(x, y);
            int delta = 0;
            while (indexAtPosition < _currentLevel.MaskTextureData.Length && !_currentLevel.MaskTextureData[indexAtPosition].IsWalkable() && delta <= WALL_HEIGHT)
            {
                delta++;
                indexAtPosition += _currentLevel.Width;
            }
            if (indexAtPosition > _currentLevel.MaskTextureData.Length)
            {
                return WALL_HEIGHT + 1;
            }
            return delta;
        }


        public static List<Lemming> blockers = new List<Lemming>();
        public bool TestBlocker()
        {
            foreach (Lemming blocker in blockers)
            {
                if (Math.Abs(blocker.PixelPositionY - PixelPositionY) > 10)
                {
                    continue;
                }

                int deltaX = blocker.PixelPositionX - PixelPositionX;
                if (Math.Abs(deltaX) < BLOCKER_DISTANCE && Math.Sign(deltaX) == Math.Sign(MoveDirection.X))
                {
                    TurnAround();
                }
            }

            return false;
        }

        private int _fallStartHeight;
        private void FallEnter()
        {
            SetAnimation(ANIMATION_FALL);
            SetSpeedMultiplier(FALL_SPEED);
            MoveDirection = new Vector2(0, 1);
            _fallStartHeight = PixelPositionY;
            _typeName = IsFloater ? "FLOATER" : "FALLER";
        }

        private void FallUpdate(GameTime time, float arg2)
        {
            Fall(onLand: () => SetState(STATE_WALK), onLandDie: () => SetState(STATE_DIE_FALL));
        }

        private void Fall(Action onLand, Action onLandDie)
        {
            int indexInLevelTexture = _currentLevel.IndexInLevelData(PixelPositionX, PixelPositionY);

            if (_inWater && PixelPositionY > _currentLevel.Height - WATER_DROWN_HEIGHT)
            {
                SetState(STATE_DIE_DROWN);
                return;
            }

            if (PixelPositionY > _currentLevel.Height + 5)
            {
                _lemmingDieSoundInstance.Stop();
                _lemmingDieSoundInstance.Play();
                Kill();
                return;
            }

            if (indexInLevelTexture >= _currentLevel.MaskTextureData.Length)
            {
                return;
            }

            if (PixelPositionY - _fallStartHeight >= FLOATER_HEIGHT && IsFloater && _simpleStateMachine.CurrentState != STATE_FLOATER)
            {
                SetState(STATE_FLOATER_OPEN);
                return;
            }


            Color groundColor = _currentLevel.MaskTextureData[indexInLevelTexture];
            if (groundColor.IsWalkable())
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
            else if (groundColor.IsWater())
            {
                _inWater = true;
            }
        }

        private void DieFallEnter()
        {
            SetSpeedMultiplier(0);
            SetAnimation(ANIMATION_DIE_FALL, onAnimationEnd: Kill);
            _splotchSoundInstance.Play();
        }

        private void DieDrownEnter()
        {
            MoveDirection = new Vector2(CurrentScale.X, 0);
            SetAnimation(ANIMATION_DIE_DROWN, onAnimationEnd: Kill, onAnimationFrame: OnDieDrownFrame);
            _ploufSoundInstance?.Stop();
            _ploufSoundInstance?.Play();
        }

        private void OnDieDrownFrame(int frameIndex)
        {
            if (frameIndex == 11)
            {
                _glouglouSoundInstance?.Stop();
                _glouglouSoundInstance?.Play();
            }
        }

        private void ExplodeEnter()
        {
            _countDown.Deactivate();
            SetSpeedMultiplier(0f);
            SetAnimation(ANIMATION_OHNO, onAnimationEnd:
                () =>
                {
                    DrawOrder = 99;
                    SetAnimation(ANIMATION_EXPLODE, onAnimationEnd: Destroy);
                    _popSoundInstance.Stop();
                    _popSoundInstance.Play();
                });
            _typeName = "BOMBER";
        }

        private void ExplodeUpdate(GameTime gameTime, float stateTime)
        {
            if (PixelPositionY >= _currentLevel.Texture.Height)
            {
                return;
            }

            Color groundColor;

            int offsetX = PixelPositionX;
            int indexInLevelTexture = _currentLevel.IndexInLevelData(offsetX, PixelPositionY);
            groundColor = _currentLevel.MaskTextureData[indexInLevelTexture];
            if (!groundColor.IsWalkable())
            {
                MoveBy(new Vector2(0, FALL_SPEED * _baseSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds));
            }
        }

        private Vector2 _scaleOnDigDown;
        private void DigDownEnter()
        {
            SetAnimation(ANIMATION_DIG_DOWN, onAnimationFrame: DigLine);
            SetSpeedMultiplier(0f);
            _scaleOnDigDown = CurrentScale;
            SetScale(new Vector2(1, 1));
            EventsManager.FireEvent(EVENT_START_DIG_LINE, this);
            _typeName = "DIGGER";

        }

        private void DigLine(int frameIndex)
        {
            if (frameIndex == _spriteSheet.GetAnimationFrameCount(ANIMATION_DIG_DOWN) - 1)
            {
                if (PixelPositionY >= Lemmings2024.PLAYGROUND_HEIGHT - 1)
                {
                    SetState(STATE_WALK);
                    return;
                }

                bool canDig = CanDigDown();

                if (!canDig)
                {
                    SetState(STATE_WALK);
                }
                else
                {
                    MoveBy(new Vector2(0, 1));
                    EventsManager.FireEvent(EVENT_DIG_LINE, this);
                }
            }
            else if (frameIndex == 0)
            {
                SetScale(new Vector2(-CurrentScale.X, CurrentScale.Y));
            }
        }

        private bool CanDigDown()
        {
            int endPosition = Math.Min(_currentLevel.Width, PixelPositionX + Lemmings2024.LINE_DIG_LENGTH);
            int length = endPosition - PixelPositionX;
            int startIndex = PixelPositionY * _currentLevel.Width + PixelPositionX;
            for (int i = 0; i < length; i++)
            {
                if (_currentLevel.MaskTextureData[startIndex + i].IsWalkable())
                {
                    if (_currentLevel.MaskTextureData[startIndex + i].R == 0
                    && _currentLevel.MaskTextureData[startIndex + i].G == 0
                    && _currentLevel.MaskTextureData[startIndex + i].B == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        private void DigDownExit()
        {
            SetScale(_scaleOnDigDown);
        }

        private void StopEnter()
        {
            SetAnimation(ANIMATION_STOP);
            SetSpeedMultiplier(0f);
            blockers.Add(this);
            _typeName = "BLOCKER";

        }

        private void StopExit()
        {
            blockers.Remove(this);
        }

        private void StopUpdate(GameTime gameTime, float stateTime)
        {
            int indexAtPosition = _currentLevel.IndexInLevelData(PixelPositionX, PixelPositionY + 1);
            // TODO: test more pixels
            if (indexAtPosition < _currentLevel.MaskTextureData.Length && !_currentLevel.MaskTextureData[indexAtPosition].IsWalkable())
            {
                SetState(STATE_WALK);
            }

        }

        private void ClimbEnter()
        {
            SetAnimation(ANIMATION_CLIMB, onAnimationFrame: OnClimbFrame);
            SetSpeedMultiplier(0f);
            _typeName = "CLIMBER";
        }

        private void OnClimbFrame(int frameIndex)
        {
            if (frameIndex >= 4)
            {
                MoveBy(new Vector2(0, -1));
            }
        }

        private void ClimbUpdate(GameTime gameTime, float stateTime)
        {
            if (!CanClimb())
            {
                TurnAround();
                SetState(STATE_FALL);
                return;
            }

            if (!_currentLevel.GetMaskPixel(new Point(PixelPositionX, PixelPositionY - CLIMBER_TEST)).IsWalkable())
            {
                SetState(STATE_END_CLIMB);
            }
        }

        private bool CanClimb()
        {
            for (int i = 1; i < 5; i++)
            {
                if (_currentLevel.GetMaskPixel(new Point(PixelPositionX - i * (int)_currentScale.X, PixelPositionY - 10)).IsWalkable())
                {
                    return false;
                }
            }

            return true;
        }

        private void EndClimbEnter()
        {
            SetAnimation(ANIMATION_END_CLIMB,
                onAnimationFrame: (index) =>
                {
                    if (index > 0 && index < 4)
                        MoveBy(new Vector2(0, -2));
                },
                onAnimationEnd: () =>
                {
                    SetState(STATE_WALK);
                }); ;
            SetSpeedMultiplier(0f);
        }

        private void SavedEnter()
        {
            SetSpeedMultiplier(0f);
            SetAnimation(ANIMATION_SAVED, onAnimationEnd: () => EventsManager.FireEvent(EVENT_SAVED, this));
        }

        private void FloaterEnter()
        {
            SetAnimation(ANIMATION_FLOATER);
            SetSpeedMultiplier(FALL_SPEED);
            _typeName = "FLOATER";
        }

        private void FloaterUpdate(GameTime time, float arg2)
        {
            Fall(onLand: () => SetState(STATE_WALK), onLandDie: () => SetState(STATE_WALK));
        }

        private void FloaterOpenEnter()
        {
            SetSpeedMultiplier(0);
            SetAnimation(ANIMATION_FLOATER_OPEN, onAnimationEnd: () => SetState(STATE_FLOATER));
        }

        private int _buildCount;

        private void BuilderEnter()
        {
            _buildCount = 0;
            SetAnimation(ANIMATION_BUILDER, onAnimationFrame: OnBuildFrame, onAnimationEnd: OnBuildEnd);
            SetSpeedMultiplier(0);
            _typeName = "BUILDER";

        }

        private void OnBuildEnd()
        {
            MoveBy(new Vector2(CurrentScale.X * 2, -1));
            if (_buildCount >= 12)
            {
                SetState(STATE_SHRUG);
            }
        }

        private void OnBuildFrame(int frameIndex)
        {
            if (frameIndex == 9)
            {
                _currentLevel.Build(new Point(PixelPositionX + (CurrentScale.X < 0 ? -BUILD_WIDTH : 0), PixelPositionY - 1), BUILD_WIDTH);

                _buildCount++;
                if (_buildCount > 9)
                {
                    _buildEndSoundInstance.Play();
                }
            }
        }

        private void BuilderUpdate(GameTime gameTime, float stateTime)
        {
            Color groundColor;

            int offsetX = PixelPositionX + Math.Sign(_currentScale.X) * BUILD_WIDTH / 2;
            int indexInLevelTexture = _currentLevel.IndexInLevelData(offsetX, PixelPositionY);
            groundColor = _currentLevel.MaskTextureData[indexInLevelTexture];
            if (groundColor.IsWalkable())
            {
                int positionChange = FindUpwardPosition(offsetX, PixelPositionY);
                if (positionChange > WALL_HEIGHT)
                {

                    SetState(STATE_SHRUG);
                    TurnAround();
                    return;
                }
            }
            if (!CanBuild())
            {
                SetState(STATE_SHRUG);
                TurnAround();
            }
        }

        private bool CanBuild()
        {
            for (int i = -1; i < 2; i++)
            {
                if (_currentLevel.GetMaskPixel(new Point(PixelPositionX - i * (int)_currentScale.X, PixelPositionY - 10)).IsWalkable())
                {
                    return false;
                }
            }

            return true;
        }


        private void BasherEnter()
        {
            SetAnimation(ANIMATION_BASHER, onAnimationFrame: OnBasherFrame);
            SetSpeedMultiplier(0);
            _typeName = "BASHER";
        }


        private void OnBasherFrame(int frameIndex)
        {
            if (frameIndex >= 11 && frameIndex <= 15 || frameIndex >= 27)
            {
                MoveBy(new Vector2(_currentScale.X, 0));
            }

            if (frameIndex == 3 || frameIndex == 19)
            {
                EventsManager.FireEvent(EVENT_BASH, this);
            }
        }

        private void BasherUpdate(GameTime gameTime, float stateTime)
        {
            if (!IsOnWalkableGround())
            {
                SetState(STATE_FALL);
            }
        }

        private void MinerEnter()
        {
            SetAnimation(ANIMATION_MINER, onAnimationFrame: OnMinerFrame, onAnimationEnd: OnMinerEnd);
            SetSpeedMultiplier(0f);
            MoveBy(new Vector2(2, 1));
            _typeName = "MINER";
        }

        private void OnMinerEnd()
        {
            MoveBy(new Vector2(0, 1));
            if (!IsOnWalkableGround())
            {
                SetState(STATE_FALL);
            }
        }

        private void OnMinerFrame(int frameIndex)
        {
            if (frameIndex == 2)
            {
                EventsManager.FireEvent(EVENT_MINE, this);
            }

            if (frameIndex == 15)
            {
                MoveBy(new Vector2(2 * _currentScale.X, 0));
            }
        }

        private void ShrugEnter()
        {
            SetSpeedMultiplier(0f);
            SetAnimation(ANIMATION_SHRUG, onAnimationEnd: () => SetState(STATE_WALK));
        }

        #endregion

        private void DebugDraw()
        {
            //int offset = _currentScale.X > 0 ? 0 : SpriteSheet.FrameWidth - SpriteSheet.DefaultPivot.X * 2 - 1;
            //SpriteBatch.DrawRectangle(new Vector2(PixelPositionX + offset, PixelPositionY - 7), Vector2.Zero, Color.Red);

            //SpriteBatch.DrawRectangle(new Vector2(PixelPositionX + 2, PixelPositionY-10), new Vector2(4, 9), Color.Yellow);
        }
    }
}
