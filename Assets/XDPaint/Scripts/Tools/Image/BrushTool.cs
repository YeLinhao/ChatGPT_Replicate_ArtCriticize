using System;
using UnityEngine.Scripting;
using XDPaint.Core;
using XDPaint.Tools.Image.Base;

namespace XDPaint.Tools.Image
{
    [Serializable]
    public class BrushTool : BasePaintTool
    {
        [Preserve] public BrushTool(IPaintData paintData) : base(paintData) { }
        
        public override PaintTool Type => PaintTool.Brush;
    }
}