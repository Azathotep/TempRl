using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TempRl
{
    /// <summary>
    /// Provides methods for handling directions
    /// </summary>
    public class Compass
    {
        public static CompassPoint GetOppositeDirection(CompassPoint direction)
        {
            switch (direction)
            {
                case CompassPoint.North:
                    return CompassPoint.South;
                case CompassPoint.East:
                    return CompassPoint.West;
                case CompassPoint.South:
                    return CompassPoint.North;
                case CompassPoint.West:
                    return CompassPoint.East;
            }
            return CompassPoint.East;
        }

        /// <summary>
        /// Returns the direction of an end point from a start point. The method assumes the points are
        /// aligned on the X or Y axis and that they are different.
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public static CompassPoint GetDirectionOf(Point startPoint, Point endPoint)
        {
            if (endPoint.X > startPoint.X)
                return CompassPoint.East;
            if (endPoint.X < startPoint.X)
                return CompassPoint.West;
            if (endPoint.Y > startPoint.Y)
                return CompassPoint.South;
            return CompassPoint.North;
        }

        /// <summary>
        /// Returns the direction to the right of the specified direction
        /// </summary>
        public static CompassPoint GetRightDirection(CompassPoint direction)
        {
            switch (direction)
            {
                case CompassPoint.North:
                    return CompassPoint.East;
                case CompassPoint.East:
                    return CompassPoint.South;
                case CompassPoint.South:
                    return CompassPoint.West;
                case CompassPoint.West:
                    return CompassPoint.North;
            }
            return CompassPoint.North;
        }

        /// <summary>
        /// Returns the direction to the left of the specified direction
        /// </summary>
        public static CompassPoint GetLeftDirection(CompassPoint direction)
        {
            return GetOppositeDirection(GetRightDirection(direction));
        }

        public static CompassPoint Rotate180(CompassPoint direction)
        {
            return Rotate90(Rotate90(direction));
        }

        public static CompassPoint Rotate270(CompassPoint direction)
        {
            return Rotate90(Rotate180(direction));
        }

        public static CompassPoint Rotate90(CompassPoint direction)
        {
            switch (direction)
            {
                case CompassPoint.North:
                    return CompassPoint.East;
                case CompassPoint.East:
                    return CompassPoint.South;
                case CompassPoint.South:
                    return CompassPoint.West;
                default:
                    return CompassPoint.North;
            }
        }

        /// <summary>
        /// Returns a unit vector of the specified direction
        /// </summary>
        public static Point GetDirectionVector(CompassPoint direction)
        {
            switch (direction)
            {
                case CompassPoint.North:
                    return new Point(0, -1);
                case CompassPoint.East:
                    return new Point(1, 0);
                case CompassPoint.South:
                    return new Point(0, 1);
                case CompassPoint.West:
                    return new Point(-1, 0);
            }
            return Point.Empty;
        }
    }
}
