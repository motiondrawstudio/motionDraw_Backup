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
using System.Windows.Shapes;
using SharpGL;
using SharpGL.SceneGraph;
using Microsoft.Win32;
using Microsoft.Kinect;

namespace MotionDrawKxt
{


    /// <summary>
    /// Interaction logic for ControlBox.xaml
    /// </summary>
    public partial class ControlBox : Window
    {

        
        SaveFileDialog dlg = new SaveFileDialog();
        MainWindow mainwindow;


        public ControlBox(MainWindow main)
        {
            InitializeComponent();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".mdd"; // Motion Draw Data
            dlg.Filter = "MotionDraw Data file |*.mdd"; // Filter files by extension 
           
            dlg.OverwritePrompt = true;

            //reference to main window
            mainwindow = main;
        
    
            /* Load configuration file: TODO */

            bool noconfig = true;


            //if no configuration file is defined
            if (noconfig)
            {

                    for (int skel = 0; skel < 2; skel++)
                    {


                        for (int i = 0; i < 20; i++)
                        {
                            main.jointPropertiesv[skel, i].joint = mainwindow.jointOrder[i];
                            main.jointPropertiesv[skel, i].max_points = 40;
                            main.jointPropertiesv[skel, i].size = 1.0f;
                            main.jointPropertiesv[skel, i].dots = new SkeletonPoint[MainWindow.MAX_POINTS];
                            main.jointPropertiesv[skel, i].numDots = 0;
                            main.jointPropertiesv[skel, i].firstDot = -1;
                            if (i == 8 || i == 9)
                            {
                                main.jointPropertiesv[skel, i].visible = true;
                                if (i == 8)
                                {
                                    main.jointPropertiesv[skel, i].red = 255;
                                    main.jointPropertiesv[skel, i].green = 0;
                                    main.jointPropertiesv[skel, i].blue = 0;
                                }
                                else
                                {
                                    main.jointPropertiesv[skel, i].red = 0;
                                    main.jointPropertiesv[skel, i].green = 255;
                                    main.jointPropertiesv[skel, i].blue = 0;
                                }
                            }
                            else
                            {
                                main.jointPropertiesv[skel, i].visible = false;
                                main.jointPropertiesv[skel, i].red = 255;
                                main.jointPropertiesv[skel, i].green = 255;
                                main.jointPropertiesv[skel, i].blue = 255;
                            }
                    }
                }
            }

        }
        

        //onLoad window
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //refresh kinect list
            btnRefresh_Click(this, null);

            //auto-connect to first kinect available
            btnConnect_Click(this, null);

            //create conductor server
            conductorServer = new PipeServer(this);

            Dispatcher.BeginInvoke((Action)(() =>
            {
                conductorServer.startServer();
            }));
            

        }


        /* =======================================================================================
         * 
         *                                       Recording
         *
         * =======================================================================================*/

        /*
         Load file button
         */
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                bool cont = false;

                try
                {
                    mainwindow.file = new System.IO.StreamWriter(dlg.FileName);
                    cont = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error opening file " + dlg.FileName + " for writing:\n" + ex.Message, "MotionDraw Kxt Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    cont = false;
                    mainwindow.file = null;
                }

                if (cont)
                {
                    mainwindow.filename = dlg.FileName;
                    btnStartRecording.IsEnabled = true;
                    labelOutputfileName.Content = dlg.FileName;
                    //button2.IsEnabled = false;
                }
            }
        }

        //START RECORDING BUTTON CLICK
        private void btnStartRecording_Click(object sender, RoutedEventArgs e)
        {
            if (mainwindow.file != null)
            {
                mainwindow.recording = true;
                button2.IsEnabled = false;
                btnStartRecording.IsEnabled = false;
                btnStopRecording.IsEnabled = true;
            }
        }

        //STOP RECORDING BUTTON CLICK
        private void btnStopRecording_Click(object sender, RoutedEventArgs e)
        {
            mainwindow.recording = false;
            mainwindow.file.Close();
            mainwindow.file = null;
            button2.IsEnabled = true;
            btnStopRecording.IsEnabled = false;
            labelOutputfileName.Content = "None";
        }

        /* =======================================================================================
         * 
         *                                       Closing
         *
         * =======================================================================================*/

        //close button
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show("Do you really want to close MotionDraw Kxt?", "MotionDraw Kxt", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                e.Cancel = true;
            else
            {
                if (mainwindow.file != null)
                    mainwindow.file.Close();
                mainwindow.Close();
                
            }

        }




        /* =======================================================================================
         * 
         *                                      Selecting Kinect
         *
         * =======================================================================================*/

        private List<KinectSensor> kinects = new List<KinectSensor>();
        private int target = -1;
        private bool kxtConnected = false;

        /*
            Update kinect list
         */
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {

            kinects.Clear();
            kinectsList.Items.Clear();
            int count = 0;
            target = -1;
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {

                kinectsList.Items.Add(potentialSensor.UniqueKinectId + " -> " +potentialSensor.Status.ToString());
                kinects.Add(potentialSensor);

                /*
                    Fill target with the first connected kinnect
                 */
                if (target == -1)
                {
                    if (potentialSensor.Status == KinectStatus.Connected)
                        target = count;
                }
                ++count;

                kinectsList.SelectedIndex = target;
            }

           

        }

        /*
            Connect to a kinect device
         */
        private void connectKinect(int index)
        {
            KinectSensor k = null;
            try
            {
                k = kinects.ElementAt(index);
            }
            catch (Exception)
            {

            }


            if (k != null)
            {

                if (k.Status == KinectStatus.Connected)
                {
                    try
                    {
                        mainwindow.sensor = k;
                        mainwindow.sensor.SkeletonStream.Enable();
                        mainwindow.sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);

                        // Add an event handler to be called whenever there is new color frame data
                        mainwindow.sensor.SkeletonFrameReady += mainwindow.SensorSkeletonFrameReady;
                        mainwindow.sensor.DepthFrameReady += mainwindow.DepthFrameReady;


                        //try to start
                        k.Start();

                        //if possible so change status to connected
                        kxtConnected = true;

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Exception Raised while starting kinect device:\n" + ex.Message, "MotionDraw Kxt", MessageBoxButton.OK, MessageBoxImage.Error);
                    }


                }
            }
        }

        //connect button
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            tabControls.IsEnabled = true;
            tabExport.IsEnabled = true;
            tabPlugins.IsEnabled = true;

            kinectsList.IsEnabled = false;
            btnRefresh.IsEnabled = false;
            /* disconnect behaviour */
            if (kxtConnected)
            {
                mainwindow.sensor.SkeletonFrameReady -= mainwindow.SensorSkeletonFrameReady;
                mainwindow.sensor.DepthFrameReady -= mainwindow.DepthFrameReady;
               // mainwindow.sensor.SkeletonStream.Disable();
               // mainwindow.sensor.Stop();

                mainwindow.sensor = null;
                lblKinectName.Content = "None";

                kxtConnected = false;
                //if writing to file then stop
                if (btnStartRecording.IsEnabled)
                    btnStartRecording_Click(btnConnect, null);

                tabControls.IsEnabled = false;
                tabExport.IsEnabled = false;
                tabPlugins.IsEnabled = false;

                kinectsList.IsEnabled = true;
                btnRefresh.IsEnabled = true;

                btnConnect.Content = "Connect";

                //refresh list
                btnRefresh_Click(this, null);


            }
            /* connect behaviour*/
            else
            {
                int idx = kinectsList.SelectedIndex;
                if (idx > -1)
                    connectKinect(idx);
                if (kxtConnected)
                {
                    lblKinectName.Content = mainwindow.sensor.UniqueKinectId;

                    tabControls.IsEnabled = true;
                    tabExport.IsEnabled = true;
                    tabPlugins.IsEnabled = true;

                    kinectsList.IsEnabled = false;
                    btnRefresh.IsEnabled = false;

                    btnConnect.Content = "Disconnect";
                }
            }
        }

        //on select listbox
        private void kinectsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnConnect.IsEnabled = true;
        }

        /* =======================================================================================
         * 
         *                                       Controls Tab
         *
         * =======================================================================================*/

        System.Windows.Forms.ColorDialog jointcolorsel1 = new System.Windows.Forms.ColorDialog();
        System.Windows.Media.Color selColor = new System.Windows.Media.Color();
        bool[] selectedCheckboxes = new bool[20];
        int selectedCheckboxesCount = 0;
        int selectedSkeleton = 0;

        //select color
        private void button3_Click(object sender, RoutedEventArgs e)
        {

            jointcolorsel1.AllowFullOpen = true;


            if (jointcolorsel1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                selColor.A = jointcolorsel1.Color.A;
                selColor.B = jointcolorsel1.Color.B;
                selColor.G = jointcolorsel1.Color.G;
                selColor.R = jointcolorsel1.Color.R;
                rectColor.Fill = new SolidColorBrush(selColor);
                textColor.Text = selColor.ToString();
            }

        }

        //pixel size slider
        private void sliderPointSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                labelPointsize.Content = "" + sliderPointSize.Value.ToString() + " px";
            }));
            
        }

        //buffer size slider
        private void sliderBuffer_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                labelBuffer.Content = "" + sliderBuffer.Value.ToString() + " point(s)";
            }));
        }

        private void setIsEnabledJointProperties(bool status)
        {
            button3.IsEnabled = status;
            checkboxVisible.IsEnabled = status;
            sliderPointSize.IsEnabled = status;
            sliderBuffer.IsEnabled = status;
            btnUpdateJoints.IsEnabled = status;
        }


        //checkbox selection code
        //checkbox attribute 'tag' is joint number (from 0 to 19)
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            string joints = (string)(sender as CheckBox).Tag;

            int joint = Convert.ToInt32(joints);


            if (joint >= 0 && joint < 20)
            {

                if ((sender as CheckBox).IsChecked == true)
                {
                    selectedCheckboxesCount++;
                    selectedCheckboxes[joint] = true;

                    //fill window with this joint properties
                    jProperties jp = mainwindow.jointPropertiesv[selectedSkeleton, joint];

                    //color
                    selColor.A = 255;
                    selColor.R = jp.red;
                    selColor.G = jp.green;
                    selColor.B = jp.blue;
                    rectColor.Fill = new SolidColorBrush(selColor);
                    textColor.Text = selColor.ToString();

                    //point size
                    sliderPointSize.Value = jp.size;

                    //buffer size
                    sliderBuffer.Value = jp.max_points;

                    //visibility
                    checkboxVisible.IsChecked = jp.visible;

                    

                }
                else
                {
                    selectedCheckboxes[joint] = false;
                    selectedCheckboxesCount--;
                }

                labelNumberSelected.Content = selectedCheckboxesCount + " selected";
                if (selectedCheckboxesCount > 0)
                    setIsEnabledJointProperties(true);
                else
                    setIsEnabledJointProperties(false);
            }
        }

        //skeleton1 radiobutton
        private void radioButton1_Checked(object sender, RoutedEventArgs e)
        {
            selectedSkeleton = 0;
        }

        //skeleton2 radiobutton
        private void radioButton2_Checked(object sender, RoutedEventArgs e)
        {
            selectedSkeleton = 1;
        }


        //set  button
        private void btnUpdateJoints_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 20; i++)
            {
                if (selectedCheckboxes[i])
                {
                    //color
                    mainwindow.jointPropertiesv[selectedSkeleton, i].blue = selColor.B;
                    mainwindow.jointPropertiesv[selectedSkeleton, i].red = selColor.R;
                    mainwindow.jointPropertiesv[selectedSkeleton, i].green = selColor.G;

                    //visible
                    if(checkboxVisible.IsChecked == true)
                        mainwindow.jointPropertiesv[selectedSkeleton, i].visible = true;
                    else
                        mainwindow.jointPropertiesv[selectedSkeleton, i].visible = false;
                    

                    //point size
                    mainwindow.jointPropertiesv[selectedSkeleton, i].size = (float) sliderPointSize.Value;

                    //buffer size
                    //(if buffer is changed than cached points are ignored)
                    int newbuffersize = (int)sliderBuffer.Value;
                    int lastbuffersize = mainwindow.jointPropertiesv[selectedSkeleton, i].max_points;
                    
                    //buffer size changed
                    if (newbuffersize != lastbuffersize)
                    {
                        mainwindow.jointPropertiesv[selectedSkeleton, i].max_points = newbuffersize;
                        mainwindow.jointPropertiesv[selectedSkeleton, i].numDots = 0;
                        mainwindow.jointPropertiesv[selectedSkeleton, i].firstDot = -1;
                    }
                    



                }
            }
        }

        /* =======================================================================================
         * 
         *                                       Conductors Tab
         *
         * =======================================================================================*/

        //starts not listening to conductor
        public bool listeningConductor = false;
        public PipeServer conductorServer;

        public void print(string line)
        {
            
            Dispatcher.BeginInvoke((Action)(() =>
            {
                listBoxConductorLog.Items.Add("[" + DateTime.Now + "] - " + line + ((listeningConductor) ? "" : "[Ignored]"));
            }));

            char command = line[0];
            string data = line.Substring(1);

            bool valid = false;
            switch (command)
            {
                case 'x':
                    mainwindow.dir = new Vertex(1,0,0);
                    valid = true;
                break;

                case 'y':

                    mainwindow.dir = new Vertex(0, 1, 0);
                    valid = true;
                break;

                case 'z':

                    mainwindow.dir = new Vertex(0, 0, 1);
                    valid = true;
                   
                    
                break;
            }

            if (valid)
            {
                mainwindow.angle = 0;
                mainwindow.maxAngle = Int16.Parse(data);
                
                mainwindow.direction = (mainwindow.maxAngle < 0)? -1 : 1;

                if (mainwindow.direction < 0)
                {
                    mainwindow.angle = Math.Abs(mainwindow.maxAngle);
                    mainwindow.maxAngle = 0;

                }
                mainwindow.rotate = true;
            }
         
        }

        private void checkBoxConductor_Click(object sender, RoutedEventArgs e)
        {
            if (checkBoxConductor.IsChecked == true)
                listeningConductor = true;
            else
                listeningConductor = false;
        }


        
        /* =======================================================================================
         * 
         *                                       Viewer Contronls SubTab
         *
         * =======================================================================================*/




        private void btn_Click(object sender, RoutedEventArgs e)
        {
            mainwindow.Show();
        }

        private void button5_Click(object sender, RoutedEventArgs e)
        {
            mainwindow.dir = new Vertex(0, 1, 0);
            mainwindow.angle = 45;
            mainwindow.maxAngle = 0;
            mainwindow.direction = -1;
            mainwindow.rotate = true;
        }

        private void button4_Click(object sender, RoutedEventArgs e)
        {
            mainwindow.dir = new Vertex(0, 1, 0);
            mainwindow.angle = 0;
            mainwindow.maxAngle = 45;
            mainwindow.direction = 1;
            mainwindow.rotate = true;
        }

        private void button6_Click(object sender, RoutedEventArgs e)
        {
            mainwindow.dir = new Vertex(1, 0, 0);
            mainwindow.angle = 0;
            mainwindow.maxAngle = 45;
            mainwindow.direction = 1;
            mainwindow.rotate = true;
        }

        private void button7_Click(object sender, RoutedEventArgs e)
        {
            mainwindow.dir = new Vertex(1, 0, 0);
            mainwindow.angle = 45;
            mainwindow.maxAngle = 0;
            mainwindow.direction = -1;
            mainwindow.rotate = true;
        }


    }
}
