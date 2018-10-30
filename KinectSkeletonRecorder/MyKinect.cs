using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows.Media;
using System.Windows;
using System.Diagnostics;
using System.IO;

namespace KinectSkeletonRecorder
{
    class MyKinect
    {
        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        public KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        public Dictionary<JointType, Joint> Joints { get; set; }

        public Dictionary<JointType, Point> JointPoints { get; set; }

        private bool _kinectIsReady = false;
        public bool KinectIsReady
        {
            get
            {
                return _kinectIsReady;
            }
            set
            {
                if (_kinectIsReady != value)
                {
                    _kinectIsReady = value;
                }
            }
        }

        private bool record = false;
        public string path = null;
        private FileStream fs;

        public MyKinect()
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            //this.displayWidth = frameDescription.Width;
            //this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            //this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
            //                                              : Properties.Resources.SensorNotAvailableStatusText;
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                KinectIsReady = true;

                foreach (Body body in this.bodies)
                {
                    if (body.IsTracked)
                    {

                        IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                        // convert the joint points to depth (display) space
                        Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                        foreach (JointType jointType in joints.Keys)
                        {
                            // sometimes the depth(Z) of an inferred joint may show as negative
                            // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)

                            CameraSpacePoint position = joints[jointType].Position;
                            //CameraSpacePoint position = mockupJoints[i][jointType].Position;

                            if (position.Z < 0)
                            {
                                position.Z = InferredZPositionClamp;
                            }

                            DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                            jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                        }

                        // Copy data to public attributes  
                        Joints = joints.ToDictionary(k => k.Key, v => v.Value);
                        JointPoints = jointPoints;
                    }
                    if (record && Joints != null && JointPoints != null)
                    {
                        int i = 0, max = Enum.GetValues(typeof(JointType)).Length;
                        foreach (JointType jointType in Enum.GetValues(typeof(JointType)))
                        {
                            //Debug.WriteLine(JointPoints[jointType].X + " " + JointPoints[jointType].Y);
                            if (i < max - 1)
                            {
                                AddText(fs, Joints[jointType].Position.X + " " + Joints[jointType].Position.Y + " " + Joints[jointType].Position.Z + " " + JointPoints[jointType].X + " " + JointPoints[jointType].Y + " ");
                            }
                            else
                            {
                                AddText(fs, Joints[jointType].Position.X + " " + Joints[jointType].Position.Y + " " + Joints[jointType].Position.Z + " " + JointPoints[jointType].X + " " + JointPoints[jointType].Y);
                            }

                            i++;
                        }
                        AddText(fs, "\n");
                    }
                }

            }
        }

        public void StartRecording()
        {
            if (File.Exists(path))
            {
                //File.Delete(path);
            }

            fs = File.Create(path);
            record = true;
        }

        public void StopRecording()
        {
            record = false;
            fs.Close();
        }

        public bool IsRecording()
        {
            return record;
        }

        private static void AddText(FileStream fs, string value)
        {
            if (value == "\n")
            {
                byte[] newline = Encoding.ASCII.GetBytes(Environment.NewLine);
                fs.Write(newline, 0, newline.Length);
            }
            else
            {
                byte[] info = new UTF8Encoding(true).GetBytes(value);
                fs.Write(info, 0, info.Length);
                //byte[] space = Encoding.ASCII.GetBytes(" ");
                //fs.Write(space, 0, space.Length);
            }
        }


    }
}
