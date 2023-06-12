using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace XDPaint.Tools.Raycast
{
    [RequireComponent(typeof(GraphicRaycaster))]
    public class CanvasGraphicRaycaster : MonoBehaviour
    {
        private GraphicRaycaster raycaster;
        private EventSystem eventSystem;
        private PointerEventData pointerEventData;

        void Start()
        {
            raycaster = GetComponent<GraphicRaycaster>();
            eventSystem = GetComponent<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = FindObjectOfType<EventSystem>();
            }
        }

        public List<RaycastResult> GetRaycasts(Vector2 position)
        {
            if (raycaster == null)
                return null;
            
            pointerEventData = new PointerEventData(eventSystem) {position = position};
            var results = new List<RaycastResult>();
            raycaster.Raycast(pointerEventData, results);
            return results;
        }
    }
}