using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TempRl
{
    //Wrapper around System.Random to make access slightly easier
    public class Dice
    {
        static Random _random = new Random();
        public static Random Random
        {
            get
            {
                return _random;
            }
        }

        public static int Next(int max)
        {
            return _random.Next(max);
        }

        public static int Next(int min, int max)
        {
            return _random.Next(min, max);
        }

        public static double NextDouble()
        {
            return _random.NextDouble();
        }
    }
}
