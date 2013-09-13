using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TempRl
{
    /// <summary>
    /// Can be used to lay a series of corridor and corner templates between two joins to produce a corridor on the map running between them
    /// </summary>
    public class CorridorBuilder
    {
        MapDesigner _designer;
        List<Template> _sections = new List<Template>();
        List<Template> _corners = new List<Template>();
        CorridorSet _corridorSet;

        public bool IgnoreChasm = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="designer">Map designer</param>
        /// <param name="corridorSet">The style set to use for the corridor</param>
        public CorridorBuilder(MapDesigner designer, CorridorSet corridorSet)
        {
            _designer = designer;
            _corridorSet = corridorSet;
        }

        public List<Template> Sections
        {
            get
            {
                return _sections;
            }
        }

        public List<Template> Corners
        {
            get
            {
                return _corners;
            }
        }

        /// <summary>
        /// Attempt to create a new corridor between the two specified joins on the map. Determines a path between the joins
        /// and lays corridor and corner templates along the path to join it up. If any template fails to validate then all are removed.
        /// This method either succeeds in which case all templates are placed on the map, or it fails and no templates are placed.
        /// </summary>
        /// <param name="joinStart"></param>
        /// <param name="joinEnd"></param>
        /// <returns>true if the corridor was created, false if creation failed</returns>
        public bool CreatePath(JoinTile joinStart, JoinTile joinEnd)
        {
            _sections.Clear();
            _corners.Clear();
            bool success = CreatePathEx(joinStart, joinEnd);
            if (!success)
            {
                foreach (Template section in _sections)
                    _designer.RemoveTemplateFromMap(section);
                foreach (Template corner in _corners)
                    _designer.RemoveTemplateFromMap(corner);
            }
            return success;
        }

        /// <summary>
        /// Adds a repeatable corridor section that connects to one end of a path
        /// </summary>
        /// <param name="join">The join to connect the repeatable section to</param>
        /// <param name="connectionDir">Which edge of the repeatable corridor section to connect to the join.
        /// For corridors this can be north or south</param>
        /// <param name="templateName">The name of the repeatable corridor section template</param>
        /// <returns></returns>
        Template AddEndSection(JoinTile join, CompassPoint connectionDir, string templateName)
        {
            Template section = TemplateLoader.GetNewTemplate(templateName);
            JoinTile sectionJoin = section.GetRandomUnconnectedJoin(connectionDir);
            if (sectionJoin == null)
                return null;
            TemplateConnection connection = section.ConnectTo(join, sectionJoin);
            if (!_designer.Validate(section, IgnoreChasm))
            {
                connection.Remove();
                return null;
            }
            _designer.AddTemplateToMap(section);
            _sections.Add(section);
            return section;
        }

        /// <summary>
        /// Adds a corridor section that bridges two existing corridor sections A and B
        /// </summary>
        /// <param name="startJoin">The join of existing corridor section A to connect this section to</param>
        /// <param name="endJoin">The join of existing corridor section B to connect this section to</param>
        /// <param name="section">The corridor section to bridge with</param>
        /// <returns>true if the section was placed</returns>
        bool AddCentralSection(JoinTile startJoin, JoinTile endJoin, Template section)
        {
            JoinTile sectionJoinStart = section.GetRandomUnconnectedJoin(CompassPoint.North);
            if (sectionJoinStart == null)
                return false;
            JoinTile sectionJoinEnd = section.GetRandomUnconnectedJoin(CompassPoint.South);
            if (sectionJoinEnd == null)
                return false;
            TemplateConnection connection = section.ConnectTo(startJoin, sectionJoinStart);
            TemplateConnection connectionEnd = section.ConnectTo(endJoin, sectionJoinEnd);
            if (!_designer.Validate(section, IgnoreChasm))
            {
                connection.Remove();
                connectionEnd.Remove();
                return false;
            }
            _designer.AddTemplateToMap(section);
            _sections.Add(section);
            return true;
        }

        /// <summary>
        /// Creates a path that bridges two joins. This Ex method is to support single point of return logic 
        /// in CreatePath()
        /// </summary>
        bool CreatePathEx(JoinTile joinStart, JoinTile joinEnd)
        {
            //obtain a path between the two joins
            List<Point> path = GetPath(joinStart, joinEnd);
            if (path == null)
                return false;

            CompassPoint pathDirection = joinStart.MapFacingDirection;
            Point lastPosition = joinStart.MapPosition;
            JoinTile lastJoin = joinStart;
            for (int i = 1; i < path.Count - 1; i++)
            {
                CompassPoint inDirection = Compass.GetDirectionOf(path[i], path[i - 1]);
                CompassPoint outDirection = Compass.GetDirectionOf(path[i], path[i + 1]);
                JoinTile inJoin;
                JoinTile outJoin;
                Template corner = CreateCorner(path[i], inDirection, outDirection, out inJoin, out outJoin);
                if (corner == null)
                    return false;
                //connect corner to previous join
                //if the corner matches up exactl with the previous join then connect them directly otherwise create
                //a corridor to bridge them
                if (lastJoin.MapPosition.X == inJoin.MapPosition.X && lastJoin.MapPosition.Y == inJoin.MapPosition.Y)
                    corner.ConnectTo(lastJoin, inJoin);
                else
                {
                    bool success = CreateCorridor(lastJoin, inJoin);
                    if (!success)
                        return false;
                }
                lastJoin = outJoin;
            }
            return CreateCorridor(lastJoin, joinEnd);
        }

        /// <summary>
        /// Creates a corner template and orientates it on the map so that two of its joins match the specified in and out direction
        /// and row/columns specified in the position.
        /// </summary>
        /// <param name="position">row/column of the paths in and out of the corner</param>
        /// <param name="inDirection">direction of the path into the corner</param>
        /// <param name="outDirection">direction of the path out of the corner</param>
        /// <param name="inJoin">returns the join belonging to the produced corner which attaches to the in path</param>
        /// <param name="outJoin">returns the join belonging to the produced corner which attaches to the out path</param>
        /// <returns></returns>
        Template CreateCorner(Point position, CompassPoint inDirection, CompassPoint outDirection, out JoinTile inJoin, out JoinTile outJoin)
        {
            Template ret = TemplateLoader.GetNewTemplate(_corridorSet.CornerTemplateName);
            inJoin = null;
            outJoin = null;

            JoinTile join1 = null;
            JoinTile join2 = null;
            for (int i = 0; i < 4; i++)
            {
                join1 = ret.GetRandomUnconnectedJoin(inDirection);
                join2 = ret.GetRandomUnconnectedJoin(outDirection);
                if (join1 != null && join2 != null)
                    break;
                ret.Rotate90();
            }
            //if either join is null then no valid orientation of the corner could be found
            //(this shouldn't happen but just in case)
            if (join1 == null || join2 == null)
                return null;

            //The corner template is now orientated so that two joins face the necessary directions
            //Position the template so that the joins align with the path row/column

            int posX = 0;
            int posY = 0;
            //if the direction is N or S then the template Y position is aligned with the other join
            //using join.MapX, MapY to obtain local coordinates works because the template X and Y is 0
            if (inDirection == CompassPoint.North || inDirection == CompassPoint.South)
                posY = position.Y - join2.MapPosition.Y;
            else
                posX = position.X - join2.MapPosition.X;

            if (outDirection == CompassPoint.North || outDirection == CompassPoint.South)
                posY = position.Y - join1.MapPosition.Y;
            else
                posX = position.X - join1.MapPosition.X;

            ret.X = posX;
            ret.Y = posY;

            if (!_designer.Validate(ret, IgnoreChasm))
                return null;

            inJoin = join1;
            outJoin = join2; 
            _designer.AddTemplateToMap(ret);
            _corners.Add(ret);
            return ret;
        }

        /// <summary>
        /// Returns a list of corners making up the path between two joins, or null if there are no
        /// valid paths
        /// </summary>
        List<Point> GetPath(JoinTile joinStart, JoinTile joinEnd)
        {
            List<Point> path = new List<Point>();
            path.Add(joinStart.MapPosition);

            if (joinStart.MapFacingDirection == joinEnd.MapFacingDirection)
            {
                //joins face the same direction, so build a U shaped corridor to link them
                return null;
            }
            else if (joinStart.MapFacingDirection == Compass.GetOppositeDirection(joinEnd.MapFacingDirection))
            {
                //invalid if join is facing away from the other join
                if (joinStart.MapFacingDirection == CompassPoint.East && joinStart.MapPosition.X >= joinEnd.MapPosition.X)
                    return null;
                if (joinStart.MapFacingDirection == CompassPoint.West && joinStart.MapPosition.X <= joinEnd.MapPosition.X)
                    return null;
                if (joinStart.MapFacingDirection == CompassPoint.South && joinStart.MapPosition.Y >= joinEnd.MapPosition.Y)
                    return null;
                if (joinStart.MapFacingDirection == CompassPoint.North && joinStart.MapPosition.Y <= joinEnd.MapPosition.Y)
                    return null;

                int distance = 0;
                if (joinStart.MapFacingDirection == CompassPoint.North || joinStart.MapFacingDirection == CompassPoint.South)
                {
                    if (joinStart.MapPosition.X == joinEnd.MapPosition.X)
                    {
                        //joins are aligned, straight path between both
                        path.Add(new Point(joinEnd.MapPosition.X, joinEnd.MapPosition.Y));
                        return path;
                    }
                    distance = Math.Abs(joinStart.MapPosition.Y - joinEnd.MapPosition.Y);
                    if (distance < 4)
                        return null;
                    int barY = joinStart.MapPosition.Y + (joinEnd.MapPosition.Y - joinStart.MapPosition.Y) / 2;
                    path.Add(new Point(joinStart.MapPosition.X, barY));
                    path.Add(new Point(joinEnd.MapPosition.X, barY));
                }
                else
                {
                    if (joinStart.MapPosition.Y == joinEnd.MapPosition.Y)
                    {
                        //joins are aligned, straight path between both
                        path.Add(new Point(joinEnd.MapPosition.X, joinEnd.MapPosition.Y));
                        return path;
                    }
                    distance = Math.Abs(joinStart.MapPosition.X - joinEnd.MapPosition.X);
                    if (distance < 4)
                        return null;
                    int barX = joinStart.MapPosition.X + (joinEnd.MapPosition.X - joinStart.MapPosition.X) / 2;
                    path.Add(new Point(barX, joinStart.MapPosition.Y));
                    path.Add(new Point(barX, joinEnd.MapPosition.Y));
                }
            }
            else
            {
                //joins are separated by 90 degrees. There is one L shaped path the corridor can take

                if (Math.Abs(joinStart.MapPosition.X - joinEnd.MapPosition.X) < 2 ||
                    Math.Abs(joinStart.MapPosition.Y - joinEnd.MapPosition.Y) < 2)
                    return null;

                //invalid if facing away from the other join
                if (joinStart.MapPosition.X > joinEnd.MapPosition.X && (joinStart.MapFacingDirection == CompassPoint.East || joinEnd.MapFacingDirection == CompassPoint.West))
                    return null;
                if (joinStart.MapPosition.X < joinEnd.MapPosition.X && (joinStart.MapFacingDirection == CompassPoint.West || joinEnd.MapFacingDirection == CompassPoint.East))
                    return null;
                if (joinStart.MapPosition.Y > joinEnd.MapPosition.Y && (joinStart.MapFacingDirection == CompassPoint.South || joinEnd.MapFacingDirection == CompassPoint.North))
                    return null;
                if (joinStart.MapPosition.Y < joinEnd.MapPosition.Y && (joinStart.MapFacingDirection == CompassPoint.North || joinEnd.MapFacingDirection == CompassPoint.South))
                    return null;

                Point join1dir = Compass.GetDirectionVector(joinStart.MapFacingDirection);
                Point join2dir = Compass.GetDirectionVector(joinEnd.MapFacingDirection);
                int cornerX = 0;
                int cornerY = 0;
                if (joinStart.MapFacingDirection == CompassPoint.North || joinStart.MapFacingDirection == CompassPoint.South)
                {
                    cornerX = joinStart.MapPosition.X;
                    cornerY = joinEnd.MapPosition.Y;
                }
                else
                {
                    cornerY = joinStart.MapPosition.Y;
                    cornerX = joinEnd.MapPosition.X;
                }
                path.Add(new Point(cornerX, cornerY));
            }
            path.Add(new Point(joinEnd.MapPosition.X, joinEnd.MapPosition.Y));
            return path;
        }

        bool CreateCorridor(JoinTile joinStart, JoinTile joinEnd)
        {
            //if the joins are both on the same tile then no need for a corridor, just connect them directly
            if (joinStart.MapPosition.X == joinEnd.MapPosition.X && joinStart.MapPosition.Y == joinEnd.MapPosition.Y)
            {
                joinStart.Template.ConnectTo(joinEnd, joinStart);
                return true;
            }
            int length;
            if (joinStart.MapFacingDirection == CompassPoint.North || joinStart.MapFacingDirection == CompassPoint.South)
                length = Math.Abs(joinEnd.MapPosition.Y - joinStart.MapPosition.Y);
            else
                length = Math.Abs(joinEnd.MapPosition.X - joinStart.MapPosition.X);

            if (_corridorSet.SupportedCentralLengths.Contains(length))
            {
                Template section = TemplateLoader.GetNewTemplate(_corridorSet.GetSectionTemplateName(length));
                return AddCentralSection(joinStart, joinEnd, section);
            }
            else
            {
                string templateName = _corridorSet.GetBestRepeatableSection(length);
                Template section = AddEndSection(joinStart, CompassPoint.North, templateName);
                Template section2 = AddEndSection(joinEnd, CompassPoint.South, templateName);
                if (section == null || section2 == null)
                    return false;
                joinStart = section.GetRandomUnconnectedJoin(joinStart.MapFacingDirection);
                joinEnd = section2.GetRandomUnconnectedJoin(joinEnd.MapFacingDirection);
                return CreateCorridor(joinStart, joinEnd);
            }
        }
    }

    public class BridgeCorridorSet : CorridorSet
    {
        public override string GetSectionTemplateName(int length)
        {
            switch (length)
            {
                case 1:
                    return "thinbridge1.bmp";
                case 2:
                    return "thinbridge2.bmp";
                case 4:
                    return "thinbridge4.bmp";
            }
            return "";
        }

        public override int[] RepeatableSectionLengths
        {
            get { return new int[] { 1, 2, 4 }; }
        }

        int[] _supported = new int[] { 1, 2 };
        public override int[] SupportedCentralLengths
        {
            get
            {
                return _supported;
            }
        }

        public override string CornerTemplateName
        {
            get { return "corner1.bmp"; }
        }
    }

    public class RegularCorridorSet : CorridorSet
    {
        public override string GetSectionTemplateName(int length)
        {
            switch (length)
            {
                case 1:
                    return "thincorridor1.bmp";
                case 2:
                    return "thincorridor2.bmp";
                case 4:
                    return "thincorridor4.bmp";
            }
            return "";
        }

        public override int[] RepeatableSectionLengths
        {
            get { return new int[] { 1,2,4 }; }
        }

        int[] _supported = new int[] { 1,2 };
        public override int[] SupportedCentralLengths
        {
            get
            {
                return _supported;
            }
        }

        public override string CornerTemplateName
        {
            get { return "corner1.bmp"; }
        }
    }

    public class SpikyCorridorSet : CorridorSet
    {
        public override string GetSectionTemplateName(int length)
        {
            switch (length)
            {
                case 1:
                    return "corridor1.bmp";
                case 2:
                    return "corridor2.bmp";
                case 3:
                    return "corridor3.bmp";
                case 4:
                    return "corridor4.bmp";
                case 5:
                    return "corridor5.bmp";
            }
            return "";
        }

        public override int[] RepeatableSectionLengths
        {
            get { return new int[] { 2 }; }
        }

        int[] _supported = new int[] { 1, 2, 3, 4, 5 };
        public override int[] SupportedCentralLengths
        {
            get 
            {
                return _supported;
            }
        }

        public override string CornerTemplateName
        {
            get { return "corner2.bmp"; }
        }
    }

    public abstract class CorridorSet
    { 
        abstract public string GetSectionTemplateName(int length);

        abstract public int[] RepeatableSectionLengths
        {
            get;
        }

        abstract public int[] SupportedCentralLengths
        {
            get;
        }

        abstract public string CornerTemplateName
        {
            get;
        }

        /// <summary>
        /// Returns the name of the longest repeatable section that can be used twice in the available space
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public string GetBestRepeatableSection(int length)
        {
            int longest = (from l in RepeatableSectionLengths where l * 2 < length select l).Max();
            return GetSectionTemplateName(longest);
        }
    }
}
