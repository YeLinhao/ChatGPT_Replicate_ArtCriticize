using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using XDPaint.Core;
using XDPaint.Core.Layers;
using XDPaint.Core.Materials;
using XDPaint.Core.PaintModes;
using XDPaint.Core.PaintObject.Base;

namespace XDPaint.Tools.Image.Base
{
    [Serializable]
    public abstract class BasePaintTool : IPaintTool
    {
        /// <summary>
        /// Type of the tool
        /// </summary>
        public abstract PaintTool Type { get; }

        protected IPaintData Data;

        public virtual bool ShowPreview => Data.Brush.Preview;
        public virtual bool RenderToLayer { get; protected set; }
        public virtual bool RenderToInput { get; protected set; }
        public virtual bool AllowRender => RenderToLayer || RenderToInput || ShowPreview;
        public virtual bool CanDrawLines { get; protected set; }
        public virtual bool ConsiderPreviousPosition => DrawOnBrushMove;
        public virtual bool RenderToTextures => RenderToLayer || RenderToInput;
        public virtual bool DrawPreProcess { get; protected set; }
        public virtual bool DrawProcess => true;
        public virtual bool BakeInputToPaint => true;
        protected virtual PaintPass InputToPaintPass => PaintPass.Blend;
        private RenderTexture PaintRenderTexture => Data.TexturesHelper.GetTexture(RenderTarget.ActiveLayerTemp);

        #region Settings

        [PaintToolProperty] public bool CanPaintLines { get => CanDrawLines; set => CanDrawLines = value; }
        [PaintToolProperty] public bool DrawOnBrushMove { get; set; } = false;

        #endregion
        
        private bool hasPluralLayersEnabled;

        protected BasePaintTool(IPaintData paintData)
        {
            Data = paintData;
        }

        /// <summary>
        /// Enter the tool
        /// </summary>
        public virtual void Enter()
        {
            CanDrawLines = true;
            RenderToLayer = true;
            RenderToInput = true;
            Data.Material.SetTexture(Constants.PaintShader.PaintTexture, GetTexture(RenderTarget.ActiveLayer));
            Data.Material.SetTexture(Constants.PaintShader.InputTexture, GetTexture(RenderTarget.Input));
            Data.LayersController.OnLayersCollectionChanged += OnLayersCollectionChanged;
            Data.LayersController.OnLayerChanged += OnLayerChanged;
            Data.Brush.OnPreviewChanged += OnBrushPreviewChanged;
            UpdateTextures();
        }

        /// <summary>
        /// Exit from the tool
        /// </summary>
        public virtual void Exit()
        {
            Data.Render();
            Data.LayersController.OnLayersCollectionChanged -= OnLayersCollectionChanged;
            Data.LayersController.OnLayerChanged -= OnLayerChanged;
            Data.Brush.OnPreviewChanged -= OnBrushPreviewChanged;
        }
        
        private void OnLayersCollectionChanged(ObservableCollection<ILayer> collection, NotifyCollectionChangedEventArgs notify)
        {
            UpdateTextures();
        }

        private void OnLayerChanged(ILayer layer)
        {
            UpdateTextures();
        }
        
        private void OnBrushPreviewChanged(bool previewEnabled)
        {
            UpdateTextures();
        }
        
        private void UpdateTextures()
        {
            hasPluralLayersEnabled = Data.LayersController.Layers.Count(x => x.Enabled && x.Opacity > 0) > 1;
            if (hasPluralLayersEnabled || ShowPreview)
            {
                Data.TexturesHelper.CreateCombinedTempRenderTexture();
            }
            else
            {
                Data.TexturesHelper.ReleaseCombinedTempRenderTexture();
            }
        }

        public virtual void DoDispose()
        {
        }

        /// <summary>
        /// On Mouse Hover handler
        /// </summary>
        /// <param name="localPosition"></param>
        /// <param name="screenPosition"></param>
        /// <param name="uv"></param>
        /// <param name="paintPosition"></param>
        /// <param name="pressure"></param>
        public virtual void UpdateHover(Vector3 localPosition, Vector2 screenPosition, Vector2 uv, Vector2 paintPosition, float pressure)
        {
        }

        /// <summary>
        /// On Mouse Down handler
        /// </summary>
        /// <param name="localPosition"></param>
        /// <param name="screenPosition"></param>
        /// <param name="uv"></param>
        /// <param name="paintPosition"></param>
        /// <param name="pressure"></param>
        public virtual void UpdateDown(Vector3 localPosition, Vector2 screenPosition, Vector2 uv, Vector2 paintPosition, float pressure)
        {
        }

        /// <summary>
        /// On Mouse Press handler
        /// </summary>
        /// <param name="localPosition"></param>
        /// <param name="screenPosition"></param>
        /// <param name="uv"></param>
        /// <param name="paintPosition"></param>
        /// <param name="pressure"></param>
        public virtual void UpdatePress(Vector3 localPosition, Vector2 screenPosition, Vector2 uv, Vector2 paintPosition, float pressure)
        {
        }

        /// <summary>
        /// On Mouse Up handler
        /// </summary>
        /// <param name="screenPosition"></param>
        /// <param name="inBounds"></param>
        public virtual void UpdateUp(Vector2 screenPosition, bool inBounds)
        {
        }
        
        public void FillWithColor(Color color)
        {
            Data.CommandBuilder.Clear().LoadOrtho().SetRenderTarget(GetTarget(RenderTarget.ActiveLayer)).ClearRenderTarget(color).Execute();
            Data.LayersController.ActiveLayer.SaveState();
            Data.Render();
        }
                
        /// <summary>
        /// Returns Vector4 for brush preview
        /// </summary>
        /// <returns></returns>
        protected virtual Vector4 GetPreviewVector(Texture texture, Vector2 paintPosition, float pressure)
        {
            var brushScale = Data.Brush.Size * pressure;
            var sourceTextureScaled = new Vector2(Data.Brush.SourceTexture.width, Data.Brush.SourceTexture.height) * brushScale;
            var texel = texture.texelSize;
            var offset = new Vector2(paintPosition.x / texture.width, paintPosition.y / texture.height);
            offset -= sourceTextureScaled * texel / 2f;
            
            if (Data.Brush.RenderOffset.x > 0)
            {
                offset.x += texel.x / 2f;
                if (Data.Brush.SourceTexture.width > 1)
                {
                    offset.x -= offset.x % texel.x;
                }
            }
            
            if (Data.Brush.RenderOffset.y > 0)
            {
                offset.y += texel.y / 2f;
                if (Data.Brush.SourceTexture.height > 1)
                {
                    offset.y -= offset.y % texel.y;
                }
            }

            var scale = new Vector2(sourceTextureScaled.x / texture.width, sourceTextureScaled.y / texture.height);
            return new Vector4(offset.x, offset.y, scale.x, scale.y);
        }

        public virtual void SetPaintMode(IPaintMode mode)
        {
            if (mode.UsePaintInput)
            {
                if (GetTexture(RenderTarget.ActiveLayerTemp) == null)
                {
                    Data.TexturesHelper.CreateActiveLayerTempRenderTexture();
                }
            }
            else
            {
                Data.TexturesHelper.ReleaseActiveLayerTempRenderTexture();
            }
        }

        public virtual void OnBrushChanged(IBrush brush)
        {
            UpdateTextures();
        }

        /// <summary>
        /// Pre Draw Process handler
        /// </summary>
        /// <param name="combined"></param>
        public virtual void OnDrawPreProcess(RenderTargetIdentifier combined)
        {
            DrawPreProcess = false;
        }

        /// <summary>
        /// Draw Process handler
        /// </summary>
        /// <param name="combined"></param>
        public virtual void OnDrawProcess(RenderTargetIdentifier combined)
        {
            RenderLayers(combined);
            if (!Data.IsPainting)
            {
                RenderPreview(combined);
            }
        }

        /// <summary>
        /// Render Layers in the Combined Render Texture
        /// </summary>
        /// <param name="combined"></param>
        protected virtual void RenderLayers(RenderTargetIdentifier combined)
        {
            var rendered = false;
            var paintTexture = Data.Material.GetTexture(Constants.PaintShader.PaintTexture);
            var inputTexture = Data.Material.GetTexture(Constants.PaintShader.InputTexture);
            var color = Data.Material.color;

            Data.Material.SetFloat(Constants.PaintShader.Opacity, 1);
            if (hasPluralLayersEnabled || ShowPreview)
            {
                Data.CommandBuilder.Clear().LoadOrtho().SetRenderTarget(GetTarget(RenderTarget.CombinedTemp)).ClearRenderTarget().Execute();
            }

            foreach (var currentLayer in Data.LayersController.Layers)
            {
                if (!currentLayer.Enabled || currentLayer.Opacity == 0)
                    continue;

                if (currentLayer == Data.LayersController.ActiveLayer)
                {
                    DrawCurrentLayer();
                }

                foreach (var keyword in Data.Material.shaderKeywords)
                {
                    Data.Material.DisableKeyword(keyword);
                }

                if (rendered)
                {
                    var blendKeyword = string.Format(Constants.PaintShader.LayerBlendFormat, currentLayer.BlendingMode.ToString().ToUpper());
                    Data.Material.EnableKeyword(blendKeyword);
                }
                else
                {
                    var blendKeyword = string.Format(Constants.PaintShader.LayerBlendFormat, BlendingMode.Normal.ToString().ToUpper());
                    Data.Material.EnableKeyword(blendKeyword);
                }

                Data.Material.SetFloat(Constants.PaintShader.Opacity, currentLayer.Opacity);
                if (Data.PaintMode.UsePaintInput && currentLayer == Data.LayersController.ActiveLayer)
                {
                    Data.Material.SetTexture(Constants.PaintShader.PaintTexture, PaintRenderTexture);
                }
                else
                {
                    Data.Material.SetTexture(Constants.PaintShader.PaintTexture, currentLayer.RenderTexture);
                }

                if (hasPluralLayersEnabled || ShowPreview)
                {
                    Graphics.Blit(GetTexture(RenderTarget.Combined), GetTexture(RenderTarget.CombinedTemp));
                    Data.Material.SetTexture(Constants.PaintShader.InputTexture, GetTexture(RenderTarget.CombinedTemp));
                }
                else
                {
                    Data.Material.SetTexture(Constants.PaintShader.InputTexture, GetTexture(RenderTarget.Input));
                }

                if (currentLayer.MaskEnabled && currentLayer.MaskRenderTexture != null)
                {
                    Data.Material.SetTexture(Constants.PaintShader.MaskTexture, currentLayer.MaskRenderTexture);
                }
                else
                {
                    Data.Material.SetTexture(Constants.PaintShader.MaskTexture, null);
                }

                Data.Material.color = new Color(color.r, color.g, color.b, currentLayer.Opacity);
                Data.CommandBuilder.Clear().LoadOrtho().SetRenderTarget(combined).DrawMesh(Data.QuadMesh, Data.Material).Execute();
                rendered = true;
            }

            Data.Material.SetTexture(Constants.PaintShader.PaintTexture, paintTexture);
            Data.Material.SetTexture(Constants.PaintShader.InputTexture, inputTexture);
            Data.Material.color = color;
        }

        /// <summary>
        /// Render Active Layer
        /// </summary>
        protected virtual void DrawCurrentLayer()
        {
            if (Data.PaintMode.UsePaintInput)
            {
                Data.Material.SetTexture(Constants.PaintShader.PaintTexture, GetTexture(RenderTarget.ActiveLayer));
                Data.Material.SetTexture(Constants.PaintShader.InputTexture, GetTexture(RenderTarget.Input));
                Data.CommandBuilder.Clear().LoadOrtho().SetRenderTarget(GetTarget(RenderTarget.ActiveLayerTemp)).DrawMesh(Data.QuadMesh, Data.Material, InputToPaintPass).Execute();
            }
        }

        /// <summary>
        /// Render brush preview
        /// </summary>
        /// <param name="combined"></param>
        protected void RenderPreview(RenderTargetIdentifier combined)
        {
            if (ShowPreview && Data.InBounds)
            {
                var previousPaintTexture = Data.Material.GetTexture(Constants.PaintShader.PaintTexture);
                Graphics.Blit(GetTexture(RenderTarget.Combined), GetTexture(RenderTarget.CombinedTemp));
                Data.Material.SetTexture(Constants.PaintShader.PaintTexture, GetTexture(RenderTarget.CombinedTemp));
                Data.CommandBuilder.Clear().LoadOrtho().SetRenderTarget(combined).DrawMesh(Data.QuadMesh, Data.Material, PaintPass.Preview).Execute();
                Data.Material.SetTexture(Constants.PaintShader.PaintTexture, previousPaintTexture);
            }
        }

        /// <summary>
        /// Bake Input RenderTexture into Paint Render Texture (Active Layer)
        /// </summary>
        /// <param name="activeLayer"></param>
        public virtual void OnBakeInputToLayer(RenderTargetIdentifier activeLayer)
        {
            if (Data.PaintMode.UsePaintInput)
            {
                Graphics.Blit(GetTexture(RenderTarget.ActiveLayer), GetTexture(RenderTarget.ActiveLayerTemp));
                Data.Material.SetTexture(Constants.PaintShader.PaintTexture, GetTexture(RenderTarget.ActiveLayerTemp));
            }
            else
            {
                Data.Material.SetTexture(Constants.PaintShader.PaintTexture, GetTexture(RenderTarget.ActiveLayer));
            }
            Data.CommandBuilder.LoadOrtho().Clear().SetRenderTarget(activeLayer).DrawMesh(Data.QuadMesh, Data.Material, InputToPaintPass).Execute();
            Data.Material.SetTexture(Constants.PaintShader.PaintTexture, GetTexture(RenderTarget.ActiveLayer));
        }

        protected RenderTexture GetTexture(RenderTarget target)
        {
            if (target == RenderTarget.ActiveLayer)
            {
                return Data.LayersController.ActiveLayer.RenderTexture;
            }
            return Data.TexturesHelper.GetTexture(target);
        }

        protected RenderTargetIdentifier GetTarget(RenderTarget target)
        {
            if (target == RenderTarget.ActiveLayer)
            {
                return Data.LayersController.ActiveLayer.RenderTarget;
            }
            return Data.TexturesHelper.GetTarget(target);
        }
    }
}