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
            {
                consoleUI.ToggleConsole(DevCmdStartingSelectedButton.Input);
            }
            else if (UnityEngine.InputSystem.Gamepad.current != null &&
                     UnityEngine.InputSystem.Gamepad.current.selectButton.wasPressedThisFrame)
            {
                consoleUI.ToggleConsole(DevCmdStartingSelectedButton.Option);
            }
#else
            if (Input.GetKeyDown(KeyCode.Tilde))
            {
                consoleUI.ToggleConsole(DevCmdStartingSelectedButton.Input);
            }
#endif
        }
    }
}
