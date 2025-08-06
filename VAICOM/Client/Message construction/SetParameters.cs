using System.Collections.Generic;
using System.Security.Cryptography;
using VAICOM.Static;

namespace VAICOM
{

    namespace Client
    {

        public partial class DcsClient
        {
            public static partial class Message
            {

                public static void SetParameters()
                {
                    string selector = State.currentcommand.dcsid;

                    switch (selector)
                    {
                        case "wMsgLeaderMakeRecon": // make recon
                            EnsureParametersIsList();
                            ((List<object>)State.currentmessage.parameters).Add(State.currentcommand.parameters);
                            break;

                        case "wMsgLeaderInbound": // ATC inbound 
                            State.currentcommand.point = State.currentstate.availablerecipients["Player"][0].pos;
                            EnsureParametersIsList();
                            ((List<object>)State.currentmessage.parameters).Add(State.currentcommand.point);
                            break;

                        case "wMsgLeaderCheckIn":// jtac check-in
                            EnsureParametersIsList();
                            ((List<object>)State.currentmessage.parameters).Add(State.currentcommand.parameters);
                            break;

                        case "wMsgLeader9LineReadback": // jtac readback
                            EnsureParametersIsList();
                            ((List<object>)State.currentmessage.parameters).Add(State.currentcommand.readback);
                            break;

                        case "wMsgLeaderSpecialCommand": // crew special command to add device and paramater types.
                            {
                                int commandkey = State.currentcommand.uniqueid;
                                switch (commandkey)
                                {
                                    case 18002: //boarding ladder
                                        Log.Write("Setting parameters for Stow Boarding ladder", Colors.Inline);
                                        State.currentmessage.parameters = new Dictionary<string, object> { { "type", 7 } };
                                        break;

                                    case 18003: //inertial starter
                                        Log.Write("Setting parameters for Inertial Starter", Colors.Inline);
                                        State.currentmessage.parameters = new Dictionary<string, object> { { "type", 9 } };
                                        break;

                                    case 18004: // Request HMD
                                        Log.Write("Setting parameters for Request HMD", Colors.Inline);
                                        State.currentmessage.parameters = new Dictionary<string, object> { { "type", 4 }, { "device", 0 } };
                                        break;

                                    case 18005: // Request NVG
                                        Log.Write("Setting parameters for Request NVG", Colors.Inline);
                                        State.currentmessage.parameters = new Dictionary<string, object> { { "type", 4 }, { "device", 1 } };
                                        break;

                                    case 18007: //epu on
                                        Log.Write("Setting parameters for EPU ON", Colors.Inline);
                                        State.currentmessage.parameters = new Dictionary<string, object> { { "type", 5 }, { "power_source", 0 } };
                                        break;

                                    case 18008: //epu off
                                        Log.Write("Setting parameters for EPU OFF", Colors.Inline);
                                        State.currentmessage.parameters = new Dictionary<string, object> { { "type", 5 }, { "power_source", 1 } };
                                        break;

                                    case 18009: //turbo on
                                        Log.Write("Setting parameters for Turbo ON", Colors.Inline);
                                        State.currentmessage.parameters = new Dictionary<string, object> { { "name", State.currentcommand.parametername }, { "value", State.currentcommand.value } };
                                        break;

                                    case 18010: //turbo off
                                        Log.Write("Setting parameters for Turbo OFF", Colors.Inline);
                                        State.currentmessage.parameters = new Dictionary<string, object> { { "name", State.currentcommand.parametername }, { "value", State.currentcommand.value } };
                                        break;

                                    case 18006: //load water
                                        EnsureParametersIsList();
                                        ((List<object>)State.currentmessage.parameters).Add(State.currentcommand.device); // load water for AV-8B
                                        ((List<object>)State.currentmessage.parameters).Add(State.currentcommand.volume);
                                        break;

                                    default:
                                        break;
                                }
                            }
                            break;

                        case "wMsgLeaderRequestRefueling":
                            EnsureParametersIsList();
                            ((List<object>)State.currentmessage.parameters).Add(State.currentcommand.volume);
                            break;

                        case "wMsgLeaderGroundToggleElecPower":
                            EnsureParametersIsList();
                            ((List<object>)State.currentmessage.parameters).Add(State.currentcommand.on);
                            break;

                        case "wMsgLeaderGroundToggleWheelChocks":
                            EnsureParametersIsList();
                            ((List<object>)State.currentmessage.parameters).Add(State.currentcommand.on); // F-14 try leaving state null?
                            break;

                        case "wMsgLeaderGroundToggleCanopy":
                            EnsureParametersIsList();
                            ((List<object>)State.currentmessage.parameters).Add(State.currentcommand.close); //.on
                            break;

                        case "wMsgLeaderGroundToggleAir":
                            EnsureParametersIsList();
                            ((List<object>)State.currentmessage.parameters).Add(State.currentcommand.on);
                            break;

                        case "wMsgLeaderGroundApplyAir":
                            EnsureParametersIsList();
                            ((List<object>)State.currentmessage.parameters).Add(State.currentcommand.on);
                            break;

                        case "wMsgLeaderGroundGestureSalut":
                            EnsureParametersIsList();
                            ((List<object>)State.currentmessage.parameters).Add(State.currentcommand.on);
                            break;

                        case "wMsgLeaderGroundRequestLaunch":
                            EnsureParametersIsList();
                            ((List<object>)State.currentmessage.parameters).Add(State.currentcommand.on);
                            break;

                        default:
                            Log.Write($"Unhandled command: {State.currentcommand.dcsid}, uniqueid: {State.currentcommand.uniqueid}", Colors.Warning);
                            break;
                    }
                }

                // Helper method to ensure parameters is a List<object>
                private static void EnsureParametersIsList()
                {
                    if (!(State.currentmessage.parameters is List<object>))
                    {
                        State.currentmessage.parameters = new List<object>();
                    }
                }

            }
        }
    }
}




