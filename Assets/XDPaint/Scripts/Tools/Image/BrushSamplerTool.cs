using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using XDPaint.Core;
using XDPaint.Tools.Image.Base;
using XDPaint.Utils;
using Object = UnityEngine.Object;

namespace XDPaint.Tools.Image
{
	[Serializable]
	public class BrushSamplerTool : BasePaintTool
	{
		[Preserve] public BrushSamplerTool(IPaintData paintData) : base(paintData) { }
		
		public override PaintTool Type => PaintTool.BrushSampler;
		public override bool ShowPreview => preview && base.ShowPreview;
		public override bool RenderToLayer => false;
		public override bool RenderToInput => false;

		private Material brushSamplerMaterial;
		private RenderTexture brushTexture;
		private RenderTexture previewTexture;
		private RenderTargetIdentifier brushTarget;
		private RenderTexture brushTextureMask;
		private bool preview;
		private bool shouldSetBrushTextureParam;

		public override void Enter()
		{
			preview = Data.Brush.Preview;
			base.Enter();
			InitMaterial();
			Data.Brush.SetColor(new Color(1, 1, 1, Data.Brush.Color.a), false, false);
			SetCircleBrushPreview();
		}

		public override void Exit()
		{
			base.Exit();
			if (previewTexture != null)
			{
				previewTexture.ReleaseTexture();
			}
			Data.Material.SetTexture(Constants.PaintShader.BrushTexture, Data.Brush.RenderTexture);
			if (brushSamplerMaterial != null)
			{
				Object.Destroy(brushSamplerMaterial);
				brushSamplerMaterial = null;
			}
			if (brushTextureMask != null)
			{
				brushTextureMask.ReleaseTexture();
				brushTextureMask = null;
			}
		}

		public override void DoDispose()
		{
			base.DoDispose();
			if (brushTexture != null)
			{
				brushTexture.ReleaseTexture();
				brushTexture = null;
			}
		}

		public override void UpdatePress(Vector3 localPosition, Vector2 screenPosition, Vector2 uv, Vector2 paintPosition, float pressure)
		{
			base.UpdatePress(localPosition, screenPosition, uv, paintPosition, pressure);
			var brushVector = GetPreviewVector(GetTexture(RenderTarget.ActiveLayer), paintPosition, pressure);
			brushSamplerMaterial.SetVector(Constants.BrushSamplerShader.BrushOffset, brushVector);
			RenderBrush();
		}

		public override void UpdateDown(Vector3 localPosition, Vector2 screenPosition, Vector2 uv, Vector2 paintPosition, float pressure)
		{
			base.UpdateDown(localPosition, screenPosition, uv, paintPosition, pressure);
			UpdateRenderTexture();
			if (shouldSetBrushTextureParam)
			{
				brushSamplerMaterial.SetTexture(Constants.BrushSamplerShader.BrushTexture, brushTexture);
				shouldSetBrushTextureParam = false;
			}
		}
		        
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
			Data.Brush.SetColor(previousBrushColor, true, false);
			Data.Brush.SetTexture(brushSourceTexture, true, false);
			Data.Brush.Hardness = brushHardness;
		}

		private void InitMaterial()
		{
			if (brushSamplerMaterial == null)
			{
				brushSamplerMaterial = new Material(Settings.Instance.BrushSamplerShader);
				shouldSetBrushTextureParam = true;
				brushSamplerMaterial.mainTexture = GetTexture(RenderTarget.Combined);
				brushSamplerMaterial.SetTexture(Constants.BrushSamplerShader.BrushMaskTexture, Data.Brush.SourceTexture);
			}
		}

		/// <summary>
		/// Renders part of Result texture into RenderTexture, set new brush
		/// </summary>
		private void RenderBrush()
		{
			preview = false;
			Data.Render();
			Data.CommandBuilder.LoadOrtho().Clear().SetRenderTarget(brushTarget).DrawMesh(Data.QuadMesh, brushSamplerMaterial).Execute();
			var brushSourceTexture = Data.Brush.SourceTexture;
			var previousColor = Data.Brush.Color;
			var brushHardness = Data.Brush.Hardness;
			Data.Brush.Hardness = 1.0f;
			Data.Brush.SetColor(Color.white, false, false);
			Data.Brush.SetTexture(Settings.Instance.DefaultCircleBrush, true, false, false);
			Graphics.Blit(Data.Brush.RenderTexture, previewTexture);
			Data.Brush.SetColor(previousColor, false, false);
			Data.Brush.SetTexture(brushTexture, true, false, false);
			Data.Brush.SourceTexture = brushSourceTexture;
			Data.Brush.Hardness = brushHardness;
			preview = true;
		}

		/// <summary>
		/// Updates brush texture
		/// </summary>
		private void UpdateRenderTexture()
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
			brushSamplerMaterial.SetTexture(Constants.BrushSamplerShader.BrushMaskTexture, Data.Brush.SourceTexture);
		}
	}
}