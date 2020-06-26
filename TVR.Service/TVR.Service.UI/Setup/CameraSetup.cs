﻿using DirectShowLib;
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Text;
using System.Threading.Tasks;
using TVR.Service.Core.Logging;
using TVR.Service.Core.Model.Camera;
using TVR.Service.Core.Video;

namespace TVR.Service.UI.Setup
{
    public class CameraSetup
    {
        public CameraProfile CameraProfile { get; } = new CameraProfile();

        public VideoCapture VideoCapture { get; private set; }

        public int TimerSeconds { get; private set; } = 30;

        public event EventHandler<StatusMessage> StatusMessageReceived;

        // Setup state
        private CameraDetectionState currentDetectionState = CameraDetectionState.CircleDiameter;
        private SetupState currentState = SetupState.DetectingCalibrationParameters;

        // Capture state
        private readonly DsDevice device;
        private readonly Mat hsvFrame = new Mat();
        private readonly Mat tempFrame = new Mat();
        private Image<Gray, byte> filteredImage;

        // Brightness calibration
        private int warmupFrames = 0;
        private int cooldownFrames = 0;
        private int exposure = 0;
        private int frameTimeout;

        // Utils for brightness
        private double prevBrightness;
        private int stableCounter = 0;

        // Depth calibration
        private double totalCircleDia;
        private double circleDiaSamples;
        private double perceivedCircleSize;

        // Horizontal calibration
        private double circle_x0;
        private int circle0time;
        private double circle_maxX;

        /*
         * After the automatic calibration step the following has to be done:
         * 
         * 
         * This has to explain the following steps to the user while calculating the camera parameters:
         * 
         *  1. Place tracker sphere 1 meter away from the camera
         *  2. Calculate Parameters.FocalLength = (circleDiameter * 1m) / 0.04m
         *  3. Stay at 1 meter away from the camera, go to left side of frame with the ball and move 1 meters to the right
         *     and back multiple times
         *      
         *      Code: Correct the XY coordinates for distance
         *            Take the maximums of each run, average those together
         *            Divide the pixel difference from left to right by two (because 2 meters)
         *            That's the px/m
         *            Save that.
         *            Be happy.
         */

        // Left is red
        // Right is blue


        private enum SetupState
        {
            AwaitingUserInput,
            DetectingCalibrationParameters,
            DetectingCameraParameters,
            Completed
        }

        private enum CameraDetectionState
        {
            CircleDiameter,
            AwaitingZeroPosition,
            PixelsPerMeter
        }

        public enum StatusMessage
        {
            CalibrationParametersDetected,
            CountdownChanged,
            BeginSamplingCircleDiameter,
            BeginSamplingPixelsPerMeter,
            AwaitingZeroPosition,
            Completed
        }

        public CameraSetup(DsDevice device, int deviceIndex)
        {
            this.device = device;
            InitCapture(deviceIndex);
        }

        private void InitCapture(int deviceIndex)
        {
            CameraProfile.CameraParameters = new CameraParameters();
            CameraProfile.CalibrationParameters = new CalibrationParameters();
            CameraProfile.Identifier = BuildIdentifier(device);
            CameraProfile.Model = device.Name;

            VideoCapture = new VideoCapture(deviceIndex, VideoCapture.API.DShow);
            VideoCapture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.AutoExposure, 0);
            VideoCapture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Exposure, 0);
            VideoCapture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth, 1280);
            VideoCapture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight, 720);

            CameraProfile.CameraParameters.FrameWidth = (int)VideoCapture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth);
            CameraProfile.CameraParameters.FrameHeight = (int)VideoCapture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight);

            filteredImage = new Image<Gray, byte>(CameraProfile.CameraParameters.FrameWidth, CameraProfile.CameraParameters.FrameHeight);
        }

        public async void BeginDepthSamplingCountdown()
        {
            while (TimerSeconds > 0)
            {
                TimerSeconds--;
                SendStatusMessage(StatusMessage.CountdownChanged);
                await Task.Delay(1000);
            }

            // After the timer is over, start detection!
            currentState = SetupState.DetectingCameraParameters;
            SendStatusMessage(StatusMessage.BeginSamplingCircleDiameter);
        }

        public IInputArray HandleFrame(Mat frame)
        {
            // Process frame            
            ImageProcessing.BgrToHsv(frame, hsvFrame);
            double frameBrightness = ImageProcessing.GetBrightness(hsvFrame);

            switch (currentState)
            {

                case SetupState.DetectingCalibrationParameters:
                    HandleDetectCalibrationParameters(frameBrightness);
                    return frame;
                case SetupState.AwaitingUserInput:
                    FilterImage(frameBrightness);
                    return filteredImage;
                case SetupState.DetectingCameraParameters:
                    FilterImage(frameBrightness);
                    HandleDetectCameraParameters(frameBrightness);
                    return filteredImage;
            }

            return frame;
        }


        private void FilterImage(double frameBrightness)
        {
            ImageProcessing.ColorFilter(hsvFrame, filteredImage, tempFrame, CameraProfile.ColorProfiles[0], frameBrightness);
            ImageProcessing.SmoothGaussian(filteredImage, 7);
            filteredImage = filteredImage.Flip(Emgu.CV.CvEnum.FlipType.Horizontal);
        }

        private void HandleDetectCameraParameters(double frameBrightness)
        {
            var circles = ImageProcessing.HoughCircles(filteredImage, 125, 1, 3, filteredImage.Width / 2, 15, 75);
            if (circles.Length == 0)
                return;
            var circle = circles[0];
            filteredImage.Draw(circle, new Gray(128), 4);

            switch (currentDetectionState)
            {
                case CameraDetectionState.CircleDiameter:
                    totalCircleDia += Math.Floor(circle.Radius * 2);
                    circleDiaSamples++;

                    if (circleDiaSamples > 10)
                    {
                        perceivedCircleSize = totalCircleDia / circleDiaSamples;
                        CameraProfile.CameraParameters.FocalLength = (perceivedCircleSize * 1) / 0.04; // TODO: Don't hardcode real-world sphere size of 4cm
                        SendStatusMessage(StatusMessage.AwaitingZeroPosition);
                        currentDetectionState = CameraDetectionState.AwaitingZeroPosition;
                    }

                    break;
                case CameraDetectionState.AwaitingZeroPosition:
                    if (circle.Center.X < perceivedCircleSize)
                    {
                        circle0time++;
                        circle_x0 += circle.Center.X;
                    }
                    else
                    {
                        circle_x0 = 0;
                        circle0time = 0;
                    }

                    if (circle0time > 5)
                    {
                        circle_x0 /= circle0time;
                        SendStatusMessage(StatusMessage.BeginSamplingPixelsPerMeter);
                        currentDetectionState = CameraDetectionState.PixelsPerMeter;
                    }

                    break;

                case CameraDetectionState.PixelsPerMeter:
                    double xOffset = circle.Center.X - circle_x0;
                    if (xOffset > circle_maxX)
                    {
                        circle_maxX = xOffset;
                    }
                    else if (xOffset > 0 && circle_maxX - xOffset > Math.Max(50, circle_maxX / 2))
                    {
                        double pxm = circle_maxX - circle_x0;
                        CameraProfile.CameraParameters.PixelsPerMeter = pxm;

                        SendStatusMessage(StatusMessage.Completed);
                        currentState = SetupState.Completed;
                    }
                    break;
            }
        }

        private void HandleDetectCalibrationParameters(double frameBrightness)
        {
            if (CameraProfile.CalibrationParameters.WarmupFrames == 0)
            {
                warmupFrames++;
                LoggerFactory.Current.Log(LogLevel.Debug, "Warming up... " + warmupFrames + "; " + frameBrightness);
                if (CheckStability(frameBrightness, 30, 10.0))
                {
                    CameraProfile.CalibrationParameters.WarmupFrames = warmupFrames - 20;
                    CameraProfile.CalibrationParameters.StableFrames = Math.Min((warmupFrames - 30) / 2 + 1, 4);
                    LoggerFactory.Current.Log(LogLevel.Debug, "Warmup complete! " + CameraProfile.CalibrationParameters.WarmupFrames + "; " + CameraProfile.CalibrationParameters.StableFrames);
                }
            }
            else if (CameraProfile.CalibrationParameters.CooldownFrames == 0)
            {
                if (cooldownFrames == 0)
                {
                    VideoCapture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Exposure, -10);
                }
                cooldownFrames++;

                if (CheckStability(frameBrightness, CameraProfile.CalibrationParameters.StableFrames, 2.0))
                {
                    CameraProfile.CalibrationParameters.CooldownFrames = cooldownFrames;
                }
            }
            else if (CameraProfile.CalibrationParameters.BrightnessThreshold == 0)
            {
                VideoCapture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Exposure, exposure);
                exposure--;
                if ((CheckStability(frameBrightness, CameraProfile.CalibrationParameters.StableFrames, 2.0) && frameBrightness < 20) || exposure < -30)
                {
                    // Frame brightness is now the lowest it will go, offset that
                    CameraProfile.CalibrationParameters.BrightnessThreshold = frameBrightness + 2.5f;
                }
            }
            else if (frameBrightness < CameraProfile.CalibrationParameters.BrightnessThreshold)
            {
                if (frameTimeout <= 0)
                {
                    exposure++;
                    VideoCapture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Exposure, exposure);
                    frameTimeout = CameraProfile.CalibrationParameters.CooldownFrames;
                }
                frameTimeout--;
            }
            else
            {
                OnCalibParametersDetected();
            }
            prevBrightness = frameBrightness;
        }


        private void OnCalibParametersDetected()
        {
            // TODO: Currently loading default color profiles, add an auto-detect for this
            CameraProfile.ColorProfiles = new ColorProfile[2];
            CameraProfile.ColorProfiles[0] = new ColorProfile()
            {
                ColorRanges = new ColorRange[]
                {
                    new ColorRange() { Minimum = new HSVColor(0, 76, 66), Maximum = new HSVColor(70, 255, 255) },
                    new ColorRange() { Minimum = new HSVColor(151, 76, 66), Maximum = new HSVColor(179, 255, 255) }
                }
            };
            CameraProfile.ColorProfiles[1] = new ColorProfile()
            {
                ColorRanges = new ColorRange[]
                {
                    new ColorRange() { Minimum = new HSVColor(58, 125, 110), Maximum = new HSVColor(137, 255, 255) }
                }
            };

            currentState = SetupState.AwaitingUserInput;
            SendStatusMessage(StatusMessage.CalibrationParametersDetected);
        }

        private bool CheckStability(double frameBrightness, int stability, double threshold)
        {
            if (Math.Abs(frameBrightness - prevBrightness) < threshold)
                stableCounter++;
            else stableCounter = 0;
            return stableCounter > stability;
        }


        private string BuildIdentifier(DsDevice device)
        {
            var builder = new StringBuilder();
            foreach (char chr in device.Name)
            {
                if (char.IsLetterOrDigit(chr))
                    builder.Append(char.ToLower(chr));
                else if (char.IsWhiteSpace(chr))
                    builder.Append('-');
            }
            return builder.ToString();
        }

        private void SendStatusMessage(StatusMessage statusMessage)
        {
            StatusMessageReceived?.Invoke(this, statusMessage);
        }
    }
}
