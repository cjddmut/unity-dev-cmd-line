using DevCmdLine.UI;
using UnityEngine;

namespace DevCmdLine
{
    internal class DevCmdToggleSimple : MonoBehaviour
    {
        public DevCmdConsoleUI consoleUI;
        
        private void Update()
        {
            
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.backquoteKey.wasPressedThisFrame)
#else
            if (Input.GetKeyDown(KeyCode.Tilde))
#endif
            {
                consoleUI.ToggleConsole();
            }
        }
    }
}
