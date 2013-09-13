using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TempRl
{
    /// <summary>
    /// Can be used to build a bridge on the map
    /// </summary>
    public class BridgeBuilder
    {
        MapDesigner _designer;
        CorridorSet _corridorSet;
        public BridgeBuilder(MapDesigner designer, CorridorSet corridorSet)
        {
            _designer = designer;
            _corridorSet = corridorSet;
        }

        /// <summary>
        /// Returns whether the specified join tile is a valid tile for the start of a bridge
        /// To be valid the tile must look onto a chasm/lake
        /// </summary>
        public bool IsValidBridgeStart(JoinTile candidate)
        {
            //see if the join faces out onto a chasm/lake by checking the forwards neighbour of the join tile
            Point bridgeDir = Compass.GetDirectionVector(candidate.MapFacingDirection);
            Tile frontTile = _designer.Map.GetTile(candidate.MapPosition.X + bridgeDir.X, candidate.MapPosition.Y + bridgeDir.Y);
            if (frontTile == null)
                return false;
            if (frontTile.Type == TileType.Chasm || frontTile.Type == TileType.Lava)
                return true;
            return false;
        }

        Template _bridgeEnd = null;
        Template _bridgeStartLedge = null;
        Template _bridgeEndLedge = null;
        JoinTile _bridgeEndJoin = null;

        bool PlaceBridgeEnd(JoinTile start)
        {
            //attempt to place a structure at the other side of the chasm
            //first find the other side of the chasm
            int roomNum = Dice.Next(8) + 1;
            _bridgeEnd = _designer.LoadTemplate("room" + roomNum + ".bmp");
            JoinTile endJoin = _bridgeEnd.GetRandomUnconnectedJoin();
            CompassPoint endFacingDirection = Compass.GetOppositeDirection(start.MapFacingDirection);

            //travel from the bridge entrance one tile at a time trying to place the bridge end
            Point bridgeDir = Compass.GetDirectionVector(start.MapFacingDirection);
            Tile frontTile = _designer.Map.GetTile(start.MapPosition.X + bridgeDir.X, start.MapPosition.Y + bridgeDir.Y);
            bool reachedSolid = false;
            int timeout = 5;
            for (Point p = new Point(frontTile.X, frontTile.Y); ; p.X += bridgeDir.X, p.Y += bridgeDir.Y)
            {
                if (reachedSolid)
                {
                    //once the end of the chasm/lake has been reached give a few tiles leeway to allow
                    //a room to be successfully placed. The solid tiles skipped can be overwritten by the
                    //bridge building later.
                    timeout--;
                    if (timeout <= 0)
                        break;
                }
                else
                {
                    //check if the current tile has reached the end of the chasm/lake
                    Tile currentTile = _designer.Map.GetTile(p.X, p.Y);
                    if (currentTile == null)
                        break;
                    if (currentTile.Type != TileType.Chasm && currentTile.Type != TileType.Lava)
                        reachedSolid = true;
                }

                //Only start testing placement of the bridge end if the span across the chasm/lake is long enough so far
                if (Math.Abs(frontTile.X - p.X) + Math.Abs(frontTile.Y - p.Y) < 5)
                    continue;

                //position the bridge end so that the end join sits on this tile facing the bridge start
                _bridgeEnd.PositionJoin(endJoin, p.X, p.Y, endFacingDirection);
                //check if the bridge end is valid in this position
                if (_designer.Validate(_bridgeEnd))
                {
                    _bridgeEndJoin = endJoin;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Adds a ledge at existing join
        /// </summary>
        public Template AddLedge(JoinTile existingJoin)
        {
            Template ledge = _designer.LoadTemplate("bridgeend1.bmp");
            JoinTile ledgeJoin = ledge.GetRandomUnconnectedJoin(CompassPoint.North);
            TemplateConnection connection = ledge.ConnectTo(existingJoin, ledgeJoin);
            if (!_designer.Validate(ledge, true))
            {
                existingJoin.Template.DisconnectConnection(connection);
                return null;
            }
            _designer.AddTemplateToMap(ledge);
            return ledge;
        }

        public bool CreateBridge(JoinTile start)
        {
            bool ret = CreateBridgeEx(start);
            if (!ret)
            {
                if (_bridgeEnd != null)
                    _designer.RemoveTemplateFromMap(_bridgeEnd);
                if (_bridgeStartLedge != null)
                    _designer.RemoveTemplateFromMap(_bridgeStartLedge);
                if (_bridgeEndLedge != null)
                    _designer.RemoveTemplateFromMap(_bridgeEndLedge);
            }
            return ret;
        }

        /// <summary>
        /// Designs a bridge from the start tile
        /// </summary>
        public bool CreateBridgeEx(JoinTile start)
        {
            if (!IsValidBridgeStart(start))
                return false;

            _bridgeStartLedge = null;
            _bridgeEndLedge = null;
            _bridgeEnd = null;
            _bridgeEndJoin = null;

            //first place a template on the otherside of the chasm/lake to act as the bridge end anchor
            bool success = PlaceBridgeEnd(start);
            if (!success)
                return false;

            _designer.AddTemplateToMap(_bridgeEnd);

            //_bridgeStartLedge = AddLedge(start);
            //_bridgeEndLedge = AddLedge(_bridgeEndJoin);

            //if (_bridgeStartLedge == null || _bridgeEndLedge == null)
            //    return false;

            JoinTile startJoin = start; // _bridgeStartLedge.GetRandomUnconnectedJoin();
            JoinTile endJoin = _bridgeEndJoin; // _bridgeEndLedge.GetRandomUnconnectedJoin();

            CorridorBuilder cb = new CorridorBuilder(_designer, new BridgeCorridorSet());
            cb.IgnoreChasm = true;
            if (!cb.CreatePath(startJoin, endJoin))
                return false;
            return true;
        }
    }
}
