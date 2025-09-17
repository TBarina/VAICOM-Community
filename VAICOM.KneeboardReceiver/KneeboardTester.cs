using System;
using System.Threading.Tasks;

namespace VAICOM.KneeboardReceiver
{
    // Classe di supporto per il test
    //public class KneeboardServerTestData
    //{
    //    public string theater { get; set; } = "Caucasus";
    //    public string dcsversion { get; set; } = "DCS.openbeta";
    //    public string aircraft { get; set; } = "FA-18C_hornet";
    //    public int flightsize { get; set; } = 4;
    //    public string playerusername { get; set; } = "TestPilot";
    //    public string playercallsign { get; set; } = "Viper 1-1";
    //    public string coalition { get; set; } = "BLUE";
    //    public string sortie { get; set; } = "TO PG MST 12:00 GST";
    //    public string task { get; set; } = "CAP";
    //    public string country { get; set; } = "USA";
    //    public string missiontitle { get; set; } = "Test Mission";
    //    public string missionbriefing { get; set; } = "This is a test mission for the kneeboard manager";
    //    public string missiondetails { get; set; } = "Test details for mission validation";
    //    public bool multiplayer { get; set; } = false;
    //}
    public static class KneeboardTester
    {
        private static AdvancedKneeboardManager _testManager;

        public static void SetupTestManager()
        {
            _testManager = Program.KneeboardManager;

            //Console.WriteLine("Setting up test manager...");
            _testManager.LogMessage("Setting up test manager...");

            string testPath = CreateTestEnvironment();
            _testManager.Initialize(testPath);

            // Dati di test
            var testData = new KneeboardServerData
            {
                aircraft = "FA-18C_hornet",
                theater = "PersianGulf",
                coalition = "BLUE",
                missiontitle = "Test Mission",
                missionbriefing = "Testing the kneeboard UI interface",
                flightsize = 2,
                playercallsign = "Test 1-1"
            };

            _testManager.UpdateFromServerData(testData);
            //Console.WriteLine("Test manager setup completed");
            _testManager.LogMessage("Test manager setup completed");

        }

        //static void RunTestMode(string[] args)
        //{
        //    if (args.Length > 1 && args[1] == "--interactive")
        //    {
        //        // Modalità console interattiva (vecchia)
        //        KneeboardTester.InteractiveTest();
        //    }
        //    else if (args.Length > 1 && args[1] == "--ui")
        //    {
        //        // Modalità UI grafica per test
        //        RunTestUI();
        //    }
        //    else
        //    {
        //        // Default: avvia l'UI di test
        //        RunTestUI();
        //    }
        //}

        //static void RunTestUI()
        //{
        //    Console.WriteLine("Starting UI test mode...");

        //    // Setup del manager per il test
        //    KneeboardTester.SetupTestManager();

        //    // Crea e avvia il form
        //    var mainForm = new MainForm();
        //    Application.Run(mainForm);
        //}

        //public static void RunTest()
        //{
        //    Console.WriteLine("=== KNEEBOARD MANAGER TEST MODE ===");
        //    Console.WriteLine("Testing UI without DCS...\n");

        //    try
        //    {
        //        // Setup
        //        string testPath = CreateTestEnvironment();
        //        var manager = new AdvancedKneeboardManager();
        //        manager.Initialize(testPath);

        //        // Simula dati DCS
        //        var testData = new KneeboardServerData
        //        {
        //            aircraft = "FA-18C_hornet",
        //            theater = "PersianGulf",
        //            coalition = "BLUE",
        //            missiontitle = "UI Test Mission",
        //            missionbriefing = "Testing the kneeboard UI interface",
        //            flightsize = 2,
        //            playercallsign = "Test 1-1"
        //        };

        //        manager.UpdateFromServerData(testData);

        //        // Avvia l'UI
        //        Console.WriteLine("Launching UI...");
        //        Console.WriteLine("Press any key to continue...");
        //        Console.ReadKey();

        //        var display = new KneeboardDisplay(manager);
        //        display.Run();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Test failed: {ex.Message}");
        //        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        //    }
        //}
       
        private static string CreateTestEnvironment()
        {
            return Path.Combine(Path.GetTempPath(), "DCS_Test_Kneeboard"); 


            string testPath = Path.Combine(Path.GetTempPath(), "DCS_Test_Kneeboard");

            try
            {
                // Pulisci e crea struttura
                if (Directory.Exists(testPath))
                {
                    Directory.Delete(testPath, true);
                }
                Directory.CreateDirectory(testPath);
                //Console.WriteLine($"Created test directory: {testPath}");
                Program.KneeboardManager.LogMessage($"Created test directory: {testPath}");

                // Crea contenuti di test più ricchi
                CreateAircraftContent(testPath, "FA-18C_hornet");
                CreateAircraftContent(testPath, "F-16C_50");
                CreateGeneralContent(testPath);

                return testPath;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error creating test environment: {ex.Message}");
                Program.KneeboardManager.LogMessage($"Error creating test environment: {ex.Message}");
                return testPath;
            }
        }

        private static void CreateAircraftContent(string basePath, string aircraft)
        {
            string aircraftPath = Path.Combine(basePath, aircraft);
            Directory.CreateDirectory(aircraftPath);

            // Checklist
            File.WriteAllText(Path.Combine(aircraftPath, "001-CheckList_Quick-001.png"),
                $"=== {aircraft} QUICK START ===\n1. Battery ON\n2. Engine START\n3. Systems CHECK");

            File.WriteAllText(Path.Combine(aircraftPath, "002-CheckList_Quick_Night-001.png"),
                $"=== {aircraft} NIGHT START ===\n1. Night Vision ON\n2. Lights DIM\n3. Instruments NIGHT");

            // Procedures
            File.WriteAllText(Path.Combine(aircraftPath, "003-Procedures_Takeoff-001.png"),
                $"=== {aircraft} TAKEOFF ===\n1. Flaps T/O\n2. Throttle MIL\n3. Rotate 150KT");

            // Weapons
            File.WriteAllText(Path.Combine(aircraftPath, "004-Ordnance-001.png"),
                $"=== {aircraft} WEAPONS ===\nAIM-9X, AIM-120, GBU-12\nJDAM, JSOW, TGP");
        }

        private static void CreateGeneralContent(string basePath)
        {
            // Brevity codes
            File.WriteAllText(Path.Combine(basePath, "001-BrevityCodes_A-001.png"),
                "=== BREVITY CODES A ===\nANGELS - Altitude in thousands\nBOGEY - Unknown aircraft");

            File.WriteAllText(Path.Combine(basePath, "002-BrevityCodes_B-001.png"),
                "=== BREVITY CODES B ===\nBANDIT - Hostile aircraft\nBOOMER - Tanker aircraft");

            // Maps
            File.WriteAllText(Path.Combine(basePath, "003-Maps_PersianGulf-001.png"),
                "=== PERSIAN GULF MAP ===\nAirbases:\n- Dubai\n- Abu Dhabi\n- Al Dhafra");
        }


        //private static void TestInitialization()
        //{
        //    Console.WriteLine("1. Testing Initialization...");

        //    // Crea un percorso di test temporaneo
        //    string testPath = Path.Combine(Path.GetTempPath(), "DCS_Test_Kneeboard");

        //    try
        //    {
        //        // Pulisci eventuali test precedenti
        //        if (Directory.Exists(testPath))
        //        {
        //            Console.WriteLine("Cleaning previous test...");
        //            Directory.Delete(testPath, true);
        //        }

        //        Directory.CreateDirectory(testPath);
        //        Console.WriteLine("Created test directory");

        //        // Crea struttura di test
        //        CreateTestStructure(testPath);

        //        // Verifica che i file siano stati creati
        //        Console.WriteLine("Checking created files...");
        //        string[] allFiles = Directory.GetFiles(testPath, "*", SearchOption.AllDirectories);
        //        foreach (string file in allFiles)
        //        {
        //            Console.WriteLine($"Found: {file}");
        //        }

        //        // Test del manager
        //        var manager = new AdvancedKneeboardManager();
        //        manager.Initialize(testPath);

        //        // Test della scansione gruppi
        //        Console.WriteLine("Testing group scanning...");
        //        manager.TestScanGroups();

        //        Console.WriteLine($"✓ Initialization successful: {testPath}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"✗ Initialization failed: {ex.Message}");
        //        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        //    }
        //}

        private static void CreateTestStructure(string basePath)
        {
            // Crea struttura FA-18C
            string fa18Path = Path.Combine(basePath, "FA-18C_hornet");
            Directory.CreateDirectory(fa18Path);

            // Crea file di test
            string testChecklist = @"=== FA-18C STARTUP CHECKLIST ===
                1. Battery - ON
                2. JFS - START
                3. Engine Instruments - CHECK
                4. APU - ON
                5. Avionics - POWER
                6. INS - ALIGN
                7. Radar - SETUP
                8. Armament - CONFIG";

            File.WriteAllText(Path.Combine(fa18Path, "001-CheckList_Quick-001.png"), testChecklist);
            File.WriteAllText(Path.Combine(fa18Path, "002-CheckList_Quick_Night-001.png"), testChecklist + "\nNIGHT VERSION");
            File.WriteAllText(Path.Combine(fa18Path, "003-Ordnance-001.png"), "=== ORDNANCE ===\nGBU-12, AIM-120, AIM-9X");

            // Crea struttura F-16C
            string f16Path = Path.Combine(basePath, "F-16C_50");
            Directory.CreateDirectory(f16Path);

            File.WriteAllText(Path.Combine(f16Path, "001-CheckList-001.png"), "=== F-16C STARTUP ===\n1. Battery\n2. JFS\n3. Engine");
        }

        //private static void TestWithSimulatedData()
        //{
        //    Console.WriteLine("\n2. Testing with Simulated DCS Data...");

        //    string testPath = Path.Combine(Path.GetTempPath(), "DCS_Test_Kneeboard");
        //    var manager = new AdvancedKneeboardManager();
        //    manager.Initialize(testPath);

        //    // Simula dati DCS
        //    var testData = new KneeboardServerData
        //    {
        //        aircraft = "FA-18C_hornet",
        //        theater = "PersianGulf",
        //        coalition = "BLUE",
        //        missiontitle = "Test Mission",
        //        missionbriefing = "This is a test mission for kneeboard manager",
        //        flightsize = 4,
        //        playercallsign = "Viper 1-1",
        //        playerusername = "TestUser"
        //    };

        //    manager.UpdateFromServerData(testData);

        //    Console.WriteLine("✓ Simulated data processed successfully");
        //}

        //private static void TestNavigation()
        //{
        //    Console.WriteLine("\n3. Testing Navigation Commands...");

        //    string testPath = Path.Combine(Path.GetTempPath(), "DCS_Test_Kneeboard");
        //    var manager = new AdvancedKneeboardManager();
        //    manager.Initialize(testPath);

        //    // Simula dati per avere contenuto
        //    var testData = new KneeboardServerData
        //    {
        //        aircraft = "FA-18C_hornet",
        //        theater = "PersianGulf"
        //    };

        //    manager.UpdateFromServerData(testData);

        //    // Test comandi
        //    Console.WriteLine("Testing group selection...");
        //    manager.ProcessCommand("1"); // Seleziona primo gruppo

        //    Console.WriteLine("Testing navigation...");
        //    manager.ProcessCommand("N"); // Next page
        //    manager.ProcessCommand("P"); // Previous page

        //    Console.WriteLine("Testing night mode...");
        //    manager.ProcessCommand("T"); // Toggle night mode

        //    Console.WriteLine("Testing menu return...");
        //    manager.ProcessCommand("M"); // Back to menu

        //    Console.WriteLine("✓ Navigation commands working");
        //}

        //public static void InteractiveTest()
        //{
        //    Console.WriteLine("=== INTERACTIVE TEST MODE ===");
        //    Console.WriteLine("Type commands to test the manager interactively");
        //    Console.WriteLine("Commands: 1-9=Select Group, N=Next, P=Previous, T=Toggle Night, M=Menu, Q=Quit\n");

        //    string testPath = Path.Combine(Path.GetTempPath(), "DCS_Test_Kneeboard");
        //    var manager = new AdvancedKneeboardManager();
        //    manager.Initialize(testPath);

        //    // Dati di test
        //    var testData = new KneeboardServerData
        //    {
        //        aircraft = "FA-18C_hornet",
        //        theater = "PersianGulf",
        //        coalition = "BLUE",
        //        missiontitle = "Interactive Test"
        //    };

        //    manager.UpdateFromServerData(testData);

        //    // Loop interattivo
        //    bool running = true;
        //    while (running)
        //    {
        //        var key = Console.ReadKey(intercept: true);
        //        string command = key.KeyChar.ToString();

        //        if (command.ToUpper() == "Q")
        //        {
        //            running = false;
        //            continue;
        //        }

        //        manager.ProcessCommand(command);
        //    }
        //}

    //    public static void InteractiveTest2()
    //    {
    //        Console.WriteLine("=== INTERACTIVE UI TEST ===");

    //        string testPath = CreateTestEnvironment();
    //        var manager = new AdvancedKneeboardManager();
    //        manager.Initialize(testPath);

    //        // Dati di test
    //        var testData = new KneeboardServerData
    //        {
    //            aircraft = "FA-18C_hornet",
    //            theater = "PersianGulf",
    //            coalition = "BLUE",
    //            missiontitle = "Interactive UI Test"
    //        };

    //        manager.UpdateFromServerData(testData);

    //        Console.WriteLine("UI Ready. Press any key to start...");
    //        Console.ReadKey();

    //        var display = new KneeboardDisplay(manager);
    //        display.Run();
    //    }


    }


}