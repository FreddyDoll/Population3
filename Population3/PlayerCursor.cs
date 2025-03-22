using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using MonoGame.Extended;

namespace Population3
{

        public static class Primitives
        {
            public static void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness = 2f)
            {
                spriteBatch.DrawLine(start, end, color, thickness);
            }

            public static void DrawArrow(SpriteBatch spriteBatch, Vector2 from, Vector2 to, Color color)
            {
                spriteBatch.DrawLine(from, to, color, 2f);

                // Arrowhead
                Vector2 dir = Vector2.Normalize(to - from);
                Vector2 left = new Vector2(-dir.Y, dir.X) * 6f;
                Vector2 right = new Vector2(dir.Y, -dir.X) * 6f;
                Vector2 back = to - dir * 12f;

                spriteBatch.DrawLine(to, back + left, color, 2f);
                spriteBatch.DrawLine(to, back + right, color, 2f);
            }

            public static void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, int segments, Color color, float thickness = 2f)
            {
                spriteBatch.DrawCircle(center, radius, segments, color, thickness);
            }
        }



    public class PlayerCursor
    {
        public PointMass? SelectedMass { get; private set; }
        public bool LockCamToSelected { get; private set; } = true;
        public RahPower ActiveRahPower { get; private set; } = RahPower.None;
        public Vector2 CurrentImpulse { get; private set; } = Vector2.Zero;

        private List<PointMass> _particles;
        private GasGrid _gasGrid;
        private Camera2D _camera;
        private int _selectionIndex = 0;
        private float _selectionRadius = GameConstants.SimulationHalfWidth / 100;

        // Added field to store previous state for detecting button presses.
        private GamePadState _prevGamePadState;

        public PlayerCursor(List<PointMass> particles, GasGrid gasGrid, Camera2D camera)
        {
            _particles = particles;
            _gasGrid = gasGrid;
            _camera = camera;
            // Initialize previous state. Adjust PlayerIndex if needed.
            _prevGamePadState = GamePad.GetState(PlayerIndex.One);
        }

        internal void Update(GamePadState gamePadState, PositionCache<PointMass> tree)
        {
            Vector2 cursorWorldPos = _camera.ScreenToWorld(Vector2.Zero);

            // ---- Toggle Camera Lock ----
            // Only toggle if Y was just pressed
            if (gamePadState.IsButtonDown(Buttons.Y) && !_prevGamePadState.IsButtonDown(Buttons.Y))
                LockCamToSelected = !LockCamToSelected;

            // ---- Selection with LB/RB ----
            List<PointMass> nearby = new();
            float originalRadius = _selectionRadius;
            while (nearby.Count == 0 && _particles.Count != 0)
            {
                nearby = tree.GetInRadius(cursorWorldPos, _selectionRadius);
                _selectionRadius *= 2;
            }
            // Reset selection radius for next frame
            _selectionRadius = originalRadius;

            if (nearby.Count > 0)
            {
                // Check for "just pressed" for selection cycling
                if (gamePadState.IsButtonDown(Buttons.LeftShoulder) && !_prevGamePadState.IsButtonDown(Buttons.LeftShoulder))
                    CycleSelection(nearby, reverse: true);
                else if (gamePadState.IsButtonDown(Buttons.RightShoulder) && !_prevGamePadState.IsButtonDown(Buttons.RightShoulder))
                    CycleSelection(nearby, reverse: false);
            }

            // ---- Input for Impulse Direction ----
            Vector2 stick = gamePadState.ThumbSticks.Left;
            stick.Y *= -1; // Y-axis inversion
            CurrentImpulse = stick * 200000f;

            // ---- Handle Rah Powers ----
            if (SelectedMass != null)
            {
                var mass = SelectedMass;
                Vector2 pos = mass.Position;
                GasCell cell = _gasGrid.GetCellFromWorld(pos);

                if (gamePadState.IsButtonDown(Buttons.A))
                {
                    ActiveRahPower = RahPower.Attract;
                    float absorbAmount = 1.2f;// (float)cell.Mass / 60f;
                    //if (cell.Mass >= absorbAmount)
                    {
                        cell.Mass -= absorbAmount;
                        mass.Mass += absorbAmount;

                        // Check for collapse threshold
                        if (mass.Mass >= 100f) // example critical mass
                        {
                            float fact = 10.0f;

                            float ejected = mass.Mass * fact;
                            mass.Mass -= ejected;
                            cell.Mass += ejected;

                            Vector2 impulse = -CurrentImpulse;
                            mass.Velocity -= impulse * fact;
                            cell.ApplyImpulse(impulse / fact);
                        }
                    }
                }
                else if (gamePadState.IsButtonDown(Buttons.B))
                {
                    ActiveRahPower = RahPower.Dissolve;
                    float lostMass = 1f;
                    if (mass.Mass > lostMass)
                    {
                        mass.Mass -= lostMass;
                        cell.Mass += lostMass;

                        Vector2 impulse = CurrentImpulse;
                        mass.Velocity += impulse;
                        cell.ApplyImpulse(-impulse);
                    }
                }
                else
                {
                    ActiveRahPower = RahPower.None;
                }

                if (LockCamToSelected)
                    _camera.Position = -mass.Position;
            }

            // Update previous state at the end of the frame.
            _prevGamePadState = gamePadState;
        }

        internal void Draw(SpriteBatch spriteBatch)
        {
            if (SelectedMass != null)
            {
                Vector2 screenPos = _camera.WorldToScreen(SelectedMass.Position);

                // Draw selection ring
                float radius = SelectedMass.Radius;
                Primitives.DrawCircle(spriteBatch, screenPos, radius + 5, 24, Color.Yellow);

                if (CurrentImpulse.Length() > 1f)
                {
                    // Draw impulse arrow
                    Vector2 target = screenPos + CurrentImpulse * 0.2f;
                    Primitives.DrawArrow(spriteBatch, screenPos, target, Color.Red);
                }
            }
        }

        private void CycleSelection(List<PointMass> candidates, bool reverse)
        {
            if (candidates.Count == 0) return;

            _selectionIndex += reverse ? -1 : 1;
            if (_selectionIndex < 0) _selectionIndex = candidates.Count - 1;
            if (_selectionIndex >= candidates.Count) _selectionIndex = 0;

            SelectedMass = candidates[_selectionIndex];
        }
    }


    public enum RahPower
    {
        None,
        Attract,
        Dissolve,
    }
}
