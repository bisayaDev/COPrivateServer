/*
 * User: cookc
 * Date: 7/20/2013
 * Time: 11:42 AM
 */
using System;
using System.Collections.Generic;
using Redux.Game_Server;
using Redux.Packets.Game;
using Redux.Structures;


namespace Redux.Items
{
    /// <summary>
    /// Handles item usage for [722825] LanternToken
    /// </summary>
    public class Item_722825: IItem
	{		
        public override void Run(Player _client, ConquerItem _item)
        {
            Npcs.Manager.ProcessNpc(_client, 1000941, 0);
        }
	}
}
