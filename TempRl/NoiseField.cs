using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TempRl
{
    /// <summary>
    /// Represents a field of random noise. Can be used to produce fields of perlin noise.
    /// </summary>
    public class NoiseField
    {
        int _width;
        int _height;
        double[,] _values;
        /// <summary>
        /// Constructor. Initializes the field to zero values
        /// </summary>
        public NoiseField(int width, int height)
        {
            _width = width;
            _height = height;
            _values = new double[width, height];
        }

        /// <summary>
        /// Sets all cells of the field to random values between 0 and 1
        /// </summary>
        public void GenerateRandomNoise()
        {
            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                    _values[x, y] = Dice.NextDouble();
        }

        /// <summary>
        /// Adds the cells of another noise field to the cells of this one
        /// </summary>
        public void Add(NoiseField n)
        {
            if (n._width != _width || n._height != _height)
                throw new Exception();
            for (int y = 0; y < n._height; y++)
                for (int x = 0; x < n._width; x++)
                    _values[x, y] += n._values[x, y];
        }

        /// <summary>
        /// Returns the n% highest value from the field
        /// Similar to picking the nth highest value, but in this case instead of asking for
        /// n, a percentage is requested when nPercent = n / total number of values in field
        /// Eg if the field contained 10 values: 1,1,2,3,4,4,4,5,6,6 the 80% value is 5
        /// </summary>
        /// <param name="nPercent">between 0 and 1</param>
        public double NthValue(double nPercent)
        {
            List<double> list = new List<double>();
            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                    list.Add(_values[x, y]);
            list = (from l in list orderby l select l).ToList();
            int total = _height * _width;
            int nth = (int)(nPercent * total);
            return list[nth];
        }

        /// <summary>
        /// Generates another field representing a perlin noise style octave of the existing data
        /// in this field
        /// </summary>
        /// <param name="frequency">sampling frequency of the existing field</param>
        /// <param name="amp">Amplification factor to amplify resulting cells</param>
        /// <returns></returns>
        public NoiseField GenerateOctave(double frequency, double amp)
        {
            NoiseField ret = new NoiseField(_width, _height);
            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                {
                    double xpos = x * frequency;
                    double ypos = y * frequency;
                    int xTile = (int)xpos;
                    int yTile = (int)ypos;
                    double xd = xpos - xTile;
                    double yd = ypos - yTile;
                    int xTileN = (xTile + 1) % _width;
                    int yTileN = (yTile + 1) % _height;
                    //obtain the sample point value by interpolation
                    //trying to find the value of X given the four corners:
                    /*
                     *   3..a.4
                     *   .    .
                     *   .  x .
                     *   2..b.5
                     */
                    //where xd and yd is the offset of x from the top left corner
                    //first interpolate 3 & 4 to obtain a. Then interpolate 2 and 5 to obtain b.
                    //Finally interpolate a and b to obtain an approximation of x.
                    double xi1 = Interpolate(_values[xTile, yTile], _values[xTileN, yTile], xd);
                    double xi2 = Interpolate(_values[xTile, yTileN], _values[xTileN, yTileN], xd);
                    double res = Interpolate(xi1, xi2, yd);
                    ret._values[x, y] = res * amp;
                }
            return ret;
        }

        /// <summary>
        /// Linear interpolation
        /// </summary>
        double Interpolate(double a, double b, double t)
        {
            return (1 - t) * a + t * b;
        }

        public double[,] Values
        {
            get
            {
                return _values;
            }
        }

        /// <summary>
        /// Normalizes all cells in the field (sets them to between 0 and 1)
        /// </summary>
        public void Normalize()
        {
            double? max = null;
            double? min = null;
            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                {
                    if (!max.HasValue || _values[x, y] > max)
                        max = _values[x, y];
                    if (!min.HasValue || _values[x, y] < min)
                        min = _values[x, y];
                }
            double diff = max.Value - min.Value;
            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                    _values[x, y] = (_values[x, y] - min.Value) / diff;
        }
    }
}
