﻿using System;
using System.Threading;

namespace TVR.Service.Common
{
    public class TVRHost
    {
        private ServiceContext serviceContext;

        private Thread updateThread;
        private Thread broadcastThread;

        private volatile bool running = true;

        private int updateDelay;

        public void Start()
        {
            serviceContext = new ServiceContext("tvrconfig.json");

            (updateThread = new Thread(UpdateLoop)).Start();
            (broadcastThread = new Thread(BroadcastLoop)).Start();

            updateDelay = (int)(1000.0f / serviceContext.Config.Tracker.UpdateRate);
        }

        public void Stop()
        {
            running = false;
            updateThread?.Join();
            broadcastThread?.Join();
        }

        private void UpdateLoop()
        {
            while (running)
            {
                serviceContext.Update();
            }
        }

        private void BroadcastLoop()
        {
            while (running)
            {
                var start = DateTime.Now;
                serviceContext.Broadcast();
                var broadcastDuration = (int)(DateTime.Now - start).TotalMilliseconds;

                var timeout = updateDelay - broadcastDuration;
                if (timeout > 0)
                    Thread.Sleep(timeout);
            }
        }

    }
}
