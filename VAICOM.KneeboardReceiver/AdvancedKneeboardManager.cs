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
        public static bool IsWindowsFormsMode { get; set; } = false;

#region Properties     
        private IKneeboardDisplay _display;

        private string _kneeboardPath = "";


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
        private List<KneeboardPage> _currentPages = new List<KneeboardPage>();

        // CAMPI PRIVATI per la cache
        private class FileMetadata
        {
            public string FilePath { get; set; }
            public string FileName { get; set; }
            public string GroupName { get; set; }
            public string SubGroup { get; set; }
            public bool IsNightVersion { get; set; }
            public int PageNumber { get; set; }
            public string DisplayName { get; set; }
        }

        private string _cachedAircraft = "";
        private string _cachedTheater = "";
        private string _cachedEra = "";
        private bool _cachedNightMode = false;
        private Dictionary<string, List<FileMetadata>> _fileMetadataCache = new Dictionary<string, List<FileMetadata>>();
        private Dictionary<string, List<PageGroup>> _groupCache = new Dictionary<string, List<PageGroup>>();
        private Dictionary<string, List<KneeboardPage>> _pageCache = new Dictionary<string, List<KneeboardPage>>();


        // PROPRIETA' PUBBLICHE

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
            public string GroupName { get; set; }
            public string SubGroup { get; set; }
            public string PageNumber { get; set; }
            public string DisplayName { get; set; }

            // Oppure costruttore se necessario
            public KneeboardPage(string filePath, string groupName, string subGroup, string pageNumber, string displayName)
            {
                FilePath = filePath;
                GroupName = groupName;
                SubGroup = subGroup;
                PageNumber = pageNumber;
                DisplayName = displayName;
            }
        }

        public string CurrentAircraft => _currentAircraft;
        public string CurrentTheater => _currentTheater;
        public string CurrentEra => _currentEra;
        public bool NightMode => _nightMode;
        public string CurrentGroup { get; private set; }
        public string CurrentSubGroup { get; private set; }
        public int CurrentPageIndex => _currentPage;
        public int TotalPages => _currentPages.Count;
#endregion


        // METODI PER SOLLEVARE GLI EVENTI
        protected virtual void OnGroupsUpdated() => GroupsUpdated?.Invoke();
        protected virtual void OnPageChanged() => PageChanged?.Invoke();
        protected virtual void OnNightModeToggled() => NightModeToggled?.Invoke();

        #region Init...:
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
        public void DisplayClear()
        {
            _display?.Clear();
        }
        #endregion

        public void UpdateFromServerData(KneeboardServerData data)
        {
            try
            {
                string newAircraft = data.aircraft ?? "Unknown";
                string newTheater = data.theater ?? "Unknown";
                string newEra = _eraFilters.ContainsKey(newAircraft) ? _eraFilters[newAircraft] : "Modern";

                bool scenarioChanged = newAircraft != _cachedAircraft ||
                                      newTheater != _cachedTheater ||
                                      newEra != _cachedEra;

                _currentAircraft = newAircraft;
                _currentTheater = newTheater;
                _currentEra = newEra;

                if (scenarioChanged)
                {
                    // Scenario cambiato: pulisce TUTTE le cache
                    _fileMetadataCache.Clear();
                    _groupCache.Clear();
                    _pageCache.Clear();
                    LogMessage("All caches cleared due to scenario change");
                }

                // Scansiona metadati (SE necessario, UNA VOLTA SOLA)
                ScanAndCacheFileMetadata();

                // Costruisci gruppi dai metadati cache
                ScanAvailableGroups();

                _cachedAircraft = _currentAircraft;
                _cachedTheater = _currentTheater;
                _cachedEra = _currentEra;

                SafeInvoke(() => GroupsUpdated?.Invoke());
            }
            catch (Exception ex)
            {
                LogMessage($"Error updating server data: {ex.Message}");
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


        public List<PageGroup> GetAvailableGroups() => _availableGroups;

        private void ScanAvailableGroups()
        {
            string cacheKey = GetCacheKey(_currentAircraft, _currentTheater, _currentEra);

            if (_groupCache.ContainsKey(cacheKey))
            {
                LogMessage("Using cached groups");
                _availableGroups = _groupCache[cacheKey];
                return;
            }

            // Assicurati che i metadati siano in cache (PARSING UNA VOLTA SOLA)
            if (!_fileMetadataCache.ContainsKey(cacheKey))
            {
                ScanAndCacheFileMetadata();
            }

            _availableGroups.Clear();
            LogMessage("Building groups from cached metadata...");

            // Costruisci gruppi dai metadati cache - ZERO PARSING
            var groupedMetadata = _fileMetadataCache[cacheKey]
                .GroupBy(m => m.GroupName, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groupedMetadata)
            {
                var pageGroup = new PageGroup
                {
                    Name = group.Key,
                    DisplayName = group.Key.Replace("_", " "),
                    HasDayVersion = group.Any(m => !m.IsNightVersion),
                    HasNightVersion = group.Any(m => m.IsNightVersion),
                    SubGroups = group.Where(m => !string.IsNullOrEmpty(m.SubGroup))
                                   .Select(m => m.SubGroup)
                                   .Distinct()
                                   .ToList(),
                    FilePath = group.First().FilePath // Prendi il percorso dal primo file
                };

                _availableGroups.Add(pageGroup);
            }

            _availableGroups = _availableGroups
                .OrderBy(g => g.DisplayName)
                .ToList();

            // Salva nella cache dei gruppi
            _groupCache[cacheKey] = _availableGroups;
            LogMessage($"Built {_availableGroups.Count} groups from cached metadata");
        }
        private PageInfo ParseFileName(string fileName)
        {
            string prefix;
            string groupPart;
            string pageNumber;

            try
            {
                LogMessage($"Parsing filename: {fileName}"); // DEBUG

                var match = Regex.Match(fileName, @"^(\d+)-([A-Za-z0-9_\-;]+)-(\d+)$"); // Accetta lettere, numeri, underscores e semi-colon. Es.: 007-CheckList;001_Night-001

                if (!match.Success)
                {
                    // SECONDO TENTATIVO: pattern alternativo per formati diversi
                    match = Regex.Match(fileName, @"^([A-Za-z0-9_\-;]+)-(\d+)$");   // Accetta lettere, numeri, underscores e semi-colon.
                    if (match.Success)
                    {
                        // Gestione formato alternativo senza prefix
                        groupPart = match.Groups[1].Value;
                        pageNumber = match.Groups[2].Value;

                        return CreatePageInfo(groupPart, pageNumber, "0");
                    }

                    LogMessage($"Regex failed for: {fileName}");
                    return null;
                }
                

                prefix     = match.Groups[1].Value;
                groupPart  = match.Groups[2].Value; // Può contenere underscore e semi-colon. Es.: 007-CheckList;001_Night-001
                pageNumber = match.Groups[3].Value;

/*              bool isNight = groupPart.EndsWith("_Night");
                string cleanGroup = isNight ? groupPart.Replace("_Night", "") : groupPart;
                string[] groupPartParts = cleanGroup.Split(';');
                cleanGroup = groupPartParts[0];
                // Separa gruppo e sottogruppo
                string[] groupParts = cleanGroup.Split('_');
                string groupName = groupParts[0];
                // Il sottogruppo è tutto ciò che viene dopo il primo underscore
                string subGroup = groupParts.Length > 1 ? string.Join("_", groupParts.Skip(1)) : null;
*/
                return CreatePageInfo(groupPart, pageNumber, prefix);
            }
            catch (Exception ex)
            {
                LogMessage($"Error parsing filename '{fileName}': {ex.Message}");
                return null;
            }
        }
        private PageInfo CreatePageInfo(string groupPart, string pageNumber, string prefix)
        {

            bool isNight = groupPart.EndsWith("_Night");
            string cleanGroup = isNight ? groupPart.Replace("_Night", "") : groupPart;
            // Separa l'eventuale id
            string[] groupPartParts = cleanGroup.Split(';');
            cleanGroup = groupPartParts[0];
            
            // Separa gruppo e sottogruppo
            //string[] groupParts = cleanGroup.Split('_');
            //string groupName = groupParts[0];
            //// Il sottogruppo è tutto ciò che viene dopo il primo underscore
            //string subGroup = groupParts.Length > 1 ? string.Join("_", groupParts.Skip(1)) : null;


            // LOGICA MIGLIORATA per sottogruppi
            string groupName = cleanGroup;
            string subGroup = null;

            // Cerca l'ultimo underscore per separare gruppo/sottogruppo
            int lastUnderscore = cleanGroup.LastIndexOf('_');
            if (lastUnderscore > 0)
            {
                groupName = cleanGroup.Substring(0, lastUnderscore);
                subGroup = cleanGroup.Substring(lastUnderscore + 1);

                // Se il sottogruppo è numerico, potrebbe essere parte del gruppo
                //if (int.TryParse(subGroup, out _))
                //{
                //    groupName = cleanGroup;
                //    subGroup = null;
                //}
            }

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
       
        public void LoadGroup(string groupName)
        {
            CurrentGroup = groupName;
            LoadGroup(groupName, null);
        }
        public void LoadGroup(string groupName, string subGroup = null)
        {
            try
            {
                string cacheKey = GetCacheKey(_currentAircraft, _currentTheater, _currentEra);
                string pageCacheKey = GetPageCacheKey(groupName, subGroup);

                // Assicurati che i metadati siano in cache (PARSING UNA VOLTA SOLA)
                if (!_fileMetadataCache.ContainsKey(cacheKey))
                {
                    ScanAndCacheFileMetadata();
                }

                // Controlla cache pagine
                bool usePageCache = _nightMode == _cachedNightMode && _pageCache.ContainsKey(pageCacheKey);

                if (usePageCache)
                {
                    LogMessage($"Using cached pages for: {pageCacheKey}");
                    _currentPages = _pageCache[pageCacheKey];
                }
                else
                {
                    LogMessage($"Filtering pages from metadata cache for: {pageCacheKey}");

                    // FILTRO IN MEMORIA - ZERO PARSING
                    var filteredMetadata = _fileMetadataCache[cacheKey]
                        .Where(m => m.GroupName.Equals(groupName, StringComparison.OrdinalIgnoreCase))
                        .Where(m => string.IsNullOrEmpty(subGroup) ?
                                   string.IsNullOrEmpty(m.SubGroup) :
                                   m.SubGroup?.Equals(subGroup, StringComparison.OrdinalIgnoreCase) == true)
                        .Where(m => _nightMode ? m.IsNightVersion : !m.IsNightVersion)
                        .OrderBy(m => m.PageNumber)
                        .ToList();

                    // Converti in KneeboardPage (SENZA PARSING!)
                    _currentPages = filteredMetadata.Select(m => new KneeboardPage(
                        filePath: m.FilePath,
                        groupName: m.GroupName,
                        subGroup: m.SubGroup ?? string.Empty,
                        pageNumber: m.PageNumber.ToString("D3"),
                        displayName: m.DisplayName
                    )).ToList();

                    // Aggiorna cache pagine
                    _pageCache[pageCacheKey] = _currentPages;
                    _cachedNightMode = _nightMode;
                }

                CurrentGroup = groupName;
                CurrentSubGroup = subGroup;

                LogMessage($"Loaded {_currentPages.Count} pages from metadata cache");

                if (_currentPages.Count > 0)
                {
                    _currentPage = 0;
                    DisplayCurrentPage();
                    OnPageChanged();
                }
                else
                {
                    LogMessage($"No pages found for: {groupName}{(subGroup != null ? "/" + subGroup : "")}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error loading group {groupName}: {ex.Message}");
            }
        }

        
        public void ToggleNightMode()
        {
            _nightMode = !_nightMode;
            LogMessage($"Night mode: {(_nightMode ? "ON" : "OFF")}");

            // Night mode cambiata: invalida solo la cache delle pagine
            // I METADATI RESTANO VALIDI perché i file sono gli stessi!
            _pageCache.Clear();
            LogMessage("Page cache cleared due to night mode change");

            NightModeToggled?.Invoke();

            if (!string.IsNullOrEmpty(CurrentGroup))
            {
                // Ricarica dalla cache metadati (SENZA PARSING!)
                LoadGroup(CurrentGroup, CurrentSubGroup);
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
                SafeConsoleWriteLine($"=== {page.DisplayName} ===");
                SafeConsoleWriteLine($"Page {_currentPage + 1} of {_currentPages.Count}");
                SafeConsoleWriteLine("==========================================");
                SafeConsoleWriteLine($"File: {Path.GetFileName(page.FilePath)}");
                SafeConsoleWriteLine("==========================================");
                SafeConsoleWriteLine("Commands: N=Next, P=Previous, M=Menu, T=Toggle Night, Q=Quit");
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

        private string GetCacheKey(string aircraft, string theater, string era)
        {
            return $"{aircraft}_{theater}_{era}";
        }

        private string GetPageCacheKey(string groupName, string subGroup)
        {
            string subGroupPart = string.IsNullOrEmpty(subGroup) ? "MAIN" : subGroup;
            return $"{_currentAircraft}_{_currentTheater}_{_currentEra}_{groupName}_{subGroupPart}_{_nightMode}";
        }

        public void ClearCache()
        {
            _fileMetadataCache.Clear();
            _groupCache.Clear();
            _pageCache.Clear();
            LogMessage("All caches cleared manually");
        }
        public string GetCacheStatus()
        {
            return $"Metadata: {_fileMetadataCache.Count}, Groups: {_groupCache.Count}, Pages: {_pageCache.Count}";
        }

        private void ScanAndCacheFileMetadata()
        {
            string cacheKey = GetCacheKey(_currentAircraft, _currentTheater, _currentEra);

            if (_fileMetadataCache.ContainsKey(cacheKey))
            {
                LogMessage("Using cached file metadata");
                return;
            }

            LogMessage("Scanning and caching all file metadata...");
            var allMetadata = new List<FileMetadata>();

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

                    allMetadata.Add(new FileMetadata
                    {
                        FilePath = file,
                        FileName = fileName,
                        GroupName = pageInfo.GroupName,
                        SubGroup = pageInfo.SubGroup,
                        IsNightVersion = pageInfo.IsNightVersion,
                        PageNumber = pageInfo.PageNumber,
                        DisplayName = pageInfo.FullName ?? fileName
                    });
                }
            }

            _fileMetadataCache[cacheKey] = allMetadata;
            LogMessage($"Cached {allMetadata.Count} file metadata entries for key: {cacheKey}");
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
       
    //    public void DebugFileStructure()
    //    {
    //        LogMessage("=== DEBUG FILE STRUCTURE ===");

    //        var searchDirectories = new[]
    //        {
    //    Path.Combine(_kneeboardPath, _currentAircraft),
    //    _kneeboardPath
    //};

    //        foreach (var directory in searchDirectories)
    //        {
    //            if (!Directory.Exists(directory))
    //            {
    //                LogMessage($"Directory not found: {directory}");
    //                continue;
    //            }

    //            LogMessage($"=== FILES IN {directory} ===");
    //            var files = Directory.GetFiles(directory, "*.png");

    //            foreach (var file in files)
    //            {
    //                var fileName = Path.GetFileNameWithoutExtension(file);
    //                var pageInfo = ParseFileName(fileName);

    //                if (pageInfo != null)
    //                {
    //                    LogMessage($"{fileName} → Group: {pageInfo.GroupName}, SubGroup: {pageInfo.SubGroup}, Night: {pageInfo.IsNightVersion}");
    //                }
    //                else
    //                {
    //                    LogMessage($"{fileName} → CANNOT PARSE");
    //                }
    //            }
    //        }

    //        LogMessage("=== END DEBUG ===");
    //    }
    //    private void DebugMismatchedFiles(string groupName, string subGroup)
    //    {
    //        LogMessage("=== DEBUG MISMATCHED FILES ===");

    //        string aircraftSpecificPath = Path.Combine(_kneeboardPath, _currentAircraft);
    //        var searchDirectories = new[] { aircraftSpecificPath, _kneeboardPath };

    //        foreach (var directory in searchDirectories)
    //        {
    //            if (!Directory.Exists(directory)) continue;

    //            var allFiles = Directory.GetFiles(directory, $"*{groupName}*.png");
    //            if (allFiles.Length == 0) continue;

    //            LogMessage($"Files containing '{groupName}' in {directory}:");

    //            foreach (var file in allFiles)
    //            {
    //                var fileName = Path.GetFileNameWithoutExtension(file);
    //                var pageInfo = ParseFileName(fileName);

    //                if (pageInfo != null)
    //                {
    //                    bool nightMatch = _nightMode ? pageInfo.IsNightVersion : pageInfo.IsDayVersion;
    //                    LogMessage($"  {fileName} → Group: {pageInfo.GroupName}, SubGroup: {pageInfo.SubGroup}, Night: {pageInfo.IsNightVersion}, NightMatch: {nightMatch}");
    //                }
    //                else
    //                {
    //                    LogMessage($"  {fileName} → Cannot parse");
    //                }
    //            }
    //        }
    //    }
    //    public void DebugSearchPaths(string groupName)
    //    {
    //        LogMessage("=== DEBUG SEARCH PATHS ===");

    //        string aircraftPath = Path.Combine(_kneeboardPath, _currentAircraft + "_hornet");
    //        var searchDirectories = new[] { aircraftPath, _kneeboardPath };

    //        foreach (var directory in searchDirectories)
    //        {
    //            if (!Directory.Exists(directory))
    //            {
    //                LogMessage($"❌ Directory not found: {directory}");
    //                continue;
    //            }

    //            LogMessage($"🔍 Searching in: {directory}");

    //            // Cerca tutti i file PNG
    //            var allFiles = Directory.GetFiles(directory, "*.png");
    //            LogMessage($"   Found {allFiles.Length} PNG files");

    //            // Cerca file specifici per il gruppo
    //            var groupFiles = Directory.GetFiles(directory, $"*{groupName}*.png");
    //            LogMessage($"   Found {groupFiles.Length} files containing '{groupName}'");

    //            foreach (var file in groupFiles)
    //            {
    //                LogMessage($"     📄 {Path.GetFileName(file)}");
    //            }

    //            // Mostra i primi 10 file per vedere cosa c'è nella cartella
    //            if (allFiles.Length > 0)
    //            {
    //                LogMessage($"   First 10 files in directory:");
    //                foreach (var file in allFiles.Take(10))
    //                {
    //                    LogMessage($"     📄 {Path.GetFileName(file)}");
    //                }
    //            }
    //        }
    //    }
    //    public void DebugGroupLoading(string groupName, string subGroup = null)
    //    {
    //        LogMessage($"=== DEBUG GROUP LOADING ===");
    //        LogMessage($"Group: {groupName}, SubGroup: {subGroup ?? "(null)"}");
    //        LogMessage($"Night Mode: {_nightMode}");

    //        var targetGroup = _availableGroups.FirstOrDefault(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
    //        if (targetGroup != null)
    //        {
    //            LogMessage($"Group found: {targetGroup.Name}");
    //            LogMessage($"Has subgroups: {targetGroup.SubGroups.Count}");
    //            foreach (var sub in targetGroup.SubGroups)
    //            {
    //                LogMessage($"  - {sub}");
    //            }
    //        }
    //        else
    //        {
    //            LogMessage($"Group not found in available groups!");
    //        }
    //    }
    //    public void DebugChartsParsing()
    //    {
    //        LogMessage("=== DEBUG CHARTS PARSING ===");

    //        string aircraftPath = Path.Combine(_kneeboardPath, _currentAircraft + "_hornet");

    //        if (Directory.Exists(aircraftPath))
    //        {
    //            var chartsFiles = Directory.GetFiles(aircraftPath, "*Charts*.png");
    //            LogMessage($"Found {chartsFiles.Length} Charts files");

    //            foreach (var file in chartsFiles)
    //            {
    //                var fileName = Path.GetFileNameWithoutExtension(file);
    //                var pageInfo = ParseFileName(fileName);

    //                if (pageInfo != null)
    //                {
    //                    LogMessage($"File: {fileName}");
    //                    LogMessage($"  → Group: {pageInfo.GroupName}, SubGroup: {pageInfo.SubGroup ?? "(null)"}, Night: {pageInfo.IsNightVersion}");
    //                }
    //                else
    //                {
    //                    LogMessage($"File: {fileName} → CANNOT PARSE");
    //                }
    //            }
    //        }
    //    }
    //    public void DebugOrdnanceFiles()
    //    {
    //        LogMessage("=== DEBUG ORDNANCE FILES ===");

    //        var searchDirectories = new[]
    //        {
    //    Path.Combine(_kneeboardPath, _currentAircraft),
    //    _kneeboardPath
    //};

    //        foreach (var directory in searchDirectories)
    //        {
    //            if (!Directory.Exists(directory))
    //            {
    //                LogMessage($"Directory not found: {directory}");
    //                continue;
    //            }

    //            var ordnanceFiles = Directory.GetFiles(directory, "*Ordnance*.png");
    //            LogMessage($"Found {ordnanceFiles.Length} Ordnance files in {directory}");

    //            foreach (var file in ordnanceFiles)
    //            {
    //                var fileName = Path.GetFileNameWithoutExtension(file);
    //                LogMessage($"File: {fileName}");

    //                var pageInfo = ParseFileName(fileName);
    //                if (pageInfo != null)
    //                {
    //                    LogMessage($"  → Group: {pageInfo.GroupName}, SubGroup: {pageInfo.SubGroup}, Night: {pageInfo.IsNightVersion}, Page: {pageInfo.PageNumber}");
    //                }
    //                else
    //                {
    //                    LogMessage($"  → CANNOT PARSE");
    //                }
    //            }
    //        }
    //    }   
    }

}