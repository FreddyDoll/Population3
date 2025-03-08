using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Population3
{
    public static class FirstCollapseGeneration
    {
        public const int StarTextureRadius = 40;

        public const float TotalBarionicMass = 500; //Solar Masses 

        public static GasGrid GenerateGasGridCentral(Random random)
        {
            int gridWidth = GameConstants.GasGridWidth;
            int gridHeight = GameConstants.GasGridHeight;
            float cellSize = (2 * GameConstants.SimulationHalfWidth) / gridWidth;
            var gasGrid = new GasGrid(gridWidth, gridHeight, cellSize);

            float volume = cellSize * cellSize;

            // Determine the maximum possible distance from the center for normalization
            float maxDistance = GameConstants.SimulationHalfWidth * 2.0f;

            float currentMass = 0;
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

                    // Create a radial gradient for mass distribution
                    float mass = TotalBarionicMass / (gridWidth * gridHeight);
                    mass *= maxDistance - distanceFromCenter;  // Mass decreases with distance

                    cell.Mass = mass;
                    currentMass += cell.Mass;

                    // Radial velocity example: inward flow towards center
                    //Vector2 directionToCenter = new Vector2(-centerX, -centerY);
                    //if (directionToCenter != Vector2.Zero)
                    //    directionToCenter.Normalize();

                    // Velocity decreases with distance from center
                    //float maxVelocity = 100000f;
                    //cell.Velocity = directionToCenter * maxVelocity * (1 - normalizedDistance);
                }
            }

            float fact = TotalBarionicMass / currentMass;
            for (int i = 0; i < gridWidth; i++)
            {
                for (int j = 0; j < gridHeight; j++)
                {
                    var cell = gasGrid.GetCell(i, j);
                    cell.Mass *= fact;

                    cell.Velocity = Vector2.Zero;

                    cell.Temperature = 100.0f;

                    cell.Density = cell.Mass / volume;
                    cell.Pressure = cell.Density * GameConstants.GasConstant * cell.Temperature;
                }
            }

            return gasGrid;
        }

    }
}
