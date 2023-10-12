using DevCmdLine.UI;
using UnityEngine;

namespace DevCmdLine
{
    internal class DevCmdToggleSimple : MonoBehaviour
    {
        private void Update()
        {
            
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.backquoteKey.wasPressedThisFrame)
            {
                DevCmdConsole.ToggleConsole(DevCmdStartingSelectedButton.Input);
            }
            else if (UnityEngine.InputSystem.Gamepad.current != null &&
                     UnityEngine.InputSystem.Gamepad.current.selectButton.wasPressedThisFrame)
            {
                DevCmdConsole.ToggleConsole(DevCmdStartingSelectedButton.Option);
            }
#else
            if (Input.GetKeyDown(KeyCode.Tilde))
            {
                DevCmdConsole.ToggleConsole(DevCmdStartingSelectedButton.Input);
            }
#endif
        }
    }
}
