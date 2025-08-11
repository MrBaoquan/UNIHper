using System.Linq;
using UnityEngine;

namespace UNIHper
{
    public static class InputExtension
    {
#if ENABLE_INPUT_SYSTEM
        public static bool HasAnyInput(this UnityEngine.InputSystem.Keyboard keyboard)
        {
            if (keyboard == null)
                return false;
            return keyboard.anyKey.wasPressedThisFrame;
        }

        public static bool HasAnyInput(this UnityEngine.InputSystem.Mouse mouse)
        {
            if (mouse == null)
                return false;

            return mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame;
        }

        public static bool HasAnyInput(this UnityEngine.InputSystem.Touchscreen touchscreen)
        {
            if (touchscreen == null)
                return false;
            return touchscreen.touches.Any(_touch => _touch.press.wasPressedThisFrame);
        }
#endif
    }
}
