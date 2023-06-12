using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using XDPaint.Core.Layers;

namespace XDPaint.States
{
    [Serializable]
    public class StatesController : IStatesController
    {
        public event Action<RenderTexture> OnClearTextureAction;
        public event Action OnRenderTextureAction;
        public event Action OnChangeState;
        public event Action OnResetState;
        public event Action OnUndo;
        public event Action OnRedo;
        public event Action<bool> OnUndoStatusChanged;
        public event Action<bool> OnRedoStatusChanged;

        private List<List<BaseChangeRecord>> statesGroups = new List<List<BaseChangeRecord>>();
        private List<RenderTextureChangeRecord> dirtyRenderTextureRecords = new List<RenderTextureChangeRecord>();
        private List<RenderTextureChangeReference> renderTextureChanges = new List<RenderTextureChangeReference>();
#if UNITY_EDITOR && XDP_DEBUG
        public List<RenderTextureChangeRecord> texturesStates = new List<RenderTextureChangeRecord>();
#endif

        private ILayersController layersController;
        private List<BaseChangeRecord> currentGroup;
        private int currentGroupIndex = -1;
        private bool isUndoRedo;
        private bool isGroupingEnabled;
        private bool isEnabled;

        public int ChangesCount => statesGroups.Count;

        public IStatesController GetStatesController()
        {
            return this;
        }

        public void DoDispose()
        {
            foreach (var stateGroup in statesGroups)
            {
                foreach (var state in stateGroup)
                {
                    if (state is RenderTextureChangeRecord renderTextureChangeRecord)
                    {
                        renderTextureChangeRecord.DoDispose();
                    }
                }
            }
            statesGroups.Clear();

            foreach (var dirtyRenderTextureChangeRecord in dirtyRenderTextureRecords)
            {
                dirtyRenderTextureChangeRecord?.DoDispose();
            }
            dirtyRenderTextureRecords.Clear();
            
            renderTextureChanges.Clear();
            currentGroupIndex = -1;
#if UNITY_EDITOR && XDP_DEBUG
            texturesStates.Clear();
#endif
        }

        public void Init(ILayersController layersControllerInstance)
        {
            layersController = layersControllerInstance;
        }

        public void Enable()
        {
            isEnabled = true;
        }

        public void Disable()
        {
            isEnabled = false;
        }
        
        public void AddState(Action action)
        {
            if (isUndoRedo || !isEnabled)
                return;
            
            var willAddNewGroup = isGroupingEnabled && !statesGroups.Contains(currentGroup) || !isGroupingEnabled;
            if (willAddNewGroup)
            {
                UpdateChanges();
            }
            
            var record = new ActionRecord(action);
            if (isGroupingEnabled)
            {
                if (currentGroup.Count == 0)
                {
                    statesGroups.Add(currentGroup);
                }
                currentGroup.Add(record);
            }
            else
            {
                statesGroups.Add(new List<BaseChangeRecord> { record });
            }
            OnAddState();
        }

        public void AddState(object entity, string property, RenderTexture oldValue, RenderTexture newValue, Texture source)
        {
            if (isUndoRedo || !isEnabled)
                return;
            
            var willAddNewGroup = isGroupingEnabled && !statesGroups.Contains(currentGroup) || !isGroupingEnabled;
            if (willAddNewGroup)
            {
                UpdateChanges();
            }

            Texture sourceTexture;
            var stateGroupIndex = Mathf.Clamp(currentGroupIndex, 0, currentGroupIndex);
            if (currentGroupIndex >= StatesSettings.Instance.UndoRedoMaxActionsCount - 1)
            {
                renderTextureChanges.RemoveAll(x => x.StateGroupIndex > currentGroupIndex);
            }
            else
            {
                renderTextureChanges.RemoveAll(x => x.StateGroupIndex >= currentGroupIndex);
            }
            var textureChange = renderTextureChanges.FirstOrDefault(x => x.ChangedObject == entity && x.PropertyName == property);
            var dirtyRecord = dirtyRenderTextureRecords.FirstOrDefault(x => x.Entity == entity && x.Property == property);
            if (textureChange == null && dirtyRecord == null)
            {
                sourceTexture = source;
                renderTextureChanges.Add(new RenderTextureChangeReference(entity, property, stateGroupIndex));
            }
            else
            {
                sourceTexture = GetPreviousRenderTexture(entity, property);
            }
            
            var record = new RenderTextureChangeRecord(entity, property, oldValue, newValue, sourceTexture)
            {
                OnAction = OnRenderTextureAction,
                OnClearTexture = OnClearTextureAction
            };
            if (isGroupingEnabled)
            {
                if (currentGroup.Count == 0)
                {
                    statesGroups.Add(currentGroup);
                }
                currentGroup.Add(record);
            }
            else
            {         
                statesGroups.Add(new List<BaseChangeRecord> { record });
            }
#if UNITY_EDITOR && XDP_DEBUG
            texturesStates.Add(record);
#endif
            OnAddState();
        }

        public void AddState(object entity, string property, object oldValue, object newValue)
        {
            if (isUndoRedo || !isEnabled)
                return;
            
            var willAddNewGroup = isGroupingEnabled && !statesGroups.Contains(currentGroup) || !isGroupingEnabled;
            if (willAddNewGroup)
            {
                UpdateChanges();
            }
            
            var record = new PropertyChangeRecord(entity, property, oldValue, newValue);
            if (isGroupingEnabled)
            {
                if (currentGroup.Count == 0)
                {
                    statesGroups.Add(currentGroup);
                }
                currentGroup.Add(record);
            }
            else
            {
                statesGroups.Add(new List<BaseChangeRecord> { record });
            }
            OnAddState();
        }

        public void AddState(IList collection, NotifyCollectionChangedEventArgs rawEventArg)
        {
            if (isUndoRedo || !isEnabled)
                return;
            
            var willAddNewGroup = isGroupingEnabled && !statesGroups.Contains(currentGroup) || !isGroupingEnabled;
            if (willAddNewGroup)
            {
                UpdateChanges();
            }
            
            var record = new CollectionChangeRecord(collection, rawEventArg);
            if (isGroupingEnabled)
            {
                if (currentGroup.Count == 0)
                {
                    statesGroups.Add(currentGroup);
                }
                currentGroup.Add(record);
            }
            else
            {
                statesGroups.Add(new List<BaseChangeRecord> { record });
            }
            OnAddState();
        }

        public void Undo()
        {
            if (!isEnabled)
                return;
            
            var index = currentGroupIndex - 1;
            if (index >= 0)
            {
                OnResetState?.Invoke();
                OnChangeState?.Invoke();
                isUndoRedo = true;
                for (var i = statesGroups[index].Count - 1; i >= 0; i--)
                {
                    statesGroups[index][i].Undo();
                }
                isUndoRedo = false;
            }
            currentGroupIndex = Mathf.Clamp(currentGroupIndex - 1, -1, statesGroups.Count);
            UpdateStatus();
            OnUndo?.Invoke();
        }

        public void Redo()
        {
            if (currentGroupIndex == statesGroups.Count || !isEnabled)
                return;

            var index = currentGroupIndex;
            OnChangeState?.Invoke();
            isUndoRedo = true;
            for (var i = 0; i < statesGroups[index].Count; i++)
            {
                var state = statesGroups[index][i];
                state.Redo();
            }
            isUndoRedo = false;
            currentGroupIndex = Mathf.Clamp(index + 1, 0, statesGroups.Count);
            UpdateStatus();
            OnRedo?.Invoke();
        }

        public int GetUndoActionsCount()
        {
            return currentGroupIndex == -1 ? statesGroups.Count : currentGroupIndex;
        }

        public int GetRedoActionsCount()
        {
            if (currentGroupIndex == -1)
                return -1;

            return statesGroups.Count - currentGroupIndex;
        }
        
        /// <summary>
        /// Returns if can Undo
        /// </summary>
        /// <returns></returns>
        public bool CanUndo()
        {
            return statesGroups.Count > 0 && currentGroupIndex > 0;
        }

        /// <summary>
        /// Returns if can Redo
        /// </summary>
        /// <returns></returns>
        public bool CanRedo()
        {
            return statesGroups.Count > 0 && currentGroupIndex < statesGroups.Count;
        }

        /// <summary>
        /// Enable states grouping - all states before invoking DisableGrouping() will be in the same group
        /// </summary>
        public void EnableGrouping()
        {
            if (!isEnabled)
                return;
            
            isGroupingEnabled = true;
            currentGroup = new List<BaseChangeRecord>();
        }

        /// <summary>
        /// Disable states grouping
        /// </summary>
        public void DisableGrouping()
        {
            if (!isEnabled)
                return;

            isGroupingEnabled = false;
            currentGroup = null;
        }
        
        private RenderTexture GetPreviousRenderTexture(object entity, string property)
        {
            RenderTexture previousTexture = null;
            var index = Mathf.Clamp(currentGroupIndex, 0, currentGroupIndex);
            if (index > 0 && statesGroups.Count > 0)
            {
                for (var i = index - 1; i >= 0; i--)
                {
                    var group = statesGroups[i];
                    for (var j = group.Count - 1; j >= 0; j--)
                    {
                        var state = statesGroups[i][j];
                        if (state is RenderTextureChangeRecord p && p.Entity == entity && p.Property == property)
                        {
                            previousTexture = p.NewTexture;
                            break;
                        }
                    }
                    if (previousTexture != null)
                    {
                        break;
                    }
                }
            }
            if (previousTexture == null && dirtyRenderTextureRecords.Count > 0)
            {
                foreach (var dirtyRecord in dirtyRenderTextureRecords)
                {
                    if (dirtyRecord.Entity == entity && dirtyRecord.Property == property)
                    {
                        previousTexture = dirtyRecord.NewTexture;
                        break;
                    }
                }
            }
            return previousTexture;
        }

        private void UpdateChanges()
        {
            if (currentGroupIndex != -1)
            {
                for (var i = statesGroups.Count - 1; i >= currentGroupIndex; i--)
                {
                    var group = statesGroups[i];
                    foreach (var state in group)
                    {
                        if (state is RenderTextureChangeRecord renderTextureChangeRecord && !dirtyRenderTextureRecords.Contains(renderTextureChangeRecord))
                        {
                            renderTextureChangeRecord.DoDispose();
                        }
                    }
                }

                if (currentGroupIndex >= StatesSettings.Instance.UndoRedoMaxActionsCount)
                {
                    if (statesGroups.Count > 0)
                    {
                        var firstGroup = statesGroups[0];
                        foreach (var groupItem in firstGroup)
                        {
                            if (groupItem is RenderTextureChangeRecord r)
                            {
                                r.ReleaseOldTexture();
                                var dirtyRenderTextureRecord = dirtyRenderTextureRecords.FirstOrDefault(x => x.Entity == r.Entity && x.Property == r.Property);
                                if (dirtyRenderTextureRecord != null)
                                {
                                    dirtyRenderTextureRecord.ReleaseOldTexture();
                                    dirtyRenderTextureRecords.Remove(dirtyRenderTextureRecord);
                                    dirtyRenderTextureRecords.Add(r);
                                    continue;
                                }
                                dirtyRenderTextureRecords.Add(r);
                            }
                        }

                        //remove dirty record when no layers in undo/redo stack
                        for (var i = dirtyRenderTextureRecords.Count - 1; i >= 0; i--)
                        {
                            var dirtyRenderTextureRecord = dirtyRenderTextureRecords[i];
                            var found = false;
                            if (layersController.Layers.Contains(dirtyRenderTextureRecord.Entity as ILayer))
                            {
                                continue;
                            }
                            
                            foreach (var group in statesGroups)
                            {
                                foreach (var state in group)
                                {
                                    if (state is CollectionChangeRecord c)
                                    {
                                        if (c.RawEventArgs.OldItems != null)
                                        {
                                            if (c.RawEventArgs.OldItems.Contains(dirtyRenderTextureRecord.Entity))
                                            {
                                                found = true;
                                                break;
                                            }
                                        }

                                        if (c.RawEventArgs.NewItems != null)
                                        {
                                            if (c.RawEventArgs.NewItems.Contains(dirtyRenderTextureRecord.Entity))
                                            {
                                                found = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                                
                                if (found)
                                {
                                    break;
                                }
                            }
                  
                            if (!found)
                            {
                                dirtyRenderTextureRecords.RemoveAt(i);
                            }
                        }
                    }

                    statesGroups = statesGroups.GetRange(1, statesGroups.Count - 1);
#if UNITY_EDITOR && XDP_DEBUG
                    texturesStates.Clear();
                    foreach (var group in statesGroups)
                    {
                        foreach (var state in group)
                        {
                            if (state is RenderTextureChangeRecord s)
                            {
                                texturesStates.Add(s);
                            }
                        }
                    }
#endif
                }
                else if (statesGroups.Count > currentGroupIndex)
                {
                    statesGroups = statesGroups.GetRange(0, currentGroupIndex);
#if UNITY_EDITOR && XDP_DEBUG
                    texturesStates.Clear();
                    foreach (var group in statesGroups)
                    {
                        foreach (var state in group)
                        {
                            if (state is RenderTextureChangeRecord s)
                            {
                                texturesStates.Add(s);
                            }
                        }
                    }
#endif
                }
                currentGroupIndex = statesGroups.Count;
            }
        }
        
        private void UpdateStatus()
        {
            OnUndoStatusChanged?.Invoke(CanUndo());
            OnRedoStatusChanged?.Invoke(CanRedo());
        }

        private void OnAddState()
        {
            currentGroupIndex = statesGroups.Count;
            UpdateStatus();
        }
    }
}