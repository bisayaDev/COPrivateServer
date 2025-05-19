using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Redux.Events;
using Redux.Managers;

namespace Redux.Threading
{
    public class WorldThread:ThreadBase 
    {

        private const int TIMER_OFFSET_LIMIT = 10;
        private const int THREAD_SPEED = 1000;
        private const int INTERVAL_EVENT = 60; 
        
        private long _nextTrigger;
        protected override void OnInit()
        {
            _nextTrigger = Common.Clock + THREAD_SPEED;
        }
        protected override bool OnProcess()
        {
            var curr = Common.Clock;
            if (curr >= _nextTrigger)
            {
                _nextTrigger += THREAD_SPEED;

                var offset = (curr - _nextTrigger) / Common.MS_PER_SECOND;
                if (Math.Abs(offset) > TIMER_OFFSET_LIMIT)
                {
                    _nextTrigger = curr + THREAD_SPEED;
                }

                //Run managers
                PlayerManager.PlayerManager_Tick();

                MapManager.MapManager_Tick();

                GuildWar.GuildWar_Tick();
                
                /*if (DateTime.UtcNow.Minute == 45 && DateTime.UtcNow.Second == 00)
                    FreeForAll.StartEvent();
                else if (DateTime.UtcNow.Minute == 46 && DateTime.UtcNow.Second == 00)
                    FreeForAll.Send();
                else if (DateTime.UtcNow.Minute == 55 && DateTime.UtcNow.Second == 00)
                    FreeForAll.EndEvent();*/
            }

            return true;
        }
        protected override void OnDestroy()
        {
        }
    }
}
