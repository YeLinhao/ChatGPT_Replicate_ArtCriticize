using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using XDPaint.Core;
using XDPaint.Core.Materials;
using XDPaint.Core.PaintModes;
using XDPaint.Tools;
using XDPaint.Utils;

namespace XDPaint.Controllers
{
	public class PaintController : Singleton<PaintController>
	{   
		public bool OverrideCamera;

		[SerializeField] private Camera currentCamera;
		public Camera Camera
		{
			get
			{
				if (currentCamera == null)
				{
					currentCamera = Camera.main;
				}
				return currentCamera;
			}
			set
			{
				currentCamera = value;
				if (InputController.Instance != null)
				{
					InputController.Instance.Camera = currentCamera;
				}
				if (RaycastController.Instance != null)
				{
					RaycastController.Instance.Camera = currentCamera;
				}
				if (initialized)
				{
					foreach (var paintManager in paintManagers)
					{
						paintManager.PaintObject.Camera = currentCamera;
					}
				}
			}
		}
		
		private List<IPaintMode> paintModes;
		[SerializeField] private PaintMode paintModeType;
		[SerializeField] private bool useSharedSettings = true;
		public bool UseSharedSettings
		{
			get => useSharedSettings;
			set
			{
				useSharedSettings = value;
				if (!initialized)
					return;
				
				if (useSharedSettings)
				{
					foreach (var paintManager in paintManagers)
					{
						if (paintManager == null)
							continue;
						paintManager.Brush = brush;
						paintManager.Tool = paintTool;
						paintManager.SetPaintMode(paintModeType);
					}
				}
				else
				{
					foreach (var paintManager in paintManagers)
					{
						if (paintManager == null)
							continue;
						paintManager.InitBrush();
						paintManager.Tool = paintTool;
					}
				}
			}
		}
		
		public PaintMode PaintMode
		{
			get => paintModeType;
			set
			{
				var previousModeType = paintModeType;
				paintModeType = value;
				mode = GetPaintMode(paintModeType);
				if (Application.isPlaying && paintModeType != previousModeType && useSharedSettings)
				{
					foreach (var paintManager in paintManagers)
					{
						if (paintManager == null)
							continue;
						paintManager.SetPaintMode(paintModeType);
					}
				}
			}
		}

		[SerializeField] private PaintTool paintTool;
		public PaintTool Tool
		{
			get => paintTool;
			set
			{
				paintTool = value;
				if (initialized && useSharedSettings)
				{
					foreach (var paintManager in paintManagers)
					{
						if (paintManager == null)
							continue;
						paintManager.Tool = paintTool;
					}
				}
			}
		}

		[SerializeField] private Brush brush = new Brush();
		public Brush Brush
		{
			get => brush;
			set => brush.SetValues(value);
		}

		private List<PaintManager> paintManagers;
		private IPaintMode mode;
		private bool initialized;

		private new void Awake()
		{
			base.Awake();
			paintManagers = new List<PaintManager>();
			CreatePaintModes();
			Init();
		}

		private void CreatePaintModes()
		{
			if (paintModes == null)
			{
				paintModes = new List<IPaintMode>();
				var type = typeof(IPaintMode);
				var types = AppDomain.CurrentDomain.GetAssemblies()
					.SelectMany(s => s.GetTypes())
					.Where(p => type.IsAssignableFrom(p) && p.IsClass);
				foreach (var modeType in types)
				{
					var paintMode = Activator.CreateInstance(modeType) as IPaintMode;
					paintModes.Add(paintMode);
				}
			}
		}

		private void Init()
		{
			if (Application.isPlaying && !initialized)
			{
				mode = GetPaintMode(paintModeType);
				if (brush.SourceTexture == null)
				{
					brush.SourceTexture = Settings.Instance.DefaultBrush;
				}
				brush.Init(mode);
				brush.SetPaintMode(mode);
				brush.SetPaintTool(paintTool);
				initialized = true;
			}
		}

		public IPaintMode GetPaintMode(PaintMode paintMode)
		{
			if (paintModes == null)
			{
				CreatePaintModes();
			}
			return paintModes.FirstOrDefault(x => x.PaintMode == paintMode);
		}

		public void RegisterPaintManager(PaintManager paintManager)
		{
			UnRegisterPaintManager(paintManager);
			paintManagers.Add(paintManager);
		}

		public void UnRegisterPaintManager(PaintManager paintManager)
		{
			if (paintManagers.Contains(paintManager))
			{
				paintManagers.Remove(paintManager);
			}
		}

		private async void OnDestroy()
		{
			if (useSharedSettings && paintManagers.Count > 0)
			{
				while (paintManagers.Count > 0)
				{
					await Task.Yield();
				}
			}
			brush.DoDispose();
		}

		public PaintManager[] ActivePaintManagers()
		{
			return paintManagers?.Where(paintManager => paintManager != null && paintManager.gameObject.activeInHierarchy && paintManager.enabled && paintManager.Initialized).ToArray();
		}

		public PaintManager[] AllPaintManagers()
		{
			return paintManagers?.Where(paintManager => paintManager != null).ToArray();
		}
	}
}