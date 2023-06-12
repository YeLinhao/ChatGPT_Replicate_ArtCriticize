﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using XDPaint.Utils;

namespace XDPaint.Core
{
	public class RenderTextureHelper : IRenderTextureHelper
	{
		private Dictionary<RenderTarget, KeyValuePair<RenderTexture, RenderTargetIdentifier>> renderTexturesData;

		/// <summary>
		/// Creates 3 RenderTextures:
		/// PaintInput - for paint between using Input down and up events (AdditivePaintMode) or for current frame paint result storing (DefaultPaintMode);
		/// Combined - for combining source texture with paint textures and for brush preview;
		/// CombinedTemp - for combining layers.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="filterMode"></param>
		public void Init(int width, int height, FilterMode filterMode)
		{
			renderTexturesData = new Dictionary<RenderTarget, KeyValuePair<RenderTexture, RenderTargetIdentifier>>();
			if (!renderTexturesData.ContainsKey(RenderTarget.ActiveLayerTemp))
			{
				renderTexturesData.Add(RenderTarget.ActiveLayerTemp, default);
			}
			
			if (!renderTexturesData.ContainsKey(RenderTarget.Input))
			{
				var paintInput = RenderTextureFactory.CreateRenderTexture(width, height, 0, RenderTextureFormat.ARGB32, filterMode);
				paintInput.name = "Input";
				renderTexturesData.Add(RenderTarget.Input, new KeyValuePair<RenderTexture, RenderTargetIdentifier>(paintInput, new RenderTargetIdentifier(paintInput)));
			}

			if (!renderTexturesData.ContainsKey(RenderTarget.Combined))
			{
				var combined = RenderTextureFactory.CreateRenderTexture(width, height, 0, RenderTextureFormat.ARGB32, filterMode);
				combined.name = "Combined";
				renderTexturesData.Add(RenderTarget.Combined, new KeyValuePair<RenderTexture, RenderTargetIdentifier>(combined, new RenderTargetIdentifier(combined)));
			}
			
			if (!renderTexturesData.ContainsKey(RenderTarget.CombinedTemp))
			{
				renderTexturesData.Add(RenderTarget.CombinedTemp, default);
			}
		}

		public void CreateActiveLayerTempRenderTexture()
		{
			if (renderTexturesData[RenderTarget.ActiveLayerTemp].Equals(default(KeyValuePair<RenderTexture, RenderTargetIdentifier>)))
			{
				var texture = renderTexturesData[RenderTarget.Combined].Key;
				var activeLayerTemp = RenderTextureFactory.CreateRenderTexture(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, texture.filterMode);
				activeLayerTemp.name = "ActiveLayerTemp";
				renderTexturesData[RenderTarget.ActiveLayerTemp] = new KeyValuePair<RenderTexture, RenderTargetIdentifier>(activeLayerTemp, new RenderTargetIdentifier(activeLayerTemp));
			}
		}

		public void ReleaseActiveLayerTempRenderTexture()
		{
			renderTexturesData[RenderTarget.ActiveLayerTemp].Key.ReleaseTexture();
			renderTexturesData[RenderTarget.ActiveLayerTemp] = default;
		}

		public void CreateCombinedTempRenderTexture()
		{
			if (renderTexturesData[RenderTarget.CombinedTemp].Equals(default(KeyValuePair<RenderTexture, RenderTargetIdentifier>)))
			{
				ReleaseCombinedTempRenderTexture();
				var texture = renderTexturesData[RenderTarget.Combined].Key;
				var combinedTemp = RenderTextureFactory.CreateRenderTexture(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, texture.filterMode);
				combinedTemp.name = "CombinedTemp";
				renderTexturesData[RenderTarget.CombinedTemp] = new KeyValuePair<RenderTexture, RenderTargetIdentifier>(combinedTemp, new RenderTargetIdentifier(combinedTemp));
			}
		}

		public void ReleaseCombinedTempRenderTexture()
		{
			renderTexturesData[RenderTarget.CombinedTemp].Key.ReleaseTexture();
			renderTexturesData[RenderTarget.CombinedTemp] = default;
		}

		public void DoDispose()
		{
			ReleaseRT(RenderTarget.Input);
			ReleaseRT(RenderTarget.Combined);
			ReleaseRT(RenderTarget.CombinedTemp);
			ReleaseRT(RenderTarget.ActiveLayerTemp);
		}

		public RenderTargetIdentifier GetTarget(RenderTarget target)
		{
			return renderTexturesData[target].Value;
		}

		public RenderTexture GetTexture(RenderTarget target)
		{
			return renderTexturesData[target].Key;
		}

		private void ReleaseRT(RenderTarget target)
		{
			if (renderTexturesData != null && renderTexturesData.ContainsKey(target))
			{
				renderTexturesData[target].Key.ReleaseTexture();
				renderTexturesData.Remove(target);
			}
		}
	}
}