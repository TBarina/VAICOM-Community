using VAICOM.KneeboardReceiver;

public class KneeboardDisplay
{
    private AdvancedKneeboardManager _manager;
    private int _currentSelection = 0;
    private bool _inGroupView = true;

    public KneeboardDisplay(AdvancedKneeboardManager manager)
    {
        _manager = manager;
    }

    public void Run()
    {
        Console.CursorVisible = false;

        while (true)
        {
            if (_inGroupView)
            {
                DisplayGroupSelection();
            }
            else
            {
                DisplayPageView();
            }

            var key = Console.ReadKey(intercept: true);
            HandleInput(key);
        }
    }

    private void DisplayGroupSelection()
    {
        Console.Clear();
        Console.WriteLine($"=== {_manager.CurrentAircraft} - {_manager.CurrentTheater} ===");
        Console.WriteLine($"Era: {_manager.CurrentEra} | Night Mode: {(_manager.NightMode ? "ON" : "OFF")}");
        Console.WriteLine("========================");
        Console.WriteLine("SELECT GROUP:");
        Console.WriteLine("========================");

        var groups = _manager.GetAvailableGroups();

        for (int i = 0; i < groups.Count; i++)
        {
            if (i == _currentSelection)
            {
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = ConsoleColor.Black;
            }

            string nightIndicator = groups[i].HasNightVersion ? " 🌙" : "";
            Console.WriteLine($"{i + 1}. {groups[i].DisplayName}{nightIndicator}");

            if (i == _currentSelection)
            {
                Console.ResetColor();
            }
        }

        Console.WriteLine("========================");
        Console.WriteLine("↑↓: Navigate | ENTER: Select | T: Night Mode | Q: Quit");
    }

    private void DisplayPageView()
    {
        Console.Clear();
        var currentPage = _manager.GetCurrentPage();

        if (currentPage != null)
        {
            Console.WriteLine($"=== {_manager.CurrentGroup} ===");
            Console.WriteLine($"Page {_manager.CurrentPageIndex + 1} of {_manager.TotalPages}");
            Console.WriteLine("========================");

            // Simula la visualizzazione del contenuto (nelle versione reale sarebbe un'immagine)
            Console.WriteLine($"CONTENT: {Path.GetFileName(currentPage.FilePath)}");
            Console.WriteLine("========================");

            // Mostra anteprima del contenuto per test
            try
            {
                string content = File.ReadAllText(currentPage.FilePath);
                Console.WriteLine(content);
            }
            catch
            {
                Console.WriteLine("[Binary content - PNG image]");
            }
        }
        else
        {
            Console.WriteLine("No pages available");
        }

        Console.WriteLine("========================");
        Console.WriteLine("←→: Navigate | M: Menu | T: Night Mode | Q: Quit");
    }

    private void HandleInput(ConsoleKeyInfo key)
    {
        if (_inGroupView)
        {
            HandleGroupViewInput(key);
        }
        else
        {
            HandlePageViewInput(key);
        }
    }

    private void HandleGroupViewInput(ConsoleKeyInfo key)
    {
        var groups = _manager.GetAvailableGroups();

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                _currentSelection = Math.Max(0, _currentSelection - 1);
                break;

            case ConsoleKey.DownArrow:
                _currentSelection = Math.Min(groups.Count - 1, _currentSelection + 1);
                break;

            case ConsoleKey.Enter:
                if (groups.Count > 0)
                {
                    _manager.LoadGroup(groups[_currentSelection].Name);
                    _inGroupView = false;
                }
                break;

            case ConsoleKey.T:
                _manager.ToggleNightMode();
                break;

            case ConsoleKey.Q:
                Environment.Exit(0);
                break;
        }
    }

    private void HandlePageViewInput(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                _manager.PreviousPage();
                break;

            case ConsoleKey.RightArrow:
                _manager.NextPage();
                break;

            case ConsoleKey.M:
                _inGroupView = true;
                break;

            case ConsoleKey.T:
                _manager.ToggleNightMode();
                break;

            case ConsoleKey.Q:
                Environment.Exit(0);
                break;
        }
    }
}
