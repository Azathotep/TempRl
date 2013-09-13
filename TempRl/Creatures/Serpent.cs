using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace TempRl
{
    /// <summary>
    /// Serpent was an experiment to make a multi-tile creature. It's not very robust, too many complicated 
    /// two way references between snake segments and tiles on the map and there's a bug where an invisible
    /// segment gets left behind (which kills the player if they walk into it!).
    /// </summary>
    public class Serpent : Creature
    {
        List<Creature> _segments = new List<Creature>();
        int _length;
        Dictionary<int, Tile> _segmentLocations = new Dictionary<int, Tile>();
        public Serpent(int length)
        {
            _length = length;
            _sightRadius = 15;
        }

        public override bool Place(Tile tile)
        {
            _segmentLocations[0] = tile;
            for (int i = 0; i < _length - 1; i++)
            {
                int attempts = 20;
                for (; attempts > 0; attempts--)
                {
                    Tile n = _segmentLocations[i].GetNeighbour(Map.GetRandomDirection());
                    if (!n.IsPassable)
                        continue;
                    if (n.Creature != null)
                        continue;
                    if (_segmentLocations.Values.Contains(n))
                        continue;
                    _segmentLocations[i + 1] = n;
                    break;
                }
                if (attempts == 0)
                    return false;
            }
            base.Place(tile);
            for (int i = 0; i < _length; i++)
            {
                _segmentLocations[i].Creature = this;
            }
            return true;
        }

        int GetSegmentNumber(Tile tile)
        {
            for (int i = 0; i < _length; i++)
                if (_segmentLocations[i] == tile)
                    return i;
            return -1;
        }

        public override void Render(Graphics g, Tile tile)
        {
            //this is a nasty mess because I wanted the snakes to appear rounded rather than just
            //a series of square blocks, but rushed it.
            for (int segnum = 0; segnum < _length; segnum++)
            {
                if (_segmentLocations[segnum] != tile)
                    continue;
                CompassPoint? nextSegment = null;
                CompassPoint? prevSegment = null;
                if (segnum < _length - 1)
                    nextSegment = _segmentLocations[segnum + 1].RelativeTo(tile);
                if (segnum > 0)
                    prevSegment = _segmentLocations[segnum - 1].RelativeTo(tile);

                SolidBrush brush = new SolidBrush(GetColorAt(tile));
                if (nextSegment.HasValue && prevSegment.HasValue)
                {
                    if (prevSegment.Value == nextSegment.Value)
                    {
                        if (prevSegment.Value == CompassPoint.North)
                            g.FillRectangle(brush, tile.X * 5 + 1, tile.Y * 5 + 1, 4, 4);
                        else
                            g.FillRectangle(brush, tile.X * 5 + 1, tile.Y * 5 + 1, 4, 4);
                    }
                    else if (prevSegment.Value == Map.GetOppositeDirection(nextSegment.Value))
                    {
                        //segment is straight mid-section
                        if (prevSegment.Value == CompassPoint.North || prevSegment.Value == CompassPoint.South)
                            g.FillRectangle(brush, tile.X * 5 + 1, tile.Y * 5 + 1, 4, 4);
                        else
                            g.FillRectangle(brush, tile.X * 5 + 1, tile.Y * 5 + 1, 4, 4);
                    }
                    else
                    {
                        //segment is mid-section corner
                        if ((prevSegment.Value == CompassPoint.North && nextSegment.Value == CompassPoint.East) ||
                            (prevSegment.Value == CompassPoint.East && nextSegment.Value == CompassPoint.North))
                            g.FillClosedCurve(brush, new Point[] { new Point(tile.X * 5 + 5, tile.Y * 5), new Point(tile.X * 5, tile.Y * 5), new Point(tile.X * 5 + 5, tile.Y * 5 + 5) });
                        else if ((prevSegment.Value == CompassPoint.North && nextSegment.Value == CompassPoint.West) ||
                            (prevSegment.Value == CompassPoint.West && nextSegment.Value == CompassPoint.North))
                            g.FillClosedCurve(brush, new Point[] { new Point(tile.X * 5, tile.Y * 5), new Point(tile.X * 5 + 5, tile.Y * 5), new Point(tile.X * 5, tile.Y * 5 + 5) });
                        else if ((prevSegment.Value == CompassPoint.South && nextSegment.Value == CompassPoint.East) ||
                            (prevSegment.Value == CompassPoint.East && nextSegment.Value == CompassPoint.South))
                            g.FillClosedCurve(brush, new Point[] { new Point(tile.X * 5 + 5, tile.Y * 5 + 5), new Point(tile.X * 5, tile.Y * 5 + 5), new Point(tile.X * 5 + 5, tile.Y * 5) });
                        else if ((prevSegment.Value == CompassPoint.South && nextSegment.Value == CompassPoint.West) ||
                            (prevSegment.Value == CompassPoint.West && nextSegment.Value == CompassPoint.South))
                            g.FillClosedCurve(brush, new Point[] { new Point(tile.X * 5, tile.Y * 5), new Point(tile.X * 5 + 5, tile.Y * 5 + 5), new Point(tile.X * 5, tile.Y * 5 + 5) });
                    }
                }
                else
                {
                    if (nextSegment.HasValue)
                    {
                        switch (nextSegment.Value)
                        {
                            case CompassPoint.South:
                                g.FillClosedCurve(brush, new Point[] { new Point(tile.X * 5, tile.Y * 5 + 5), new Point(tile.X * 5 + 2, tile.Y * 5), new Point(tile.X * 5 + 3, tile.Y * 5), new Point(tile.X * 5 + 5, tile.Y * 5 + 5) });
                                break;
                            case CompassPoint.North:
                                g.FillClosedCurve(brush, new Point[] { new Point(tile.X * 5, tile.Y * 5), new Point(tile.X * 5 + 2, tile.Y * 5 + 5), new Point(tile.X * 5 + 3, tile.Y * 5 + 5), new Point(tile.X * 5 + 5, tile.Y * 5) });
                                break;
                            case CompassPoint.East:
                                g.FillClosedCurve(brush, new Point[] { new Point(tile.X * 5 + 5, tile.Y * 5), new Point(tile.X * 5, tile.Y * 5 + 2), new Point(tile.X * 5, tile.Y * 5 + 3), new Point(tile.X * 5 + 5, tile.Y * 5 + 5) });
                                break;
                            case CompassPoint.West:
                                g.FillClosedCurve(brush, new Point[] { new Point(tile.X * 5, tile.Y * 5), new Point(tile.X * 5 + 5, tile.Y * 5 + 2), new Point(tile.X * 5 + 5, tile.Y * 5 + 3), new Point(tile.X * 5, tile.Y * 5 + 5) });
                                break;
                        }
                    }
                    else
                    {
                        switch (prevSegment.Value)
                        {
                            case CompassPoint.South:
                                g.FillPolygon(brush, new Point[] { new Point(tile.X * 5, tile.Y * 5 + 5), new Point(tile.X * 5 + 2, tile.Y * 5), new Point(tile.X * 5 + 3, tile.Y * 5), new Point(tile.X * 5 + 5, tile.Y * 5 + 5) });
                                break;
                            case CompassPoint.North:
                                g.FillClosedCurve(brush, new Point[] { new Point(tile.X * 5, tile.Y * 5), new Point(tile.X * 5 + 2, tile.Y * 5 + 5), new Point(tile.X * 5 + 3, tile.Y * 5 + 5), new Point(tile.X * 5 + 5, tile.Y * 5) });
                                break;
                            case CompassPoint.East:
                                g.FillClosedCurve(brush, new Point[] { new Point(tile.X * 5 + 5, tile.Y * 5), new Point(tile.X * 5, tile.Y * 5 + 2), new Point(tile.X * 5, tile.Y * 5 + 3), new Point(tile.X * 5 + 5, tile.Y * 5 + 5) });
                                break;
                            case CompassPoint.West:
                                g.FillClosedCurve(brush, new Point[] { new Point(tile.X * 5, tile.Y * 5), new Point(tile.X * 5 + 5, tile.Y * 5 + 2), new Point(tile.X * 5 + 5, tile.Y * 5 + 3), new Point(tile.X * 5, tile.Y * 5 + 5) });
                                break;
                        }
                    }
                }
            }
        }

        public override Color Color
        {
            get
            {
                return Color.Fuchsia;
            }
        }

        public override Color GetColorAt(Tile tile)
        {
            //Produces alternate colors for the segments
            if (_segmentLocations[0] == tile)
                return Color.DarkOliveGreen;
            for (int i = 1; i < _length; i++)
            {
                if (_segmentLocations[i] == tile)
                {
                    if (i % 2 == 0)
                        return Color.DarkRed;
                    else
                        return Color.DarkGreen;
                }
            }
            return Color.DarkGreen;
        }

        Tile HeadTile
        {
            get
            {
                return _segmentLocations[0];
            }
        }

        public override void OnTurn()
        {
            //snakes snack on both the player and zombies
            foreach (Creature e in GetVisibleEntities())
            {
                if (e.Tile.Type == TileType.HidingHole)
                    continue;
                if (e.GetType() == typeof(Player) || e.GetType() == typeof(Zombie))
                {
                    Tile next = Map.NextTile(HeadTile, e.Tile);
                    CompassPoint dir = next.RelativeTo(HeadTile);
                    Slither(dir, true);
                    return;
                }
            }

            //when the snake has no target it moves in a random walk. It tries not to cross over itself,
            //but will if it has to otherwise it would get stuck. (which begs the question if a snake in real life
            //went down a small tube, would it get stuck? I looked it up, apparently they can slowly reverse)
            for (int i = 0; i < 20; i++)
            {
                CompassPoint dir = Map.GetRandomDirection();
                bool overSelf = false;
                //after a certain number of failed attempts allow the snake to cross over it's own body.
                if (i > 15)
                    overSelf = true;
                if (Slither(dir, overSelf))
                    break;
            }
            base.OnTurn();
        }

        /// <summary>
        /// Moves the snake in a certain direction.
        /// </summary>
        /// <param name="dir">direction to move</param>
        /// <param name="overSelf">if true the snake will move in the specified direction even if it means
        /// crossing over its own body</param>
        /// <returns></returns>
        bool Slither(CompassPoint dir, bool overSelf)
        {
            //move the head first and then update each segment behind
            bool grow = false;
            Tile target = _segmentLocations[0].GetNeighbour(dir);
            if (!target.IsPassable)
                return false;
            //chomp on any creature in the way
            if (target.Creature != null)
            {
                if (target.Creature == Player.Instance)
                {
                    target.Creature = null;
                    Form1.GameOver = true;
                    Form1.GameOverReason = Form1.GameOverReasonSnake;
                }
                else if (target.Creature.GetType() == typeof(Zombie))
                {
                    target.Creature.IsAlive = false;
                    target.Creature = null;
                    grow = true;
                }
                else
                {
                    if (target.Creature != this)
                        return false;
                    if (target.Creature == this && !overSelf)
                        return false;
                }
            }
            target.Creature = this;
            Tile oldTailTile = _segmentLocations[_length - 1];
            for (int i = _length - 1; i > 0; i--)
                _segmentLocations[i] = _segmentLocations[i - 1];
            _segmentLocations[0] = target;
            if (!_segmentLocations.Values.Contains(oldTailTile))
                oldTailTile.Creature = null;
            if (grow)
            {
                //_length++;
                //_segmentLocations.Add(_length, oldTailTile);
                //oldTailTile.Entity = this;
            }
            _tile = HeadTile;

            return true;
        }
    }

}
