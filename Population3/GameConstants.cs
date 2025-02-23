namespace Population3
{
    public static class GameConstants
    {
        // Screen settings
        public const int ScreenWidth = 2560;
        public const int ScreenHeight = 1440;

        // Simulation bounds
        public const float SimulationSize = 2_000_000f;

        //Gas Constant for Field Simulation
        public const float GasConstant = 28705000.0f;

        // Gravitational constant
        public const float GravitationalConstant = 1_000_000f;

        // Random seed for reproducibility
        public const int RandomSeed = 2;

        // Camera settings
        public const float CameraMoveSpeed = 10f;
        public const float InitialZoom = 0.001f;
        public const float ZoomInFactor = 1.005f;
        public const float ZoomOutFactor = 0.995f;

        // Additional constants used in physics and collision
        public const float MinimumMassForQuadtree = 10.0f;
        public const float GravityNeighborRadius = 600000f;
        public const float MinimumDistanceSquared = 1f;
    }
}
