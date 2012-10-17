using System;
using System.Collections.Generic;
using System.Linq;
//using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;


namespace Steering
{
   
    public class Dalek:Entity
    {
        public Vector3 targetPos = Vector3.Zero;
        private Dalek target = null;
        public Vector3 offset;
        bool added = false;

        protected Song sound;

        //[DllImport("winmm.dll")]
        //public static extern long PlaySound(String fileName, long a, long b);

        public Dalek Target
        {
            get { return target; }
            set { target = value; }
        }
        private Dalek leader = null;

        public Dalek Leader
        {
            get { return leader; }
            set { leader = value; }
        }

        protected SpriteFont spriteFont;
        int points = 6;
        VertexPositionColor[] pointList;
        public float maxSpeed = 5.0f;
        BasicEffect basicEffect;
        bool drawAxis;
        protected float lastFired = 0.25f;

        List<Vector3> feelers = new List<Vector3>();
        
        public List<Vector3> Feelers
        {
            get { return feelers; }
            set { feelers = value; }
        }

        public bool DrawAxis
        {
            get { return drawAxis; }
            set { drawAxis = value; }
        }

        public Dalek()
        {
            worldTransform = Matrix.Identity;
            pos = new Vector3(0, 5, 0);
            look = new Vector3(0, 0, -1);
            right = new Vector3(1, 0, 0);
            up = new Vector3(0, 1, 0);
            globalUp = new Vector3(0, 1, 0);
            drawAxis = false;
            spriteFont = XNAGame.Instance().Content.Load<SpriteFont>("Verdana");

            pointList = new VertexPositionColor[points];

            basicEffect = new BasicEffect(XNAGame.Instance().GraphicsDevice);
            basicEffect.VertexColorEnabled = true;
            
        }

        public override void LoadContent()
        {            
            model = XNAGame.Instance().Content.Load<Model>("dalek");
            sound = XNAGame.Instance().Content.Load<Song>("Exterminate");
        }

        public override void UnloadContent()
        {
            
        }

        public override void Update(GameTime gameTime)
        {
            float timeDelta = (float) gameTime.ElapsedGameTime.TotalSeconds;
            float speed = 5.0f;
            KeyboardState keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.L))
            {
                strafe(speed * timeDelta);
            }

            if (keyState.IsKeyDown(Keys.J))
            {
                strafe(-speed * timeDelta);
            }
            if (keyState.IsKeyDown(Keys.Up))
            {
                walk(speed * timeDelta);
            }

            if (keyState.IsKeyDown(Keys.Down))
            {
                walk(-speed * timeDelta);
            }

            if (keyState.IsKeyDown(Keys.Left))
            {
                yaw(speed * timeDelta);
            }

            if (keyState.IsKeyDown(Keys.Right))
            {
                yaw(-speed * timeDelta);
            }

            if (keyState.IsKeyDown(Keys.I))
            {
                pos.Y += speed * timeDelta;
            }

            if (keyState.IsKeyDown(Keys.K))
            {
                pos.Y -= speed * timeDelta;
            }

            if (keyState.IsKeyDown(Keys.Space))
            {
                if (lastFired >= 0.1f)
                {
                    lastFired = 0.0f;

                    // Fire lazer!!
                    Fire();
                }

                lastFired += timeDelta;
            }
     

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            XNAGame.Instance().SpriteBatch.DrawString(spriteFont, "Pos: " + pos.X + " " + pos.Y + " " + pos.Z, new Vector2(10, 10), Color.White);
            XNAGame.Instance().SpriteBatch.DrawString(spriteFont, "Look: " + look.X + " " + look.Y + " " + look.Z, new Vector2(10, 30), Color.White);
            XNAGame.Instance().SpriteBatch.DrawString(spriteFont, "Right: " + right.X + " " + right.Y + " " + right.Z, new Vector2(10, 50), Color.White);
            XNAGame.Instance().SpriteBatch.DrawString(spriteFont, "Up: " + up.X + " " + up.Y + " " + up.Z, new Vector2(10, 70), Color.White);

            // Draw the mesh
            if (model != null)
            {
                foreach (ModelMesh mesh in model.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.EnableDefaultLighting();
                        effect.PreferPerPixelLighting = true;
                        effect.World = worldTransform;
                        effect.Projection = XNAGame.Instance().Camera.getProjection();
                        effect.View = XNAGame.Instance().Camera.getView();
                    }
                    mesh.Draw();
                }
            }

            basicEffect.World = Matrix.Identity;
            basicEffect.View = XNAGame.Instance().Camera.getView();
            basicEffect.Projection = XNAGame.Instance().Camera.getProjection();

            if (drawAxis)
            {
                pointList[0] = new VertexPositionColor(pos, Color.Red);
                pointList[1] = new VertexPositionColor(pos + (look * 10), Color.Red);

                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    XNAGame.Instance().GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, pointList, 0, 1);
                }

                pointList[0] = new VertexPositionColor(pos, Color.Blue);
                pointList[1] = new VertexPositionColor(pos + (right * 10), Color.Blue);
                
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    XNAGame.Instance().GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, pointList, 0, 1);
                }

                pointList[0] = new VertexPositionColor(pos, Color.White);
                pointList[1] = new VertexPositionColor(pos + (up * 10), Color.White);

                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    XNAGame.Instance().GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, pointList, 0, 1);
                }
            }            
        }

        protected void Fire()
        {
            PlayAlertSound();

            Lazer lazer = new Lazer();

            // Why load content explicitly???
            lazer.LoadContent();

            // Set bullet position where it should be fired
            lazer.pos = pos + look * 10;
            lazer.pos.Y = 5;

            // Set direction at which bullet should be fired
            lazer.look = look;

            XNAGame.Instance().Children.Add(lazer);
        }

        protected void PlayAlertSound()
        {
            //PlaySound("Exterminate.mp3", 0, 0);
            MediaPlayer.Play(sound);
        }

        protected void PlayFireSound()
        {
        }
    }
}
