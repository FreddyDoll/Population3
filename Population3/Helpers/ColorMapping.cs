using Microsoft.Xna.Framework;

namespace Population3
{
    public static class ColorMapping
    {
        public static Color FloatToColor(float value, float lowerLim, float upperLim)
        {
            float t = MathHelper.Clamp((value - lowerLim) / (upperLim - lowerLim), 0f, 1f);
            return new Color((byte)(t * GameConstants.ColorMaxValue), 0, (byte)((1 - t) * GameConstants.ColorMaxValue));
        }
    }
}
