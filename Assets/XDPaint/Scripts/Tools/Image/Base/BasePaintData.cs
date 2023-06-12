using UnityEngine;
using XDPaint.Core;
using XDPaint.Core.Layers;
using XDPaint.Core.Materials;
using XDPaint.Core.PaintModes;
using XDPaint.Core.PaintObject.Base;
using XDPaint.Utils;

namespace XDPaint.Tools.Image.Base
{
    public class BasePaintData : IPaintData
    {
        public ILayersController LayersController => paintManager.LayersController;
        public IRenderTextureHelper TexturesHelper { get; }
        public IRenderComponentsHelper RenderComponents { get; }
        public IBrush Brush => paintManager.Brush;
        public IPaintMode PaintMode => paintManager.GetPaintMode();
        public Camera Camera => paintManager.Camera;
        public Material Material => paintManager.Material.Material;
        public CommandBufferBuilder CommandBuilder => commandBufferBuilder;
        public Mesh QuadMesh => quadMesh;
        public bool IsPainted => paintObject.IsPainted;
        public bool IsPainting => paintObject.IsPainting;
        public bool InBounds => paintObject.InBounds;

        private readonly PaintManager paintManager;
        private readonly BasePaintObject paintObject;
        private CommandBufferBuilder commandBufferBuilder;
        private Mesh quadMesh;

        public BasePaintData(PaintManager currentPaintManager, IRenderTextureHelper currentRenderTextureHelper, IRenderComponentsHelper componentsHelper)
        {
            paintManager = currentPaintManager;
            paintObject = paintManager.PaintObject;
            TexturesHelper = currentRenderTextureHelper;
            RenderComponents = componentsHelper;
            commandBufferBuilder = new CommandBufferBuilder();
            quadMesh = MeshGenerator.GenerateQuad(Vector3.one, Vector3.zero);
        }

        public virtual void Render()
        {
            paintManager.Render();
        }

        public void DoDispose()
        {
            if (commandBufferBuilder != null)
            {
                commandBufferBuilder.Release();
                commandBufferBuilder = null;
            }
            if (quadMesh != null)
            {
                Object.Destroy(quadMesh);
                quadMesh = null;
            }
        }
    }
}