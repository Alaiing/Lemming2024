using System;

namespace Oudidon
{
    public static class CommonRandom
    {
        private static Random _random;
        public static Random Random => _random;

        public static void Init()
        {
            _random = new Random();
        }

        public static void Init(int seed)
        {
            _random = new Random(seed);
        }
    }
}
