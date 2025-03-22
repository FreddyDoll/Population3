using Microsoft.Xna.Framework;
using System;

namespace Population3
{
    // Gas simulation classes

    public class GasCell
    {
        // The mass contained in this cell.
        public double Mass;
        // Density (mass per unit volume; here, volume is implicitly 1 per cell)
        public double Density;
        // Pressure in the cell.
        public double Pressure;
        // Temperature in Kelvin.
        public double Temperature;
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

        internal void ApplyImpulse(Vector2 impulse)
        {
            Velocity += impulse / (float)Mass;
        }
    }
}
