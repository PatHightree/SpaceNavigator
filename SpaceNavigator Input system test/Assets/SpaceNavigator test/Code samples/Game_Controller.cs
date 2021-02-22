// This file contains the second code sample from the thread on receiving HID data with multiple reports (reportId_workaround_as_integer).
// https://forum.unity.com/threads/new-input-system-problem-rx2sim-game-controller-every-second-frame-is-zero.871330/#post-5753059
// This code does not require the project to allow unsafe code. 


using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

public partial class Game_Main : MonoBehaviour
{
    // used axis channels in game
    float[] input_channel_from_event_proccessing = new float[8];
 
    // for Unity workaround with this device
    public RX2SIM_Game_Controller RX2SIM_Game_Controller_;
 
 
 
    // ############################################################################
    // on event
    // ############################################################################
    private void OnEnable()
    {
        InputSystem.onEvent += (eventPtr, device) => IO_Proccess_Input(eventPtr, device);
    }
    private void OnDisable()
    {
        InputSystem.onEvent -= (eventPtr, device) => IO_Proccess_Input(eventPtr, device);
    }
    // ############################################################################

    private List<string> connected_input_devices_names;
    private List<string> connected_input_devices_type;
    private int selected_input_device_id;
 
 
    // ############################################################################
    // Input System Event
    // ############################################################################
    void IO_Proccess_Input(InputEventPtr eventPtr, InputDevice device)
    {
        // Ignore anything that isn't a state event.
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
            return;
 
        // proccess selected device
        if (device.name.Contains(connected_input_devices_names[selected_input_device_id]))
        {
            // if device is a gamepad
            if (connected_input_devices_type[selected_input_device_id].Contains("Gamepad"))
            {
                for (int i = 0; i < 8; i++) input_channel_from_event_proccessing[i] = 0;
 
                foreach (var each_control in ((Gamepad)device).allControls)
                {
                    if (each_control.displayName.Contains("Left Stick X"))
                        ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[0]);
                    if (each_control.displayName.Contains("Left Stick Y"))
                        ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[1]);
                    if (each_control.displayName.Contains("Right Stick X"))
                        ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[2]);
                    if (each_control.displayName.Contains("Right Stick Y"))
                        ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[3]);
                    if (each_control.displayName.Contains("Left Trigger"))
                        ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[4]);
                    if (each_control.displayName.Contains("Right Trigger"))
                        ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[5]);
                }
                input_channel_from_event_proccessing[6] = 0;
                input_channel_from_event_proccessing[7] = 0;
 
                // Can handle events yourself, for example, and then stop them from further processing by marking them as handled.
                eventPtr.handled = true;
            }
 
            // if device is a joystick
            if (connected_input_devices_type[selected_input_device_id].Contains("Joystick"))
            {
                // process standard Joysticks
                if (!device.description.product.Contains("RX2SIM Game Controller"))
                {
                    for (int i = 0; i < 8; i++) input_channel_from_event_proccessing[i] = 0;
 
                    foreach (var each_control in ((Joystick)device).allControls)
                    {
                        if (each_control.displayName.Contains("Stick X"))
                            ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[0]);
                        if (each_control.displayName.Contains("Stick Y"))
                            ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[1]);
                        if (each_control.displayName.Contains("Rz"))
                            ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[2]);
                        if (each_control.displayName.Contains("Z"))
                            ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[3]);
                        if (each_control.displayName.Contains("Rx"))
                            ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[4]);
                        if (each_control.displayName.Contains("Ry"))
                            ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[5]);
                    }
                    input_channel_from_event_proccessing[6] = 0;
                    input_channel_from_event_proccessing[7] = 0;
                }
                else // workaround for not functioning "RX2SIM Game Controller" device
                {
                    int reportId_workaround_as_integer = 0;
 
                    foreach (var each_control in ((RX2SIM_Game_Controller)device).allControls)
                    {
                        // get reportId
                        if (each_control.name.Contains("reportId_workaround_as_integer"))
                            ((IntegerControl)each_control).ReadValueFromEvent(eventPtr, out reportId_workaround_as_integer);
 
                        // process first data frame wih axes 0..3
                        if (reportId_workaround_as_integer == 1)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                if (each_control.name.Contains("axis" + i.ToString()))
                                {
                                    ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[i]);
                                    input_channel_from_event_proccessing[i] = (input_channel_from_event_proccessing[i] - 0.5f) * 2f;
                                }
                            }
                        }
                        // process second data frame wih axes 4..7 (following 8 buttons are not considered yet)
                        if (reportId_workaround_as_integer == 2)
                        {
                            for (int i = 4; i < 8; i++)
                            {
                                if (each_control.name.Contains("axis" + (i - 4).ToString()))
                                {
                                    ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[i]);
                                    input_channel_from_event_proccessing[i] = (input_channel_from_event_proccessing[i] - 0.5f) * 2f;
                                }
                            }
                        }
                    }
 
                }
 
                // Can handle events yourself, for example, and then stop them from further processing by marking them as handled.
                eventPtr.handled = true;
            }
        }
    }
    // ############################################################################
 
 
 
    // ############################################################################
    // Awake
    // ############################################################################
    void Awake()
    {
        RX2SIM_Game_Controller_ = new RX2SIM_Game_Controller();
    }
    // ############################################################################
 
 
 
}
 
 
 
 
 
 
 
 
 
 
 
 
// ############################################################################
// RX2SIM Game Controller - Workaround
// ############################################################################
// We receive data as raw HID input reports. This struct
// describes the raw binary format of such a report.
public struct RX2SIM_Game_Controller_Device_State : IInputStateTypeInfo
{
    // Because all HID input reports are tagged with the 'HID ' FourCC,
    // this is the format we need to use for this state struct.
    public FourCC format => new FourCC('H', 'I', 'D');
 
    // HID input reports can start with an 8-bit report ID. It depends on the device
    // whether this is present or not. On the RX2SIM Game Controller, it is
    // present and in the two frames it has the value 1 or 2.
    [InputControl(name = "reportId_workaround_as_integer", format = "BIT", layout = "Integer", displayName = "Button reportId1 workaround", bit = 0, offset = 0, sizeInBits = 2)]
    public byte reportId_workaround_as_integer;
 
    // https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html
    [InputControl(name = "axis0", format = "BIT", layout = "Axis", displayName = "Axis 0", bit = 0, offset = 1, sizeInBits = 12)] // axis-X
    public float axis0;
 
    [InputControl(name = "axis1", format = "BIT", layout = "Axis", displayName = "Axis 1", bit = 4, offset = 2, sizeInBits = 12)] // axis-Y
    public float axis1;
 
    [InputControl(name = "axis2", format = "BIT", layout = "Axis", displayName = "Axis 2", bit = 0, offset = 4, sizeInBits = 12)] // axis-Z
    public float axis2;
 
    [InputControl(name = "axis3", format = "BIT", layout = "Axis", displayName = "Axis 3", bit = 4, offset = 5, sizeInBits = 12)] // axis-Rx
    public float axis3;
 
    //[InputControl(name = "axis4", format = "BIT", layout = "Axis", displayName = "Axis 4", bit = 0, offset = 8, sizeInBits = 12)] // axis-Ry
    //public float axis4;
 
    //[InputControl(name = "axis5", format = "BIT", layout = "Axis", displayName = "Axis 5", bit = 4, offset = 9, sizeInBits = 12)] // axis-Rz
    //public float axis5;
 
    //[InputControl(name = "axis6", format = "BIT", layout = "Axis", displayName = "Axis 6", bit = 0, offset = 11, sizeInBits = 12)] // Slider
    //public float axis6;
 
    //[InputControl(name = "axis7", format = "BIT", layout = "Axis", displayName = "Axis 7", bit = 4, offset = 12, sizeInBits = 12)] // Dial
    //public float axis7;
 
    //[InputControl(name = "button1", format = "BIT", layout = "Button", displayName = "Button 1", bit = 0, offset = 14, sizeInBits = 1)]
    //[InputControl(name = "button2", format = "BIT", layout = "Button", displayName = "Button 2", bit = 1, offset = 14, sizeInBits = 1)]
    //[InputControl(name = "button3", format = "BIT", layout = "Button", displayName = "Button 3", bit = 2, offset = 14, sizeInBits = 1)]
    //[InputControl(name = "button4", format = "BIT", layout = "Button", displayName = "Button 4", bit = 3, offset = 14, sizeInBits = 1)]
    //[InputControl(name = "button5", format = "BIT", layout = "Button", displayName = "Button 5", bit = 4, offset = 14, sizeInBits = 1)]
    //[InputControl(name = "button6", format = "BIT", layout = "Button", displayName = "Button 6", bit = 5, offset = 14, sizeInBits = 1)]
    //[InputControl(name = "button7", format = "BIT", layout = "Button", displayName = "Button 7", bit = 6, offset = 14, sizeInBits = 1)]
    //[InputControl(name = "button8", format = "BIT", layout = "Button", displayName = "Button 8", bit = 7, offset = 14, sizeInBits = 1)]
    //[FieldOffset(10)]
    //public byte buttons1;
}
 
 
// ############################################################################
// Using InputControlLayoutAttribute, we tell the system about the state
// struct we created, which includes where to find all the InputControl
// attributes that we placed on there.This is how the Input System knows
// what controls to create and how to configure them.
// ############################################################################
[InputControlLayout(stateType = typeof(RX2SIM_Game_Controller_Device_State))]
#if UNITY_EDITOR
[InitializeOnLoad] // Make sure static constructor is called during startup.
#endif
public class RX2SIM_Game_Controller : Joystick // Gamepad // Joystick // InputDevice
{
    static RX2SIM_Game_Controller()
    {
        //// This is one way to match the Device.
        InputSystem.RegisterLayout<RX2SIM_Game_Controller>(
            matches: new InputDeviceMatcher()
                .WithInterface("HID")
                .WithManufacturer("RCWARE")
                .WithProduct("RX2SIM Game Controller"));
 
        //// Alternatively, you can also match by PID and VID, which is generally
        //// more reliable for HIDs.
        //InputSystem.RegisterLayout<RX2SIM_Game_Controller>(
        //    matches: new InputDeviceMatcher()
        //        .WithInterface("HID")
        //        .WithCapability("vendorId", 1155) // RCWARE
        //        .WithCapability("productId", 41195)); // RX2SIM Game Controller
 
    }
 
    //// In the Player, to trigger the calling of the static constructor,
    //// create an empty method annotated with RuntimeInitializeOnLoadMethod.
    [RuntimeInitializeOnLoadMethod]
    static void Init() { }
}
// ############################################################################
