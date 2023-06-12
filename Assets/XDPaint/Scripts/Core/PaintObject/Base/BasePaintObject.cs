using System;
using UnityEngine;
using XDPaint.Core.Materials;
using XDPaint.States;
using XDPaint.Tools.Raycast;

namespace XDPaint.Core.PaintObject.Base
{
    public abstract class BasePaintObject : BasePaintObjectRenderer
    {
        #region Events
        
        public event Action<Vector3, Vector2, Vector2, Vector2, float> OnMouseHoverHandler;
        public event Action<Vector3, Vector2, Vector2, Vector2, float> OnMouseDownHandler;
        public event Action<Vector3, Vector2, Vector2, Vector2, float> OnMouseHandler;
        public event Action<Vector2, bool> OnMouseUpHandler;
        public event Action<Vector2, float> OnDrawPointHandler;
        public event Action<Vector2, Vector2, float, float> OnDrawLineHandler;
        
        #endregion

        #region Properties and variables
        
        public bool IsPainting { get; private set; }
        public bool IsPainted { get; private set; }
        public bool ProcessInput = true;

        private Camera thisCamera;
        public new Camera Camera
        {
            protected get { return thisCamera; }
            set
            {
                thisCamera = value;
                base.Camera = thisCamera;
            }
        }

        protected Vector3? LocalPosition { get; set; }
        protected Vector2? PaintPosition { private get; set; }
        protected Transform ObjectTransform { get; private set; }
        
        private float pressure = 1f;
        private float Pressure
        {
            get => Mathf.Clamp(pressure, 0.01f, 10f);
            set => pressure = value;
        }

        private IStatesController statesController;
        private LineData lineData;
        private Vector2 previousPaintPosition;
        private bool shouldClearTexture = true;
        private bool writeClear;
        private const float HalfTextureRatio = 0.5f;
        
        #endregion

        #region Abstract methods
        
        protected abstract void Init();
        protected abstract void CalculatePaintPosition(Vector3 position, Vector2? uv = null, bool usePostPaint = true);
        protected abstract bool IsInBounds(Vector3 position);

        #endregion

        public void Init(Camera camera, Transform objectTransform, Paint paint, 
            IRenderTextureHelper renderTextureHelper, IStatesController states)
        {
            thisCamera = camera;
            ObjectTransform = objectTransform;
            PaintMaterial = paint;
            RenderTextureHelper = renderTextureHelper;
            statesController = states;
            InitRenderer(Camera, PaintMaterial);
            lineData = new LineData();
            InitStatesController();
            Init();
        }

        public override void DoDispose()
        {
            if (statesController != null)
            {
                statesController.OnRenderTextureAction -= OnExtraDraw;
                statesController.OnClearTextureAction -= OnClearTexture;
                statesController.OnResetState -= OnResetState;
            }
            base.DoDispose();
        }

        private void InitStatesController()
        {
            if (statesController == null)
                return;
            
            statesController.OnRenderTextureAction += OnExtraDraw;
            statesController.OnClearTextureAction += OnClearTexture;
            statesController.OnResetState += OnResetState;
        }

        private void OnResetState()
        {
            shouldClearTexture = true;
        }

        #region Input

        public void OnMouseHover(Vector3 position, Triangle triangle = null)
        {
            if (ObjectTransform == null)
            {
                Debug.LogError("ObjectForPainting has been destroyed!");
                return;
            }
            if (!ProcessInput || !ObjectTransform.gameObject.activeInHierarchy)
                return;
            
            if (!IsPainting)
            {
                if (triangle != null)
                {
                    CalculatePaintPosition(position, triangle.UVHit, false);
                    LocalPosition = triangle.Hit;
                    if (OnMouseHoverHandler != null && LocalPosition != null && PaintPosition != null)
                    {
                        OnMouseHoverHandler(LocalPosition.Value, position, triangle.UVHit, PaintPosition.Value, 1f);
                    }
                }
                else 
                {
                    CalculatePaintPosition(position, null, false);
                    if (OnMouseHoverHandler != null && LocalPosition != null && PaintPosition != null)
                    {
                        var uv = new Vector2(
                            PaintPosition.Value.x / PaintMaterial.SourceTexture.width, 
                            PaintPosition.Value.y / PaintMaterial.SourceTexture.height);
                        OnMouseHoverHandler(LocalPosition.Value, position, uv, PaintPosition.Value, 1f);
                    }
                }
            }
        }

        public void OnMouseHoverFailed(Vector3 position, Triangle triangle = null)
        {
            InBounds = false;
        }

        public void OnMouseDown(Vector3 position, float pressure = 1f, Triangle triangle = null)
        {
            if (ObjectTransform == null)
            {
                Debug.LogError("ObjectForPainting has been destroyed!");
                return;
            }
            if (!ProcessInput || !ObjectTransform.gameObject.activeInHierarchy)
                return;
            if (triangle != null && triangle.Transform != ObjectTransform)
                return;
            IsPaintingDone = false;
            InBounds = false;
            Pressure = pressure;

            if (!IsPainting && PaintPosition == null)
            {
                if (triangle != null)
                {
                    CalculatePaintPosition(position, triangle.UVHit);
                    LocalPosition = triangle.Hit;
                }
                else
                {
                    CalculatePaintPosition(position);
                }
            }
            
            if (PaintPosition != null && LocalPosition != null)
            {
                if (OnMouseDownHandler != null)
                {
                    if (triangle == null)
                    {
                        if (IsInBounds(position))
                        {
                            var uv = new Vector2(PaintPosition.Value.x / PaintMaterial.SourceTexture.width,  PaintPosition.Value.y / PaintMaterial.SourceTexture.height);
                            OnMouseDownHandler(LocalPosition.Value, position, uv, PaintPosition.Value, Pressure);
                        }
                    }
                    else
                    {
                        OnMouseDownHandler(LocalPosition.Value, position, triangle.UVHit, PaintPosition.Value, Pressure);
                    }
                }
            }
        }

        public void OnMouseButton(Vector3 position, float brushPressure = 1f, Triangle triangle = null)
        {
            if (ObjectTransform == null)
            {
                Debug.LogError("ObjectForPainting has been destroyed!");
                return;
            }
            if (!ProcessInput || !ObjectTransform.gameObject.activeInHierarchy)
                return;

            if (triangle == null)
            {
                IsPainting = true;
                lineData.AddBrush(brushPressure * Brush.Size);
                CalculatePaintPosition(position);
                Pressure = brushPressure;
                if (PaintPosition != null)
                {
                    IsPainting = true;
                }
                if (InBounds && LocalPosition != null && PaintPosition != null && OnMouseHandler != null)
                {
                    var uv = new Vector2(PaintPosition.Value.x / PaintMaterial.SourceTexture.width, PaintPosition.Value.y / PaintMaterial.SourceTexture.height);
                    OnMouseHandler(LocalPosition.Value, position, uv, PaintPosition.Value, Pressure);
                }
            }
            else if (triangle.Transform == ObjectTransform)
            {
                IsPainting = true;
                lineData.AddTriangleBrush(triangle, brushPressure * Brush.Size);
                Pressure = brushPressure;
                CalculatePaintPosition(position, triangle.UVHit);
                LocalPosition = triangle.Hit;
                if (OnMouseHandler != null && LocalPosition != null && PaintPosition != null)
                {
                    OnMouseHandler(LocalPosition.Value, position, triangle.UVHit, PaintPosition.Value, Pressure);
                }
            }
            else
            {
                LocalPosition = null;
                PaintPosition = null;
                lineData.Clear();
            }
        }

        public void OnMouseUp(Vector3 position)
        {
            if (ObjectTransform == null)
            {
                Debug.LogError("ObjectForPainting has been destroyed!");
                return;
            }
            if (!ProcessInput || !ObjectTransform.gameObject.activeInHierarchy)
                return;
            FinishPainting();
            OnMouseUpHandler?.Invoke(position, IsInBounds(position));
        }

        public Vector2? GetPaintPosition(Vector3 position, Triangle triangle = null)
        {
            if (ObjectTransform == null)
            {
                Debug.LogError("ObjectForPainting has been destroyed!");
                return null;
            }
            if (!ProcessInput || !ObjectTransform.gameObject.activeInHierarchy)
                return null;
            if (triangle != null && triangle.Transform != ObjectTransform)
                return null;

            if (triangle != null)
            {
                CalculatePaintPosition(position, triangle.UVHit, false);
            }
            else
            {
                CalculatePaintPosition(position, null, false);
            }

            if (InBounds && PaintPosition != null)
            {
                return PaintPosition.Value;
            }
            return null;
        }

        #endregion
        
        #region DrawFromCode
        
        /// <summary>
        /// Draws point with pressure
        /// </summary>
        /// <param name="position"></param>
        /// <param name="brushPressure"></param>
        public void DrawPoint(Vector2 position, float brushPressure = 1f)
        {
            Pressure = brushPressure;
            PaintPosition = position;
            IsPainting = true;
            IsPaintingDone = true;
            OnDrawPointHandler?.Invoke(position, brushPressure);
            lineData.Clear();
            lineData.AddPosition(position);
            OnRender();
            Render();
            FinishPainting();
        }
        
        /// <summary>
        /// Draws line with pressure
        /// </summary>
        /// <param name="positionStart"></param>
        /// <param name="positionEnd"></param>
        /// <param name="pressureStart"></param>
        /// <param name="pressureEnd"></param>
        public void DrawLine(Vector2 positionStart, Vector2 positionEnd, float pressureStart = 1f, float pressureEnd = 1f)
        {
            Pressure = pressureEnd;
            PaintPosition = positionEnd;
            IsPainting = true;
            IsPaintingDone = true;
            OnDrawLineHandler?.Invoke(positionStart, positionEnd, pressureStart, pressureEnd);
            lineData.Clear();
            lineData.AddBrush(pressureStart * Brush.Size);
            lineData.AddBrush(Pressure * Brush.Size);
            lineData.AddPosition(positionStart);
            lineData.AddPosition(positionEnd);
            OnRender();
            Render();
            FinishPainting();
        }
        
        #endregion

        /// <summary>
        /// Resets all states, bake paint result into PaintTexture, save paint result to TextureKeeper
        /// </summary>
        public void FinishPainting()
        {
            if (IsPainting)
            {
                Pressure = 1f;
                if (PaintMode.UsePaintInput)
                {
                    BakeInputToPaint();
                    ClearTexture(RenderTarget.Input);
                }
                IsPainting = false;
                if (IsPaintingDone)
                {
                    SaveUndoTexture();
                }
                lineData.Clear();
                if (!PaintMode.UsePaintInput)
                {
                    ClearTexture(RenderTarget.Input);
                    Render();
                }
            }
            
            PaintMaterial.SetPaintPreviewVector(Vector4.zero);
            LocalPosition = null;
            PaintPosition = null;
            IsPaintingDone = false;
            InBounds = false;
            previousPaintPosition = default;
        }

        /// <summary>
        /// Renders Points and Lines, restoring textures when Undo/Redo invoking
        /// </summary>
        public void OnRender()
        {
            if (shouldClearTexture)
            {
                ClearTexture(RenderTarget.Input);
                shouldClearTexture = false;
                if (writeClear && Tool.RenderToTextures)
                {
                    SaveUndoTexture();
                    writeClear = false;
                }
            }

            if (IsPainting && PaintPosition != null && 
                (!Tool.ConsiderPreviousPosition || previousPaintPosition != PaintPosition.Value) && Tool.AllowRender)
            {
                IsPainted = true;
                if (lineData.HasOnePosition())
                {
                    DrawPoint();
                    previousPaintPosition = PaintPosition.Value;
                }
                else if (Tool.CanDrawLines)
                {
                    DrawLine(!lineData.HasNotSameTriangles());
                    previousPaintPosition = PaintPosition.Value;
                }
            }
            else
            {
                IsPainted = false;
            }
        }

        /// <summary>
        /// Combines textures, render preview
        /// </summary>
        public void Render()
        {
            DrawPreProcess();
            ClearTexture(RenderTarget.Combined);
            DrawProcess();
        }

        public void RenderToTextureWithoutPreview(RenderTexture resultTexture)
        {
            DrawPreProcess();
            ClearTexture(RenderTarget.Combined);
            //disable preview
            var inBounds = InBounds;
            InBounds = false;
            DrawProcess();
            InBounds = inBounds;
            Graphics.Blit(RenderTextureHelper.GetTexture(RenderTarget.Combined), resultTexture);
        }

        private void SaveUndoTexture()
        {
            ActiveLayer().SaveState();
        }
        
        /// <summary>
        /// Restores texture when Undo/Redo invoking
        /// </summary>
        private void OnExtraDraw()
        {
            if (!PaintMode.UsePaintInput)
            {
                ClearTexture(RenderTarget.Input);
            }
            Render();
        }

        private void OnClearTexture(RenderTexture renderTexture)
        {
            ClearTexture(renderTexture, Color.clear);
            Render();
        }

        /// <summary>
        /// Gets position for draw point
        /// </summary>
        /// <param name="holePosition"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        private Rect GetPosition(Vector2 holePosition, float scale)
        {
            return new Rect(
                (holePosition.x - (HalfTextureRatio - Brush.RenderOffset.x) * Brush.RenderTexture.width * scale) / PaintMaterial.SourceTexture.width,
                (holePosition.y - (HalfTextureRatio - Brush.RenderOffset.y) * Brush.RenderTexture.height * scale) / PaintMaterial.SourceTexture.height,
                Brush.RenderTexture.width / (float)PaintMaterial.SourceTexture.width * scale,
                Brush.RenderTexture.height / (float)PaintMaterial.SourceTexture.height * scale);
        }
        
        /// <summary>
        /// Renders quad(point)
        /// </summary>
        private void DrawPoint()
        {
            OnDrawPointHandler?.Invoke(PaintPosition.Value, Brush.Size * Pressure);
            var positionRect = GetPosition(PaintPosition.Value, Brush.Size * Pressure);
            RenderQuad(positionRect);
        }

        /// <summary>
        /// Renders a few quads (line)
        /// </summary>
        /// <param name="interpolate"></param>
        private void DrawLine(bool interpolate)
        {
            Vector2[] positions;
            Vector2[] paintPositions;
            if (interpolate)
            {
                paintPositions = lineData.GetPositions();
                positions = paintPositions;
            }
            else
            {
                paintPositions = lineData.GetPositions();
                var triangles = lineData.GetTriangles();
                positions = GetLinePositions(paintPositions[0], paintPositions[1], triangles[0], triangles[1]);
            }
            if (positions.Length > 0)
            {
                var brushes = lineData.GetBrushes();
                if (brushes.Length != 2)
                {
                    Debug.LogWarning("Incorrect length of the brushes array!");
                }
                else
                {
                    OnDrawLineHandler?.Invoke(paintPositions[0], paintPositions[1], brushes[0], brushes[1]);
                    RenderLine(positions, Brush.RenderOffset, Brush.RenderTexture, Brush.Size, brushes);
                }
            }
        }

        /// <summary>
        /// Post paint method, used by CalculatePaintPosition method
        /// </summary>
        protected void OnPostPaint()
        {
            if (PaintPosition != null && IsPainting)
            {
                lineData.AddPosition(PaintPosition.Value);
            }
            else if (PaintPosition == null)
            {
                lineData.Clear();
            }
        }

        protected void UpdateBrushPreview()
        {
            if (Brush.Preview && InBounds)
            {
                if (PaintPosition != null)
                {
                    var previewVector = GetPreviewVector();
                    PaintMaterial.SetPaintPreviewVector(previewVector);
                }
                else
                {
                    PaintMaterial.SetPaintPreviewVector(Vector4.zero);
                }
            }
        }

        /// <summary>
        /// Returns Vector4 for brush preview
        /// </summary>
        /// <returns></returns>
        private Vector4 GetPreviewVector()
        {
            var brushRatio = new Vector2(
                PaintMaterial.SourceTexture.width / (float)Brush.RenderTexture.width,
                PaintMaterial.SourceTexture.height / (float)Brush.RenderTexture.height) / Brush.Size / Pressure;
            var brushOffset = new Vector4(
                PaintPosition.Value.x / PaintMaterial.SourceTexture.width * brushRatio.x + Brush.RenderOffset.x,
                PaintPosition.Value.y / PaintMaterial.SourceTexture.height * brushRatio.y + Brush.RenderOffset.y,
                brushRatio.x, brushRatio.y);
            return brushOffset;
        }
    }
}