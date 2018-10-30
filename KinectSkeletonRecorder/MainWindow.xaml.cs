using System;
using System.Windows;
using Microsoft.Kinect;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace KinectSkeletonRecorder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private MyKinect kinect;

        private int records = 0;

        private string _statusText = "Record";
        public string StatusText
        {
            get
            {
                return _statusText;
            }

            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool usingKinectStudio;

        public MainWindow()
        {
            kinect = new MyKinect();
            kinect.kinectSensor.IsAvailableChanged += KinectSensor_IsAvailableChanged;
            DataContext = this;
            InitializeComponent();
            RecordButton.Content = "Record";
        }

        private void KinectSensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            if (usingKinectStudio == false)
            {
                if (kinect.kinectSensor.IsAvailable == false)
                {
                    StatusText = "kinect not avaliable";
                    RecordButton.IsEnabled = false;
                }
                else
                {
                    StatusText = "kinect avaliable";
                    RecordButton.IsEnabled = true;
                }
            } else
            {
                StatusText = null;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // user clicked to start recording
            if (kinect.IsRecording() == false)
            {
                if (TextBoxInput.Text == "")
                {
                    kinect.path = "playbacks/" + "recording_" + records.ToString() + ".txt";
                } else
                {
                    kinect.path = "playbacks/" + TextBoxInput.Text + ".txt";
                }
                kinect.StartRecording();
                StatusText = "Recording...";
                RecordButton.Content = "Stop";
                DisableControls();
            } else
            // user clicked to stop recording
            {
                kinect.StopRecording();
                StatusText = "Recording saved as " +  kinect.path.ToString().Split('/')[1];
                RecordButton.Content = "Record";
                TextBoxInput.Text = null;
                EnableControls();
                records++;
            }
        }

        private void DisableControls() { 
            TextBoxInput.IsEnabled = false;
            CheckBoxUsingKinectStudio.IsEnabled = false;
            //ButtonTextBoxInput.IsEnabled = false;
        }

        private void EnableControls()
        {
            TextBoxInput.IsEnabled = true;
            CheckBoxUsingKinectStudio.IsEnabled = true;
            //ButtonTextBoxInput.IsEnabled = true;
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            usingKinectStudio = true;
            StatusText = null;
            RecordButton.IsEnabled = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            usingKinectStudio = false;

            if (kinect.kinectSensor.IsAvailable == false)
            {
                StatusText = "kinect not avaliable";
                RecordButton.IsEnabled = false;
            }
            else
            {
                StatusText = "kinect avaliable";
                RecordButton.IsEnabled = true;
            }

            RecordButton.IsEnabled = kinect.kinectSensor.IsAvailable;
        }

        private void FileName_TouchEnter(object sender, System.Windows.Input.TouchEventArgs e)
        {
            Debug.WriteLine("hit enter");
        }
    }


}
