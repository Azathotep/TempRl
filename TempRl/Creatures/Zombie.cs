using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace TempRl
{
    /// <summary>
    /// Zombie is a simple creature that wanders around the map when idle. When they see the player
    /// they chase.
    /// </summary>
    public class Zombie : Creature
    {
        public override Color Color
        {
            get
            {
                return Color.Green;
            }
        }

        int waitTurn = 0;
        int _turnsWithoutMovement = 0;
        Sounding _memSound = null;

        Tile targetTile = null;

        public override void OnTurn()
        {
            //Look for the player and target them if visible
            HashSet<Tile> visibleTiles = GetVisibleTiles();
            foreach (Tile tile in visibleTiles)
            {
                if (tile.Type == TileType.HidingHole)
                    continue;
                Creature entity = tile.Creature;
                if (entity == null)
                    continue;
                if (entity.GetType() == typeof(Player))
                {
                    targetTile = tile;
                    _memSound = null;
                    break;
                }
            }
            //zombies being drawn by emitted sound is implemented but no sound emitters are implemented
            if (targetTile == null)
            {
                foreach (Sounding echo in _heardSounds)
                {
                    _memSound = echo;
                    break;
                }
                if (_memSound != null)
                    targetTile = _memSound.GetPathToOrigin(Tile).FirstOrDefault();
            }

            //if the zombie has no target then it picks a random floor tile on the map to travel to.
            if (targetTile == null)
                targetTile = Map.GetRandomFloorTile();

            if (targetTile == null)
                return;

            //zombies only move once every two turns
            if (waitTurn > 0)
            {
                waitTurn--;
                return;
            }

            //get the next tile towards the target
            List<Tile> path = Map.GetPath(Tile, targetTile);
            if (path.Count > 1)
            {
                Tile tile = path[path.Count - 2];

                if (tile.Creature == Player.Instance)
                {
                    tile.Creature = null;
                    Form1.GameOver = true;
                    Form1.GameOverReason = Form1.GameOverReasonZombie;
                }

                if (MoveTo(tile.X, tile.Y))
                {
                    _turnsWithoutMovement = 0;
                    _idleTime = 0;
                }
                else
                {
                    _turnsWithoutMovement++;
                    //if the zombie is stuck then clear the target to force a new target to be set next turn
                    //this should reduce the problem of zombies travelling in opposite directions blocking each
                    //other and becoming indefinitely stuck
                    if (_turnsWithoutMovement > 3)
                        targetTile = null;
                    _idleTime++;
                }
            }
            else
                targetTile = null;
            waitTurn = 1;
        }

    }
}
