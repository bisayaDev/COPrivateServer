using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Redux.Database;
using Redux.Enum;
using Redux.Game_Server;
using Redux.Packets.Game;
using Redux.Structures;

namespace Redux.Npcs
{

    public class NPC_1337 : INpc
    {
        /// <summary>
        /// Handles NPC usage for [1337] Welcomer
        /// </summary>
        public NPC_1337(Game_Server.Player _client)
            : base(_client)
        {
            ID = 1337;
            Face = 1;
        }

        public override void Run(Game_Server.Player _client, ushort _linkback)
        {
            Responses = new List<NpcDialogPacket>();
            AddAvatar();
            switch (_linkback)
            {
                case 0:
                    AddText("Greetings! I am the Token Awarder, in charge of administrating and managing");
                    AddText(" ingame rewards. What business do you have with me?");
                    AddOption("Claim Starter Pack", 1);
                    AddOption("VIP Token Inquire", 2);
                    break;

                case 1:
                    if (_client.Account.hasClaimReward != 1)
                    {
                        if(_client.Inventory.Count > 30)
                        {
                            AddText("You need atleast 10 inventory slot to claim the rewards. Free up some inventory space to proceed.");
                            AddOption("Sorry about that!", 255);
                            break;
                        }
                        AddText("Welcome to DFEC-CO Server! Here's your welcome reward.\n");
                        AddText("1.) ExpBall - 10pcs\n");
                        AddText("2.) Silver - 30,000");
                        AddOption("Thanks!", 255);
                        for (int i = 0; i < 10; i++)
                        {
                            var info = Database.ServerDatabase.Context.ItemInformation.GetById(723700);
                            var item = new ConquerItem((uint)Common.ItemGenerator.Counter, info);
                            item.SetOwner(_client);
                            if (_client.AddItem(item))
                                _client.Send(ItemInformationPacket.Create(item));
                            else
                                _client.SendMessage("Failed to add item.");
                        }
                        _client.Money += 30000;
                        _client.Account.hasClaimReward = 1;
                    }
                    else
                    {
                        AddText("Oppss.. It seems that you already claim the welcome reward.");
                        AddOption("Ohh.. I see.", 255);
                    }
                    break;

                case 2:
                    {
                        AddText("Hello " + _client.Name + "! What can I help you about VIP Token?");
                        AddOption("I have a VIP Token", 3);
                        AddOption("I changed my mind.", 255);
                    }
                    break;

                case 3:
                    AddText("Really? So, can you input your VIP Token below so I can check it?");
                    AddInput("VIP Token", 4);
                    AddOption("I changed my mind.", 255);
                    break;

                case 4:
                    String vip_token = _client.NpcInputBox;
                    if (vip_token == "dfec")
                    {
                        AddText("Wow! This VIP Token is valid. See token information below.\n");
                        AddText("VIP Level = 7");
                        AddText("\nDuration = 30 days");
                        AddOption("Claim VIP Token reward", 5);
                        AddOption("Thanks for the info. Bye!", 255);
                    }
                    else
                    {
                        AddText("I'm sorry, the VIP Token you provided is not a valid token.");
                        AddOption("My bad.", 255);
                    }
                    break;

                case 5:
                    AddText("How do you want to claim your VIP Token reward?");
                    AddOption("Reward it to me.", 6);
                    AddOption("Gift it to someone", 7);
                    AddOption("Nevermind. I'll come back", 255);
                    break;

                case 6:
                    AddText("Are you sure you want to claim the reward now in this character?");
                    AddOption("Yes please!", 8);
                    AddOption("Gift it to someone", 7);
                    AddOption("I change my mind.", 255);
                    break;

                case 7:
                    AddText("Are you sure you want to gift the reward to someone? If Yes, then give me the name of the character you want me to send the VIP reward.");
                    AddInput("Character Name", 9);
                    AddOption("I change my mind.", 255);
                    break;
                case 8:
                    _client.Account.VipLevel = Enum.PlayerVipLevel.Level7;
                    ServerDatabase.Context.Accounts.Update(_client.Account);
                    _client.SendToScreen(StringsPacket.Create(_client.UID, StringAction.Fireworks, "Congratulations!!!"), true);
                    AddText("Gotcha! " + _client.Name + " has claim the reward.");
                    AddOption("Thank you, Bye!", 255);

                    break;

                case 9://Gift the reward to someone.
                    String target_name = _client.NpcInputBox;
                    Player target = null;
                    foreach (var p in Managers.PlayerManager.Players.Values)
                        if (p.Name.ToLower() == target_name)
                        {
                            target = p;
                            break;
                        }
                    if (target == null)
                    {
                        AddText("Oppss... It seems that " + target_name + " is not online or could not be found.");
                        AddOption("My bad.", 255);
                    }
                    else
                    {
                        target.Account.VipLevel = Enum.PlayerVipLevel.Level7;
                        target.SendToScreen(StringsPacket.Create(target.UID, StringAction.Fireworks, "Congratulations!!!"), true);
                        _client.SendToScreen(StringsPacket.Create(_client.UID, StringAction.Fireworks, "Congratulations!!!"), true);
                        AddText("Gotcha! " + target.Name + " has received the reward.");
                        AddOption("Thank you, Bye!", 255);
                    }
                    break;
            }
            AddFinish();
            Send();
        }
    }
}