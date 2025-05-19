using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Redux.Packets.Game;
using Redux.Enum;


namespace Redux.Npcs
{

    /// <summary>
    /// Handles NPC usage for [1000941] LanternToken - Proficiency Upgrade
    /// </summary>
    public class NPC_1000941 : INpc
    {

        bool hasAgreed = false;


        public NPC_1000941(Game_Server.Player _client)
            : base(_client)
        {
            ID = 1000941;
            Face = 9;
        }

        public override void Run(Game_Server.Player _client, ushort _linkback)
        {
            Responses = new List<NpcDialogPacket>();
            AddAvatar();
            switch (_linkback)
            {

                case 0:
                    {
                        AddText("Lucky that you have a LanternToken. I can upgrade any weapon proficiency up to 12. What weapon would you like to upgrade?");
                        AddOption("Blade ", 10);
                        AddOption("Club ", 80);
                        AddOption("Sword ", 20);
                        AddOption("Bow ", 100);
                        AddOption("Next Page", 2);
                        AddOption("No. I will do it myself.", 255);
                        break;
                    }

                case 2:
                    {
                        AddText("Choose the weapon you want to upgrade ițs proficiency from the list below. ");
                        AddOption("Spear ", 160);
                        AddOption("Wand ", 161);
                        AddOption("Hook ", 30);
                        AddOption("Whip ", 40);
                        AddOption("Next Page", 3);
                        AddOption("I changed my mind. ", 255);

                        break;
                    }
                case 3:
                    {
                        AddText("Choose the weapon you want to upgrade ițs proficiency from the list below. ");
                        AddOption("PoleAxe ", 130);
                        AddOption("LongHammer", 140);
                        AddOption("Glaive ", 110);
                        AddOption("Halbert", 180);
                        AddOption("Next Page", 4);
                        AddOption("I changed my mind. ", 255);
                        break;
                    }
                case 4:
                    {
                        AddText("Choose the weapon you want to upgrade ițs proficiency from the list below. ");
                        AddOption("Backsword ", 21);
                        AddOption("Scepter ", 81);
                        AddOption("Dagger ", 90);
                        AddOption("Hammer ", 60);
                        AddOption("Axe", 50);
                        AddOption("I changed my mind. ", 255);
                        break;
                    }
                case 10:
                case 20:
                case 21:
                case 30:
                case 40:
                case 50:
                case 60:
                case 80:
                case 81:
                case 90:
                case 100:
                case 110:
                case 130:
                case 140:
                case 160:
                case 161:
                case 180:
                    {
                        if (!_client.CombatManager.proficiencies.ContainsKey((ushort)(_linkback + 400)))
                        {
                            AddText("You must have the proficiency for that weapon at least level 1!");
                            AddOption("Nevermind..", 255);
                            break;
                        }
                        var prof = _client.CombatManager.proficiencies[(ushort)(_linkback + 400)];
                        var cost = prof.Level + 1;
                        if (prof.Level > 11)
                        {
                            AddText("Sorry but I can only upgrade weapon proficiency up to level 12. You can still improve it by attacking Monster.");
                            AddOption("Nevermind..", 255);

                            break;
                        }
                        if (hasAgreed && !_client.HasItem(Constants.LANTERNTOKEN_ID, cost))
                        {
                            AddText("There must be some mistake. You do not have LanternToken, you must carry " + cost + "!");
                            AddOption("Nevermind..", 255);

                            break;
                        }
                        else if (hasAgreed)
                        {
                            for (var i = 0; i < cost; i++)
                                _client.DeleteItem(Constants.LANTERNTOKEN_ID);
                            prof.Level++;
                            prof.Experience = 0;
                            _client.CombatManager.AddOrUpdateProf((ushort)(_linkback + 400), (ushort)(prof.Level), 0);
                            AddText("It is done!");
                            AddOption("No thanks.", 255);
                            _client.Save();
                        }
                        else
                        {
                            hasAgreed = true;
                            AddText("Are you sure you want to spend  " + cost + " LanternToken to upgrade it?");
                            AddOption("Yes.", (byte)_linkback);
                            AddOption("No thanks.", 255);

                        }
                        break;
                    }
            }
            AddFinish();
            Send();
        }
    }
}