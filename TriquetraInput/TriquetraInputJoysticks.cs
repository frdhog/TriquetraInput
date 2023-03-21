using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.DirectInput;

namespace Triquetra.Input
{
    public static class TriquetraInputJoysticks
    {
        private static List<TriquetraJoystick> activeJoysticks = new List<TriquetraJoystick>();
        private static List<Binding> keyboardBindings = new List<Binding>();

        public static void PopulateActiveJoysticks()
        {
            activeJoysticks.Clear();
            keyboardBindings.Clear();
            // Logger.WriteLine("Populating active joysticks");
            foreach (Binding binding in Binding.Bindings)
            {
                if (binding.IsKeyboard)
                {
                    keyboardBindings.Add(binding);
                    continue;
                }
                if (binding.JoystickDevice == null)
                    continue;
                // Only if this is a new, unique joystick
                if (!activeJoysticks.Any(x => binding.Controller.Properties.JoystickId == x.Properties.JoystickId))
                {
                    activeJoysticks.Add(binding.Controller);
                }
            }
        }

        public static void PollActiveJoysticks()
        {
            if (activeJoysticks == null || activeJoysticks.Count == 0)
                return;
            foreach(TriquetraJoystick joystick in activeJoysticks)
            {
                joystick.Poll();
            }
        }

        public static void HandleKeyboardBindings()
        {
            foreach (Binding binding in keyboardBindings)
            {
                if (binding.KeyboardKey == null)
                    continue;
                binding.HandleKeyboardKeys();
            }
        }
    }
}
