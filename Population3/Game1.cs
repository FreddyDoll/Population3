using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Population3
{
    public class Game1 : Game
    {
        private readonly RectangleF _bounds = new RectangleF(
            -GameConstants.SimulationSize,
            -GameConstants.SimulationSize,
            2 * GameConstants.SimulationSize,
            2 * GameConstants.SimulationSize);

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Camera2D _camera;

        private Random _random;

        private GasGrid _gasGrid;

        private List<PointMass> _particles;
        private ParticleSimulator _particleSimulator;

        private Texture2D _whitePixel;
        private SpriteFont _hudFont;
        private Hud_GasGrid _hud;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = GameConstants.ScreenWidth,
                PreferredBackBufferHeight = GameConstants.ScreenHeight
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _camera = new Camera2D(Vector2.Zero, GameConstants.InitialZoom);
        }

        protected override void Initialize()
        {
            _random = new Random(GameConstants.RandomSeed);

            _particles = EarlyUniverseGeneration.GeneratePointMasses(GraphicsDevice, _random);
            _gasGrid = EarlyUniverseGeneration.GenerateGasGridCentral(_random);

            // Instantiate the ParticleSimulation with the current bounds and particle list.
            _particleSimulator = new ParticleSimulator(_bounds);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _whitePixel = new Texture2D(GraphicsDevice, 1, 1);
            _whitePixel.SetData(new[] { Color.White });
            _hudFont = Content.Load<SpriteFont>("hudFont");

            _hud = new Hud_GasGrid(_gasGrid, GraphicsDevice, _hudFont);
        }

        protected override void Update(GameTime gameTime)
        {
            float deltaT = GameConstants.FixedDeltaTime;
            var gamePadState = GamePad.GetState(PlayerIndex.One);

            _camera.Update(gamePadState, GameConstants.ScreenWidth, GameConstants.ScreenHeight);
            _hud.Update(gamePadState);

            // Delegate particle simulation update to the ParticleSimulation class.
            _particleSimulator.Update(_particles,_gasGrid,deltaT);

            _gasGrid.Update(deltaT);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(transformMatrix: _camera.Transform);

            // Draw gas grid cells.
            for (int i = 0; i < _gasGrid.Width; i++)
            {
                for (int j = 0; j < _gasGrid.Height; j++)
                {
                    GasCell cell = _gasGrid.GetCell(i, j);

                    float propertyValue = 0f;
                    float minValue = 0f;
                    float maxValue = 0f;
                    float volume = _gasGrid.CellSize * _gasGrid.CellSize;

                    switch (_hud.CurrentLayer)
                    {
                        case VisualizationLayer.Mass:
                            propertyValue = cell.Mass;
                            minValue = 0f;
                            maxValue = EarlyUniverseGeneration.StarMass;
                            break;
                        case VisualizationLayer.Density:
                            propertyValue = cell.Density;
                            minValue = (EarlyUniverseGeneration.StarMass / 4f) / volume;
                            maxValue = (EarlyUniverseGeneration.StarMass / 2f) / volume;
                            break;
                        case VisualizationLayer.Temperature:
                            propertyValue = cell.Temperature;
                            minValue = 10f;
                            maxValue = 30f;
                            break;
                        case VisualizationLayer.Pressure:
                            propertyValue = cell.Pressure;
                            float minDensity = (EarlyUniverseGeneration.StarMass / 4f) / volume;
                            float maxDensity = (EarlyUniverseGeneration.StarMass / 2f) / volume;
                            minValue = minDensity * GameConstants.GasConstant * 10f;
                            maxValue = maxDensity * GameConstants.GasConstant * 30f;
                            break;
                    }

                    Color cellColor = ColorMapping.FloatToColor(propertyValue, minValue, maxValue);

                    Rectangle rect = new Rectangle(
                        (int)(-GameConstants.SimulationSize + i * _gasGrid.CellSize),
                        (int)(-GameConstants.SimulationSize + j * _gasGrid.CellSize),
                        (int)_gasGrid.CellSize,
                        (int)_gasGrid.CellSize);
                    _spriteBatch.Draw(_whitePixel, rect, cellColor * GameConstants.GasGridAlpha);
                }
            }

            // Draw particles.
            foreach (var particle in _particles)
            {
                if (particle.Merged)
                    continue;

                Vector2 origin = new Vector2(particle.Texture.Width / 2f, particle.Texture.Height / 2f);
                float scale = particle.Radius / (particle.Texture.Width / 2f);
                _spriteBatch.Draw(
                    particle.Texture,
                    particle.Position,
                    null,
                    Color.White,
                    0f,
                    origin,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }

            _spriteBatch.End();

            // Draw the HUD without the camera transform.
            _spriteBatch.Begin();
            _hud.Draw(_spriteBatch);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
