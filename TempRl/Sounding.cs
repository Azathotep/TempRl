using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TempRl
{
    public class MapEchoNode
    {
        public Tile Tile;
        public int Distance;
        public MapEchoNode From;
    }

    /// <summary>
    /// Can be used to send an echo through the dungeon from a starting tile. The echo is recorded
    /// and a path from any point covered by the echo to the echos origin can be retrieved.
    /// </summary>
    public class Sounding
    {
        Map _map;
        Action<Sounding, Tile, int> _callback;
        Dictionary<Tile, MapEchoNode> _tileToNode = new Dictionary<Tile, MapEchoNode>();
        public Sounding(Map map, Action<Sounding, Tile, int> callback)
        {
            _map = map;
            _callback = callback;
        }

        public List<Tile> GetPathToOrigin(Tile startTile)
        {
            List<Tile> ret = new List<Tile>();
            Tile tile = startTile;
            MapEchoNode startNode;
            //obtain the node for the tile. If there is no node then the echo did not cover this tile so
            //just return an empty path
            if (!_tileToNode.TryGetValue(tile, out startNode))
                return ret;
            //trace the tiles back to the origin of the echo
            for (MapEchoNode n = startNode; n != null; n = n.From)
            {
                if (n != startNode)
                    ret.Add(n.Tile);
            }
            return ret;
        }

        public void Run(Tile startTile, int maxDistance)
        {
            HashSet<Tile> tilesVisited = new HashSet<Tile>();

            Queue<MapEchoNode> tilesToVisit = new Queue<MapEchoNode>();
            MapEchoNode node = new MapEchoNode();
            node.Tile = startTile;
            node.From = null;
            node.Distance = 0;
            tilesToVisit.Enqueue(node);
            tilesVisited.Add(startTile);
            _tileToNode[startTile] = node;
            while (true)
            {
                MapEchoNode n = tilesToVisit.Dequeue();
                if (n.Distance > maxDistance)
                    break;
                Tile tile = n.Tile;

                if (!tile.IsFloor)
                    continue;

                //run callback
                _callback(this, tile, n.Distance);

                List<Tile> adjTiles = _map.GetAdjacentTiles(tile.X, tile.Y);
                foreach (Tile a in adjTiles)
                {
                    if (!tilesVisited.Contains(a))
                    {
                        MapEchoNode newNode = new MapEchoNode();
                        newNode.Tile = a;
                        newNode.From = n;
                        newNode.Distance = n.Distance + 1;
                        _tileToNode[a] = newNode;
                        tilesToVisit.Enqueue(newNode);
                        tilesVisited.Add(a);
                    }
                }
            }
        }
    }
}
