using System;

namespace XDPaint.States
{
    public class ActionRecord : BaseChangeRecord
    {
        private Action action;

        public ActionRecord(Action action)
        {
            this.action = action;
        }

        public override void Redo()
        {
            action?.Invoke();
        }

        public override void Undo()
        {
            
        }
    }
}