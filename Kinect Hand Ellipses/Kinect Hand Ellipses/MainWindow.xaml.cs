// Create objects that are removed by the ellipse collision.
// This will be a beginning of the engine for my game that "shoots" an 
// enemy when you highlight over it.
// 
// Need to find x and y coordinates of the outline of the ellipse.
// With this, I can create collisions with other objects.
// Need to start with a square to make it easy.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Research.Kinect.Nui;

namespace Kinect_Hand_Ellipses
{
    /// Interaction logic for MainWindow.xaml
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        Runtime nui;
        int totalFrames = 0;
        int lastFrames = 0;
        DateTime lastTime = DateTime.MaxValue;

        // We want to control how depth data gets converted into false-color data
        // for more intuitive visualization, so we keep 32-bit color frame buffer versions of
        // these, to be updated whenever we receive and process a 16-bit frame.
        const int RED_IDX = 2;
        const int GREEN_IDX = 1;
        const int BLUE_IDX = 0;
        byte[] depthFrame32 = new byte[320 * 240 * 4];

        List<JointID> jointList = new List<JointID>();

        //Dictionary<JointID, Brush> jointColors = new Dictionary<JointID, Brush>() { 
        Dictionary<JointID, Brush> jointColors = new Dictionary<JointID, Brush>() { 
            {JointID.HipCenter, new SolidColorBrush(Color.FromRgb(0, 0, 0))},
            {JointID.Spine, new SolidColorBrush(Color.FromRgb(0, 0, 0))},
            {JointID.ShoulderCenter, new SolidColorBrush(Color.FromRgb(0, 0, 0))},
            {JointID.Head, new SolidColorBrush(Color.FromRgb(0, 0, 0))},
            {JointID.ShoulderLeft, new SolidColorBrush(Color.FromRgb(0, 0, 0))},
            {JointID.ElbowLeft, new SolidColorBrush(Color.FromRgb(0, 0, 0))},
            {JointID.WristLeft, new SolidColorBrush(Color.FromRgb(0, 0, 0))},
            {JointID.ShoulderRight, new SolidColorBrush(Color.FromRgb(0, 0, 0))},
            {JointID.ElbowRight, new SolidColorBrush(Color.FromRgb(0, 0, 0))},
            {JointID.WristRight, new SolidColorBrush(Color.FromRgb(0, 0, 0))},
            {JointID.HipLeft, new SolidColorBrush(Color.FromRgb(0, 0, 0))},
            {JointID.KneeLeft, new SolidColorBrush(Color.FromRgb(0, 0, 0))},
            {JointID.AnkleLeft, new SolidColorBrush(Color.FromRgb(0, 0, 0))},
            {JointID.FootLeft, new SolidColorBrush(Color.FromRgb(0, 0, 0))},
            {JointID.HipRight, new SolidColorBrush(Color.FromRgb(0, 0, 0))},
            {JointID.KneeRight, new SolidColorBrush(Color.FromRgb(0, 0, 0))},
            {JointID.AnkleRight, new SolidColorBrush(Color.FromRgb(0, 0, 0))},
            {JointID.FootRight, new SolidColorBrush(Color.FromRgb(0, 0, 0))},

            {JointID.HandRight, new SolidColorBrush(Color.FromRgb(255, 255, 255))},
            {JointID.HandLeft, new SolidColorBrush(Color.FromRgb(255, 255, 255))}
        };
        /* {JointID.HandLeft, new SolidColorBrush(Color.FromRgb(215,  86, 0))},
         {JointID.HandRight, new SolidColorBrush(Color.FromRgb(37,  69, 243))}
     };*/

        private void Window_Loaded(object sender, EventArgs e)
        {
            nui = new Runtime();

            try
            {
                nui.Initialize(RuntimeOptions.UseDepthAndPlayerIndex | RuntimeOptions.UseSkeletalTracking | RuntimeOptions.UseColor);
            }
            catch (InvalidOperationException)
            {
                System.Windows.MessageBox.Show("Runtime initialization failed. Please make sure Kinect device is plugged in.");
                return;
            }


            try
            {
                nui.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution640x480, ImageType.Color);
                nui.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.DepthAndPlayerIndex);
            }
            catch (InvalidOperationException)
            {
                System.Windows.MessageBox.Show("Failed to open stream. Please make sure to specify a supported image type and resolution.");
                return;
            }

            lastTime = DateTime.Now;

            //nui.DepthFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_DepthFrameReady);
            nui.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);
            //nui.VideoFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_ColorFrameReady);
        }

        // Converts a 16-bit grayscale depth frame which includes player indexes into a 32-bit frame
        // that displays different players in different colors
        byte[] convertDepthFrame(byte[] depthFrame16)
        {
            for (int i16 = 0, i32 = 0; i16 < depthFrame16.Length && i32 < depthFrame32.Length; i16 += 2, i32 += 4)
            {
                int player = depthFrame16[i16] & 0x07;
                int realDepth = (depthFrame16[i16 + 1] << 5) | (depthFrame16[i16] >> 3);
                // transform 13-bit depth information into an 8-bit intensity appropriate
                // for display (we disregard information in most significant bit)
                byte intensity = (byte)(255 - (255 * realDepth / 0x0fff));

                depthFrame32[i32 + RED_IDX] = 0;
                depthFrame32[i32 + GREEN_IDX] = 0;
                depthFrame32[i32 + BLUE_IDX] = 0;

                // choose different display colors based on player
                switch (player)
                {
                    case 0:
                        depthFrame32[i32 + RED_IDX] = (byte)(intensity / 2);
                        depthFrame32[i32 + GREEN_IDX] = (byte)(intensity / 2);
                        depthFrame32[i32 + BLUE_IDX] = (byte)(intensity / 2);
                        break;
                    case 1:
                        depthFrame32[i32 + RED_IDX] = intensity;
                        break;
                    case 2:
                        depthFrame32[i32 + GREEN_IDX] = intensity;
                        break;
                    case 3:
                        depthFrame32[i32 + RED_IDX] = (byte)(intensity / 4);
                        depthFrame32[i32 + GREEN_IDX] = (byte)(intensity);
                        depthFrame32[i32 + BLUE_IDX] = (byte)(intensity);
                        break;
                    case 4:
                        depthFrame32[i32 + RED_IDX] = (byte)(intensity);
                        depthFrame32[i32 + GREEN_IDX] = (byte)(intensity);
                        depthFrame32[i32 + BLUE_IDX] = (byte)(intensity / 4);
                        break;
                    case 5:
                        depthFrame32[i32 + RED_IDX] = (byte)(intensity);
                        depthFrame32[i32 + GREEN_IDX] = (byte)(intensity / 4);
                        depthFrame32[i32 + BLUE_IDX] = (byte)(intensity);
                        break;
                    case 6:
                        depthFrame32[i32 + RED_IDX] = (byte)(intensity / 2);
                        depthFrame32[i32 + GREEN_IDX] = (byte)(intensity / 2);
                        depthFrame32[i32 + BLUE_IDX] = (byte)(intensity);
                        break;
                    case 7:
                        depthFrame32[i32 + RED_IDX] = (byte)(255 - intensity);
                        depthFrame32[i32 + GREEN_IDX] = (byte)(255 - intensity);
                        depthFrame32[i32 + BLUE_IDX] = (byte)(255 - intensity);
                        break;
                }
            }
            return depthFrame32;
        }

        private Point getDisplayPosition(Joint joint)
        {
            float depthX, depthY;
            nui.SkeletonEngine.SkeletonToDepthImage(joint.Position, out depthX, out depthY);
            depthX = Math.Max(0, Math.Min(depthX * 320, 320));  //convert to 320, 240 space
            depthY = Math.Max(0, Math.Min(depthY * 240, 240));  //convert to 320, 240 space
            int colorX, colorY;
            ImageViewArea iv = new ImageViewArea();
            // only ImageResolution.Resolution640x480 is supported at this point
            nui.NuiCamera.GetColorPixelCoordinatesFromDepthPixel(ImageResolution.Resolution640x480, iv, (int)depthX, (int)depthY, (short)0, out colorX, out colorY);

            // map back to skeleton.Width & skeleton.Height
            return new Point((int)(skeleton.Width * colorX / 640.0), (int)(skeleton.Height * colorY / 480));
        }

        // create an ellipse on hands
        Ellipse DrawCircle(Microsoft.Research.Kinect.Nui.JointsCollection joints, Brush brush, JointID id)
        {

            Microsoft.Research.Kinect.Nui.Joint joint = joints[id];

            Point jointPos = getDisplayPosition(joint);

            Ellipse ellipse = new Ellipse();
            Canvas.SetTop(ellipse, jointPos.Y - 60);
            Canvas.SetLeft(ellipse, jointPos.X - 60);
            ellipse.Width = 120;
            ellipse.Height = 120;
            ellipse.Stroke = brush;
            ellipse.StrokeThickness = 5;

            return ellipse;
        }

        void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            SkeletonFrame skeletonFrame = e.SkeletonFrame;
            int iSkeleton = 0;
            skeleton.Children.Clear();
            foreach (SkeletonData data in skeletonFrame.Skeletons)
            {
                if (SkeletonTrackingState.Tracked == data.TrackingState)
                {
                    // draw ellipses
                    Brush brushL = new SolidColorBrush(Color.FromRgb(15, 240, 15));
                    Brush brushR = new SolidColorBrush(Color.FromRgb(15, 15, 240));
                    skeleton.Children.Add(DrawCircle(data.Joints, brushR, JointID.HandRight));
                    skeleton.Children.Add(DrawCircle(data.Joints, brushL, JointID.HandLeft));

                    jointList.Add(JointID.HandRight);
                    jointList.Add(JointID.HandLeft);

                    // Draw Joints
                    foreach (Joint joint in data.Joints)
                    {
                        Point jointPos = getDisplayPosition(joint);
                        Line jointLine = new Line();
                        jointLine.X1 = jointPos.X - 3;
                        jointLine.X2 = jointLine.X1 + 6;
                        jointLine.Y1 = jointLine.Y2 = jointPos.Y;
                        jointLine.Stroke = jointColors[joint.ID];
                        jointLine.StrokeThickness = 6;
                        skeleton.Children.Add(jointLine);
                    }
                }
                iSkeleton++;
            } // for each skeleton
        }

        /*void nui_ColorFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            // 32-bit per pixel, RGBA image
            PlanarImage Image = e.ImageFrame.Image;
            video.Source = BitmapSource.Create(
                Image.Width, Image.Height, 96, 96, PixelFormats.Bgr32, null, Image.Bits, Image.Width * Image.BytesPerPixel);
        }*/

        private void Window_Closed(object sender, EventArgs e)
        {
            nui.Uninitialize();
            Environment.Exit(0);
        }

    }
}