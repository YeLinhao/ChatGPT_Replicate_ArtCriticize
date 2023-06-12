using UnityEngine;
using XDPaint.Controllers.InputData.Base;
using XDPaint.Tools.Raycast;

namespace XDPaint.Controllers.InputData
{
    public class InputDataVR : InputDataBase
    {
        private Ray? ray;
        private Triangle triangle;
        private Transform penTransform;
        private Vector3 penDirection;
        private Vector3 screenPoint = -Vector3.one;

        public override void Init(PaintManager paintManager, Camera camera)
        {
            base.Init(paintManager, camera);
            penTransform = InputController.Instance.PenTransform;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            ray = null;
            triangle = null;
        }

        protected override void OnHoverSuccess(Vector3 position, Triangle triangleData)
        {
            screenPoint = -Vector3.one;
            penDirection = penTransform.forward;
            ray = new Ray(penTransform.position, penDirection);
            RaycastController.Instance.Raycast(ray.Value, out triangle);
            if (triangle != null)
            { 
                screenPoint = Camera.WorldToScreenPoint(triangle.Hit);
                base.OnHoverSuccess(screenPoint, triangle);
            }
            else
            {
                base.OnHoverFailed();
            }
        }

        protected override void OnDownSuccess(Vector3 position, float pressure = 1.0f)
        {
            IsOnDownSuccess = true;
            if (ray == null)
            {
                ray = Camera.ScreenPointToRay(position);
            }
            if (triangle == null)
            {
                RaycastController.Instance.Raycast(ray.Value, out triangle);
            }
            if (triangle != null)
            {
                screenPoint = Camera.WorldToScreenPoint(triangle.Hit);
            }
            OnDownSuccessInvoke(screenPoint, pressure, triangle);
        }

        public override void OnPress(Vector3 position, float pressure = 1.0f)
        {
            if (IsOnDownSuccess)
            {
                if (ray == null)
                {
                    ray = Camera.ScreenPointToRay(position);
                }
                if (triangle == null)
                {
                    RaycastController.Instance.Raycast(ray.Value, out triangle);
                }
                if (triangle != null)
                {
                    screenPoint = Camera.WorldToScreenPoint(triangle.Hit);
                }
                OnPressInvoke(position, pressure, triangle);
            }
        }
    }
}