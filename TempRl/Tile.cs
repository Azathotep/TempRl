using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace TempRl
{
    public class Tile
    {
        Map _map;
        int _x;
        int _y;
        double _waterLevel = 0;
        double _waterFall = 0;
        TileType _type;

        TileContent _content = null;

        public TileType Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }

        public void EmitSound(int volume)
        {
            _map.EmitSound(this, volume);
        }

        public CompassPoint RelativeTo(Tile other)
        {
            if (X == other.X + 1)
                return CompassPoint.East;
            if (X == other.X - 1)
                return CompassPoint.West;
            if (Y == other.Y + 1)
                return CompassPoint.South;
            return CompassPoint.North;
        }

        public Tile GetNeighbour(CompassPoint direction)
        {
            return _map.GetRelativeTile(this, direction);
        }

        public TileContent Content
        {
            get
            {
                return _content;
            }
            set
            {
                _content = value;
                _content.Tile = this;
            }
        }

        public Tile(Map map, int x, int y)
        {
            _map = map;
            _x = x;
            _y = y;
            _type = TileType.SolidRock;
        }

        public Map Map
        {
            get
            {
                return _map;
            }
        }

        static Color chasmColor = Color.FromArgb(30, 30, 50);
        static Color holeColor = Color.FromArgb(90, 120, 80);

        public void Render(Graphics g, bool isVisible, bool isRemembered)
        {
            if (!isVisible && !isRemembered)
            {
                g.FillRectangle(new SolidBrush(Color.Black), X * 5, Y * 5, 5, 5);
                return;
            }
            Color color = Color.White;
            switch (Type)
            {
                case TileType.StoneFloor:
                    color = Color.LightGray;
                    break;
                case TileType.StoneWall:
                    color = Color.DarkSlateGray;
                    //color = Color.DarkGray;
                    //color = Color.Black;
                    break;
                case TileType.Chasm:
                    color = chasmColor;
                    break;
                case TileType.Lava:
                    color = Color.Orange;
                    break;
                case TileType.SolidRock:
                case TileType.GoldOre:
                    color = Color.DarkGray;
                    break;
                case TileType.WindowEW:
                case TileType.WindowNS:
                    color = Color.Cyan;
                    break;
                case TileType.HidingHole:
                    color = Color.DarkSlateGray;
                    break;
            }
            g.FillRectangle(new SolidBrush(color), _x * 5, _y * 5, 5, 5);

            if (Type == TileType.HidingHole)
                g.FillRectangle(new SolidBrush(Color.Black), _x * 5 + 1, _y * 5 + 1, 4, 4);

            if (IsExit)
            {
                if (Form1.level == 8)
                {
                    g.DrawRectangle(Pens.Red, _x * 5, _y * 5, 4, 4);
                    g.DrawRectangle(Pens.YellowGreen, _x * 5+2, _y * 5+2, 2, 2);
                }
                else
                    g.DrawRectangle(Pens.Red, _x * 5, _y * 5, 4, 4);

            }

            if (Type == TileType.GoldOre)
                g.FillRectangle(new SolidBrush(Color.Gold), _x * 5+1, _y * 5+1, 3, 3);

            //if (Type == TileType.StoneWall)
            //{
            //    Tile north = GetNeighbour(CompassPoint.North);
            //    if (north != null && north.Type == TileType.StoneFloor)
            //        g.FillRectangle(new SolidBrush(Color.DarkSlateGray), _x * 5, _y * 5, 5, 2);

            //    Tile south = GetNeighbour(CompassPoint.South);
            //    if (south != null && south.Type == TileType.StoneFloor)
            //        g.FillRectangle(new SolidBrush(Color.DarkSlateGray), _x * 5, _y * 5+3, 5, 2);

            //    Tile east = GetNeighbour(CompassPoint.East);
            //    if (east != null && east.Type == TileType.StoneFloor)
            //        g.FillRectangle(new SolidBrush(Color.DarkSlateGray), _x * 5 + 3, _y * 5, 2, 5);

            //    Tile west = GetNeighbour(CompassPoint.West);
            //    if (west != null && west.Type == TileType.StoneFloor)
            //        g.FillRectangle(new SolidBrush(Color.DarkSlateGray), _x * 5, _y * 5, 2, 5);
            //}

            //if (WaterLevel > 0.1)
            //{
            //    int opacity = (int)(WaterLevel * 255);
            //    if (opacity > 255)
            //        opacity = 255;
            //    g.FillRectangle(new SolidBrush(Color.FromArgb(opacity, 0, 0, 255)), _x * 5, _y * 5, 5, 5);
            //}

            if (_content != null)
                _content.Render(g, _x, _y);

            if (isVisible)
            {
                Creature e = Creature;
                if (e != null)
                    e.Render(g, this);
            }
            else if (isRemembered)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 0, 0, 0)), _x * 5, _y * 5, 5, 5);
            }
            //g.DrawRectangle(Pens.DarkGray, _x * 5, _y * 5, 5, 5);
        }

        Creature _entity = null;
        public Creature Creature
        {
            get
            {
                return _entity;
            }
            set
            {
                _entity = value;
            }
        }

        public void HearSound(Sounding echo)
        {

        }

        public double WaterLevel
        {
            get
            {
                return _waterLevel;
            }
            set
            {
                _waterLevel = value;
            }
        }

        public double WaterFall
        {
            get
            {
                return _waterFall;
            }
            set
            {
                _waterFall = value;
            }
        }

        public bool IsWall
        {
            get
            {
                return TileTypeInfo.IsWall(_type);
            }
        }

        public bool IsOpaque
        {
            get
            {
                if (_content != null && _content.IsOpaque)
                    return true;
                return !TileTypeInfo.IsOpaque(_type);
            }
        }

        public bool IsPassable
        {
            get
            {
                if (_content != null && !_content.IsPassable)
                    return false;
                return TileTypeInfo.IsPassable(_type);
            }
        }

        public bool IsFloor
        {
            get
            {
                return TileTypeInfo.IsFloor(_type);
            }
        }

        public int X
        {
            get
            {
                return _x;
            }
        }

        public int Y
        {
            get
            {
                return _y;
            }
        }

        public void SetPosition(int x, int y)
        {
            _x = x;
            _y = y;
        }

        public bool IsExit=false;
    }
}
