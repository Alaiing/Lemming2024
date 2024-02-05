using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Oudidon;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lemmings2024
{
    public class Lemmings2024 : OudidonGame
    {
        public const int PLAYGROUND_WIDTH = 1600;
        public const int PLAYGROUND_HEIGHT = 160;

        private const string STATE_MENU = "Menu";
        private const string STATE_PRE_GAME = "Pre-Game";
        private const string STATE_GAME = "Game";
        private const string STATE_POST_GAME = "Post-Game";

        public const int LINE_DIG_LENGTH = 9;

        private Texture2D _levelTexture;
        private Color[] _levelTextureData;
        private RenderTarget2D _gameRender;

        private SpriteSheet _lemmingSprite;
        private List<Lemming> _lemmings = new();

        private MouseCursor _mouseCursorPoint;
        private MouseCursor _mouseCursorSelect;

        private Texture2D _roundTexture;
        private Color[] _roundTextureData;

        private Texture2D _preGameBackground;
        private Texture2D _postGameBackground;

        private float _spawnRate = 50f;
        private float _spawnCount;
        private float _savedLemmings;

        private Character[] _hatches;
        private Character _exit;

        protected override void Initialize()
        {
            _gameRender = new RenderTarget2D(GraphicsDevice, PLAYGROUND_WIDTH, PLAYGROUND_HEIGHT);

            EventsManager.ListenTo<Lemming>(Lemming.EVENT_DIE, OnLemmingDead);
            EventsManager.ListenTo<Lemming>(Lemming.EVENT_EXPLODE, OnLemmingExplode);
            EventsManager.ListenTo<Lemming>(Lemming.EVENT_START_DIG_LINE, OnLemmingStartDigLine);
            EventsManager.ListenTo<Lemming>(Lemming.EVENT_DIG_LINE, OnLemmingDigLine);
            EventsManager.ListenTo<Lemming>(Lemming.EVENT_SAVED, OnLemmingSaved);

            base.Initialize();
        }

        protected override void InitStateMachine()
        {
            base.InitStateMachine();
            AddSate(STATE_MENU);
            AddSate(STATE_PRE_GAME, onEnter: PreGameEnter, onUpdate: PreGameUpdate, onDraw: PreGameDraw);
            AddSate(STATE_GAME, onEnter: GameEnter, onUpdate: GameUpdate, onExit: GameExit, onDraw: GameDraw);
            AddSate(STATE_POST_GAME, onEnter: PostGameEnter, onUpdate: PostGameUpdate, onDraw: PostGameDraw);
            SetState(STATE_PRE_GAME);
        }

        protected override void LoadContent()
        {
            Texture2D mouseCursorTexture = Content.Load<Texture2D>("cursor_point");
            _mouseCursorPoint = CreateCursor(mouseCursorTexture);

            mouseCursorTexture = Content.Load<Texture2D>("cursor_select");
            _mouseCursorSelect = CreateCursor(mouseCursorTexture);

            _preGameBackground = Content.Load<Texture2D>("pre_game");
            _postGameBackground = Content.Load<Texture2D>("post_game");

            _lemmingSprite = new SpriteSheet(Content, "lemming", 20, 20, new Point(9, 10));
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_WALK, 0, 7, Lemming.BASE_SPEED);
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_FALL, 32, 35, Lemming.BASE_SPEED, new Point(8, 11));
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_DIE_FALL, 176, 191, Lemming.BASE_SPEED);
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_OHNO, 160, 175, Lemming.BASE_SPEED);
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_EXPLODE, 221, 221, Lemming.BASE_SPEED);
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_DIG_DOWN, 128, 135, Lemming.BASE_SPEED, new Point(9, 11));
            _lemmingSprite.RegisterAnimation(Lemming.ANIMATION_SAVED, 19, 25, Lemming.BASE_SPEED);

            SpriteSheet hatch = new SpriteSheet(Content, "hatch", 41, 25, new Point(20, 0));
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

            _roundTexture = Content.Load<Texture2D>("circle_mask");
            _roundTextureData = new Color[_roundTexture.Width * _roundTexture.Height];
            _roundTexture.GetData(_roundTextureData);


            _hatches[0].MoveTo(new Vector2(744, 37));
            _exit.MoveTo(new Vector2(890, 133));
            OpenHatch(0);
        }

        private void LoadLevel(string levelName)
        {
            if (_levelTexture != null)
            {
                Content.UnloadAsset("level1");
            }
            _levelTexture = Content.Load<Texture2D>(levelName);
            _levelTextureData = new Color[_levelTexture.Width * _levelTexture.Height];
            _levelTexture.GetData(_levelTextureData);
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

        private float _spawnTimer = 0;
        private float _scrollX = 640;
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

        private void SpawnLemming(int hatchIndex, int direction)
        {
            Lemming newLemming = new Lemming(_lemmingSprite, this);

            newLemming.MoveTo(_hatches[hatchIndex].Position + new Vector2(0, _lemmingSprite.TopMargin + 3));
            newLemming.SetScale(new Vector2(direction, 1));
            newLemming.SetCurrentLevel(_levelTextureData);
            Components.Add(newLemming);
            _lemmings.Add(newLemming);
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
                SetState(STATE_POST_GAME);
        }

        private void OnLemmingSaved(Lemming savedLemming)
        {
            RemoveLemming(savedLemming);
            _savedLemmings++;
        }

        private void OnLemmingExplode(Lemming explodingLemming)
        {
            DigLevel(_levelTexture, _levelTextureData, _roundTexture, _roundTextureData, new Point(explodingLemming.PixelPositionX - _roundTexture.Width / 2, explodingLemming.PixelPositionY - _roundTexture.Height / 2));
        }

        private void OnLemmingStartDigLine(Lemming lemming)
        {
            DigLevel(_levelTexture, _levelTextureData, new Point(lemming.PixelPositionX - LINE_DIG_LENGTH / 2 - 1, lemming.PixelPositionY), LINE_DIG_LENGTH);
            DigLevel(_levelTexture, _levelTextureData, new Point(lemming.PixelPositionX - LINE_DIG_LENGTH / 2 - 1, lemming.PixelPositionY - 1), LINE_DIG_LENGTH);
            DigLevel(_levelTexture, _levelTextureData, new Point(lemming.PixelPositionX - LINE_DIG_LENGTH / 2 - 1, lemming.PixelPositionY - 2), LINE_DIG_LENGTH);
        }

        private void OnLemmingDigLine(Lemming lemming)
        {
            DigLevel(_levelTexture, _levelTextureData, new Point(lemming.PixelPositionX - LINE_DIG_LENGTH / 2 - 1, lemming.PixelPositionY), LINE_DIG_LENGTH);
        }

        private void DigLevel(Texture2D levelTexture, Color[] levelTextureData, Texture2D digTexture, Color[] digTextureData, Point position)
        {
            int startIndex = position.Y * levelTexture.Width + position.X;
            for (int i = 0; i < digTextureData.Length; i++)
            {
                int textureRelativeIndex = i % digTexture.Width + (i / digTexture.Width) * levelTexture.Width;
                if (digTextureData[i].A > 0)
                {
                    if (startIndex + textureRelativeIndex < _levelTextureData.Length)
                    {
                        levelTextureData[startIndex + textureRelativeIndex] = Color.Transparent;
                    }
                }
            }
            levelTexture.SetData(levelTextureData);
        }

        private void DigLevel(Texture2D levelTexture, Color[] levelTextureData, Point position, int length)
        {
            if (position.Y >= levelTexture.Height)
                return;

            int endPosition = Math.Min(levelTexture.Width, position.X + length);
            length = endPosition - position.X;
            int startIndex = position.Y * levelTexture.Width + position.X;
            for (int i = 0; i < length; i++)
            {
                levelTextureData[startIndex + i] = Color.Transparent;
            }
            levelTexture.SetData(levelTextureData);
        }

        private void OpenHatch(int index)
        {
            _hatches[index].Activate();
            _hatches[index].SetAnimation("Open", onAnimationEnd: () => _hatches[index].SetAnimation("Idle"));
        }

        #region States
        private void PreGameEnter()
        {
            LoadLevel("level1");
            // TODO: load level data
        }

        private void PreGameDraw(SpriteBatch batch, GameTime time)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_preGameBackground, Vector2.Zero, Color.White);
            _spriteBatch.End();
        }

        private void PreGameUpdate(GameTime time, float arg2)
        {
            MouseState mouseState = Mouse.GetState();
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                SetState(STATE_GAME);
            }
        }

        private void PostGameEnter()
        {
            // TODO: load level data
        }

        private void PostGameDraw(SpriteBatch batch, GameTime time)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_postGameBackground, Vector2.Zero, Color.White);
            _spriteBatch.End();
        }

        private void PostGameUpdate(GameTime time, float arg2)
        {
            MouseState mouseState = Mouse.GetState();
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                SetState(STATE_PRE_GAME);
            }
        }


        private void GameEnter()
        {
            Mouse.SetCursor(_mouseCursorPoint);
            _spawnCount = 0;
            _spawnTimer = 0;
            // TODO: reset scroll offset
        }

        private void GameExit()
        {
            // TODO: remove all lemmings
        }

        private void GameUpdate(GameTime gameTime, float stateTime)
        {
            MouseState mouseState = Mouse.GetState();

            Point scaledMousePosition = new Point(mouseState.X / ScreenScaleX, mouseState.Y / ScreenScaleY);
            //if (scaledMousePosition.X < 5
            //    || scaledMousePosition.X > ScreenWidth - 5)
            //{
            //    float scrollSpeed = 100f;
            //    float direction = scaledMousePosition.X < ScreenWidth / 2 ? -1 : 1;
            //    if (mouseState.RightButton == ButtonState.Pressed)
            //        scrollSpeed *= 2f;

            //    _scrollX += direction * scrollSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            //}

            //_scrollX = MathHelper.Clamp(_scrollX, 0, PLAYGROUND_WIDTH - ScreenWidth);

            Point scrolledMousePosition = new Point(scaledMousePosition.X + (int)_scrollX, scaledMousePosition.Y);
            Lemming hoveredLemming = null;
            foreach (Lemming lemming in _lemmings)
            {
                Rectangle bounds = lemming.GetBounds();
                if (bounds.Contains(scrolledMousePosition))
                {
                    hoveredLemming = lemming;
                    break;
                }
            }

            if (hoveredLemming != null)
            {
                Mouse.SetCursor(_mouseCursorSelect);
                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    hoveredLemming.DigDown();
                }
            }
            else
            {
                if (mouseState.RightButton == ButtonState.Pressed)
                {
                    DigLevel(_levelTexture, _levelTextureData, _roundTexture, _roundTextureData, scrolledMousePosition);
                }
                Mouse.SetCursor(_mouseCursorPoint);
            }

            if (_spawnCount < 1)
            {
                _spawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_spawnTimer > 100f / _spawnRate)
                {
                    _spawnTimer -= 100f / _spawnRate;
                    SpawnLemming(0, 1);
                    _spawnCount++;
                }
            }

            foreach (Lemming lemming in _lemmings)
            {
                if (Vector2.Distance(lemming.Position, _exit.Position) < 3)
                {
                    lemming.MoveTo(_exit.Position + new Vector2(0, -1));
                    lemming.Save();
                }
            }
        }


        private void GameDraw(SpriteBatch batch, GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(_gameRender);
            GraphicsDevice.Clear(new Color(0, 0, 51));

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_levelTexture, Vector2.Zero, Color.White);
            DrawComponents(gameTime);
            _spriteBatch.End();

            GraphicsDevice.SetRenderTarget(_renderTarget);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_gameRender, new Rectangle(0, 0, 320, 160), new Rectangle((int)_scrollX, 0, 320, 160), Color.White);
            _spriteBatch.End();
        }

        #endregion
    }
}
