using Microsoft.Xna.Framework;

namespace Population3
{
    // Gas simulation classes

    public class GasCell
    {
        // The mass contained in this cell.
        public float Mass;
        // Density (mass per unit volume; here, volume is implicitly 1 per cell)
        public float Density;
        // Pressure in the cell.
        public float Pressure;
        // Temperature in Kelvin.
        public float Temperature;
        // Velocity vector for the gas in this cell.
        public Vector2 Velocity;
        public Vector2 LocalAccelerationFromGravity;

        public GasCell()
        {
            // Initialize with some defaults.
            Mass = 0f;
            Density = 1f;
            Pressure = 0f;
            Temperature = 300f; // room temperature
            Velocity = Vector2.Zero;
            LocalAccelerationFromGravity = Vector2.Zero;
        }
    }
}
