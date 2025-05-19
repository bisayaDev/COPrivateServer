using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Redux.Database.Domain;
using Redux.Managers;
using Redux.Packets.Game;
using Redux.Space;
using Redux.Enum;
using System.Data;
using Redux.Structures;

namespace Redux.Game_Server
{
    public class Monster : Entity
    {
        public bool IsActive = false;
        public Monster Mob;
        public uint TargetID { get; private set; }
        public CombatManager CombatEngine { get; private set; }
        public DbMonstertype BaseMonster { get; private set; }
        public SpawnManager Owner { get; private set; }
        public MonsterMode Mode { get; private set; }
        public byte Direction { get { return SpawnPacket.Direction; } set { SpawnPacket.Direction = value; } }
        public long DiedAt = 0, LastMove = 0, LastAttack = 0;
        public override void SetDisguise(Database.Domain.DbMonstertype _mob, long _duration) { }
        public Monster(DbMonstertype _type, SpawnManager _owner, Entity playerOwner = null)
            : base()
        {
            PkMode = _type.AttackMode == 3 ? PKMode.PK : PKMode.Capture;
            BaseMonster = _type;
            Owner = _owner;
            Map = Owner.Map;
            UID = (uint)Map.MobCounter.Counter;
            CombatStats = Structures.CombatStatistics.Create(BaseMonster);
            CombatEngine = new CombatManager(this);
            SpawnPacket = SpawnEntityPacket.Create(this);
        }

        public Monster(DbMonstertype _type)
            : base()
        {
            BaseMonster = _type;
            CombatStats = Structures.CombatStatistics.Create(BaseMonster);
            CombatEngine = new CombatManager(this);
            Life = _type.Life;
        }

        public override byte Level { get { return BaseMonster.Level; } set { } }
        public override uint Life
        {
            get { return SpawnPacket.Life; }
            set { SpawnPacket.Life = value; }
        }

        //Gets us a new location and handles all map insertion.
        public void Respawn()
        {
            base.sentDeath = false;
            Mode = MonsterMode.Idle;
            DiedAt = 0;
            SpawnPacket.StatusEffects = 0;
            ClientEffects.Clear();
            Direction = (byte)Common.Random.Next(9);
            //Pull a new location for us that fits within spawn grounds
            var loc = new Point(Common.Random.Next(Owner.TopLeft.X, Owner.BottomRight.X), Common.Random.Next(Owner.TopLeft.Y, Owner.BottomRight.Y));
            var loop = 0;

            while (!Map.IsValidMonsterLocation(loc))
            {
                loop++;
                loc = new Point(Common.Random.Next(Owner.TopLeft.X, Owner.BottomRight.X), Common.Random.Next(Owner.TopLeft.Y, Owner.BottomRight.Y));
                if (loop > 100)
                {
                    Console.WriteLine("Error assigning monster spawn! Monster will not be revived");
                    break;
                }
            }
            if (!Map.IsValidMonsterLocation(loc))
                return;

            LastMove = LastAttack = Common.Clock + Common.MS_PER_SECOND + 3;

            X = (ushort)loc.X;
            Y = (ushort)loc.Y;
            Life = MaximumLife;
            Map.Insert(this);
            UpdateSurroundings(true);

            foreach (var id in VisibleObjects.Keys)
                if (id > 1000000)
                { IsActive = true; break; }
            if (IsActive)
                Owner.IsActive = true;

            SendToScreen(GeneralActionPacket.Create(UID, DataAction.SpawnEffect, X, Y));
            Common.MapService.AddFlag(MapID, X, Y, TinyMap.TileFlag.Monster);
            Owner.AliveMembers.TryAdd(UID, this);
            if (!Alive)
                Console.WriteLine("Revived a monster that is still dead! {0}", UID);
        }

        public void Faded()
        {
            Monster m;
            Owner.DeadMembers.Enqueue(this);
            Owner.DyingMembers.TryRemove(UID, out m);
            Common.MapService.RemoveFlag(MapID, X, Y, TinyMap.TileFlag.Monster);
            var packet = GeneralActionPacket.Create(UID, DataAction.RemoveEntity, 0, 0);
            foreach (var p in Map.QueryPlayers(this))
            {
                if (p.VisibleObjects.ContainsKey(UID))
                { uint x; p.Send(packet); p.VisibleObjects.TryRemove(UID, out x); }
                if (VisibleObjects.ContainsKey(p.UID))
                { uint x; VisibleObjects.TryRemove(p.UID, out x); }
            }
        }

        public override void Kill(uint _dieType, uint _attacker)
        {
            TargetID = 0;
            Mode = MonsterMode.Dormancy;
            DiedAt = Common.Clock;
            Monster x;
            Owner.AliveMembers.TryRemove(UID, out x);
            Owner.DyingMembers.TryAdd(UID, this);
            SpawnPacket.StatusEffects = (ClientEffect)((ulong)ClientEffect.Ghost + (ulong)ClientEffect.Dead + (ulong)ClientEffect.Fade);
            SendToScreen(SpawnPacket);
            Map.Remove(this, false);
            
            if (x.BaseMonster.Name == "IcySerpent")
            {
                if (Common.PercentSuccess(Constants.TOKEN_DROP_RATE))
                {
                    if (Common.PercentSuccess(50))
                    {
                        DropItemByID(Constants.LANTERNTOKEN_ID, _attacker);
                    }
                    else
                    {
                        DropItemByID(Constants.GALAXYTOKEN_ID, _attacker);
                    }
                }
                else
                {
                    DropItemByID(Constants.DRAGONBALL_ID, _attacker);
                }
                
            }
            else
            {
                GenerateDrops(_attacker);
            }
                
            

            var killer = PlayerManager.GetUser(_attacker);
            if (killer != null && killer.Team != null)
            {
                foreach (var p in killer.Team.Members)
                    if (p == killer || !p.Alive || !p.VisibleObjects.ContainsKey(killer.UID))
                        continue;
                    else
                    {
                        var bonusexp = (uint)(Redux.Common.MulDiv(BaseMonster.Life, 5, 100));
                        if (p.Spouse == killer.Spouse)
                            bonusexp *= 2;
                        p.GainExperience(bonusexp);
                        p.SendMessage("Congratulations you have gained " + bonusexp * Constants.EXP_RATE + " team experience!", ChatType.System);                         
                    }
            }
            base.Kill(_dieType, _attacker);
        }

        private void GenerateDrops(uint _killer)
        {
            #region Drop Rules
            var dropCount = 0;
            foreach (var rule in Owner.DropRules)
            {
                while (Common.PercentSuccess(rule.RuleChance) && dropCount < rule.RuleAmount)
                {
                    dropCount++;
                    switch (rule.RuleType)
                    {
                        case DropRuleType.GroundItem:
                            {
                                DropItemByID(rule.RuleValue, _killer);
                                break;
                            }
                        case DropRuleType.InventoryCP:
                            {
                                var killer = Map.Search<Player>(_killer);
                                if (killer != null)
                                {
                                    killer.CP += rule.RuleValue;
                                    killer.SendMessage("You've received a bonus " + rule.RuleValue + " cp for killing a(n) " + BaseMonster.Name + "!", ChatType.System);
                                }
                                break;
                            }
                        case DropRuleType.InventoryGold:
                            {
                                var killer = Map.Search<Player>(_killer);
                                if (killer != null)
                                {
                                    killer.Money += rule.RuleValue;
                                    killer.SendMessage("You've received a bonus " + rule.RuleValue + " silver for killing a(n) " + BaseMonster.Name + "!", ChatType.System);
                                }
                                break;
                            }
                        case DropRuleType.InventoryItem:
                            {
                                var killer = Map.Search<Player>(_killer);
                                if (killer != null)
                                {
                                    var item = killer.CreateItem(rule.RuleValue);
                                    if (item != null && item.BaseItem != null)
                                        killer.SendMessage("You've received a " + item.BaseItem.Name + " for killing a(n) " + BaseMonster.Name + "!", ChatType.System);
                                }
                                break;
                            }
                    }
                }
            }
            #endregion

            #region Drop Rares (DRAGONBALL & METEOR)
            var killerz = Map.Search<Player>(_killer);
            if (killerz == null)
                return;
            var client = PlayerManager.GetUser(killerz.UID);

            if (Common.PercentSuccess(Constants.CHANCE_METEOR)) {

                if (client.Account.VipLevel > PlayerVipLevel.Level3)// If account is atleast VIP 3, auto pick-up METEOR drop.
                {
                    
                    if (killerz != null)
                    {
                        if (killerz.Inventory.Count >= 40)
                        {
                            killerz.SendMessage("Warning: Inventory is full!");
                            killerz.SendMessage("A Meteor was dropped by a " + BaseMonster.Name + " monster killed by " + killerz.Name, ChatType.System);
                            DropItemByID(Constants.METEOR_ID, _killer);
                        }
                        else
                        {
                            var item = killerz.CreateItem(Constants.METEOR_ID);
                            if (item != null && item.BaseItem != null)
                                killerz.SendMessage("You've received a " + item.BaseItem.Name + " for killing a(n) " + BaseMonster.Name + "!", ChatType.System);
                        }
                        
                    }
                }
                else
                {
                    killerz.SendMessage("A Meteor was dropped by a " + BaseMonster.Name + " monster killed by " + killerz.Name, ChatType.System);
                    DropItemByID(Constants.METEOR_ID, _killer);
                }
            }

            if (Common.PercentSuccess(Constants.CHANCE_DRAGONBALL)) {

                if (client.Account.VipLevel > PlayerVipLevel.Level5)//if account is atleast VIP5 auto pick-up Dragonball Drop
                {

                    if (killerz != null)
                    {
                        if (killerz.Inventory.Count >= 40)
                        {
                            killerz.SendMessage("Warning: Inventory is full!");
                            killerz.SendMessage("A Dragonball was dropped by a " + BaseMonster.Name + " monster killed by " + killerz.Name, ChatType.System);
                            DropItemByID(Constants.DRAGONBALL_ID, _killer);
                        }
                        else
                        {
                            var item = killerz.CreateItem(Constants.DRAGONBALL_ID);
                            if (item != null && item.BaseItem != null)
                                killerz.SendMessage("You've received a " + item.BaseItem.Name + " for killing a(n) " + BaseMonster.Name + "!", ChatType.System);
                        }

                    }
                }
                else
                {
                    killerz.SendMessage("A Dragonball was dropped by a " + BaseMonster.Name + " monster killed by " + killerz.Name, ChatType.System);
                    DropItemByID(Constants.DRAGONBALL_ID, _killer);
                }

            }
            #endregion

            #region Drop Gear
            while (Common.PercentSuccess(10) && dropCount < 4)
                DropItemByID(DropManager.GenerateDropID(BaseMonster.Level), _killer);
            #endregion

            #region Drop Money
            while (Common.PercentSuccess(20) && dropCount < 4)
            {
                var killer = Map.Search<Player>(_killer);
                uint value = (uint)Common.Random.Next(BaseMonster.Level, (int)(BaseMonster.Level * 15 * Constants.GOLD_RATE));
                uint itemID = 1090000;
                if (value > 10000)
                    itemID = 1091020;
                else if (value > 5000)
                    itemID = 1091010;
                else if (value > 1000)
                    itemID = 1091000;
                else if (value > 100)
                    itemID = 1090020;
                else if (value > 50)
                    itemID = 1090010;
                
                
                
                if (killer.Account.VipLevel == PlayerVipLevel.Level7) //if VIP 7, auto loot money + advantage drop rate.
                {
                    int add_rate = (int)(BaseMonster.Level * 15);
                    value += (uint)add_rate;
                    killer.Money += value;
                }
                else if (killer.Account.VipLevel > PlayerVipLevel.Level0) //if atleast VIP 1 then auto loot money.
                    killer.Money += value;
                else
                {
                    DropItemByID(itemID, _killer, CurrencyType.Silver, value);
                }

                dropCount++;
            }
            #endregion
            
            //REMOVE DROP POTIONS
            #region Drop Potions
           /* while (Common.PercentSuccess(Constants.CHANCE_POTION) && dropCount < 4)
            {
                var itemID = BaseMonster.DropHP;
                if (Common.Random.Next(100) > 60)
                    itemID = BaseMonster.DropMP;
                DropItemByID(itemID, _killer);
                dropCount++;
            }*/
            #endregion
        }

        private void DropItemByID(uint _id, uint _killer, CurrencyType _currency = CurrencyType.None, uint _value = 0)
        {
            var killer = Map.Search<Player>(_killer);
            var itemInfo = Database.ServerDatabase.Context.ItemInformation.GetById(_id);
            if (itemInfo != null)
            {
                var loc = GetValidLocation();
                if (Map.IsValidItemLocation(loc))
                {
                    var coItem = new Structures.ConquerItem((uint)Map.ItemCounter.Counter, itemInfo);

                    //AUTOMATICALLY DROP DB AND METEOR TO GROUND
                    if (_id == Constants.METEOR_ID || _id == Constants.DRAGONBALL_ID || _id == Constants.LANTERNTOKEN_ID || _id == Constants.GALAXYTOKEN_ID)
                    {
                        var gi = new GroundItem(coItem, coItem.UniqueID, loc, Map, _killer, _currency, _value);
                        gi.AddToMap();
                        return;
                    }
                    
                    if ((coItem.EquipmentSort == 1 || coItem.EquipmentSort == 3 || coItem.EquipmentSort == 4) && coItem.BaseItem.TypeDesc != "Earring")
                        coItem.Color = (byte)Common.Random.Next(3, 7);
                    if (coItem.EquipmentSort > 0 && coItem.EquipmentQuality > 3)
                        coItem.Durability = (ushort)Common.Random.Next(coItem.MaximumDurability);
                    else
                        coItem.Color = 3;

                    if (Common.PercentSuccess(Constants.CHANCE_PLUS))
                        coItem.Plus = 1;

                    if (coItem.IsWeapon)//Add socket chance if Weapon drop.
                    {
                        if (Common.PercentSuccess(Constants.SOCKET_DROP))
                        {
                            coItem.Gem1 = 255;

                            if (Common.PercentSuccess(Constants.SOCKET_DROP))
                            {
                                coItem.Gem2 = 255;
                            }
                        }
                    }

                    //if coItem is not Meteor or DB
                    
                    if (Common.PercentSuccess(Constants.CHANCE_NEGATIVE_1))
                    {
                        coItem.Bless = 1;
                    }
                    if (Common.PercentSuccess(Constants.CHANCE_NEGATIVE_3))
                    {
                        coItem.Bless = 3;
                    }
                    if (Common.PercentSuccess(Constants.CHANCE_NEGATIVE_5))
                    {
                        coItem.Bless = 5;
                    }
                     

                    var groundItem = new GroundItem(coItem, coItem.UniqueID, loc, Map, _killer, _currency, _value);


                    #region VIP_AUTO_LOOT_PLUS_BLESS
                    if (killer.Account.VipLevel >= PlayerVipLevel.Level5 && coItem.plus > 0) // If account is atleast VIP5, can auto loot +1 drop items
                    {
                        #region VIP6_AUTO_PICK_BLESS_ITEM
                        if (killer.Account.VipLevel >= PlayerVipLevel.Level6 && coItem.Bless > 0)//if account is atleast VIP6 autoLoot negative or bless items
                        {

                            if (killer != null)
                            {
                                if (killer.Inventory.Count >= 40)
                                {
                                    killer.SendMessage("Warning: Inventory is full!");
                                    //DROP ITEM TO THE GROUND
                                    Managers.PlayerManager.SendToServer(new TalkPacket(ChatType.System, "A bless " + coItem.Bless + " " + coItem.BaseItem.Name + " has drop by a " + BaseMonster.Name + " killed by " + killer.Name));
                                    groundItem.AddToMap();
                                    return;
                                }
                                else
                                {
                                    //ADD NEGATIVE ITEM TO INVENTORY
                                    var item = killer.CreateItem(coItem.StaticID, coItem.plus, coItem.Bless, 0, coItem.Gem1, coItem.Gem2);
                                    Add_Save_Drop_Item(killer, item, "bless");
                                    return;
                                }

                            }
                            else //if killer is null
                            {
                                return;
                            }
                        }
                        #endregion

                        #region VIP5_AUTO_PICK_PLUS_ITEM
                        if (killer != null)
                        {
                            if (killer.Inventory.Count >= 40)
                            {
                                killer.SendMessage("Warning: Inventory is full!");
                                //DROP ITEM TO THE GROUND
                                killer.SendMessage("A plus " + coItem.plus + " " + coItem.BaseItem.Name + " has drop by a " + BaseMonster.Name + " killed by " + killer.Name, ChatType.System);
                                groundItem.AddToMap();
                                return;
                            }
                            else
                            {
                                //ADD PLUS ITEM TO INVENTORY
                                var item = killer.CreateItem(coItem.StaticID, coItem.plus, coItem.Bless, 0, coItem.Gem1, coItem.Gem2);
                                Add_Save_Drop_Item(killer, item, "plus");
                                return;
                            }

                        }
                        else //if killer is null
                        {
                            return;
                        }
                        #endregion

                    }

                    #region VIP6_AUTO_PICK_BLESS_ITEM
                    if (killer.Account.VipLevel >= PlayerVipLevel.Level6 && coItem.Bless > 0)//if account is atleast VIP6 autoLoot negative or bless items
                    {

                        if (killer != null)
                        {
                            if (killer.Inventory.Count >= 40)
                            {
                                killer.SendMessage("Warning: Inventory is full!");
                                //DROP ITEM TO THE GROUND
                                Managers.PlayerManager.SendToServer(new TalkPacket(ChatType.System, "A bless " + coItem.Bless + " " + coItem.BaseItem.Name + " has drop by a " + BaseMonster.Name + " killed by " + killer.Name));
                                groundItem.AddToMap();
                                return;
                            }
                            else
                            {
                                //ADD BLESS ITEM TO INVENTORY
                                var item = killer.CreateItem(coItem.StaticID,coItem.plus,coItem.Bless,0,coItem.Gem1,coItem.Gem2);
                                Add_Save_Drop_Item(killer, item, "bless");
                                return;
                            }

                        }
                        else //if killer is null
                        {
                            return;
                        }
                    }
                    #endregion
                    #endregion

                    groundItem.AddToMap();

                }
            }
        }

        public void Add_Save_Drop_Item(Player killer,ConquerItem item, String attr)
        {
            String str_attr = "";
            if (attr == "plus")
                str_attr = " plus " + item.plus + " ";
            else if(attr == "bless")
                str_attr = " bless " + item.Bless + " ";


            if (item != null && item.BaseItem != null)
            {
                item.SetOwner(killer);
                item.Save();
                killer.SendMessage("You've received a" + str_attr + item.BaseItem.Name + " for killing a(n) " + BaseMonster.Name + "!", ChatType.System);
            }
        }


        public Point GetValidLocation()
        {
            Point loc = Location;
            for (var i = 0; i < 9; i++)
            {
                if (Map.IsValidItemLocation(loc))
                {
                    loc.X = Location.X + Common.DeltaX[i];
                    loc.Y = Location.Y + Common.DeltaY[i];
                }
            }
            return loc;
        }
        public void Monster_Timer()
        {
            //Perform all monster logic here
            switch (Mode)
            {
                case MonsterMode.Attack:
                    {
                        var target = Map.Search<Entity>(TargetID);
                        if (target == null || !target.Alive || target.HasEffect(ClientEffect.Fly))
                        { Mode = MonsterMode.Idle; return; }
                        var dist = Calculations.GetDistance(Location, target.Location);
                        if (dist > BaseMonster.ViewRange)
                            Mode = MonsterMode.Idle;
                        else if (dist > BaseMonster.AttackRange)
                            Mode = MonsterMode.Walk;
                        else if (Common.Clock - LastAttack > BaseMonster.AttackSpeed)
                        {
                            LastAttack = Common.Clock;
                            CombatEngine.ProcessInteractionPacket(InteractPacket.Create(UID, TargetID, target.X, target.Y, InteractAction.Attack, 0));
                        }

                        break;
                    }
                case MonsterMode.Idle:
                    {
                        var d1 = BaseMonster.ViewRange;
                        foreach (var t in Map.QueryScreen<Entity>(this))
                        {
                            if (!CombatEngine.IsValidTarget(t) || (BaseMonster.Mesh != 900 && t.HasEffect(ClientEffect.Fly)) || t.HasStatus(Enum.ClientStatus.ReviveProtection))
                                continue;
                            var d2 = Calculations.GetDistance(Location, t.Location);
                            if (d2 < d1)
                            {
                                d1 = d2;
                                TargetID = t.UID;
                                if (d2 <= BaseMonster.AttackRange)
                                    break;
                            }
                        }
                        var Target = Map.Search<Entity>(TargetID);
                        if (Target != null)
                        {
                            var dist = Calculations.GetDistance(Location, Target.Location);
                            if (dist < BaseMonster.AttackRange)
                                Mode = MonsterMode.Attack;
                            else if (dist < BaseMonster.ViewRange)
                                Mode = MonsterMode.Walk;
                        }

                        else if (BaseMonster.Mesh != 900 && Common.Clock - LastMove > BaseMonster.MoveSpeed * 4)
                        {
                            var dir = (byte)Common.Random.Next(9);
                            Point tryMove = new Point(Location.X + Common.DeltaX[dir], Location.Y + Common.DeltaY[dir]);
                            if (Common.MapService.Valid(MapID, (ushort)tryMove.X, (ushort)tryMove.Y) && !Common.MapService.HasFlag(MapID, (ushort)tryMove.X, (ushort)tryMove.Y, TinyMap.TileFlag.Monster))
                            {
                                //Send to screen new walk packet
                                SendToScreen(WalkPacket.Create(UID, dir));
                                Common.MapService.RemoveFlag(MapID, X, Y, TinyMap.TileFlag.Monster);
                                X = (ushort)tryMove.X;
                                Y = (ushort)tryMove.Y;
                                Direction = dir;
                                Common.MapService.AddFlag(MapID, X, Y, TinyMap.TileFlag.Monster);
                                LastMove = Common.Clock;
                                UpdateSurroundings();

                            }
                        }
                        break;
                    }
                case MonsterMode.Walk:
                    {
                        var target = Map.Search<Entity>(TargetID);
                        if (target == null || !target.Alive || (BaseMonster.Mesh != 900 && target.HasEffect(ClientEffect.Fly) || target.HasStatus(Enum.ClientStatus.ReviveProtection)))
                            Mode = MonsterMode.Idle;
                        else if (Common.Clock - LastMove > BaseMonster.MoveSpeed)
                        {
                            var dist = Calculations.GetDistance(Location, target.Location);
                            if (dist > BaseMonster.ViewRange)
                                Mode = MonsterMode.Idle;
                            else if (dist > BaseMonster.AttackRange)
                            {
                                var dir = Calculations.GetDirection(Location, target.Location);
                                Point tryMove = new Point(Location.X + Common.DeltaX[dir], Location.Y + Common.DeltaY[dir]);
                                if (Common.MapService.Valid(MapID, (ushort)tryMove.X, (ushort)tryMove.Y) && !Common.MapService.HasFlag(MapID, (ushort)tryMove.X, (ushort)tryMove.Y, TinyMap.TileFlag.Monster))
                                {
                                    //Send to screen new walk packet
                                    SendToScreen(WalkPacket.Create(UID, dir));
                                    Common.MapService.RemoveFlag(MapID, X, Y, TinyMap.TileFlag.Monster);
                                    X = (ushort)tryMove.X;
                                    Y = (ushort)tryMove.Y;
                                    Direction = dir;
                                    Common.MapService.AddFlag(MapID, X, Y, TinyMap.TileFlag.Monster);
                                    LastMove = Common.Clock;
                                }
                                else if (Common.Clock - LastMove > BaseMonster.MoveSpeed * 5)
                                    Mode = MonsterMode.Idle;
                            }
                            else
                            {
                                LastAttack = Common.Clock - AttackSpeed + 100;
                                Mode = MonsterMode.Attack;
                            }
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Basic override. Not used as monsters cannot send or receive packets to themselves.
        /// </summary>
        /// <param name="_packet"></param>
        public override void Send(byte[] _packet)
        {
        }

        /// <summary>
        /// Send packet to all players near me. T
        /// </summary>
        /// <param name="_packet">Packet to send</param>
        /// <param name="_self">Not used by Monster</param>
        public override void SendToScreen(byte[] _packet, bool _self = false)
        {
            foreach (var id in VisibleObjects.Keys)
            {
                var p = Map.Search<Player>(id);
                if (p != null)
                    p.Send(_packet.UnsafeClone());
            }
        }
    }
}
