﻿using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVRSvc.Tracking
{
    public struct TrackerSettings
    {
        public static TrackerSettings Blue { get; } = new TrackerSettings(new MCvScalar(250, 140, 5), new MCvScalar(255, 255, 255), 100);

        public static TrackerSettings Red { get; } = new TrackerSettings(new MCvScalar(0, 140, 160), new MCvScalar(255, 255, 255), 100);

        public MCvScalar Minimum { get; }

        public MCvScalar Maximum { get; }

        public float CannyThreshold { get; }

        public TrackerSettings(MCvScalar minimum, MCvScalar maximum, float cannyThreshold)
        {
            Minimum = minimum;
            Maximum = maximum;
            CannyThreshold = cannyThreshold;
        }
    }
}
