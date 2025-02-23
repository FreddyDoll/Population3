using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Population3
{
    /// <summary>
    /// A simple rectangle struct for 2D bounds.
    /// </summary>
    public struct RectangleF
    {
        public float X, Y, Width, Height;

        public RectangleF(float x, float y, float width, float height)
        {
            X = x; Y = y; Width = width; Height = height;
        }

        public bool Contains(Vector2 point)
        {
            return point.X >= X && point.X <= X + Width &&
                   point.Y >= Y && point.Y <= Y + Height;
        }

        public bool IntersectsCircle(Vector2 center, float radius)
        {
            // Find the closest point within the rectangle to the circle's center.
            float closestX = Math.Clamp(center.X, X, X + Width);
            float closestY = Math.Clamp(center.Y, Y, Y + Height);
            float distanceSquared = (center.X - closestX) * (center.X - closestX) +
                                    (center.Y - closestY) * (center.Y - closestY);
            return distanceSquared <= radius * radius;
        }
    }
}