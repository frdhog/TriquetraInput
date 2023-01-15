using System;
using System.Collections.Generic;
using SharpDX.DirectInput;

namespace Triquetra.Input
{
    public class TriquetraJoystick : Joystick
    {
        private static Dictionary<int, JoystickState> joystickStates = new Dictionary<int, JoystickState>();
        private static Dictionary<int, JoystickUpdate[]> rawStates = new Dictionary<int, JoystickUpdate[]>();
        private bool hasAcquired;

        public TriquetraJoystick(IntPtr nativePtr) : base(nativePtr)
        {
        }

        public TriquetraJoystick(DirectInput directInput, Guid deviceGuid) : base(directInput, deviceGuid)
        {
        }

        public bool HasAcquired { get => hasAcquired; private set => hasAcquired = value; }
        public JoystickState State { get
            {
                if (!joystickStates.ContainsKey(Properties.JoystickId))
                {
                    joystickStates.Add(Properties.JoystickId, new JoystickState());
                }
                return joystickStates[Properties.JoystickId];
            }
        }
        public JoystickUpdate[] RawState
        {
            get
            {
                if (!rawStates.ContainsKey(Properties.JoystickId))
                {
                    rawStates.Add(Properties.JoystickId, new JoystickUpdate[268]);
                }
                return rawStates[Properties.JoystickId];
            }
        }

        public new void Acquire()
        {
            hasAcquired = true;
            Properties.BufferSize = 128;
            base.Acquire();
        }

        public delegate void JoystickUpdated(TriquetraJoystick joystick, JoystickUpdate update);

        public event JoystickUpdated Updated;

        public new void Poll()
        {
            if (!hasAcquired)
            {
                Acquire();
            }
            base.Poll();

            JoystickUpdate[] updates = base.GetBufferedData();
            foreach (JoystickUpdate update in updates)
            {
                foreach(Binding binding in Binding.Bindings)
                {
                    if (binding.Controller.Properties.JoystickId == this.Properties.JoystickId)
                    {
                        if (binding.Offset == update.Offset)
                        {
                            binding.RunAction(update.Value);
                        }
                        binding.Controller.Updated?.Invoke(this, update);
                    }
                }
                State.Update(update);
                RawState[update.RawOffset] = update;
            }
        }
    }
}