using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Population3
{
    public class PointMass : IHavePosition
    {
        public Guid Id { get; } = Guid.NewGuid();
        public bool Merged { get; set; } = false;
        public Vector2 Position { get; set; }
        public Vector2 Velocity;
        public Vector2 CurrentForce;
        public float Mass;

        // Density property with a default value
        public float Density { get; set; } = 1.0f;

        public Texture2D Texture;

        // Compute the effective radius based on the mass and density.
        // For a circle: Area = Mass / Density = π * r^2  =>  r = sqrt((Mass / Density) / π)
        public float Radius => (float)Math.Sqrt((Mass / Density) / Math.PI);

        public PointMass ApplyForceAndIntegrate(float deltaT)
        {
            Velocity += CurrentForce * deltaT / Mass;
            Position += Velocity * deltaT;
            CurrentForce = Vector2.Zero;
            return this;
        }

        public static PointMass Merge(PointMass a, PointMass b)
        {
            float totalMass = a.Mass + b.Mass;

            // Compute the individual "areas" of the particles (area = mass / density)
            float areaA = a.Mass / a.Density;
            float areaB = b.Mass / b.Density;
            float totalArea = areaA + areaB;

            // The new density is calculated from the total mass divided by the total area.
            float newDensity = totalMass / totalArea;

            return new PointMass
            {
                Mass = totalMass,
                Position = (a.Position * a.Mass + b.Position * b.Mass) / totalMass,
                Velocity = (a.Velocity * a.Mass + b.Velocity * b.Mass) / totalMass,
                Texture = a.Texture, // Use a.Texture as a placeholder.
                Density = newDensity
            };
        }
    }
}
