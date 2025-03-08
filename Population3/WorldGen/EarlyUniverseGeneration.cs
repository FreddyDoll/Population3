using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Population3
{
    public static class EarlyUniverseGeneration
    {
        public const int StarTextureRadius = 40;
        public const int StarParticleCount = 50;
        public const float StarMass = 1000000000f;
        public const float StarDensity = 0.3f;

        public static GasGrid GenerateGasGridCentral(Random random)
        {
            int gridWidth = GameConstants.GasGridWidth;
            int gridHeight = GameConstants.GasGridHeight;
            float cellSize = (2 * GameConstants.SimulationHalfWidth) / gridWidth;
            var gasGrid = new GasGrid(gridWidth, gridHeight, cellSize);

            float volume = cellSize * cellSize;

            // Define the central circle radius (adjust as needed)
            float centralRadius = GameConstants.SimulationHalfWidth / 3.0f;

            // Loop through each cell and assign properties based on distance from center.
            for (int i = 0; i < gridWidth; i++)
            {
                for (int j = 0; j < gridHeight; j++)
                {
                    var cell = gasGrid.GetCell(i, j);

                    // Compute cell center in world coordinates.
                    float centerX = i * cellSize - GameConstants.SimulationHalfWidth + 0.5f * cellSize;
                    float centerY = j * cellSize - GameConstants.SimulationHalfWidth + 0.5f * cellSize;
                    float distanceFromCenter = MathF.Sqrt(centerX * centerX + centerY * centerY);

                    // If the cell is within the central circle, assign a high mass; otherwise, assign a lower mass.
                    float mass;
                    if (distanceFromCenter < centralRadius)
                    {
                        // High-density region: use a higher mass range.
                        mass = random.NextSingle(EarlyUniverseGeneration.StarMass / 1.5f, EarlyUniverseGeneration.StarMass);
                    }
                    else
                    {
                        // Low-density region: use a lower mass range.
                        mass = random.NextSingle(EarlyUniverseGeneration.StarMass / 20.0f, EarlyUniverseGeneration.StarMass / 5.0f);
                    }

                    cell.Mass = mass;
                    cell.Density = mass / volume;
                    cell.Temperature = random.NextSingle(10, 30);
                    cell.Pressure = cell.Density * GameConstants.GasConstant * cell.Temperature;
                    cell.Velocity = Vector2.One * 100000f;//Vector2.Zero;
                }
            }

            return gasGrid;
        }

        public static GasGrid GenerateGasGrid(Random random)
        {
            int gridWidth = GameConstants.GasGridWidth;
            int gridHeight = GameConstants.GasGridHeight;
            float cellSize = (2 * GameConstants.SimulationHalfWidth) / gridWidth;
            var gasGrid = new GasGrid(gridWidth, gridHeight, cellSize);

            float volume = cellSize * cellSize;

            for (int i = 0; i < gridWidth; i++)
            {
                for (int j = 0; j < gridHeight; j++)
                {
                    var cell = gasGrid.GetCell(i, j);

                    float mass = random.NextSingle(EarlyUniverseGeneration.StarMass / 2.0f, EarlyUniverseGeneration.StarMass / 1.0f);
                    cell.Mass = mass;

                    cell.Density = mass / volume;

                    cell.Temperature = random.NextSingle(10, 30);

                    cell.Pressure = cell.Density * GameConstants.GasConstant * cell.Temperature;

                    cell.Velocity = new Vector2(
                        (float)(random.NextDouble() * 2 - 1),
                        (float)(random.NextDouble() * 2 - 1)
                    );
                }
            }

            return gasGrid;
        }

        public static List<PointMass> GeneratePointMasses(GraphicsDevice graphicsDevice, Random random)
        {
            List<PointMass> particles = new List<PointMass>();
            var starTexture = Population3.WorldGen.GenHelpers.CreateFadingCircle(graphicsDevice, Color.White, StarTextureRadius);
            for (int i = 0; i < StarParticleCount; i++)
            {
                var particle = new PointMass
                {
                    Position = random.NextVectorRect() * GameConstants.SimulationHalfWidth,
                    Velocity = random.NextVectorRect() * 10000f,
                    Density = StarDensity,
                    Mass = StarMass,
                    Texture = starTexture,
                };
                particles.Add(particle);
            }

            return particles;
        }
    }
}
