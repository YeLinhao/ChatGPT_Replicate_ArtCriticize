using System;
using System.Linq;
using UnityEngine;
using XDPaint.Core;
#if XDP_DEBUG
using XDPaint.Tools.Image;
#endif
using XDPaint.Tools.Image.Base;
using IDisposable = XDPaint.Core.IDisposable;

namespace XDPaint.Tools
{
	[Serializable]
	public class ToolsManager : IDisposable
	{
		public IPaintTool CurrentTool => currentTool;
		private IPaintTool[] allTools;
		private IPaintTool currentTool;
		private PaintManager paintManager;
		private bool initialized;
		
#if XDP_DEBUG
#pragma warning disable 414
		[SerializeField] private BrushTool brushTool;
		[SerializeField] private EraseTool eraseTool;
		[SerializeField] private EyedropperTool eyedropperTool;
		[SerializeField] private BrushSamplerTool brushSamplerTool;
		[SerializeField] private CloneTool cloneTool;
		[SerializeField] private BlurTool blurTool;
		[SerializeField] private GaussianBlurTool gaussianBlurTool;
		[SerializeField] private GrayscaleTool grayscaleTool;
#pragma warning restore 414
#endif

		public ToolsManager(PaintTool paintTool, IPaintData paintData)
		{
			var tools = AppDomain.CurrentDomain.GetAssemblies().SelectMany(
				s => s.GetTypes()).Where(p => typeof(IPaintTool).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);
			var toolsArray = tools.ToArray();
			allTools = new IPaintTool[toolsArray.Length];
			for (var i = 0; i < toolsArray.Length; i++)
			{
				var tool = toolsArray[i];
				var toolInstance = Activator.CreateInstance(tool, paintData) as IPaintTool;
				allTools[i] = toolInstance;
			}
			currentTool = allTools.First(x => x.Type == paintTool);
			currentTool.Enter();
#if XDP_DEBUG
			brushTool = allTools.First(x => x.Type == PaintTool.Brush) as BrushTool;
			eraseTool = allTools.First(x => x.Type == PaintTool.Erase) as EraseTool;
			eyedropperTool = allTools.First(x => x.Type == PaintTool.Eyedropper) as EyedropperTool;
			brushSamplerTool = allTools.First(x => x.Type == PaintTool.BrushSampler) as BrushSamplerTool;
			cloneTool = allTools.First(x => x.Type == PaintTool.Clone) as CloneTool;
			blurTool = allTools.First(x => x.Type == PaintTool.Blur) as BlurTool;
			gaussianBlurTool = allTools.First(x => x.Type == PaintTool.BlurGaussian) as GaussianBlurTool;
			grayscaleTool = allTools.First(x => x.Type == PaintTool.Grayscale) as GrayscaleTool;
#endif
		}

		public void Init(PaintManager thisPaintManager)
		{
			paintManager = thisPaintManager;
			paintManager.PaintObject.OnMouseHoverHandler += OnMouseHover;
			paintManager.PaintObject.OnMouseDownHandler += OnMouseDown;
			paintManager.PaintObject.OnMouseHandler += OnMouse;
			paintManager.PaintObject.OnMouseUpHandler += OnMouseUp;
			initialized = true;
		}

		public void DoDispose()
		{
			if (!initialized)
				return;
			
			paintManager.PaintObject.OnMouseHoverHandler -= OnMouseHover;
			paintManager.PaintObject.OnMouseDownHandler -= OnMouseDown;
			paintManager.PaintObject.OnMouseHandler -= OnMouse;
			paintManager.PaintObject.OnMouseUpHandler -= OnMouseUp;
			
			foreach (var tool in allTools)
			{
				if (currentTool == tool)
				{
					tool.Exit();
				}
				tool.DoDispose();
			}
			allTools = null;
			initialized = false;
		}

		public void SetTool(PaintTool newTool)
		{
			foreach (var tool in allTools)
			{
				if (tool.Type == newTool)
				{
					currentTool.Exit();
					currentTool = tool;
					currentTool.Enter();
					break;
				}
			}
		}

		private void OnMouseHover(Vector3 localPosition, Vector2 screenPosition, Vector2 uv, Vector2 paintPosition, float pressure)
		{
			currentTool.UpdateHover(localPosition, screenPosition, uv, paintPosition, pressure);
		}

		private void OnMouseDown(Vector3 localPosition, Vector2 screenPosition, Vector2 uv, Vector2 paintPosition, float pressure)
		{
			currentTool.UpdateDown(localPosition, screenPosition, uv, paintPosition, pressure);
		}
		
		private void OnMouse(Vector3 localPosition, Vector2 screenPosition, Vector2 uv, Vector2 paintPosition, float pressure)
		{
			currentTool.UpdatePress(localPosition, screenPosition, uv, paintPosition, pressure);
		}
		
		private void OnMouseUp(Vector2 screenPosition, bool inBounds)
		{
			currentTool.UpdateUp(screenPosition, inBounds);
		}
	}
}