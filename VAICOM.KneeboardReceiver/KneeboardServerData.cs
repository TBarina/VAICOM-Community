using System;

namespace VAICOM.KneeboardReceiver
{
    [Serializable]
    public class KneeboardServerData
    {
        public string theater { get; set; }
        public string dcsversion { get; set; }
        public string aircraft { get; set; }
        public int flightsize { get; set; }
        public string playerusername { get; set; }
        public string playercallsign { get; set; }
        public string coalition { get; set; }
        public string sortie { get; set; }
        public string task { get; set; }
        public string country { get; set; }
        public string missiontitle { get; set; }
        public string missionbriefing { get; set; }
        public string missiondetails { get; set; }
        public bool multiplayer { get; set; }

        public KneeboardServerData()
        {
            // Valori di default per il testing
            theater = "Caucasus";
            dcsversion = "DCS.openbeta";
            aircraft = "FA-18C_hornet";
            flightsize = 4;
            playerusername = "TestPilot";
            playercallsign = "Viper 1-1";
            coalition = "BLUE";
            sortie = "TO PG MST 12:00 GST";
            task = "Combat Air Patrol";
            country = "USA";
            missiontitle = "Test Mission";
            missionbriefing = "This is a test mission for the kneeboard manager";
            missiondetails = "Test details for mission validation";
            multiplayer = false;
        }
    }
}
