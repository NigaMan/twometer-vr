﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVRSvc.Core.Config
{
    public class CameraConfig
    {       
        public int CameraIndex { get; set; }

        public int FrameWidth { get; set; }

        public int FrameHeight { get; set; }

        public float FocalLength { get; set; }

        public float SphereSize { get; set; }

        public float PixelsPerMeter { get; set; }

        public float HeightAboveGround { get; set; }

        public int Latency { get; set; }
    }
}
