﻿using System;
using TVR.Service.Common;
using TVR.Service.Core.Logging;

namespace TVR.Service.CLI
{
    class Program
    {
        public static void Main(string[] args)
        {
            LoggerFactory.Current.Log(LogLevel.Info, "TwometerVR starting up...");
            var vrHost = new TVRHost();
            vrHost.Start();
            LoggerFactory.Current.Log(LogLevel.Info, "Initialized");

            while (true)
            {
                var cmd = Console.ReadLine();
                if (cmd == "help")
                {
                    Console.WriteLine(" help: Show this help");
                    Console.WriteLine(" stop: Stop the service");
                }
                else if (cmd == "stop")
                {
                    LoggerFactory.Current.Log(LogLevel.Info, "Shutting down...");
                    vrHost.Stop();
                    return;
                }
            }
        }
    }
}
