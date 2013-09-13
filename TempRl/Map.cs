using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace TempRl
{
    public class Map
    {
        Tile[,] _tiles;
        int _width;
        int _height;
        
        public int StairsX;
        public int StairsY;

        public Map(int width, int height)
        {
            _width = width;
            _height = height;
            _tiles = new Tile[width, height];
            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                {
                    _tiles[x, y] = new Tile(this, x, y);
                }
        }

        public int Width
        {
            get
            {
                return _width;
            }
        }

        public int Height
        {
            get
            {
                return _height;
            }
        }

        public Tile GetTile(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _width || y >= _height)
                return null;
            return _tiles[x, y];
        }

        public List<Tile> GetAdjacentTiles(int x, int y)
        {
            List<Tile> ret = new List<Tile>();
            Tile tile = GetTile(x + 1, y);
            if (tile != null)
                ret.Add(tile);
            tile = GetTile(x - 1, y);
            if (tile != null)
                ret.Add(tile);
            tile = GetTile(x, y - 1);
            if (tile != null)
                ret.Add(tile);
            tile = GetTile(x, y + 1);
            if (tile != null)
                ret.Add(tile);
            return ret;
        }

        public List<Tile> GetNeighbouringTiles(int x, int y)
        {
            List<Tile> ret = new List<Tile>();
            for (int yd = -1; yd < 2; yd++)
                for (int xd = -1; xd < 2; xd++)
                {
                    if (xd == 0 && yd == 0)
                        continue;
                    Tile tile = GetTile(x + xd, y + yd);
                    if (tile != null)
                        ret.Add(tile);
                }
            return ret;
        }


        class TileSearchNode : IComparable<TileSearchNode>
        {
            public TileSearchNode(Tile tile)
            {
                Tile = tile;
            }

            public Tile Tile;

            public int DistanceSoFar;
            public int Cost;

            public TileSearchNode Parent;

            public int CompareTo(TileSearchNode other)
            {
                //have to mess about a bit here to ensure nodes are never equal, or else
                //the sortedlist insertion can complain that an item with the same key already exists
                int ret = Cost.CompareTo(other.Cost);
                if (ret != 0)
                    return ret;
                ret = Tile.X.CompareTo(other.Tile.X);
                if (ret != 0)
                    return ret;
                return Tile.Y.CompareTo(other.Tile.Y);
            }
        }

        public Tile NextTile(Tile start, Tile goal)
        {
            List<Tile> path = GetPath(start, goal);
            if (path.Count > 1)
                return path[path.Count - 2];
            return start;
        }

        public List<Tile> GetPath(Tile start, Tile end)
        {
            List<Tile> ret = new List<Tile>();
            SortedList<TileSearchNode, TileSearchNode> workingNodes = new SortedList<TileSearchNode, TileSearchNode>();

            Dictionary<Tile, TileSearchNode> tileToNode = new Dictionary<Tile, TileSearchNode>();

            HashSet<Tile> processedTiles = new HashSet<Tile>();

            TileSearchNode node = new TileSearchNode(start);
            workingNodes.Add(node, node);
            tileToNode.Add(node.Tile, node);

            while (true)
            {
                if (workingNodes.Count == 0)
                    break;
                TileSearchNode currentNode = workingNodes.ElementAt(0).Value;
                workingNodes.Remove(currentNode);
                
                processedTiles.Add(currentNode.Tile);

                if (currentNode.Tile == end)
                {
                    for (TileSearchNode n = currentNode; n != null; n = n.Parent)
                        ret.Add(n.Tile);
                    break;
                }

                foreach (Tile t in GetAdjacentTiles(currentNode.Tile.X, currentNode.Tile.Y))
                {
                    if (!t.IsFloor)
                        continue;
                    if (processedTiles.Contains(t))
                        continue;
                    
                        
                    //    continue;
                    
                    int distanceSoFar = currentNode.DistanceSoFar + 1;

                    Creature e = t.Creature;
                    if (e != null)
                        distanceSoFar += e._idleTime;
                    
                    int estDistanceToTarget = Math.Abs(t.X - end.X) + Math.Abs(t.Y - end.Y);
                    int cost = distanceSoFar + estDistanceToTarget;

                    TileSearchNode n;
                    if (tileToNode.TryGetValue(t, out n))
                    {
                        if (cost < n.Cost)
                        {
                            workingNodes.Remove(n);
                            n.Parent = currentNode;
                            n.DistanceSoFar = distanceSoFar;    
                            n.Cost = cost;
                            workingNodes.Add(n, n);
                        }
                    }
                    else
                    {
                        n = new TileSearchNode(t);
                        n.Parent = currentNode;
                        n.DistanceSoFar = distanceSoFar;
                        n.Cost = cost;
                        workingNodes.Add(n, n);
                        tileToNode.Add(n.Tile, n);
                    }                    
                }
            }
            return ret;
        }

        //public Entity GetEntity(int x, int y)
        //{
        //    return (from e in Entities where e.X == x && e.Y == y select e).FirstOrDefault();
        //}

        public Tile GetRandomTile()
        {
            return _tiles[Dice.Next(_width), Dice.Next(_height)];
        }

        public Tile GetRandomFloorTile()
        {
            while (true)
            {
                Tile tile = _tiles[Dice.Next(_width), Dice.Next(_height)];
                if (tile.IsFloor)
                    return tile;
            }
        }

        public static CompassPoint GetRandomDirection()
        {
            return (CompassPoint)Dice.Next(4);
        }

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

        public void EmitSound(Tile origin, int volume)
        {
            Sounding sounding = new Sounding(this, new Action<Sounding,Tile,int>(
                (e, tile, distance) => 
            {
                if (tile.Creature != null)
                {
                    tile.Creature.HearSound(e);
                }
                //tile.WaterLevel = 5;
            }));
            sounding.Run(origin, volume);
        }

        public List<Creature> Entities = new List<Creature>();

        public Bitmap ToBitmap(HashSet<Tile> visibleTiles, HashSet<Tile> rememberedTiles)
        {
            Bitmap bm = new Bitmap(_width * 5, _height * 5);
            Graphics g = Graphics.FromImage(bm);
            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                {
                    Tile tile = _tiles[x, y];
                    bool isRemembered = rememberedTiles.Contains(tile);
                    bool isVisible = visibleTiles.Contains(tile);
                    if (Form1.CheatMode)
                        isVisible = true;
                    _tiles[x, y].Render(g, isVisible, isRemembered);
                    //if (_tiles[x, y].WaterFall > 0)
                    //{
                    //    int opacity = (int)(_tiles[x, y].WaterFall * 255)+100;
                    //    if (opacity > 255)
                    //        opacity = 255;
                    //    g.FillRectangle(new SolidBrush(Color.FromArgb(opacity, 0, 0, 255)), x * 5, y * 5, 5, 5);
                    //}
                }

            //foreach (Room r in Rooms)
            //    foreach (Entrance e in r.Entrances)
            //    {
            //        if (!visibleTiles.Contains(_tiles[e.Position.X, e.Position.Y]))
            //            continue;
            //        g.FillEllipse(new SolidBrush(Color.Orange), e.Position.X * 5 + 1, e.Position.Y * 5 + 1, 3, 3);
            //    }

            if (visibleTiles.Contains(_tiles[StairsX, StairsY]))
                g.FillEllipse(new SolidBrush(Color.White), StairsX * 5 + 1, StairsY * 5 + 1, 3, 3);

            g.Dispose();
            return bm;
        }


        public class TileChange2
        {
            public Tile Tile;
            public double Value;
            public TileChange2(Tile tile, double value)
            {
                Tile = tile;
                Value = value;
            }
        }

        public void AdvanceWater()
        {
            List<TileChange2> changes = new List<TileChange2>();
            for (int y = 0; y < _width; y++)
                for (int x = 0; x < _height; x++)
                {
                    Tile tile = _tiles[x,y];
                    
                    List<Tile> neighbours = GetAdjacentTiles(x, y);
                    double sum = (from n in neighbours select n.WaterLevel).Sum();
                    sum += tile.WaterLevel;
                    double average = sum / 5;

                    if (tile.Type == TileType.Chasm)
                    {
                        if (average > 0.1)
                            tile.WaterFall = average;
                        else
                            tile.WaterFall = 0;
                        continue;
                    }
                        
                    
                    if (average != tile.WaterLevel)
                        changes.Add(new TileChange2(tile, average));
                }
            foreach (TileChange2 c in changes)
                c.Tile.WaterLevel = c.Value;
         }

        public void AddCreature(Creature e)
        {
            Entities.Add(e);
        }

        public Tile GetRelativeTile(Tile tile, CompassPoint direction)
        {
            switch (direction)
            {
                case CompassPoint.North:
                    return GetTile(tile.X, tile.Y - 1);
                case CompassPoint.East:
                    return GetTile(tile.X + 1, tile.Y);
                case CompassPoint.South:
                    return GetTile(tile.X, tile.Y + 1);
                case CompassPoint.West:
                    return GetTile(tile.X - 1, tile.Y);
            }
            return null;
        }

        internal void RemoveEntity(Creature entity)
        {
            entity.Tile.Creature = null;
            Entities.Remove(entity);
        }
    }
}
