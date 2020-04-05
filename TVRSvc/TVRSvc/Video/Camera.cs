﻿using Emgu.CV;
using System;

namespace TVRSvc.Video
{
    public class Camera : IDisposable
    {
        private readonly VideoCapture videoCapture;

        public float Exposure
        {
            set
            {
                videoCapture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Exposure, value);
            }
        }

        public Camera()
        {
            videoCapture = new VideoCapture();
            videoCapture.FlipHorizontal = true;
            videoCapture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.AutoExposure, 0);
        }

        public Mat QueryFrame()
        {
            return videoCapture.QueryFrame();
        }

        public void Dispose()
        {
            videoCapture.Dispose();
        }
    }
}
