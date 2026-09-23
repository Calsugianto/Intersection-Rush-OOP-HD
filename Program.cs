using SplashKitSDK;

namespace IntersectionRush
{
    public class Program
    {
        public static void Main()
        {
            Window gameWindow = new Window("Intersection Rush", 1100, 650);
            IntersectionRushGame game = new IntersectionRushGame(gameWindow);

            // The loop only ever stops when the player closes the window - the
            // game itself never ends the loop. When the game is "over"
            // internally (gridlock), it just stops updating gameplay and
            // shows the final score instead, exactly like Healthy Bites did.
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