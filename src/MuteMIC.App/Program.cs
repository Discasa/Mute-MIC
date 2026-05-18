namespace MuteMIC.App;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (RunCommandLine(args))
        {
            return;
        }

        using Mutex mutex = new(true, @"Local\MuteMIC.App", out bool createdNew);
        if (!createdNew)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }

    private static bool RunCommandLine(string[] args)
    {
        if (args.Length == 0)
        {
            return false;
        }

        AudioInputMuteService audioService = new();
        switch (args[0].Trim().ToLowerInvariant())
        {
            case "--mute-all":
                audioService.SetAllMuted(true);
                return true;
            case "--unmute-all":
                audioService.SetAllMuted(false);
                return true;
            case "--toggle-all":
                audioService.ToggleAll();
                return true;
            default:
                return false;
        }
    }
}
