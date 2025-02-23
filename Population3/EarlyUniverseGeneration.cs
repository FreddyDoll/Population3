using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Population3
{
    public static class EarlyUniverseGeneration
    {
        public const int StarTextureRadius = 400;
        public const int StarParticleCount = 1000;
        public const float StarMass = 1000000f;
        public const float StarDensity = 0.5f;

        public static List<PointMass> Generate(GraphicsDevice graphicsDevice, Random random)
        {
            List<PointMass> particles = new List<PointMass>();
            var starTexture = CreateFadingCircle(graphicsDevice, Color.White, StarTextureRadius);
            for (int i = 0; i < StarParticleCount; i++)
            {
                var particle = new PointMass
                {
                    Position = random.NextVectorRect() * GameConstants.SimulationSize,
                    Velocity = random.NextVectorRect() * 10000f,
                    Density = StarDensity,
                    Mass = StarMass,
                    Texture = starTexture,
                };
                particles.Add(particle);
            }

            return particles;
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
