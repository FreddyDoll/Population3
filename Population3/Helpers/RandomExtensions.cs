using System;
using Microsoft.Xna.Framework;

namespace Population3
{
    public static class RandomExtensions
    {
        public static Vector2 NextVectorRect(this Random random)
        {
            return new Vector2(random.NextSingle() * 2.0f - 1.0f, random.NextSingle() * 2.0f - 1.0f);
        }

        public static Vector2 RandomVector(this Random random, float xMin, float xMax, float yMin, float yMax)
        {
            return new Vector2(
                random.NextSingle() * (xMax - xMin) + xMin,
                random.NextSingle() * (yMax - yMin) + yMin
            );
        }
        public static float NextSingle(this Random random, float min, float max)
        {
            float range = max - min;
            float ret = min + (float)random.NextSingle() * range;
            return ret;
        }
    }
}
