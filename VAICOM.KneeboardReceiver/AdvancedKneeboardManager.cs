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
        public string CurrentSubGroup { get; private set; }
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
                    Console.WriteLine(message);
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
                    Console.Clear();
                }
                catch (IOException)
                {
                    // Ignora errori di clear quando la console non è disponibile
                }
            }
        }

        public void LogMessage(string message = "")
        {
            if (string.IsNullOrEmpty(message)) return;

            // Log su console immediato (sempre disponibile)
            if (Program.IsTestMode)
            {
                Console.WriteLine($"[LOG] {message}");
            }

            // Invoca l'evento SOLO se safe
            SafeInvoke(() =>
            {
                LogMessageReceived?.Invoke(message);
            });
        }
        private void SafeInvoke(Action action)
        {
            try
            {
                // Se non siamo in Windows Forms o non ci sono subscribers, skip
                if (!System.Windows.Forms.Application.MessageLoop || LogMessageReceived == null)
                    return;

                // Controlla se uno qualsiasi dei subscribers è un controllo UI
                var hasUIControls = LogMessageReceived.GetInvocationList()
                    .Any(handler => handler.Target is System.Windows.Forms.Control);

                if (!hasUIControls)
                {
                    // Nessun controllo UI, invoca direttamente
                    action();
                    return;
                }

                // Cerca il primo controllo UI valido per l'invoke
                var uiControl = LogMessageReceived.GetInvocationList()
                    .Select(handler => handler.Target as System.Windows.Forms.Control)
                    .FirstOrDefault(control => control != null && control.IsHandleCreated && !control.IsDisposed);

                if (uiControl != null)
                {
                    uiControl.BeginInvoke(action);
                }
                else
                {
                    // Nessun controllo UI pronto, logga su console
                    Console.WriteLine($"[UI NOT READY] {action.Method.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SAFEINVOKE ERROR] {ex.Message}");
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
                //GroupsUpdated?.Invoke();
                SafeInvoke(() => GroupsUpdated?.Invoke());
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

            // DEBUG: mostra la struttura dei file
            DebugFileStructure();

            // Cerca nella cartella specifica dell'aereo
            string aircraftPath = Path.Combine(_kneeboardPath, _currentAircraft);
            if (Directory.Exists(aircraftPath))
            {
                ScanGroupsInDirectory(aircraftPath);
            }

            // Cerca nella cartella generale (per contenuti cross-aircraft)
            ScanGroupsInDirectory(_kneeboardPath);

            // RIMOZIONE DUPLICATI - controllo aggiuntivo
            _availableGroups = _availableGroups
                .GroupBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var first = g.First();
                    return new PageGroup
                    {
                        Name = first.Name,
                        DisplayName = first.DisplayName,
                        HasDayVersion = g.Any(x => x.HasDayVersion),
                        HasNightVersion = g.Any(x => x.HasNightVersion),
                        FilePath = first.FilePath
                    };
                })
                .OrderBy(g => g.DisplayName)
                .ToList();

//SafeInvoke(() => GroupsUpdated?.Invoke());

            LogMessage($"Scan completed. Found {_availableGroups.Count} unique groups.");
        }

        private void ScanGroupsInDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return;

            foreach (var file in Directory.GetFiles(directoryPath, "*.png"))
            {
                var pageInfo = ParseFileName(Path.GetFileNameWithoutExtension(file));
                if (pageInfo == null || !IsEraAppropriate(pageInfo.GroupName)) continue;

                var existingGroup = _availableGroups.FirstOrDefault(g =>
                    g.Name.Equals(pageInfo.GroupName, StringComparison.OrdinalIgnoreCase));

                if (existingGroup == null)
                {
                    existingGroup = new PageGroup
                    {
                        Name = pageInfo.GroupName,
                        DisplayName = pageInfo.GroupName.Replace("_", " "),
                        FilePath = directoryPath
                    };
                    _availableGroups.Add(existingGroup);
                }

                //// AGGIUNGI QUESTA PARTE PER I SOTTOGRUPPI:
                //if (!string.IsNullOrEmpty(pageInfo.SubGroup))
                //{
                //    // Aggiorna le versioni giorno/notte per il sottogruppo
                //    if (pageInfo.IsDayVersion)
                //        existingGroup.SubGroupDayVersions[pageInfo.SubGroup] = true;
                //    if (pageInfo.IsNightVersion)
                //        existingGroup.SubGroupNightVersions[pageInfo.SubGroup] = true;
                //}
                //else
                //{
                //    // Gestisci il gruppo principale (senza sottogruppo)
                //    if (pageInfo.IsDayVersion) existingGroup.HasDayVersion = true;
                //    if (pageInfo.IsNightVersion) existingGroup.HasNightVersion = true;
                //}

                if (pageInfo.IsDayVersion) existingGroup.HasDayVersion = true;
                if (pageInfo.IsNightVersion) existingGroup.HasNightVersion = true;

                // AGGIUNGI IL SOTTOGRUPPO SE ESISTE
                if (!string.IsNullOrEmpty(pageInfo.SubGroup) &&
                    !existingGroup.SubGroups.Contains(pageInfo.SubGroup))
                {
                    existingGroup.SubGroups.Add(pageInfo.SubGroup);
                }
            }
        }

        private PageInfo ParseFileName(string fileName)
        {
            try
            {
                LogMessage($"Parsing filename: {fileName}"); // DEBUG

                // Pattern corretto: deve accettare underscore nel nome del gruppo
                // Vecchio pattern: @"^(\d+)-([^-]+)-(\d+)$" ← NON accetta underscore
                // Nuovo pattern: @"^(\d+)-([A-Za-z_]+)-(\d+)$" ← Accetta lettere e underscore
                var match = Regex.Match(fileName, @"^(\d+)-([A-Za-z_]+)-(\d+)$");

                if (!match.Success)
                {
                    LogMessage($"Regex failed for: {fileName}");
                    return null;
                }

                string prefix = match.Groups[1].Value;
                string groupPart = match.Groups[2].Value; // Ora può contenere underscore
                string pageNumber = match.Groups[3].Value;

                LogMessage($"Groups: {prefix}, {groupPart}, {pageNumber}"); // DEBUG

                bool isNight = groupPart.Contains("_Night");
                string cleanGroup = isNight ? groupPart.Replace("_Night", "") : groupPart;

                // Separa gruppo e sottogruppo
                string[] groupParts = cleanGroup.Split('_');
                string groupName = groupParts[0];

                // Il sottogruppo è tutto ciò che viene dopo il primo underscore
                string subGroup = groupParts.Length > 1 ? string.Join("_", groupParts.Skip(1)) : null;

                LogMessage($"Parsed: Group={groupName}, SubGroup={subGroup ?? "(null)"}, Night={isNight}"); // DEBUG

                return new PageInfo
                {
                    GroupName = groupName,
                    SubGroup = subGroup,
                    IsNightVersion = isNight,
                    IsDayVersion = !isNight,
                    FullName = groupPart,
                    PageNumber = int.Parse(pageNumber)
                };
            }
            catch (Exception ex)
            {
                LogMessage($"Error parsing filename '{fileName}': {ex.Message}");
                return null;
            }
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
                LogMessage($"=== LOADING GROUP: {groupName}{(subGroup != null ? "/" + subGroup : "")} ===");

                DebugGroupLoading(groupName, subGroup);

                CurrentGroup = groupName;
                CurrentSubGroup = subGroup;
                _currentPages.Clear();

                var searchDirectories = new[]
                {
                    Path.Combine(_kneeboardPath, _currentAircraft),
                    _kneeboardPath
                };

                foreach (var directory in searchDirectories)
                {
                    if (!Directory.Exists(directory)) continue;

                    foreach (var file in Directory.GetFiles(directory, "*.png"))
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var pageInfo = ParseFileName(fileName);
                        if (pageInfo == null) continue;

                        bool groupMatches = pageInfo.GroupName.Equals(groupName, StringComparison.OrdinalIgnoreCase);

                        bool subGroupMatches;

                        if (string.IsNullOrEmpty(subGroup))
                        {
                            // Se cerchi il gruppo principale, accetta sia file senza sottogruppo che con sottogruppo
                            // subGroupMatches = string.IsNullOrEmpty(pageInfo.SubGroup);
                            subGroupMatches = true; // ← ACCETTA TUTTI I FILE DEL GRUPPO
                        }
                        else
                        {
                            // Se cerchi un sottogruppo specifico, matcha esattamente
                            subGroupMatches = pageInfo.SubGroup?.Equals(subGroup, StringComparison.OrdinalIgnoreCase) == true;
                        } 

                        bool nightModeMatches = _nightMode ? pageInfo.IsNightVersion : !pageInfo.IsNightVersion;

                        if (groupMatches && subGroupMatches && nightModeMatches)
                        {
                            LogMessage($"✓ MATCH: {fileName}");
                            _currentPages.Add(new KneeboardPage
                            {
                                FilePath = file,
                                Title = $"{groupName}{(subGroup != null ? " " + subGroup : "")}",
                                PageNumber = pageInfo.PageNumber
                            });
                        }
                        else if (groupMatches)
                        {
                            LogMessage($"✗ NO MATCH: {fileName} (Group: {groupMatches}, SubGroup: {subGroupMatches}, NightMode: {nightModeMatches})");
                        }
                    } 
                } 

                LogMessage($"Total pages found: {_currentPages.Count}");

                if (_currentPages.Count > 0)
                {
                    _currentPages = _currentPages.OrderBy(p => p.PageNumber).ToList();
                    _currentPage = 0;
                    DisplayCurrentPage();
                    OnPageChanged();
                }
                else
                {
                    LogMessage($"No pages found for: {groupName}{(subGroup != null ? "/" + subGroup : "")}");

                    // Se non trova file senza subgroup, prova a mostrare il primo subgroup disponibile
                    if (string.IsNullOrEmpty(subGroup))
                    {
                        TryLoadFirstSubGroup(groupName);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error loading group {groupName}: {ex.Message}");
            }
        }
        public void LoadGroup(string groupName, string subGroup = null, bool preventLoop = false)
        {
            try
            {
                LogMessage($"=== LOADING GROUP: {groupName}{(subGroup != null ? "/" + subGroup : "")} ===");

                CurrentGroup = groupName;
                CurrentSubGroup = subGroup;
                _currentPages.Clear();

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
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var pageInfo = ParseFileName(fileName);
                        if (pageInfo == null) continue;

                        bool groupMatches = pageInfo.GroupName.Equals(groupName, StringComparison.OrdinalIgnoreCase);

                        bool subGroupMatches;
                        if (string.IsNullOrEmpty(subGroup))
                        {
                            subGroupMatches = string.IsNullOrEmpty(pageInfo.SubGroup);
                        }
                        else
                        {
                            subGroupMatches = pageInfo.SubGroup?.Equals(subGroup, StringComparison.OrdinalIgnoreCase) == true;
                        }

                        bool nightModeMatches = _nightMode ? pageInfo.IsNightVersion : !pageInfo.IsNightVersion;

                        if (groupMatches && subGroupMatches && nightModeMatches)
                        {
                            LogMessage($"✓ MATCH: {fileName}");
                            _currentPages.Add(new KneeboardPage
                            {
                                FilePath = file,
                                Title = $"{groupName}{(subGroup != null ? " " + subGroup : "")}",
                                PageNumber = pageInfo.PageNumber
                            });
                        }
                    }
                }

                LogMessage($"Total pages found: {_currentPages.Count}");

                if (_currentPages.Count > 0)
                {
                    _currentPages = _currentPages.OrderBy(p => p.PageNumber).ToList();
                    _currentPage = 0;
                    DisplayCurrentPage();
                    OnPageChanged();
                }
                else
                {
                    LogMessage($"No pages found for: {groupName}{(subGroup != null ? "/" + subGroup : "")}");

                    // EVITA IL LOOP: chiama TryLoadFirstSubGroup solo se non è già in modalità preventLoop
                    if (string.IsNullOrEmpty(subGroup) && !preventLoop)
                    {
                        TryLoadFirstSubGroup(groupName, preventLoop: true);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error loading group {groupName}: {ex.Message}");
            }
        }

        private void TryFindAlternativePages(string groupName, string subGroup)
        {
            LogMessage($"Trying to find alternative pages for: {groupName}/{subGroup}");

            var searchDirectories = new[]
            {
                Path.Combine(_kneeboardPath, _currentAircraft),
                _kneeboardPath
            };

            foreach (var directory in searchDirectories)
            {
                if (!Directory.Exists(directory)) continue;

                foreach (var file in Directory.GetFiles(directory, "*.png"))
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var pageInfo = ParseFileName(fileName);
                    if (pageInfo == null) continue;

                    // Cerca file che contengono il nome del gruppo nel filename
                    bool nameContainsGroup = fileName.IndexOf(groupName, StringComparison.OrdinalIgnoreCase) >= 0;
                    bool nightModeMatches = _nightMode ? pageInfo.IsNightVersion : !pageInfo.IsNightVersion;

                    if (nameContainsGroup && nightModeMatches)
                    {
                        LogMessage($"✓ ALTERNATIVE FOUND: {fileName}");
                        _currentPages.Add(new KneeboardPage
                        {
                            FilePath = file,
                            Title = $"{groupName}{(subGroup != null ? " " + subGroup : "")}",
                            PageNumber = pageInfo.PageNumber
                        });
                    }
                }
            }

            if (_currentPages.Count > 0)
            {
                _currentPages = _currentPages.OrderBy(p => p.PageNumber).ToList();
                _currentPage = 0;
                DisplayCurrentPage();
                OnPageChanged();
                LogMessage($"Found {_currentPages.Count} alternative pages");
            }
        }

        //private void TryLoadFirstSubGroup(string groupName)
        //{
        //    LogMessage($"=== TRYLOAD FIRST SUBGROUP FOR: {groupName} ===");

        //    // Prima cerca nella cartella dell'aereo
        //    string aircraftPath = Path.Combine(_kneeboardPath, _currentAircraft);

        //    LogMessage($"Looking in: {aircraftPath}");
        //    LogMessage($"Directory exists: {Directory.Exists(aircraftPath)}");

        //    if (Directory.Exists(aircraftPath))
        //    {
        //        var groupFiles = Directory.GetFiles(aircraftPath, $"*{groupName}*.png");
        //        LogMessage($"Found {groupFiles.Length} files with '{groupName}' in name");

        //        foreach (var file in groupFiles)
        //        {
        //            var fileName = Path.GetFileNameWithoutExtension(file);
        //            LogMessage($"Checking file: {fileName}");

        //            var pageInfo = ParseFileName(fileName);

        //            if (pageInfo == null)
        //            {
        //                LogMessage($"  → Cannot parse filename");
        //                continue;
        //            }

        //            LogMessage($"  → Parsed: Group={pageInfo.GroupName}, SubGroup={pageInfo.SubGroup ?? "(null)"}");

        //            if (pageInfo.GroupName.Equals(groupName, StringComparison.OrdinalIgnoreCase) &&
        //                !string.IsNullOrEmpty(pageInfo.SubGroup))
        //            {
        //                LogMessage($"✓ FOUND SUBGROUP: {pageInfo.SubGroup}");
        //                LoadGroup(groupName, pageInfo.SubGroup);
        //                return;
        //            }
        //            else
        //            {
        //                LogMessage($"✗ Not suitable: GroupMatch={pageInfo.GroupName.Equals(groupName)}, HasSubGroup={!string.IsNullOrEmpty(pageInfo.SubGroup)}");
        //            }
        //        }
        //    }
        //    else
        //    {
        //        LogMessage($"❌ Aircraft path does not exist: {aircraftPath}");

        //        // Cerca in altre cartelle
        //        LogMessage("Checking other directories...");
        //        var allFolders = Directory.GetDirectories(_kneeboardPath);
        //        foreach (var folder in allFolders)
        //        {
        //            var filesInFolder = Directory.GetFiles(folder, $"*{groupName}*.png");
        //            if (filesInFolder.Length > 0)
        //            {
        //                LogMessage($"Found {filesInFolder.Length} files in {Path.GetFileName(folder)}");
        //            }
        //        }
        //    }

        //    LogMessage($"No subgroups found for {groupName}");
        //}
        private void TryLoadFirstSubGroup(string groupName, bool preventLoop = false)
        {
            if (!preventLoop)
            {
                LogMessage($"=== TRYLOAD FIRST SUBGROUP FOR: {groupName} ===");
            }
            else
            {
                LogMessage($"=== TRYLOAD FIRST SUBGROUP (PREVENT LOOP) FOR: {groupName} ===");
            }

            // Prima cerca nella cartella dell'aereo
            string aircraftPath = Path.Combine(_kneeboardPath, _currentAircraft + "_hornet");

            if (Directory.Exists(aircraftPath))
            {
                var groupFiles = Directory.GetFiles(aircraftPath, $"*{groupName}*.png");

                foreach (var file in groupFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var pageInfo = ParseFileName(fileName);

                    if (pageInfo != null && pageInfo.GroupName.Equals(groupName, StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrEmpty(pageInfo.SubGroup))
                    {
                        LogMessage($"Found subgroup in file: {pageInfo.SubGroup}");

                        // USA IL PARAMETRO preventLoop PER EVITARE IL LOOP
                        LoadGroup(groupName, pageInfo.SubGroup, preventLoop: true);
                        return;
                    }
                }
            }

            LogMessage($"No subgroups found for {groupName}");
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

            // Usa il metodo dell'interfaccia invece di cast diretto
            _display?.ShowCurrentPage();

            // Console fallback
            if (Program.IsTestMode && _display == null)
            {
                SafeConsoleClear();
                SafeConsoleWriteLine($"=== {_currentAircraft} - {_currentTheater} ===");
                SafeConsoleWriteLine($"=== {page.Title} ===");
                SafeConsoleWriteLine($"Page {_currentPage + 1} of {_currentPages.Count}");
                SafeConsoleWriteLine("==========================================");
                SafeConsoleWriteLine($"File: {Path.GetFileName(page.FilePath)}");
                SafeConsoleWriteLine("==========================================");
                SafeConsoleWriteLine("Commands: N=Next, P=Previous, M=Menu, T=Toggle Night, Q=Quit");
            }
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
        public void DebugFileStructure()
        {
            LogMessage("=== DEBUG FILE STRUCTURE ===");

            var searchDirectories = new[]
            {
        Path.Combine(_kneeboardPath, _currentAircraft),
        _kneeboardPath
    };

            foreach (var directory in searchDirectories)
            {
                if (!Directory.Exists(directory))
                {
                    LogMessage($"Directory not found: {directory}");
                    continue;
                }

                LogMessage($"=== FILES IN {directory} ===");
                var files = Directory.GetFiles(directory, "*.png");

                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var pageInfo = ParseFileName(fileName);

                    if (pageInfo != null)
                    {
                        LogMessage($"{fileName} → Group: {pageInfo.GroupName}, SubGroup: {pageInfo.SubGroup}, Night: {pageInfo.IsNightVersion}");
                    }
                    else
                    {
                        LogMessage($"{fileName} → CANNOT PARSE");
                    }
                }
            }

            LogMessage("=== END DEBUG ===");
        }
        public void DebugOrdnanceFiles()
        {
            LogMessage("=== DEBUG ORDNANCE FILES ===");

            var searchDirectories = new[]
            {
        Path.Combine(_kneeboardPath, _currentAircraft),
        _kneeboardPath
    };

            foreach (var directory in searchDirectories)
            {
                if (!Directory.Exists(directory))
                {
                    LogMessage($"Directory not found: {directory}");
                    continue;
                }

                var ordnanceFiles = Directory.GetFiles(directory, "*Ordnance*.png");
                LogMessage($"Found {ordnanceFiles.Length} Ordnance files in {directory}");

                foreach (var file in ordnanceFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    LogMessage($"File: {fileName}");

                    var pageInfo = ParseFileName(fileName);
                    if (pageInfo != null)
                    {
                        LogMessage($"  → Group: {pageInfo.GroupName}, SubGroup: {pageInfo.SubGroup}, Night: {pageInfo.IsNightVersion}, Page: {pageInfo.PageNumber}");
                    }
                    else
                    {
                        LogMessage($"  → CANNOT PARSE");
                    }
                }
            }
        }
        private void DebugMismatchedFiles(string groupName, string subGroup)
        {
            LogMessage("=== DEBUG MISMATCHED FILES ===");

            string aircraftSpecificPath = Path.Combine(_kneeboardPath, _currentAircraft);
            var searchDirectories = new[] { aircraftSpecificPath, _kneeboardPath };

            foreach (var directory in searchDirectories)
            {
                if (!Directory.Exists(directory)) continue;

                var allFiles = Directory.GetFiles(directory, $"*{groupName}*.png");
                if (allFiles.Length == 0) continue;

                LogMessage($"Files containing '{groupName}' in {directory}:");

                foreach (var file in allFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var pageInfo = ParseFileName(fileName);

                    if (pageInfo != null)
                    {
                        bool nightMatch = _nightMode ? pageInfo.IsNightVersion : pageInfo.IsDayVersion;
                        LogMessage($"  {fileName} → Group: {pageInfo.GroupName}, SubGroup: {pageInfo.SubGroup}, Night: {pageInfo.IsNightVersion}, NightMatch: {nightMatch}");
                    }
                    else
                    {
                        LogMessage($"  {fileName} → Cannot parse");
                    }
                }
            }
        }
        public void DebugSearchPaths(string groupName)
        {
            LogMessage("=== DEBUG SEARCH PATHS ===");

            string aircraftPath = Path.Combine(_kneeboardPath, _currentAircraft + "_hornet");
            var searchDirectories = new[] { aircraftPath, _kneeboardPath };

            foreach (var directory in searchDirectories)
            {
                if (!Directory.Exists(directory))
                {
                    LogMessage($"❌ Directory not found: {directory}");
                    continue;
                }

                LogMessage($"🔍 Searching in: {directory}");

                // Cerca tutti i file PNG
                var allFiles = Directory.GetFiles(directory, "*.png");
                LogMessage($"   Found {allFiles.Length} PNG files");

                // Cerca file specifici per il gruppo
                var groupFiles = Directory.GetFiles(directory, $"*{groupName}*.png");
                LogMessage($"   Found {groupFiles.Length} files containing '{groupName}'");

                foreach (var file in groupFiles)
                {
                    LogMessage($"     📄 {Path.GetFileName(file)}");
                }

                // Mostra i primi 10 file per vedere cosa c'è nella cartella
                if (allFiles.Length > 0)
                {
                    LogMessage($"   First 10 files in directory:");
                    foreach (var file in allFiles.Take(10))
                    {
                        LogMessage($"     📄 {Path.GetFileName(file)}");
                    }
                }
            }
        }

        public void DebugGroupLoading(string groupName, string subGroup = null)
        {
            LogMessage($"=== DEBUG GROUP LOADING ===");
            LogMessage($"Group: {groupName}, SubGroup: {subGroup ?? "(null)"}");
            LogMessage($"Night Mode: {_nightMode}");

            var targetGroup = _availableGroups.FirstOrDefault(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
            if (targetGroup != null)
            {
                LogMessage($"Group found: {targetGroup.Name}");
                LogMessage($"Has subgroups: {targetGroup.SubGroups.Count}");
                foreach (var sub in targetGroup.SubGroups)
                {
                    LogMessage($"  - {sub}");
                }
            }
            else
            {
                LogMessage($"Group not found in available groups!");
            }
        }
        public void DebugChartsParsing()
        {
            LogMessage("=== DEBUG CHARTS PARSING ===");

            string aircraftPath = Path.Combine(_kneeboardPath, _currentAircraft + "_hornet");

            if (Directory.Exists(aircraftPath))
            {
                var chartsFiles = Directory.GetFiles(aircraftPath, "*Charts*.png");
                LogMessage($"Found {chartsFiles.Length} Charts files");

                foreach (var file in chartsFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var pageInfo = ParseFileName(fileName);

                    if (pageInfo != null)
                    {
                        LogMessage($"File: {fileName}");
                        LogMessage($"  → Group: {pageInfo.GroupName}, SubGroup: {pageInfo.SubGroup ?? "(null)"}, Night: {pageInfo.IsNightVersion}");
                    }
                    else
                    {
                        LogMessage($"File: {fileName} → CANNOT PARSE");
                    }
                }
            }
        }
    }


    public class PageGroup
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool HasDayVersion { get; set; }
        public bool HasNightVersion { get; set; }
        public string FilePath { get; set; }
        public List<string> SubGroups { get; set; } = new List<string>();
        public Dictionary<string, bool> SubGroupDayVersions { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> SubGroupNightVersions { get; set; } = new Dictionary<string, bool>();
    }

    public class PageInfo
    {
        public string GroupName { get; set; }
        public string SubGroup { get; set; }
        public bool IsNightVersion { get; set; }
        public bool IsDayVersion { get; set; }
        public string FullName { get; set; }
        public int PageNumber { get; set; }
    }

    public class KneeboardPage
    {
        public string FilePath { get; set; }
        public string Title { get; set; }
        public int PageNumber { get; set; }
    }
}