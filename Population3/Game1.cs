using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Population3
{
    public class Game1 : Game
    {
        private readonly RectangleF _bounds = new RectangleF(
            -GameConstants.SimulationHalfWidth,
            -GameConstants.SimulationHalfWidth,
            2 * GameConstants.SimulationHalfWidth,
            2 * GameConstants.SimulationHalfWidth);

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

            //_particles = GalaxyGeneration.Generate(GraphicsDevice, _random);
            //_particles = EarlyUniverseGeneration.GeneratePointMasses(GraphicsDevice, _random);
            _particles = new();

            _particleSimulator = new ParticleSimulator(_bounds);
 
            _gasGrid = FirstCollapseGeneration.GenerateGasGridCentral(_random);


            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _whitePixel = new Texture2D(GraphicsDevice, 1, 1);
            _whitePixel.SetData(new[] { Color.White });
            _hudFont = Content.Load<SpriteFont>("hudFont");

            _hud = new Hud_GasGrid(_gasGrid, GraphicsDevice, _hudFont);
            
            var stats = _gasGrid.GetMassStatsMass();
            _hud.MaxMassPerCell = GameConstants.MaxMassPerCell;
        }

        protected override void Update(GameTime gameTime)
        {
            float deltaT = GameConstants.PhysicsTickTime;
            var gamePadState = GamePad.GetState(PlayerIndex.One);

            _camera.Update(gamePadState, GameConstants.ScreenWidth, GameConstants.ScreenHeight);
            _hud.Update(gamePadState);

            var tree = _particleSimulator.Update(_particles,_gasGrid,deltaT);
            _gasGrid.Update(tree ,deltaT);

            for (int i = 0; i < _gasGrid.Width; i++)
                for (int j= 0; j < _gasGrid.Height; j++)
                {
                    var c = _gasGrid.GetCell(i, j);
                    if ( c.Mass > GameConstants.MaxMassPerCell)
                    {
                        var p = new PointMass
                        {
                            Mass = (float)c.Mass,
                            Density = 0.0000001f,
                            Position = new Vector2(
                                (i + 0.5f) * _gasGrid.CellSize - GameConstants.SimulationHalfWidth, 
                                (j + 0.5f) * _gasGrid.CellSize - GameConstants.SimulationHalfWidth
                                ),
                            Texture = WorldGen.GenHelpers.CreateFadingCircle(GraphicsDevice, Color.White, EarlyUniverseGeneration.StarTextureRadius),
                            Velocity = 0.02f * c.Velocity, //TODO: Better solution for fixed factor
                        };
                        c.Mass = 0;
                        _particles.Add(p);
                    }
                }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(transformMatrix: _camera.CurrentTransform);

            #region Draw GasGrid


            for (int i = 0; i < _gasGrid.Width; i++)
            {
                for (int j = 0; j < _gasGrid.Height; j++)
                {
                    GasCell cell = _gasGrid.GetCell(i, j);
            
                    float minValue;
                    float maxValue;
                    _hud.GetCurrentLayerRange(out minValue, out maxValue);

                    float propertyValue;
                    _hud.SelectPropertyFromCurrentLayer(cell, out propertyValue);

                    Color cellColor = ColorMapping.FloatToColor(propertyValue, minValue, maxValue);
            
                    Rectangle rect = new Rectangle(
                        (int)(-GameConstants.SimulationHalfWidth + i * _gasGrid.CellSize),
                        (int)(-GameConstants.SimulationHalfWidth + j * _gasGrid.CellSize),
                        (int)_gasGrid.CellSize,
                        (int)_gasGrid.CellSize);
                    _spriteBatch.Draw(_whitePixel, rect, cellColor * GameConstants.GasGridAlpha);
                }
            }
            #endregion

            #region Draw Particals
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
            #endregion

            _spriteBatch.End();

            // Draw the HUD without the camera transform.
            _spriteBatch.Begin();
            _hud.Draw(_spriteBatch);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
