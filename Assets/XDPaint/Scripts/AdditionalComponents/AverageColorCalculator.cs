using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using XDPaint.Core;
using XDPaint.Tools;
using XDPaint.Utils;

namespace XDPaint.AdditionalComponents
{
	public class AverageColorCalculator : MonoBehaviour
	{
		public event Action<Color> OnGetAverageColor;

		public PaintManager PaintManager;
		public PaintRenderTexture PaintRenderTexture;
		public bool SkipTransparentPixels;
		
		private Material averageColorMaterial;
		private RenderTexture percentRenderTexture;
		private RenderTargetIdentifier rti;
		private CommandBufferBuilder commandBufferBuilder;
		private Mesh mesh;
		private int accuracy = 64;
		private const string SourceTextureParam = "_SourceTex";
		private const string AccuracyParam = "_Accuracy";

		#region MonoBehaviour Methods

		IEnumerator Start()
		{
			yield return null;
			Initialize();
		}

		void OnDestroy()
		{
			percentRenderTexture.ReleaseTexture();
			if (mesh != null)
			{
				Destroy(mesh);
			}
			if (commandBufferBuilder != null)
			{
				commandBufferBuilder.Release();
			}
			if (averageColorMaterial != null)
			{
				Destroy(averageColorMaterial);
				averageColorMaterial = null;
			}
		}

		void Update()
		{
			if (OnGetAverageColor != null && PaintManager.PaintObject.IsPainted)
			{
				UpdateAverageColor();
			}
		}

		#endregion

		private void Initialize()
		{
			if (averageColorMaterial == null)
			{
				if (SkipTransparentPixels)
				{
					averageColorMaterial = new Material(Settings.Instance.AverageColorCutOffShader);
					averageColorMaterial.SetTexture(SourceTextureParam, PaintManager.Material.SourceTexture);
				}
				else
				{
					averageColorMaterial = new Material(Settings.Instance.AverageColorShader);
				}
				SetAccuracy(accuracy);
			}
			averageColorMaterial.mainTexture = PaintRenderTexture == PaintRenderTexture.PaintTexture
				? PaintManager.GetPaintTexture()
				: PaintManager.GetResultRenderTexture();
			commandBufferBuilder?.Release();
			commandBufferBuilder = new CommandBufferBuilder("AverageColor");
			percentRenderTexture = RenderTextureFactory.CreateRenderTexture(1, 1);
			rti = new RenderTargetIdentifier(percentRenderTexture);
			mesh = MeshGenerator.GenerateQuad(Vector3.one, Vector3.zero);
		}

		/// <summary>
		/// Calculates average color
		/// </summary>
		private void CalcAverageColor()
		{
			var prevRenderTextureT = RenderTexture.active;
			RenderTexture.active = percentRenderTexture;
			var averageColorTexture = new Texture2D(percentRenderTexture.width, percentRenderTexture.height, TextureFormat.ARGB32, false, true);
			averageColorTexture.ReadPixels(new Rect(0, 0, percentRenderTexture.width, percentRenderTexture.height), 0, 0);
			averageColorTexture.Apply();
			RenderTexture.active = prevRenderTextureT;
			var averageColor = averageColorTexture.GetPixel(0, 0);
			OnGetAverageColor?.Invoke(averageColor);
		}

		private void UpdateAverageColor()
		{
			commandBufferBuilder.LoadOrtho().Clear().SetRenderTarget(rti).ClearRenderTarget(Constants.Color.ClearBlack).DrawMesh(mesh, averageColorMaterial).Execute();
			CalcAverageColor();
		}
		
		public void SetAccuracy(int value)
		{
			accuracy = value;
			if (averageColorMaterial != null)
			{
				averageColorMaterial.SetInt(AccuracyParam, accuracy);
			}
		}
	}
}