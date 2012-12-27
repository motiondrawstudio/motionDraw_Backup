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
using SharpGL;
using SharpGL.SceneGraph;
using SharpGL.SceneGraph.Primitives;
using SharpGL.SceneGraph.Shaders;
using SharpGL.SceneGraph.Effects;
using Microsoft.Kinect;
using System.IO;



namespace MotionDrawKxt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public struct jProperties
    {
        public bool visible;
        public float size;
        public int color;
        public SkeletonPoint[] dots;

        public int numDots;
        public int firstDot;

        public JointType joint;

        //colors
        public byte red, green, blue;
        public int max_points;
    };


    public partial class MainWindow : Window
    {
        ControlBox cb;
        
        //define the max_point limit for each joint
        public const int MAX_POINTS = 100;

        /*
         File
         */
        public bool recording = false;
        public string filename = "";
        public System.IO.StreamWriter file;

        /*
         Kinect
         */

        //Width of left column, room used by parameter settings
        public JointType[] jointOrder = {JointType.Head, JointType.ShoulderCenter, JointType.ShoulderLeft, JointType.ShoulderRight,
                                          JointType.ElbowLeft, JointType.ElbowRight, JointType.WristLeft, JointType.WristRight,
                                          JointType.HandLeft, JointType.HandRight, JointType.Spine, JointType.HipCenter,
                                          JointType.HipLeft, JointType.HipRight, JointType.KneeLeft, JointType.KneeRight,
                                          JointType.AnkleLeft, JointType.AnkleRight, JointType.FootLeft, JointType.FootRight};
        private float[,] colors = { { 0.0f, 0.0f, 1.0f }, { 0.0f, 1.0f, 0.0f }, { 1.0f, 0.0f, 0.0f }, { 1.0f, 1.0f, 1.0f } };




        /*
         OpenGL
         */

        public KinectSensor sensor;
        private ArcBallEffectX abf = new ArcBallEffectX();


        private FragmentShader fs = new FragmentShader();
        private VertexShader vs = new VertexShader();

        private bool arcMove = false;

        //TODO: alow more than one shader program
        Program program = new Program();                    //shader program

        //Window selection parameters (color, selected joint, conductor, etc)

        public bool[] skeletonVisible = new bool[2];
        public jProperties[,] jointPropertiesv = new jProperties[2,20];
        
        public jProperties[] jointProperties = new jProperties[20];

        /*
         Conductors control information
         */


        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            //Initially, only hands are visible
            for (int i = 0; i < 20; i++)
            {
                jointProperties[i].max_points = 20;
                jointProperties[i].size = 3.0f;
                jointProperties[i].color = 3;
                jointProperties[i].dots = new SkeletonPoint[MAX_POINTS];
                jointProperties[i].numDots = 0;
                jointProperties[i].firstDot = -1;
                jointProperties[i].joint = jointOrder[i];
                if (i == 8 || i == 9 || i== 19 | i==18)
                {
                    jointProperties[i].visible = true;
                    if (i == 8)
                        jointProperties[i].color = 1;
                    else
                        jointProperties[i].color = 2;
                }
                else
                    jointProperties[i].visible = false;
            }
            InitializeComponent();

            cb = new ControlBox(this);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            cb.Show();
        }


        /* =========================================================================
         *   
         *                                OpenGL Code
         * 
         * ======================================================================== */

        /*
         
         */

        public Vertex dir = new Vertex();
        public float maxAngle = 0;
        public float angle = 0;
        public float direction = -1;
        public bool rotate = false;

        bool matrixsaved = false;
        double[] savedMatrix = new double[16];

        private void OpenGLControl_OpenGLDraw(object sender, OpenGLEventArgs args)
        {  
            OpenGL gl = args.OpenGL;

            // Clear The Screen And The Depth Buffer
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
            gl.MatrixMode(OpenGL.GL_MODELVIEW);
       

            // Move Left And Into The Screen
            if (!matrixsaved)
                gl.LoadIdentity();
  //          else
  //              gl.LoadMatrix(savedMatrix);

            gl.Rotate(angle, dir.X, dir.Y, dir.Z);
            
            if (rotate)
            {
            
                if(direction < 0 )
                {
                    if(angle > maxAngle)
                        angle+= direction;
                    else
                        rotate = false;
                }
                else
                {
                    if (angle < maxAngle)
                        angle += direction;
                    else
                    {
                        rotate = false;
//                    gl.GetDouble(OpenGL.GL_MODELVIEW, savedMatrix);
                    //matrixsaved = true;
                    }
                }
            }
           // abf.ArcBall.TransformMatrix(gl);

            for (int skel = 0; skel < 2; skel++)
            {
                for (int i = 0; i < 20; i++)
                {

                    if (jointPropertiesv[skel, i].visible)
                    {
                        gl.PointSize(jointPropertiesv[skel, i].size);
                        gl.Begin(OpenGL.GL_POINTS);
                        gl.Color((float)jointPropertiesv[skel, i].red / 255.0, (float)jointPropertiesv[skel, i].green / 255.0, (float)jointPropertiesv[skel, i].blue / 255.0);



                        if (jointPropertiesv[skel, i].numDots < jointPropertiesv[skel, i].max_points)
                            for (int j = 0; j < jointPropertiesv[skel, i].numDots; j++)
                                gl.Vertex(jointPropertiesv[skel, i].dots[j].X, jointPropertiesv[1, i].dots[j].Y, jointPropertiesv[skel, i].dots[j].Z);
                        else
                            for (int j = jointPropertiesv[skel, i].firstDot; j < jointPropertiesv[skel, i].max_points + jointPropertiesv[skel, i].firstDot; j++)
                                gl.Vertex(jointPropertiesv[skel, i].dots[j % jointPropertiesv[skel, i].max_points].X, jointPropertiesv[skel, i].dots[j % jointPropertiesv[skel, i].max_points].Y, jointPropertiesv[skel, i].dots[j % jointPropertiesv[skel, i].max_points].Z);
                        gl.End();
                    }
                }
            }
            

           /* CoordinateMapper cm = new CoordinateMapper(this.sensor);

            if (depthPixelMap != null)
            {
                gl.PointSize(1);
                gl.Color(1.0f, 1.0f, 1.0f);
                gl.Begin(OpenGL.GL_POINTS);
                for (int y = 0; y < 240; y++)
                {
                    int mul = y*320;
                    for (int x = 0; x < 320; x++)
                    {
                        gl.Color((byte)255, (byte)255, (byte)255);
                        
                        //SkeletonPoint p = cm.MapDepthPointToSkeletonPoint(DepthImageFormat.Resolution320x240Fps30, depthPixelMap[x + mul].Depth);
                        //this.sensor
                        //SkeletonPoint p=
                        //this.sensor.MapDepthToSkeletonPoint(DepthImageFormat.Resolution320x240Fps30,(int) x/320.0f, (int)y/240.0f, (short)(((ushort)depthMap[x + mul]) >> 3));


                        //gl.Vertex(p.X,p.Y,-p.Z + displacement);
                        
                        //gl.Color((byte)x, (byte)y, (byte)255);
                        //gl.Vertex(x, y, -1000);
                        
                        //gl.Vertex(x,y,((ushort)depthMap[x+mul])>>3);
                    }
                }
                gl.End();


            }

            */

           // gl.Begin(OpenGL.GL_TRIANGLES);

           // gl.Color(1.0f, 0.0f, 0.0f);
           // gl.Vertex(-1.0f, -0.5f, -4.0f);    // A

           // gl.Color(0.0f, 1.0f, 0.0f);
           // gl.Vertex(1.0f, -0.5f, -4.0f);     // B

           // gl.Color(0.0f, 0.0f, 1.0f);
           // gl.Vertex(0.0f, 0.5f, -4.0f);      // C

           // gl.End();


        }



        /// <summary>
        /// Handles the OpenGLInitialized event of the OpenGLControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="SharpGL.SceneGraph.OpenGLEventArgs"/> instance containing the event data.</param>
        private void OpenGLControl_OpenGLInitialized(object sender, OpenGLEventArgs args)
        {
            OpenGL gl = args.OpenGL;
            //  Set the modelview matrix.

            gl.Enable(OpenGL.GL_COLOR);
            gl.Enable(OpenGL.GL_DEPTH_TEST);
            gl.Enable(OpenGL.GL_BLEND);

            float[] global_ambient = new float[] { 0.5f, 0.5f, 0.5f, 1.0f };
            float[] light0pos = new float[] { 0.0f, 5.0f, 10.0f, 1.0f };
            float[] light0ambient = new float[] { 0.2f, 0.2f, 0.2f, 1.0f };
            float[] light0diffuse = new float[] { 0.3f, 0.3f, 0.3f, 1.0f };
            float[] light0specular = new float[] { 0.8f, 0.8f, 0.8f, 1.0f };

            float[] lmodel_ambient = new float[] { 0.2f, 0.2f, 0.2f, 1.0f };
            gl.LightModel(OpenGL.GL_LIGHT_MODEL_AMBIENT, lmodel_ambient);

            gl.LightModel(OpenGL.GL_LIGHT_MODEL_AMBIENT, global_ambient);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_POSITION, light0pos);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_AMBIENT, light0ambient);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_DIFFUSE, light0diffuse);
           gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_SPECULAR, light0specular);
    
          //  gl.Enable(OpenGL.GL_LIGHTING);
          //  gl.Enable(OpenGL.GL_LIGHT0);

            gl.ShadeModel(OpenGL.GL_SMOOTH);

            //gl.LineWidth(LineSize);
            //gl.PointSize(LineSize); // 10 pixel dot!


            //build a program
            program.CreateInContext(gl);
            try
            {
                fs.CreateInContext(oglw.OpenGL);
                vs.CreateInContext(oglw.OpenGL);
            }
            catch (Exception e)
            {
                Dispatcher.BeginInvoke((Action) (() =>
                    {
                        MessageBox.Show(e.ToString());
                    }));

            }


            //load fragmentShader
            try
            {
                using (FileStream stream = new FileStream("shaders/glow.frag", FileMode.Open))
                {
                    StreamReader reader = new StreamReader(stream);
                    fs.SetSource(reader.ReadToEnd());
                    reader.Close();
                }
            }
            catch (Exception e)
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    MessageBox.Show(e.ToString());
                }));

            }


            try
            {
                //load vertexShader
                using (FileStream stream = new FileStream("shaders/glow.vertex", FileMode.Open))
                {
                    StreamReader reader = new StreamReader(stream);
                    vs.SetSource(reader.ReadToEnd());
                    reader.Close();
                }
            }
            catch (Exception e)
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    MessageBox.Show(e.ToString());
                }));

            }

            //compile both shaders
            try
            {

               // vs.Compile();

                
                if ( !(bool)vs.CompileStatus)
                {
                   /* int[] infoReturned = new int[] { 0 };
                    int[] infoLength = new int[] { 0 };
                    gl.GetShader(vs.ShaderObject,
                        OpenGL.GL_INFO_LOG_LENGTH, infoLength);

                    //  Get the compile info.
                    char[] tmp = new char[infoLength[0]];
                    
                    String il = new String(tmp);
                    //StringBuilder il = new StringBuilder(infoLength[0]);
                    OpenGL.Inv
                    InvokeExtensionFunction<glGetShaderInfoLog>(shader, bufSize, length, infoLog);
                    gl.GetShaderInfoLog(vs.ShaderObject, infoLength[0],
                       infoReturned, il);

                  
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        
                        MessageBox.Show( (string) infoReturned[0].ToString());
                        MessageBox.Show(il);
                    }));*/

                    Dispatcher.BeginInvoke((Action)(() =>
                    {
      //                  MessageBox.Show("VS: Error compiling!");
                    }));                
                }

                //fs.Compile();

                if (!(bool)fs.CompileStatus)
                {

                    Dispatcher.BeginInvoke((Action)(() =>
                    {
    //                    MessageBox.Show("FS: Error compiling!");
                    }));
                }



                //attach to a program
                program.AttachShader(vs);
                program.AttachShader(fs);

                program.Link();
                program.Push(gl, null);
            }
            catch (Exception e)
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    MessageBox.Show(e.ToString());
                }));
            }

        }

        private void OpenGLControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void OpenGLControl_Resized(object sender, OpenGLEventArgs args)
        {
            OpenGL gl = args.OpenGL;
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            gl.Viewport(0, 0, (int)(Width), (int)Height);

            
            gl.LoadIdentity();

            gl.Perspective(45, (double)(Width) / (double)Height, 0.01, 8000);
            //gl.LookAt(0, 0, -10, 160, 120, 0, 0, -1, 0);

            //gl.Perspective(45.0f, (double) (Width - 248)/ (double)Height, 0.01, 100.0);
            gl.Translate(0.0f,0.0f, -10.0f);
            //gl.MatrixMode(OpenGL.GL_WOR

            abf.ArcBall.SetBounds((float) (Width), (float)Height);

        }

        /***************************************************************************************
         * 
         *                                MOUSE / TRACKBALL CODE
         * 
         ***************************************************************************************/

        private void OpenGLControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            abf.ArcBall.MouseUp(e);
        }
        private void OpenGLControl_MouseMove(object sender, MouseEventArgs evt)
        {

            abf.ArcBall.MouseMove(sender, evt);

        }

        private void OpenGLControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            abf.ArcBall.MouseDown(sender, e);
        }


        /* ======================================================================================
         *
         *                          Kinect event handling functions
         * 
         * ======================================================================================*/



        /*
         * Skeleton stream
         */

          public Skeleton[] skeletons = new Skeleton[6];     //max skeletons = 6
        int[] skeletonId = { -1, -1 };

        public void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {

            Joint joint;
            DepthImagePoint depthPoint;
            bool newSkeleton = false;
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    newSkeleton = true;
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }


            int[] trackedOnes = { -1, -1 };
            int x = 0;
            if (newSkeleton)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                    {
                        trackedOnes[x++] = i;

                        //no one is beeing tracked
                        if (skeletonId[0] == -1 && skeletonId[1] == -1)
                        {
                            skeletonId[0] = i;
                        }
                        else
                        {
                            if (skeletonId[0] == -1)
                            {
                                if (i != skeletonId[1])
                                {
                                    skeletonId[0] = i;
                                }
                            }

                            if (skeletonId[1] == -1)
                            {
                                if (i != skeletonId[0])
                                {
                                    skeletonId[1] = i;
                                }
                            }

                        }


                    }
                }
                if (skeletonId[0] != trackedOnes[0] && skeletonId[0] != trackedOnes[1])
                {
                    skeletonId[0] = -1;
                }

                if (skeletonId[1] != trackedOnes[0] && skeletonId[1] != trackedOnes[1])
                {
                    skeletonId[1] = -1;
                }

                for (int j = 0; j < 2; j++)
                {
                    int i = trackedOnes[j];
                    
                    if (i != -1 && skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                    {

                        //no one is beeing tracked
                        if (skeletonId[0] == -1 && skeletonId[1] == -1)
                        {
                            skeletonId[0] = i;
                        }
                        else
                        {
                            if (skeletonId[0] == -1)
                            {
                                if (i != skeletonId[1])
                                {
                                    skeletonId[0] = i;
                                }
                            }

                            if (skeletonId[1] == -1)
                            {
                                if (i != skeletonId[0])
                                {
                                    skeletonId[1] = i;
                                }
                            }

                        }


                    }
                }

                        /*if (i != skeletonId[0] && skeletonId[0] == -1)
                        skeletonId[0] = i;
                        else
                        {
                        if (i != skeletonId[1] && skeletonId[1] == -1)
                        skeletonId[1] = i;
                        }
                        }
                        else
                        {
                        if(skeletonId[0] == i)
                        skeletonId[0] = -1;
                        if(skeletonId[1] == i)
                        skeletonId[1] = -1;
                        }
                        }*/

                        //Print joints to file
                        for (int s = 0; s < 2; s++)
                        {
                            if (skeletonId[s] != -1)
                            {
                                for (int i = 0; i < 20; i++)
                                {
                                    joint = skeletons[skeletonId[s]].Joints[jointPropertiesv[s, i].joint];
                                    depthPoint = SkeletonPointToDepth(joint.Position);
                                    if (recording)
                                    {
                                        file.Write("Skeleton" + (s + 1) + joint.JointType + ": ");
                                        if (joint.TrackingState == JointTrackingState.Tracked)
                                            file.WriteLine("T, X = " + depthPoint.X.ToString() + ", Y = " + depthPoint.Y.ToString() + ", Z = " + depthPoint.Depth.ToString());
                                        if (joint.TrackingState == JointTrackingState.Inferred)
                                            file.WriteLine("I, X = " + depthPoint.X.ToString() + ", Y = " + depthPoint.Y.ToString() + ", Z = " + depthPoint.Depth.ToString());
                                    }
                                    if (joint.TrackingState != JointTrackingState.NotTracked)
                                    {
                                        jointPropertiesv[s, i].firstDot = (jointPropertiesv[s, i].firstDot + 1) % jointPropertiesv[s, i].max_points;
                                        jointPropertiesv[s, i].dots[jointPropertiesv[s, i].firstDot] = joint.Position;
                                        if (jointPropertiesv[s, i].numDots < jointPropertiesv[s, i].max_points)
                                            jointPropertiesv[s, i].numDots++;
                                    }
                                }
                            }

                            if (recording)
                            {
                                file.WriteLine();
                                file.Flush();
                            }
                        }

                    }
                }
         



        /*
         * Depth stream
         */

        public short[] depthMap = null;
        public DepthImagePixel[] depthPixelMap = null;
        public void DepthFrameReady(object sender,  DepthImageFrameReadyEventArgs e)
        {

             DepthImageFrame imageFrame = e.OpenDepthImageFrame();
             if (imageFrame != null)
             {
                 if (depthPixelMap == null)
                     depthPixelMap = new DepthImagePixel[imageFrame.PixelDataLength];

                 imageFrame.CopyDepthImagePixelDataTo(depthPixelMap);
                 cb.lblMaxDepth.Content = imageFrame.MaxDepth + "mm";
                 cb.lblMinDepth.Content = imageFrame.MinDepth + "mm";
             }
            
           

            
            /*  if (imageFrame != null)
             {
                 imageFrame.CopyDepthImagePixelDataTo
                 if (depthMap == null)
                     depthMap = new short[imageFrame.PixelDataLength];


                 imageFrame.CopyPixelDataTo(depthMap);
             }*/
         }

        /*=================================================================================================*/
        private DepthImagePoint SkeletonPointToDepth(SkeletonPoint skelpoint)
        {
            
            return this.sensor.MapSkeletonPointToDepth(skelpoint, DepthImageFormat.Resolution640x480Fps30);
        }


        private void oglw_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
        }

        private float displacement = 0;

        private void oglw_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.R)
            {
                abf.ArcBall.Reset();
            }

            if (e.Key == Key.Q)
            {
                displacement++;
            }

            if (e.Key == Key.W)
            {
                displacement--;
            }
        
        }

       
    }
}
