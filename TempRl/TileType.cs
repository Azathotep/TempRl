using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TempRl
{
    public enum TileType
    {
        None,
        SolidRock,
        StoneWall,
        StoneFloor,
        Chasm,
        Lava,
        WindowNS,
        WindowEW,
        HidingHole,
        GoldOre
    }

    public class TileTypeInfo
    {
        public static bool IsFloor(TileType type)
        {
            
            switch (type)
            {
                case TileType.StoneFloor:
                    return true;
            }
            return false;
        }

        public static bool IsOpaque(TileType type)
        {
            switch (type)
            {
                case TileType.SolidRock:
                case TileType.StoneWall:
                    return false;
            }
            return true;
        }

        public static bool IsPassable(TileType type)
        {

            switch (type)
            {
                case TileType.StoneFloor:
                case TileType.HidingHole:
                    return true;
            }
            return false;
        }

        public static bool IsWall(TileType type)
        {

            switch (type)
            {
                case TileType.StoneWall:
                    return true;
            }
            return false;
        }
    }
}
