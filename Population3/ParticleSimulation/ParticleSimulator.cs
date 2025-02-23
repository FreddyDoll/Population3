using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Population3
{
    public class ParticleSimulator
    {
        private readonly RectangleF _bounds;

        public ParticleSimulator(RectangleF bounds)
        {
            _bounds = bounds;
        }

        public void Update(List<PointMass> particles, GasGrid gasDistribution, float deltaT)
        {
            // Build the spatial structure (quadtree or similar) for efficient neighbour queries.
            var tree = new PositionCache<PointMass>(_bounds);
            tree.Build(particles.Where(p => p.Mass >= GameConstants.MinimumMassForQuadtree && !p.Merged));

            ProcessCollisions(tree, particles);
            UpdatePhysics(tree, deltaT, particles, gasDistribution);
        }

        private void ProcessCollisions(PositionCache<PointMass> treeForCollision, List<PointMass> particles)
        {
            var mergeEvents = new ConcurrentBag<(PointMass a, PointMass b)>();

            Parallel.For(0, particles.Count, i =>
            {
                var particle = particles[i];
                if (particle.Merged)
                    return;

                var candidates = treeForCollision.GetInRadius(particle.Position, particle.Radius * GameConstants.CollisionMultiplier);
                foreach (var candidate in candidates)
                {
                    float radiiSum = particle.Radius + candidate.Radius;
                    float distanceSq = GetWrappedDifference(particle.Position, candidate.Position).LengthSquared();
                    if (distanceSq < radiiSum * radiiSum)
                    {
                        if (particle.Id.CompareTo(candidate.Id) > 0)
                        {
                            mergeEvents.Add((particle, candidate));
                        }
                    }
                }
            });

            foreach (var (a, b) in mergeEvents)
            {
                if (!a.Merged && !b.Merged)
                {
                    var mergedParticle = PointMass.Merge(a, b);
                    a.Mass = mergedParticle.Mass;
                    a.Position = mergedParticle.Position;
                    a.Velocity = mergedParticle.Velocity;
                    a.Density = mergedParticle.Density;
                    b.Merged = true;
                }
            }
        }

        private void UpdatePhysics(PositionCache<PointMass> treeForGravity, float deltaT, List<PointMass> particles, GasGrid gasGrid)
        {
            Parallel.For(0, particles.Count, i =>
            {
                var particle = particles[i];
                if (particle.Merged)
                    return;

                // Existing gravitational force from nearby particles.
                Vector2 netForce = AddGravityForces(treeForGravity, particle);

                // Get local gas acceleration and convert to force (F = m * a).
                Vector2 gasAcceleration = gasGrid.GetGasAccelerationAt(particle.Position);
                netForce += particle.Mass * gasAcceleration;

                // Apply the combined force to the particle.
                particle.CurrentForce = netForce;
                particle = particle.ApplyForceAndIntegrate(deltaT);
                particle.Position = Wrap(particle.Position);
                particles[i] = particle;
            });
        }
        private Vector2 AddGravityForces(PositionCache<PointMass> tree, PointMass particle)
        {
            Vector2 netForce = Vector2.Zero;
            var neighbours = tree.GetInRadius(particle.Position, GameConstants.GravityNeighborRadius);
            foreach (var neighbor in neighbours)
            {
                if (particle.Equals(neighbor))
                    continue;

                Vector2 direction = GetWrappedDifference(neighbor.Position, particle.Position);
                float distanceSquared = direction.LengthSquared();
                if (distanceSquared < GameConstants.MinimumDistanceSquared)
                    distanceSquared = GameConstants.MinimumDistanceSquared;

                float forceMagnitude = GameConstants.GravitationalConstant * particle.Mass * neighbor.Mass / distanceSquared;
                netForce += Vector2.Normalize(direction) * forceMagnitude;
            }
            return netForce;
        }

        private Vector2 GetWrappedDifference(Vector2 a, Vector2 b)
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

        private Vector2 Wrap(Vector2 position)
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
