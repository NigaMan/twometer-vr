﻿using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVR.Service.Core.Model.Camera
{
    public struct HSVColor
    {
        public double H { get; set; }

        public double S { get; set; }

        public double V { get; set; }

        public MCvScalar ToMCvScalar()
        {
            return new MCvScalar(H, S, V);
        }
    }
}
