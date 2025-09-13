using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace VAICOM.KneeboardReceiver
{

    public class AdvancedKneeboardManager
    {
        private IKneeboardDisplay _display;

        private string _kneeboardPath = "";

        public static bool IsWindowsFormsMode { get; set; } = false;

        // EVENTI
        public event Action GroupsUpdated;
        public event Action PageChanged;
        public event Action NightModeToggled;
        public event Action<string> LogMessageReceived;

        // CAMPI PRIVATI
        private string _currentAircraft;
        private string _currentTheater;
        private bool _nightMode = false;
        private string _currentEra = "Modern"; // Modern, WWII, ColdWar
        private int _currentPage = 0;
        private List<KneeboardPage> _currentPages = new List<KneeboardPage>();
        private List<PageGroup> _availableGroups = new List<PageGroup>();
        private Dictionary<string, string> _eraFilters = new Dictionary<string, string>
        {
            {"F-16C_50", "Modern"},
            {"FA-18C", "Modern"},
            {"A-10C_II", "Modern"},
            {"P-51D", "WWII"},
            {"BF-109K-4", "WWII"},
            {"F-86F", "ColdWar"},
            {"MiG-15bis", "ColdWar"}
        };

        // PROPRIETA' PUBBLICHE
        public string CurrentAircraft => _currentAircraft;
        public string CurrentTheater => _currentTheater;
        public string CurrentEra => _currentEra;
        public bool NightMode => _nightMode;
        public string CurrentGroup { get; private set; }
        public int CurrentPageIndex => _currentPage;
        public int TotalPages => _currentPages.Count;

        // METODI PER SOLLEVARE GLI EVENTI
        protected virtual void OnGroupsUpdated() => GroupsUpdated?.Invoke();
        protected virtual void OnPageChanged() => PageChanged?.Invoke();
        protected virtual void OnNightModeToggled() => NightModeToggled?.Invoke();

        public void Initialize(string kneeboardPath)
        {
            if (string.IsNullOrEmpty(kneeboardPath))
            {
                LogMessage("Kneeboard path cannot be null or empty");
                return;
            }
            else {
                _kneeboardPath = kneeboardPath;

                LogMessage($"Kneeboard manager initialized with path: {_kneeboardPath}");

                // Verifica che il percorso esista
                if (!Directory.Exists(_kneeboardPath))
                {
                    LogMessage($"Warning: Kneeboard path does not exist: {_kneeboardPath}");
                    Directory.CreateDirectory(_kneeboardPath);
                    LogMessage($"Created kneeboard directory: {_kneeboardPath}");
                }

                EnsureDisplayInitialized();
            }
        }

        public bool IsInitialized => !string.IsNullOrEmpty(_kneeboardPath);

        public void SetDisplay(IKneeboardDisplay display)
        {
            _display = display;
            LogMessage("Display initialized: " + display.GetType().Name);
        }
        private void EnsureDisplayInitialized()
        {
            if (_display == null)
            {
                // Fallback: se non c'è display, usa logging
                LogMessage("Warning: Display not initialized. Using fallback logging.");
            }
        }

        private bool CanUseConsole()
        {
            // Possibile controllare se siamo in modalità console
            return Program.IsTestMode && !IsWindowsFormsMode;
        }

        private bool IsRunningInWindowsForms()
        {
            // Verifica se l'applicazione sta usando Windows Forms
            try
            {
                return System.Windows.Forms.Application.MessageLoop;
            }
            catch
            {
                return false;
            }
        }

        public void SafeConsoleWriteLine(string message)
        {
            if (CanUseConsole())
            {
                try
                {
                    SafeConsoleWriteLine(message);
                }
                catch (IOException)
                {
                    // Ignora errori di console quando non disponibile
                }
            }
        }
        public void SafeConsoleClear()
        {
            if (CanUseConsole())
            {
                try
                {
                    SafeConsoleClear();
                }
                catch (IOException)
                {
                    // Ignora errori di clear quando la console non è disponibile
                }
            }
        }

        public void LogMessage(string message = "")
        {
            if (message != "")
            {
                LogMessageReceived?.Invoke(message);

                // Per debug, anche su console se in test mode
                if (Program.IsTestMode)
                {
                    SafeConsoleWriteLine($"[LOG] {message}");
                }
            }
        }

        public void DisplayClear()
        {
            _display?.Clear();
        }

        public void UpdateFromServerData(KneeboardServerData data)
        {
            try
            {
                _currentAircraft = data.aircraft ?? "Unknown";
                _currentTheater = data.theater ?? "Unknown";

                // Determina l'epoca in base all'aereo
                _currentEra = _eraFilters.ContainsKey(_currentAircraft)
                    ? _eraFilters[_currentAircraft]
                    : "Modern";

                LogMessage($"Server data updated: {_currentAircraft} in {_currentTheater} ({_currentEra})");

                ScanAvailableGroups();

                // Notifica aggiornamento gruppi
                GroupsUpdated?.Invoke();
            }
            catch (Exception ex)
            {
                LogMessage($"Error updating server data: {ex.Message}");
            }
        }

        private void ScanAvailableGroups()
        {
            _availableGroups.Clear();
            LogMessage("Scanning for available groups...");

            // Cerca nella cartella specifica dell'aereo
            string aircraftPath = Path.Combine(_kneeboardPath, _currentAircraft + "_hornet");
            if (Directory.Exists(aircraftPath))
            {
                ScanGroupsInDirectory(aircraftPath);
            }

            // Cerca nella cartella generale (per contenuti cross-aircraft)
            ScanGroupsInDirectory(_kneeboardPath);

            // Ordina i gruppi per nome
            _availableGroups = _availableGroups
                .OrderBy(g => g.DisplayName)
                .ToList();
        }

        private void ScanGroupsInDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                LogMessage($"Directory does not exist: {directoryPath}");
                return;
            }

            LogMessage($"Scanning directory: {directoryPath}");

            foreach (var file in Directory.GetFiles(directoryPath, "*.png"))
            {
                var pageInfo = ParseFileName(Path.GetFileNameWithoutExtension(file));

                if (pageInfo != null && IsEraAppropriate(pageInfo.GroupName))
                {
                    var existingGroup = _availableGroups.FirstOrDefault(g => g.Name == pageInfo.GroupName);
                    if (existingGroup == null)
                    {
                        _availableGroups.Add(new PageGroup
                        {
                            Name = pageInfo.GroupName,
                            DisplayName = pageInfo.GroupName.Replace("_", " "),
                            HasDayVersion = pageInfo.IsDayVersion,
                            HasNightVersion = pageInfo.IsNightVersion,
                            FilePath = directoryPath
                        });
                    }
                    else
                    {
                        if (pageInfo.IsDayVersion) existingGroup.HasDayVersion = true;
                        if (pageInfo.IsNightVersion) existingGroup.HasNightVersion = true;
                    }
                    LogMessage($"Found group: {pageInfo.GroupName}");
                }
            }
        }

        private PageInfo ParseFileName(string fileName)
        {
            // Pattern: 001-Ordnance-001
            // Pattern: 002-Ordnance_Night-001
            // Pattern: 003-CheckList_Quick-001
            var match = Regex.Match(fileName, @"^(\d+)-([^-]+)-(\d+)$");
            if (!match.Success) return null;

            string groupPart = match.Groups[2].Value;
            bool isNight = groupPart.Contains("_Night");
            string cleanGroup = isNight ? groupPart.Replace("_Night", "") : groupPart;

            // Separa gruppo e sottogruppo
            string[] groupParts = cleanGroup.Split('_');
            string groupName = groupParts[0];
            string subGroup = groupParts.Length > 1 ? groupParts[1] : null;

            return new PageInfo
            {
                GroupName = groupName,
                SubGroup = subGroup,
                IsNightVersion = isNight,
                IsDayVersion = !isNight,
                FullName = groupPart
            };
        }

        private bool IsEraAppropriate(string groupName)
        {
            // Filtra i contenuti in base all'epoca
            if (_currentEra == "WWII")
            {
                // Per WWII, escludi contenuti moderni
                var modernGroups = new[] { "BrevityCodes", "Simbology", "Ordnance" };
                return !modernGroups.Any(g => groupName.Contains(g));
            }
            else if (_currentEra == "ColdWar")
            {
                // Per Cold War, limitati a contenuti basici
                var coldWarGroups = new[] { "CheckList", "Procedures", "Charts" };
                return coldWarGroups.Any(g => groupName.Contains(g));
            }

            // Modern: tutto consentito
            return true;
        }
       
        public List<PageGroup> GetAvailableGroups() => _availableGroups;
        public void LoadGroup(string groupName)
        {
            CurrentGroup = groupName;
            LoadGroup(groupName, null);
        }
        public void LoadGroup(string groupName, string subGroup = null)
        {
            try
            {
                LogMessage($"Loading group: {groupName}");

                CurrentGroup = groupName;

                _currentPages.Clear();


                // Cerca in tutte le directory possibili
                var searchDirectories = new[]
                {
                    Path.Combine(_kneeboardPath, _currentAircraft + "_hornet"),
                    _kneeboardPath
                };

                foreach (var directory in searchDirectories)
                {
                    if (!Directory.Exists(directory)) continue;

                    foreach (var file in Directory.GetFiles(directory, "*.png"))
                    {
                        var pageInfo = ParseFileName(Path.GetFileNameWithoutExtension(file));
                        if (pageInfo != null &&
                            pageInfo.GroupName == groupName &&
                            (subGroup == null || pageInfo.SubGroup == subGroup) &&
                            (_nightMode ? pageInfo.IsNightVersion : pageInfo.IsDayVersion))
                        {
                            _currentPages.Add(new KneeboardPage
                            {
                                FilePath = file,
                                Title = $"{groupName}{(subGroup != null ? " " + subGroup : "")}",
                                PageNumber = int.Parse(Regex.Match(file, @"-(\d+)\.png$").Groups[1].Value)
                            });
                        }
                    }
                }

                _currentPages = _currentPages.OrderBy(p => p.PageNumber).ToList();
                _currentPage = 0;

                if (_currentPages.Count > 0)
                {
                    DisplayCurrentPage();
                }
                else
                {
                    SafeConsoleWriteLine("No pages found for selected group!");
                }

                LogMessage($"Loaded {_currentPages.Count} pages for group: {groupName}");

                // Solleva evento dopo aver caricato le pagine
                OnPageChanged();
            }
            catch (Exception ex)
            {
                LogMessage($"Error loading group {groupName}: {ex.Message}");
            }
        }

        //public void ToggleNightMode()
        //{
        //    _nightMode = !_nightMode;
        //    LogMessage($"Night mode: {(_nightMode ? "ON" : "OFF")}");

        //    // Ricarica il gruppo corrente con la modalità aggiornata
        //    if (_currentPages.Count > 0)
        //    {
        //        var currentGroup = _availableGroups.FirstOrDefault(g =>
        //            g.Name == _currentPages[0].Title.Split(' ')[0]);
        //        if (currentGroup != null)
        //        {
        //            LoadGroup(currentGroup.Name);
        //        }
        //    }
        //}
        public void ToggleNightMode()
        {
            _nightMode = !_nightMode;
            LogMessage($"Night mode: {(_nightMode ? "ON" : "OFF")}");
            NightModeToggled?.Invoke();

            if (!string.IsNullOrEmpty(CurrentGroup))
            {
                LoadGroup(CurrentGroup);
            }
        }

        public void DisplayMainMenu()
        {
            _display?.ShowMainMenu();
        }

        public void DisplayCurrentPage()
        {
            if (_currentPages.Count == 0) return;

            var page = _currentPages[_currentPage];
            string pageInfo = $"Page {_currentPage + 1} of {_currentPages.Count}";

            SafeConsoleClear();
            SafeConsoleWriteLine($"=== {_currentAircraft} - {_currentTheater} ===");
            SafeConsoleWriteLine($"=== {page.Title} ===");
            SafeConsoleWriteLine(pageInfo);
            SafeConsoleWriteLine("==========================================");
            SafeConsoleWriteLine($"File: {Path.GetFileName(page.FilePath)}");
            SafeConsoleWriteLine("==========================================");
            SafeConsoleWriteLine("Commands: N=Next, P=Previous, M=Menu, T=Toggle Night, Q=Quit");
        }

        public void ProcessCommand(string command)
        {
            switch (command.ToUpper())
            {
                case "N":
                    NextPage();
                    break;
                case "P":
                    PreviousPage();
                    break;
                case "M":
                    DisplayMainMenu();
                    break;
                case "T":
                    ToggleNightMode();
                    break;
                case "R":
                    ScanAvailableGroups();
                    DisplayMainMenu();
                    break;
                case "Q":
                    Environment.Exit(0);
                    break;
                default:
                    // Controlla se è la selezione di un gruppo
                    if (int.TryParse(command, out int groupIndex) && groupIndex > 0 && groupIndex <= _availableGroups.Count)
                    {
                        var selectedGroup = _availableGroups[groupIndex - 1];
                        LoadGroup(selectedGroup.Name);
                    }
                    else
                    {
                        SafeConsoleWriteLine("Unknown command. Use: 1-9, N, P, M, T, R, Q");
                    }
                    break;
            }
        }

        public KneeboardPage GetCurrentPage()
        {
            return _currentPages.Count > 0 ? _currentPages[_currentPage] : null;
        }

        public void NextPage() => NavigatePages(1);
        public void PreviousPage() => NavigatePages(-1);

        private void NavigatePages(int direction)
        {
            if (_currentPages.Count == 0) return;

            _currentPage = (_currentPage + direction + _currentPages.Count) % _currentPages.Count;
            DisplayCurrentPage();
        }


        public void TestScanGroups()
        {
            SafeConsoleWriteLine("=== SCANNING GROUPS ===");
            SafeConsoleWriteLine($"Kneeboard path: {_kneeboardPath}");
            SafeConsoleWriteLine($"Directory exists: {Directory.Exists(_kneeboardPath)}");

            if (Directory.Exists(_kneeboardPath))
            {
                string[] allFiles = Directory.GetFiles(_kneeboardPath, "*", SearchOption.AllDirectories);
                SafeConsoleWriteLine($"Found {allFiles.Length} files total:");

                foreach (string file in allFiles)
                {
                    SafeConsoleWriteLine($"  {Path.GetFileName(file)}");

                    // Test del parsing
                    var pageInfo = ParseFileName(Path.GetFileNameWithoutExtension(file));
                    if (pageInfo != null)
                    {
                        SafeConsoleWriteLine($"    → Group: {pageInfo.GroupName}, Night: {pageInfo.IsNightVersion}");
                    }
                    else
                    {
                        SafeConsoleWriteLine($"    → Failed to parse filename");
                    }
                }
            }

            ScanAvailableGroups();
            SafeConsoleWriteLine($"=== FOUND {_availableGroups.Count} GROUPS ===");

            foreach (var group in _availableGroups)
            {
                SafeConsoleWriteLine($"- {group.Name} (Day: {group.HasDayVersion}, Night: {group.HasNightVersion})");
            }
        }

        public void TestLoadGroup(string groupName)
        {
            SafeConsoleWriteLine($"Loading group: {groupName}");
            LoadGroup(groupName);

            SafeConsoleWriteLine($"Loaded {_currentPages.Count} pages");
            if (_currentPages.Count > 0)
            {
                DisplayCurrentPage();
            }
        }
    }


    public class PageInfo
    {
        public string GroupName { get; set; }
        public string SubGroup { get; set; }
        public bool IsNightVersion { get; set; }
        public bool IsDayVersion { get; set; }
        public string FullName { get; set; }
    }

    public class PageGroup
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool HasDayVersion { get; set; }
        public bool HasNightVersion { get; set; }
        public string FilePath { get; set; }
    }

    public class KneeboardPage
    {
        public string FilePath { get; set; }
        public string Title { get; set; }
        public int PageNumber { get; set; }
    }
}