using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace TempRl
{
    /// <summary>
    /// Templates are small tile grids containing the design for a room or coridor section.
    /// These templates are positioned and imprinted on the map to produce interlinked rooms
    /// and corridors. Templates can be loaded from file and rotated before being built into the map.
    /// </summary>
    public class Template
    {
        TemplateRotation _rotation = TemplateRotation.R0;
        int _x;
        int _y;

        TemplateTile[,] _tiles;
        List<JoinTile> _joinTiles = new List<JoinTile>();
        List<JoinTile> _unconnectedJoins = new List<JoinTile>();

        public HashSet<TemplateConnection> Connections = new HashSet<TemplateConnection>();

        int _templateWidth;
        int _templateHeight;

        public Template()
        {
        }


        /// <summary>
        /// Clone constructor
        /// </summary>
        public Template(Template other)
        {
            _templateWidth = other.Width;
            _templateHeight = other.Height;
            _rotation = other._rotation;
            _x = other._x;
            _y = other._y;
            _tiles = new TemplateTile[_templateWidth, _templateHeight];
            for (int y = 0; y < _templateHeight; y++)
                for (int x = 0; x < _templateWidth; x++)
                {
                    if (other._tiles[x, y] == null)
                        _tiles[x, y] = null;
                    else
                    {
                        _tiles[x, y] = other._tiles[x, y].Clone();
                        _tiles[x, y].Template = this;
                        if (_tiles[x, y] as JoinTile != null)
                        {
                            _joinTiles.Add(_tiles[x, y] as JoinTile);
                            _unconnectedJoins.Add(_tiles[x, y] as JoinTile);
                        }
                    }
                }
        }

        /// <summary>
        /// Transforms a direction relative to the template into a direction relative to the map.
        /// </summary>
        /// <param name="relativeDirection"></param>
        /// <returns></returns>
        public CompassPoint GetGlobalDirection(CompassPoint templateDirection)
        {
            //eg if the room is rotated 90 degrees then the template direction North is equivalent
            //to the map direction East.
            switch (_rotation)
            {
                case TemplateRotation.R0:
                    return templateDirection;
                case TemplateRotation.R90:
                    return Compass.Rotate90(templateDirection);
                case TemplateRotation.R180:
                    return Compass.Rotate180(templateDirection);
                default:
                    return Compass.Rotate270(templateDirection);
            }
        }

        /// <summary>
        /// Returns whether the tile of this template at the specified map coordinate is an
        /// edge tile of the template.
        /// </summary>
        public bool IsEdge(int mx, int my)
        {
            //the tile itself needs to have content to be an edge tile
            if (GetMapTile(mx, my) == null)
                return false;
            //at least one of the adjacent tiles needs to be void of content
            if (GetMapTile(mx - 1, my) == null)
                return true;
            if (GetMapTile(mx + 1, my) == null)
                return true;
            if (GetMapTile(mx, my - 1) == null)
                return true;
            if (GetMapTile(mx, my + 1) == null)
                return true;
            return false;
        }

        /// <summary>
        /// Obtains the X position of the top left corner of the template on the map
        /// </summary>
        public int X
        {
            get
            {
                return _x;
            }
            set
            {
                _x = value;
            }
        }

        public int Y
        {
            get
            {
                return _y;
            }
            set
            {
                _y = value;
            }
        }

        /// <summary>
        /// Sets or gets the current orientation of the template. The template can be rotated
        /// 0, 90, 180 or 270 degrees.
        /// </summary>
        public TemplateRotation Orientation
        {
            get
            {
                return _rotation;
            }
            set
            {
                _rotation = value;
            }
        }

        public Template Clone()
        {
            return new Template(this);
        }

        /// <summary>
        /// Initializes the template from a bitmap file on disk.
        /// </summary>
        /// <param name="filePath"></param>
        public void LoadFromDisk(string filePath)
        {
            Bitmap bm = Bitmap.FromFile(filePath) as Bitmap;
            _templateWidth = bm.Width;
            _templateHeight = bm.Height;
            _tiles = new TemplateTile[bm.Width, bm.Height];
            //the color of pixels in the bitmap determines which tile types go where
            //eg which tiles are walls, which tiles are join tiles (can be used to connect other templates)
            for (int y = 0; y < bm.Height; y++)
                for (int x = 0; x < bm.Width; x++)
                {
                    _tiles[x, y] = null;
                    Color c = bm.GetPixel(x, y);
                    if (c == Color.FromArgb(255, 255, 255))
                        continue;
                    TemplateTile tile;
                    JoinTile joinTile=null;
                    if (c == Color.FromArgb(255, 201, 14) ||
                        c == Color.FromArgb(255, 127, 39))
                    {
                        tile = new JoinTile();
                        joinTile = tile as JoinTile;
                    }
                    else
                        tile = new TemplateTile();

                    tile.Template = this;

                    tile.X = x;
                    tile.Y = y;
                    _tiles[x, y] = tile;
                    if (c == Color.FromArgb(127, 127, 127))
                        tile.Type = TileType.StoneWall;
                    else if (c == Color.FromArgb(255, 201, 14))
                    {
                        tile.Type = TileType.StoneWall;
                        //tile.SemiExclusion = true;
                        joinTile.Priority = 20;
                        _joinTiles.Add(joinTile);
                        _unconnectedJoins.Add(joinTile);
                    }
                    else if (c == Color.FromArgb(195, 195, 195))
                    {
                        //stone floor exclusion zone means no other template can overlap this tile
                        tile.Exclusive = true;
                        tile.Type = TileType.StoneFloor;
                    }
                    else if (c == Color.FromArgb(255, 174, 201))
                    {
                        tile.Type = TileType.StoneWall;
                        tile.SemiExclusion = true;
                    }
                    else if (c == Color.FromArgb(239, 228, 176))
                    {
                        tile.Exclusive = true;
                    }
                    else if (c == Color.FromArgb(181, 230, 29))
                    {
                        tile.SemiExclusion = true;
                        tile.Type = TileType.None;
                    }
                    else if (c == Color.FromArgb(255, 127, 39))
                    {
                        tile.Type = TileType.StoneWall;
                        joinTile.Priority = 200; 
                        _joinTiles.Add(joinTile);
                        _unconnectedJoins.Add(joinTile);
                    }
                }

            for (int y = 0; y < bm.Height; y++)
                for (int x = 0; x < bm.Width; x++)
                {
                    JoinTile tile = GetTemplateTile(x, y) as JoinTile;
                    if (tile == null)
                        continue;
                    if (GetTemplateTile(x - 1, y) == null)
                        tile.TemplateFacingDirection = CompassPoint.West;
                    else if (GetTemplateTile(x + 1, y) == null)
                        tile.TemplateFacingDirection = CompassPoint.East;
                    else if (GetTemplateTile(x, y - 1) == null)
                        tile.TemplateFacingDirection = CompassPoint.North;
                    else
                        tile.TemplateFacingDirection = CompassPoint.South;
                }
        }

        /// <summary>
        /// Width of the template given its orientation on the map
        /// </summary>
        public int Width
        {
            get
            {
                //if the template is rotated 90 or 270 degrees then it's width on the map is the template's height
                if (_rotation == TemplateRotation.R0 || _rotation == TemplateRotation.R180)
                    return _templateWidth;
                else
                    return _templateHeight;
            }
        }

        /// <summary>
        /// Height of the template given its orientation on the map
        /// </summary>
        public int Height
        {
            get
            {
                if (_rotation == TemplateRotation.R0 || _rotation == TemplateRotation.R180)
                    return _templateHeight;
                else
                    return _templateWidth;
            }
        }

        public bool IsConnectedTo(Template other)
        {
            foreach (TemplateConnection tc in Connections)
                if (tc.Join1.Template == other || tc.Join2.Template == other)
                    return true;
            return false;
        }

        /// <summary>
        /// Connects this template to another template via a specified join of each
        /// </summary>
        /// <param name="other">The other template to connect this template to</param>
        /// <param name="otherJoin">The join of the other template to join with</param>
        /// <param name="ownJoin">The join of this template to join with</param>
        /// <returns>The connection made</returns>
        public TemplateConnection ConnectTo(JoinTile otherJoin, JoinTile ownJoin)
        {
            //place the new template into the correct orientation so that it joins with the new template
            PositionJoin(ownJoin, otherJoin.MapPosition.X, otherJoin.MapPosition.Y, Map.GetOppositeDirection(otherJoin.MapFacingDirection));

            //Add a new connection between the two template join sites. The connection is needed for the validation step next
            //as some validation rules require knowing which template the new template is connected to.
            TemplateConnection connection = new TemplateConnection();
            connection.Join1 = ownJoin;
            connection.Join2 = otherJoin;
            connection.X = otherJoin.MapPosition.X;
            connection.Y = otherJoin.MapPosition.Y;

            _unconnectedJoins.Remove(ownJoin);
            otherJoin.Template._unconnectedJoins.Remove(otherJoin);

            otherJoin.Template.Connections.Add(connection);
            Connections.Add(connection);
            return connection;
        }

        /// <summary>
        /// Disconnects a connection from this template
        /// </summary>
        /// <param name="connection"></param>
        public void DisconnectConnection(TemplateConnection connection)
        {
            Connections.Remove(connection);
            if (connection.Join1.Template == this)
                _unconnectedJoins.Add(connection.Join1);
            else
                _unconnectedJoins.Add(connection.Join2);
        }

        /// <summary>
        /// Rotates this template by 90 degrees
        /// </summary>
        public void Rotate90()
        {
            switch (_rotation)
            {
                case TemplateRotation.R0:
                    _rotation = TemplateRotation.R90;
                    break;
                case TemplateRotation.R90:
                    _rotation = TemplateRotation.R180;
                    break;
                case TemplateRotation.R180:
                    _rotation = TemplateRotation.R270;
                    break;
                default:
                    _rotation = TemplateRotation.R0;
                    break;
            }
        }

        /// <summary>
        /// Gets the template tile at the specified map position
        /// </summary>
        /// <param name="mapX"></param>
        /// <param name="mapY"></param>
        /// <returns></returns>
        public TemplateTile GetMapTile(int mapX, int mapY)
        {
            //if the coordinates are outside the template extent return null
            if (mapX < _x || mapY < _y || mapX > _x + Width - 1 || mapY > _y + Height - 1)
                return null;
            Point p = TransformGlobalToTemplateCoords(mapX, mapY);
            return _tiles[p.X, p.Y];
        }

        //Template coordinates are the (x,y) coordinates in the bitmap. These are static and never change.
        //Local coordinates are template coordinates rotated by the current template orientation.
        //Map coordinates are the local coordinates tranformed by the position of the template on the map.
        //Example:
        //A template 4x2 
        //The lower right corner of the template in template coordinates is (3,1)
        //Local coordinates are also (3,1) if the template is not rotated. If the template is rotated 90, 180 or 270
        //degrees the local coordinates will be (0,3), (0,0) and (1,0) respectively.
        //The map coordinates of the lower right corner of the template are given by the local coordinates plus the
        //offset of the template on the map

        /// <summary>
        /// Rotates template coordinates to their position in the template
        /// when rotated by a specified amount. Returns the position of a tile at x,y if
        /// the template were rotated by the specific amount.
        /// </summary>
        /// <param name="x">local template x coordinate</param>
        /// <param name="y">local template y coordinate</param>
        /// <param name="rotation">rotation to apply to the coordinates</param>
        /// <returns>template coordinates after rotatation</returns>
        Point RotateLocal(int x, int y, TemplateRotation rotation)
        {
            Point ret = new Point();
            switch (_rotation)
            {
                case TemplateRotation.R0:
                    ret.X = x;
                    ret.Y = y;
                    break;
                case TemplateRotation.R90:
                    ret.X = -y + (_templateHeight - 1);
                    ret.Y = x;
                    break;
                case TemplateRotation.R180:
                    ret.X = -x + (_templateWidth - 1);
                    ret.Y = -y + (_templateHeight - 1);
                    break;
                case TemplateRotation.R270:
                    ret.X = y;
                    ret.Y = -x + (_templateWidth - 1);
                    break;
            }
            return ret;
        }

        /// <summary>
        /// Positions the template so that its specified join tile sits at the specified map coordinates
        /// and faces in the specified direction
        /// </summary>
        /// <param name="join">Join tile which must belong to the template</param>
        /// <param name="mapX">X map position</param>
        /// <param name="mapY">Y map position</param>
        /// <param name="facingDirection">direction the join should face</param>
        public void PositionJoin(JoinTile join, int mapX, int mapY, CompassPoint facingDirection)
        {
            //set the template orientation so that its join faces the same direction as specified
            //if the join direction is not the same as the direction the template should face then need to rotate the template
            Orientation = TemplateRotation.R0;
            if (join.TemplateFacingDirection == CompassPoint.North && facingDirection == CompassPoint.East ||
                join.TemplateFacingDirection == CompassPoint.East && facingDirection == CompassPoint.South ||
                join.TemplateFacingDirection == CompassPoint.South && facingDirection == CompassPoint.West ||
                join.TemplateFacingDirection == CompassPoint.West && facingDirection == CompassPoint.North)
                Orientation = TemplateRotation.R90;
            else if (join.TemplateFacingDirection == CompassPoint.North && facingDirection == CompassPoint.South ||
                join.TemplateFacingDirection == CompassPoint.East && facingDirection == CompassPoint.West ||
                join.TemplateFacingDirection == CompassPoint.South && facingDirection == CompassPoint.North ||
                join.TemplateFacingDirection == CompassPoint.West && facingDirection == CompassPoint.East)
                Orientation = TemplateRotation.R180;
            else if (join.TemplateFacingDirection == CompassPoint.North && facingDirection == CompassPoint.West ||
                join.TemplateFacingDirection == CompassPoint.East && facingDirection == CompassPoint.North ||
                join.TemplateFacingDirection == CompassPoint.South && facingDirection == CompassPoint.East ||
                join.TemplateFacingDirection == CompassPoint.West && facingDirection == CompassPoint.South)
                Orientation = TemplateRotation.R270;
            
            //obtain the local coordinates of the join
            Point localCoords = GetLocalCoords(join.X, join.Y);

            //position the template so that the join tile falls on the specified coordinates on the map
            _x = mapX - localCoords.X;
            _y = mapY - localCoords.Y;
        }

        Point GetLocalCoords(int templateX, int templateY)
        {
            return RotateLocal(templateX, templateY, _rotation);
        }

        public Point GetGlobalCoords(int templateX, int templateY)
        {
            Point ret = new Point();
            ret = GetLocalCoords(templateX, templateY);
            ret.X += _x;
            ret.Y += _y;
            return ret;
        }

        Point TransformGlobalToTemplateCoords(int mapX, int mapY)
        {
            Point ret = new Point();

            //obtain the position relative to the top left corner of the template
            int relX = mapX - _x;
            int relY = mapY - _y;

            //90 degree rotation:
            //x2=-y1
            //y2=x1
            switch (_rotation)
            {
                case TemplateRotation.R0:
                    //template is not rotated, no transformation needed
                    ret.X = relX;
                    ret.Y = relY;
                    break;
                case TemplateRotation.R90:
                    //template is rotated by 90 degrees so need to rotate the point by -90 degrees
                    //also need to shift it downwards by the height of the room template
                    ret.X = relY;
                    ret.Y = (_templateHeight - 1) - relX;
                    break;
                case TemplateRotation.R180:
                    //rotate point by -180 degrees
                    ret.X = (_templateWidth - 1) - relX;
                    ret.Y = (_templateHeight - 1) - relY;
                    break;
                case TemplateRotation.R270:
                    //rotate point by -270 (90) degrees
                    ret.X = -relY + (_templateWidth - 1);
                    ret.Y = relX;
                    break;
            }
            return ret;
        }

        /// <summary>
        /// Obtains a template tile by template coordinate
        /// </summary>
        TemplateTile GetTemplateTile(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _templateWidth || y >= _templateHeight)
                return null;
            return _tiles[x, y];
        }

        public void Build(Map map)
        {
            for (int y = _y; y < _y + Height; y++)
                for (int x = _x; x < _x + Width; x++)
                {
                    TemplateTile tile = GetMapTile(x, y);
                    if (tile == null)
                        continue;
                    TileType tileType = tile.Type;
                    if (tileType == TileType.None)
                        continue;
                    map.GetTile(x, y).Type = tileType;
                }
            
            foreach (TemplateConnection c in Connections)
            {
                map.GetTile(c.X, c.Y).Type = TileType.StoneFloor;
                continue;
                //The connection will be at the edge of the template and isn't always connected to the
                //room. Need to drill a path from the connection into the room.
                //Get the join for this connection to determine the direction that needs to be drilled
                JoinTile join = GetMapTile(c.X, c.Y) as JoinTile;
                //get the opposite direction of the join direction in global map space
                CompassPoint inwardsDirection = Compass.GetOppositeDirection(join.MapFacingDirection);

                //get the inwards and right vector
                Point inwards = Compass.GetDirectionVector(inwardsDirection);
                Point right = Compass.GetDirectionVector(Compass.GetRightDirection(inwardsDirection));
                //start at the connection
                int x = c.X;
                int y = c.Y;

                //Drill a stone floor path with walls at the side into the template until existing floor is hit
                while (true)
                {
                    Tile pathTile = map.GetTile(x, y);
                    //if this isn't the connection tile and the path tile is already floor then we've successfully joined the room to the connection
                    if (!(x == c.X && y == c.Y) && pathTile.IsFloor)
                        break;
                    //create a floor tile and wall tiles either side
                    map.GetTile(x, y).Type = TileType.StoneFloor;
                    //map.GetTile(x + right.X, y + right.Y).Type = TileType.StoneWall;
                    //map.GetTile(x - right.X, y - right.Y).Type = TileType.StoneWall;
                    //advance inwards
                    x += inwards.X;
                    y += inwards.Y;
                }
            }
        }

        public JoinTile GetRandomUnconnectedJoin(CompassPoint facingDirection)
        {
            var directionJoins = (from j in _joinTiles where j.MapFacingDirection == facingDirection select j).ToList();
            if (directionJoins.Count() == 0)
                return null;
            return directionJoins[Dice.Next(directionJoins.Count)];
        }

        public List<JoinTile> UnconnectedJoins
        {
            get
            {
                return _unconnectedJoins;
            }
        }

        public List<JoinTile> Joins
        {
            get
            {
                return _joinTiles;
            }
        }

        public JoinTile GetRandomUnconnectedJoin()
        {
            List<JoinTile> joins = _unconnectedJoins.ToList();
            if (joins.Count == 0)
                return null;
            JoinTile ret =  joins[Dice.Next(joins.Count)];
            return ret;
        }

        public List<Template> ConnectedTemplates
        {
            get
            {
                List<Template> ret = new List<Template>();
                //each connection connects two templates. One of the templates is this template, return all the other templates.
                foreach (TemplateConnection connection in Connections)
                    if (connection.Join1.Template == this)
                        ret.Add(connection.Join2.Template);
                    else
                        ret.Add(connection.Join1.Template);
                return ret;
            }
        }
    }

    public class TemplateTile
    {
        public Template Template;
        public int X;
        public int Y;
        public TileType Type;
        public bool SemiExclusion;
        public bool Exclusive;

        public Point MapPosition
        {
            get
            {
                return Template.GetGlobalCoords(X, Y);
            }
        }

        public virtual TemplateTile Clone()
        {
            TemplateTile ret = new TemplateTile();
            ret.Template = Template;
            ret.X = X;
            ret.Y = Y;
            ret.Type = Type;
            ret.SemiExclusion = SemiExclusion;
            ret.Exclusive = Exclusive;
            return ret;
        }
    }

    public class JoinTile : TemplateTile
    {
        /// <summary>
        /// The facing direction of the join in template space
        /// </summary>
        public CompassPoint TemplateFacingDirection;

        public int Priority;
        /// <summary>
        /// The facing direction of the join in map space
        /// </summary>
        public CompassPoint MapFacingDirection
        {
            get
            {
                return Template.GetGlobalDirection(TemplateFacingDirection);
            }
        }

        public override TemplateTile Clone()
        {
            JoinTile ret = new JoinTile();
            ret.TemplateFacingDirection = TemplateFacingDirection;
            ret.Template = Template;
            ret.X = X;
            ret.Y = Y;
            ret.Type = Type;
            ret.SemiExclusion = SemiExclusion;
            ret.Exclusive = Exclusive;
            ret.Priority = Priority;
            return ret;
        }

        /// <summary>
        /// Returns true if the join is connected to the specified template (either belonging to it, or by connecting to it)
        /// </summary>
        public bool Connects(Template template)
        {
            TemplateConnection connection = Connection;
            if (connection == null)
                return false;
            if (connection.Join1.Template == template || connection.Join2.Template == template)
                return true;
            return false;
        }

        public bool IsConnected
        {
            get
            {
                return Connection != null;
            }
        }

        public TemplateConnection Connection
        {
            get
            {
                foreach (TemplateConnection connection in Template.Connections)
                    if (connection.Join1 == this || connection.Join2 == this)
                        return connection;
                return null;
            }
        }
    }

    public class TemplateConnection
    {
        public JoinTile Join1;
        public JoinTile Join2;

        public int X;
        public int Y;

        public void Remove()
        {
            Join1.Template.DisconnectConnection(this);
            Join2.Template.DisconnectConnection(this);
        }
    }

    public enum TemplateRotation
    {
        R0,
        R90,
        R180,
        R270
    }
}
