using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Population3
{
    public enum VisualizationLayer
    {
        Mass,
        Density,
        Temperature,
        Pressure
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Camera2D _camera;

        // Define simulation bounds based on GameConstants.
        private RectangleF _bounds = new RectangleF(
            -GameConstants.SimulationSize,
            -GameConstants.SimulationSize,
            2 * GameConstants.SimulationSize,
            2 * GameConstants.SimulationSize);

        // Particle simulation.
        private List<PointMass> _particles;
        private Random _random;

        // Gas simulation grid.
        private GasGrid _gasGrid;
        // A simple white texture for drawing rectangles.
        private Texture2D _whitePixel;

        // Visualization layer for the gas grid.
        private VisualizationLayer _currentLayer = VisualizationLayer.Mass;
        private GamePadState _previousGamePadState;

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
            _particles = EarlyUniverseGeneration.Generate(GraphicsDevice, _random);

            // Initialize the gas grid.
            int gridWidth = 70;
            int gridHeight = 70;
            float cellSize = (2 * GameConstants.SimulationSize) / gridWidth;
            _gasGrid = new GasGrid(gridWidth, gridHeight, cellSize);

            // Define the scaled gas constant and compute cell volume (assuming unit depth).
            float volume = cellSize * cellSize;

            // Randomize each cell’s properties.
            for (int i = 0; i < gridWidth; i++)
            {
                for (int j = 0; j < gridHeight; j++)
                {
                    var cell = _gasGrid.GetCell(i, j);

                    float mass = _random.NextSingle(EarlyUniverseGeneration.StarMass / 4.0f, EarlyUniverseGeneration.StarMass / 2.0f);
                    cell.Mass = mass;

                    // Compute density using fixed cell volume.
                    cell.Density = mass / volume;

                    // Temperature around 300K with ±50K variation.
                    cell.Temperature = _random.NextSingle(10, 30);

                    // Precalculate pressure.
                    cell.Pressure = cell.Density * GameConstants.GasConstant * cell.Temperature;

                    // Assign a small random velocity (between -1 and 1 for both x and y).
                    cell.Velocity = new Vector2(
                        (float)(_random.NextDouble() * 2 - 1),
                        (float)(_random.NextDouble() * 2 - 1)
                    );
                }
            }

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _whitePixel = new Texture2D(GraphicsDevice, 1, 1);
            _whitePixel.SetData(new[] { Color.White });
        }

        protected override void Update(GameTime gameTime)
        {
            float deltaT = 0.01666f; // fixed time step
            var gamePadState = GamePad.GetState(PlayerIndex.One);
            _camera.Update(gamePadState, GameConstants.ScreenWidth, GameConstants.ScreenHeight);

            // Check for digipad input to switch visualization layers.
            // (Only trigger on the edge of the press.)
            if (_previousGamePadState.DPad.Right == ButtonState.Released &&
                gamePadState.DPad.Right == ButtonState.Pressed)
            {
                // Cycle to next layer.
                _currentLayer = (VisualizationLayer)(((int)_currentLayer + 1) % Enum.GetNames(typeof(VisualizationLayer)).Length);
            }
            if (_previousGamePadState.DPad.Left == ButtonState.Released &&
                gamePadState.DPad.Left == ButtonState.Pressed)
            {
                // Cycle to previous layer.
                int index = (int)_currentLayer - 1;
                if (index < 0)
                    index = Enum.GetNames(typeof(VisualizationLayer)).Length - 1;
                _currentLayer = (VisualizationLayer)index;
            }
            _previousGamePadState = gamePadState;

            // Update particle simulation.
            var tree = new PositionCache<PointMass>(_bounds);
            tree.Build(_particles.Where(p => p.Mass >= GameConstants.MinimumMassForQuadtree && !p.Merged));
            ProcessCollisions(tree);
            UpdatePhysics(tree, deltaT);

            // Update gas simulation.
            _gasGrid.Update(deltaT);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(transformMatrix: _camera.Transform);

            // Draw the gas grid as a heatmap using the current visualization layer.
            for (int i = 0; i < _gasGrid.Width; i++)
            {
                for (int j = 0; j < _gasGrid.Height; j++)
                {
                    GasCell cell = _gasGrid.GetCell(i, j);

                    float propertyValue = 0f;
                    float minValue = 0f;
                    float maxValue = 0f;
                    float volume = _gasGrid.CellSize * _gasGrid.CellSize;

                    switch (_currentLayer)
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
                            minValue = 10f; // as defined in initialization
                            maxValue = 30f; // as defined in initialization
                            break;
                        case VisualizationLayer.Pressure:
                            propertyValue = cell.Pressure;
                            float minDensity = (EarlyUniverseGeneration.StarMass / 4f) / volume;
                            float maxDensity = (EarlyUniverseGeneration.StarMass / 2f) / volume;
                            minValue = minDensity * GameConstants.GasConstant * 10f;
                            maxValue = maxDensity * GameConstants.GasConstant * 30f;
                            break;
                    }

                    Color cellColor = MapFloatToColor(propertyValue, minValue, maxValue);

                    Rectangle rect = new Rectangle(
                        (int)(-GameConstants.SimulationSize + i * _gasGrid.CellSize),
                        (int)(-GameConstants.SimulationSize + j * _gasGrid.CellSize),
                        (int)_gasGrid.CellSize,
                        (int)_gasGrid.CellSize);
                    _spriteBatch.Draw(_whitePixel, rect, cellColor * 0.6f);
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
            base.Draw(gameTime);
        }

        // Maps a float value to a heatmap color.
        private Color MapFloatToColor(float value, float lowerLim, float upperLim)
        {
            float t = MathHelper.Clamp((value - lowerLim) / (upperLim - lowerLim), 0f, 1f);
            return new Color((byte)(t * 255), 0, (byte)((1 - t) * 255));
        }

        // Computes the difference between two positions, taking the wrap-around into account.
        private Vector2 GetWrappedDifference(Vector2 a, Vector2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            float halfWidth = _bounds.Width / 2f;
            float halfHeight = _bounds.Height / 2f;

            if (dx > halfWidth)
                dx -= _bounds.Width;
            else if (dx < -halfWidth)
                dx += _bounds.Width;

            if (dy > halfHeight)
                dy -= _bounds.Height;
            else if (dy < -halfHeight)
                dy += _bounds.Height;

            return new Vector2(dx, dy);
        }

        // Wraps a position so that it remains within the simulation bounds.
        private Vector2 Wrap(Vector2 position)
        {
            float width = _bounds.Width;
            float height = _bounds.Height;
            float x = position.X;
            float y = position.Y;

            while (x < _bounds.X)
                x += width;
            while (x >= _bounds.X + width)
                x -= width;
            while (y < _bounds.Y)
                y += height;
            while (y >= _bounds.Y + height)
                y -= height;

            return new Vector2(x, y);
        }

        private void ProcessCollisions(PositionCache<PointMass> treeForCollision)
        {
            var mergeEvents = new ConcurrentBag<(PointMass a, PointMass b)>();
            Parallel.For(0, _particles.Count, i =>
            {
                var particle = _particles[i];
                if (particle.Merged)
                    return;

                var candidates = treeForCollision.GetInRadius(particle.Position, particle.Radius * 2.0f);
                foreach (var candidate in candidates)
                {
                    float radiiSum = particle.Radius + candidate.Radius;
                    float distanceSq = GetWrappedDifference(particle.Position, candidate.Position).LengthSquared();
                    if (distanceSq < radiiSum * radiiSum)
                    {
                        if (particle.Id.CompareTo(candidate.Id) > 0)
                        {
                            mergeEvents.Add((particle, candidate));
                        }
                    }
                }
            });

            foreach (var (a, b) in mergeEvents)
            {
                if (!a.Merged && !b.Merged)
                {
                    var mergedParticle = PointMass.Merge(a, b);
                    a.Mass = mergedParticle.Mass;
                    a.Position = mergedParticle.Position;
                    a.Velocity = mergedParticle.Velocity;
                    a.Density = mergedParticle.Density;
                    b.Merged = true;
                }
            }
        }

        private void UpdatePhysics(PositionCache<PointMass> treeForGravity, float deltaT)
        {
            Parallel.For(0, _particles.Count, i =>
            {
                var particle = _particles[i];
                if (particle.Merged)
                    return;

                particle.CurrentForce = AddGravityForces(treeForGravity, particle);
                particle = particle.ApplyForceAndIntegrate(deltaT);
                // Wrap the position after integration.
                particle.Position = Wrap(particle.Position);
                _particles[i] = particle;
            });
        }

        private Vector2 AddGravityForces(PositionCache<PointMass> tree, PointMass particle)
        {
            Vector2 netForce = Vector2.Zero;
            var neighbours = tree.GetInRadius(particle.Position, GameConstants.GravityNeighborRadius);
            foreach (var neighbor in neighbours)
            {
                if (particle.Equals(neighbor))
                    continue;

                // Use wrapped difference to find the shortest vector.
                Vector2 direction = GetWrappedDifference(neighbor.Position, particle.Position);
                float distanceSquared = direction.LengthSquared();
                if (distanceSquared < GameConstants.MinimumDistanceSquared)
                    distanceSquared = GameConstants.MinimumDistanceSquared;

                float forceMagnitude = GameConstants.GravitationalConstant * particle.Mass * neighbor.Mass / distanceSquared;
                netForce += Vector2.Normalize(direction) * forceMagnitude;
            }
            return netForce;
        }
    }

    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new Game1())
                game.Run();
        }
    }
}
