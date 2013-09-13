using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace TempRl
{
    public partial class Form1 : Form
    {
        public static int level = 0;
        System.Windows.Forms.Timer progressTimer;

        public static bool GameOver = false;
        public Form1()
        {
            InitializeComponent();
            progressTimer = new System.Windows.Forms.Timer();
            progressTimer.Tick += new EventHandler(progressTimer_Tick);
            progressTimer.Interval = 10;
            lblGameOver.BackColor = Color.Transparent;
        }

        void progressTimer_Tick(object sender, EventArgs e)
        {
            progressBar1.BeginInvoke(new Action(() =>
            {
                if (progressBar1.Value < progressBar1.Maximum)
                    progressBar1.Value++;
            }));
        }

        Map _map;

        Player _player = Player.Instance;

        private void button1_Click(object sender, EventArgs e)
        {
           
            

            //_map = new Map(60, 60);
            //map.Design();
            //map.Build();

            //map.Rooms[20].Color = Color.Red;
            //foreach (Room r in map.Rooms[20].Neighbours)
            //    r.Color = Color.Blue;
            //Bitmap bm = map.ToBitmap(5);
            //Bitmap sbm = new Bitmap(bm.Width * 5, bm.Height * 5);
            //Graphics g = Graphics.FromImage(sbm);
            //g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            //g.DrawImage(bm, new Rectangle(0, 0, sbm.Width, sbm.Height), 0, 0, bm.Width, bm.Height, GraphicsUnit.Pixel);
            //g.Dispose();
            
            //button1.Enabled = false;
        }
        MapDesigner designer;

        void Debug()
        {
            designer.Test();
        }

        int Distance(Tile tile1, Tile tile2)
        {
            return Math.Abs(tile1.X - tile2.X) + Math.Abs(tile1.Y - tile2.Y);
        }

        Map CreateMap(int level)
        {
            Map newMap;
            int numHidingHoles = 0;
            int mapSize = 30;
            if (level > 2)
                mapSize = 40;
            if (level > 5)
                mapSize = 50;
            if (level > 7)
                mapSize = 60;
            //determines the minimum and maximum number of templates (rooms and corridors) for the map to be
            //acceptable. The map generator will keep extending the map until the max limit is reached. After generation
            //if the map hasn't reached the minimum number the map will be invalid and a new map will be generated.
            int minTemplates = 4;
            int maxTemplates = 4;
            int numZombies = 0;
            int numSnakes = 0;
            int snakeLength = 6;
            //level = 5;
            if (level == 1)
            {
                numZombies = 0;
                minTemplates = 5;
                maxTemplates = 5;
            }
            if (level == 2)
            {
                numHidingHoles = 1;
                numZombies = 2;
                minTemplates = 15;
                maxTemplates = 15;
            }
            if (level == 3)
            {
                numHidingHoles = 2;
                numSnakes = 1;
                snakeLength = 10;
                minTemplates = 15;
                maxTemplates = 15;
            }
            if (level == 4)
            {
                numHidingHoles = 2;
                numZombies = 3;
                minTemplates = 20;
                maxTemplates = 20;
            }
            if (level == 5)
            {
                numHidingHoles = 3;
                numZombies = 5;
                minTemplates = 25;
                maxTemplates = 25;
            }
            
            if (level == 6)
            {
                mapSize = 50;
                numHidingHoles = 4;
                numZombies = 0;
                numSnakes = 3;
                snakeLength = 8;
                minTemplates = 30;
                maxTemplates = 30;
            }            
            if (level == 7)
            {
                numHidingHoles = 6;
                numZombies = 10;
                numSnakes = 1;
                snakeLength = 8;
                minTemplates = 50;
                maxTemplates = 50;
            }
            if (level == 8)
            {
                numHidingHoles = 8;
                mapSize = 50;
                numZombies = 0;
                numSnakes = 8;
                snakeLength = 8;
                minTemplates = 60;
                maxTemplates = 99;
            }

            while (true)
            {
                designer = new MapDesigner();
                designer.MaxTemplates = maxTemplates;
                designer.NumHidingHoles = numHidingHoles;
                newMap = designer.CreateMap(mapSize, mapSize); //, (double)numericChasmFraction.Value / 100);

                if (designer.NumTemplates >= minTemplates)
                    break;
            }

            Tile playerTile = newMap.GetRandomFloorTile();
            _player.Place(playerTile);


            int minDistance = 100;
            while (true)
            {
                minDistance--;
                Tile exitTile = newMap.GetRandomFloorTile();
                int distance = Math.Abs(exitTile.X - playerTile.X) + Math.Abs(exitTile.Y - playerTile.Y);
                if (distance < minDistance)
                    continue;
                exitTile.IsExit = true;
                break;
            }

            

            int zombiesPlaced = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (zombiesPlaced == numZombies)
                    break;
                Zombie z = new Zombie();
                Tile zombieTile = newMap.GetRandomFloorTile();
                if (Distance(zombieTile, playerTile) < 20)
                    continue;
                z.Place(zombieTile);
                zombiesPlaced++;
            }

            int createdSnakes = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (createdSnakes == numSnakes)
                    break;
                int length=snakeLength;

                Tile floorTile = newMap.GetRandomFloorTile();
                if (Distance(floorTile, playerTile) < 20)
                    continue;

                //if (d < 3)
                    //length = 10;
               // else if (d < 5)
                 //   length = Dice.Next(10) + Dice.Next(10);
                Serpent serpent = new Serpent(length);
                bool success = serpent.Place(floorTile);
                if (success)
                    createdSnakes++;
                
            }
            return newMap;
        }

        HashSet<Tile> _rememberedTiles = new HashSet<Tile>();
        void RefreshMap()
        {
            HashSet<Tile> visibleTiles = _player.GetVisibleTiles();
            _rememberedTiles.UnionWith(visibleTiles);
            pictureBox1.Image = _map.ToBitmap(visibleTiles, _rememberedTiles);

            //Room room = _map.GetRoomAt(_player.X, _player.Y);
            //if (room == null)
            //    lblRoomDescription.Text = "";
            //else
            //    lblRoomDescription.Text = room.FullDescription;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _map.GetTile(30, 30).WaterLevel = 100;
            for (int i = 0; i < 20; i++)
                _map.AdvanceWater();
            double totalWater = 0;
            for (int y = 0; y < _map.Height; y++)
                for (int x = 0; x < _map.Width; x++)
                    totalWater += _map.GetTile(x, y).WaterLevel;

            Text = Convert.ToString(totalWater);

            RefreshMap();
        }

        void StartProgressBar()
        {
            progressBar1.Value = 0;
            progressBar1.Visible = true;
            progressTimer.Start();
        }

        void StopProgressBar()
        {
            progressBar1.Visible = false;
            progressTimer.Stop();
        }

        void DoCreateMap()
        {
            if (level > 1)
                lblDescending.Visible = true;
            StartProgressBar();
            Thread thread = new Thread(new ThreadStart(() =>
            {
                DateTime timeStarted = DateTime.Now;
                Map newMap = CreateMap(level);
                DateTime timeEnd = DateTime.Now;
                int generationTimeMs = timeEnd.Subtract(timeStarted).Milliseconds;
                int minWaitMs = 2000;
                int waitTime = Math.Max(0, minWaitMs - generationTimeMs);
                Thread.Sleep(waitTime);
                progressBar1.BeginInvoke(new Action(() =>
                    {
                        lblLevel.Text = "Level " + level;
                        lblLevel.Visible = true;
                        lblTurnNumber.Visible = true;
                        lblDescending.Visible = false;
                        StopProgressBar();
                        _map = newMap;
                        RefreshMap();
                    }));
            }));
            thread.Start();
        }

        void ShowMessage(string message)
        {
            lblMsg.Text = message;
        }

        void HideMessage()
        {
            lblMsg.Text = "";
        }

        bool _doOpen = false;
        bool _doClose = false;

        bool IsBusy
        {
            get
            {
                return progressBar1.Visible;
            }
        }

        int turnNumber = 1;

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (lblInstruct.Visible == true && e.KeyCode != Keys.Space)
                return;
            if (GameOver)
            {
                if (e.KeyCode == Keys.Escape)
                    Application.Exit();
                return;
            }
            if (IsBusy)
                return;
            bool hasMoved = false;
            CompassPoint? moveDirection = null;
            switch (e.KeyCode)
            {
                case Keys.F9:
                    CheatMode = !CheatMode;
                    break;
                case Keys.R:
                case Keys.Space:
                    if (level == 0)
                    {
                        level = 1;
                        lblMsg.Visible = false;
                        lblInstruct.Visible = false;
                        DoCreateMap();
                        return;
                    }
                    break;
                case Keys.T:
                    
                    //Debug();
                
                    //designer.Clean(TileType.Chasm);
                    break;
                case Keys.Up:
                    moveDirection = CompassPoint.North;
                    break;
                case Keys.Right:
                    moveDirection = CompassPoint.East;
                    break;
                case Keys.Down:
                    moveDirection = CompassPoint.South;
                    break;
                case Keys.Left:
                    moveDirection = CompassPoint.West;
                    break;
                case Keys.OemPeriod:
                    hasMoved = true;
                    break;
                case Keys.E:
                    //_map.EmitSound(_map.GetTile(_player.X, _player.Y), 100);
                    break;
                case Keys.O:
                    _doOpen = true;
                    return;
                case Keys.C:
                    _doClose = true;
                    return;
            }

            if (moveDirection.HasValue)
            {
                if (_doOpen || _doClose)
                {
                    Tile adjTile = _player.Tile.GetNeighbour(moveDirection.Value);
                    Door door = adjTile.Content as Door;
                    if (door != null)
                    {
                        if (_doOpen && !door.IsOpen)
                        {
                            door.Open();
                            hasMoved = true;
                        }
                        else if (_doClose && door.IsOpen)
                        {
                            door.Close();
                            hasMoved = true;
                        }
                    }
                }
                else
                {
                    hasMoved = _player.Move(moveDirection.Value);
                    //if the player tried to move but was blocked see if it's because they ran into a monster
                    //if so count it as a move
                    if (!hasMoved)
                    {
                        Tile neighbourTile = _player.Tile.GetNeighbour(moveDirection.Value);
                        if (neighbourTile != null && neighbourTile.Creature != null)
                            hasMoved = true;
                    }
                }
            }

            _doClose = false;
            _doOpen = false;

            if (hasMoved && !GameOver)
            {
                if (_player.Tile.IsExit)
                {
                    RefreshMap();
                    if (level == 8)
                    {
                        GameOver = true;
                        GameOverReason = "You have found the treasure. You win!";
                    }
                    else
                    {
                        Descend();
                        return;
                    }
                }
                else
                {
                    foreach (Creature entity in _map.Entities)
                    {
                        if (!entity.IsAlive)
                            continue;
                        entity.DoTurn();
                        if (GameOver)
                            break;
                    }
                }
                turnNumber++;
                lblTurnNumber.Text = "Turn " + turnNumber;
            }

            if (GameOver)
            {
                lblGameOver.Text = GameOverReason + Environment.NewLine + "You reached level " + level + " and survived " + 
                    + turnNumber + " turns. Press escape to quit";
                lblGameOver.Left = Width / 2 - lblGameOver.Width / 2;
                lblGameOver.Top = 100;
                if (_player.Y < 20)
                    lblGameOver.Top = Height / 2;
                lblGameOver.Visible = true;
            }

            RefreshMap();
            Refresh();
        }

        private void Descend()
        {
            level++;
            DoCreateMap();
        }

        public static string GameOverReason { get; set; }

        public static string GameOverReasonZombie = "You have been eaten by a zombie (yeah those green things are zombies..)";
        public static string GameOverReasonSnake = "You have been eaten by a snake";
        public static bool CheatMode=false;
    }

    //public class FloorTile2 : Tile
    //{
    //    public override Color Color
    //    {
    //        get { return Color.LightGray; }
    //    }
    //}

    //public class FloorTile : Tile
    //{
    //    public override Color Color
    //    {
    //        get { return Color.LightGray; }
    //    }

    //    public override bool IsFloor
    //    {
    //        get
    //        {
    //            return true;
    //        }
    //    }
    //}

    //public class ChasmTile : Tile
    //{
    //    public override Color Color
    //    {
    //        get { return Color.Black; }
    //    }

    //    public override bool IsWall
    //    {
    //        get
    //        {
    //            return false;
    //        }
    //    }
    //}

    //public class HidingHoleTile : Tile
    //{
    //    public override Color Color
    //    {
    //        get { return Color.Black; }
    //    }

    //    public override bool IsFloor
    //    {
    //        get
    //        {
    //            return true;
    //        }
    //    }
    //}

    //public class ColorTile : Tile
    //{
    //    protected Color _color;

    //    public override Color Color
    //    {
    //        get
    //        {
    //            return _color;
    //        }
    //    }

    //    public void SetColor(Color color)
    //    {
    //        _color = color;
    //    }
    //}

    //public class WallTile : Tile
    //{
    //    public override Color Color
    //    {
    //        get { return Color.DarkSlateGray; }
    //    }

    //    public override bool IsWall
    //    {
    //        get
    //        {
    //            return true;
    //        }
    //    }
    //}

    //public class RockTile : Tile
    //{
    //    public override Color Color
    //    {
    //        get { return Color.DarkGray; }
    //    }
    //}

    /// <summary>
    /// Doors have been disabled in the game, but they emit sound when opened or closed which can attract zombies
    /// </summary>
    public class Door : TileContent
    {
        bool _isOpen = false;

        public override void Render(Graphics g, int x, int y)
        {
            if (_isOpen)
                g.DrawRectangle(Pens.Orange, x * 5 + 1, y * 5 + 1, 3, 3);
            else
                g.FillRectangle(new SolidBrush(Color.Orange), x * 5 + 1, y * 5 + 1, 4, 4);
        }

        public void Open()
        {
            _isOpen = true;
            _tile.EmitSound(50);
        }

        public void Close()
        {
            _isOpen = false;
            _tile.EmitSound(50);
        }

        public bool IsOpen
        {
            get
            {
                return _isOpen;
            }
        }

        public override bool IsPassable
        {
            get
            {
                return _isOpen;
            }
        }

        public override bool IsOpaque
        {
            get
            {
                return !_isOpen;
            }
        }
    }

    public abstract class TileContent
    {
        protected Tile _tile;

        public Tile Tile
        {
            set
            {
                _tile = value;
            }
        }

        public abstract void Render(Graphics g, int x, int y);

        public virtual bool IsPassable
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsOpaque
        {
            get
            {
                return false;
            }
        }
    }

    
    
    public enum CompassPoint
    {
        North=0,
        East=1,
        South=2,
        West=3
    }

    public class Player : Creature
    {
        private Player()
        {
            _sightRadius = 99;
        }

        static Player _player = new Player();
        public static Player Instance
        {
            get
            {
                return _player;
            }
        }
    }

    
}
