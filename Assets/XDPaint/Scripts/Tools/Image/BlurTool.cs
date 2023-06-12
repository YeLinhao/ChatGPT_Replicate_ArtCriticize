using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using XDPaint.Core;
using XDPaint.Core.PaintObject.Base;
using XDPaint.Tools.Image.Base;
using XDPaint.Utils;
using Object = UnityEngine.Object;

namespace XDPaint.Tools.Image
{
	[Serializable]
    public class BlurTool : BasePaintTool
    {
	    [Preserve] public BlurTool(IPaintData paintData) : base(paintData) { }
	    
	    public override PaintTool Type => PaintTool.Blur;
	    public override bool ShowPreview => false;
	    public override bool RenderToLayer => false;
	    public override bool RenderToInput => true;
	    public override bool DrawPreProcess => true;

	    private RenderTexture SourceTexture => UseAllActiveLayers ? 
		    Data.TexturesHelper.GetTexture(RenderTarget.Combined) : 
		    Data.LayersController.ActiveLayer.RenderTexture;

	    #region Blur settings

        [PaintToolProperty, PaintToolRange(1, 5)] public int Iterations { get; set; } = 3;
        [PaintToolProperty, PaintToolRange(0.01f, 5f)] public float BlurStrength { get; set; } = 1.5f;
        [PaintToolProperty, PaintToolRange(1, 16)] public int DownscaleRatio { get; set; } = 1;
        [PaintToolProperty] public bool UseAllActiveLayers { get; set; } = true;

        #endregion

        private BlurData blurData;
		private bool initialized;

		public override void Enter()
		{
			base.Enter();
			blurData = new BlurData();
			blurData.Enter(Data);
			UpdateRenderTextures();
			initialized = true;
		}

		public override void Exit()
		{
			Data.Material.SetTexture(Constants.PaintShader.InputTexture, GetTexture(RenderTarget.Input));
			initialized = false;
			base.Exit();
			if (blurData != null)
			{
				blurData.Exit();
				blurData = null;
			}
		}

		#region Initialization

		private void UpdateRenderTextures()
		{
			if (blurData.BlurTexture != null)
				return;

			var renderTexture = GetTexture(RenderTarget.ActiveLayer);
			blurData.BlurTexture = RenderTextureFactory.CreateRenderTexture(renderTexture);
			blurData.BlurTarget = new RenderTargetIdentifier(blurData.BlurTexture);
			Data.CommandBuilder.LoadOrtho().Clear().SetRenderTarget(blurData.BlurTarget).ClearRenderTarget().Execute();
			blurData.PreBlurTexture = RenderTextureFactory.CreateRenderTexture(renderTexture);
			blurData.PreBlurTarget = new RenderTargetIdentifier(blurData.PreBlurTexture);
			Data.CommandBuilder.LoadOrtho().Clear().SetRenderTarget(blurData.PreBlurTarget).ClearRenderTarget().Execute();
			blurData.InitMaterials();
		}

		#endregion

		private void Blur(Material blurMaterial, RenderTexture source, RenderTexture destination)
		{
			if (blurMaterial != null)
			{
				var width = source.width / DownscaleRatio;
				var height = source.height / DownscaleRatio;
				var buffer0 = RenderTexture.GetTemporary(width, height, 0, source.format);
				buffer0.filterMode = FilterMode.Bilinear;
				Graphics.Blit(source, buffer0);
				blurMaterial.SetFloat(Constants.BlurShader.BlurSize, BlurStrength);
				for (var i = 0; i < Iterations; i++)
				{
					var buffer1 = RenderTexture.GetTemporary(width, height, 0);
					Graphics.Blit(buffer0, buffer1, blurMaterial, 0);
					RenderTexture.ReleaseTemporary(buffer0);
					buffer0 = buffer1;
					buffer1 = RenderTexture.GetTemporary(width, height, 0);
					Graphics.Blit(buffer0, buffer1, blurMaterial, 1);
					RenderTexture.ReleaseTemporary(buffer0);
					buffer0 = buffer1;
				}
				Graphics.Blit(buffer0, destination);
				RenderTexture.ReleaseTemporary(buffer0);
			}
			else
			{
				Graphics.Blit(source, destination);
			}
		}

		public override void OnDrawPreProcess(RenderTargetIdentifier combined)
		{
			base.OnDrawPreProcess(combined);
			if (Data.IsPainted)
			{
				blurData.MaskMaterial.color = Data.Brush.Color;
				//clear render texture
				Data.CommandBuilder.Clear().LoadOrtho().SetRenderTarget(blurData.PreBlurTarget).ClearRenderTarget().Execute();
				Blur(blurData.BlurMaterial, SourceTexture, blurData.PreBlurTexture);
				//render with mask
				Data.CommandBuilder.Clear().SetRenderTarget(blurData.BlurTarget).ClearRenderTarget().DrawMesh(Data.QuadMesh, blurData.MaskMaterial).Execute();
			}
		}

		public override void OnDrawProcess(RenderTargetIdentifier combined)
		{
			if (!initialized)
			{
				base.OnDrawProcess(combined);
				Data.CommandBuilder.Clear().SetRenderTarget(GetTarget(RenderTarget.Input)).ClearRenderTarget().Execute();
				return;
			}

			Data.Material.SetTexture(Constants.PaintShader.InputTexture, blurData.BlurTexture);
			base.OnDrawProcess(combined);
			if (Data.IsPainted)
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
			
			if (Data.PaintMode.UsePaintInput)
			{
				Data.Material.SetTexture(Constants.PaintShader.PaintTexture, GetTexture(RenderTarget.ActiveLayer));
				Data.Material.SetTexture(Constants.PaintShader.InputTexture, blurData.BlurTexture);
				Data.CommandBuilder.Clear().LoadOrtho().SetRenderTarget(GetTarget(RenderTarget.ActiveLayerTemp)).DrawMesh(Data.QuadMesh, Data.Material, PaintPass.Blend).Execute();
			}
		}

		public override void OnBakeInputToLayer(RenderTargetIdentifier activeLayer)
		{
			base.OnBakeInputToLayer(activeLayer);
			Data.Material.SetTexture(Constants.PaintShader.InputTexture, GetTexture(RenderTarget.Input));
			Data.CommandBuilder.Clear().SetRenderTarget(GetTarget(RenderTarget.Input)).ClearRenderTarget().Execute();
		}

		[Serializable]
		private class BlurData
		{
			public Material BlurMaterial;
			public Material MaskMaterial;
			public RenderTexture BlurTexture;
			public RenderTargetIdentifier BlurTarget;
			public RenderTexture PreBlurTexture;
			public RenderTargetIdentifier PreBlurTarget;
			
			private IPaintData data;
		
			public void Enter(IPaintData paintData)
			{
				data = paintData;
			}
		
			public void Exit()
			{
				if (PreBlurTexture != null)
				{
					PreBlurTexture.ReleaseTexture();
				}
				if (BlurTexture != null)
				{
					BlurTexture.ReleaseTexture();
				}
				if (BlurMaterial != null)
				{
					Object.Destroy(BlurMaterial);
				}
				BlurMaterial = null;
				if (MaskMaterial != null)
				{
					Object.Destroy(MaskMaterial);
				}
				MaskMaterial = null;
			}
		
			public void InitMaterials()
			{
				if (MaskMaterial == null)
				{
					MaskMaterial = new Material(Settings.Instance.BrushBlurShader);
				}
				MaskMaterial.mainTexture = PreBlurTexture;
				MaskMaterial.SetTexture(Constants.BlurShader.MaskTexture, data.TexturesHelper.GetTexture(RenderTarget.Input));
				MaskMaterial.color = data.Brush.Color;
				if (BlurMaterial == null)
				{
					BlurMaterial = new Material(Settings.Instance.BlurShader);
				}
				BlurMaterial.mainTexture = BlurTexture;
			}
		}
    }
}