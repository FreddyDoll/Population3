using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Population3
{
    public class GasGrid
    {
        private GasCell[,] _cells;
        public int Width { get; }
        public int Height { get; }
        // The size of each cell in world units.
        public float CellSize { get; }

        // Precomputed gravitational kernel.
        private Vector2[,] _gravKernel;
        private int _gravKernelRadius;
        private bool _kernelInitialized = false;

        // Precomputed cell centers (fixed in world space).
        private Vector2[,] _cellCenters;

        public GasGrid(int width, int height, float cellSize)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;
            _cells = new GasCell[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    _cells[i, j] = new GasCell();
                }
            }

            // Precompute cell centers.
            _cellCenters = new Vector2[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    float centerX = i * CellSize - GameConstants.SimulationSize + 0.5f * CellSize;
                    float centerY = j * CellSize - GameConstants.SimulationSize + 0.5f * CellSize;
                    _cellCenters[i, j] = new Vector2(centerX, centerY);
                }
            }

            // Set the kernel radius.
            _gravKernelRadius = (int)(GameConstants.GravityNeighborRadius / CellSize);
            // For testing you might override this (e.g., _gravKernelRadius = 3)
            PrecomputeGravKernel();
        }

        // Precompute the gravitational kernel: for each offset (di,dj) compute:
        //   kernel = G * disp / (r^3)
        private void PrecomputeGravKernel()
        {
            int kernelSize = 2 * _gravKernelRadius + 1;
            _gravKernel = new Vector2[kernelSize, kernelSize];
            for (int di = -_gravKernelRadius; di <= _gravKernelRadius; di++)
            {
                for (int dj = -_gravKernelRadius; dj <= _gravKernelRadius; dj++)
                {
                    if (di == 0 && dj == 0)
                    {
                        _gravKernel[di + _gravKernelRadius, dj + _gravKernelRadius] = Vector2.Zero;
                    }
                    else
                    {
                        float dx = di * CellSize;
                        float dy = dj * CellSize;
                        Vector2 disp = new Vector2(dx, dy);
                        float rSquared = disp.LengthSquared();
                        float r = (float)Math.Sqrt(rSquared);
                        _gravKernel[di + _gravKernelRadius, dj + _gravKernelRadius] =
                            GameConstants.GravitationalConstant * disp / (r * rSquared);
                    }
                }
            }
            _kernelInitialized = true;
        }
        private void ApplyLocalMassCorrection(GasCell[,] oldCells, int localRadius = 3)
        {
            // Create a temporary array to store the corrected cells.
            GasCell[,] correctedCells = new GasCell[Width, Height];

            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    float localMassOld = 0.0f;
                    float localMassNew = 0.0f;

                    // Sum the masses in the neighborhood defined by localRadius.
                    for (int di = -localRadius; di <= localRadius; di++)
                    {
                        for (int dj = -localRadius; dj <= localRadius; dj++)
                        {
                            int ni = WrapIndex(i + di, Width);
                            int nj = WrapIndex(j + dj, Height);
                            localMassOld += oldCells[ni, nj].Mass;
                            localMassNew += _cells[ni, nj].Mass;
                        }
                    }

                    // Avoid division by zero.
                    float localCorrection = localMassNew != 0.0f ? localMassOld / localMassNew : 1.0f;

                    // Create a new cell with corrected mass.
                    correctedCells[i, j] = new GasCell();
                    correctedCells[i, j].Mass = _cells[i, j].Mass * localCorrection;
                    correctedCells[i, j].Density = correctedCells[i, j].Mass / (CellSize * CellSize);
                    // Optionally copy other properties.
                    correctedCells[i, j].Temperature = _cells[i, j].Temperature;
                    correctedCells[i, j].Pressure = _cells[i, j].Pressure;
                    correctedCells[i, j].Velocity = _cells[i, j].Velocity;
                    correctedCells[i, j].Acceleration = _cells[i, j].Acceleration;
                }
            }

            // Replace the current grid with the corrected one.
            _cells = correctedCells;
        }



        // Helper method to wrap an index (periodic boundaries).
        private int WrapIndex(int index, int max)
        {
            return ((index % max) + max) % max;
        }

        // Access a cell using periodic boundaries.
        public GasCell GetCell(int x, int y)
        {
            x = WrapIndex(x, Width);
            y = WrapIndex(y, Height);
            return _cells[x, y];
        }

        /// <summary>
        /// Updates the gas simulation.
        /// </summary>
        /// <param name="deltaT">Time step.</param>
        public void Update(float deltaT)
        {
            float simWidth = 2 * GameConstants.SimulationSize;
            float simHeight = 2 * GameConstants.SimulationSize;

            // 1. Recompute pressure for each cell.
            Parallel.For(0, Width, i =>
            {
                for (int j = 0; j < Height; j++)
                {
                    GasCell cell = _cells[i, j];
                    cell.Pressure = cell.Density * GameConstants.GasConstant * cell.Temperature;
                }
            });

            // 2. Create a snapshot of the current grid state.
            GasCell[,] oldCells = new GasCell[Width, Height];
            Parallel.For(0, Width, i =>
            {
                for (int j = 0; j < Height; j++)
                {
                    GasCell cell = _cells[i, j];
                    oldCells[i, j] = new GasCell()
                    {
                        Density = cell.Density,
                        Temperature = cell.Temperature,
                        Velocity = cell.Velocity,
                        Mass = cell.Mass,
                        Pressure = cell.Pressure,
                        Acceleration = cell.Acceleration
                    };
                }
            });

            // 3. Update velocities based on pressure gradients and Newtonian gravity.
            Parallel.For(0, Width, i =>
            {
                for (int j = 0; j < Height; j++)
                {
                    GasCell cell = _cells[i, j];

                    // Compute pressure gradient.
                    Vector2 pressureGradient = Vector2.Zero;
                    GasCell left = GetCell(i - 1, j);
                    GasCell right = GetCell(i + 1, j);
                    GasCell up = GetCell(i, j - 1);
                    GasCell down = GetCell(i, j + 1);
                    pressureGradient.X = (right.Pressure - left.Pressure) / (2 * CellSize);
                    pressureGradient.Y = (down.Pressure - up.Pressure) / (2 * CellSize);

                    // Compute gravitational acceleration using the precomputed kernel.
                    Vector2 gravitationalAcceleration = Vector2.Zero;
                    for (int di = -_gravKernelRadius; di <= _gravKernelRadius; di++)
                    {
                        for (int dj = -_gravKernelRadius; dj <= _gravKernelRadius; dj++)
                        {
                            if (di == 0 && dj == 0)
                                continue;
                            int ni = WrapIndex(i + di, Width);
                            int nj = WrapIndex(j + dj, Height);
                            float neighborMass = _cells[ni, nj].Mass;
                            gravitationalAcceleration += _gravKernel[di + _gravKernelRadius, dj + _gravKernelRadius] * neighborMass;
                        }
                    }
                    // Total acceleration is the pressure gradient contribution plus gravitational acceleration.
                    Vector2 acceleration = (-pressureGradient / cell.Density) + gravitationalAcceleration * 1.0f;
                    cell.Acceleration = gravitationalAcceleration*1.0f;

                    cell.Velocity += acceleration * deltaT;
                }
            });

            // 4. Advect scalar properties using bilinear interpolation.
            Advect(deltaT, oldCells);

            // 5. Apply a global mass correction to maintain mass conservation.
            ApplyLocalMassCorrection(oldCells);
        }

        public Vector2 GetGasAccelerationAt(Vector2 position)
        {
            // Convert world position to grid indices.
            float relativeX = position.X + GameConstants.SimulationSize;
            float relativeY = position.Y + GameConstants.SimulationSize;
            int cellX = (int)(relativeX / CellSize);
            int cellY = (int)(relativeY / CellSize);

            // You could improve this using bilinear interpolation if needed.
            return GetCell(cellX, cellY).Acceleration;
        }

        /// <summary>
        /// Advects the cell properties (density, temperature, mass) using a semi-Lagrangian method with bilinear interpolation.
        /// Periodic boundaries are applied during sampling.
        /// </summary>
        /// <param name="deltaT">Time step.</param>
        /// <param name="oldCells">Snapshot of the grid state before the velocity update.</param>
        private void Advect(float deltaT, GasCell[,] oldCells)
        {
            GasCell[,] newCells = new GasCell[Width, Height];
            Parallel.For(0, Width, i =>
            {
                for (int j = 0; j < Height; j++)
                {
                    newCells[i, j] = new GasCell();
                    GasCell updatedCell = _cells[i, j];
                    // Compute the source position (in grid coordinates) based on the updated velocity.
                    float srcX = i + 0.5f - (updatedCell.Velocity.X * deltaT) / CellSize;
                    float srcY = j + 0.5f - (updatedCell.Velocity.Y * deltaT) / CellSize;
                    float sampleX = srcX - 0.5f;
                    float sampleY = srcY - 0.5f;
                    int i0 = (int)Math.Floor(sampleX);
                    int j0 = (int)Math.Floor(sampleY);
                    int i1 = i0 + 1;
                    int j1 = j0 + 1;
                    float fracX = sampleX - i0;
                    float fracY = sampleY - j0;
                    i0 = WrapIndex(i0, Width);
                    j0 = WrapIndex(j0, Height);
                    i1 = WrapIndex(i1, Width);
                    j1 = WrapIndex(j1, Height);
                    GasCell Q11 = oldCells[i0, j0];
                    GasCell Q21 = oldCells[i1, j0];
                    GasCell Q12 = oldCells[i0, j1];
                    GasCell Q22 = oldCells[i1, j1];

                    float interpDensity =
                        (1 - fracX) * (1 - fracY) * Q11.Density +
                        fracX * (1 - fracY) * Q21.Density +
                        (1 - fracX) * fracY * Q12.Density +
                        fracX * fracY * Q22.Density;
                    float interpTemperature =
                        (1 - fracX) * (1 - fracY) * Q11.Temperature +
                        fracX * (1 - fracY) * Q21.Temperature +
                        (1 - fracX) * fracY * Q12.Temperature +
                        fracX * fracY * Q22.Temperature;
                    float interpMass =
                        (1 - fracX) * (1 - fracY) * Q11.Mass +
                        fracX * (1 - fracY) * Q21.Mass +
                        (1 - fracX) * fracY * Q12.Mass +
                        fracX * fracY * Q22.Mass;

                    newCells[i, j].Density = interpDensity;
                    newCells[i, j].Temperature = interpTemperature;
                    newCells[i, j].Mass = interpMass;
                    newCells[i, j].Velocity = updatedCell.Velocity;
                    newCells[i, j].Acceleration = updatedCell.Acceleration;
                }
            });

            _cells = newCells;
        }
    }
}
