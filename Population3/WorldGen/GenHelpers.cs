using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Population3.WorldGen
{
    public class GenHelpers
    {


        /// <summary>
        /// Creates a texture representing a fading circle.
        /// </summary>
        public static Texture2D CreateFadingCircle(GraphicsDevice graphicsDevice, Color color, int radius)
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
