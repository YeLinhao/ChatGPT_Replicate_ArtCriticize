using XDPaint.Controllers.InputData.Base;
using XDPaint.Core;
using XDPaint.Tools;

namespace XDPaint.Controllers.InputData
{
    public class InputDataResolver
    {
        public InputDataBase Resolve(ObjectComponentType objectComponentType)
        {
            if (Settings.Instance.IsVRMode)
            {
                return new InputDataVR();
            }
            if (objectComponentType == ObjectComponentType.MeshFilter || objectComponentType == ObjectComponentType.SkinnedMeshRenderer)
            {
                return new InputDataMesh();
            }
            if (objectComponentType == ObjectComponentType.RawImage)
            {
                return new InputDataCanvas();
            }
            return new InputDataDefault();
        }
    }
}