using Redux.Enum;
using Redux.Packets.Game;
using Redux.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Redux.Events
{
    public static class FreeForAll
    {
        public static bool Running = false;
        public static uint totalP = 0;
        public static bool signup = false;
        public static int signup_count = 0;
        // public static bool Send = false;

        /*public static void SendTimer()
        {
            Console.Write("FreeForAll Timer started! \n");
            System.Timers.Timer TimerA = new System.Timers.Timer(1000.0);
            TimerA.Start();
            TimerA.Elapsed += delegate { StartEvent(); };

            System.Timers.Timer TimerB = new System.Timers.Timer(1000.0);
            TimerB.Start();
            TimerB.Elapsed += delegate { Send(); };

            System.Timers.Timer TimerC = new System.Timers.Timer(1000.0);
            TimerC.Start();
            TimerC.Elapsed += delegate { CheckAlive(); };

            System.Timers.Timer TimerD = new System.Timers.Timer(1000.0);
            TimerD.Start();
            TimerD.Elapsed += delegate { EndEvent(); };
        } */
        public static void StartEvent()
        {
            
            foreach (var player in Managers.PlayerManager.Players.Values)
            {
                signup = true;
                Running = true;
                totalP = 0;
                Managers.PlayerManager.SendToServer(new TalkPacket(ChatType.GM, "Free For All Tournament has been started! Player may now Sign-Up to ArenaGuard at Twin City (438,360). You only have 1 - minute To Signup the event. Hurry Up!"));
                Managers.PlayerManager.SendToServer(new TalkPacket(ChatType.Broadcast, "FFA Tournament soon! Players may now Sign-Up at ArenaGuard in Twin City."));
                
            }
            
        }

        public static void Send()
        {
            
                
                Managers.PlayerManager.SendToServer(new TalkPacket(ChatType.Broadcast, "Free For All Event Started! First one to hit 100 times will win 1,000,000 silver. Good Luck!"));
                // CheckAlive();
                foreach (var player in Managers.PlayerManager.Players.Values)
                {
                    if (player.FFA_Signed == true)
                    {
                        player.totalHits = 0;
                        var map_coords = Common.Random.Next(1, 18);
                        
                        switch (map_coords)
                        {
                            case 1:
                                player.ChangeMap(1509, 82, 119);
                                break;
                            case 2:
                                player.ChangeMap(1509, 44, 091);
                                break;
                            case 3:
                                player.ChangeMap(1509, 53, 108);
                                break;
                            case 4:
                                player.ChangeMap(1509, 61, 88);
                                break;
                            case 5:
                                player.ChangeMap(1509, 74, 62);
                                break;
                            case 6:
                                player.ChangeMap(1509, 111, 77);
                                break;
                            case 7:
                                player.ChangeMap(1509, 103, 44);
                                break;
                            case 8:
                                player.ChangeMap(1509, 118, 92);
                                break;
                            case 9:
                                player.ChangeMap(1509, 130, 94);
                                break;
                            case 10:
                                player.ChangeMap(1509, 139, 77);
                                break;
                            case 11:
                                player.ChangeMap(1509, 139, 118);
                                break;
                            case 12:
                                player.ChangeMap(1509, 128, 131);
                                break;
                            case 13:
                                player.ChangeMap(1509, 138, 144);
                                break;
                            case 14:
                                player.ChangeMap(1509, 109, 143);
                                break;
                            case 15:
                                player.ChangeMap(1509, 86, 140);
                                break;
                            case 16:
                                player.ChangeMap(1509, 73, 146);
                                break;
                            case 17:
                                player.ChangeMap(1509, 73, 105);
                                break;
                            case 18:
                                player.ChangeMap(1509, 82, 101);
                                break;

                        }
                        player.PkMode = PKMode.PK;
                    }
                }
            

        }
        public static void CheckAlive()
        {
            signup_count = 0;
            foreach (var player in Managers.PlayerManager.Players.Values)
            {
                if (player.MapID == 1509)
                {
                    signup_count++;
                }
                Managers.PlayerManager.SendToServer(new Packets.Game.TalkPacket(ChatType.SynWarNext, "Event Title: Free For All "));
                Managers.PlayerManager.SendToServer(new Packets.Game.TalkPacket(ChatType.SynWarNext, "Total Player: " + signup_count));

            }
        }

        public static void CheckTotalSignup()
        {
            signup_count = 0;
            foreach (var player in Managers.PlayerManager.Players.Values)
            {
                if (player.FFA_Signed)
                {
                    signup_count++;
                }
            }
            Managers.PlayerManager.SendToServer(new Packets.Game.TalkPacket(ChatType.SynWarFirst, "Tournament Title: <Free For All>"));
            Managers.PlayerManager.SendToServer(new Packets.Game.TalkPacket(ChatType.SynWarNext, "Total SignUp Players: " + signup_count));

        }

        public static void EndEvent()
        {
            foreach (var player in Managers.PlayerManager.Players.Values)
            {
                
                if (player.MapID == 1509)
                {
                    player.SendSysMessage("Sorry! Someone already won the event. 20,000 silver as consolation prize. - Better luck next time!");
                    player.ChangeMap(1002, 438, 362);
                    player.Money += 20000;
                    player.PkMode = PKMode.Capture;
                    player.FFA_Signed = false;
                }
                Running = false;
                signup = false;


                /*else if (player.ToTalkills < 5)
                {
                    if (player.MapID == 1509)
                    {
                        player.ChangeMap(1002, 400, 400);
                        player.CP += 15;
                        player.FFA_Signed = false;
                    }
                    Running = false;
                    signup = false;
                }*/

            }
        }
    }
}