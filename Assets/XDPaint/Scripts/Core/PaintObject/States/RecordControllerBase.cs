using System;
using System.ComponentModel;
using UnityEngine;

namespace XDPaint.States
{
    public class RecordControllerBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private IStatesController statesControllerRoot;
        private readonly RecordControllerBase recordControllerParent;

        protected RecordControllerBase(RecordControllerBase parent)
        {
            recordControllerParent = parent;
        }
        
        public void SetStateController(IStatesController statesController)
        {
            statesControllerRoot = statesController;
        }

        private IStatesController GetRoot()
        {
            if (recordControllerParent != null)
            {
                return recordControllerParent.GetRoot();
            }
            return statesControllerRoot;
        }
        
        protected void OnPropertyChanged(string propertyName, object oldValue, object newValue)
        {
            if (!StatesSettings.Instance.EnableUndoRedoForPropertiesAndActions)
                return;
            
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            GetRoot()?.GetStatesController().AddState(this, propertyName, oldValue, newValue);
        }
        
        protected void OnPropertyChanged(object entity, string property, RenderTexture oldValue, RenderTexture newValue, Texture sourceTexture)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RenderTexture"));
            GetRoot()?.GetStatesController().AddState(entity, property, oldValue, newValue, sourceTexture);
        }

        protected void OnDidAction(Action action)
        {
            if (!StatesSettings.Instance.EnableUndoRedoForPropertiesAndActions)
                return;
            
            GetRoot()?.GetStatesController().AddState(action);
        }

        protected void EnableStatesGrouping()
        {
            GetRoot()?.GetStatesController().EnableGrouping();
        }

        protected void DisableStatesGrouping()
        {
            GetRoot()?.GetStatesController().DisableGrouping();
        }
    }
}