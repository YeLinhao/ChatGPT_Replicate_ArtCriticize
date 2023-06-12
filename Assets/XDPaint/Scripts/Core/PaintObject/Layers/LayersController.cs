using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using XDPaint.States;
using XDPaint.Utils;

namespace XDPaint.Core.Layers
{
    [Serializable]
    public class LayersController : RecordControllerBase, ILayersController
    {
        private readonly RenderTextureFormat LayerRenderTextureFormat = RenderTextureFormat.ARGB32;
        private RenderTextureFormat MaskRenderTextureFormat => SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat) ? 
            RenderTextureFormat.RFloat : RenderTextureFormat.ARGB32;
        
        public event Action<ObservableCollection<ILayer>, NotifyCollectionChangedEventArgs> OnLayersCollectionChanged;
        public event Action<ILayer> OnLayerChanged;
        public event Action<ILayer> OnActiveLayerSwitched;
        public event Action<bool> OnCanRemoveLayer;

        //stores layers
        private ObservableCollection<ILayer> layers = new ObservableCollection<ILayer>();
#if UNITY_EDITOR
        //stores layers for PropertyDrawer
        [SerializeField] private List<Layer> layersList = new List<Layer>();
#endif
        //store layers for user
        private ReadOnlyCollection<ILayer> layersCollection;
        private LayersMergeController mergeController;
        private CommandBufferBuilder commandBufferBuilder;
        private ILayer activeLayer;
        private FilterMode filterMode;
        private int defaultWidth;
        private int defaultHeight;
        private bool isMerging;
        
        public ReadOnlyCollection<ILayer> Layers => layersCollection;
        public ILayer ActiveLayer => layers[activeLayerIndex];
        public bool CanDisableLayer => layers.Count(x => x.Enabled) > 1;
        public bool CanRemoveLayer => layers.Count > 1;
        public bool CanMergeLayers => layers.Count > 1 && activeLayerIndex > 0 && ActiveLayer.Enabled && layers[activeLayerIndex - 1].Enabled;
        public bool CanMergeAllLayers => layers.Count > 1 && ActiveLayer.Enabled && layers.Select(x => x.Enabled).ToArray().Length > 1;

        [SerializeField] private int activeLayerIndex;
        [UndoRedo] public int ActiveLayerIndex
        {
            get => activeLayerIndex;
            private set
            {
                if (activeLayerIndex != value)
                {
                    var oldValue = activeLayerIndex;
                    activeLayerIndex = value;
                    OnPropertyChanged("ActiveLayerIndex", oldValue, value);
                    activeLayer = layers[activeLayerIndex];
                    OnActiveLayerSwitched?.Invoke(activeLayer);
                }
            }
        }

        public LayersController(LayersMergeController mergeController) : base(null)
        {
            this.mergeController = mergeController;
            if (StatesSettings.Instance.UndoRedoEnabled)
            {
                layers.CollectionChanged += LayersCollectionChanged;
            }
            commandBufferBuilder = new CommandBufferBuilder();
        }

        private void LayersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
#if UNITY_EDITOR
            layersList = layers.Select(x => x as Layer).ToList();
#endif
            layersCollection = new ReadOnlyCollection<ILayer>(layers);
            if (StatesSettings.Instance.EnableUndoRedoForPropertiesAndActions)
            {
                OnLayersCollectionChanged?.Invoke(layers, e);
                OnCanRemoveLayer?.Invoke(CanRemoveLayer);
            }
        }

        public void DoDispose()
        {
            if (layers == null)
                return;
            
            foreach (var layer in layers)
            {
                layer.DoDispose();
            }
            layers.CollectionChanged -= LayersCollectionChanged;
            layers.Clear();
        }
        
        public void Init(int width, int height)
        {
            defaultWidth = width;
            defaultHeight = height;
        }

        public void CreateBaseLayers(Texture sourceTexture, bool useSourceTextureAsBackground)
        {
            if (useSourceTextureAsBackground)
            {
                AddNewLayer("Background", sourceTexture);
            }
            AddNewLayer("Paint");
            ActiveLayerIndex = layers.Count - 1;
        }

        public void SetFilterMode(FilterMode mode)
        {
            filterMode = mode;
        }

        public ILayer AddNewLayer()
        {
            var layerName = "Layer " + (layers.Count + 1);
            return AddNewLayer(layerName);
        }

        public ILayer AddNewLayer(string name)
        {
            EnableStatesGrouping();
            var layer = new Layer(this);
            layer.Init(commandBufferBuilder, () => CanDisableLayer);
            layer.Create(name, defaultWidth, defaultHeight, LayerRenderTextureFormat, filterMode);
            InitLayer(layer);
            layer.OnRenderPropertyChanged = OnLayerRenderPropertyChanged;
            DisableStatesGrouping();
            return layer;
        }
        
        public ILayer AddNewLayer(string name, Texture sourceTexture)
        {
            EnableStatesGrouping();
            var layer = new Layer(this);
            layer.Init(commandBufferBuilder, () => CanDisableLayer);
            layer.Create(name, sourceTexture, LayerRenderTextureFormat, filterMode);
            InitLayer(layer);
            layer.OnRenderPropertyChanged = OnLayerRenderPropertyChanged;
            DisableStatesGrouping();
            return layer;
        }

        public void AddLayerMask(ILayer layer, Texture source)
        {
            layer.AddMask(source, MaskRenderTextureFormat);
        }

        public void AddLayerMask(ILayer layer)
        {
            layer.AddMask(MaskRenderTextureFormat);
        }
        
        public void AddLayerMask(Texture source)
        {
            ActiveLayer.AddMask(source, MaskRenderTextureFormat);
        }
        
        public void AddLayerMask()
        {
            ActiveLayer.AddMask(MaskRenderTextureFormat);
        }

        public void RemoveActiveLayerMask()
        {
            ActiveLayer.RemoveMask();
        }

        private void InitLayer(ILayer layer)
        {
            if (layers.Count > 0)
            {
                layers.Insert(activeLayerIndex + 1, layer);
            }
            else
            {
                layers.Add(layer);
            }
            ActiveLayerIndex = layers.IndexOf(layer);
            OnCanRemoveLayer?.Invoke(CanRemoveLayer);
            OnLayerRenderPropertyChanged(layer);
        }

        private void OnLayerRenderPropertyChanged(ILayer layer)
        {
            if (isMerging)
                return;
            
            OnLayerChanged?.Invoke(layer);
        }
        
        public ILayer GetActiveLayer()
        {
            return ActiveLayer;
        }

        public void SetActiveLayer(ILayer layer)
        {
            var index = layers.IndexOf(layer);
            if (index >= 0)
            {
                ActiveLayerIndex = index;
            }
            else
            {
                Debug.LogWarning($"Can't find layer \"{layer.Name}\"!");
            }
        }
                
        public void SetActiveLayer(int index)
        {
            ActiveLayerIndex = index;
        }

        public void SetLayerOrder(ILayer layer, int index)
        {
            var layerIndex = layers.IndexOf(layer);
            if (layerIndex == index)
                return;
            index = Mathf.Clamp(index, 0, layers.Count);
            layers.Move(layerIndex, index);
            activeLayerIndex = layers.IndexOf(activeLayer);
        }

        public void RemoveActiveLayer()
        {
            if (layers.Count == 1)
                return;

            EnableStatesGrouping();
            var index = activeLayerIndex;
            if (layers.Count - 1 >= activeLayerIndex)
            {
                ActiveLayerIndex = layers.Count - 2;
            }
            layers.RemoveAt(index);
            OnLayerRemoved();
            DisableStatesGrouping();
        }

        public void RemoveLayer(ILayer layer)
        {
            if (layers.Count == 1)
                return;

            if (layers.Contains(layer))
            {
                if (activeLayerIndex >= layers.Count - 1)
                {
                    ActiveLayerIndex = layers.Count - 2;
                }
                layers.Remove(layer);
                OnLayerRemoved();
            }
            else
            {
                Debug.LogWarning($"Layers does not contains layer \"{layer.Name}\"");
            }
        }

        public void RemoveLayer(int index)
        {
            if (layers.Count == 1)
                return;

            if (layers.Count > index)
            {
                EnableStatesGrouping();
                layers.RemoveAt(index);
                OnLayerRemoved();
                DisableStatesGrouping();
            }
            else
            {
                Debug.LogWarning("Incorrect layer index!");
            }
        }

        private void OnLayerRemoved()
        {
            if (activeLayerIndex >= layers.Count)
            {
                ActiveLayerIndex = layers.Count - 1;
            }
            OnCanRemoveLayer?.Invoke(CanRemoveLayer);
        }

        public void MergeLayers()
        {
            if (CanMergeLayers)
            {
                isMerging = true;
                EnableStatesGrouping();
                var layersState = layers.Select(x => x.Enabled).ToArray();
                for (var i = 0; i < layersState.Length; i++)
                {
                    if (i == activeLayerIndex || i == activeLayerIndex - 1)
                        continue;
                
                    layers[i].Enabled = false;
                }
                var layer = activeLayer;
                ActiveLayerIndex--;
                mergeController.MergeLayers(ActiveLayer.RenderTexture);
                OnDidAction(() => mergeController.MergeLayers(ActiveLayer.RenderTexture));
                for (var i = 0; i < layersState.Length; i++)
                {
                    layers[i].Enabled = layersState[i];
                }
                RemoveLayer(layer);
                ActiveLayer.Opacity = 1f;
                ActiveLayer.SaveState();
                DisableStatesGrouping();
                isMerging = false;
                OnLayerRenderPropertyChanged(ActiveLayer);
            }
        }

        public void MergeAllLayers()
        {
            if (CanMergeAllLayers)
            {
                isMerging = true;
                var enabledLayers = layers.Where(x => x.Enabled).ToArray();
                if (enabledLayers.Length > 1)
                {
                    EnableStatesGrouping();
                    var mergeLayer = ActiveLayer;
                    mergeController.MergeLayers(ActiveLayer.RenderTexture);
                    OnDidAction(() => mergeController.MergeLayers(ActiveLayer.RenderTexture));
                    foreach (var layer in enabledLayers)
                    {
                        if (layer == mergeLayer || !layer.Enabled)
                            continue;

                        RemoveLayer(layer);
                    }
                    mergeLayer.Opacity = 1f;
                    mergeLayer.SaveState();
                    DisableStatesGrouping();
                }
                isMerging = false;
            }
        }

        public void SetLayerTexture(int index, Texture texture)
        {
            if (layers.Count > index)
            {
                Graphics.Blit(texture, layers[index].RenderTexture);
                layers[index].SaveState();
            }
            else
            {
                Debug.LogWarning("Incorrect layer index!");
            }
        }
    }
}