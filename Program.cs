using SplashKitSDK;

namespace IntersectionRush
{
    public class Program
    {
        public static void Main()
        {
            // Wide enough for three schemas, each with roads long enough that
            // a vehicle can be seen driving all the way through and off the
            // far side, without any two schemas' roads touching.
            Window gameWindow = new Window("Intersection Rush", 1050, 460);
            IntersectionRushGame game = new IntersectionRushGame(gameWindow);

            // The loop only ever stops when the player closes the window - the
            // game itself never ends the loop. When the game is "over"
            // internally (gridlock), it just stops updating gameplay and
            // shows the final score instead.
            while (!gameWindow.CloseRequested)
            {
                SplashKit.ProcessEvents();

                game.HandleInput();
                game.Update();
                game.Draw();
            }

            gameWindow.Close();
        }
    }
}
