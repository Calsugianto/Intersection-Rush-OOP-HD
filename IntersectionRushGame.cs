using System;
using SplashKitSDK;
using Timer = SplashKitSDK.Timer;

namespace IntersectionRush
{
    // The game manager - owns the three Intersections, the difficulty timer,
    // the score, and the state machine. This class handles input, update,
    // and draw, the same responsibility split used throughout this unit.
    public class IntersectionRushGame
    {
        private enum GameState { Intro, Playing, LevelUp, GameOver }

        private const int LEVEL_2_SCORE = 50;
        private const int LEVEL_3_SCORE = 100;
        private const int WIN_SCORE = 200;

        private const double INTRO_DURATION_MS = 3000;
        private const double LEVEL_UP_DURATION_MS = 2000;

        // Base chance (per approach, per frame) of a new vehicle spawning, plus
        // how much that chance grows with time survived.
        private const double BASE_SPAWN_CHANCE = 0.001;
        private const double SPAWN_CHANCE_RAMP = 0.0001;

        // The three schemas sit this far apart horizontally. It has to be
        // bigger than 2 * (Intersection.HALF_SIZE + Approach.STOP_LINE_DISTANCE
        // + the road's small end margin) or neighbouring schemas' roads would
        // overlap. 320 clears that with a clean 30px gap either side.
        private const double SCHEMA_SPACING = 320;
        private const double SCHEMA_CENTER_X = 200; // left-most schema's centre
        private const double SCHEMA_CENTER_Y = 260;

        private static readonly string[] VehicleImageNames =
        {
            "CarH", "CarV", "BusH", "BusV", "TruckH", "TruckV"
        };

        private readonly Window _window;
        private readonly Intersection _fixedIntersection;
        private readonly Intersection _adaptiveIntersection;
        private readonly Intersection _playerIntersection;
        private readonly Random _random = new Random();

        private int _score;
        private int _level;
        private double _elapsedSeconds;

        private GameState _state;
        private string _bannerText;
        private string _finalMessage;
        private readonly Timer _stateTimer;

        public IntersectionRushGame(Window window)
        {
            _window = window;
            LoadResources();

            _fixedIntersection = new Intersection("Fixed-Timer AI", new FixedTimeController());
            _adaptiveIntersection = new Intersection("Adaptive AI", new AdaptiveQueueController());
            _playerIntersection = new Intersection("You", new ManualController());

            _score = 0;
            _level = 1;
            _elapsedSeconds = 0;

            _stateTimer = new Timer("IntersectionRushStateTimer");
            EnterIntro();
        }

        // Loads every vehicle sprite once, up front
        private void LoadResources()
        {
            foreach (string name in VehicleImageNames)
            {
                SplashKit.LoadBitmap(name, name + ".png");
            }
        }

        public void HandleInput()
        {
            if (_state != GameState.Playing) return;

            var playerLight = (ManualController)_playerIntersection.Controller;

            if (SplashKit.KeyTyped(KeyCode.Num1Key))
            {
                playerLight.RequestAxis(Axis.NorthSouth);
            }
            else if (SplashKit.KeyTyped(KeyCode.Num2Key))
            {
                playerLight.RequestAxis(Axis.EastWest);
            }
        }

        public void Update()
        {
            switch (_state)
            {
                case GameState.Intro:
                    if (_stateTimer.Ticks >= INTRO_DURATION_MS) EnterPlaying();
                    break;

                case GameState.Playing:
                    UpdateGameplay();
                    break;

                case GameState.LevelUp:
                    if (_stateTimer.Ticks >= LEVEL_UP_DURATION_MS) EnterPlaying();
                    break;

                case GameState.GameOver:
                    break;
            }
        }

        private void UpdateGameplay()
        {
            const double deltaSeconds = 1.0 / 60.0;
            _elapsedSeconds += deltaSeconds;

            double spawnChance = BASE_SPAWN_CHANCE + _elapsedSeconds * SPAWN_CHANCE_RAMP;

            SpawnIfDue(_fixedIntersection, spawnChance);
            SpawnIfDue(_adaptiveIntersection, spawnChance);
            SpawnIfDue(_playerIntersection, spawnChance);

            _fixedIntersection.Update(deltaSeconds, _elapsedSeconds);
            _adaptiveIntersection.Update(deltaSeconds, _elapsedSeconds);
            _playerIntersection.Update(deltaSeconds, _elapsedSeconds);

            // Only the player's own intersection earns score - the other two
            // exist purely as a live comparison, not something to be beaten
            // for points.
            _score = (int)(_playerIntersection.VehiclesPassed * 15 - _playerIntersection.TotalWaitSeconds * 2);
            if (_score < 0) _score = 0;

            CheckLevelProgress();

            if (_score >= WIN_SCORE)
            {
                EndGame(won: true);
            }
            else if (_playerIntersection.IsGridlocked)
            {
                EndGame(won: false);
            }
        }

        private void SpawnIfDue(Intersection intersection, double spawnChance)
        {
            foreach (Direction d in Enum.GetValues(typeof(Direction)))
            {
                if (_random.NextDouble() < spawnChance)
                {
                    intersection.TrySpawn(d, RandomVehicle());
                }
            }
        }

        private Vehicle RandomVehicle()
        {
            double choice = _random.NextDouble();
            if (choice < 0.6) return new Car(_elapsedSeconds);
            if (choice < 0.85) return new Truck(_elapsedSeconds);
            return new Bus(_elapsedSeconds);
        }

        // Checks whether the score has crossed a threshold that moves the
        // player up a level. This only ever advances the level - the actual
        // win check (score >= WIN_SCORE) is handled separately in
        // UpdateGameplay, since winning ends the game rather than levelling
        // it up.
        private void CheckLevelProgress()
        {
            if (_level == 1 && _score >= LEVEL_2_SCORE)
            {
                _level = 2;
                EnterLevelUp("Level 2 - traffic is picking up");
            }
            else if (_level == 2 && _score >= LEVEL_3_SCORE)
            {
                _level = 3;
                EnterLevelUp("Level 3 - rush hour");
            }
        }

        private void EnterIntro()
        {
            _state = GameState.Intro;
            _stateTimer.Stop();
            _stateTimer.Start();
        }

        private void EnterLevelUp(string text)
        {
            _state = GameState.LevelUp;
            _bannerText = text;
            _stateTimer.Stop();
            _stateTimer.Start();
        }

        private void EnterPlaying()
        {
            _state = GameState.Playing;
        }

        private void EndGame(bool won)
        {
            _state = GameState.GameOver;
            _finalMessage = won ? "You Win!" : "Gridlock!";
        }

        public void Draw()
        {
            _window.Clear(Color.RGBColor(30, 120, 60));

            DrawGameplay();

            switch (_state)
            {
                case GameState.Intro:
                    DrawBanner("Press 1 for North/South, 2 for East/West. Keep your queue short!");
                    break;
                case GameState.LevelUp:
                    DrawBanner(_bannerText);
                    break;
                case GameState.GameOver:
                    DrawBanner($"{_finalMessage} Final score: {_score}");
                    break;
            }

            _window.Refresh(60);
        }

        private void DrawGameplay()
        {
            _fixedIntersection.Draw(SCHEMA_CENTER_X, SCHEMA_CENTER_Y);
            _adaptiveIntersection.Draw(SCHEMA_CENTER_X + SCHEMA_SPACING, SCHEMA_CENTER_Y);
            _playerIntersection.Draw(SCHEMA_CENTER_X + SCHEMA_SPACING * 2, SCHEMA_CENTER_Y);

            SplashKit.DrawText($"Score: {_score}", Color.White, 10, 10);
            SplashKit.DrawText($"Level: {_level}", Color.White, 10, 30);
        }

        private void DrawBanner(string text)
        {
            double barY = _window.Height / 2.0 - 30;
            SplashKit.FillRectangle(Color.White, 0, barY, _window.Width, 60);
            SplashKit.DrawText(text, Color.Black, _window.Width / 2.0 - text.Length * 4.2, barY + 22);
        }
    }
}