using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using SharpDX.DirectInput;
using UnityEngine;
using UnityEngine.PlayerLoop;
using DeviceType = SharpDX.DirectInput.DeviceType;

namespace Triquetra.Input
{
    public class Binding
    {
        public const int AxisMin = 0;
        public const int AxisMiddle = 32768;
        public const int AxisMax = 65535;
        public const int ButtonMin = 0;
        public const int ButtonMax = 128;
        public const int Deadzone = 8192;
        public const int POVMin = 0;
        public const int POVMax = 36000;

        public static List<Binding> Bindings = new List<Binding>();
        public static DirectInput directInput = new DirectInput();

        public string Name = "New Binding";
        [XmlIgnore] public TriquetraJoystick Controller;
        public JoystickOffset Offset;
        [XmlIgnore] public int RawOffset => (int)Offset;
        public bool Invert;
        public AxisCentering AxisCentering = AxisCentering.Normal;
        public TwoAxis SelectedTwoAxis = TwoAxis.Positive;
        public POVFacing POVDirection = POVFacing.Up;
        public ControllerAction OutputAction = ControllerAction.None;
        public ThumbstickDirection ThumbstickDirection = ThumbstickDirection.None;
        public string VRInteractName = "";
        [XmlIgnore] public DeviceInstance JoystickDevice;

        // For the Xml Serialize/Deserialize
        public string ProductGuid
        {
            get
            {
                return Controller?.Information.ProductGuid.ToString() ?? "";
            }
            set
            {
                DeviceInstance device = directInput.GetDevices().Where(x => IsJoystick(x)).FirstOrDefault(x => x.ProductGuid.ToString() == value);
                if (device == null)
                    return;

                this.JoystickDevice = device;
                Controller = new TriquetraJoystick(directInput, JoystickDevice.InstanceGuid);
            }
        }

        public static bool IsButton(int offset) => offset >= (int)JoystickOffset.Buttons0 && offset <= (int)JoystickOffset.Buttons127;
        public static bool IsPOV(int offset) => offset >= (int)JoystickOffset.PointOfViewControllers0 && offset <= (int)JoystickOffset.PointOfViewControllers3;
        public static bool IsAxis(int offset) => !IsButton(offset) && !IsPOV(offset);

        [XmlIgnore] public bool IsOffsetButton => RawOffset >= (int)JoystickOffset.Buttons0 && RawOffset <= (int)JoystickOffset.Buttons127;
        [XmlIgnore] public bool IsOffsetPOV => RawOffset >= (int)JoystickOffset.PointOfViewControllers0 && RawOffset <= (int)JoystickOffset.PointOfViewControllers3;
        [XmlIgnore] public bool IsOffsetAxis => !IsOffsetButton && !IsOffsetPOV;

        [XmlIgnore] public bool OffsetSelectOpen = false;
        [XmlIgnore] public bool OutputActionSelectOpen = false;
        [XmlIgnore] public bool POVDirectionSelectOpen = false;
        [XmlIgnore] public bool DetectingOffset = false;
        [XmlIgnore] public bool ThumbstickDirectionSelectOpen = false;
        [XmlIgnore] public bool AxisCenteringSelectOpen = false;

        [XmlIgnore] public TriquetraJoystick.JoystickUpdated bindingDelegate;

        public Binding()
        {
            NextJoystick();
        }

        private int currentJoystickIndex = -1;
        public void NextJoystick()
        {
            List<DeviceInstance> devices = directInput.GetDevices().Where(x => IsJoystick(x)).ToList();
            if (devices.Count == 0)
            {
                return;
            }
            currentJoystickIndex = (currentJoystickIndex + 1) % devices.Count;

            this.JoystickDevice = devices[currentJoystickIndex];
            Controller = new TriquetraJoystick(directInput, JoystickDevice.InstanceGuid);
        }
        public void PrevJoystick()
        {
            List<DeviceInstance> devices = directInput.GetDevices().Where(x => IsJoystick(x)).ToList();
            if (devices.Count == 0)
            {
                return;
            }
            currentJoystickIndex = (currentJoystickIndex - 1) % devices.Count;

            this.JoystickDevice = devices[currentJoystickIndex];
            Controller = new TriquetraJoystick(directInput, JoystickDevice.InstanceGuid);
        }

        public bool IsJoystick(DeviceInstance deviceInstance)
        {
            return deviceInstance.Type == DeviceType.Joystick
                   || deviceInstance.Type == DeviceType.Gamepad
                   || deviceInstance.Type == DeviceType.FirstPerson
                   || deviceInstance.Type == DeviceType.Flight
                   || deviceInstance.Type == DeviceType.Driving
                   || deviceInstance.Type == DeviceType.Supplemental;
        }

        public void RunAction(int joystickValue)
        {
            if (OutputAction == ControllerAction.Print)
            {
                ControllerActions.Print(this, joystickValue);
            }
            if (TriquetraInput.IsFlyingScene()) // Only try and get throttle in a flying scene
            {
                if (OutputAction == ControllerAction.Throttle)
                {
                    ControllerActions.Throttle.SetThrottle(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.Pitch)
                {
                    ControllerActions.Joystick.SetPitch(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.Yaw)
                {
                    ControllerActions.Joystick.SetYaw(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.Roll)
                {
                    ControllerActions.Joystick.SetRoll(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.JoystickTrigger)
                {
                    ControllerActions.Joystick.Trigger(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.SwitchWeapon)
                {
                    ControllerActions.Joystick.SwitchWeapon(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.JoystickThumbStick)
                {
                    ControllerActions.Joystick.Thumbstick(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.ThrottleThumbStick)
                {
                    ControllerActions.Throttle.Thumbstick(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.Countermeasures)
                {
                    ControllerActions.Throttle.Countermeasures(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.Brakes)
                {
                    ControllerActions.Throttle.TriggerBrakes(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.FlapsIncrease)
                {
                    ControllerActions.Flaps.IncreaseFlaps(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.FlapsDecrease)
                {
                    ControllerActions.Flaps.DecreaseFlaps(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.FlapsCycle)
                {
                    ControllerActions.Flaps.CycleFlaps(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.PTT)
                {
                    ControllerActions.Radio.PTT(this, joystickValue);
                }
                else if (OutputAction == ControllerAction.VRInteract)
                {
                    VRInteractable interactable = GameObject.FindObjectsOfType<VRInteractable>(false)
                        .Where(i => i.interactableName.ToLower() == VRInteractName.ToLower())
                        .FirstOrDefault();
                    if (interactable != null)
                    {
                        if (GetButtonPressed(joystickValue))
                            Interactions.Interact(interactable);
                        else
                            Interactions.AntiInteract(interactable);
                    }
                }
            }
        }

        public float GetAxisAsFloat(int value)
        {
            if (IsOffsetButton)
            {
                if (Invert)
                    return 1f - ((float)value / ButtonMax);
                return (float)value / ButtonMax;
            }
            if (IsOffsetPOV)
            {
                return (float)value / POVMax;
            }
            if (AxisCentering == AxisCentering.TwoAxis)
            {
                if (value > AxisMiddle && SelectedTwoAxis == TwoAxis.Positive)
                {
                    float val = 1f - Math.Abs((float)((float)(value - AxisMiddle) / AxisMiddle));
                    return Invert ? val : 1f - val;
                }
                else if (value < AxisMiddle && SelectedTwoAxis == TwoAxis.Negative)
                {
                    float val = Math.Abs((float)((float)value / AxisMiddle));
                    return Invert ? val : 1f - val;
                }
                else
                    return 0;

            }
            if (Invert)
                return 1f - ((float)value / AxisMax);
            return (float)value / AxisMax;
        }

        public bool GetButtonPressed(int value)
        {
            if (IsOffsetAxis)
            {
                if (AxisCentering == AxisCentering.Middle)
                    return value < AxisMiddle - Deadzone || value > AxisMiddle + Deadzone;
                else if (AxisCentering == AxisCentering.TwoAxis)
                {
                    return GetAxisAsFloat(value) >= 0.5f;
                }
                else // Normal Min-Max
                {
                    if (Invert) // Max-Min
                        return value < AxisMax - Deadzone;
                    else // Min-Max
                        return value > AxisMin + Deadzone;
                }
            }
            if (IsOffsetButton)
            {
                if (Invert)
                    return value <= ButtonMax;
                else
                    return value >= ButtonMax;
            }
            if (IsOffsetPOV)
            {
                if (POVDirection == POVFacing.Up)
                    return value == (int)POVFacing.Up || value == (int)POVFacing.UpRight || value == (int)POVFacing.UpLeft;
                else if (POVDirection == POVFacing.Right)
                    return value == (int)POVFacing.Right || value == (int)POVFacing.DownRight || value == (int)POVFacing.UpRight;
                else if (POVDirection == POVFacing.Down)
                    return value == (int)POVFacing.Down || value == (int)POVFacing.DownLeft || value == (int)POVFacing.DownRight;
                else if (POVDirection == POVFacing.Left)
                    return value == (int)POVFacing.Left || value == (int)POVFacing.UpLeft || value == (int)POVFacing.DownLeft;
                else
                    return false;
            }
            return false;
        }
    }

    public enum AxisCentering
    {
        Normal, // Minimum
        Middle,
        TwoAxis
    }

    public enum POVFacing : int
    {
        None = -1,
        Up = 0,
        UpRight = 4500,
        Right = 9000,
        DownRight = 13500,
        Down = 18000,
        DownLeft = 22500,
        Left = 27000,
        UpLeft = 31500,
    }

    public enum ThumbstickDirection
    {
        None,
        Up,
        Down,
        Left,
        Right,
        Press
    }

    public enum TwoAxis
    {
        Positive,
        Negative
    }
}