using VAICOM.KneeboardReceiver;

public interface IKneeboardDisplay
{
    void ShowMainMenu();
    void ShowServerData(KneeboardServerData data);
    void ShowCurrentPage();
    void LogMessage(string message);
    void Clear();
}

public class ConsoleDisplay : IKneeboardDisplay
{
    public void ShowMainMenu()
    {
        try
        {
            Console.Clear();
            // ... logica di visualizzazione console ...
        }
        catch (IOException)
        {
            // Ignora errori di console
        }
    }

    public void ShowServerData(KneeboardServerData data)
    {
        Clear();
        Console.WriteLine("=== VAICOM KNEELBOARD SERVER DATA ===");
        Console.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine("=====================================");
        Console.WriteLine();
        Console.WriteLine("SERVER INFORMATION:");
        Console.WriteLine($"Theater: {data.theater ?? "N/A"}");
        Console.WriteLine($"DCS Version: {data.dcsversion ?? "N/A"}");
        Console.WriteLine($"Aircraft: {data.aircraft ?? "N/A"}");
        // ... aggiungi altri campi ...
        Console.WriteLine();
        Console.WriteLine("=====================================");
    }

    public void ShowCurrentPage()
    {
        try
        {
            Console.Clear();
            // ... logica di visualizzazione console ...
        }
        catch (IOException)
        {
            // Ignora errori di console
        }
    }

    public void LogMessage(string message)
    {
        try
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
        catch (IOException)
        {
            // Ignora errori di console
        }
    }

    public void Clear()
    {
        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
            // Ignora errori di console
        }
    }
}

public class FormsDisplay : IKneeboardDisplay
{
    private readonly MainForm _mainForm;
    public FormsDisplay(MainForm mainForm)
    {
        _mainForm = mainForm;
    }

    public void ShowMainMenu()
    {
        // Gestito automaticamente dal Forms
        _mainForm.Invoke((MethodInvoker)(() => _mainForm.LoadGroups()));
    }

    public void ShowCurrentPage()
    {
        // Gestito automaticamente dal Forms
        _mainForm.Invoke((MethodInvoker)(() => _mainForm.DisplayCurrentPage()));
    }

    public void LogMessage(string message)
    {
        _mainForm.Invoke((MethodInvoker)(() => _mainForm.AddLogMessage(message)));
    }

    public void Clear()
    {
        if (_mainForm.IsHandleCreated && !_mainForm.IsDisposed)
        {
            _mainForm.BeginInvoke(new Action(() =>
            {
                if (!_mainForm.IsDisposed)
                    _mainForm.ClearDisplay();
            }));
        }
    }

    public void ShowServerData(KneeboardServerData data)
    {
        if (_mainForm.IsHandleCreated && !_mainForm.IsDisposed)
        {
            _mainForm.BeginInvoke(new Action(() =>
            {
                if (!_mainForm.IsDisposed)
                    _mainForm.ShowServerData(data);
            }));
        }
    }
}