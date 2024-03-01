using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Oudidon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lemmings2024
{
    public class Lemmings2024 : OudidonGame
    {
        public const int PLAYGROUND_WIDTH = 1600;
        public const int PLAYGROUND_HEIGHT = 160;
        public const float FADE_DURATION = 0.5f;

        private const string STATE_MENU = "Menu";
        private const string STATE_PRE_GAME = "Pre-Game";
        private const string STATE_GAME = "Game";
        private const string STATE_POST_GAME = "Post-Game";

        public const int LINE_DIG_LENGTH = 9;
        public const float RATE_INCREASE_SPEED = 20f;

        public enum ClickType { Pressed, Clicked, DoubleClicked }

        private RenderTarget2D _gameRender;

        private SpriteSheet _lemmingSprite;
        private List<Lemming> _lemmings = new();

        private MouseCursor _mouseCursorPoint;
        private MouseCursor _mouseCursorSelect;

        private Texture2D _explodeTexture;
        private Color[] _explodeTextureData;

        private Texture2D _digTexture;
        private Color[] _digTextureData;

        private Texture2D _preGameBackground;
        private Texture2D _postGameBackground;

        private Texture2D _hudTexture;
        private Texture2D _hudSelectTexture;
        private Texture2D _radarFrameTexture;
        Rectangle _radarRect;
        float _radarRatio;

        private SpriteSheet _digits;

        private float _spawnCount;
        private float _savedLemmings;

        private Character[] _hatches;
        private Character _exit;

        private int _currentLemmingAction;
        private (Action<int>, ClickType)[] _hudActions;
        private Func<Lemming[], bool>[] _lemmingActions;
        private int[] _availableActions = new int[8];

        private Level _currentLevel;
        private float _spawnTimer = 0;
        private float _scrollOffset;
        private float ScrollOffset
        {
            get => _scrollOffset;
            set
            {
                _scrollOffset = Math.Clamp(value, 0, PLAYGROUND_WIDTH - ScreenWidth / 2);
            }
        }

        private SoundEffect _hatchSound;
        private SoundEffectInstance _hatchSoundInstance;
        private SoundEffect _letsgoSound;
        private SoundEffectInstance _letsgoSoundInstance;
        private SoundEffect _actionSelectSound;
        private SoundEffectInstance _actionSelectSoundInstance;
        private SoundEffect _lemmingSelectSound;
        private SoundEffectInstance _lemmingSelectSoundInstance;
        private SoundEffect _lemmingSavedSound;
        private SoundEffectInstance _lemmingSavedSoundInstance;
        private SoundEffect _lemmingExplodeSound;
        private SoundEffectInstance _lemmingExplodeSoundInstance;

        private SoundEffect _lemmingLevel1Music;
        private SoundEffectInstance _lemmingLevel1MusicInstance;

        protected override void Initialize()
        {
            _gameRender = new RenderTarget2D(GraphicsDevice, PLAYGROUND_WIDTH, PLAYGROUND_HEIGHT);

            EventsManager.ListenTo<Lemming>(Lemming.EVENT_DIE, OnLemmingDead);
            EventsManager.ListenTo<Lemming>(Lemming.EVENT_EXPLODE, OnLemmingExplode);
            EventsManager.ListenTo<Lemming>(Lemming.EVENT_START_DIG_LINE, OnLemmingStartDigLine);
            EventsManager.ListenTo<Lemming>(Lemming.EVENT_DIG_LINE, OnLemmingDigLine);
            EventsManager.ListenTo<Lemming>(Lemming.EVENT_DIG_CHUNK, OnLemmingDigChunk);
            EventsManager.ListenTo<Lemming>(Lemming.EVENT_SAVED, OnLemmingSaved);

            _hudActions = new (Action<int>, ClickType)[]
                            {
                                (ActionModifySpawn, ClickType.Pressed),
                                (ActionModifySpawn, ClickType.Pressed),
                                (ActionLemming, ClickType.Clicked),
                                (ActionLemming, ClickType.Clicked),
                                (ActionLemming, ClickType.Clicked),
                                (ActionLemming, ClickType.Clicked),
                                (ActionLemming, ClickType.Clicked),
                                (ActionLemming, ClickType.Clicked),
                                (ActionLemming, ClickType.Clicked),
                                (ActionLemming, ClickType.Clicked),
                                (ActionPause, ClickType.Clicked),
                                (ActionArmageddon, ClickType.DoubleClicked)
                            };

            _lemmingActions = new Func<Lemming[], bool>[]
            {
                LemmingActionClimber,
                LemmingActionFloater,
                LemmingActionBomb,
                LemmingActionBlocker,
                LemmingActionBridgeBuilder,
                LemmingActionBasher,
                LemmingActionMiner,
                LemmingActionDigger
            };

            base.Initialize();

            _timeScale = 1f;
        }

        protected override void InitStateMachine()
        {
            base.InitStateMachine();
            AddSate(STATE_MENU);
            AddSate(STATE_PRE_GAME, onEnter: PreGameEnter, onUpdate: PreGameUpdate, onDraw: PreGameDraw);
            AddSate(STATE_GAME, onEnter: GameEnter, onUpdate: GameUpdate, onExit: GameExit, onDraw: GameDraw);
            AddSate(STATE_POST_GAME, onEnter: PostGameEnter, onUpdate: PostGameUpdate, onDraw: PostGameDraw);
        }

        protected override void LoadContent()
        {
            Texture2D mouseCursorTexture = Content.Load<Texture2D>("cursor_point");
            _mouseCursorPoint = CreateCursor(mouseCursorTexture);

            mouseCursorTexture = Content.Load<Texture2D>("cursor_select");
            _mouseCursorSelect = CreateCursor(mouseCursorTexture);

            _preGameBackground = Content.Load<Texture2D>("pre_game");
            _postGameBackground = Content.Load<Texture2D>("post_game");
            _hudTexture = Content.Load<Texture2D>("HUD");
            _hudSelectTexture = Content.Load<Texture2D>("HUD_select");
            _radarFrameTexture = Content.Load<Texture2D>("radar-frame");
            _digits = new SpriteSheet(Content, "digits", 8, 8, Point.Zero);
            _radarRect = new Rectangle(416, PLAYGROUND_HEIGHT + 16 + 2, 200, 20);
            _radarRatio = (float)PLAYGROUND_WIDTH / _radarRect.Width;

            _hatchSound = Content.Load<SoundEffect>("hatch-sound");
            _hatchSoundInstance = _hatchSound.CreateInstance();

            _letsgoSound = Content.Load<SoundEffect>("letsgo");
            _letsgoSoundInstance = _letsgoSound.CreateInstance();

            _actionSelectSound = Content.Load<SoundEffect>("tongon");
            _actionSelectSoundInstance = _actionSelectSound.CreateInstance();

            _lemmingSelectSound = Content.Load<SoundEffect>("chtonk");
            _lemmingSelectSoundInstance = _lemmingSelectSound.CreateInstance();

            _lemmingSavedSound = Content.Load<SoundEffect>("oing");
            _lemmingSavedSoundInstance = _lemmingSavedSound.CreateInstance();

            _lemmingExplodeSound = Content.Load<SoundEffect>("ohno");
            _lemmingExplodeSoundInstance = _lemmingExplodeSound.CreateInstance();

            _lemmingLevel1Music = Content.Load<SoundEffect>("lemmings-level1");
            _lemmingLevel1MusicInstance = _lemmingLevel1Music.CreateInstance();
            _lemmingLevel1MusicInstance.IsLooped = true;

            _lemmingSprite = new SpriteSheet(Content, "lemming", 20, 20, new Point(8, 10));
            _lemmingSprite.AddLayer(Content, "lemming-dirt");
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_WALK, 0, 7, Lemming.BASE_SPEED);
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_FALL, 32, 35, Lemming.BASE_SPEED, new Point(8, 11));
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_DIE_FALL, 176, 191, Lemming.BASE_SPEED);
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_OHNO, 160, 175, Lemming.BASE_SPEED);
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_EXPLODE, 221, 221, Lemming.BASE_SPEED);
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_DIG_DOWN, 128, 135, Lemming.BASE_SPEED, new Point(9, 11));
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_STOP, 48, 63, Lemming.BASE_SPEED, new Point(9, 11));
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_CLIMB, 64, 71, Lemming.BASE_SPEED, new Point(8, 12));
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_END_CLIMB, 72, 79, Lemming.BASE_SPEED, new Point(8, 11));
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_FLOATER_OPEN, 36, 39, Lemming.BASE_SPEED, new Point(8, 17));
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_FLOATER, 40, 43, Lemming.BASE_SPEED, new Point(8, 17), SpriteSheet.AnimationType.PingPong);
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_BUILDER, 80, 95, Lemming.BASE_SPEED, new Point(8, 13));
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_BASHER, 96, 127, Lemming.BASE_SPEED);
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_SHRUG, 9, 15, Lemming.BASE_SPEED);
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_SAVED, 19, 25, Lemming.BASE_SPEED);

            SpriteSheet hatch = new SpriteSheet(Content, "hatch", 41, 25, new Point(20, 0));
            hatch.RegisterAnimation("Closed", 0, 0, Lemming.BASE_SPEED);
            hatch.RegisterAnimation("Open", 0, 9, Lemming.BASE_SPEED);
            hatch.RegisterAnimation("Idle", 9, 9, Lemming.BASE_SPEED);
            _hatches = new Character[4];
            for (int i = 0; i < _hatches.Length; i++)
            {
                _hatches[i] = new Character(hatch, this);
                _hatches[i].SetAnimation("Idle");
                Components.Add(_hatches[i]);
                _hatches[i].Deactivate();
            }

            SpriteSheet exit = new SpriteSheet(Content, "exit1", 40, 40, new Point(18, 30));
            exit.RegisterAnimation("Idle", 0, 5, Lemming.BASE_SPEED);
            _exit = new Character(exit, this);
            _exit.SetAnimation("Idle");
            Components.Add(_exit);

            _explodeTexture = Content.Load<Texture2D>("circle_mask");
            _explodeTextureData = new Color[_explodeTexture.Width * _explodeTexture.Height];
            _explodeTexture.GetData(_explodeTextureData);

            _digTexture = Content.Load<Texture2D>("dig_mask");
            _digTextureData = new Color[_digTexture.Width * _digTexture.Height];
            _digTexture.GetData(_digTextureData);

            _currentLevel = new Level(Content, "level1.data");
            SetState(STATE_PRE_GAME);
        }

        private MouseCursor CreateCursor(Texture2D cursorTexture)
        {
            RenderTarget2D _mouseCursorPointScaled;
            _mouseCursorPointScaled = new RenderTarget2D(GraphicsDevice, cursorTexture.Width * ScreenScaleX, cursorTexture.Height * ScreenScaleY);
            GraphicsDevice.SetRenderTarget(_mouseCursorPointScaled);
            GraphicsDevice.Clear(Color.Transparent);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
            _spriteBatch.Draw(cursorTexture, new Rectangle(0, 0, _mouseCursorPointScaled.Width, _mouseCursorPointScaled.Height), Color.White);
            _spriteBatch.End();
            GraphicsDevice.SetRenderTarget(_renderTarget);

            return MouseCursor.FromTexture2D(_mouseCursorPointScaled, _mouseCursorPointScaled.Width / 2, _mouseCursorPointScaled.Height / 2);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime); // Updates state machine and components, in that order
        }

        protected override void DrawUI(GameTime gameTime)
        {
            // TODO: Draw your overlay UI here
        }

        private void OnLemmingDead(Lemming deadLemming)
        {
            RemoveLemming(deadLemming);
        }

        private void RemoveLemming(Lemming deadLemming)
        {
            _lemmings.Remove(deadLemming);
            Components.Remove(deadLemming);
            if (_lemmings.Count == 0)
                CameraFade.FadeToBlack(FADE_DURATION, OnFadeDone: () => SetState(STATE_POST_GAME));
        }

        private void OnLemmingSaved(Lemming savedLemming)
        {
            RemoveLemming(savedLemming);
            _savedLemmings++;
        }

        private void OnLemmingExplode(Lemming explodingLemming)
        {
            _currentLevel.Dig(_explodeTexture, _explodeTextureData, new Point(explodingLemming.PixelPositionX - _explodeTexture.Width / 2, explodingLemming.PixelPositionY - _explodeTexture.Height / 2));
        }

        private void OnLemmingStartDigLine(Lemming lemming)
        {
            _currentLevel.Dig(new Point(lemming.PixelPositionX - LINE_DIG_LENGTH / 2 - 1, lemming.PixelPositionY), LINE_DIG_LENGTH);
            _currentLevel.Dig(new Point(lemming.PixelPositionX - LINE_DIG_LENGTH / 2 - 1, lemming.PixelPositionY - 1), LINE_DIG_LENGTH);
            _currentLevel.Dig(new Point(lemming.PixelPositionX - LINE_DIG_LENGTH / 2 - 1, lemming.PixelPositionY - 2), LINE_DIG_LENGTH);
        }

        private void OnLemmingDigLine(Lemming lemming)
        {
            _currentLevel.Dig(new Point(lemming.PixelPositionX - LINE_DIG_LENGTH / 2, lemming.PixelPositionY), LINE_DIG_LENGTH);
        }

        private void OnLemmingDigChunk(Lemming lemming)
        {
            _currentLevel.Dig(_digTexture, _digTextureData, new Point(lemming.PixelPositionX - 1, lemming.PixelPositionY - _digTexture.Height));
        }

        private void ShowHatch(int index)
        {
            _hatches[index].Activate();
            _hatches[index].SetAnimation("Closed");
        }

        private void OpenHatch(int index, Action onHatchOpened)
        {
            _hatches[index].SetAnimation("Open", onAnimationEnd: () =>
            {
                _hatches[index].SetAnimation("Idle");
                onHatchOpened?.Invoke();
            });
        }

        #region States
        private void PreGameEnter()
        {
            _currentLevel.ReloadTexture();
            for (int i = 0; i < _hatches.Length; i++)
            {
                if (i < _currentLevel.HatchPositions.Length)
                {
                    _hatches[i].MoveTo(_currentLevel.HatchPositions[i].ToVector2());
                }
                else
                {
                    _hatches[i].Deactivate();
                }
            }

            CameraFade.FadeFrom(Color.Black, FADE_DURATION);
        }

        private void PreGameDraw(SpriteBatch batch, GameTime time)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_preGameBackground, Vector2.Zero, Color.White);
            _spriteBatch.Draw(_currentLevel.Texture, new Rectangle(0, 0, ScreenWidth, 46), Color.White);
            _spriteBatch.End();
        }

        private void PreGameUpdate(GameTime time, float arg2)
        {
            if (CameraFade.IsFading)
                return;
            SimpleControls.GetStates();
            if (SimpleControls.LeftMouseButtonPressedThisFrame())
            {
                CameraFade.FadeToBlack(FADE_DURATION, OnFadeDone: () => SetState(STATE_GAME));
            }
        }

        private void PostGameEnter()
        {
            CameraFade.FadeFromBlack(FADE_DURATION);
        }

        private void PostGameDraw(SpriteBatch batch, GameTime time)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_postGameBackground, Vector2.Zero, Color.White);
            _spriteBatch.End();
        }

        private void PostGameUpdate(GameTime time, float arg2)
        {
            if (CameraFade.IsFading)
                return;
            SimpleControls.GetStates();
            if (SimpleControls.LeftMouseButtonPressedThisFrame())
            {
                CameraFade.FadeToBlack(FADE_DURATION, OnFadeDone: () => SetState(STATE_PRE_GAME));
            }
        }

        private void GameEnter()
        {
            Mouse.SetCursor(_mouseCursorPoint);
            _spawnCount = 0;
            _spawnTimer = 0;
            for (int i = 0; i < _currentLevel.HatchPositions.Length; i++)
            {
                ShowHatch(i);
            }
            _currentSpawnRate = _currentLevel.LemmingRate;
            _currentLevel.AvailableActions.CopyTo(_availableActions, 0);
            _exit.MoveTo(_currentLevel.ExitPosition.ToVector2());
            ScrollOffset = _currentLevel.StartScrollOffset;

            CameraFade.FadeFromBlack(FADE_DURATION, OnFadeDone: StartLevel);
        }

        private void StartLevel()
        {
            for (int i = 0; i < _currentLevel.HatchPositions.Length; i++)
            {
                OpenHatch(i, onHatchOpened: (i == 0 ? OnHatchOpen : null));
            }
            _hatchSoundInstance.Play();
        }

        private void OnHatchOpen()
        {
            StartSpawn();
        }

        private void StartSpawn()
        {
            _isSpawning = true;
            _letsgoSoundInstance.Play();
        }

        private void GameExit()
        {
            // TODO: remove all lemmings
            _lemmingLevel1MusicInstance.Stop();
        }

        private void GameUpdate(GameTime gameTime, float stateTime)
        {
            SimpleControls.GetStates();

            Point mousePosition = SimpleControls.GetMousePosition();

            Point scaledMousePosition = new Point(mousePosition.X / 2 / ScreenScaleX, mousePosition.Y / ScreenScaleY);

            ApplyScrolling(gameTime, SimpleControls.IsRightMouseButtonDown(), scaledMousePosition);

            ApplyHUDAction(scaledMousePosition);

            ApplyLemmingAction(scaledMousePosition);

            SpawnLemmings(gameTime);

            TestBlockers();

            SaveLemmings();
        }

        private void TestBlockers()
        {
            foreach (Lemming lemming in _lemmings)
            {
                lemming.TestBlocker();
            }
        }

        private void SaveLemmings()
        {
            foreach (Lemming lemming in _lemmings)
            {
                if (!lemming.IsSaved && Vector2.Distance(lemming.Position, _exit.Position) < 3)
                {
                    lemming.MoveTo(_exit.Position + new Vector2(0, -1));
                    lemming.Save();
                    _lemmingSavedSoundInstance.Stop();
                    _lemmingSavedSoundInstance.Play();
                }
            }
        }

        private void SpawnLemming(int hatchIndex, int direction)
        {
            Lemming newLemming = new Lemming(_lemmingSprite, this);

            newLemming.MoveTo(_hatches[hatchIndex].Position + new Vector2(0, _lemmingSprite.TopMargin + 3));
            newLemming.SetScale(new Vector2(direction, 1));
            newLemming.SetCurrentLevel(_currentLevel);
            Components.Add(newLemming);
            _lemmings.Add(newLemming);
        }

        private int _hatchIndex;
        private float _currentSpawnRate;
        private bool _isSpawning;
        private void SpawnLemmings(GameTime gameTime)
        {
            if (_isSpawning && _spawnCount < _currentLevel.LemmingsCount)
            {
                float waitTime = ((100 - (int)_currentSpawnRate) / 2 + 3) * (3f / 50f);
                _spawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_spawnTimer > waitTime)
                {
                    _spawnTimer -= waitTime;
                    SpawnLemming(_hatchIndex, 1);
                    _hatchIndex = (_hatchIndex + 1) % _currentLevel.HatchPositions.Length;
                    _spawnCount++;
                    //_lemmingLevel1MusicInstance.Play();
                }
            }
        }

        private void ApplyHUDAction(Point scaledMousePosition)
        {
            if (scaledMousePosition.Y < PLAYGROUND_HEIGHT + 16)
                return;

            Mouse.SetCursor(_mouseCursorPoint);

            if (scaledMousePosition.X < 192)
            {
                int actionIndex = scaledMousePosition.X / 16;
                if (actionIndex < 0 || actionIndex >= _hudActions.Length)
                    return;

                (Action<int> action, ClickType clickType) hoveredAction = _hudActions[actionIndex];
                if (GetClickPerformed(hoveredAction.clickType))
                {
                    // Actions
                    hoveredAction.action.Invoke(actionIndex);
                }
            }
            else if (scaledMousePosition.X >= 208)
            {
                if (SimpleControls.IsLeftMouseButtonDown())
                {
                    float radarPositionX = scaledMousePosition.X - _radarRect.X / 2;
                    float playgroundPositionX = radarPositionX * _radarRatio;
                    ScrollOffset = playgroundPositionX * 2 - ScreenWidth / 4;
                }
            }
        }

        float _lastClickTime;
        private bool GetClickPerformed(ClickType clickType)
        {
            switch (clickType)
            {
                case ClickType.Clicked:
                    return SimpleControls.LeftMouseButtonPressedThisFrame();
                    break;
                case ClickType.Pressed:
                    return SimpleControls.IsLeftMouseButtonDown();
                    break;
                case ClickType.DoubleClicked:
                    if (SimpleControls.LeftMouseButtonPressedThisFrame())
                    {
                        if (GameTime.TotalGameTime.TotalSeconds - _lastClickTime < 0.2f)
                        {
                            return true;
                        }
                        _lastClickTime = (float)GameTime.TotalGameTime.TotalSeconds;
                    }
                    break;
            }

            return false;
        }

        private void ApplyLemmingAction(Point scaledMousePosition)
        {
            if (scaledMousePosition.Y >= PLAYGROUND_HEIGHT)
                return;

            Point scrolledMousePosition = new Point(scaledMousePosition.X + (int)ScrollOffset, scaledMousePosition.Y);

            Lemming[] hoveredLemmings = GetLemmingsUnderMouse(scrolledMousePosition);

            if (hoveredLemmings != null && hoveredLemmings.Length > 0)
            {
                Mouse.SetCursor(_mouseCursorSelect);
                if (SimpleControls.LeftMouseButtonPressedThisFrame() && _availableActions[_currentLemmingAction] > 0)
                {
                    if (_lemmingActions[_currentLemmingAction].Invoke(hoveredLemmings))
                    {
                        _availableActions[_currentLemmingAction]--;
                        _lemmingSelectSoundInstance.Stop();
                        _lemmingSelectSoundInstance.Pitch = 0;
                        _lemmingSelectSoundInstance.Play();
                    }
                }
            }
            else
            {
                if (SimpleControls.RightMouseButtonPressedThisFrame())
                {
                    Lemming.ExplosionParticles(scrolledMousePosition.ToVector2());
                }
                Mouse.SetCursor(_mouseCursorPoint);
            }
        }

        private Lemming[] GetLemmingsUnderMouse(Point scrolledMousePosition)
        {
            List<(Lemming lemming, float distance)> closestLemmings = new();
            foreach (Lemming lemming in _lemmings)
            {
                Rectangle bounds = lemming.GetBounds();
                if (bounds.Contains(scrolledMousePosition))
                {
                    float distance = (scrolledMousePosition.ToVector2() - lemming.Position).LengthSquared();
                    closestLemmings.Add((lemming, distance));
                }
            }
            closestLemmings.Sort((l1, l2) => (int)(l1.distance - l2.distance));
            return closestLemmings.Select(l => l.lemming).ToArray();
        }

        private void ApplyScrolling(GameTime gameTime, bool doubleSpeed, Point scaledMousePosition)
        {
            if (scaledMousePosition.Y >= PLAYGROUND_HEIGHT)
                return;

            if (scaledMousePosition.X < 5
                || scaledMousePosition.X > ScreenWidth / 2 - 5)
            {
                float scrollSpeed = 100f;
                float direction = scaledMousePosition.X < ScreenWidth / 4 ? -1 : 1;
                if (doubleSpeed)
                    scrollSpeed *= 2f;

                ScrollOffset += direction * scrollSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }

            ScrollOffset = MathHelper.Clamp(ScrollOffset, 0, PLAYGROUND_WIDTH - ScreenWidth);
        }

        private void GameDraw(SpriteBatch batch, GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(_gameRender);
            GraphicsDevice.Clear(new Color(0, 0, 51));

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_currentLevel.Texture, Vector2.Zero, Color.White);
            DrawComponents(gameTime);
            DrawParticles();
            _spriteBatch.End();

            GraphicsDevice.SetRenderTarget(_renderTarget);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_gameRender, new Rectangle(0, 0, 640, 160), new Rectangle((int)ScrollOffset, 0, 320, 160), Color.White);
            _spriteBatch.Draw(_hudTexture, new Vector2(0, 176), Color.White);
            Vector2 startPosition = new Vector2(6, 177);
            for (int i = 0; i < _availableActions.Length; i++)
            {
                if (_currentLevel.AvailableActions[i] > 0)
                {
                    int offset = (i + 2) * 32;
                    DrawNumber(_availableActions[i], startPosition + new Vector2(offset, 0));
                }
            }

            DrawNumber(_currentLevel.LemmingRate, startPosition);
            DrawNumber((int)_currentSpawnRate, startPosition + new Vector2(32, 0));

            _spriteBatch.Draw(_hudSelectTexture, new Vector2((_currentLemmingAction + 2) * 32, 176), Color.White);
            _spriteBatch.End();

            DrawRadar();

        }

        private void DrawRadar()
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.Opaque);
            _spriteBatch.Draw(_currentLevel.MaskTexture, _radarRect, Color.White);
            foreach (Lemming lemming in _lemmings)
            {
                int x = (int)MathF.Floor(lemming.PixelPositionX / _radarRatio);
                int y = (int)MathF.Floor(lemming.PixelPositionY / _radarRatio);
                _spriteBatch.DrawRectangle(_radarRect.Location.ToVector2() + new Vector2(x, y), Vector2.Zero, Color.Yellow);
            }
            _spriteBatch.End();

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
            _spriteBatch.Draw(_radarFrameTexture, _radarRect.Location.ToVector2() + new Vector2(ScrollOffset / _radarRatio - 2, -2), Color.White);
            _spriteBatch.End();
        }

        private void DrawNumber(int number, Vector2 position)
        {
            _digits.DrawFrame(number / 10, _spriteBatch, position, Point.Zero, 0, Vector2.One, Color.White);
            _digits.DrawFrame(number % 10, _spriteBatch, position + new Vector2(8, 0), Point.Zero, 0, Vector2.One, Color.White);
        }
        #endregion

        #region Actions
        public void ActionLemming(int index)
        {
            if (IsPause)
                return;

            _actionSelectSoundInstance.Stop();
            _actionSelectSoundInstance.Pitch = (index - 4) * (1 / 12f);
            _actionSelectSoundInstance.Play();

            _currentLemmingAction = index - 2;
        }

        private float _spawnSoundLastTime;
        public void ActionModifySpawn(int index)
        {
            if (IsPause)
                return;

            _currentSpawnRate = _currentSpawnRate + (index * 2 - 1) * DeltaTime * RATE_INCREASE_SPEED;

            if (_currentSpawnRate <= _currentLevel.LemmingRate || _currentSpawnRate >= 99)
            {
                _currentSpawnRate = Math.Clamp(_currentSpawnRate, _currentLevel.LemmingRate, 99); ;
                return;
            }

            if (GameTime.TotalGameTime.TotalSeconds - _spawnSoundLastTime > 0.15f)
            {
                _lemmingSelectSoundInstance.Stop();
                _lemmingSelectSoundInstance.Pitch = MathHelper.Lerp(-1f, 1f, _currentSpawnRate / 99f);
                _lemmingSelectSoundInstance.Play();
                _spawnSoundLastTime = (float)GameTime.TotalGameTime.TotalSeconds;
            }
        }

        public void ActionPause(int index)
        {
            _timeScale = 1 - _timeScale;
        }

        public void ActionArmageddon(int index)
        {
            if (IsPause)
                return;

            _isSpawning = false;
            for (int i = 0; i < _lemmings.Count; i++)
            {
                _lemmings[i].Explode(i * 0.05f);
            }
            _lemmingExplodeSoundInstance.Stop();
            _lemmingExplodeSoundInstance.Play();
        }

        public bool LemmingActionClimber(Lemming[] lemmings)
        {
            foreach (var lemming in lemmings)
            {
                if (lemming.Climb())
                    return true;
            }
            return false;
        }

        public bool LemmingActionFloater(Lemming[] lemmings)
        {
            foreach (var lemming in lemmings)
            {
                if (lemming.Float())
                    return true;
            }
            return false;
        }

        public bool LemmingActionBomb(Lemming[] lemmings)
        {
            foreach (var lemming in lemmings)
            {
                if (lemming.Explode())
                {
                    _lemmingSelectSoundInstance.Stop();
                    _lemmingExplodeSoundInstance.Stop();
                    _lemmingExplodeSoundInstance.Play();
                    return true;
                }
            }
            return false;
        }

        public bool LemmingActionBlocker(Lemming[] lemmings)
        {
            foreach (var lemming in lemmings)
            {
                if (lemming.Stop())
                    return true;
            }
            return true;

        }

        public bool LemmingActionBridgeBuilder(Lemming[] lemmings)
        {
            foreach (var lemming in lemmings)
            {
                if (lemming.Build())
                    return true;
            }
            return false;
        }

        public bool LemmingActionBasher(Lemming[] lemmings)
        {
            foreach (var lemming in lemmings)
            {
                if (lemming.Bash())
                    return true;
            }
            return false;
        }

        public bool LemmingActionMiner(Lemming[] lemmings)
        {
            // TODO
            return true;

        }

        public bool LemmingActionDigger(Lemming[] lemmings)
        {
            foreach (var lemming in lemmings)
            {
                if (lemming.DigDown())
                    return true;
            }

            return false;
        }
        #endregion
    }
}
