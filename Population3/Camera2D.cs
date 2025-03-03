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
                var p = Position;
                p.X -= rightStick.X * GameConstants.CameraMoveSpeed / Zoom;
                p.Y += rightStick.Y * GameConstants.CameraMoveSpeed / Zoom;
                Position = p;

                if (gamePadState.Buttons.RightShoulder == ButtonState.Pressed)
                    Zoom *= GameConstants.ZoomInFactor;
                if (gamePadState.Buttons.LeftShoulder == ButtonState.Pressed)
                    Zoom *= GameConstants.ZoomOutFactor;
            }

            Vector2 screenCenter = new Vector2(screenWidth / 2, screenHeight / 2);
            CurrentTransform = Matrix.CreateTranslation(new Vector3(Position, 0)) *
                        Matrix.CreateScale(Zoom) *
                        Matrix.CreateTranslation(new Vector3(screenCenter, 0));
        }
    }
}
