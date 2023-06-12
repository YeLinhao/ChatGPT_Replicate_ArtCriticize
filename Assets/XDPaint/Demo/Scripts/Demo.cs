using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XDPaint.Controllers;
using XDPaint.Core;
using XDPaint.Demo.UI;
using XDPaint.Tools;
using XDPaint.Tools.Image;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace XDPaint.Demo
{
	public class Demo : MonoBehaviour
	{
		[Serializable]
		public class PaintManagersData
		{
			public PaintManager PaintManager;
			public string Text;
		}
		
		[Serializable]
		public class PaintItem
		{
			public Image Image;
			public Button Button;
		}
		
		[SerializeField] private PaintManagersData[] paintManagers;
		[SerializeField] private CameraMover cameraMover;
		[SerializeField] private Camera mainCamera;
		[SerializeField] private bool loadPrefs = true;

		[Header("Tutorial")]
		[SerializeField] private GameObject tutorialObject;
		[SerializeField] private EventTrigger tutorial;
		[SerializeField] private Button tutorialButton;
		
		[Header("Top panel")]
		[SerializeField] private ToolToggle[] toolsToggles; 
		[SerializeField] private UIDoubleClick brushDoubleClick;
		[SerializeField] private UIDoubleClick brushToolDoubleClick;
		[SerializeField] private UIDoubleClick eraseToolDoubleClick;
		[SerializeField] private UIDoubleClick blurToolDoubleClick;
		[SerializeField] private UIDoubleClick gaussianBlurToolDoubleClick;
		[SerializeField] private Toggle rotateToggle;
		[SerializeField] private Toggle playPauseToggle;
		[SerializeField] private RawImage brushPreview;
		[SerializeField] private RectTransform brushPreviewTransform;
		[SerializeField] private EventTrigger topPanel;
		[SerializeField] private EventTrigger colorPalette;
		[SerializeField] private EventTrigger brushesPanel;
		[SerializeField] private EventTrigger blurPanel;
		[SerializeField] private EventTrigger gaussianBlurPanel;
		[SerializeField] private Slider blurSlider;
		[SerializeField] private Slider gaussianBlurSlider;
		[SerializeField] private PaintItem[] colors;
		[SerializeField] private PaintItem[] brushes;

		[Header("Left panel")]
		[SerializeField] private Slider opacitySlider;
		[SerializeField] private Slider brushSizeSlider;
		[SerializeField] private Slider hardnessSlider;
		[SerializeField] private Button undoButton;
		[SerializeField] private Button redoButton;
		[SerializeField] private EventTrigger rightPanel;
		
		[Header("Right panel")]
		[SerializeField] private LayersUIController layersUI;

		[Header("Bottom panel")]
		[SerializeField] private Button nextButton;
		[SerializeField] private Button previousButton;
		[SerializeField] private Text bottomPanelText;
		[SerializeField] private EventTrigger bottomPanel;

		[SerializeField] private EventTrigger allArea;
		
		private EventTrigger.Entry tutorialClick;
		private EventTrigger.Entry hoverEnter;
		private EventTrigger.Entry hoverExit;
		private EventTrigger.Entry onDown;
		private PaintManager PaintManager => paintManagers[currentPaintManagerId].PaintManager;
		private Texture selectedTexture;
		private Animator paintManagerAnimator;
		private PaintTool previousTool;
		private int currentPaintManagerId;
		private bool previousCameraMoverState;
		
		private const int TutorialShowCount = 3;
		
		void Awake()
		{
#if !UNITY_WEBGL
			Application.targetFrameRate = Mathf.Clamp(Screen.currentResolution.refreshRate, 30, Screen.currentResolution.refreshRate);
#endif
			if (mainCamera == null)
			{
				mainCamera = Camera.main;
			}

			selectedTexture = Settings.Instance.DefaultBrush;
			PreparePaintManagers();

			for (var i = 0; i < paintManagers.Length; i++)
			{
				var manager = paintManagers[i];
				var active = i == 0;
				manager.PaintManager.gameObject.SetActive(active);
			}

			PaintManager.OnInitialized += OnInitialized;
			
			//tutorial
			tutorialClick = new EventTrigger.Entry {eventID = EventTriggerType.PointerClick};
			tutorialClick.callback.AddListener(ShowStartTutorial);				
			tutorial.triggers.Add(tutorialClick);
			var tutorialShowsCount = PlayerPrefs.GetInt("XDPaintDemoTutorialShowsCount", 0);
			if (tutorialShowsCount < TutorialShowCount)
			{
				if (playPauseToggle.interactable)
				{
					OnPlayPause(true);
				}
				tutorialObject.gameObject.SetActive(true);
				InputController.Instance.enabled = false;
			}
			else
			{
				OnTutorial(false);
			}
		}

		private IEnumerator Start()
		{
			yield return null;
			
			hoverEnter = new EventTrigger.Entry {eventID = EventTriggerType.PointerEnter};
			hoverEnter.callback.AddListener(HoverEnter);
			hoverExit = new EventTrigger.Entry {eventID = EventTriggerType.PointerExit};
			hoverExit.callback.AddListener(HoverExit);
			
			//top panel
			tutorialButton.onClick.AddListener(ShowTutorial);
			brushToolDoubleClick.OnDoubleClick.AddListener(OpenBrushPanel);
			eraseToolDoubleClick.OnDoubleClick.AddListener(OpenBrushPanel);
			blurToolDoubleClick.OnDoubleClick.AddListener(OpenBlurPanel);
			gaussianBlurToolDoubleClick.OnDoubleClick.AddListener(OpenGaussianBlurPanel);
			rotateToggle.onValueChanged.AddListener(SetRotateMode);
			playPauseToggle.onValueChanged.AddListener(OnPlayPause);
			brushDoubleClick.OnDoubleClick.AddListener(OpenColorPalette);
			topPanel.triggers.Add(hoverEnter);
			topPanel.triggers.Add(hoverExit);
			colorPalette.triggers.Add(hoverEnter);
			colorPalette.triggers.Add(hoverExit);
			brushesPanel.triggers.Add(hoverEnter);
			brushesPanel.triggers.Add(hoverExit);
			blurPanel.triggers.Add(hoverEnter);
			blurPanel.triggers.Add(hoverExit);
			blurSlider.onValueChanged.AddListener(OnBlurSlider);
			gaussianBlurPanel.triggers.Add(hoverEnter);
			gaussianBlurPanel.triggers.Add(hoverExit);
			gaussianBlurSlider.onValueChanged.AddListener(OnGaussianBlurSlider);
			
			brushSizeSlider.value = PaintController.Instance.Brush.Size;
			hardnessSlider.value = PaintController.Instance.Brush.Hardness;
			opacitySlider.value = PaintController.Instance.Brush.Color.a;

			//right panel
			opacitySlider.onValueChanged.AddListener(OnOpacitySlider);
			brushSizeSlider.onValueChanged.AddListener(OnBrushSizeSlider);
			hardnessSlider.onValueChanged.AddListener(OnHardnessSlider);
			undoButton.onClick.AddListener(OnUndo);
			redoButton.onClick.AddListener(OnRedo);
			rightPanel.triggers.Add(hoverEnter);
			rightPanel.triggers.Add(hoverExit);
			
			//bottom panel
			nextButton.onClick.AddListener(SwitchToNextPaintManager);
			previousButton.onClick.AddListener(SwitchToPreviousPaintManager);
			bottomPanel.triggers.Add(hoverEnter);
			bottomPanel.triggers.Add(hoverExit);	
			
			onDown = new EventTrigger.Entry {eventID = EventTriggerType.PointerDown};
			onDown.callback.AddListener(ResetPlates);
			allArea.triggers.Add(onDown);

			//colors
			foreach (var colorItem in colors)
			{
				colorItem.Button.onClick.AddListener(delegate { ColorClick(colorItem.Image.color); });
			}
			
			//brushes
			for (var i = 0; i < brushes.Length; i++)
			{
				var brushItem = brushes[i];
				var brushId = i;
				brushItem.Button.onClick.AddListener(delegate { BrushClick(brushItem.Image.mainTexture, brushId); });
			}

			if (loadPrefs)
			{
				LoadPrefs();
			}
			
			foreach (var toggle in toolsToggles)
			{
				toggle.Toggle.enabled = true;
			}
		}

		private void OnDestroy()
		{
			tutorialClick.callback.RemoveListener(ShowStartTutorial);				
			tutorial.triggers.Remove(tutorialClick);
			hoverEnter.callback.RemoveListener(HoverEnter);
			hoverExit.callback.RemoveListener(HoverExit);
			tutorialButton.onClick.RemoveListener(ShowTutorial);
			brushToolDoubleClick.OnDoubleClick.RemoveListener(OpenBrushPanel);
			eraseToolDoubleClick.OnDoubleClick.RemoveListener(OpenBrushPanel);
			blurToolDoubleClick.OnDoubleClick.RemoveListener(OpenBlurPanel);
			gaussianBlurToolDoubleClick.OnDoubleClick.RemoveListener(OpenGaussianBlurPanel);
			rotateToggle.onValueChanged.RemoveListener(SetRotateMode);
			playPauseToggle.onValueChanged.RemoveListener(OnPlayPause);
			brushDoubleClick.OnDoubleClick.RemoveListener(OpenColorPalette);
			topPanel.triggers.Remove(hoverEnter);
			topPanel.triggers.Remove(hoverExit);
			colorPalette.triggers.Remove(hoverEnter);
			colorPalette.triggers.Remove(hoverExit);
			brushesPanel.triggers.Remove(hoverEnter);
			brushesPanel.triggers.Remove(hoverExit);
			blurPanel.triggers.Remove(hoverEnter);
			blurPanel.triggers.Remove(hoverExit);
			blurSlider.onValueChanged.RemoveListener(OnBlurSlider);
			gaussianBlurPanel.triggers.Remove(hoverEnter);
			gaussianBlurPanel.triggers.Remove(hoverExit);
			gaussianBlurSlider.onValueChanged.RemoveListener(OnGaussianBlurSlider);
			opacitySlider.onValueChanged.RemoveListener(OnOpacitySlider);
			brushSizeSlider.onValueChanged.RemoveListener(OnBrushSizeSlider);
			hardnessSlider.onValueChanged.RemoveListener(OnHardnessSlider);
			undoButton.onClick.RemoveListener(OnUndo);
			redoButton.onClick.RemoveListener(OnRedo);
			rightPanel.triggers.Remove(hoverEnter);
			rightPanel.triggers.Remove(hoverExit);
			nextButton.onClick.RemoveListener(SwitchToNextPaintManager);
			previousButton.onClick.RemoveListener(SwitchToPreviousPaintManager);
			bottomPanel.triggers.Remove(hoverEnter);
			bottomPanel.triggers.Remove(hoverExit);	
			onDown.callback.RemoveListener(ResetPlates);
			allArea.triggers.Remove(onDown);
			foreach (var colorItem in colors)
			{
				colorItem.Button.onClick.RemoveListener(delegate { ColorClick(colorItem.Image.color); });
			}
			for (var i = 0; i < brushes.Length; i++)
			{
				var brushItem = brushes[i];
				var brushId = i;
				brushItem.Button.onClick.RemoveListener(delegate { BrushClick(brushItem.Image.mainTexture, brushId); });
			}
		}
		
		public void DisableStates()
		{
			if (PaintManager != null && PaintManager.Initialized)
			{
				PaintManager.StatesController.Disable();
			}
		}

		public void EnableStates()
		{
			if (PaintManager != null && PaintManager.Initialized)
			{
				PaintManager.StatesController.Enable();
			}
		}

		private void OnInitialized(PaintManager paintManagerInstance)
		{
			//undo/redo status
			if (paintManagerInstance.StatesController != null)
			{
				paintManagerInstance.StatesController.OnUndoStatusChanged += OnUndoStatusChanged;
				paintManagerInstance.StatesController.OnRedoStatusChanged += OnRedoStatusChanged;
			}
			if (PaintController.Instance.UseSharedSettings)
			{
				PaintController.Instance.Brush.OnColorChanged += OnBrushColorChanged;
			}
			else
			{
				PaintManager.Brush.OnColorChanged += OnBrushColorChanged;
			}
			brushPreview.texture = PaintController.Instance.UseSharedSettings 
				? PaintController.Instance.Brush.RenderTexture 
				: PaintManager.Brush.RenderTexture;
			foreach (var toolToggle in toolsToggles)
			{
				toolToggle.SetPaintManager(paintManagerInstance);
			}
			layersUI.SetLayersController(paintManagerInstance.LayersController);
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
			PaintController.Instance.Brush.Preview = false;
#endif
		}

		private void OnBrushColorChanged(Color color)
		{
			opacitySlider.value = color.a;
		}

		private void LoadPrefs()
		{
			//tool
			/*var tool = (PaintTool)PlayerPrefs.GetInt("XDPaintDemoTool");
			foreach (var toggle in toolsToggles)
			{
				if (toggle.Tool == tool)
				{
					toggle.Toggle.isOn = true;
					PaintManager.Tool = toggle.Tool;
					break;
				}
			}*/
			//brush id
			var brushId = PlayerPrefs.GetInt("XDPaintDemoBrushId");
			PaintController.Instance.Brush.SetTexture(brushes[brushId].Image.mainTexture, true, false);
			selectedTexture = brushes[brushId].Image.mainTexture;
			//opacity
			opacitySlider.value = PlayerPrefs.GetFloat("XDPaintDemoBrushOpacity", 1f);
			//size
			brushSizeSlider.value = PlayerPrefs.GetFloat("XDPaintDemoBrushSize", 1f);
			//hardness
			hardnessSlider.value = PlayerPrefs.GetFloat("XDPaintDemoBrushHardness", 1f);
			//color
			ColorUtility.TryParseHtmlString("#" + PlayerPrefs.GetString("XDPaintDemoBrushColor", "#FFFFFF"), out var color);
			ColorClick(color);
		}

		private void ShowStartTutorial(BaseEventData eventData)
		{
			var tutorialShowsCount = PlayerPrefs.GetInt("XDPaintDemoTutorialShowsCount", 0);
			PlayerPrefs.SetInt("XDPaintDemoTutorialShowsCount", tutorialShowsCount + 1);
			OnTutorial(false);
		}

		private void ShowTutorial()
		{
			OnTutorial(true);
		}

		private void OnTutorial(bool showTutorial)
		{
			tutorialObject.gameObject.SetActive(showTutorial);
			if (playPauseToggle.interactable)
			{
				OnPlayPause(showTutorial);
				if (!showTutorial)
				{
					playPauseToggle.isOn = false;
				}
			}
			InputController.Instance.enabled = !showTutorial;
			if (showTutorial)
			{
				layersUI.SetNextPlateState();
				previousCameraMoverState = cameraMover.enabled;
				SetRotateMode(false);
			}
			else
			{
				SetRotateMode(previousCameraMoverState);
			}
		}

		private void PreparePaintManagers()
		{
			for (var i = 0; i < paintManagers.Length; i++)
			{
				paintManagers[i].PaintManager.gameObject.SetActive(i == currentPaintManagerId);
				if (paintManagerAnimator == null)
				{
					if (paintManagers[i].PaintManager.ObjectForPainting.TryGetComponent<SkinnedMeshRenderer>(out _))
					{
						var animator = paintManagers[i].PaintManager.GetComponentInChildren<Animator>(true);
						if (animator != null)
						{
							paintManagerAnimator = animator;
						}
					}
				}
			}
		}

		private void OpenColorPalette(float value)
		{
			brushesPanel.gameObject.SetActive(false);
			blurPanel.gameObject.SetActive(false);
			gaussianBlurPanel.gameObject.SetActive(false);
			colorPalette.gameObject.SetActive(!colorPalette.gameObject.activeInHierarchy);
		}

		private void OpenBrushPanel(float xPosition)
		{
			colorPalette.gameObject.SetActive(false);
			blurPanel.gameObject.SetActive(false);
			gaussianBlurPanel.gameObject.SetActive(false);
			brushesPanel.gameObject.SetActive(true);
			var brushesPanelTransform = brushesPanel.transform;
			brushesPanelTransform.position = new Vector3(xPosition, brushesPanelTransform.position.y, brushesPanelTransform.position.z);
		}
		
		private void OpenBlurPanel(float xPosition)
		{
			colorPalette.gameObject.SetActive(false);
			brushesPanel.gameObject.SetActive(false);
			gaussianBlurPanel.gameObject.SetActive(false);
			blurPanel.gameObject.SetActive(true);
			var blurPanelTransform = blurPanel.transform;
			blurPanelTransform.position = new Vector3(xPosition, blurPanelTransform.position.y, blurPanelTransform.position.z);
		}
		
		private void OpenGaussianBlurPanel(float xPosition)
		{
			colorPalette.gameObject.SetActive(false);
			brushesPanel.gameObject.SetActive(false);
			blurPanel.gameObject.SetActive(false);
			gaussianBlurPanel.gameObject.SetActive(true);
			var blurPanelTransform = gaussianBlurPanel.transform;
			blurPanelTransform.position = new Vector3(xPosition, blurPanelTransform.position.y, blurPanelTransform.position.z);
		}

		private void SetRotateMode(bool isOn)
		{
			cameraMover.enabled = isOn;
			if (isOn && PaintManager != null && PaintManager.Initialized)
			{
				PaintManager.PaintObject.FinishPainting();
			}
			InputController.Instance.enabled = !isOn;
		}

		private void OnPlayPause(bool isOn)
		{
			if (paintManagerAnimator != null)
			{
				paintManagerAnimator.enabled = !isOn;
			}
		}

		private void OnOpacitySlider(float value)
		{
			var color = Color.white;
			if (PaintController.Instance.UseSharedSettings)
			{
				color = PaintController.Instance.Brush.Color;
			}
			else if (PaintManager != null && PaintManager.Initialized)
			{
				color = PaintManager.Brush.Color;
			}
			color.a = value;
			if (PaintController.Instance.UseSharedSettings)
			{
				PaintController.Instance.Brush.SetColor(color);
			}
			else if (PaintManager != null && PaintManager.Initialized)
			{
				PaintManager.Brush.SetColor(color);
			}
			PlayerPrefs.SetFloat("XDPaintDemoBrushOpacity", value);
		}
		
		private void OnBrushSizeSlider(float value)
		{
			if (PaintController.Instance.UseSharedSettings)
			{
				PaintController.Instance.Brush.Size = value;
			}
			else if (PaintManager != null && PaintManager.Initialized)
			{
				PaintManager.Brush.Size = value;
			}
			brushPreviewTransform.localScale = Vector3.one * value;
			PlayerPrefs.SetFloat("XDPaintDemoBrushSize", value);
		}

		private void OnHardnessSlider(float value)
		{
			if (PaintController.Instance.UseSharedSettings)
			{
				PaintController.Instance.Brush.Hardness = value;
			}
			else if (PaintManager != null && PaintManager.Initialized)
			{
				PaintManager.Brush.Hardness = value;
			}
			PlayerPrefs.SetFloat("XDPaintDemoBrushHardness", value);
		}
		
		private void OnBlurSlider(float value)
		{
			if (PaintManager.ToolsManager.CurrentTool is BlurTool blurTool)
			{
				blurTool.Iterations = Mathf.RoundToInt(1f + value * 4f);
				blurTool.BlurStrength = 0.01f + value * 4.99f;
			}
		}
		
		private void OnGaussianBlurSlider(float value)
		{
			if (PaintManager.ToolsManager.CurrentTool is GaussianBlurTool blurTool)
			{
				blurTool.KernelSize = Mathf.RoundToInt(3f + value * 4f);
				blurTool.Spread = 0.01f + value * 4.99f;
			}
		}
		
		private void OnUndo()
		{
			if (PaintManager.StatesController != null && PaintManager.StatesController.CanUndo())
			{
				PaintManager.StatesController.Undo();
				PaintManager.Render();
			}
		}
		
		private void OnRedo()
		{
			if (PaintManager.StatesController != null && PaintManager.StatesController.CanRedo())
			{
				PaintManager.StatesController.Redo();
				PaintManager.Render();
			}
		}

		private void SwitchToNextPaintManager()
		{
			SwitchPaintManager(true);
		}

		private void SwitchToPreviousPaintManager()
		{
			SwitchPaintManager(false);
		}
		
		private void SwitchPaintManager(bool switchToNext)
		{
			PaintManager.gameObject.SetActive(false);
			if (PaintManager.StatesController != null)
			{
				PaintManager.StatesController.OnUndoStatusChanged -= OnUndoStatusChanged;
				PaintManager.StatesController.OnRedoStatusChanged -= OnRedoStatusChanged;
			}
			if (PaintController.Instance.UseSharedSettings)
			{
				PaintController.Instance.Brush.OnColorChanged -= OnBrushColorChanged;
			}
			else
			{
				PaintManager.Brush.OnColorChanged -= OnBrushColorChanged;
			}
			PaintManager.DoDispose();
			if (switchToNext)
			{
				currentPaintManagerId = (currentPaintManagerId + 1) % paintManagers.Length;
			}
			else
			{
				currentPaintManagerId--;
				if (currentPaintManagerId < 0)
				{
					currentPaintManagerId = paintManagers.Length - 1;
				}
			}
			toolsToggles.First(x => x.Tool == PaintTool.Brush).Toggle.isOn = true;
			PaintManager.gameObject.SetActive(true);
			PaintManager.OnInitialized -= OnInitialized;
			PaintManager.OnInitialized += OnInitialized;
			PaintManager.Init();
			PaintManager.Tool = PaintTool.Brush;
			PaintManager.Brush.SetTexture(selectedTexture);
			cameraMover.ResetCamera();
			UpdateButtons();
		}

		private void OnRedoStatusChanged(bool canRedo)
		{
			redoButton.interactable = canRedo;
		}

		private void OnUndoStatusChanged(bool canUndo)
		{
			undoButton.interactable = canUndo;
		}

		private void UpdateButtons()
		{
			var hasSkinnedMeshRenderer = PaintManager.ObjectForPainting.TryGetComponent<SkinnedMeshRenderer>(out _);
			if (!hasSkinnedMeshRenderer)
			{
				playPauseToggle.isOn = false;
			}
			playPauseToggle.interactable = hasSkinnedMeshRenderer;
			if (paintManagerAnimator != null)
			{
				paintManagerAnimator.enabled = hasSkinnedMeshRenderer;
			}
			bottomPanelText.text = paintManagers[currentPaintManagerId].Text;
		}
		
		private void HoverEnter(BaseEventData data)
		{
			if (!PaintManager.Initialized)
				return;
			
#if ENABLE_INPUT_SYSTEM
			if (Mouse.current != null)
#elif ENABLE_LEGACY_INPUT_MANAGER
			if (Input.mousePresent)
#endif
			{
				PaintManager.PaintObject.ProcessInput = false;
			}
			PaintManager.PaintObject.FinishPainting();
		}
		
		private void HoverExit(BaseEventData data)
		{
			if (!PaintManager.Initialized)
				return;
			
#if ENABLE_INPUT_SYSTEM
			if (Mouse.current != null)
#elif ENABLE_LEGACY_INPUT_MANAGER
			if (Input.mousePresent)
#endif
			{
				PaintManager.PaintObject.ProcessInput = true;
			}
		}
		
		private void ColorClick(Color color)
		{
			var brushColor = Color.white;
			if (PaintController.Instance.UseSharedSettings)
			{
				brushColor = PaintController.Instance.Brush.Color;
			}
			else if (PaintManager != null && PaintManager.Initialized)
			{
				brushColor = PaintManager.Brush.Color;
			}
			brushColor = new Color(color.r, color.g, color.b, brushColor.a);
			if (PaintController.Instance.UseSharedSettings)
			{
				PaintController.Instance.Brush.SetColor(brushColor);
			}
			else if (PaintManager != null && PaintManager.Initialized)
			{
				PaintManager.Brush.SetColor(brushColor);
			}
			foreach (var toolToggle in toolsToggles)
			{
				if (toolToggle.Tool == PaintTool.Brush)
				{
					toolToggle.Toggle.isOn = true;
					break;
				}
			}
			var colorString = ColorUtility.ToHtmlStringRGB(brushColor);
			PlayerPrefs.SetString("XDPaintDemoBrushColor", colorString);
		}

		private void BrushClick(Texture texture, int brushId)
		{
			PaintController.Instance.Brush.SetTexture(texture, true, false);
			selectedTexture = texture;
			brushesPanel.gameObject.SetActive(false);
			PlayerPrefs.SetInt("XDPaintDemoBrushId", brushId);
		}

		private void ResetPlates(BaseEventData data)
		{
			if (colorPalette.gameObject.activeInHierarchy || brushesPanel.gameObject.activeInHierarchy || blurPanel.gameObject.activeInHierarchy || gaussianBlurPanel.gameObject.activeInHierarchy)
			{
				colorPalette.gameObject.SetActive(false);
				brushesPanel.gameObject.SetActive(false);
				blurPanel.gameObject.SetActive(false);
				gaussianBlurPanel.gameObject.SetActive(false);
			}
			HoverExit(null);
		}
	}
}