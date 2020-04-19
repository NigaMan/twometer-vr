﻿using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVR.Service.Core.Video
{
    public class ImageProcessing
    {

        public static void Erode<TColor, TDepth>(Image<TColor, TDepth> image, int iterations) where TColor : struct, IColor where TDepth : new()
        {
            CvInvoke.Erode(image, image, null, new Point(-1, -1), iterations, BorderType.Constant, CvInvoke.MorphologyDefaultBorderValue);
        }

        public static void Dilate<TColor, TDepth>(Image<TColor, TDepth> image, int iterations) where TColor : struct, IColor where TDepth : new()
        {
            CvInvoke.Dilate(image, image, null, new Point(-1, -1), iterations, BorderType.Constant, CvInvoke.MorphologyDefaultBorderValue);
        }

        public static void ThresholdBinary<TColor, TDepth>(Image<TColor, TDepth> image, TColor threshold, TColor maxValue) where TColor : struct, IColor where TDepth : new()
        {
            CvInvoke.Threshold(image, image, threshold.MCvScalar.V0, maxValue.MCvScalar.V0, ThresholdType.Binary);
        }

        public static void SmoothGaussian<TColor, TDepth>(Image<TColor, TDepth> image, int kernelSize) where TColor : struct, IColor where TDepth : new()
        {
            CvInvoke.GaussianBlur(image, image, new Size(kernelSize, kernelSize), 0, 0);
        }

    }
}