using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using XDPaint.Core;
using XDPaint.Core.Layers;
using XDPaint.Tools.Image.Base;
using XDPaint.Utils;
using Object = UnityEngine.Object;

namespace XDPaint.Tools.Image
{
	[Serializable]
    public class GrayscaleTool : BasePaintTool
    {
	    [Preserve] public GrayscaleTool(IPaintData paintData) : base(paintData) { }
	    
	    public override PaintTool Type => PaintTool.Grayscale;
	    public override bool ShowPreview => preview && base.ShowPreview;
	    public override bool RenderToLayer => false;
	    public override bool RenderToInput => true;
	    public override bool DrawPreProcess => true;
	    public override bool BakeInputToPaint => bakeInputToPaint && Data.PaintMode.UsePaintInput;

		private GrayscaleData grayscaleData;
		private bool initialized;
		private Material brushSamplerMaterial;
		private RenderTexture brushTexture;
		private RenderTargetIdentifier brushTarget;
		private Color previousColor;
		private bool bakeInputToPaint;
		private bool preview;

		public override void Enter()
		{
			preview = Data.Brush.Preview;
			base.Enter();
			bakeInputToPaint = false;
			Data.Render();
			grayscaleData = new GrayscaleData();
			grayscaleData.Enter(Data);
			UpdateRenderTextures();
			InitBrushMaterial();
			preview = true;
			previousColor = Data.Brush.Color;
			Data.Brush.SetColor(new Color(1, 1, 1, previousColor.a), false, false);
			Data.Brush.OnPreviewChanged += OnBrushPreviewChanged;
			Data.Brush.OnTextureChanged += OnBrushTextureChanged;
			Data.Brush.OnColorChanged += OnBrushColorChanged;
			Data.LayersController.OnActiveLayerSwitched += OnActiveLayerSwitched;
			initialized = true;
		}

		public override void Exit()
		{
			if (Data.Brush != null)
			{
				Data.Brush.OnPreviewChanged -= OnBrushPreviewChanged;
				Data.Brush.OnTextureChanged -= OnBrushTextureChanged;
				Data.Brush.OnColorChanged -= OnBrushColorChanged;
				Data.Brush.SetColor(previousColor, false, false);
				Data.Brush.SetTexture(Data.Brush.SourceTexture, true, false);
			}
			Data.LayersController.OnActiveLayerSwitched -= OnActiveLayerSwitched;
			Data.Material.SetTexture(Constants.PaintShader.InputTexture, GetTexture(RenderTarget.Input));
			initialized = false;
			base.Exit();
			if (grayscaleData != null)
			{
				grayscaleData.Exit();
				grayscaleData = null;
			}
			if (brushSamplerMaterial != null)
			{
				Object.Destroy(brushSamplerMaterial);
				brushSamplerMaterial = null;
			}
			if (brushTexture != null)
			{
				brushTexture.ReleaseTexture();
				brushTexture = null;
			}
		}
		
		private void OnBrushPreviewChanged(bool previewEnabled)
		{
			if (!previewEnabled)
			{
				Data.Brush.SetTexture(Data.Brush.SourceTexture, true, false);
			}
		}
		
		private void OnBrushTextureChanged(Texture texture)
		{
			if (brushTexture != null)
			{
				brushTexture.ReleaseTexture();
				brushTexture = null;
			}
			UpdateBrushRenderTexture();
			RenderBrush();
		}
		
		private void OnBrushColorChanged(Color color)
		{
			previousColor = color;
			Data.Brush.SetColor(new Color(1, 1, 1, previousColor.a), false, false);
		}

		public override void UpdateHover(Vector3 localPosition, Vector2 screenPosition, Vector2 uv, Vector2 paintPosition, float pressure)
		{
			base.UpdateHover(localPosition, screenPosition, uv, paintPosition, pressure);
			if (ShowPreview && (grayscaleData.PrevPaintPosition != paintPosition || grayscaleData.PrevUV != uv || grayscaleData.PrevPressure != pressure))
			{
				//render new brush
				var paintOffset = paintPosition;
				var brushOffset = GetPreviewVector(GetTexture(RenderTarget.ActiveLayer), paintOffset, pressure);
				brushSamplerMaterial.SetVector(Constants.BrushSamplerShader.BrushOffset, brushOffset);
				UpdateBrushRenderTexture();
				RenderBrush();
				grayscaleData.PrevUV = uv;
				grayscaleData.PrevPaintPosition = paintPosition;
				grayscaleData.PrevPressure = pressure;
			}
		}

		public override void UpdateUp(Vector2 screenPosition, bool inBounds)
		{
			base.UpdateUp(screenPosition, inBounds);
			RenderGrayscaleMaterial();
			preview = true;
			Data.Render();
			bakeInputToPaint = false;
		}

		private void RenderGrayscaleMaterial()
		{
			var previousTexture = grayscaleData.GrayscaleMaterial.GetTexture(Constants.GrayscaleShader.MaskTexture);
			grayscaleData.GrayscaleMaterial.SetTexture(Constants.GrayscaleShader.MaskTexture, null);
			Data.CommandBuilder.LoadOrtho().Clear().SetRenderTarget(grayscaleData.GrayscaleTarget).DrawMesh(Data.QuadMesh, grayscaleData.GrayscaleMaterial).Execute();
			grayscaleData.GrayscaleMaterial.SetTexture(Constants.GrayscaleShader.MaskTexture, previousTexture);
		}

		private void UpdateBrushRenderTexture()
		{
			if (brushTexture != null && brushTexture.IsCreated() && 
			    (brushTexture.width != Data.Brush.SourceTexture.width || brushTexture.height != Data.Brush.SourceTexture.height))
			{
				brushTexture.Release();
				brushTexture.width = Data.Brush.SourceTexture.width;
				brushTexture.height = Data.Brush.SourceTexture.height;
				brushTexture.Create();
			}
			else if (brushTexture == null)
			{
				brushTexture = RenderTextureFactory.CreateRenderTexture(Data.Brush.SourceTexture);
				brushTarget = new RenderTargetIdentifier(brushTexture);
			}
			brushSamplerMaterial.SetTexture(Constants.BrushSamplerShader.BrushTexture, brushTexture);
			brushSamplerMaterial.SetTexture(Constants.BrushSamplerShader.BrushMaskTexture, Data.Brush.SourceTexture);
		}

		private void RenderBrush()
		{
			preview = false;
			Data.Render();
			brushSamplerMaterial.mainTexture = grayscaleData.GrayscaleTexture;
			Data.CommandBuilder.LoadOrtho().Clear().SetRenderTarget(brushTarget).ClearRenderTarget().DrawMesh(Data.QuadMesh, brushSamplerMaterial).Execute();
			Data.Brush.RenderFromTexture(brushTexture);
			Data.Material.SetTexture(Constants.PaintShader.BrushTexture, Data.Brush.RenderTexture);
			preview = true;
		}

		#region Initialization

		private void OnActiveLayerSwitched(ILayer layer)
		{
			UpdateRenderTextures();
		}
		
		private void InitBrushMaterial()
		{
			if (brushSamplerMaterial == null)
			{
				brushSamplerMaterial = new Material(Settings.Instance.BrushSamplerShader);
			}
			brushSamplerMaterial.mainTexture = grayscaleData.GrayscaleTexture;
		}
		
		private void UpdateRenderTextures()
		{
			var renderTexture = GetTexture(RenderTarget.ActiveLayer);
			if (grayscaleData.GrayscaleTexture != null && grayscaleData.GrayscaleTexture.IsCreated() && 
			    (grayscaleData.GrayscaleTexture.width != renderTexture.width || grayscaleData.GrayscaleTexture.height != renderTexture.height))
			{
				var grayscaleRenderTexture = grayscaleData.GrayscaleTexture;
				grayscaleRenderTexture.Release();
				grayscaleRenderTexture.width = renderTexture.width;
				grayscaleRenderTexture.height = renderTexture.height;
				grayscaleRenderTexture.Create();
			}
			else if (grayscaleData.GrayscaleTexture == null)
			{
				grayscaleData.GrayscaleTexture = RenderTextureFactory.CreateRenderTexture(renderTexture);
				grayscaleData.GrayscaleTarget = new RenderTargetIdentifier(grayscaleData.GrayscaleTexture);
			}
			grayscaleData.InitMaterial();
			RenderGrayscaleMaterial();
		}

		#endregion
		
		public override void OnDrawPreProcess(RenderTargetIdentifier combined)
		{
			if (!initialized)
				return;
			
			base.OnDrawPreProcess(combined);
			if (Data.IsPainted)
			{
				Data.CommandBuilder.Clear().SetRenderTarget(grayscaleData.GrayscaleTarget).DrawMesh(Data.QuadMesh, grayscaleData.GrayscaleMaterial).Execute();
				bakeInputToPaint = true;
			}
		}

		public override void OnDrawProcess(RenderTargetIdentifier combined)
		{
			if (!initialized)
			{
				base.OnDrawProcess(combined);
				return;
			}

			base.OnDrawProcess(combined);
			if (!Data.PaintMode.UsePaintInput && Data.IsPainted)
			{
				OnBakeInputToLayer(GetTarget(RenderTarget.ActiveLayer));
			}
		}
		
		protected override void DrawCurrentLayer()
		{
			if (!Data.IsPainting)
			{
				base.DrawCurrentLayer();
				return;
			}

			if (Data.PaintMode.UsePaintInput && Data.IsPainted)
			{
				Data.Material.SetTexture(Constants.PaintShader.PaintTexture, GetTexture(RenderTarget.ActiveLayer));
				Data.Material.SetTexture(Constants.PaintShader.InputTexture, grayscaleData.GrayscaleTexture);
				Data.CommandBuilder.Clear().LoadOrtho().SetRenderTarget(GetTarget(RenderTarget.ActiveLayerTemp)).DrawMesh(Data.QuadMesh, Data.Material, InputToPaintPass).Execute();
			}
		}
		
		public override void OnBakeInputToLayer(RenderTargetIdentifier activeLayer)
		{
			if (bakeInputToPaint)
			{
				Graphics.Blit(grayscaleData.GrayscaleTexture, GetTexture(RenderTarget.Input));
			}
			else if (Data.PaintMode.UsePaintInput)
			{
				return;
			}
			base.OnBakeInputToLayer(activeLayer);
		}

		[Serializable]
		private class GrayscaleData
		{
			public Material GrayscaleMaterial;
			public RenderTexture GrayscaleTexture;
			public RenderTargetIdentifier GrayscaleTarget;
			public Vector2 PrevUV = -Vector2.one;
			public Vector2 PrevPaintPosition = -Vector2.one;
			public float PrevPressure = -1f;
			private IPaintData data;

			public void Enter(IPaintData paintData)
			{
				data = paintData;
			}
		
			public void Exit()
			{
				if (GrayscaleMaterial != null)
				{
					Object.Destroy(GrayscaleMaterial);
					GrayscaleMaterial = null;
				}
				if (GrayscaleTexture != null)
				{
					GrayscaleTexture.ReleaseTexture();
					GrayscaleTexture = null;
				}
			}
		
			public void InitMaterial()
			{
				if (GrayscaleMaterial == null)
				{
					GrayscaleMaterial = new Material(Settings.Instance.GrayscaleShader);
				}
				GrayscaleMaterial.mainTexture = data.LayersController.ActiveLayer.RenderTexture;
				GrayscaleMaterial.SetTexture(Constants.GrayscaleShader.MaskTexture, data.TexturesHelper.GetTexture(RenderTarget.Input));
			}
		}
    }
}