using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Population3.Helpers;
using SharpDX.Direct3D9;

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
                    float centerX = i * CellSize - GameConstants.SimulationHalfWidth + 0.5f * CellSize;
                    float centerY = j * CellSize - GameConstants.SimulationHalfWidth + 0.5f * CellSize;
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

        public (double minMass, double maxMass, double totalMass) GetMassStatsMass()
        {
            double maxMass = double.MinValue;
            double minMass = double.MaxValue;
            double totalMass = 0;
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    var cell = GetCell(i, j);

                    totalMass += cell.Mass;

                    if (cell.Mass > maxMass)
                        maxMass = cell.Mass;

                    if (cell.Mass < minMass)
                        minMass = cell.Mass;
                }
            }

            return (minMass, maxMass, totalMass);
        }

        /// <summary>
        /// Updates the gas simulation.
        /// </summary>
        /// <param name="deltaT">Time step.</param>
        public void Update(PositionCache<PointMass> particleTree, float deltaT)
        {
            float simWidth = 2 * GameConstants.SimulationHalfWidth;
            float simHeight = 2 * GameConstants.SimulationHalfWidth;

            // 1. Recompute pressure for each cell.
            Parallel.For(0, Width, i =>
            {
                for (int j = 0; j < Height; j++)
                {
                    GasCell cell = _cells[i, j];
                    cell.Pressure = cell.Density * GameConstants.GasConstant * cell.Temperature;
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
                    pressureGradient.X = (float)(right.Pressure - left.Pressure) / (2 * CellSize);
                    pressureGradient.Y = (float)(down.Pressure - up.Pressure) / (2 * CellSize);

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
                            float neighborMass = (float)_cells[ni, nj].Mass;
                            gravitationalAcceleration += _gravKernel[di + _gravKernelRadius, dj + _gravKernelRadius] * neighborMass;
                        }
                    }

                    // Get the world-space center of the cell.
                    Vector2 cellCenter = _cellCenters[i, j];
                    Vector2 accelFromGravityParticles = Vector2.Zero;

                    // Query the tree for particles near the cell center.
                    var neighbours = particleTree.GetInRadius(cellCenter, GameConstants.GravityNeighborRadius * 3);

                    // Sum up the gravitational acceleration contributions from each particle.
                    foreach (var particle in neighbours)
                    {
                        // Compute the displacement vector using periodic (wrapped) distances.
                        Vector2 disp = CoordinateWrapping.GetWrappedDifference(particleTree.WorldBounds, particle.Position, cellCenter);
                        float rSquared = disp.LengthSquared();
                        if (rSquared < GameConstants.MinimumDistanceSquared)
                            rSquared = GameConstants.MinimumDistanceSquared;

                        // Acceleration contribution: a = G * m / r^2 in the direction of disp.
                        accelFromGravityParticles += Vector2.Normalize(disp) * (GameConstants.GravitationalConstant * particle.Mass / rSquared);
                    }

                    // Total acceleration is the pressure gradient contribution plus gravitational acceleration.
                    Vector2 accelFromPressure = (-pressureGradient / (float)cell.Density);
                    //Vector2 accele = accelFromPressure + accelFromGravityParticles + gravitationalAcceleration;
                    //Vector2 accele = accelFromGravityParticles*100.0f;
                    Vector2 accele = gravitationalAcceleration + accelFromPressure + accelFromGravityParticles;

                    cell.LocalAccelerationFromGravity = gravitationalAcceleration;

                    cell.Velocity += accele * deltaT;
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
                        LocalAccelerationFromGravity = cell.LocalAccelerationFromGravity
                    };
                }
            });

            Advect(deltaT, oldCells);
        }

        public Vector2 GetGasAccelerationAt(Vector2 position)
        {
            // Convert world position to grid indices.
            float relativeX = position.X + GameConstants.SimulationHalfWidth;
            float relativeY = position.Y + GameConstants.SimulationHalfWidth;
            int cellX = (int)(relativeX / CellSize);
            int cellY = (int)(relativeY / CellSize);

            // You could improve this using bilinear interpolation if needed.
            return GetCell(cellX, cellY).LocalAccelerationFromGravity;
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


            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    newCells[i, j] = new GasCell();
                }
            }

            //Parallel.For(0, Width, i =>
            for (int i = 0; i < Width; i++)
            {
                try
                {

                    for (int j = 0; j < Height; j++)
                    {
                        GasCell updatedCell = _cells[i, j];
                        // Compute the source position (in grid coordinates) based on the updated velocity.
                        float srcX = i + 0.5f + (updatedCell.Velocity.X * deltaT) / CellSize;
                        float srcY = j + 0.5f + (updatedCell.Velocity.Y * deltaT) / CellSize;

                        var sampleX = (int)Math.Floor(srcX);
                        var sampleY = (int)Math.Floor(srcY);

                        double reldistanceSum = 0f;

                        var maxDist = float.MinValue;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                var sdX = sampleX + dx;
                                var sdY = sampleY + dy;
                                var diffX = srcX - (sdX + 0.5f);
                                var diffY = srcY - (sdY + 0.5f);
                                float dist = (new Vector2(diffX, diffY)).Length();
                                reldistanceSum += dist;
                                if (maxDist < dist)
                                    maxDist = dist;
                            }
                        }
                        reldistanceSum = 9 * maxDist - reldistanceSum;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                var sdX = sampleX + dx;
                                var sdY = sampleY + dy;

                                var diffX = srcX - (sdX + 0.5f);
                                var diffY = srcY - (sdY + 0.5f);
                                float dist = (new Vector2(diffX, diffY)).Length();
                                var targetCell = newCells[WrapIndex(sdX, Width), WrapIndex(sdY, Height)];

                                float fact = (maxDist - dist) / (float)reldistanceSum;

                                targetCell.Mass += fact * updatedCell.Mass;
                                if (((float)targetCell.Mass + (float)updatedCell.Mass * fact) > 0.3)
                                { 
                                    //targetCell.Velocity = (targetCell.Velocity * (float)targetCell.Mass + updatedCell.Velocity * (float)updatedCell.Mass * fact) / ((float)targetCell.Mass + (float)updatedCell.Mass * fact);
                                    targetCell.Pressure = (targetCell.Pressure * (float)targetCell.Mass + updatedCell.Pressure * (float)updatedCell.Mass * fact) / ((float)targetCell.Mass + (float)updatedCell.Mass * fact);
                                    targetCell.Temperature = (targetCell.Temperature * (float)targetCell.Mass + updatedCell.Temperature * (float)updatedCell.Mass * fact) / ((float)targetCell.Mass + (float)updatedCell.Mass * fact);
                                }
                                targetCell.Density = targetCell.Mass / CellSize / CellSize;
                            }
                        }

                        newCells[i, j].Velocity = updatedCell.Velocity;
                        newCells[i, j].LocalAccelerationFromGravity = updatedCell.LocalAccelerationFromGravity;
                    }

                }
                catch (Exception ex)
                {

                }
            }//);

            _cells = newCells;
        }

    }
}
