using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using XDPaint.Controllers;
using XDPaint.Controllers.InputData;
using XDPaint.Controllers.InputData.Base;
using XDPaint.Core;
using XDPaint.Core.Layers;
using XDPaint.Core.Materials;
using XDPaint.Core.PaintModes;
using XDPaint.Core.PaintObject;
using XDPaint.Core.PaintObject.Base;
using XDPaint.States;
using XDPaint.Tools;
using XDPaint.Tools.Image.Base;
using XDPaint.Tools.Layers;
using XDPaint.Tools.Raycast;
using XDPaint.Tools.Triangles;
using XDPaint.Utils;
using IDisposable = XDPaint.Core.IDisposable;

namespace XDPaint
{
    [DisallowMultipleComponent]
    public class PaintManager : MonoBehaviour, IDisposable
    {
        #region Events

        public event Action<PaintManager> OnInitialized;
        public event Action OnDisposed;

        #endregion
        
        #region Properties and variables

        public GameObject ObjectForPainting;
        [FormerlySerializedAs("Material")] [SerializeField] private Paint material = new Paint();
        public Paint Material => material;
        [FormerlySerializedAs("CopySourceTextureToPaintTexture")] public bool CopySourceTextureToLayer = true;
        [SerializeField] private LayersController layersController;
        public ILayersController LayersController => layersController;
        [SerializeField] private LayersContainer layersContainer;
        public BasePaintObject PaintObject { get; private set; }
        
        private StatesController statesController;
        public IStatesController StatesController => statesController;
        
        [SerializeField] private PaintMode paintModeType;
        [SerializeField] private FilterMode filterMode = FilterMode.Bilinear;
        public FilterMode FilterMode
        {
            get => filterMode;
            set
            {
                filterMode = value;
                if (initialized)
                {
                    layersController.SetFilterMode(filterMode);
                }
            }
        }

        [NonSerialized] private Brush currentBrush;
        [SerializeField] private Brush brush = new Brush();
        public Brush Brush
        {
            get
            {
                if (Application.isPlaying)
                {
                    return currentBrush;
                }
                return brush;
            }
            set
            {
                if (Application.isPlaying)
                {
                    currentBrush = value;
                    currentBrush.Init(mode);
                    currentBrush.SetPaintTool(PaintController.Instance.UseSharedSettings ? PaintController.Instance.Tool : paintTool);
                    toolsManager.CurrentTool.OnBrushChanged(currentBrush);
                }
                else
                {
                    brush = value;
                }
                
                if (initialized)
                {
                    PaintObject.Brush = currentBrush;
                    Material.SetPreviewTexture(currentBrush.RenderTexture);
                }
            }
        }
        
        [SerializeField] private ToolsManager toolsManager;
        public ToolsManager ToolsManager => toolsManager;

        [SerializeField] private PaintTool paintTool;
        public PaintTool Tool
        {
            get
            {
                if (toolsManager.CurrentTool != null)
                {
                    return toolsManager.CurrentTool.Type;
                }
                return PaintTool.Brush;
            } 
            set
            {
                paintTool = value;
                if (initialized)
                {
                    currentBrush.SetPaintTool(paintTool);
                    Material.SetPreviewTexture(currentBrush.RenderTexture);
                    if (toolsManager != null)
                    {
                        toolsManager.SetTool(paintTool);
                        toolsManager.CurrentTool.SetPaintMode(mode);
                        PaintObject.Tool = toolsManager.CurrentTool;
                    }
                }
            }
        }

        public Camera Camera => PaintController.Instance.Camera;

        [SerializeField] private bool useSourceTextureAsBackground;
        public bool UseSourceTextureAsBackground
        {
            get => useSourceTextureAsBackground;
            set => useSourceTextureAsBackground = value;
        }

        [SerializeField] private bool useNeighborsVerticesForRaycasts;
        public bool UseNeighborsVerticesForRaycasts
        {
            get => useNeighborsVerticesForRaycasts;
            set
            {
                useNeighborsVerticesForRaycasts = value;
                if (!Application.isPlaying)
                {
                    if (!useNeighborsVerticesForRaycasts)
                    {
                        ClearTrianglesNeighborsData();
                    }
                }
                if (initialized)
                {
                    PaintObject.UseNeighborsVertices = useNeighborsVerticesForRaycasts;
                }
            }
        }

        public bool HasTrianglesData => triangles != null && triangles.Length > 0;
        public bool Initialized => initialized;

        [SerializeField] private TrianglesContainer trianglesContainer;
        [SerializeField] private Triangle[] triangles;
                
        [SerializeField] private int subMesh;
        public int SubMesh
        {
            get => subMesh;
            set => subMesh = value;
        }
        
        [SerializeField] private int uvChannel;
        public int UVChannel
        {
            get => uvChannel;
            set => uvChannel = value;
        }

        private ObjectComponentType componentType;
        public ObjectComponentType ComponentType => componentType;
        
        private LayersMergeController layersMergeController;
        private IRenderTextureHelper renderTextureHelper;
        private IRenderComponentsHelper renderComponentsHelper;
        private IPaintMode mode;
        private IPaintData paintData;
        private InputDataBase inputData;
        private bool initialized;

        #endregion
        
        private void Start()
        {
            if (initialized)
                return;
            
            Init();
        }

        private void Update()
        {
            if (initialized && (PaintObject.IsPainting || currentBrush.Preview))
            {
                Render();
            }
        }

        private void OnDestroy()
        {
            DoDispose();
        }

        public void Init()
        {
            if (initialized)
                DoDispose();
            
            initialized = false;
            if (ObjectForPainting == null)
            {
                Debug.LogError("ObjectForPainting is null!");
                return;
            }

            RestoreSourceMaterialTexture();
            
            if (renderComponentsHelper == null)
            {
                renderComponentsHelper = new RenderComponentsHelper();
            }
            renderComponentsHelper.Init(ObjectForPainting, out componentType);
            if (componentType == ObjectComponentType.Unknown)
            {
                Debug.LogError("Unknown component type!");
                return;
            }

            if (ControllersContainer.Instance == null)
            {
                var containerGameObject = new GameObject(Settings.Instance.ContainerGameObjectName);
                containerGameObject.AddComponent<ControllersContainer>();
            }

            if (renderComponentsHelper.IsMesh())
            {
                var paintComponent = renderComponentsHelper.PaintComponent;
                var renderComponent = renderComponentsHelper.RendererComponent;
                var mesh = renderComponentsHelper.GetMesh();
                if (trianglesContainer != null)
                {
                    triangles = trianglesContainer.Data;
                }
                if (triangles == null || triangles.Length == 0)
                {
                    if (mesh != null)
                    {
                        Debug.LogWarning("PaintManager does not have triangles data! Getting it may take a while.");
                        triangles = TrianglesData.GetData(mesh, subMesh, uvChannel, useNeighborsVerticesForRaycasts);
                    }
                    else
                    {
                        Debug.LogError("Mesh is null!");
                        return;
                    }
                }
                RaycastController.Instance.InitObject(Camera, paintComponent, renderComponent, uvChannel, triangles);
            }

            InitRenderTexture();
            InitLayers();
            InitMaterial();
            InitStates();
            
            //register PaintManager
            PaintController.Instance.RegisterPaintManager(this);
            InitBrush();
            InitPaintObject();
            InitTools();

            InputController.Instance.Camera = Camera;
            SubscribeInputEvents(componentType);
            initialized = true;
            Render();

            OnInitialized?.Invoke(this);
        }

        public void DoDispose()
        {
            if (!initialized)
                return;
            
            //unregister PaintManager
            PaintController.Instance.UnRegisterPaintManager(this);
            //restore source material and texture
            RestoreSourceMaterialTexture();
            //free tools resources
            toolsManager.DoDispose();
            paintData.DoDispose();
            //free brush resources
            if (brush != null)
            {
                brush.OnTextureChanged -= Material.SetPreviewTexture;
                brush.OnPreviewChanged -= UpdatePreviewInput;
                brush.DoDispose();
            }
            //destroy created material
            Material.DoDispose();
            //free RenderTextures
            renderTextureHelper.DoDispose();
            layersController.OnLayerChanged -= OnLayerChanged;
            layersController.OnLayersCollectionChanged -= statesController.AddState;
            layersController.DoDispose();
            statesController?.DoDispose();
            //destroy raycast data
            if (renderComponentsHelper.IsMesh())
            {
                var renderComponent = renderComponentsHelper.RendererComponent;
                RaycastController.Instance.DestroyMeshData(renderComponent);
            }
            //unsubscribe input events
            UnsubscribeInputEvents();
            inputData.DoDispose();
            //free undo/redo RenderTextures and meshes
            PaintObject.DoDispose();
            initialized = false;
            OnDisposed?.Invoke();
        }

        public void Render()
        {
            if (initialized)
            {
                PaintObject.OnRender();
                PaintObject.Render();
            }
        }
        
        public void FillTrianglesData(bool fillNeighbors = true)
        {
            if (renderComponentsHelper == null)
            {
                renderComponentsHelper = new RenderComponentsHelper();
            }
            renderComponentsHelper.Init(ObjectForPainting, out var objectComponent);
            if (objectComponent == ObjectComponentType.Unknown)
            {
                return;
            }
            if (renderComponentsHelper.IsMesh())
            {
                var mesh = renderComponentsHelper.GetMesh();
                if (mesh != null)
                {
                    triangles = TrianglesData.GetData(mesh, subMesh, uvChannel, fillNeighbors);
                    if (fillNeighbors)
                    {
                        Debug.Log("Added triangles with neighbors data. Triangles count: " + triangles.Length);
                    }
                    else
                    {
                        Debug.Log("Added triangles data. Triangles count: " + triangles.Length);
                    }
                }
                else
                {
                    Debug.LogError("Mesh is null!");
                }
            }
        }

        public void SetPaintMode(PaintMode paintMode)
        {
            paintModeType = paintMode;
            if (Application.isPlaying)
            {
                mode = PaintController.Instance.GetPaintMode(PaintController.Instance.UseSharedSettings ? PaintController.Instance.PaintMode : paintModeType);
                toolsManager.CurrentTool.SetPaintMode(mode);
                PaintObject.SetPaintMode(mode);
                currentBrush.SetPaintMode(mode);
                currentBrush.SetPaintTool(paintTool);
                Material.SetPreviewTexture(currentBrush.RenderTexture);
            }
        }

        public IPaintMode GetPaintMode()
        {
            if (initialized && Application.isPlaying)
            {
                mode = PaintController.Instance.GetPaintMode(PaintController.Instance.UseSharedSettings ? PaintController.Instance.PaintMode : paintModeType);
            }
            return mode;
        }
        
        public void ClearTrianglesData()
        {
            triangles = null;
        }

        public void ClearTrianglesNeighborsData()
        {
            if (triangles != null)
            {
                foreach (var triangle in triangles)
                {
                    triangle.N.Clear();
                }
            }
        }
                
        public Triangle[] GetTriangles()
        {
            return triangles;
        }

        public void SetTriangles(Triangle[] trianglesData)
        {
            triangles = trianglesData;
        }

        public void SetTriangles(TrianglesContainer trianglesContainerData)
        {
            trianglesContainer = trianglesContainerData;
            triangles = trianglesContainer.Data;
        }

        public RenderTexture GetPaintTexture()
        {
            return layersController.ActiveLayer.RenderTexture;
        }

        public RenderTexture GetPaintInputTexture()
        {
            return renderTextureHelper.GetTexture(RenderTarget.Input);
        }

        public RenderTexture GetResultRenderTexture()
        {
            return renderTextureHelper.GetTexture(RenderTarget.Combined);
        }

        /// <summary>
        /// Returns result texture
        /// </summary>
        /// <param name="hideBrushPreview">Whether to hide brush preview</param>
        /// <returns></returns>
        public Texture2D GetResultTexture(bool hideBrushPreview = false)
        {
            var needToHideBrushPreview = hideBrushPreview && currentBrush.Preview;
            if (needToHideBrushPreview)
            {
                currentBrush.Preview = false;
                Render();
            }
            RenderTexture temp = null;
            var renderTexture = renderTextureHelper.GetTexture(RenderTarget.Combined);
            if (renderComponentsHelper.ComponentType == ObjectComponentType.SpriteRenderer)
            {
                var spriteRenderer = renderComponentsHelper.RendererComponent as SpriteRenderer;
                if (spriteRenderer != null && spriteRenderer.material != null && spriteRenderer.material.shader == Settings.Instance.SpriteMaskShader)
                {
                    temp = RenderTextureFactory.CreateTemporaryRenderTexture(renderTexture, false);
                    var rti = new RenderTargetIdentifier(temp);
                    var commandBufferBuilder = new CommandBufferBuilder("ResultTexture");
                    commandBufferBuilder.LoadOrtho().Clear().SetRenderTarget(rti).ClearRenderTarget().Execute();
                    Graphics.Blit(spriteRenderer.material.mainTexture, temp, spriteRenderer.material);
                    commandBufferBuilder.Release();
                }
            }
            var resultTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
            var previousRenderTexture = RenderTexture.active;
            RenderTexture.active = temp != null ? temp : renderTexture;
            resultTexture.ReadPixels(new Rect(0, 0, resultTexture.width, resultTexture.height), 0, 0, false);
            resultTexture.Apply();
            RenderTexture.active = previousRenderTexture;
            if (temp != null)
            {
                RenderTexture.ReleaseTemporary(temp);
            }
            if (needToHideBrushPreview)
            {
                currentBrush.Preview = true;
            }
            return resultTexture;
        }

        /// <summary>
        /// Set Layers data from LayersContainer
        /// </summary>
        /// <param name="container"></param>
        public void SetLayersData(LayersContainer container)
        {
            foreach (var layerData in container.LayersData)
            {
                ILayer layer;
                if (layerData.SourceTexture == null)
                {
                    layer = layersController.AddNewLayer(layerData.Name);
                }
                else
                {
                    layer = layersController.AddNewLayer(layerData.Name, layerData.SourceTexture);
                }
                layer.Enabled = layerData.IsEnabled;
                layer.MaskEnabled = layerData.IsMaskEnabled;
                layer.Opacity = layerData.Opacity;
                layer.BlendingMode = layerData.BlendingMode;
                if (layerData.Mask != null)
                {
                    layersController.AddLayerMask(layer);
                    Graphics.Blit(layerData.Mask, layer.MaskRenderTexture);
                }
            }
            layersController.SetActiveLayer(container.ActiveLayerIndex);
        }

        /// <summary>
        /// Returns Layers data
        /// </summary>
        /// <returns></returns>
        public LayerData[] GetLayersData()
        {
            var layersData = new LayerData[layersController.Layers.Count];
            for (var i = 0; i < layersController.Layers.Count; i++)
            {
                var layer = layersController.Layers[i];
                layersData[i] = new LayerData(layer);
            }
            return layersData;
        }

        /// <summary>
        /// Restore source material and texture
        /// </summary>
        private void RestoreSourceMaterialTexture()
        {
            if (initialized && Material.SourceMaterial != null)
            {
                if (Material.SourceMaterial.GetTexture(Material.ShaderTextureName) == null)
                {
                    Material.SourceMaterial.SetTexture(Material.ShaderTextureName, Material.SourceTexture);
                }
                renderComponentsHelper.SetSourceMaterial(Material.SourceMaterial, Material.MaterialIndex);
            }
        }
        
        public void InitBrush()
        {
            if (PaintController.Instance.UseSharedSettings)
            {
                currentBrush = PaintController.Instance.Brush;
            }
            else
            {
                if (currentBrush != null)
                {
                    currentBrush.OnTextureChanged -= Material.SetPreviewTexture;
                }
                currentBrush = brush;
                currentBrush.Init(mode);
                if (PaintObject != null)
                {
                    PaintObject.Brush = currentBrush;
                }
                currentBrush.SetPaintMode(mode);
                currentBrush.SetPaintTool(paintTool);
            }
            Material.SetPreviewTexture(currentBrush.RenderTexture);
            currentBrush.OnTextureChanged -= Material.SetPreviewTexture;
            currentBrush.OnTextureChanged += Material.SetPreviewTexture;
            currentBrush.OnPreviewChanged -= UpdatePreviewInput;
            currentBrush.OnPreviewChanged += UpdatePreviewInput;
        }
        
        private void InitRenderTexture()
        {
            mode = PaintController.Instance.GetPaintMode(PaintController.Instance.UseSharedSettings ? PaintController.Instance.PaintMode : paintModeType);
            if (renderTextureHelper == null)
            {
                renderTextureHelper = new RenderTextureHelper();
            }
            var sourceTexture = layersContainer != null ? layersContainer.LayersData[0].SourceTexture : null;
            Material.Init(renderComponentsHelper, sourceTexture);
            renderTextureHelper.Init(Material.SourceTexture.width, Material.SourceTexture.height, filterMode);
        }

        private void InitLayers()
        {
            layersMergeController = new LayersMergeController();
            layersController?.DoDispose();
            layersController = new LayersController(layersMergeController);
            layersController.Init(Material.SourceTexture.width, Material.SourceTexture.height);
            layersController.SetFilterMode(filterMode);
            if (layersContainer != null)
            {
                SetLayersData(layersContainer);
            }
            else
            {
                layersController.CreateBaseLayers(Material.SourceTexture, useSourceTextureAsBackground);
            }
            if (CopySourceTextureToLayer)
            {
                var lastLayerIndex = LayersController.Layers.Count - 1;
                layersController.ActiveLayer.SourceTexture = Material.SourceTexture;
                Graphics.Blit(Material.SourceTexture, layersController.Layers[lastLayerIndex].RenderTexture);
            }
        }
        
        private void InitMaterial()
        {
            if (Material.SourceTexture != null)
            {
                Graphics.Blit(Material.SourceTexture, renderTextureHelper.GetTexture(RenderTarget.Combined));
            }
            Material.SetObjectMaterialTexture(renderTextureHelper.GetTexture(RenderTarget.Combined));
            Material.SetPaintTexture(layersController.ActiveLayer.RenderTexture);
            Material.SetInputTexture(renderTextureHelper.GetTexture(RenderTarget.Input));
        }

        private void InitStates()
        {
            if (StatesSettings.Instance.UndoRedoEnabled)
            {
                statesController = new StatesController();
                statesController.Init(layersController);
                layersController.SetStateController(statesController);
                layersController.OnLayerChanged -= OnLayerChanged;
                layersController.OnLayerChanged += OnLayerChanged;
                layersController.OnLayersCollectionChanged -= OnLayersCollectionChanged;
                layersController.OnLayersCollectionChanged += OnLayersCollectionChanged;
                layersController.OnLayersCollectionChanged -= statesController.AddState;
                layersController.OnLayersCollectionChanged += statesController.AddState;
            }
            statesController.Enable();
        }
        
        private void OnLayerChanged(ILayer layer)
        {
            TryRender();
        }

        private void OnLayersCollectionChanged(ObservableCollection<ILayer> collection, NotifyCollectionChangedEventArgs notifyArgs)
        {
            TryRender();
        }

        private void TryRender()
        {
            if (currentBrush != null && !currentBrush.Preview)
            {
                Render();
            }
        }

        private void InitPaintObject()
        {
            if (PaintObject != null)
            {
                UnsubscribeInputEvents();
                PaintObject.DoDispose();
            }
            if (renderComponentsHelper.ComponentType == ObjectComponentType.RawImage)
            {
                PaintObject = new CanvasRendererPaint();
            }
            else if (renderComponentsHelper.ComponentType == ObjectComponentType.SpriteRenderer)
            {
                PaintObject = new SpriteRendererPaint();
            }
            else
            {
                PaintObject = new MeshRendererPaint();
            }
            PaintObject.Init(Camera, ObjectForPainting.transform, Material, renderTextureHelper, statesController);
            PaintObject.Brush = currentBrush;
            PaintObject.SetActiveLayer(layersController.GetActiveLayer);
            PaintObject.SetPaintMode(mode);
            PaintObject.UseNeighborsVertices = useNeighborsVerticesForRaycasts;
            layersMergeController.OnLayersMerge = PaintObject.RenderToTextureWithoutPreview;
        }

        private void InitTools()
        {
            toolsManager?.DoDispose();
            if (PaintController.Instance.UseSharedSettings)
            {
                paintTool = PaintController.Instance.Tool;
            }
            paintData = new BasePaintData(this, renderTextureHelper, renderComponentsHelper);
            toolsManager = new ToolsManager(paintTool, paintData);
            toolsManager.Init(this);
            toolsManager.CurrentTool.SetPaintMode(mode);
            PaintObject.Tool = toolsManager.CurrentTool;
        }

        #region Setup Input Events

        private void SubscribeInputEvents(ObjectComponentType component)
        {
            if (inputData != null)
            {
                UnsubscribeInputEvents();
                inputData.DoDispose();
            }
            inputData = new InputDataResolver().Resolve(component);
            inputData.Init(this, Camera);
            UpdatePreviewInput(currentBrush.Preview);
            InputController.Instance.OnUpdate += inputData.OnUpdate;
            InputController.Instance.OnMouseDown += inputData.OnDown;
            InputController.Instance.OnMouseButton += inputData.OnPress;
            InputController.Instance.OnMouseUp += inputData.OnUp;
            inputData.OnDownHandler += PaintObject.OnMouseDown;
            inputData.OnPressHandler += PaintObject.OnMouseButton;
            inputData.OnUpHandler += PaintObject.OnMouseUp;
        }

        private void UnsubscribeInputEvents()
        {
            inputData.OnHoverSuccessHandler -= PaintObject.OnMouseHover;
            inputData.OnHoverFailedHandler -= PaintObject.OnMouseHoverFailed;
            inputData.OnDownHandler -= PaintObject.OnMouseDown;
            inputData.OnPressHandler -= PaintObject.OnMouseButton;
            inputData.OnUpHandler -= PaintObject.OnMouseUp;
            InputController.Instance.OnUpdate -= inputData.OnUpdate;
            InputController.Instance.OnMouseHover -= inputData.OnHover;
            InputController.Instance.OnMouseDown -= inputData.OnDown;
            InputController.Instance.OnMouseButton -= inputData.OnPress;
            InputController.Instance.OnMouseUp -= inputData.OnUp;
        }

        private void UpdatePreviewInput(bool preview)
        {
            if (preview)
            {
                inputData.OnHoverSuccessHandler -= PaintObject.OnMouseHover;
                inputData.OnHoverSuccessHandler += PaintObject.OnMouseHover;
                inputData.OnHoverFailedHandler -= PaintObject.OnMouseHoverFailed;
                inputData.OnHoverFailedHandler += PaintObject.OnMouseHoverFailed;
                InputController.Instance.OnMouseHover -= inputData.OnHover;
                InputController.Instance.OnMouseHover += inputData.OnHover;
            }
            else
            {
                inputData.OnHoverSuccessHandler -= PaintObject.OnMouseHover;
                inputData.OnHoverFailedHandler -= PaintObject.OnMouseHoverFailed;
                InputController.Instance.OnMouseHover -= inputData.OnHover;
            }
        }

        #endregion
    }
}