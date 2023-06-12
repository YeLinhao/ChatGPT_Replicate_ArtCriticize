using System;
using UnityEngine;
using UnityEngine.Rendering;
using XDPaint.States;
using XDPaint.Utils;

namespace XDPaint.Core.Layers
{
    [Serializable]
    public class Layer : RecordControllerBase, ILayer
    {
        public event Action<ILayer> OnLayerChanged;
        public Action<Layer> OnRenderPropertyChanged;
        
        [SerializeField] private bool enabled = true;
        [UndoRedo] public bool Enabled
        {
            get => enabled;
            set
            {
                if (enabled != value && (value || canLayerBeDisabled()))
                {
                    var previousValue = enabled;
                    enabled = value;
                    OnPropertyChanged("Enabled", previousValue, value);
                    OnRenderPropertyChanged?.Invoke(this);
                    OnLayerChanged?.Invoke(this);
                }
            }
        }

        public bool CanBeDisabled => (enabled && canLayerBeDisabled()) || !enabled;
        
        [SerializeField] private bool maskEnabled;
        [UndoRedo] public bool MaskEnabled
        {
            get => maskEnabled;
            set
            {
                if (maskEnabled != value && maskRenderTexture != null)
                {
                    var previousValue = maskEnabled;
                    maskEnabled = value;
                    OnPropertyChanged("MaskEnabled", previousValue, value);
                    OnRenderPropertyChanged?.Invoke(this);
                    OnLayerChanged?.Invoke(this);
                }
            }
        }

        [SerializeField] private string name = "Layer";
        [UndoRedo] public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    var previousValue = name;
                    name = value;
                    OnPropertyChanged("Name", previousValue, value);
                    OnLayerChanged?.Invoke(this);
                }
            }
        }

        [SerializeField] private float opacity = 1f;
        [UndoRedo] public float Opacity
        {
            get => opacity;
            set
            {
                if (opacity != value)
                {
                    var previousValue = opacity;
                    opacity = value;
                    OnPropertyChanged("Opacity", previousValue, value);
                    OnRenderPropertyChanged?.Invoke(this);
                    OnLayerChanged?.Invoke(this);
                }
            }
        }
        
        [SerializeField] private Texture sourceTexture;
        [UndoRedo] public Texture SourceTexture
        {
            get => sourceTexture;
            set
            {
                if (sourceTexture != value)
                {
                    var previousValue = sourceTexture;
                    sourceTexture = value;
                    OnPropertyChanged("SourceTexture", previousValue, value);
                    OnLayerChanged?.Invoke(this);
                }
            }
        }
        
        [SerializeField] private RenderTexture renderTexture;
        [UndoRedo] public RenderTexture RenderTexture
        {
            get => renderTexture;
            private set
            {
                if (renderTexture != value)
                {
                    var oldValue = renderTexture;
                    renderTexture = value;
                    OnPropertyChanged(this, "RenderTexture", oldValue, renderTexture, sourceTexture);
                    OnRenderPropertyChanged?.Invoke(this);
                    OnLayerChanged?.Invoke(this);
                }
            }
        }

        private RenderTargetIdentifier renderTarget;
        public RenderTargetIdentifier RenderTarget => renderTarget;
        
        [SerializeField] private Texture maskSourceTexture;
        [UndoRedo] public Texture MaskSourceTexture
        {
            get => maskSourceTexture;
            set
            {
                if (maskSourceTexture != value)
                {
                    var previousValue = maskSourceTexture;
                    maskSourceTexture = value;
                    OnPropertyChanged("MaskSourceTexture", previousValue, value);
                    OnLayerChanged?.Invoke(this);
                }
            }
        }
        
        [SerializeField] private RenderTexture maskRenderTexture;
        [UndoRedo] public RenderTexture MaskRenderTexture
        {
            get => maskRenderTexture;
            private set
            {
                if (maskRenderTexture != value)
                {
                    var oldValue = maskRenderTexture;
                    maskRenderTexture = value;
                    OnPropertyChanged(this, "MaskRenderTexture", oldValue, maskRenderTexture, maskSourceTexture);
                    OnRenderPropertyChanged?.Invoke(this);
                    OnLayerChanged?.Invoke(this);
                }
            }
        }
        
        private RenderTargetIdentifier maskRenderTarget;
        public RenderTargetIdentifier MaskRenderTarget => maskRenderTarget;

        [SerializeField] private BlendingMode blendingMode;
        [UndoRedo] public BlendingMode BlendingMode
        {
            get => blendingMode;
            set
            {
                if (blendingMode != value)
                {
                    var previousValue = blendingMode;
                    blendingMode = value;
                    OnPropertyChanged("BlendingMode", previousValue, blendingMode);
                    OnRenderPropertyChanged?.Invoke(this);
                    OnLayerChanged?.Invoke(this);
                }
            }
        }

        private Func<bool> canLayerBeDisabled;
        private CommandBufferBuilder commandBufferBuilder;

        public Layer(RecordControllerBase recordController) : base(recordController)
        {
        }
        
        public void Create(string layerName, int width, int height, RenderTextureFormat format, FilterMode filterMode)
        {
            Name = layerName;
            renderTexture = RenderTextureFactory.CreateRenderTexture(width, height, 0, format, filterMode);
            renderTexture.name = layerName;
            renderTarget = new RenderTargetIdentifier(renderTexture);
            
            commandBufferBuilder.Clear().SetRenderTarget(renderTarget).ClearRenderTarget(Color.clear).Execute();
            
            OnPropertyChanged(this, "RenderTexture", null, renderTexture, null);
            OnRenderPropertyChanged?.Invoke(this);
        }

        public void Create(string layerName, Texture source, RenderTextureFormat format, FilterMode filterMode)
        {
            Name = layerName;
            sourceTexture = source;
            renderTexture = RenderTextureFactory.CreateRenderTexture(sourceTexture.width, sourceTexture.height, 0, format, filterMode);
            renderTexture.name = layerName;
            renderTarget = new RenderTargetIdentifier(renderTexture);
            
            commandBufferBuilder.Clear().SetRenderTarget(renderTarget).ClearRenderTarget(Color.clear).Execute();

            Graphics.Blit(sourceTexture, renderTexture);
            
            OnPropertyChanged(this, "RenderTexture", null, renderTexture, maskSourceTexture);
            OnRenderPropertyChanged?.Invoke(this);
        }

        public void Init(CommandBufferBuilder bufferBuilder, Func<bool> canDisableLayer)
        {
            commandBufferBuilder = bufferBuilder;
            canLayerBeDisabled = canDisableLayer;
        }
        
        public void AddMask(RenderTextureFormat format)
        {
            maskRenderTexture = RenderTextureFactory.CreateRenderTexture(renderTexture.width, renderTexture.height, 0, format);
            maskRenderTexture.name = $"Mask_{renderTexture.name}";
            maskRenderTarget = new RenderTargetIdentifier(maskRenderTexture);
            
            commandBufferBuilder.Clear().SetRenderTarget(maskRenderTarget).ClearRenderTarget(Color.clear).Execute();

            OnPropertyChanged(this, "MaskRenderTexture", null, maskRenderTexture, maskSourceTexture);
            OnRenderPropertyChanged?.Invoke(this);
        }

        public void AddMask(Texture maskTexture, RenderTextureFormat format)
        {
            maskSourceTexture = maskTexture;
            maskRenderTexture = RenderTextureFactory.CreateRenderTexture(renderTexture.width, renderTexture.height, 0, format);
            maskRenderTexture.name = $"Mask_{renderTexture.name}";
            maskRenderTarget = new RenderTargetIdentifier(maskRenderTexture);    
            
            commandBufferBuilder.Clear().SetRenderTarget(maskRenderTarget).ClearRenderTarget(Color.clear).Execute();
            
            if (maskTexture != null)
            {
                Graphics.Blit(maskSourceTexture, maskRenderTexture);
            }
            OnPropertyChanged(this, "MaskRenderTexture", null, maskRenderTexture, maskSourceTexture);
            OnRenderPropertyChanged?.Invoke(this);
        }

        public void RemoveMask()
        {
            if (maskRenderTexture == null)
                return;
            var oldValue = maskRenderTexture;
            maskRenderTexture = null;
            OnRenderPropertyChanged?.Invoke(this);
            OnPropertyChanged(this, "MaskRenderTexture", oldValue, maskRenderTexture, maskSourceTexture);
        }

        public void SaveState()
        {
            OnPropertyChanged(this, "RenderTexture", RenderTexture, RenderTexture, sourceTexture);
        }
        
        public void DoDispose()
        {
            OnRenderPropertyChanged = null;
            if (renderTexture != null && renderTexture.IsCreated())
            {
                renderTexture.ReleaseTexture();
                renderTexture = null;
            }
            if (maskRenderTexture != null && maskRenderTexture.IsCreated())
            {
                maskRenderTexture.ReleaseTexture();
                maskRenderTexture = null;
            }
            sourceTexture = null;
            RemoveMask();
            commandBufferBuilder.Release();
            commandBufferBuilder = null;
        }
    }
}