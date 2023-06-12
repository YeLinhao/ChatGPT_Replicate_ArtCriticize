using UnityEngine;
using XDPaint.Controllers.InputData.Base;
using XDPaint.Tools.Raycast;

namespace XDPaint.Controllers.InputData
{
    public class InputDataMesh : InputDataBase
    {
        private Ray? ray;
        private Triangle triangle;
        
        public override void OnUpdate()
        {
            base.OnUpdate();
            ray = null;
            triangle = null;
        }

        protected override void OnHoverSuccess(Vector3 position, Triangle triangleData)
        {
            ray = Camera.ScreenPointToRay(position);
            RaycastController.Instance.Raycast(ray.Value, out triangle);
            if (triangle != null)
            {
                base.OnHoverSuccess(position, triangle);
            }
            else
            {
                base.OnHoverFailed();
            }
        }

        protected override void OnDownSuccess(Vector3 position, float pressure = 1.0f)
        {
            IsOnDownSuccess = true;
            if (ray == null || IsShouldRaycast)
            {
                ray = Camera.ScreenPointToRay(position);
            }
            if (triangle == null || IsShouldRaycast)
            {
                RaycastController.Instance.Raycast(ray.Value, out triangle);
            }
            IsShouldRaycast = false;
            OnDownSuccessInvoke(position, pressure, triangle);
        }

        public override void OnPress(Vector3 position, float pressure = 1.0f)
        {
            if (IsOnDownSuccess)
            {
                if (ray == null || IsShouldRaycast)
                {
                    ray = Camera.ScreenPointToRay(position);
                }
                if (triangle == null || IsShouldRaycast)
                {
                    RaycastController.Instance.Raycast(ray.Value, out triangle);
                }
                IsShouldRaycast = false;
                OnPressInvoke(position, pressure, triangle);
            }
        }
    }
}