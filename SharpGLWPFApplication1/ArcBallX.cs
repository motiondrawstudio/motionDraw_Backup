using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SharpGL;
using SharpGL.SceneGraph;
using SharpGL.SceneGraph.Core;


namespace MotionDrawKxt
{
    /// <summary>
    /// The ArcBall camera supports arcball projection, making it ideal for use with a mouse.
    /// </summary>
    [Serializable()]
    public class ArcBallX
    {

        private Matrix transfMatrix = new Matrix(4,4);


        public ArcBallX()
        {
            //  Set the identity matrices.
            transfMatrix.SetIdentity();
        }

        public void TransformMatrix(OpenGL gl)
        {
            gl.MultMatrix(transfMatrix.AsColumnMajorArray);
        }

        public void Reset()
        {
            transfMatrix.SetIdentity();
        }

        public void MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition((IInputElement)sender);
            int x = (int)p.X, y = (int)p.Y;

            //new positions
            lastx = (float)((20.0 * x) / Width - 10);
            lasty = (float)(-(20.0 * y) / Height + 10);

            scalebar = 1;
        }

        int Height, Width;
        float lastx, lasty;
        float scalebar = 1;

        public void MouseMove(Object sender, MouseEventArgs evt)
        {
            //get mouse position
            Point p = evt.GetPosition((IInputElement)sender);
            int x = (int)p.X, y = (int)p.Y;

            //recalculate coordinates accordinly to a new coordinate system
	        float nx,ny;

	        //new positions
            nx = (float) ((20.0*x)/Width - 10);
            ny = (float) (-(20.0*y)/Height + 10);

	        //cout << nx << " " << ny << endl;

	        //if mouse is clicked and move
            if(evt.LeftButton == MouseButtonState.Pressed)
	        {
                
                Vertex s,e;
                s = new Vertex(lastx,lasty,1);
                e = new Vertex(nx,ny,1);

                s.UnitLength();
                e.UnitLength();
               

                if ((Math.Abs(s.X - e.X) > 0.0001) && (Math.Abs(s.Y - e.Y) > 0.0001) && (Math.Abs(s.Z - e.Z) > 0.0001))
                    rotateArcBall(s.VectorProduct(e), (float) Math.Acos(s.ScalarProduct(e)));
                
	        }

            if (evt.MiddleButton == MouseButtonState.Pressed)
            {

                Matrix d = new Matrix(4,4);
                d.SetIdentity();
                
                //define translation matrix
                d[0,3] = nx - lastx;
                d[1,3] = ny - lasty;
                d[2,3] = 0;

                transfMatrix = d * transfMatrix;

            }


            if (evt.RightButton == MouseButtonState.Pressed)
            {
                scalebar += (ny - lasty);

                Matrix d = new Matrix(4, 4);
                d.SetIdentity();

                //define scale matrix
                d[0, 0] = scalebar;
                d[1, 1] = scalebar;
                d[2, 2] = scalebar;

                transfMatrix = d * transfMatrix;
                

            }


            //save this position to be used in next call
            lastx = nx;
            lasty = ny;

      }

        public void MouseUp(MouseButtonEventArgs e)
        {
        }

        //
        //  Calculate rotation matrix 
        //
        private void rotateArcBall(Vertex axis, float angle)
        {

            float vectX=axis.X, vectY=axis.Y, vectZ=axis.Z;
            double c = Math.Cos(angle), s = Math.Sin(angle);

            axis.UnitLength();

            Matrix d = new Matrix(4,4);

            //first row
            d[0,0] = (1 - c) * (vectX * vectX - 1) + 1;
            d[0,1] = -vectZ * s + (1 - c) * vectX * vectY;
            d[0,2] = vectY * s + (1 - c) * vectX * vectZ;
            d[0,3] = 0;

            //second row
            d[1,0] = vectZ * s + (1 - c) * vectY * vectX; ;
            d[1,1] = 1 + (1 - c) * (vectY * vectY - 1);
            d[1,2] = -vectX * s + (1 - c) * vectY * vectZ;
            d[1,3] = 0;

            //third row
            d[2,0] = -vectY * s + (1 - c) * vectZ * vectX;
            d[2,1] = vectX * s + (1 - c) * vectZ * vectY;
            d[2,2] = (1 - c) * (vectZ * vectZ - 1) + 1;
            d[2,3] = 0;

            //last row
            d[3,0] = 0;
            d[3,1] = 0;
            d[3,2] = 0;
            d[3,3] = 1;

            //apply rotation            
            transfMatrix = d * transfMatrix;
   
        }

      
     
        

        public void SetBounds(float width, float height)
        {
            //  Set the adjust width and height.
            Height = (int)height;
            Width  = (int) width;
        }



    }
}

