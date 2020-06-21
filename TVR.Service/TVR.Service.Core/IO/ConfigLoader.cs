﻿using System.IO;
using TVR.Service.Core.Model.Config;

namespace TVR.Service.Core.IO
{
    public class ConfigLoader
    {
        public static UserConfig LoadUserConfig()
        {
            var configFile = FileManager.Instance.ConfigFile.FullName;

            if (!File.Exists(configFile))
                throw new FileNotFoundException($"Cannot load user config because {configFile} does not exist.");

            var data = File.ReadAllText(configFile);
            var deserializer = new YamlDotNet.Serialization.Deserializer();
            var config = deserializer.Deserialize<UserConfig>(data);
            config.CameraInfo.Profile = CameraLoader.LoadCameraProfile(config.CameraInfo.ProfileName);
            return config;
        }
    }
}
