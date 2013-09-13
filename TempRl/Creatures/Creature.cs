using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace TempRl
{
    //Base class for all creatures. Contains all the common stuff for creatures.
    //Specific creatures inherit from this class and override the methods to provide their own implementation
    public class Creature
    {
        //X and Y position of the creature on the map
        public int X 
        {
            get
            {
                return _tile.X;
            }
        }

        public int Y
        {
            get
            {
                return _tile.Y;
            }
        }

        /// <summary>
        /// Color the creature will be drawn with
        /// </summary>
        public virtual Color Color
        {
            get
            {
                return Color.Red;
            }
        }

        /// <summary>
        /// For multi-tile creatures (eg snakes) allows the creature to provide a different color
        /// per tile
        /// </summary>
        public virtual Color GetColorAt(Tile tile)
        {
            return Color;
        }

        public virtual void Render(Graphics g, Tile tile)
        {
            g.FillEllipse(new SolidBrush(GetColorAt(tile)), tile.X * 5 + 1, tile.Y * 5 + 1, 3, 3);
        }

        /// <summary>
        /// Places the creature on the map for the first time
        /// </summary>
        public virtual bool Place(Tile tile)
        {
            _map = tile.Map;
            _map.Creature(this);
            _tile = tile;
            tile.Creature = this;
            return true;
        }

        protected Tile _tile = null;
        /// <summary>
        /// The Tile this creature is on
        /// </summary>
        public Tile Tile
        {
            get
            {
                return _tile;
            }
        }

        protected Stack<Sounding> _heardSounds = new Stack<Sounding>();

        //todo: probably add a sound class that can store more info: path to sound origin, sound type, volume, etc
        public void HearSound(Sounding echo)
        {
            _heardSounds.Push(echo);
        }

        public void DoTurn()
        {
            OnTurn();
            _heardSounds.Clear();
        }

        /// <summary>
        /// This method is called on the creature's turn. Creature implementations override this
        /// method to provide their behavior
        /// </summary>
        public virtual void OnTurn()
        {

        }

        //creatures can override this field value to customize their vision radius
        protected int _sightRadius = 8;

        private bool IsOpaque(int x, int y)
        {
            Tile tile = Map.GetTile(x, y);
            if (tile == null)
                return true;
            return tile.IsOpaque;
        }

        /// <summary>
        /// Implementation of recursive shadowcasting FOV
        /// http://roguebasin.roguelikedevelopment.org/index.php?title=FOV_using_recursive_shadowcasting
        /// This should be in it's own class but for now it's just in here
        /// </summary>
        public HashSet<Tile> GetVisibleTiles()
        {
            HashSet<Tile> ret = new HashSet<Tile>();
            ret.Add(Tile);
            OctScan(1, 1, 1, 0, ref ret);
            OctScan(2, 1, -1, 0, ref ret);
            OctScan(3, 1, -1, 0, ref ret);
            OctScan(4, 1, 1, 0, ref ret);
            OctScan(5, 1, 1, 0, ref ret);
            OctScan(6, 1, -1, 0, ref ret);
            OctScan(7, 1, -1, 0, ref ret);
            OctScan(8, 1, 1, 0, ref ret);
            return ret;
        }

        //This is one of those methods that's a black box. I can't remember how it all works now. best to leave
        //it alone and hope it works...
        private void OctScan(int oct, int depth, float slopeStart, float slopeEnd, ref HashSet<Tile> visibleTiles)
        {
            int gx = 0;
            int gy = 0;
            float endX = 0.5f;
            float endY = 0.5f;
            float startX = 0.5f;
            float startY = 0.5f;
            switch (oct)
            {
                case 1:
                    gx = 1;
                    startX = -0.5f;
                    startY = -0.5f;
                    endX = -0.5f;
                    break;
                case 2:
                    gx = -1;
                    startY = -0.5f;
                    break;
                case 3:
                    gy = 1;
                    startY = -0.5f;
                    endX = -0.5f;
                    endY = -0.5f;
                    break;
                case 4:
                    gy = -1;
                    endX = -0.5f;
                    break;
                case 5:
                    endY = -0.5f;
                    gx = -1;
                    break;
                case 6:
                    startX = -0.5f;
                    endX = -0.5f;
                    endY = -0.5f;
                    gx = 1;
                    break;
                case 7:
                    startX = -0.5f;
                    gy = -1;
                    break;
                case 8:
                    startX = -0.5f;
                    startY = -0.5f;
                    endY = -0.5f;
                    gy = 1;
                    break;
            }

            int dx = 0;
            int dy = 0;
            switch (oct)
            {
                case 1:
                case 2:
                    dx = -(int)Math.Round(depth * slopeStart);
                    dy = -depth;
                    break;
                case 3:
                case 4:
                    dx = depth;
                    dy = (int)Math.Round(depth * slopeStart);
                    break;
                case 5:
                case 6:
                    dx = (int)Math.Round(depth * slopeStart);
                    dy = depth;
                    break;
                case 7:
                case 8:
                    dx = -depth;
                    dy = -(int)Math.Round(depth * slopeStart);
                    break;
            }
            int y;
            int x;
            bool first = true;
            while (true)
            {
                x = X + dx;
                y = Y + dy;
                bool atEnd = false;
                switch (oct)
                {
                    case 1:
                    case 5:
                        if (GetSlope(X, Y, x, y) < slopeEnd)
                            atEnd = true;
                        break;
                    case 2:
                    case 6:
                        if (GetSlope(X, Y, x, y) > slopeEnd)
                            atEnd = true;
                        break;
                    case 3:
                    case 7:
                        if (GetSlopeInv(X, Y, x, y) > slopeEnd)
                            atEnd = true;
                        break;
                    case 4:
                    case 8:
                        if (GetSlopeInv(X, Y, x, y) < slopeEnd)
                            atEnd = true;
                        break;
                }
                if (atEnd)
                    break;

                if (!first)
                {
                    if (IsOpaque(x, y))
                    {
                        if (!IsOpaque(x - gx, y - gy))
                        {
                            if (oct == 1 || oct == 2 || oct == 5 || oct == 6)
                                OctScan(oct, depth + 1, slopeStart, GetSlope(X, Y, x + endX, y + endY), ref visibleTiles);
                            else
                                OctScan(oct, depth + 1, slopeStart, GetSlopeInv(X, Y, x + endX, y + endY), ref visibleTiles);
                        }
                    }
                    else
                    {
                        if (IsOpaque(x - gx, y - gy))
                        {
                            if (oct == 1 || oct == 2 || oct == 5 || oct == 6)
                                slopeStart = GetSlope(X, Y, x + startX, y + startY);
                            else
                                slopeStart = GetSlopeInv(X, Y, x + startX, y + startY);
                        }
                    }
                }
                Tile tile = Map.GetTile(x, y);
                if (dx * dx + dy * dy <= _sightRadius * _sightRadius)
                    if (tile != null)
                        visibleTiles.Add(tile);
                dx += gx;
                dy += gy;
                first = false;
            }
            dx -= gx;
            dy -= gy;

            if (depth < _sightRadius)
            {
                if (!IsOpaque(X + dx, Y + dy))
                    OctScan(oct, depth + 1, slopeStart, slopeEnd, ref visibleTiles);
            }
        }

        private float GetSlope(float x, float y, float x2, float y2)
        {
            return (x2 - x) / (y2 - y);
        }

        private float GetSlopeInv(float x, float y, float x2, float y2)
        {
            return (y2 - y) / (x2 - x);
        }

        public HashSet<Creature> GetVisibleEntities()
        {
            //go over all visible tiles and amass the entities on them
            HashSet<Creature> ret = new HashSet<Creature>();
            foreach (Tile tile in GetVisibleTiles())
                if (tile.Creature != null && tile.Creature != this)
                    ret.Add(tile.Creature);
            return ret;
        }

        protected Map _map;
        /// <summary>
        /// Returns the map the creature is on
        /// </summary>
        public Map Map
        {
            get
            {
                return _map;
            }
        }

        /// <summary>
        /// Moves the creature from its current tile to the specified location
        /// </summary>
        /// <returns>true if the move was successful, false if it wasn't</returns>
        public bool MoveTo(int x, int y)
        {
            Tile moveTo = _map.GetTile(x, y);
            if (moveTo == null)
                return false;
            if (!moveTo.IsPassable)
                return false;
            if (moveTo.Creature != null)
                return false;
            _tile.Creature = null;
            moveTo.Creature = this;
            _tile = moveTo;
            return true;
        }

        /// <summary>
        /// Moves the creature one tile in the specified direction
        /// </summary>
        public bool Move(CompassPoint compassPoint)
        {
            Tile moveTo = Map.GetRelativeTile(Tile, compassPoint);
            return MoveTo(moveTo.X, moveTo.Y);
        }

        public int _idleTime = 0;
        public bool IsAlive=true;
    }

}
