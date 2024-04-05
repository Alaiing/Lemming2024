using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Oudidon;
using System;
using System.Collections.Generic;
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

        public const string TIMES_UP_MESSAGE = "Your time is up!";
        public const string WIN_LOSE_MESSAGE = "All Lemmings accounted for.";

        private Color _greenColor = new Color(68, 170, 68, 1);
        private Color _radarColor = new Color(0, 136, 0, 1);
        private Color _callToActionColor = new Color(51, 136, 221);

        public enum ClickType { Pressed, Clicked, DoubleClicked }

        private RenderTarget2D _gameRender;
        private RenderTarget2D _waterRender;

        private SpriteSheet _lemmingSprite;
        private List<Lemming> _lemmings = new();

        private MouseCursor _mouseCursorMenu;
        private MouseCursor _mouseCursorPoint;
        private MouseCursor _mouseCursorSelect;
        private Texture2D _explodeTexture;
        private Color[] _explodeTextureData;

        private Texture2D _bashTexture;
        private Color[] _bashTextureData;

        private Texture2D _mineTexture;
        private Color[] _mineTextureData;

        private Texture2D _preGameBackground;
        private Texture2D _postGameBackground;

        private Texture2D _menuBackground;
        private Texture2D _hudTexture;
        private Texture2D _hudSelectTexture;
        private Texture2D _radarFrameTexture;
        private Texture2D _arrowsTexture;
        Rectangle _radarRect;
        float _radarRatio;

        private SpriteSheet _digits;

        private Texture2D _waterTexture;
        private float _waterAnimationIndex;
        private float _waterAnimationSpeed;

        private float _spawnCount;
        private int _savedLemmings;
        private int SavedPercent => _savedLemmings * 100 / _currentLevel.LemmingsCount;

        private Character[] _hatches;
        private Character _exit;

        private int _currentLemmingAction;
        private (Action<int>, ClickType)[] _hudActions;
        private Func<Lemming[], bool>[] _lemmingActions;
        private int[] _availableActions = new int[8];

        private Level _currentLevel;
        private float _spawnTimer = 0;
        private float _levelTimer;
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
        private SoundEffect _lemmingDieSound;
        private SoundEffectInstance _lemmingDieSoundInstance;
        private SoundEffect _pocSound;
        private SoundEffectInstance _pocSoundInstance;
        private SoundEffect _lemmingLevel1Music;
        private SoundEffectInstance _lemmingLevel1MusicInstance;

        private Effect _fontEffect;
        private Effect _colorEffect;
        private Effect _maskedRepeatEffect;
        private Effect _waterEffect;
        private Effect _repeatEffect;
        private SpriteFont _font;

        protected override void Initialize()
        {
            _gameRender = new RenderTarget2D(GraphicsDevice, PLAYGROUND_WIDTH, PLAYGROUND_HEIGHT);

            EventsManager.ListenTo<Lemming>(Lemming.EVENT_DIE, OnLemmingDead);
            EventsManager.ListenTo<Lemming>(Lemming.EVENT_EXPLODE, OnLemmingExplode);
            EventsManager.ListenTo<Lemming>(Lemming.EVENT_START_DIG_LINE, OnLemmingStartDigLine);
            EventsManager.ListenTo<Lemming>(Lemming.EVENT_DIG_LINE, OnLemmingDigLine);
            EventsManager.ListenTo<Lemming>(Lemming.EVENT_BASH, OnLemmingBash);
            EventsManager.ListenTo<Lemming>(Lemming.EVENT_MINE, OnLemmingMine);
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
            AddSate(STATE_MENU, onEnter: MenuEnter, onUpdate: MenuUpdate, onDraw: MenuDraw);
            AddSate(STATE_PRE_GAME, onEnter: PreGameEnter, onUpdate: PreGameUpdate, onDraw: PreGameDraw);
            AddSate(STATE_GAME, onEnter: GameEnter, onUpdate: GameUpdate, onExit: GameExit, onDraw: GameDraw);
            AddSate(STATE_POST_GAME, onEnter: PostGameEnter, onUpdate: PostGameUpdate, onDraw: PostGameDraw);
        }

        protected override void LoadContent()
        {
            Texture2D mouseCursorTexture = Content.Load<Texture2D>("cursor_point");
            _mouseCursorPoint = CreateCursor(mouseCursorTexture, new Vector2(0.5f, 0.5f));

            mouseCursorTexture = Content.Load<Texture2D>("cursor_select");
            _mouseCursorSelect = CreateCursor(mouseCursorTexture, new Vector2(0.5f, 0.5f));

            mouseCursorTexture = Content.Load<Texture2D>("cursor_menu");
            _mouseCursorMenu = CreateCursor(mouseCursorTexture, new Vector2(0.5f, 0.5f));

            _menuBackground = Content.Load<Texture2D>("title");
            _preGameBackground = Content.Load<Texture2D>("pre_game");
            _postGameBackground = Content.Load<Texture2D>("post_game");
            _hudTexture = Content.Load<Texture2D>("HUD");
            _hudSelectTexture = Content.Load<Texture2D>("HUD_select");
            _radarFrameTexture = Content.Load<Texture2D>("radar-frame");
            _arrowsTexture = Content.Load<Texture2D>("arrows");
            _digits = new SpriteSheet(Content, "digits", 8, 8, Point.Zero);

            _font = CreateSpriteFont("font", " !%-.0123456789?ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");

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

            _pocSound = Content.Load<SoundEffect>("ping");
            _pocSoundInstance = _pocSound.CreateInstance();

            _lemmingLevel1Music = Content.Load<SoundEffect>("lemmings-level1");
            _lemmingLevel1MusicInstance = _lemmingLevel1Music.CreateInstance();
            _lemmingLevel1MusicInstance.IsLooped = true;


            _lemmingSprite = new SpriteSheet(Content, "lemming", 20, 20, new Point(8, 10));
            _lemmingSprite.AddLayer(Content, "lemming-dirt");
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_WALK, 0, 7, Lemming.BASE_SPEED);
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_FALL, 32, 35, Lemming.BASE_SPEED, new Point(8, 11));
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_DIE_FALL, 176, 191, Lemming.BASE_SPEED);
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_DIE_DROWN, 192, 207, Lemming.BASE_SPEED);
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_OHNO, 160, 175, Lemming.BASE_SPEED);
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_EXPLODE, 221, 221, Lemming.BASE_SPEED);
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_DIG_DOWN, 128, 135, Lemming.BASE_SPEED, new Point(9, 11));
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_STOP, 48, 63, Lemming.BASE_SPEED);
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_CLIMB, 64, 71, Lemming.BASE_SPEED, new Point(8, 12));
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_END_CLIMB, 72, 79, Lemming.BASE_SPEED, new Point(8, 11));
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_FLOATER_OPEN, 36, 39, Lemming.BASE_SPEED, new Point(8, 17));
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_FLOATER, 40, 43, Lemming.BASE_SPEED, new Point(8, 17), SpriteSheet.AnimationType.PingPong);
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_BUILDER, 80, 95, Lemming.BASE_SPEED, new Point(8, 13));
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_BASHER, 96, 127, Lemming.BASE_SPEED);
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_MINER, 136, 159, Lemming.BASE_SPEED, new Point(8, 12));
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

            _waterTexture = Content.Load<Texture2D>("water");
            _waterAnimationSpeed = Lemming.BASE_SPEED;
            _waterRender = new RenderTarget2D(GraphicsDevice, PLAYGROUND_WIDTH, PLAYGROUND_HEIGHT);

            _explodeTexture = Content.Load<Texture2D>("circle_mask");
            _explodeTextureData = new Color[_explodeTexture.Width * _explodeTexture.Height];
            _explodeTexture.GetData(_explodeTextureData);

            _bashTexture = Content.Load<Texture2D>("bash_mask");
            _bashTextureData = new Color[_bashTexture.Width * _bashTexture.Height];
            _bashTexture.GetData(_bashTextureData);

            _mineTexture = Content.Load<Texture2D>("mine-mask");
            _mineTextureData = new Color[_mineTexture.Width * _mineTexture.Height];
            _mineTexture.GetData(_mineTextureData);

            _currentLevel = new Level(Content, "level12.data");


            _fontEffect = Content.Load<Effect>("File");
            _colorEffect = Content.Load<Effect>("PlainColor");
            _waterEffect = Content.Load<Effect>("WaterShader");
            _repeatEffect = Content.Load<Effect>("Repeat");
            _repeatEffect.Parameters["Tiling"]?.SetValue(new Vector2(10, 1));
            _maskedRepeatEffect = Content.Load<Effect>("MaskedRepeat");
            _maskedRepeatEffect.Parameters["Tiling"]?.SetValue(new Vector2(100, 10));

            SetState(STATE_MENU);
        }

        private SpriteFont CreateSpriteFont(string fontAsset, string charsString)
        {
            Texture2D fontTexture = Content.Load<Texture2D>(fontAsset);
            List<Rectangle> glyphBounds = new List<Rectangle>();
            List<Rectangle> cropping = new List<Rectangle>();
            List<char> chars = new List<char>();
            chars.AddRange(charsString);
            List<Vector3> kerning = new List<Vector3>();
            for (int i = 0; i < fontTexture.Width / 16; i++)
            {
                glyphBounds.Add(new Rectangle(i * 16, 0, 16, 15));
                cropping.Add(new Rectangle(0, 0, 16, 15));
                kerning.Add(new Vector3(0, 16, 0));
            }
            return new SpriteFont(fontTexture, glyphBounds, cropping, chars, 0, 0, kerning, '0');
        }

        private MouseCursor CreateCursor(Texture2D cursorTexture, Vector2 origin)
        {
            RenderTarget2D _mouseCursorPointScaled;
            _mouseCursorPointScaled = new RenderTarget2D(GraphicsDevice, cursorTexture.Width * ScreenScaleX, cursorTexture.Height * ScreenScaleY);
            GraphicsDevice.SetRenderTarget(_mouseCursorPointScaled);
            GraphicsDevice.Clear(Color.Transparent);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
            _spriteBatch.Draw(cursorTexture, new Rectangle(0, 0, _mouseCursorPointScaled.Width, _mouseCursorPointScaled.Height), Color.White);
            _spriteBatch.End();
            GraphicsDevice.SetRenderTarget(_renderTarget);

            return MouseCursor.FromTexture2D(_mouseCursorPointScaled, (int)(_mouseCursorPointScaled.Width * origin.X), (int)(_mouseCursorPointScaled.Height * origin.Y));
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

        private void ClearLemmings()
        {
            foreach(Lemming lem in _lemmings)
            {
                Components.Remove(lem);
            }
            _lemmings.Clear();
        }

        private void OnLemmingSaved(Lemming savedLemming)
        {
            RemoveLemming(savedLemming);
            _savedLemmings++;
        }

        private void OnLemmingExplode(Lemming explodingLemming)
        {
            _currentLevel.Dig(_explodeTexture, _explodeTextureData, new Point(explodingLemming.PixelPositionX - _explodeTexture.Width / 2, explodingLemming.PixelPositionY - _explodeTexture.Height / 2), forceDig: true);
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

        private void OnLemmingBash(Lemming lemming)
        {
            if (!_currentLevel.Dig(_bashTexture, _bashTextureData, new Point(lemming.PixelPositionX, lemming.PixelPositionY - _bashTexture.Height), flipHorizontal: lemming.CurrentScale.X < 0))
            {
                _pocSoundInstance.Stop();
                _pocSoundInstance.Play();
                lemming.Walk();
            }
        }

        private void OnLemmingMine(Lemming lemming)
        {
            if (!_currentLevel.Dig(_mineTexture, _mineTextureData, new Point(lemming.PixelPositionX, lemming.PixelPositionY - _mineTexture.Height + 1), flipHorizontal: lemming.CurrentScale.X < 0))
            {
                _pocSoundInstance.Stop();
                _pocSoundInstance.Play();
                lemming.Walk();
            }
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

            Mouse.SetCursor(_mouseCursorMenu);
            CameraFade.FadeFrom(Color.Black, FADE_DURATION);
        }

        private void PreGameDraw(SpriteBatch batch, GameTime time)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_preGameBackground, Vector2.Zero, Color.White);
            _spriteBatch.Draw(_currentLevel.Texture, new Rectangle(0, 0, ScreenWidth, 46), Color.White);
            _spriteBatch.End();
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: _fontEffect);
            int offset = 46;
            _spriteBatch.DrawString(_font, "Level", new Vector2(0, 16 + offset), new Color(170, 34, 34, 1));
            _spriteBatch.DrawString(_font, "1", new Vector2(99, 16 + offset), new Color(170, 34, 34, 1));
            _spriteBatch.DrawString(_font, "Number of Lemmings", new Vector2(160, 47 + offset), new Color(51, 136, 221, 1));
            _spriteBatch.DrawString(_font, _currentLevel.LemmingsCount.ToString(), new Vector2(467, 47 + offset), new Color(51, 136, 221, 1));
            _spriteBatch.DrawString(_font, "To Be Saved", new Vector2(240, 63 + offset), _greenColor);
            _spriteBatch.DrawString(_font, _currentLevel.WinRate.ToString() + "%", new Vector2(163, 63 + offset), _greenColor);
            _spriteBatch.DrawString(_font, "Release Rate", new Vector2(160, 78 + offset), new Color(170, 119, 68, 1));
            _spriteBatch.DrawString(_font, _currentLevel.LemmingsRate.ToString(), new Vector2(369, 78 + offset), new Color(170, 119, 68, 1));
            _spriteBatch.DrawString(_font, "Time", new Vector2(160, 93 + offset), new Color(68, 170, 153, 1));
            _spriteBatch.DrawString(_font, $"{(int)_currentLevel.Duration / 60} Minutes", new Vector2(289, 93 + offset), new Color(68, 170, 153, 1));

            _spriteBatch.DrawString(_font, "Press mouse button to continue", new Vector2(80, 140 + offset), _callToActionColor);

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
            Mouse.SetCursor(_mouseCursorMenu);
            CameraFade.FadeFromBlack(FADE_DURATION);
        }

        private void PostGameDraw(SpriteBatch batch, GameTime time)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_postGameBackground, Vector2.Zero, Color.White);
            _spriteBatch.End();

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: _fontEffect);
            Color color = new Color(153, 68, 170, 1);
            _spriteBatch.DrawString(_font, "You needed", new Vector2(192, 32), color);
            _spriteBatch.DrawString(_font, _currentLevel.WinRate.ToString() + "%", new Vector2(380, 32), color);
            _spriteBatch.DrawString(_font, "You rescued", new Vector2(192, 47), color);
            _spriteBatch.DrawString(_font, SavedPercent.ToString() + "%", new Vector2(380, 47), color);

            color = new Color(119, 187, 187, 1);
            string topMessage;
            if (_levelTimer <= 0 && SavedPercent < _currentLevel.WinRate)
            {
                topMessage = TIMES_UP_MESSAGE;
            }
            else
            {
                topMessage = WIN_LOSE_MESSAGE;
            }
            _spriteBatch.DrawString(_font, topMessage, new Vector2(ScreenWidth / 2 - _font.MeasureString(topMessage).X / 2, 1), color);

            string endGameTextLine1;
            string endGameTextLine2;
            string nextText1;
            string nextText2;
            color = new Color(170, 34, 34, 1);
            if (SavedPercent >= _currentLevel.WinRate)
            {
                if (SavedPercent == 100)
                {
                    endGameTextLine1 = "Superb! You rescued every Lemmings on";
                    endGameTextLine2 = "that level. Can you do it again....?";
                }
                else
                {
                    endGameTextLine1 = "That level seemed no problem do you on";
                    endGameTextLine2 = "that attempt. Onto the next....";
                }
                nextText1 = "";
                nextText2 = "Press left button to continue";
            }
            else
            {
                endGameTextLine1 = "ROCK BOTTOM! I hope for you sake";
                endGameTextLine2 = "that you nuked that level";
                nextText1 = "Press left button to try again";
                nextText2 = "Press right button for menu";
            }
            Vector2 endGameTextSize1 = _font.MeasureString(endGameTextLine1);
            Vector2 endGameTextSize2 = _font.MeasureString(endGameTextLine2);
            _spriteBatch.DrawString(_font, endGameTextLine1, new Vector2(ScreenWidth / 2 - endGameTextSize1.X / 2, 81), color);
            _spriteBatch.DrawString(_font, endGameTextLine2, new Vector2(ScreenWidth / 2 - endGameTextSize2.X / 2, 81 + 15), color);

            _spriteBatch.DrawString(_font, nextText1, new Vector2(80, 169), _callToActionColor);
            _spriteBatch.DrawString(_font, nextText2, new Vector2(80, 185), _callToActionColor);

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
            else if (SavedPercent < _currentLevel.WinRate && SimpleControls.RightMouseButtonPressedThisFrame())
            {
                CameraFade.FadeToBlack(FADE_DURATION, OnFadeDone: () => SetState(STATE_MENU));
            }
        }

        private void GameEnter()
        {
            Mouse.SetCursor(_mouseCursorPoint);
            _spawnCount = 0;
            _spawnTimer = 0;
            _levelTimer = _currentLevel.Duration;
            _savedLemmings = 0;
            for (int i = 0; i < _currentLevel.HatchPositions.Length; i++)
            {
                ShowHatch(i);
            }
            _currentSpawnRate = _currentLevel.LemmingsRate;
            _currentLevel.AvailableActions.CopyTo(_availableActions, 0);
            _exit.MoveTo(_currentLevel.ExitPosition.ToVector2());
            ScrollOffset = _currentLevel.StartScrollOffset;

            _waterAnimationIndex = 0;

            CameraFade.FadeFromBlack(FADE_DURATION, OnFadeDone: StartLevel);
        }

        private void StartLevel()
        {
            _letsgoSoundInstance.Play();
            _hatches[0].DelayAction(OpenHatches, (float)_letsgoSound.Duration.TotalSeconds);
        }

        private void OpenHatches()
        {
            _hatchSoundInstance.Play();
            for (int i = 0; i < _currentLevel.HatchPositions.Length; i++)
            {
                OpenHatch(i, onHatchOpened: (i == 0 ? OnHatchOpen : null));
            }
        }

        private void OnHatchOpen()
        {
            StartSpawn();
            _lemmingLevel1MusicInstance.Play();
        }

        private void StartSpawn()
        {
            _isSpawning = true;
        }

        private void GameExit()
        {
            ClearLemmings();
            _lemmingLevel1MusicInstance.Stop();
        }

        private void GameUpdate(GameTime gameTime, float stateTime)
        {
            _waterAnimationIndex = _waterAnimationIndex + _waterAnimationSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (CameraFade.IsFading)
            {
                return;
            }

            _levelTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_levelTimer <= 0
                || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                CameraFade.FadeToBlack(FADE_DURATION, OnFadeDone:
                    () =>
                    {
                        _levelTimer = 0;
                        SetState(STATE_POST_GAME);
                    });
                return;
            }

            SimpleControls.GetStates();

            Point mousePosition = SimpleControls.GetMousePosition();
            Point scaledMousePosition = new Point(mousePosition.X / 2 / ScreenScaleX, mousePosition.Y / ScreenScaleY);

            Rectangle screen = new Rectangle(0,0, ScreenWidth / 2, ScreenHeight);
            if (screen.Contains(scaledMousePosition))
            {
                ApplyScrolling(gameTime, SimpleControls.IsRightMouseButtonDown(), scaledMousePosition);

                ApplyHUDAction(scaledMousePosition);

                ApplyLemmingAction(scaledMousePosition);
            }

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

        private Lemming[] _hoveredLemmings;
        private void ApplyLemmingAction(Point scaledMousePosition)
        {
            if (scaledMousePosition.Y >= PLAYGROUND_HEIGHT)
                return;

            Point scrolledMousePosition = new Point(scaledMousePosition.X + (int)ScrollOffset, scaledMousePosition.Y);

            _hoveredLemmings = GetLemmingsUnderMouse(scrolledMousePosition);

            if (_hoveredLemmings != null && _hoveredLemmings.Length > 0)
            {
                Mouse.SetCursor(_mouseCursorSelect);
                if (SimpleControls.LeftMouseButtonPressedThisFrame() && _availableActions[_currentLemmingAction] > 0)
                {
                    if (_lemmingActions[_currentLemmingAction].Invoke(_hoveredLemmings))
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
                if (SimpleControls.IsRightMouseButtonDown())
                {
                    _currentLevel.Dig(_explodeTexture, _explodeTextureData, scrolledMousePosition, forceDig: true);
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

        private void GameDraw(SpriteBatch spriteBatch, GameTime gameTime)
        {

            if (_currentLevel.WaterType != Level.WaterTypes.none)
            {
                GraphicsDevice.SetRenderTarget(_waterRender);
                GraphicsDevice.Clear(new Color(0, 0, 0, 0));

                int frameIndex = (int)Math.Floor(_waterAnimationIndex) % 6;
                spriteBatch.Begin(samplerState: SamplerState.PointWrap, effect: _repeatEffect);
                _maskedRepeatEffect.Parameters["Tiling"]?.SetValue(new Vector2(10, 1));
                spriteBatch.Draw(_waterTexture, new Rectangle(0, PLAYGROUND_HEIGHT - 18, _waterRender.Width, 18), new Rectangle(0, frameIndex * 18, _waterTexture.Width, 18), Color.White);
                spriteBatch.End();
            }

            GraphicsDevice.SetRenderTarget(_gameRender);
            GraphicsDevice.Clear(new Color(0, 0, 51));

            spriteBatch.Begin(samplerState: SamplerState.PointWrap, effect: _waterEffect);
            _waterEffect.Parameters["MaskTexture"]?.SetValue(_currentLevel.MaskTexture);
            _waterEffect.Parameters["AlphaClip"]?.SetValue(0.9f);
            spriteBatch.Draw(_waterRender, new Vector2(0, 0), Color.White);
            spriteBatch.End();


            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(_currentLevel.Texture, Vector2.Zero, Color.White);
            DrawComponents(gameTime);
            DrawParticles();
            spriteBatch.End();

            spriteBatch.Begin(samplerState: SamplerState.PointWrap, blendState: BlendState.AlphaBlend, effect: _maskedRepeatEffect);
            _maskedRepeatEffect.Parameters["Tiling"]?.SetValue(new Vector2(100, 10));
            _maskedRepeatEffect.Parameters["MaskTexture"]?.SetValue(_currentLevel.MaskTexture);
            _maskedRepeatEffect.Parameters["Time"]?.SetValue((float)gameTime.TotalGameTime.TotalSeconds * 8);
            _maskedRepeatEffect.Parameters["AlphaClip"]?.SetValue(1f);
            spriteBatch.Draw(_arrowsTexture, new Rectangle(0, 0, 1600, 160), Color.White);
            spriteBatch.End();

            GraphicsDevice.SetRenderTarget(_renderTarget);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(_gameRender, new Rectangle(0, 0, 640, 160), new Rectangle((int)ScrollOffset, 0, 320, 160), Color.White);
            spriteBatch.Draw(_hudTexture, new Vector2(0, 176), Color.White);
            Vector2 startPosition = new Vector2(6, 177);
            for (int i = 0; i < _availableActions.Length; i++)
            {
                if (_currentLevel.AvailableActions[i] > 0)
                {
                    int offset = (i + 2) * 32;
                    DrawNumber(_availableActions[i], startPosition + new Vector2(offset, 0), _digits);
                }
            }

            DrawNumber(_currentLevel.LemmingsRate, startPosition, _digits);
            DrawNumber((int)_currentSpawnRate, startPosition + new Vector2(32, 0), _digits);

            spriteBatch.Draw(_hudSelectTexture, new Vector2((_currentLemmingAction + 2) * 32, 176), Color.White);
            spriteBatch.End();

            DrawRadar();
            DrawInfos();

        }

        private void DrawRadar()
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointWrap, blendState: BlendState.Opaque, effect: _colorEffect);
            _spriteBatch.Draw(_currentLevel.MaskTexture, _radarRect, _radarColor);
            _spriteBatch.End();
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.Opaque);
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

        private void DrawInfos()
        {
            int time = (int)Math.Floor(Math.Max(0, _levelTimer));
            int minutes = time / 60;
            int seconds = time % 60;
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: _fontEffect);

            _spriteBatch.DrawString(_font, "TIME", new Vector2(496, 161), _greenColor);
            _spriteBatch.DrawString(_font, $"{minutes}-{seconds:00}", new Vector2(576, 161), _greenColor);

            _spriteBatch.DrawString(_font, "IN", new Vector2(371, 161), _greenColor);
            _spriteBatch.DrawString(_font, $"{SavedPercent:00}%", new Vector2(416, 161), _greenColor);

            _spriteBatch.DrawString(_font, "OUT", new Vector2(224, 161), _greenColor);
            _spriteBatch.DrawString(_font, $"{_lemmings.Count}", new Vector2(288, 161), _greenColor);

            if (_hoveredLemmings != null && _hoveredLemmings.Length > 0)
            {
                _spriteBatch.DrawString(_font, _hoveredLemmings[0].TypeName, new Vector2(0, 161), _greenColor);
                _spriteBatch.DrawString(_font, $"{_hoveredLemmings.Length}", new Vector2(128, 161), _greenColor);
            }

            _spriteBatch.End();
        }

        private void DrawNumber(int number, Vector2 position, SpriteSheet digits)
        {
            digits.DrawFrame(number / 10, _spriteBatch, position, Point.Zero, 0, Vector2.One, Color.White);
            digits.DrawFrame(number % 10, _spriteBatch, position + new Vector2(digits.FrameWidth, 0), Point.Zero, 0, Vector2.One, Color.White);
        }

        private void MenuEnter()
        {
            CameraFade.FadeFromBlack(FADE_DURATION);
            Mouse.SetCursor(_mouseCursorMenu);
        }

        private void MenuUpdate(GameTime gameTime, float stateTime)
        {
            if (CameraFade.IsFading)
            {
                return;
            }

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            SimpleControls.GetStates();
            Point mousePosition = SimpleControls.GetMousePosition();
            Point scaledMousePosition = new Point(mousePosition.X / ScreenScaleX, mousePosition.Y / ScreenScaleY);

            Rectangle startGameRectangle = new Rectangle(15, 135, 104, 37);

            if (SimpleControls.LeftMouseButtonPressedThisFrame())
            {
                if (startGameRectangle.Contains(scaledMousePosition))
                {
                    CameraFade.FadeToBlack(FADE_DURATION, () => SetState(STATE_PRE_GAME));
                }
            }
        }

        private void MenuDraw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
            spriteBatch.Draw(_menuBackground, Vector2.Zero, Color.White);
            spriteBatch.End();
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

            if (_currentSpawnRate <= _currentLevel.LemmingsRate || _currentSpawnRate >= 99)
            {
                _currentSpawnRate = Math.Clamp(_currentSpawnRate, _currentLevel.LemmingsRate, 99); ;
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
            foreach (var lemming in lemmings)
            {
                if (lemming.Mine())
                    return true;
            }
            return false;
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
