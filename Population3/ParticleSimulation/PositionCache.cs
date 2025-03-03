using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Population3
{
    public interface IHavePosition
    {
        Vector2 Position { get; }
    }

    /// <summary>
    /// A simple quadtree implementation for objects that implement IHavePosition.
    /// </summary>
    public class Quadtree<T> where T : IHavePosition
    {
        private readonly int capacity;
        private readonly RectangleF bounds;
        private List<T> items;
        private bool divided;
        private Quadtree<T> northwest;
        private Quadtree<T> northeast;
        private Quadtree<T> southwest;
        private Quadtree<T> southeast;

        public Quadtree(RectangleF bounds, int capacity = 4)
        {
            this.bounds = bounds;
            this.capacity = capacity;
            items = new List<T>();
            divided = false;
        }

        public bool Insert(T item)
        {
            if (!bounds.Contains(item.Position))
                return false;

            if (items.Count < capacity)
            {
                items.Add(item);
                return true;
            }
            else
            {
                if (!divided)
                    Subdivide();

                if (northwest.Insert(item)) return true;
                if (northeast.Insert(item)) return true;
                if (southwest.Insert(item)) return true;
                if (southeast.Insert(item)) return true;
            }
            return false;
        }

        private void Subdivide()
        {
            float x = bounds.X;
            float y = bounds.Y;
            float w = bounds.Width / 2f;
            float h = bounds.Height / 2f;

            northwest = new Quadtree<T>(new RectangleF(x, y, w, h), capacity);
            northeast = new Quadtree<T>(new RectangleF(x + w, y, w, h), capacity);
            southwest = new Quadtree<T>(new RectangleF(x, y + h, w, h), capacity);
            southeast = new Quadtree<T>(new RectangleF(x + w, y + h, w, h), capacity);
            divided = true;
        }

        public void Query(Vector2 center, float radius, List<T> found)
        {
            if (!bounds.IntersectsCircle(center, radius))
                return;

            foreach (var item in items)
            {
                if (Vector2.Distance(item.Position, center) <= radius)
                    found.Add(item);
            }

            if (divided)
            {
                northwest.Query(center, radius, found);
                northeast.Query(center, radius, found);
                southwest.Query(center, radius, found);
                southeast.Query(center, radius, found);
            }
        }
    }

    /// <summary>
    /// PositionCache uses a quadtree to allow quick lookup of items within a given radius.
    /// </summary>
    public class PositionCache<T> where T : IHavePosition
    {
        private Quadtree<T> quadtree;
        public RectangleF WorldBounds { get; init; }
        private readonly int capacityPerNode;

        public PositionCache(RectangleF worldBounds, int capacityPerNode = 4)
        {
            this.WorldBounds = worldBounds;
            this.capacityPerNode = capacityPerNode;
            quadtree = new Quadtree<T>(worldBounds, capacityPerNode);
        }

        /// <summary>
        /// Rebuilds the quadtree from scratch with the current positions of all items.
        /// </summary>
        public void Build(IEnumerable<T> items)
        {
            quadtree = new Quadtree<T>(WorldBounds, capacityPerNode);
            foreach (var item in items)
            {
                quadtree.Insert(item);
            }
        }

        /// <summary>
        /// Returns all items within the specified radius of the given position,
        /// taking into account wrapped (toroidal) coordinates.
        /// </summary>
        public List<T> GetInRadius(Vector2 position, float radius)
        {
            List<T> found = new List<T>();
            // Query the primary position.
            quadtree.Query(position, radius, found);

            // Check for wrap-around in X.
            if (position.X - radius < WorldBounds.X)
                quadtree.Query(new Vector2(position.X + WorldBounds.Width, position.Y), radius, found);
            if (position.X + radius > WorldBounds.X + WorldBounds.Width)
                quadtree.Query(new Vector2(position.X - WorldBounds.Width, position.Y), radius, found);

            // Check for wrap-around in Y.
            if (position.Y - radius < WorldBounds.Y)
                quadtree.Query(new Vector2(position.X, position.Y + WorldBounds.Height), radius, found);
            if (position.Y + radius > WorldBounds.Y + WorldBounds.Height)
                quadtree.Query(new Vector2(position.X, position.Y - WorldBounds.Height), radius, found);

            // Check for corner cases.
            if (position.X - radius < WorldBounds.X && position.Y - radius < WorldBounds.Y)
                quadtree.Query(new Vector2(position.X + WorldBounds.Width, position.Y + WorldBounds.Height), radius, found);
            if (position.X + radius > WorldBounds.X + WorldBounds.Width && position.Y - radius < WorldBounds.Y)
                quadtree.Query(new Vector2(position.X - WorldBounds.Width, position.Y + WorldBounds.Height), radius, found);
            if (position.X - radius < WorldBounds.X && position.Y + radius > WorldBounds.Y + WorldBounds.Height)
                quadtree.Query(new Vector2(position.X + WorldBounds.Width, position.Y - WorldBounds.Height), radius, found);
            if (position.X + radius > WorldBounds.X + WorldBounds.Width && position.Y + radius > WorldBounds.Y + WorldBounds.Height)
                quadtree.Query(new Vector2(position.X - WorldBounds.Width, position.Y - WorldBounds.Height), radius, found);

            return found.Distinct().ToList();
        }
    }
}
