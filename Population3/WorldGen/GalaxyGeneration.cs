using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Population3
{

    public static class GalaxyGeneration
    {
        // Galaxy-specific constants

        // Texture radii for particles
        public const int StarTextureRadius = 400;
        public const int ParticleTextureRadius = 25;

        // Particle counts
        public const int StarParticleCount = 1000;
        public const int RegularParticleCount = 1000;

        // Particle mass values
        public const float StarMass = 1000000f;
        public const float RegularParticleMass = 1.0f;

        // Particle density values
        public const float StarDensity = 0.5f;
        public const float RegularParticleDensity = 0.00001f;

        // Particle placement settings
        public const float ExclusionZoneRadius = 300000f;
        public const float ParticlePlacementMultiplier = 0.9f;

        /// <summary>
        /// Generates a list of point masses forming a galaxy.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device used to create textures.</param>
        /// <param name="random">A random number generator.</param>
        /// <returns>A list of PointMass objects representing the galaxy.</returns>
        public static List<PointMass> Generate(GraphicsDevice graphicsDevice, Random random)
        {
            List<PointMass> particles = new List<PointMass>();

            // Create textures using local helper method.
            var starTexture = CreateFadingCircle(graphicsDevice, Color.White, StarTextureRadius);
            var particleTexture = CreateFadingCircle(graphicsDevice, Color.Cyan, ParticleTextureRadius);

            // Define the center of the simulation and disk radius.
            Vector2 center = Vector2.Zero;
            float diskRadius = GameConstants.SimulationSize / 2f;

            // Create the central massive object (e.g., a black hole).
            PointMass centralBlackHole = new PointMass
            {
                Position = center,
                Velocity = Vector2.Zero,
                Texture = starTexture,
                Mass = StarMass * 1000,
                Density = StarDensity * 100000,
            };
            particles.Add(centralBlackHole);

            // Initialize orbiting stars.
            for (int i = 0; i < StarParticleCount; i++)
            {
                particles.Add(CreateOrbitingParticle(starTexture, StarMass, StarDensity, diskRadius, centralBlackHole, random));
            }

            // Initialize orbiting regular particles.
            for (int i = 0; i < RegularParticleCount; i++)
            {
                particles.Add(CreateOrbitingParticle(particleTexture, RegularParticleMass, RegularParticleDensity, diskRadius, centralBlackHole, random));
            }

            return particles;
        }

        /// <summary>
        /// Creates an orbiting particle around a central mass.
        /// </summary>
        private static PointMass CreateOrbitingParticle(Texture2D texture, float mass, float density, float diskRadius, PointMass centralStar, Random random)
        {
            float radius = ExclusionZoneRadius +
                           (float)random.NextDouble() *
                           ((diskRadius - ExclusionZoneRadius) * ParticlePlacementMultiplier);
            float angle = (float)random.NextDouble() * MathHelper.TwoPi;
            Vector2 pos = new Vector2(radius * (float)Math.Cos(angle), radius * (float)Math.Sin(angle));

            // Calculate orbital speed for a circular orbit around the central mass.
            float orbitalSpeed = (float)Math.Sqrt(GameConstants.GravitationalConstant * centralStar.Mass / radius);

            // Set velocity perpendicular to the radius vector.
            Vector2 tangent = new Vector2(-pos.Y, pos.X);
            tangent.Normalize();

            return new PointMass
            {
                Position = pos,
                Velocity = tangent * orbitalSpeed,
                Texture = texture,
                Mass = mass,
                Density = density,
            };
        }

        /// <summary>
        /// Creates a texture representing a fading circle.
        /// </summary>
        private static Texture2D CreateFadingCircle(GraphicsDevice graphicsDevice, Color color, int radius)
        {
            int diameter = radius * 2;
            Texture2D circleTexture = new Texture2D(graphicsDevice, diameter, diameter);
            Color[] data = new Color[diameter * diameter];
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float distance = Vector2.Distance(pos, center);
                    if (distance <= radius)
                    {
                        float fade = 1f - (distance / radius);
                        byte alpha = (byte)(color.A * fade);
                        data[y * diameter + x] = new Color(color.R, color.G, color.B, alpha);
                    }
                    else
                    {
                        data[y * diameter + x] = Color.Transparent;
                    }
                }
            }

            circleTexture.SetData(data);
            return circleTexture;
        }
    }
}
