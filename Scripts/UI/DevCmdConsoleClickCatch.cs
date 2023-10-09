using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace DevCmdLine.UI
{
    internal class DevCmdConsoleClickCatch : MonoBehaviour, IPointerClickHandler
    {
        public UnityEvent onClick;
        
        private DevCmdConsoleUI _ui;

        private void Awake()
        {
            _ui = GetComponentInParent<DevCmdConsoleUI>();

            if (_ui == null)
            {
                Debug.LogWarning($"Could not find DevCmdConsoleUI in parent! Disabling...");
                enabled = false;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (onClick != null)
            {
                onClick.Invoke();
            }
        }
    }
}