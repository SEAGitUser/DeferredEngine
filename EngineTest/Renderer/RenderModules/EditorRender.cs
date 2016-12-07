﻿using System;
using System.Collections.Generic;
using EngineTest.Entities.Editor;
using EngineTest.Main;
using EngineTest.Recources;
using EngineTest.Renderer.Helper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EngineTest.Renderer.RenderModules
{
    public class EditorRender
    {
        public IdRenderer _idRenderer;
        public GraphicsDevice _graphicsDevice;

        private BillboardBuffer _billboardBuffer;

        private Assets _assets;

        private double mouseMoved;
        private bool mouseMovement = false;

        public void Initialize(GraphicsDevice graphics, Assets assets)
        {
            _graphicsDevice = graphics;
            _assets = assets;

            _billboardBuffer = new BillboardBuffer(Color.White, graphics);
            _idRenderer = new IdRenderer();
            _idRenderer.Initialize(graphics, _billboardBuffer, _assets);

        }

        public void Update(GameTime gameTime)
        {
            if (Input.mouseState != Input.mouseLastState)
            {
                //reset the timer!

                mouseMoved = gameTime.TotalGameTime.TotalMilliseconds + 500;
                mouseMovement = true;
            }

            if (mouseMoved < gameTime.TotalGameTime.TotalMilliseconds)
            {
                mouseMovement = false;
            }

        }

        public void SetUpRenderTarget(int width, int height)
        {
            _idRenderer.SetUpRenderTarget(width, height);
        }

        public void DrawBillboards(List<PointLightSource> lights, List<DirectionalLightSource> dirLights, Matrix staticViewProjection, Matrix view, EditorLogic.EditorSendData sendData)
        {
            int hoveredId = GetHoveredId();

            _graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            _graphicsDevice.SetVertexBuffer(_billboardBuffer.VBuffer);
            _graphicsDevice.Indices = (_billboardBuffer.IBuffer);

            Shaders.BillboardEffectParameter_Texture.SetValue(_assets.Icon_Light);

            Shaders.BillboardEffect.CurrentTechnique = Shaders.BillboardEffectTechnique_Billboard;

            Shaders.BillboardEffectParameter_IdColor.SetValue(Color.Gray.ToVector3());

            for (int index = 0; index < lights.Count; index++)
            {
                var light = lights[index];
                Matrix world = Matrix.CreateTranslation(light.Position);
                Shaders.BillboardEffectParameter_WorldViewProj.SetValue(world*staticViewProjection);
                Shaders.BillboardEffectParameter_WorldView.SetValue(world *view);

                if (light.Id == GetHoveredId())
                    Shaders.BillboardEffectParameter_IdColor.SetValue(Color.White.ToVector3());
                if (light.Id == sendData.SelectedObjectId)
                    Shaders.BillboardEffectParameter_IdColor.SetValue(Color.Gold.ToVector3());

                Shaders.BillboardEffect.CurrentTechnique.Passes[0].Apply();

                _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 2);

                if (light.Id == GetHoveredId() || light.Id == sendData.SelectedObjectId)
                    Shaders.BillboardEffectParameter_IdColor.SetValue(Color.Gray.ToVector3());
            }

            //DirectionalLights
            foreach(DirectionalLightSource light in dirLights)
            { 
                Matrix world = Matrix.CreateTranslation(light.Position);
                Shaders.BillboardEffectParameter_WorldViewProj.SetValue(world * staticViewProjection);
                Shaders.BillboardEffectParameter_WorldView.SetValue(world * view);

                if (light.Id == GetHoveredId())
                    Shaders.BillboardEffectParameter_IdColor.SetValue(Color.White.ToVector3());
                if (light.Id == sendData.SelectedObjectId)
                    Shaders.BillboardEffectParameter_IdColor.SetValue(Color.Gold.ToVector3());

                Shaders.BillboardEffect.CurrentTechnique.Passes[0].Apply();

                _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 2);

                if (light.Id == GetHoveredId() || light.Id == sendData.SelectedObjectId)
                    Shaders.BillboardEffectParameter_IdColor.SetValue(Color.Gray.ToVector3());

                LineHelperManager.AddLineStartDir(light.Position, light.Direction*10, 1, Color.Black, light.Color);

                LineHelperManager.AddLineStartDir(light.Position + Vector3.UnitX*10, light.Direction * 10, 1, Color.Black, light.Color);
                LineHelperManager.AddLineStartDir(light.Position - Vector3.UnitX * 10, light.Direction * 10, 1, Color.Black, light.Color);
                LineHelperManager.AddLineStartDir(light.Position + Vector3.UnitY * 10, light.Direction * 10, 1, Color.Black, light.Color);
                LineHelperManager.AddLineStartDir(light.Position - Vector3.UnitY * 10, light.Direction * 10, 1, Color.Black, light.Color);
                LineHelperManager.AddLineStartDir(light.Position + Vector3.UnitZ * 10, light.Direction * 10, 1, Color.Black, light.Color);
                LineHelperManager.AddLineStartDir(light.Position - Vector3.UnitZ * 10, light.Direction * 10, 1, Color.Black, light.Color);

                if (light.DrawShadows)
                {
                    BoundingFrustum _boundingFrustumShadow = new BoundingFrustum(light.LightViewProjection);

                    LineHelperManager.CreateBoundingBoxLines(_boundingFrustumShadow);
                }

                //DrawArrow(light.Position, 0,0,0, 2, Color.White, staticViewProjection, EditorLogic.GizmoModes.translation, light.Direction);
            }
        }

        public void DrawIds(MeshMaterialLibrary meshMaterialLibrary, List<PointLightSource>lights, List<DirectionalLightSource> dirLights, Matrix staticViewProjection, Matrix view, EditorLogic.EditorSendData editorData)
        {
            _idRenderer.Draw(meshMaterialLibrary, lights, dirLights, staticViewProjection, view, editorData, mouseMovement);
        }

        public void DrawEditorElements(MeshMaterialLibrary meshMaterialLibrary, List<PointLightSource> lights, List<DirectionalLightSource> dirLights, Matrix staticViewProjection, Matrix view, EditorLogic.EditorSendData editorData)
        {
            _graphicsDevice.SetRenderTarget(null);
            _graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.BlendState = BlendState.Opaque;
            
            DrawGizmo(staticViewProjection, editorData);
            DrawBillboards(lights, dirLights, staticViewProjection, view, editorData);
        }

        public void DrawGizmo(Matrix staticViewProjection, EditorLogic.EditorSendData editorData)
        {
            if (editorData.SelectedObjectId == 0) return;

            

            Vector3 position = editorData.SelectedObjectPosition;
            EditorLogic.GizmoModes gizmoMode = editorData.GizmoMode;

            //Z
            DrawArrow(position, 0, 0, 0, GetHoveredId() == 1 ? 1 : 0.5f, Color.Blue, staticViewProjection, gizmoMode); //z 1
            DrawArrow(position, -Math.PI / 2, 0, 0, GetHoveredId() == 2 ? 1 : 0.5f, Color.Green, staticViewProjection, gizmoMode); //y 2
            DrawArrow(position, 0, Math.PI / 2, 0, GetHoveredId() == 3 ? 1 : 0.5f, Color.Red, staticViewProjection, gizmoMode); //x 3

            DrawArrow(position, Math.PI, 0, 0, GetHoveredId() == 1 ? 1 : 0.5f, Color.Blue, staticViewProjection, gizmoMode); //z 1
            DrawArrow(position, Math.PI / 2, 0, 0, GetHoveredId() == 2 ? 1 : 0.5f, Color.Green, staticViewProjection, gizmoMode); //y 2
            DrawArrow(position, 0, -Math.PI / 2, 0, GetHoveredId() == 3 ? 1 : 0.5f, Color.Red, staticViewProjection, gizmoMode); //x 3
            //DrawArrowRound(position, Math.PI, 0, 0, GetHoveredId() == 1 ? 1 : 0.5f, Color.Blue, staticViewProjection); //z 1
            //DrawArrowRound(position, -Math.PI / 2, 0, 0, GetHoveredId() == 2 ? 1 : 0.5f, Color.Green, staticViewProjection); //y 2
            //DrawArrowRound(position, 0, Math.PI / 2, 0, GetHoveredId() == 3 ? 1 : 0.5f, Color.Red, staticViewProjection); //x 3
        }

        private void DrawArrow(Vector3 Position, double AngleX, double AngleY, double AngleZ, float Scale, Color color, Matrix staticViewProjection, EditorLogic.GizmoModes gizmoMode, Vector3? direction = null)
        {
            Matrix Rotation;
            if (direction != null)
            {
                Rotation = Matrix.CreateLookAt(Vector3.Zero, (Vector3) direction, Vector3.UnitX);
              

            }
            else
            {
                Rotation = Matrix.CreateRotationX((float)AngleX) * Matrix.CreateRotationY((float)AngleY) *
                                   Matrix.CreateRotationZ((float)AngleZ);
            }

            Matrix ScaleMatrix = Matrix.CreateScale(0.75f, 0.75f,Scale*1.5f);
            Matrix WorldViewProj = ScaleMatrix * Rotation * Matrix.CreateTranslation(Position) * staticViewProjection;

            Shaders.IdRenderEffectParameterWorldViewProj.SetValue(WorldViewProj);
            Shaders.IdRenderEffectParameterColorId.SetValue(color.ToVector4());

            Model model = gizmoMode == EditorLogic.GizmoModes.translation
                ? _assets.EditorArrow
                : _assets.EditorArrowRound;

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart meshpart in mesh.MeshParts)
                {
                    Shaders.IdRenderEffectDrawId.Apply();

                    _graphicsDevice.SetVertexBuffer(meshpart.VertexBuffer);
                    _graphicsDevice.Indices = (meshpart.IndexBuffer);
                    int primitiveCount = meshpart.PrimitiveCount;
                    int vertexOffset = meshpart.VertexOffset;
                    int vCount = meshpart.NumVertices;
                    int startIndex = meshpart.StartIndex;

                    _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, vertexOffset, startIndex, primitiveCount);
                }
            }
        }


        public RenderTarget2D GetOutlines()
        {
            return _idRenderer.GetRT();
        }

        /// <summary>
        /// Returns the id of the currently hovered object
        /// </summary>
        /// <returns></returns>
        public int GetHoveredId()
        {
            return _idRenderer.HoveredId;
        }
    }
}
