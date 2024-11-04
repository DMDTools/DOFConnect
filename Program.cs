// This file contains plugins for LaunchBox/BigBox to integrate with DOFLinx
// Reference documentation : https://pluginapi.launchbox-app.com/html/4cf923f7-940c-5735-83de-04107a6ae0e6.htm
namespace DOFConnect
{
    using Unbroken.LaunchBox.Plugins;
    using Unbroken.LaunchBox.Plugins.Data;
    using System.IO.Pipes;

    // Class to handle system events in LaunchBox/BigBox
    public partial class SystemEvents : ISystemEventsPlugin
    {
        string gCurrentGame = "";
        string gCurrentPlatform = "platforms";

        public void OnEventRaised(string eventType)
        {
            // Event types : https://pluginapi.launchbox-app.com/html/3e3603e5-bab6-e510-689c-ee35c0f5f694.htm
            switch (eventType)
            {
                case SystemEventTypes.PluginInitialized:
                    // Do nothing
                    break;
                case SystemEventTypes.LaunchBoxStartupCompleted:
                    // TODO: Play cabinet's marquee, per the configuration
                    break;
                case SystemEventTypes.LaunchBoxShutdownBeginning:
                case SystemEventTypes.BigBoxShutdownBeginning:
                    // TODO: Go black
                    break;
                case SystemEventTypes.GameStarting:
                    // Play "launching" GIF
                    break;
                case SystemEventTypes.GameExited:
                    // Restore Game Marquee
                    sendMenuRom(gCurrentPlatform, gCurrentGame);
                    break;
                case SystemEventTypes.SelectionChanged:
                    HandleSelectionChanged();
                    break;
                default:
                    break;
            }
        }

        // Helper method to handle selection changes
        private void HandleSelectionChanged()
        {
            // Depending if this is a new platform, or a new game, show platform marquee or game marquee
            // Generate a DOFLinx message MENU_ROM="MAME,qbert"
            IPlatform selectedPlatform = PluginHelper.StateManager.GetSelectedPlatform();
            IGame[] selectedGames = PluginHelper.StateManager.GetAllSelectedGames();
            if (selectedPlatform != null)
            {
                // If platform is not null, then we just selected a platform
                // If we changed platform
                if (gCurrentPlatform != selectedPlatform.Name)
                {
                    gCurrentPlatform = selectedPlatform.Name;
                    if (selectedGames != null)
                    {
                        sendMenuRom(gCurrentPlatform, gCurrentGame);
                    }
                    else
                    {
                        // No game selected? Then display platform's marquee
                        sendMenuRom("platforms", gCurrentPlatform);
                    }
                }
            }
            if (selectedGames != null && selectedGames.Length > 0)
            {
                IGame game = selectedGames[0];
                if (gCurrentGame != game.ApplicationPath)
                {
                    gCurrentGame = CleanGameName(game.ApplicationPath);
                    sendMenuRom(gCurrentPlatform, gCurrentGame);
                }
            }
        }
        // Helper method to clean up the game name
        private string CleanGameName(string gamePath)
        {
            string cleanName = System.IO.Path.GetFileNameWithoutExtension(gamePath);
            cleanName = System.Text.RegularExpressions.Regex.Replace(cleanName, @"\s*\([^)]*\)", "");
            cleanName = cleanName.Trim();
            cleanName = cleanName.Replace("'", "_");
            return cleanName;
        }

        // Method to send menu ROM information to DOFLinx
        public static void sendMenuRom(string platform, string game)
        {
            string romName = System.IO.Path.GetFileNameWithoutExtension(game);

            // Connect to DOFLinx named pipe
            using (var dofClient = new NamedPipeClientStream(".", "DOFLinx", PipeDirection.Out, PipeOptions.Asynchronous))
            {
                dofClient.Connect(1000);
                using (var dofSw = new StreamWriter(dofClient))
                {
                    dofSw.Write($"MENU_ROM={platform},{romName}");
                }
            }
        }
    }
}