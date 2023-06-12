using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XDPaint.Tools;
using XDPaint.Tools.Raycast;
using IDisposable = XDPaint.Core.IDisposable;
using Object = UnityEngine.Object;

namespace XDPaint.Controllers.InputData.Base
{
    public abstract class InputDataBase : IDisposable
    {
        public event Action<Vector3, Triangle> OnHoverSuccessHandler;
        public event Action<Vector3, Triangle> OnHoverFailedHandler;
        public event Action<Vector3, float, Triangle> OnDownHandler;
        public event Action<Vector3, float, Triangle> OnPressHandler;
        public event Action<Vector3> OnUpHandler;

        protected Camera Camera;
        protected bool IsOnDownSuccess;
        protected bool IsShouldRaycast;
        private PaintManager paintManager;
        private List<CanvasGraphicRaycaster> raycasters;
        private Dictionary<CanvasGraphicRaycaster, List<RaycastResult>> raycastResults;
        private bool canHover;
        
        public virtual void Init(PaintManager paintManagerInstance, Camera camera)
        {
            Camera = camera;
            paintManager = paintManagerInstance;
            raycasters = new List<CanvasGraphicRaycaster>();
            raycastResults = new Dictionary<CanvasGraphicRaycaster, List<RaycastResult>>();
            if (Settings.Instance.CheckCanvasRaycasts)
            {
                if (paintManager.ObjectForPainting.TryGetComponent<RawImage>(out var rawImage) && rawImage.canvas != null)
                {
                    if (!rawImage.canvas.TryGetComponent<CanvasGraphicRaycaster>(out var graphicRaycaster))
                    {
                        graphicRaycaster = rawImage.canvas.gameObject.AddComponent<CanvasGraphicRaycaster>();
                    }
                    if (!raycasters.Contains(graphicRaycaster))
                    {
                        raycasters.Add(graphicRaycaster);
                    }
                }

                var canvas = InputController.Instance.Canvas;
                if (canvas == null)
                {
                    canvas = Object.FindObjectOfType<Canvas>();
                }

                if (canvas != null)
                {
                    if (!canvas.TryGetComponent<CanvasGraphicRaycaster>(out var graphicRaycaster))
                    {
                        graphicRaycaster = canvas.gameObject.AddComponent<CanvasGraphicRaycaster>();
                    }
                    if (!raycasters.Contains(graphicRaycaster))
                    {
                        raycasters.Add(graphicRaycaster);
                    }
                }
            }
        }
        
        public void DoDispose()
        {
            raycasters.Clear();
            raycastResults.Clear();
        }

        public virtual void OnUpdate()
        {
            
        }

        public void OnHover(Vector3 position)
        {
            if (Settings.Instance.CheckCanvasRaycasts && raycasters.Count > 0)
            {
                raycastResults.Clear();
                foreach (var raycaster in raycasters)
                {
                    var result = raycaster.GetRaycasts(position);
                    if (result != null)
                    {
                        raycastResults.Add(raycaster, result);
                    }
                }

                if (canHover && (raycastResults.Count == 0 || CheckRaycasts()))
                {
                    OnHoverSuccess(position, null);
                }
                else
                {
                    OnHoverFailed();
                }
            }
            else
            {
                OnHoverSuccess(position, null);
            }
        }
        
        protected virtual void OnHoverSuccess(Vector3 position, Triangle triangle)
        {
            OnHoverSuccessHandler?.Invoke(position, triangle);
        }
        
        protected virtual void OnHoverFailed()
        {
            OnHoverFailedHandler?.Invoke(Vector4.zero, null);
        }

        public void OnDown(Vector3 position, float pressure = 1.0f)
        {
            if (Settings.Instance.CheckCanvasRaycasts && raycasters.Count > 0)
            {
                raycastResults.Clear();
                foreach (var raycaster in raycasters)
                {
                    var result = raycaster.GetRaycasts(position);
                    if (result != null)
                    {
                        raycastResults.Add(raycaster, result);
                    }
                }
                if (raycastResults.Count == 0 || CheckRaycasts())
                {
                    OnDownSuccess(position, pressure);
                }
                else
                {
                    canHover = false;
                    OnDownFailed(position, pressure);
                }
            }
            else
            {
                OnDownSuccess(position, pressure);
            }
        }
        
        protected virtual void OnDownSuccess(Vector3 position, float pressure = 1.0f)
        {
            OnDownSuccessInvoke(position, pressure);
            IsOnDownSuccess = true;
        }

        protected virtual void OnDownSuccessInvoke(Vector3 position, float pressure = 1.0f, Triangle triangle = null)
        {
            OnDownHandler?.Invoke(position, pressure, triangle);
        }
        
        protected virtual void OnDownFailed(Vector3 position, float pressure = 1.0f)
        {
            IsOnDownSuccess = false;
        }

        public virtual void OnPress(Vector3 position, float pressure = 1.0f)
        {
            if (IsOnDownSuccess)
            {
                OnPressInvoke(position, pressure);
            }
        }

        protected void OnPressInvoke(Vector3 position, float pressure = 1.0f, Triangle triangle = null)
        {
            OnPressHandler?.Invoke(position, pressure, triangle);
        }

        public virtual void OnUp(Vector3 position)
        {
            if (IsOnDownSuccess)
            {
                OnUpHandler?.Invoke(position);
            }
            canHover = true;
        }

        private bool CheckRaycasts()
        {
            var result = true;
            if (raycastResults.Count > 0)
            {
                var ignoreRaycasts = InputController.Instance.IgnoreForRaycasts;
                foreach (var raycaster in raycastResults.Keys)
                {
                    if (raycastResults[raycaster].Count > 0)
                    {
                        var raycast = raycastResults[raycaster][0];
                        if (raycast.gameObject == paintManager.ObjectForPainting.gameObject && paintManager.Initialized)
                        {
                            continue;
                        }

                        if (!ignoreRaycasts.Contains(raycast.gameObject))
                        {
                            result = false;
                            break;
                        }
                    }
                }
            }
            return result;
        }
    }
}