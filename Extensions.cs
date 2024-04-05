using Microsoft.Xna.Framework;

namespace Lemmings2024
{
    public static class Extensions
    {
        public static bool IsWalkable(this Color color)
        {
            return color.A == 255;
        }

        public static bool IsWater(this Color color)
        {
            return color.A > 0  && color.A < 255;
        }
    }
}
