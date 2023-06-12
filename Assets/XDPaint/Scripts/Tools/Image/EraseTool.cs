using System;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using XDPaint.Core;
using XDPaint.Core.PaintModes;
using XDPaint.Tools.Image.Base;

namespace XDPaint.Tools.Image
{
	[Serializable]
	public class EraseTool : BasePaintTool
	{
		[Preserve] public EraseTool(IPaintData paintData) : base(paintData) { }

		public override PaintTool Type => PaintTool.Erase;
		public override bool DrawPreProcess => true;
		public override bool RenderToLayer => !Data.PaintMode.UsePaintInput;
		protected override PaintPass InputToPaintPass => PaintPass.Erase;

		public override void Enter()
		{
			base.Enter();
			Data.Render();
		}

		public override void Exit()
		{
			base.Exit();
			Data.Material.SetTexture(Constants.PaintShader.PaintTexture, GetTexture(RenderTarget.ActiveLayer));
			Data.Render();
		}

		public override void SetPaintMode(IPaintMode mode)
		{
			base.SetPaintMode(mode);
			if (mode.UsePaintInput)
			{
				RenderToInput = true;
				Data.Material.SetTexture(Constants.PaintShader.PaintTexture, GetTexture(RenderTarget.ActiveLayerTemp));
			}
			else
			{
				RenderToInput = false;
				Data.Material.SetTexture(Constants.PaintShader.PaintTexture, GetTexture(RenderTarget.ActiveLayer));
			}
		}
		
		public override void OnDrawPreProcess(RenderTargetIdentifier combined)
		{
			base.OnDrawPreProcess(combined);
			if (Data.PaintMode.UsePaintInput && Data.IsPainted)
			{
				Data.Material.SetTexture(Constants.PaintShader.PaintTexture, GetTexture(RenderTarget.ActiveLayer));
				Data.CommandBuilder.Clear().SetRenderTarget(GetTarget(RenderTarget.ActiveLayerTemp)).ClearRenderTarget().
					DrawMesh(Data.QuadMesh, Data.Material, PaintPass.Paint, PaintPass.Erase).Execute();
				Data.Material.SetTexture(Constants.PaintShader.PaintTexture, GetTexture(RenderTarget.ActiveLayerTemp));
			}
		}

		public override void OnBakeInputToLayer(RenderTargetIdentifier activeLayer)
		{
			base.OnBakeInputToLayer(activeLayer);
			Data.Material.SetTexture(Constants.PaintShader.PaintTexture, GetTexture(RenderTarget.ActiveLayer));
		}
	}
}