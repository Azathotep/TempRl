using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace TempRl
{
    /// <summary>
    /// Before being hacked into a game this project was just an experiment in map generation. This bloated class
    /// was the meat of the experiments. It now contains a lot of unused code and disabled code. The map generator 
    /// is here, hidden among the mess.
    /// </summary>
    public class MapDesigner
    {
        Map _map;

        void Erode()
        {
            List<Tile> changes = new List<Tile>();
            for (int y = 0; y < _map.Height; y++)
                for (int x = 0; x < _map.Width; x++)
                {
                    List<Tile> tiles = _map.GetAdjacentTiles(x, y);

                    int chasmCount = (from t in tiles where t.Type == TileType.Chasm select t).Count();
                    if (chasmCount > 0)
                    {
                        Tile thisTile = _map.GetTile(x, y);
                        if (thisTile.Type == TileType.SolidRock)
                        {
                            bool erode = false;
                            if (chasmCount > 2)
                                erode = true;
                            else
                            {
                                if (Dice.Next(5 - chasmCount) == 0)
                                    erode = true;
                            }
                            if (erode)
                                changes.Add(thisTile);
                        }
                    }
                }
            foreach (Tile change in changes)
                change.Type = TileType.Chasm;
        }

        

        public Map CreateMap(int width, int height, double chasmFraction)
        {
            _map = new Map(width, height);
            CreateLava(chasmFraction);
            _map.GetTile(30, 30).WaterLevel = 100;
            return _map;
        }

        void CreateChasms(double cavernFraction)
        {
            NoiseField noise = new NoiseField(_map.Width, _map.Height);
            noise.GenerateRandomNoise();
            //for (int x = 0; x < _map.Width; x++)
            //{
            //    noise.Values[x, 0] = 1;
            //    noise.Values[x, _map.Height - 1] = 10;
            //}
            //for (int y = 0; y < _map.Height; y++)
            //{
            //    noise.Values[0, y] = 1;
            //    noise.Values[_map.Width - 1, y] = 10;
            //}
            NoiseField res = noise.GenerateOctave(1, 1);
            res.Add(noise.GenerateOctave(0.5, 2));
            res.Add(noise.GenerateOctave(0.1, 64));
            res.Normalize();
            double threshold = res.NthValue(cavernFraction);

            for (int y = 0; y < _map.Width; y++)
                for (int x = 0; x < _map.Height; x++)
                {
                    if (res.Values[x, y] < threshold)
                        _map.GetTile(x, y).Type = TileType.Chasm;
                }

            Clean(TileType.StoneFloor);
            Clean(TileType.SolidRock);
            Clean(TileType.Lava);
            Clean(TileType.Chasm);
        }

        void CreateLava(double chasmFraction)
        {
            NoiseField noise = new NoiseField(_map.Width, _map.Height);
            noise.GenerateRandomNoise();
            NoiseField res = noise.GenerateOctave(1, 1);
            res.Add(noise.GenerateOctave(0.5, 4));
            //res.Add(noise.GenerateOctave(0.12, 32));
            res.Add(noise.GenerateOctave(0.1, 16));
            
            //res.Add(noise.GenerateOctave(0.02, 64));
            res.Normalize();

            double threshold = res.NthValue(chasmFraction);
            
            for (int y = 0; y < _map.Width; y++)
                for (int x = 0; x < _map.Height; x++)
                {
                    if (res.Values[x, y] < threshold)
                        _map.GetTile(x, y).Type = TileType.Lava;
                    //else
                      //  _map.GetTile(x, y).Type = TileType.SolidRock;
                }

            //remove tiny islands of chasm and solid rock
            Clean(TileType.Chasm);
            Clean(TileType.SolidRock);
            Clean(TileType.StoneFloor);

            //for (int i = 0; i < 10; i++)
              // Erode();
        }

        /// <summary>
        /// Removes tiny islands of a particular tile type
        /// </summary>
        public void Clean(TileType type)
        {
            //group all adjacent tiles of the specified type. Uses the region finding method in image processing
            Dictionary<Tile, HashSet<Tile>> tileGroups = new Dictionary<Tile, HashSet<Tile>>();
            List<HashSet<Tile>> groups = new List<HashSet<Tile>>();
            for (int y = 0; y < _map.Width; y++)
                for (int x = 0; x < _map.Height; x++)
                {
                    Tile tile = _map.GetTile(x,y);
                    if (tile.Type != type)
                        continue;
                    HashSet<Tile> leftGroup = null;
                    HashSet<Tile> topGroup = null;
                    Tile left = tile.GetNeighbour(CompassPoint.West);
                    if (left != null && left.Type == type)
                        leftGroup = tileGroups[left];
                    Tile top = tile.GetNeighbour(CompassPoint.North);
                    if (top != null && top.Type == type)
                        topGroup = tileGroups[top];
                    if (leftGroup != null && topGroup != null)
                    {
                        if (leftGroup != topGroup)
                        {
                            //combine the groups
                            //eliminate the top group and put all tiles in it into the left group
                            foreach (Tile t in topGroup)
                            {
                                tileGroups[t] = leftGroup;
                                leftGroup.Add(t);
                            }
                            topGroup.Clear();
                        }
                        //add this tile to the left group
                        tileGroups[tile] = leftGroup;
                        leftGroup.Add(tile);
                    }
                    else if (leftGroup != null)
                    {
                        tileGroups[tile] = leftGroup;
                        leftGroup.Add(tile);
                    }
                    else if (topGroup != null)
                    {
                        tileGroups[tile] = topGroup;
                        topGroup.Add(tile);
                    }
                    else
                    {
                        //top and left tiles are not in existing groups
                        //create a new group for this tile
                        HashSet<Tile> newGroup = new HashSet<Tile>();
                        groups.Add(newGroup);
                        newGroup.Add(tile);
                        tileGroups[tile] = newGroup;
                    }
                }

            int minimumGroupSize = 10;
            //now go through the groups and reset any groups that are too small
            foreach (HashSet<Tile> g in groups)
            {
                //
                if (g.Count > 0 && g.Count < minimumGroupSize)
                {
                    //go through the tiles in the group looking for a neighbouring tile of a different type
                    //eg if these are groups of chasm tiles and one of the tiles has a solidrock neighbour then
                    //we use that. There should always be an adjacent tile of a different type.
                    TileType replacementType = GetDifferentAdjacentTileType(g);
                    foreach (Tile t in g)
                        t.Type = replacementType;
                }
            }
        }

        /// <summary>
        /// Returns the type of a tile adjacent to one of the specified tiles which is different than the
        /// type of that specified tile. Eg passing in a group of abyss tiles and one of them borders a solidrock tile,
        /// the method will return solidrock
        /// </summary>
        TileType GetDifferentAdjacentTileType(HashSet<Tile> tiles)
        {
            foreach (Tile t in tiles)
                foreach (Tile n in _map.GetAdjacentTiles(t.X, t.Y))
                    if (n.Type != t.Type)
                        return n.Type;
            //have to return a default even though this should never be reached
            return TileType.SolidRock;
        }


        /// <summary>
        /// Returns multiple lists of connected templates. Templates in the same list are connected directly or indirectly.
        /// Templates in different lists have no path to one another.
        /// </summary>
        /// <returns></returns>
        List<List<Template>> GetConnectedTemplateLists()
        {
            List<List<Template>> ret = new List<List<Template>>();

            HashSet<Template> inExistingGroup = new HashSet<Template>();
            
            foreach (Template template in _templates)
            {
                //from this room generated a list of connected rooms by visiting each neighbour in turn
                List<Template> connectedTemplates = new List<Template>();
                Queue<Template> templatesToVisit = new Queue<Template>();
                templatesToVisit.Enqueue(template);

                bool groupExists = false;
                while (templatesToVisit.Count > 0)
                {
                    Template currentTemplate = templatesToVisit.Dequeue();

                    //if the current template is already in an existing saved list then
                    //the list being generated must already be saved so exit
                    if (inExistingGroup.Contains(currentTemplate))
                    {
                        groupExists = true;
                        break;
                    }
                    if (connectedTemplates.Contains(currentTemplate))
                        continue;
                    connectedTemplates.Add(currentTemplate);
                    foreach (Template r in currentTemplate.ConnectedTemplates)
                        templatesToVisit.Enqueue(r);
                }
                //if there were no more templates to visit then the list must be a new connected group
                //so add it to the return list
                if (!groupExists)
                {
                    ret.Add(connectedTemplates);
                    //note that each template in the new list is in an existing group to be returned so
                    //that if it is encountered again we know the list is already being returned
                    foreach (Template r in connectedTemplates)
                        inExistingGroup.Add(r);
                }
            }
            return ret;
        }

        List<Template> _templates = new List<Template>();

        Template AddTemplateTo(Template existingPlacedTemplate, JoinTile existingJoin=null)
        {
            if (existingJoin == null)
            {
                //obtain a random join by which to connect the new template
                existingJoin = existingPlacedTemplate.GetRandomUnconnectedJoin();
                if (existingJoin == null)
                    return null;
            }
            Template newTemplate = GetRandomTemplate();
            
            //obtain a random join of the new template by which to connect it to the existing template
            JoinTile newJoin = newTemplate.GetRandomUnconnectedJoin();

            //connect the new template to the existing template
            TemplateConnection connection = newTemplate.ConnectTo(existingJoin, newJoin);
            
            if (Validate(newTemplate))
                AddTemplateToMap(newTemplate);
            else
            {
                //template is not valid so isn't added to the map
                //remove the connection to it from the existing template
                connection.Remove();
                return null;
            }
            return newTemplate;
        }

        List<JoinTile> _joinsOnMap = new List<JoinTile>();

        public void AddTemplateToMap(Template template)
        {
            _templates.Add(template);
            foreach (JoinTile join in template.UnconnectedJoins)
                _joinsOnMap.Add(join);
        }

        public void RemoveTemplateFromMap(Template template)
        {
            foreach (TemplateConnection connection in template.Connections.ToList())
                connection.Remove();
            _templates.Remove(template);
            foreach (JoinTile join in template.Joins)
                _joinsOnMap.Remove(join);
        }
       
        public bool Validate(Template template, bool ignoreChasm=false)
        {
            if (template.X < 0 || template.Y < 0 || template.X + template.Width > _map.Width || template.Y + template.Height > _map.Height)
                return false;
            for (int y=template.Y;y<template.Y + template.Height;y++)
                for (int x=template.X;x<template.X + template.Width;x++)
                {
                    TemplateTile tile = template.GetMapTile(x, y);
                    if (tile == null)
                        continue;
                    Tile mapTile = _map.GetTile(x, y);
                    if (mapTile.Type == TileType.StoneFloor)
                        return false;
                    if (!ignoreChasm && (mapTile.Type == TileType.Chasm || mapTile.Type == TileType.Lava) && tile.Type == TileType.StoneFloor)
                        return false;
                    foreach (Template existingTemplate in _templates)
                    {
                        if (existingTemplate == template)
                            continue;
                        TemplateTile existingTile = existingTemplate.GetMapTile(x, y);
                        if (existingTile == null)
                            continue;
                        
                        //trying to place an an exclusive tile but one already exists here
                        if (tile.Exclusive)
                            return false;
                        //an existing exclusive tile exists here
                        if (existingTile.Exclusive)
                            return false;
                        
                        //an existing join tile that is connected cannot be overlapped unless it is connected to this template
                        JoinTile existingJoin = existingTile as JoinTile;
                        if (existingJoin != null && existingJoin.IsConnected && !existingJoin.Connects(template))
                            return false;

                        if (existingTile.SemiExclusion && tile.SemiExclusion)
                        {
                            //overlapping semi-exclusion zone in this tile, which means
                            //overlap is acceptable if the two templates are connected

                            //TODO this prevents non connected rooms from sharing walls...this can be a 
                            //problem with corners...
                            //if (template.IsConnectedTo(existingTemplate))
                                continue;
                        }
                    }
                }
            return true;
        }

        public void Test()
        {
            foreach (Template t in _templates)
            {
                bool b = Validate(t);
            }
        }

        class PlacementMap
        {
            int[,] _scores;
            Map _map;
            public PlacementMap(MapDesigner designer, Map map, Template template)
            {
                _map = map;
                _scores = new int[map.Width, map.Height];
                for (int y = 0; y < map.Height; y++)
                    for (int x = 0; x < map.Width; x++)
                        _scores[x, y] = GetPlacementScore(designer, x, y, template);
            }

            public Point BestPosition()
            {
                Point ret = new Point();
                int highest = 0;
                for (int y = 0; y < _map.Height; y++)
                    for (int x = 0; x < _map.Width; x++)
                        if (_scores[x, y] > highest)
                        {
                            ret.X = x;
                            ret.Y = y;
                            highest = _scores[x, y];
                        }
                return ret;
            }

            int GetPlacementScore(MapDesigner designer, int mapX, int mapY, Template template)
            {
                template.X = mapX;
                template.Y = mapY;
                if (!designer.Validate(template))
                    return 0;
                int score = 1;
                for (int ty = 0; ty < template.Height; ty++)
                    for (int tx = 0; tx < template.Width; tx++)
                    {
                        int mx = mapX + tx;
                        int my = mapY + ty;
                        if (!template.IsEdge(mx, my))
                        {
                            //continue to next tile as only edge tiles can border an abyss
                            continue;
                        }
                        if (IsAbyss(template, mx + 1, my))
                            score++;
                        if (IsAbyss(template, mx - 1, my))
                            score++;
                        if (IsAbyss(template, mx, my - 1))
                            score++;
                        if (IsAbyss(template, mx, my + 1))
                            score++;
                    }
                return score;
            }

            bool IsAbyss(Template template, int mx, int my)
            {
                TemplateTile tile = template.GetMapTile(mx, my);
                //if the template has content on this tile it cannot be an abyss
                if (tile != null && tile.Type != TileType.None)
                    return false;
                Tile mapTile = _map.GetTile(mx, my);
                if (mapTile == null)
                    return false;
                if (mapTile.Type != TileType.Chasm)
                    return false;
                return true;
            }
        }

        public Tile GetRandomChasmTile()
        {
            List<Tile> _chasmTiles = new List<Tile>();
            for (int y=0;y<_map.Height;y++)
                for (int x = 0; x < _map.Width; x++)
                {
                    Tile t = _map.GetTile(x, y);
                    if (t.Type == TileType.Chasm)
                        _chasmTiles.Add(t);
                }
            if (_chasmTiles.Count == 0)
                return null;
            return _chasmTiles[Dice.Next(_chasmTiles.Count)];
        }

        public void Extend(Template template)
        {
            List<JoinTile> joinTiles = template.UnconnectedJoins;
            
        }

        Template GetRandomTemplate()
        {
            int room = Dice.Next(9) + 1;
            //room = 1;
            Template ret = TemplateLoader.GetNewTemplate("room" + room + ".bmp");
            return ret;
        }

        public Template ExtendTemplate(Template existingTemplate, JoinTile existingJoin=null)
        {
            if (existingJoin == null)
            {
                //obtain a random join by which to connect the new template
                existingJoin = existingTemplate.GetRandomUnconnectedJoin();
                if (existingJoin == null)
                    return null;
            }
            
            Template newTemplate = GetRandomTemplate();
            JoinTile newJoin = newTemplate.GetRandomUnconnectedJoin();
            if (newJoin == null)
                return null;

            List<Point> attemptPath = new List<Point>();

            int connectionMode = 0;
            if (Dice.Next(2) == 1)
                connectionMode = 1;

            CompassPoint connectionDirection = CompassPoint.North;
            if (connectionMode == 0)
            {
                //calculate the maximum distance to place the new from from the existing room
                int maxDistance = Dice.Next(10);
                Point projectionVector = Compass.GetDirectionVector(existingJoin.MapFacingDirection);
                for (int d = maxDistance; d >= 2; d--)
                {
                    //calculate the point the new template join has to be placed
                    attemptPath.Add(new Point(existingJoin.MapPosition.X + projectionVector.X * d,
                                              existingJoin.MapPosition.Y + projectionVector.Y * d));
                }
                connectionDirection = Compass.GetOppositeDirection(existingJoin.MapFacingDirection);
            }
            else
            {
                connectionDirection = Compass.GetLeftDirection(existingJoin.MapFacingDirection);
                if (Dice.Next(2) == 0)
                    connectionDirection = Compass.GetOppositeDirection(connectionDirection);
                
                Point projectionVectorA = Compass.GetDirectionVector(existingJoin.MapFacingDirection);
                Point projectionVectorB = Compass.GetDirectionVector(Compass.GetOppositeDirection(connectionDirection));
                int distanceA = Dice.Next(10);
                int distanceB = Dice.Next(10);
                for (int d = distanceB; d >= 2; d--)
                {
                    int x = existingJoin.MapPosition.X + projectionVectorA.X * distanceA + projectionVectorB.X * d;
                    int y = existingJoin.MapPosition.Y + projectionVectorA.Y * distanceA + projectionVectorB.Y * d;
                    attemptPath.Add(new Point(x,y));
                }
            }

            bool isValid = false;
            foreach (Point p in attemptPath)
            {
                newTemplate.PositionJoin(newJoin, p.X, p.Y, connectionDirection);
                if (Validate(newTemplate))
                {
                    isValid = true;
                    break;
                }
            }
            //if no valid placement for the template could be found
            if (!isValid)
                return null;

            AddTemplateToMap(newTemplate);

            CorridorBuilder cb = new CorridorBuilder(this, new RegularCorridorSet());// new SpikyCorridorSet());// RegularCorridorSet());
            bool success = cb.CreatePath(existingJoin, newJoin);
            //if (success)
            //{
            //    foreach (Template c in cb.Sections)
            //        _templatesToExtend.Add(c);
            //    foreach (Template c in cb.Corners)
            //        _templatesToExtend.Add(c);
            //}

            if (!success)
            {
                RemoveTemplateFromMap(newTemplate);
                return null;
            }
            return newTemplate;
        }

        public Template LoadTemplate(string templateName)
        {
            Template ret = TemplateLoader.GetNewTemplate(templateName);
            return ret;
        }

        public void BuildBridge(Template template)
        {
            BridgeBuilder bb = new BridgeBuilder(this, null);
            //Find a join of the template that faces onto a chasm/lake
            foreach (JoinTile join in template.UnconnectedJoins.ToArray())
            {
                //see if the join can support a bridge
                if (bb.IsValidBridgeStart(join))
                {
                    if (bb.CreateBridge(join))
                        return;
                }
            }
        }

        public Map Map
        {
            get
            {
                return _map;
            }
        }

        

        

        public abstract class DistanceMap
        {
            protected Dictionary<object, NodeInfo> _nodes = new Dictionary<object, NodeInfo>();
                
            protected class NodeInfo
            {
                public object Object;
                public int ShortestDistance;

                //Previous node in the shortest path to this node
                public object PathLastNode;
            }

            public class NeighbourInfo
            {
                public object Neighbour;
                public int Distance;
            }

            protected void AddNode(object o)
            {
                NodeInfo info = new NodeInfo();
                info.Object = o;
                _nodes.Add(o, info);
            }

            public void Generate()
            {
                Queue<NodeInfo> nodesToProcess = new Queue<NodeInfo>();
                foreach (NodeInfo node in _nodes.Values)
                    nodesToProcess.Enqueue(node);
                while (nodesToProcess.Count > 0)
                {
                    NodeInfo currentNode = nodesToProcess.Dequeue();

                    List<NeighbourInfo> neighbours = GetNeighbours(currentNode.Object);
                    foreach (NeighbourInfo n in neighbours)
                    {
                        //skip any node we've just come from
                        if (currentNode.PathLastNode == n)
                            continue;
                        int distance = currentNode.ShortestDistance + n.Distance;
                        NodeInfo node;
                        if (_nodes.TryGetValue(n.Neighbour, out node))
                        {
                            if (distance < node.ShortestDistance)
                            {
                                node.ShortestDistance = distance;
                                node.PathLastNode = currentNode;
                                nodesToProcess.Enqueue(node);
                            }
                        }
                        else
                        {
                            node = new NodeInfo();
                            node.PathLastNode = currentNode;
                            node.ShortestDistance = distance;
                            node.Object = n.Neighbour;
                            _nodes.Add(node.Object, node);
                            nodesToProcess.Enqueue(node);
                        }
                    }
                }
            }

            protected abstract List<NeighbourInfo> GetNeighbours(object o);
        }

        class TemplateDistanceMap : DistanceMap
        {
            public void SetStartTemplate(Template template)
            {
                foreach (TemplateConnection connection in template.Connections)
                    AddNode(connection);
            }

            protected override List<NeighbourInfo> GetNeighbours(object o)
            {
                List<NeighbourInfo> ret = new List<NeighbourInfo>();
                TemplateConnection tc = o as TemplateConnection;
                List<TemplateConnection> neighbouringConnections = new List<TemplateConnection>();
                neighbouringConnections.AddRange(tc.Join1.Template.Connections);
                neighbouringConnections.AddRange(tc.Join2.Template.Connections);
                
                foreach (TemplateConnection n in neighbouringConnections)
                {
                    int distance = Math.Abs(n.X - tc.X) + Math.Abs(n.Y - tc.Y);
                    //if distance is zero then the "neighbour" connection is actually the current connection
                    //and should be skipped
                    if (distance == 0)
                        continue;
                    NeighbourInfo nInfo = new NeighbourInfo();
                    nInfo.Neighbour = n;
                    nInfo.Distance = distance;
                    ret.Add(nInfo);
                }
                return ret;
            }

            public int GetShortestDistance(Template target)
            {
                int ret = 9999;
                foreach (NodeInfo n in _nodes.Values)
                {
                    TemplateConnection connection = n.Object as TemplateConnection;
                    if (connection.Join1.Template == target || connection.Join2.Template == target)
                    {
                        if (n.ShortestDistance < ret)
                            ret = n.ShortestDistance;
                    }
                }
                return ret;
            }
        }
        
        public int GetShortestDistanceBetweenTemplates(Template template1, Template template2)
        {
            TemplateDistanceMap map = new TemplateDistanceMap();
            map.SetStartTemplate(template1);
            map.Generate();
            return map.GetShortestDistance(template2);
        }

        public int DistanceBetween(Template t1, Template t2)
        {
            int xDist = 0;
            if (t2.X > t1.X + t1.Width)
                xDist = t2.X - (t1.X + t1.Width);
            else if (t2.X + t2.Width < t1.X)
                xDist = t1.X - (t2.X + t2.Width);

            int yDist = 0;
            if (t2.Y > t1.Y + t1.Height)
                yDist = t2.Y - (t1.Y + t1.Height);
            else if (t2.Y + t2.Height < t1.Y)
                yDist = t1.Y - (t2.Y + t2.Height);
            return Math.Max(xDist, yDist);
        }

        public List<Template> GetNearbyTemplates(Template template)
        {
            List<Template> ret = new List<Template>();
            foreach (Template t in _templates)
            {
                if (DistanceBetween(template, t) < 5)
                    ret.Add(t);
            }
            return ret;
        }

        /// <summary>
        /// Attempt to join two Join two templates creating a loop
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        void JoinTemplates(Template t1, Template t2)
        {
            CorridorBuilder cb = new CorridorBuilder(this, new RegularCorridorSet());// new SpikyCorridorSet());// RegularCorridorSet());

            bool success = false;
            foreach (JoinTile j1 in t1.UnconnectedJoins.ToArray())
            {
                foreach (JoinTile j2 in t2.UnconnectedJoins.ToArray())
                {
                    success = cb.CreatePath(j1, j2);
                    if (success)
                        break;
                }
                if (success)
                    break;
            }
        }

        public int NumTemplates
        {
            get
            {
                return _templates.Count;
            }
        }

        List<Template> _templatesToExtend;

        void FillMap(TileType tileType)
        {
            for (int y = 0; y < _map.Height; y++)
                for (int x = 0; x < _map.Width; x++)
                    _map.GetTile(x, y).Type = tileType;
        }

        void CreateNaturalMap()
        {
            FillMap(TileType.SolidRock);
            
            CreateLava(0.2); //0.4
            CreateChasms(0.2);
            AddGoldOre();
            
        }


        public Map CreateMap(int width, int height)
        {
            _map = new Map(width, height);

            CreateNaturalMap();

            //return _map;

            Bitmap bmp = _map.ToBitmap(new HashSet<Tile>(), new HashSet<Tile>());
            //bmp.Save(@"c:\projects\orig.bmp");

            //Tile chasmTile = GetRandomChasmTile();
            //if (chasmTile != null)
            //{
            //    CompassPoint direction;
            //    if (Dice.Next(2) == 0)
            //        direction = CompassPoint.North;
            //    else
            //        direction = CompassPoint.East;
            //    BridgeBuilder builder = new BridgeBuilder();
            //}

            for (int i = 0; i < 1; i++)
            {
                Template temp = GetRandomTemplate();

                PlacementMap pmap = new PlacementMap(this, _map, temp);
                Point bp = pmap.BestPosition();
                temp.X = bp.X;
                temp.Y = bp.Y;
                if (Validate(temp))
                    AddTemplateToMap(temp);
            }

            int lastAdded = 0;
            for (int i = 0; i < 10000; i++)
            {
                if (NumTemplates > MaxTemplates)
                    break;
                lastAdded++;
                if (lastAdded > 1000)
                    break;
                var orderedJoins = (from j in _joinsOnMap orderby j.Priority descending select j).ToList();
                int totalPriority = (from j in orderedJoins select j.Priority).Sum();
                int randomL = Dice.Next(totalPriority);
                int c = 0;
                foreach (JoinTile j in orderedJoins)
                {
                    c += j.Priority;
                    if (randomL < c)
                    {
                        Template newTemplate = null;
                        if (Dice.Next(2) == 1)
                            newTemplate = AddTemplateTo(j.Template, j);
                        else
                            newTemplate = ExtendTemplate(j.Template, j);
                        if (newTemplate == null)
                        {
                            j.Priority -= 5;
                            if (j.Priority < 0)
                                j.Priority = 0;
                        }
                        else
                        {
                            lastAdded = 0;
                            j.Priority = 0;
                            BuildBridge(newTemplate);
                        }
                        break;
                    }
                }
            }

            foreach (Template t in _templates.ToArray())
            {
                List<Template> nearbys = GetNearbyTemplates(t); //_templates.ToList(); // 
                foreach (Template nearby in nearbys)
                {
                    int dist = GetShortestDistanceBetweenTemplates(t, nearby);
                    if (dist >= 10)
                    {
                        JoinTemplates(t, nearby);
                    }
                }
            }

            //int dist = GetShortestDistanceBetweenTemplates(_templates[0], _templates[40]);

            //_templatesToExtend = new List<Template>();
            //_templatesToExtend.Add(_templates[0]);
            for (int i=0;i<0;i++)
            {
                foreach (Template t in _templatesToExtend.ToArray())
                {
                    for (int a=20;a>=0;a--)
                    {
                        if (a == 0)
                        {
                            _templatesToExtend.Remove(t);
                            break;
                        }

                        BuildBridge(t);

                        Template newTemplate = null;
                        //if (Dice.Next(2) == 1)
                            newTemplate = ExtendTemplate(t);
                        //else
                        //    newTemplate = AddTemplateTo(t);
                        if (newTemplate != null)
                        {
                            _templatesToExtend.Add(newTemplate);
                            break;
                        }
                    }
                }
            }

            CorridorBuilder cb = new CorridorBuilder(this, new RegularCorridorSet());

            for (int i = 0; i < 0; i++)
            {
                if (_templates.Count() > 0)
                {
                    for (int l = 0; l < 4; l++)
                    {
                        Template t = _templates[Dice.Next(_templates.Count)];
                        if (t.GetRandomUnconnectedJoin() == null)
                            continue;
                        Template newTemplate = AddTemplateTo(t);
                    }
                }

                if (Dice.Next(4) == 1)
                {
                    Template roomTemplate = GetRandomTemplate();
                    roomTemplate.PositionJoin(roomTemplate.GetRandomUnconnectedJoin(), Dice.Next(_map.Width), Dice.Next(_map.Height), CompassPoint.East);
                    if (Validate(roomTemplate))
                        AddTemplateToMap(roomTemplate);
                }

                //try to add a few corridors between existing rooms now and again
                if (_templates.Count > 2)
                {
                    for (int u=0;u<100;u++)
                    {
                        Template t1 = _templates[Dice.Next(_templates.Count)];
                        Template t2 = _templates[Dice.Next(_templates.Count)];
                        if (t1 == t2)
                            continue;

                        JoinTile join1 = null;
                        JoinTile join2 = null;
                        join1 = t1.GetRandomUnconnectedJoin();
                        join2 = t2.GetRandomUnconnectedJoin();
                        if (join1 == null || join2 == null)
                            continue;
                        if (Math.Abs(join1.MapPosition.X - join2.MapPosition.X) > 10 ||
                            Math.Abs(join1.MapPosition.Y - join2.MapPosition.Y) > 10)
                            continue;
                        if (cb.CreatePath(join1, join2))
                            break;
                    }
                }
            }

            for (int u = 0; u < 0; u++)
            {
                Template t1 = _templates[Dice.Next(_templates.Count)];
                Template t2 = _templates[Dice.Next(_templates.Count)];
                if (t1 == t2)
                    continue;

                JoinTile join1 = null;
                JoinTile join2 = null;
                join1 = t1.GetRandomUnconnectedJoin();
                join2 = t2.GetRandomUnconnectedJoin();
                if (join1 == null || join2 == null)
                    continue;
            }


            //RemoveUnconnectedTemplateSections();

            foreach (Template t in _templates)
                t.Build(_map);

            //roomTemplate.Build(_map);

            AddWindows();

            AddHidingHoles();
            
            //CreateBridges();


            return _map;
        }

        void AddWindows()
        {
            for (int y = 0; y < _map.Height; y++)
                for (int x = 0; x < _map.Width; x++)
                {
                    Tile tile = _map.GetTile(x, y);
                    if (tile.Type != TileType.StoneWall)
                        continue;
                    Tile north = tile.GetNeighbour(CompassPoint.North);
                    Tile south = tile.GetNeighbour(CompassPoint.South);
                    Tile east = tile.GetNeighbour(CompassPoint.East);
                    Tile west = tile.GetNeighbour(CompassPoint.West);
                    if (north != null && south != null && east != null && west != null)
                    {
                        if (((north.Type == TileType.Chasm || north.Type == TileType.Lava) && south.Type == TileType.StoneFloor) ||
                            ((south.Type == TileType.Chasm || south.Type == TileType.Lava) && north.Type == TileType.StoneFloor))
                        {
                            //if (east.Type == TileType.StoneWall && west.Type == TileType.StoneWall)
                            {
                                if (Dice.Next(5) > 0)
                                    tile.Type = TileType.WindowNS;
                            }
                        }
                        if (((east.Type == TileType.Chasm || east.Type == TileType.Lava) && west.Type == TileType.StoneFloor) ||
                            ((west.Type == TileType.Chasm || west.Type == TileType.Lava) && east.Type == TileType.StoneFloor))
                        {
                            //if (north.Type == TileType.StoneWall && south.Type == TileType.StoneWall)
                            {
                                if (Dice.Next(5) > 0)
                                    tile.Type = TileType.WindowEW;
                            }
                        }
                    }
                }
        }

        /// <summary>
        /// Hiding holes are small single tile caves that the player can hide in. Zombies and snakes cannot
        /// see the player when they are in a hiding hole and will lose track of them. Zombies will never
        /// enter a hiding hole, but snakes can randomly do so.
        /// </summary>
        void AddHidingHoles()
        {
            int added = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (added == NumHidingHoles)
                    break;
                Tile tile = _map.GetRandomTile();
                //don't build hiding holes at the edge of the map or creatures can escape the map!
                if (tile.X == 0 || tile.Y == 0 || tile.X == _map.Width - 1 || tile.Y == _map.Height - 1)
                    continue;
                if (tile.IsWall)
                {
                    List<Tile> neighbours = _map.GetAdjacentTiles(tile.X, tile.Y);
                    if ((from n in neighbours where n.IsFloor select n).Count() == 1)
                    {
                        tile.Type = TileType.HidingHole;
                        added++;
                        //_map.SetTile(tile.X, tile.Y, new HidingHoleTile());
                    }
                }
            }
        }

        void AddGoldOre()
        {
            for (int i = 0; i < 30; i++)
            {
                Tile tile = _map.GetRandomTile();
                if (tile.Type == TileType.SolidRock)
                {
                    tile.Type = TileType.GoldOre;
                }
            }
        }

        public int MaxTemplates { get; set; }
        public int NumHidingHoles;
    }
}
