using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using SharpGL;
using SharpGL.SceneGraph;
using SharpGL.SceneGraph.Primitives;
using SharpGL.SceneGraph.Effects;
using SharpGL.SceneGraph.Core;


namespace MotionDrawKxt
{
  
    /// <summary>
    /// An ArcBall is an effect that pushes an arcball transformation 
    /// onto the stack.
    /// </summary>
    public class ArcBallEffectX : Effect
    {
        /// <summary>
        /// Pushes the effect onto the specified parent element.
        /// </summary>
        /// <param name="gl">The OpenGL instance.</param>
        /// <param name="parentElement">The parent element.</param>
        public override void Push(OpenGL gl, SceneElement parentElement)
        {
            //  Push the stack.
            gl.PushMatrix();

            //  Perform the transformation.
            arcBall.TransformMatrix(gl);
        }

        /// <summary>
        /// Pops the specified parent element.
        /// </summary>
        /// <param name="gl">The OpenGL instance.</param>
        /// <param name="parentElement">The parent element.</param>
        public override void Pop(OpenGL gl, SceneElement parentElement)
        {
            //  Pop the stack.
            gl.PopMatrix();
        }

        /// <summary>
        /// The arcball.
        /// </summary>
        private ArcBallX arcBall = new ArcBallX();

        /// <summary>
        /// Gets or sets the linear transformation.
        /// </summary>
        /// <value>
        /// The linear transformation.
        /// </value>
        [Description("The ArcBall."), Category("Effect")]
        public ArcBallX ArcBall
        {
            get { return arcBall; }
            set { arcBall = value; }
        }
    }
    

}
