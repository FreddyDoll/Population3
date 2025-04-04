﻿using Microsoft.Xna.Framework;

namespace Population3
{
    public static class GameConstants
    {
        // Random seed for reproducibility
        public const int RandomSeed = 2;

        // Screen settings
        public const int ScreenWidth = 2560;
        public const int ScreenHeight = 1440;

        // Drawing
        public const float GasGridAlpha = 0.6f;

        // Camera settings
        public const float CameraMoveSpeed = 10f;
        public const float InitialZoom = 0.001f;
        public const float ZoomInFactor = 1.005f;
        public const float ZoomOutFactor = 0.995f;

        // Simulation
        public const float PhysicsTickTime = 0.05f; //million years
        public const float SimulationHalfWidth = 2_000_000f; //milli Lightyears

        // Particle Sim
        public const float GravitationalConstant = 1.0E+12f;
        public const float GravityNeighborRadius = 100000f;
        public const float MinimumDistanceSquared = 100f;

        // Gas Grid
        public const float GasConstant = 1.0E+3f;
        public const float MaxMassPerCell = 30; //Solar Masses
        public const int GasGridWidth = 100;
        public const int GasGridHeight = 100;

        // HUD
        public const int GradientBarWidth = 600;
        public const int GradientBarHeight = 20;
        public const float HUDOffsetX = 10f;
        public const float HUDOffsetY = 10f;
        public const float CollisionMultiplier = 2.0f;
        public const int ColorMaxValue = 255;
    }
}
