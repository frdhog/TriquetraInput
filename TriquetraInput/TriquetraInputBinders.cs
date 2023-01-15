using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.DirectInput;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

// TODO: Jettison button requires push in (menu button)
// TODO?: duplicate binding
// TODO?: XInput
// TODO?: Lock hand position to joystick

namespace Triquetra.Input
{
    public class TriquetraInputBinders : MonoBehaviour
    {
        public List<POVFacing> POVDirections = new List<POVFacing>() { POVFacing.Up, POVFacing.Right, POVFacing.Down, POVFacing.Left}; // TODO fix/unify

        public bool showWindow = true;
        private Rect windowRect = new Rect(Screen.width - 525, 25, 500, Screen.height - 75);
        private Vector2 scrollPosition;
        public bool Enabled = true;
        private string textFilter = "";
        private Dictionary<Binding, bool> collapsedBindings = new Dictionary<Binding, bool>();

        public void OnGUI()
        {
            if (showWindow)
                windowRect = GUI.Window(500, windowRect, DoWindow, "Binders Display");
        }

        void DoWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            GUILayout.Label("Press F10 to toggle this screen");

            Enabled = GUILayout.Toggle(Enabled, "Enabled");

            GUI.enabled = Enabled;

            GUILayout.BeginHorizontal();
            {
                try
                {
                    if (GUILayout.Button("Load"))
                    {
                        TriquetraInput.LoadBindings();
                    }
                    if (GUILayout.Button("Save"))
                    {
                        TriquetraInput.SaveBindings();
                    }
                }
                catch (Exception e)
                {
                    TriquetraInput.Instance.Log(e.Message);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Collapse All"))
                {
                    foreach(Binding binding in Binding.Bindings)
                    {
                        collapsedBindings[binding] = true;
                    }
                }
                if (GUILayout.Button("Expand All"))
                {
                    foreach (Binding binding in Binding.Bindings)
                    {
                        collapsedBindings[binding] = false;
                    }
                }
            }
            GUILayout.EndHorizontal();

            // GUILayout.Label(ControllerActions.Joystick.joystick == null ? "No joystick found" : "Joystick found");
            // GUILayout.Label(ControllerActions.Throttle.throttle == null ? "No throttle found" : "Throttle found");

            /*if (GUILayout.Button("Print all joysticks to console"))
            {
                IEnumerable<DeviceInstance> devices = Binding.directInput.GetDevices();
                foreach (var device in devices)
                {
                    Logger.WriteLine($"{device.ProductName} ({device.InstanceGuid}) - {device.Type}");
                }
            }*/

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Name Filter: ");
                textFilter = GUILayout.TextField(textFilter);
            }
            GUILayout.EndHorizontal();

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            {
                foreach (Binding binding in Binding.Bindings)
                {
                    if (binding == null) continue;

                    if (!binding.Name.ToLower().Contains(textFilter?.ToLower() ?? ""))
                        continue;

                    // New Binding [x]
                    GUILayout.BeginHorizontal();
                    {
                        binding.Name = GUILayout.TextField(binding.Name);

                        if (GUILayout.Button("x", GUILayout.Width(24)))
                        {
                            Binding.Bindings.Remove(binding);
                        }
                    }
                    GUILayout.EndHorizontal();

                    bool isCollapsed = false;
                    if (!collapsedBindings.TryGetValue(binding, out isCollapsed))
                    {
                        collapsedBindings[binding] = false;
                    }

                    if (isCollapsed)
                    {
                        if (GUILayout.Button("----- Expand -----"))
                        {
                            collapsedBindings[binding] = false;
                        }
                        continue;
                    }
                    else
                    {
                        if (GUILayout.Button("----- Collapse -----"))
                        {
                            collapsedBindings[binding] = true;
                        }
                    }


                    // [<] Joystick Name [>]
                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("<"))
                        {
                            binding.PrevJoystick();
                        }

                        if (binding.Controller != null)
                            GUILayout.Label($"{binding.Controller.Properties.ProductName} ({binding.JoystickDevice.InstanceGuid}");
                        else
                            GUILayout.Label("No Controller");

                        if (GUILayout.Button(">"))
                        {
                            binding.NextJoystick();
                        }
                    }
                    GUILayout.EndHorizontal();

                    // Offset [Select] / Offset [Cancel]
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Offset: " + binding.Offset.ToString());
                        if (GUILayout.Button(binding.DetectingOffset ? "Cancel" : "Detect"))
                        {
                            if (!binding.DetectingOffset)
                            {
                                binding.DetectingOffset = true;
                                binding.bindingDelegate = CreateControllerUpdatedDelegate(binding);
                                binding.Controller.Updated += binding.bindingDelegate;
                            }
                            else
                            {
                                try
                                {
                                    binding.Controller.Updated -= binding.bindingDelegate;
                                }
                                catch { }
                                binding.DetectingOffset = false;
                            }
                        }
                        if (GUILayout.Button(binding.OffsetSelectOpen ? "Cancel" : "Select"))
                        {
                            binding.OffsetSelectOpen = !binding.OffsetSelectOpen;
                        }
                    }
                    GUILayout.EndHorizontal();

                    // [X]
                    // [Y]
                    // [Z]
                    // ...
                    if (binding.OffsetSelectOpen)
                    {
                        foreach (JoystickOffset offset in Enum.GetValues(typeof(JoystickOffset)))
                        {
                            if (GUILayout.Button(offset.ToString()))
                            {
                                binding.Offset = offset;
                                binding.OffsetSelectOpen = false;
                            }
                        }
                    }

                    if (!binding.IsOffsetPOV) // If not a POV hat
                    {
                        // [Invert] / [Uninvert]
                        if (GUILayout.Button(binding.Invert ? "Uninvert" : "Invert"))
                        {
                            binding.Invert = !binding.Invert;
                        }
                    }
                    else
                    {
                        // POV Direction
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("└ POV Direction: " + binding.POVDirection.ToString());
                            if (GUILayout.Button(binding.POVDirectionSelectOpen ? "Cancel" : "Select"))
                            {
                                binding.POVDirectionSelectOpen = !binding.POVDirectionSelectOpen;
                            }
                        }
                        GUILayout.EndHorizontal();

                        if (binding.POVDirectionSelectOpen)
                        {
                            foreach (POVFacing pov in POVDirections)
                            {
                                if (GUILayout.Button(pov.ToString()))
                                {
                                    binding.POVDirection = pov;
                                    binding.POVDirectionSelectOpen = false;
                                }
                            }
                        }
                    }

                    // Axis Centering: [Normal] / Axis Centering: [Middle] / Axis Centering: [TwoAxis]
                    if (binding.IsOffsetAxis)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label($"Axis Centering: {binding.AxisCentering}");
                            if (GUILayout.Button(binding.AxisCenteringSelectOpen ? "Cancel" : "Select"))
                            {
                                binding.AxisCenteringSelectOpen = !binding.AxisCenteringSelectOpen;
                            }
                        }
                        GUILayout.EndHorizontal();

                        if (binding.AxisCenteringSelectOpen)
                        {
                            foreach (AxisCentering axis in Enum.GetValues(typeof(AxisCentering)))
                            {
                                if (GUILayout.Button(axis.ToString()))
                                {
                                    binding.AxisCentering = axis;
                                    binding.AxisCenteringSelectOpen = false;
                                }
                            }
                        }

                        if (binding.AxisCentering == AxisCentering.TwoAxis)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("└ Selected Axis: ");
                                if (GUILayout.Button(binding.SelectedTwoAxis.ToString()))
                                {
                                    if (binding.SelectedTwoAxis == TwoAxis.Positive)
                                        binding.SelectedTwoAxis = TwoAxis.Negative;
                                    else
                                        binding.SelectedTwoAxis = TwoAxis.Positive;
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                    }

                    // Action [Select] / Action [Cancel]
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Action: " + binding.OutputAction.ToString());
                        if (GUILayout.Button(binding.OutputActionSelectOpen ? "Cancel" : "Select"))
                        {
                            binding.OutputActionSelectOpen = !binding.OutputActionSelectOpen;
                        }
                    }
                    GUILayout.EndHorizontal();

                    if (binding.OutputActionSelectOpen)
                    {
                        foreach (ControllerAction action in Enum.GetValues(typeof(ControllerAction)))
                        {
                            if (GUILayout.Button(action.ToString()))
                            {
                                binding.OutputAction = action;
                                binding.OutputActionSelectOpen = false;
                            }
                        }
                    }

                    // Thumbstick Direction
                    if (binding.OutputAction == ControllerAction.ThrottleThumbStick || binding.OutputAction == ControllerAction.JoystickThumbStick)
                    {
                        // Thumbstick Direction [Select] / Thumbstick Direction [Cancel]
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("└ Thumbstick Direction: " + binding.ThumbstickDirection.ToString());
                            if (GUILayout.Button(binding.ThumbstickDirectionSelectOpen ? "Cancel" : "Select"))
                            {
                                binding.ThumbstickDirectionSelectOpen = !binding.ThumbstickDirectionSelectOpen;
                            }
                        }
                        GUILayout.EndHorizontal();

                        if (binding.ThumbstickDirectionSelectOpen)
                        {
                            foreach (ThumbstickDirection direction in Enum.GetValues(typeof(ThumbstickDirection)))
                            {
                                if (GUILayout.Button(direction.ToString()))
                                {
                                    binding.ThumbstickDirection = direction;
                                    binding.ThumbstickDirectionSelectOpen = false;
                                }
                            }
                        }
                    }

                    if (binding.OutputAction == ControllerAction.VRInteract)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("VRInteractable Name: ");
                            binding.VRInteractName = GUILayout.TextField(binding.VRInteractName);
                        }
                        GUILayout.EndHorizontal();
                    }

                    if (binding.Controller != null)
                    {
                        JoystickUpdate value = binding.Controller.RawState[binding.RawOffset];

                        GUI.enabled = false;
                        // Current Axis Value Slider
                        if (binding.IsOffsetButton)
                        {
                            GUILayout.HorizontalSlider(value.Value, Binding.ButtonMin, Binding.ButtonMax);
                        }
                        else if (binding.IsOffsetPOV)
                        {
                            GUILayout.HorizontalSlider(value.Value, Binding.POVMin, Binding.POVMax);
                        }
                        else if (binding.IsOffsetAxis)
                        {
                            GUILayout.HorizontalSlider(value.Value, Binding.AxisMin, Binding.AxisMax);
                        }
                        GUI.enabled = Enabled;

                        GUILayout.Label(binding.GetButtonPressed(value.Value) ? "=== Pressed! ===" : "--- Unpressed ---");
                    }

                    GUILayout.Space(30);
                }
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Create Binding"))
            {
                Binding.Bindings.Add(new Binding());
            }
            GUI.enabled = true;
        }

        private TriquetraJoystick.JoystickUpdated CreateControllerUpdatedDelegate(Binding binding)
        {
            return (TriquetraJoystick joystick, JoystickUpdate update) =>
            {
                if (joystick.Information.InstanceGuid == binding.Controller?.Information.InstanceGuid)
                {
                    // Logger.WriteLine($"Detecting update from {update.Offset} - {update.Value}");

                    if (Binding.IsButton(update.RawOffset) && update.Value >= 128)
                    {
                        binding.Offset = update.Offset;
                        binding.Controller.Updated -= binding.bindingDelegate;
                        binding.DetectingOffset = false;
                        return;
                    }
                    else if (Binding.IsPOV(update.RawOffset) && update.Value > -1)
                    {
                        binding.Offset = update.Offset;
                        binding.Controller.Updated -= binding.bindingDelegate;
                        binding.DetectingOffset = false;
                        return;
                    }
                    else if (Binding.IsAxis(update.RawOffset))
                    {
                        binding.Offset = update.Offset;
                        binding.Controller.Updated -= binding.bindingDelegate;
                        binding.DetectingOffset = false;
                        return;
                    }
                }
            };
        }

        public void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F10))
            {
                showWindow = !showWindow;
            }
        }

        int frameCount = 0;
        // Main update loop for all logic
        public void FixedUpdate()
        {
            frameCount++;
            if (!Enabled)
                return;

            if (frameCount >= 5 * 60) // every 5 seconds
            {
                frameCount = 0;
                TriquetraInputJoysticks.PopulateActiveJoysticks();
                ControllerActions.TryGetSticks();
            }
            TriquetraInputJoysticks.PollActiveJoysticks();

            if (TriquetraInput.IsFlyingScene())
            {
                ControllerActions.Joystick.UpdateStick();
                ControllerActions.Joystick.UpdateThumbstick();
                ControllerActions.Throttle.UpdateThumbstick();
            }
        }
    }
}