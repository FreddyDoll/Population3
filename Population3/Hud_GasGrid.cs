using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharpDX.Direct3D9;

namespace Population3
{
    public enum VisualizationLayer
    {
        Mass,
        Density,
        Temperature,
        Pressure
    }

    /// <summary>
    /// Draws name and Legend of the GasGrid. Also stores it
    /// </summary>
    public class Hud_GasGrid
    {
        public VisualizationLayer CurrentLayer { get; private set; }
        private GamePadState _previousGamePadState;
        private readonly GasGrid _gasGrid;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteFont _hudFont;

        public double MaxMassPerCell { get; set; } = GameConstants.MaxMassPerCell;

        public Hud_GasGrid(GasGrid gasGrid, GraphicsDevice graphicsDevice, SpriteFont hudFont)
        {
            _gasGrid = gasGrid;
            _graphicsDevice = graphicsDevice;
            _hudFont = hudFont;
            CurrentLayer = VisualizationLayer.Mass;
            _previousGamePadState = GamePad.GetState(PlayerIndex.One);
        }

        public void Update(GamePadState gamePadState)
        {
            // Cycle to next layer on Right press
            if (_previousGamePadState.DPad.Right == ButtonState.Released &&
                gamePadState.DPad.Right == ButtonState.Pressed)
            {
                int nextIndex = (((int)CurrentLayer + 1) % Enum.GetNames(typeof(VisualizationLayer)).Length);
                CurrentLayer = (VisualizationLayer)nextIndex;
            }
            // Cycle to previous layer on Left press
            if (_previousGamePadState.DPad.Left == ButtonState.Released &&
                gamePadState.DPad.Left == ButtonState.Pressed)
            {
                int index = (int)CurrentLayer - 1;
                if (index < 0)
                    index = Enum.GetNames(typeof(VisualizationLayer)).Length - 1;
                CurrentLayer = (VisualizationLayer)index;
            }
            _previousGamePadState = gamePadState;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw the current layer label.
            string layerName = "Layer: " + CurrentLayer.ToString();
            spriteBatch.DrawString(_hudFont, layerName, new Vector2(GameConstants.HUDOffsetX, GameConstants.HUDOffsetY), Color.White);

            // Determine the min/max values based on the current layer.
            GetCurrentLayerRange(out float minValueHUD, out float maxValueHUD);

            // Create gradient bar texture.
            //TODO: this is not showing
            int gradientBarWidth = GameConstants.GradientBarWidth;
            int gradientBarHeight = GameConstants.GradientBarHeight;
            Color[] gradientColors = new Color[gradientBarWidth * gradientBarHeight];
            for (int x = 0; x < gradientBarWidth; x++)
            {
                float t = (float)x / (gradientBarWidth - 1);
                float value = MathHelper.Lerp(minValueHUD, maxValueHUD, t);
                Color color = MapFloatToColor(value, minValueHUD, maxValueHUD);
                for (int y = 0; y < gradientBarHeight; y++)
                {
                    gradientColors[y * gradientBarWidth + x] = color;
                }
            }

            // Use a temporary texture (dispose after drawing).
            using (Texture2D gradientTexture = new Texture2D(_graphicsDevice, gradientBarWidth, gradientBarHeight))
            {
                gradientTexture.SetData(gradientColors);
                spriteBatch.Draw(gradientTexture,
                    new Rectangle((int)GameConstants.HUDOffsetX, (int)(GameConstants.HUDOffsetY * 4), gradientBarWidth, gradientBarHeight),
                    Color.White);
            }

            var stats = _gasGrid.GetMassStatsMass();
            // Draw min and max labels.
            string minText = "Total: " + stats.totalMass.ToString("F2");
            string maxText = "Max Mass: " + stats.maxMass.ToString("F2");
            spriteBatch.DrawString(_hudFont, minText,
                new Vector2(GameConstants.HUDOffsetX, GameConstants.HUDOffsetY * 4 + gradientBarHeight + GameConstants.HUDOffsetY / 2),
                Color.White);
            Vector2 maxTextSize = _hudFont.MeasureString(maxText);
            spriteBatch.DrawString(_hudFont, maxText,
                new Vector2(GameConstants.HUDOffsetX + gradientBarWidth - maxTextSize.X,
                            GameConstants.HUDOffsetY * 4 + gradientBarHeight + GameConstants.HUDOffsetY / 2),
                Color.White);
        }

        public void SelectPropertyFromCurrentLayer(GasCell cell, out float p)
        {
            var propertyValue = 0.0;
            switch (CurrentLayer)
            {
                case VisualizationLayer.Mass:
                    propertyValue = cell.Mass;
                    break;
                case VisualizationLayer.Density:
                    propertyValue = cell.Density;
                    break;
                case VisualizationLayer.Temperature:
                    propertyValue = cell.Temperature;
                    break;
                case VisualizationLayer.Pressure:
                    propertyValue = cell.Pressure;
                    break;
                default: 
                    propertyValue = 0;
                    break;
            }
            p = (float)propertyValue;
        }

        public void GetCurrentLayerRange(out float minValue, out float maxValue)
        {
            float volume = _gasGrid.CellSize * _gasGrid.CellSize;
            switch (CurrentLayer)
            {
                case VisualizationLayer.Mass:
                    minValue = 0f;
                    maxValue = (float)MaxMassPerCell;
                    break;
                case VisualizationLayer.Density:
                    minValue = 0;
                    maxValue = (float)MaxMassPerCell / volume;
                    break;
                case VisualizationLayer.Temperature:
                    minValue = 10f;
                    maxValue = 30f;
                    break;
                case VisualizationLayer.Pressure:
                    float minDensity = 0;
                    float maxDensity = (float)MaxMassPerCell / volume;
                    minValue = minDensity * (float)MaxMassPerCell * 10f;
                    maxValue = maxDensity * (float)MaxMassPerCell * 30f;
                    break;
                default:
                    minValue = 0f;
                    maxValue = 1f;
                    break;
            }
        }

        private Color MapFloatToColor(float value, float lowerLim, float upperLim)
        {
            float t = MathHelper.Clamp((value - lowerLim) / (upperLim - lowerLim), 0f, 1f);
            return new Color((byte)(t * GameConstants.ColorMaxValue), 0, (byte)((1 - t) * GameConstants.ColorMaxValue));
        }
    }
}
