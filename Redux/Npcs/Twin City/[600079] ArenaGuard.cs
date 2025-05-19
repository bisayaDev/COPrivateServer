using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Redux.Events;
using Redux.Packets.Game;

namespace Redux.Npcs
{
    /// <summary>
    /// Handles NPC usage for [10021] Boxer
    /// Written by pro4never 11/16/2014
    /// </summary>
    public class NPC_600079 : INpc
    {

        public NPC_600079(Game_Server.Player _client)
            :base (_client)
    	{
            ID = 600079;	
			Face = 7;    
    	}

        public override void Run(Game_Server.Player _client, ushort _linkback)
        {
            Responses = new List<NpcDialogPacket>();
            AddAvatar();
            switch (_linkback)
            {
                case 0:
                    {

                        AddText("Do you want to Sign-up for the Free For All tournament?");
                        AddOption("I want to know more", 1);
                        AddOption("Sign-up", 16);
                        AddOption("Just passing by", 255);
                        break;
                    }
                case 1:
                    {
                        AddText("Which tournament would you like to know it?");
                        AddOption("Free For All", 2);
                        break;
                    }
                #region Explain Events
                case 2:
                    {
                        AddText("Free for All tournament is simple you just need to use either FastBlade/ScentSword skills and hit any participants, and if you got a total score of " + Constants.FFA_WINNER_SCORE + " then you'll win " + Constants.FFA_WINNER_REWARD + " Silver!");
                        AddOption("Thankz", 255);
                        break;
                    }
                #endregion
                case 16:
                    {

                        #region  Free For All
                        if (Events.FreeForAll.Running == true && FreeForAll.signup == true)
                        {
                            if (_client.FFA_Signed == false)
                            {
                                _client.FFA_Signed = true;
                                FreeForAll.CheckTotalSignup();
                                AddText("You have been signed up for <Free For Fall> Tournament. Standby, the event will start soon.");
                                AddOption("Thank you!", 255);
                            }
                            else
                            {
                                AddText("You are already sign up for this tournament");
                                AddOption("Ahh I see", 255);
                            }
                        }
                        #endregion
                        else
                        {
                            if (FreeForAll.Running == false)
                            {
                                AddText("Sorry but the Free For All Tournament will start every 30 min of an hour.");
                                AddOption("Ahh I see", 255);
                            }
                        }
                        break;

                    }
            }
            AddFinish();
            Send();
        }
    }
}
