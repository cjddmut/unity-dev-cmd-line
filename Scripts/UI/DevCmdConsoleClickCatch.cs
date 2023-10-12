using UnityEngine;
using UnityEngine.EventSystems;

namespace DevCmdLine.UI
{
    internal class DevCmdConsoleClickCatch : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            DevCmdConsole.CloseConsoleWithCallback();
        }
    }
}