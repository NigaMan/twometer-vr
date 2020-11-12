﻿using System;
using Emgu.CV;
using Emgu.CV.CvEnum;
using TVR.Service.Core.Model.Config;

namespace TVR.Service.Core.Video
{
    public class Camera : IDisposable
    {
        private readonly VideoCapture videoCapture;
        private readonly Calibration calibration;

        public CameraInfo CameraInfo { get; }

        public Mat Frame { get; } = new Mat();

        public Mat HsvFrame { get; } = new Mat();

        public double FrameBrightness { get; private set; } = 0.0f;

        public bool Calibrated => calibration.IsCalibrated;

        public double Exposure
        {
            set
            {
                videoCapture.SetCaptureProperty(CapProp.Exposure, value);
            }
            get
            {
                return videoCapture.GetCaptureProperty(CapProp.Exposure);
            }
        }

        public Camera(CameraInfo cameraInfo)
        {
            CameraInfo = cameraInfo;
            calibration = new Calibration(this);
            videoCapture = new VideoCapture(cameraInfo.Index, VideoCapture.API.DShow);
            videoCapture.SetCaptureProperty(CapProp.FrameWidth, cameraInfo.Profile.CameraParameters.FrameWidth);
            videoCapture.SetCaptureProperty(CapProp.FrameHeight, cameraInfo.Profile.CameraParameters.FrameHeight);
            videoCapture.SetCaptureProperty(CapProp.Exposure, 0);
            videoCapture.SetCaptureProperty(CapProp.AutoExposure, 0);
        }

        public bool Update()
        {
            if (videoCapture.Grab())
            {
                videoCapture.Retrieve(Frame);

                ImageProcessing.BgrToHsv(Frame, HsvFrame);
                FrameBrightness = ImageProcessing.GetBrightness(HsvFrame);

                if (!calibration.IsCalibrated)
                    calibration.Update(FrameBrightness);
                else
                    return true;
            }
            return false;
        }

        public void Dispose()
        {
            videoCapture.Dispose();
        }
    }
}