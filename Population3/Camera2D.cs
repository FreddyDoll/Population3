using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Population3
{
    /// <summary>
    /// Encapsulates camera logic for position, zoom, and transform matrix calculation.
    /// </summary>
    public class Camera2D
    {
        public Vector2 Position { get; set; }
        public float Zoom { get; set; }
        public Matrix CurrentTransform { get; private set; }

        public Camera2D(Vector2 initialPosition, float initialZoom)
        {
            Position = initialPosition;
            Zoom = initialZoom;
            CurrentTransform = Matrix.Identity;
        }

        /// <summary>
        /// Updates the camera based on gamepad input and screen dimensions.
        /// </summary>
        public void Update(GamePadState gamePadState, int screenWidth, int screenHeight)
        {
            if (gamePadState.IsConnected)
            {
                Vector2 rightStick = gamePadState.ThumbSticks.Right;

                // Update camera position
                Position -= new Vector2(rightStick.X, -rightStick.Y) * (GameConstants.CameraMoveSpeed / Zoom);

                // Zoom using triggers LT/RT
                float zoomChange = gamePadState.Triggers.Right - gamePadState.Triggers.Left;

                Zoom *= MathHelper.Lerp(1f, GameConstants.ZoomInFactor, zoomChange);
                Zoom *= MathHelper.Lerp(1f, GameConstants.ZoomOutFactor, -zoomChange);

            }

            Vector2 screenCenter = new Vector2(screenWidth / 2f, screenHeight / 2f);
            CurrentTransform =
                Matrix.CreateTranslation(Position.X, Position.Y, 0) *
                Matrix.CreateScale(Zoom) *
                Matrix.CreateTranslation(screenCenter.X, screenCenter.Y, 0);
        }

        public Vector2 WorldToScreen(Vector2 worldPosition)
        {
            return Vector2.Transform(worldPosition, CurrentTransform);
        }

        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            return Vector2.Transform(screenPosition, Matrix.Invert(CurrentTransform));
        }
    }
}
