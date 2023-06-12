using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using XDPaint.Core;
using XDPaint.Core.Layers;
using XDPaint.Core.PaintObject.Base;
using XDPaint.Tools.Image.Base;
using XDPaint.Utils;
using Object = UnityEngine.Object;

namespace XDPaint.Tools.Image
{
	[Serializable]
    public class CloneTool : BasePaintTool
    {
	    [Preserve] public CloneTool(IPaintData paintData) : base(paintData) { }

	    public override PaintTool Type => PaintTool.Clone;
	    public override bool ShowPreview => preview && base.ShowPreview;
	    public override bool RenderToLayer => false;
	    public override bool RenderToInput => renderToInput;
	    public override bool DrawPreProcess => true;
	    public override bool BakeInputToPaint => bakeInputToPaint && Data.PaintMode.UsePaintInput;
	    private RenderTexture SourceTexture => UseAllActiveLayers ? 
		    Data.TexturesHelper.GetTexture(RenderTarget.Combined) : 
		    Data.LayersController.ActiveLayer.RenderTexture;
	    
	    #region Clone Settings

	    [PaintToolProperty] public bool CopyTextureOnPressDown { get; set; } = true;
	    [PaintToolProperty] public bool UseAllActiveLayers { get; set; } = true;
	    
	    #endregion
	    
	    private CloneData cloneData;
	    private Material brushSamplerMaterial;
	    private RenderTexture brushTexture;
		private RenderTargetIdentifier brushTarget;
		private RenderTexture previewTexture;
		private Color previousColor;
		private bool renderToInput;
		private bool preview;
		private bool bakeInputToPaint;
		private bool initialized;

		public override void Enter()
		{
			RenderToInput = false;
			preview = Data.Brush.Preview;
			base.Enter();
			bakeInputToPaint = false;
			Data.Render();
			cloneData = new CloneData();
			cloneData.Enter(Data, GetTexture(RenderTarget.Input), SourceTexture);
			InitCopyTexture();
			InitBrushSamplerMaterial();
			SetCircleBrushPreview();
			previousColor = Data.Brush.Color;
			Data.Brush.SetColor(new Color(1, 1, 1, previousColor.a), false, false);
			Data.Brush.OnPreviewChanged += OnBrushPreviewChanged;
			Data.Brush.OnTextureChanged += OnBrushTextureChanged;
			Data.Brush.OnColorChanged += OnBrushColorChanged;
			Data.LayersController.OnActiveLayerSwitched += OnActiveLayerSwitched;
			preview = true;
			initialized = true;
		}

		public override void Exit()
		{
			initialized = false;
			if (Data.Brush != null)
			{
				Data.Brush.OnPreviewChanged -= OnBrushPreviewChanged;
				Data.Brush.OnTextureChanged -= OnBrushTextureChanged;
				Data.Brush.OnColorChanged -= OnBrushColorChanged;
				Data.Brush.SetColor(previousColor, false, false);
				Data.Brush.SetTexture(Data.Brush.SourceTexture, true, false);
			}
			Data.LayersController.OnActiveLayerSwitched -= OnActiveLayerSwitched;
			base.Exit();
			if (cloneData != null)
			{
				cloneData.Exit();
				cloneData = null;
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
			if (previewTexture != null)
			{
				previewTexture.ReleaseTexture();
				previewTexture = null;
			}
		}
		
		private void OnActiveLayerSwitched(ILayer layer)
		{
			if (UseAllActiveLayers)
				return;
			cloneData.ClickCount = 0;
			cloneData.IsFirstClick = true;
			cloneData.IsUp = false;
			var previousPreview = preview;
			preview = false;
			Data.Render();
			preview = previousPreview;
			Graphics.Blit(SourceTexture, cloneData.CopyTexture);
		}

		public override void UpdateHover(Vector3 localPosition, Vector2 screenPosition, Vector2 uv, Vector2 paintPosition, float pressure)
		{
			base.UpdateHover(localPosition, screenPosition, uv, paintPosition, pressure);
			if (cloneData.ClickCount > 1 && Data.Brush.Preview && (cloneData.PrevPaintPosition != paintPosition || cloneData.PrevUV != uv || cloneData.PrevPressure != pressure))
			{
				var paintOffset = paintPosition - cloneData.PaintPosition;
				var brushOffset = GetPreviewVector(GetTexture(RenderTarget.ActiveLayer), paintOffset, pressure);
				brushSamplerMaterial.SetVector(Constants.BrushSamplerShader.BrushOffset, brushOffset);
				UpdateBrushRenderTexture();
				RenderBrush();
				cloneData.PrevUV = uv;
				cloneData.PrevPaintPosition = paintPosition;
				cloneData.PrevPressure = pressure;
			}
		}

		public override void UpdateDown(Vector3 localPosition, Vector2 screenPosition, Vector2 uv, Vector2 paintPosition, float pressure)
		{
			base.UpdateDown(localPosition, screenPosition, uv, paintPosition, pressure);
			preview = false;
			renderToInput = false;
			Data.Render();
		}

		public override void UpdatePress(Vector3 localPosition, Vector2 screenPosition, Vector2 uv, Vector2 paintPosition, float pressure)
		{
			base.UpdatePress(localPosition, screenPosition, uv, paintPosition, pressure);
			if (cloneData.IsUp)
			{
				if (CopyTextureOnPressDown)
				{
					Graphics.Blit(SourceTexture, cloneData.CopyTexture);
					preview = false;
					Data.Render();
					preview = true;
				}
				if (cloneData.ClickCount == 1)
				{
					cloneData.UVSecond = uv;
					cloneData.UV = cloneData.UVFirst - cloneData.UVSecond;
					cloneData.PaintPositionSecond = paintPosition;
					cloneData.PaintPosition = cloneData.PaintPositionSecond - cloneData.PaintPositionFirst;
				}
				cloneData.IsUp = false;
			}

			if (cloneData.IsFirstClick)
			{
				var brushOffset = GetPreviewVector(GetTexture(RenderTarget.ActiveLayer), paintPosition, pressure);
				brushSamplerMaterial.SetVector(Constants.BrushSamplerShader.BrushOffset, brushOffset);
				UpdateBrushRenderTexture();
				RenderBrush();
				cloneData.PaintPositionFirst = paintPosition;
				cloneData.UVFirst = uv;
				return;
			}

			renderToInput = true;
			cloneData.CloneMaterial.SetVector(Constants.CloneShader.Offset, cloneData.UV);
		}

		public override void UpdateUp(Vector2 screenPosition, bool inBounds)
		{
			base.UpdateUp(screenPosition, inBounds);
			if (inBounds)
			{
				renderToInput = true;
				cloneData.IsUp = true;
				preview = true;
				cloneData.IsFirstClick = false;
				cloneData.ClickCount++;
			}
			else if (cloneData.CopyTexture == null)
			{
				SetCircleBrushPreview();
			}
			Data.Render();
			bakeInputToPaint = false;
		}

		#region Initialization

		private void InitBrushSamplerMaterial()
		{
			if (brushSamplerMaterial == null)
			{
				brushSamplerMaterial = new Material(Settings.Instance.BrushSamplerShader);
			}
			brushSamplerMaterial.mainTexture = SourceTexture;
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

		private void InitCopyTexture()
		{
			var renderTexture = SourceTexture;
			if (cloneData.CopyTexture != null && cloneData.CopyTexture.IsCreated() &&
			    cloneData.CopyTexture.width == renderTexture.width && cloneData.CopyTexture.height == renderTexture.height)
				return;

			var previousPreview = preview;
			preview = false;
			Data.Render();
			preview = previousPreview;
			if (cloneData.CopyTexture != null)
			{
				cloneData.CopyTexture.Release();
				cloneData.CopyTexture.width = renderTexture.width;
				cloneData.CopyTexture.height = renderTexture.height;
				cloneData.CopyTexture.Create();
			}
			else
			{
				cloneData.CopyTexture = RenderTextureFactory.CreateTemporaryRenderTexture(
					renderTexture.width, renderTexture.height, 0, renderTexture.format, renderTexture.filterMode, 
					renderTexture.wrapMode, renderTexture.autoGenerateMips, renderTexture.useMipMap, renderTexture.anisoLevel);
			}
			Graphics.Blit(renderTexture, cloneData.CopyTexture);
			cloneData.CloneMaterial.mainTexture = cloneData.CopyTexture;
		}

		#endregion

		private void SetCircleBrushPreview()
		{
			previewTexture = RenderTextureFactory.CreateRenderTexture(Data.Brush.RenderTexture);
			Data.Material.SetTexture(Constants.PaintShader.BrushTexture, previewTexture);
			var brushSourceTexture = Data.Brush.SourceTexture;
			var previousBrushColor = Data.Brush.Color;
			var brushHardness = Data.Brush.Hardness;
			Data.Brush.Hardness = 1.0f;
			Data.Brush.SetColor(Color.white, false, false);
			Data.Brush.SetTexture(Settings.Instance.DefaultCircleBrush, true, false, false);
			Graphics.Blit(Data.Brush.RenderTexture, previewTexture);
			Data.Brush.SetColor(previousBrushColor, false, false);
			Data.Brush.SetTexture(brushSourceTexture, false, false);
			Data.Brush.Hardness = brushHardness;
		}
		
		private void RenderBrush()
		{
			preview = false;
			Data.Render();
			brushSamplerMaterial.mainTexture = SourceTexture;
			Data.CommandBuilder.LoadOrtho().Clear().SetRenderTarget(brushTarget).DrawMesh(Data.QuadMesh, brushSamplerMaterial).Execute();
			Data.Brush.RenderFromTexture(brushTexture);
			Data.Material.SetTexture(Constants.PaintShader.BrushTexture, Data.Brush.RenderTexture);
			preview = true;
		}

		public override void OnDrawPreProcess(RenderTargetIdentifier combined)
		{
			if (!initialized)
				return;
			
			base.OnDrawPreProcess(combined);
			if (Data.IsPainted)
			{
				Data.CommandBuilder.LoadOrtho().Clear().SetRenderTarget(cloneData.InputTarget).DrawMesh(Data.QuadMesh, cloneData.CloneMaterial).Execute();
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
				Data.Material.SetTexture(Constants.PaintShader.InputTexture, cloneData.InputTexture);
                Data.CommandBuilder.Clear().LoadOrtho().SetRenderTarget(GetTarget(RenderTarget.ActiveLayerTemp)).DrawMesh(Data.QuadMesh, Data.Material, InputToPaintPass).Execute();
			}
		}
		
		public override void OnBakeInputToLayer(RenderTargetIdentifier activeLayer)
		{
			if (bakeInputToPaint)
			{
				Graphics.Blit(cloneData.InputTexture, GetTexture(RenderTarget.Input));
			}
			else if (Data.PaintMode.UsePaintInput)
			{
				return;
			}
			base.OnBakeInputToLayer(activeLayer);
		}
		
		[Serializable]
	    private class CloneData
	    {
		    public RenderTexture InputTexture;
		    public RenderTargetIdentifier InputTarget;
		    public RenderTexture CopyTexture;
		    public Material CloneMaterial;
		    public Vector2 UVFirst;
		    public Vector2 UVSecond;
		    public Vector2 UV;
		    public Vector2 PaintPositionFirst;
		    public Vector2 PaintPositionSecond;
		    public Vector2 PaintPosition;
		    public Vector2 PrevUV = -Vector2.one;
		    public Vector2 PrevPaintPosition = -Vector2.one;
		    public float PrevPressure = -1f;
		    public int ClickCount;
		    public bool IsUp;
		    public bool IsFirstClick = true;
		    private IPaintData data;

		    public void Enter(IPaintData paintData, RenderTexture inputTexture, RenderTexture sourceTexture)
		    {
			    data = paintData;
			    InputTexture = RenderTextureFactory.CreateRenderTexture(inputTexture);
			    InputTarget = new RenderTargetIdentifier(InputTexture);
			    InitMaterial(sourceTexture);
		    }

		    public void Exit()
		    {
			    ClickCount = 0;
			    IsFirstClick = true;
			    IsUp = false;
			    if (InputTexture != null)
			    {
				    InputTexture.ReleaseTexture();
				    InputTexture = null;
			    }
			    if (CopyTexture != null)
			    {
				    RenderTexture.ReleaseTemporary(CopyTexture);
				    CopyTexture = null;
			    }
			    if (CloneMaterial != null)
			    {
				    Object.Destroy(CloneMaterial);
				    CloneMaterial = null;
			    }
		    }
		
		    private void InitMaterial(Texture sourceTexture)
		    {
			    if (CloneMaterial == null)
			    {
				    CloneMaterial = new Material(Settings.Instance.BrushCloneShader);
			    }
			    CloneMaterial.mainTexture = sourceTexture;
			    CloneMaterial.SetTexture(Constants.CloneShader.MaskTexture, data.TexturesHelper.GetTexture(RenderTarget.Input));
		    }
	    }
    }
}