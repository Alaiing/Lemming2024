using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oudidon
{
    public class OudidonGame : Game
    {
        protected int _screenWidth;
        public int ScreenWidth => _screenWidth;
        protected int _screenHeight;
        public int ScreenHeight => _screenHeight;
        protected int _screenScaleX;
        public int ScreenScaleX => _screenScaleX;
        protected int _screenScaleY;
        public int ScreenScaleY => _screenScaleY;

        protected GraphicsDeviceManager _graphics;
        protected SpriteBatch _spriteBatch;
        public SpriteBatch SpriteBatch => _spriteBatch;

        protected RenderTarget2D _renderTarget;

        protected SimpleStateMachine _stateMachine;

        protected virtual string ConfigFileName => "config.ini";

        public float DeltaTime { get; private set; }
        public GameTime GameTime { get; private set; }
        protected float _timeScale;
        public bool IsPause => _timeScale == 0;

        public OudidonGame() : base()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            ConfigManager.LoadConfig(ConfigFileName);
            InitGraphics();

            CommonRandom.Init();
        }

        protected virtual void InitGraphics()
        {
            _screenWidth = ConfigManager.GetConfig("SCREEN_WIDTH", 320);
            _screenHeight = ConfigManager.GetConfig("SCREEN_HEIGHT", 200);
            _screenScaleX = ConfigManager.GetConfig("SCREEN_SCALE_X", 4);
            _screenScaleY = ConfigManager.GetConfig("SCREEN_SCALE_Y", 4);
            _graphics.PreferredBackBufferWidth = _screenWidth * _screenScaleX;
            _graphics.PreferredBackBufferHeight = _screenHeight * _screenScaleY;
            _graphics.ApplyChanges();
            _renderTarget = new RenderTarget2D(GraphicsDevice, _screenWidth, _screenHeight);
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Initialize()
        {
            _timeScale = 1f;
            _stateMachine = new SimpleStateMachine();
            InitStateMachine();
            base.Initialize();
        }

        protected override void LoadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            gameTime.ElapsedGameTime *= _timeScale;
            GameTime = gameTime;
            CameraShake.Update(DeltaTime);
            CameraFade.Update(DeltaTime);
            Particles.Update(DeltaTime);
            UpdateStateMachine(GameTime);
            UpdateComponents(GameTime);
        }

        protected void UpdateStateMachine(GameTime gameTime)
        {
            _stateMachine.Update(gameTime);
        }

        protected void UpdateComponents(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        protected override bool BeginDraw()
        {
            GraphicsDevice.SetRenderTarget(_renderTarget);
            return true;
        }

        protected override sealed void Draw(GameTime gameTime)
        {
            DrawGameplay(gameTime);
            DrawUI(gameTime);
        }

        protected virtual void DrawGameplay(GameTime gameTime)
        {
            DrawStateMachine(gameTime);
        }

        protected void DrawParticles()
        {
            Particles.Draw(SpriteBatch);
        }

        protected void DrawStateMachine(GameTime gameTime)
        {
            _stateMachine.Draw(_spriteBatch, gameTime);
        }

        protected void DrawComponents(GameTime gameTime)
        {
            base.Draw(gameTime);
        }

        protected virtual void DrawUI(GameTime gameTime)
        {

        }


        protected override void EndDraw()
        {
            GraphicsDevice.SetRenderTarget(null);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
            _spriteBatch.Draw(_renderTarget, new Rectangle((int)MathF.Floor(CameraShake.ShakeOffset.X * _screenScaleX), (int)MathF.Floor(CameraShake.ShakeOffset.Y * _screenScaleY), _screenWidth * _screenScaleX, _screenHeight * _screenScaleY), CameraFade.Color);
            _spriteBatch.End();
            base.EndDraw();
        }

        protected virtual void InitStateMachine() { }

        protected void AddSate(string name, Action onEnter = null, Action onExit = null, Action<GameTime, float> onUpdate = null, Action<SpriteBatch, GameTime> onDraw = null)
        {
            _stateMachine.AddState(name, onEnter, onExit, onUpdate, onDraw);
        }

        protected void SetState(string state)
        {
            _stateMachine.SetState(state);
        }
    }
}
