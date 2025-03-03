using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Population3.Helpers
{
    public static class CoordinateWrapping
    {

        public static Vector2 GetWrappedDifference(RectangleF _bounds,Vector2 a, Vector2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            float halfWidth = _bounds.Width / 2f;
            float halfHeight = _bounds.Height / 2f;

            if (dx > halfWidth)
                dx -= _bounds.Width;
            else if (dx < -halfWidth)
                dx += _bounds.Width;

            if (dy > halfHeight)
                dy -= _bounds.Height;
            else if (dy < -halfHeight)
                dy += _bounds.Height;

            return new Vector2(dx, dy);
        }

        public static Vector2 Wrap(RectangleF _bounds, Vector2 position)
        {
            float width = _bounds.Width;
            float height = _bounds.Height;
            float x = position.X;
            float y = position.Y;

            while (x < _bounds.X)
                x += width;
            while (x >= _bounds.X + width)
                x -= width;
            while (y < _bounds.Y)
                y += height;
            while (y >= _bounds.Y + height)
                y -= height;

            return new Vector2(x, y);
        }
    }
}
