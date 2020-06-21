﻿namespace TVR.Service.Network.Common
{
    public static class NetworkConfig
    {
        /// <summary>
        /// The port used by the server to communicate with the driver running on
        /// localhost
        /// </summary>
        public const int DriverPort = 12741;

        /// <summary>
        /// The port used by the server to communicate with the wireless handheld
        /// controllers running on an ESP8266
        /// </summary>
        public const int ControllerPort = 12742;

        /// <summary>
        /// The port used by the ESP8266 controllers to discover this server using
        /// UDP broadcasts
        /// </summary>
        public const int DiscoveryPort = 12743;
    }
}