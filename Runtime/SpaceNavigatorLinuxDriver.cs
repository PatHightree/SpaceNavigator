#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using UnityEditor.Callbacks;

namespace SpaceNavigatorDriver
{
    [Serializable]
    [InitializeOnLoad]
    public class SpaceNavigatorLinuxDriver
    {
        [DllImport("libspnav.so.0")]
        private static extern int spnav_open();

        [DllImport("libspnav.so.0")]
        private static extern SpnavEventType spnav_wait_event(ref SpnavEvent ev);


        [DllImport("libspnav.so.0")]
        private static extern SpnavEventType spnav_poll_event(ref SpnavEvent ev);

        [DllImport("libspnav.so.0")]
        private static extern void spnav_close();

        /* Control device LED
         * SPNAV_CFG_LED_OFF(0) | SPNAV_CFG_LED_ON(1) | SPNAV_CFG_LED_AUTO(2)
         * cfgfile option: led
         */
        [DllImport("libspnav.so.0")]
        private static extern Int32 spnav_cfg_set_led(Int32 state);

        [DllImport("libspnav.so.0")]
        private static extern Int32 spnav_cfg_get_led();	/* returns led setting, -1 on error */

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct SpnavEventMotion
        {
            // public Int32 type; /* SPNAV_EVENT_MOTION */
            public Int32 x, y, z;
            public Int32 rx, ry, rz;
            public UInt32 period;
            public UInt32 data;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct SpnavEventButton
        {
            public Int32 type; /* SPNAV_EVENT_BUTTON or SPNAV_EVENT_RAWBUTTON */
            public Int32 press;
            public Int32 bnum;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct SpnavEventDev
        {
            public Int32 type; /* SPNAV_EVENT_DEV */
            public Int32 op; /* SPNAV_DEV_ADD / SPNAV_DEV_RM */
            public Int32 id;
            public Int32 devtype; /* see spnav_dev_type() */

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Int32[] usbid; /* USB id if it's a USB device, 0:0 if it's a serial device */
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SpnavEventCfg
        {
            public Int32 type; /* SPNAV_EVENT_CFG */
            public Int32 cfg; /* same as protocol REQ_GCFG* enum */

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public Int32[] data; /* same as protocol response data 0-5 */
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SpnavEventAxis
        {
            public Int32 type; /* SPNAV_EVENT_RAWAXIS */
            public Int32 idx; /* axis number */
            public Int32 value; /* value */
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct SpnavEvent
        {
            public Int32 type;
            public SpnavEventMotion motion;
            public SpnavEventButton button;
            public SpnavEventDev dev;
            public SpnavEventCfg cfg;
            public SpnavEventAxis axis;
        }

        private enum SpnavEventType
        {
            SPNAV_EVENT_ANY = 0, /* used by spnav_remove_events() */
            SPNAV_EVENT_MOTION,
            SPNAV_EVENT_BUTTON, /* includes both press and release */
            SPNAV_EVENT_DEV, /* add/remove device event */
            SPNAV_EVENT_CFG, /* configuration change event */
            SPNAV_EVENT_RAWAXIS,
            SPNAV_EVENT_RAWBUTTON
        }

        private static bool isConnected = false;
        private static float sliderX = 0f;
        private static float sliderY = 0f;
        private static float sliderZ = 0f;
        private static float sliderRX = 0f;
        private static float sliderRY = 0f;
        private static float sliderRZ = 0f;
        private static SpnavEvent ev = new SpnavEvent();

        public static readonly SpaceNavigatorLinuxDriver Instance = new SpaceNavigatorLinuxDriver();

        public static bool IsConnected => isConnected;
        public static Vector3 Translation => new Vector3(sliderX, sliderY, sliderZ) / 350f;
        public static Vector3 Rotation => new Vector3(sliderRX, sliderRY, sliderRZ) / 350f;

        static SpaceNavigatorLinuxDriver()
        {
            Init();
        }

        static void Init()
        {
            Connect();

            if (isConnected)
            {
                EditorApplication.update += Poll;
            }
        }

        [DidReloadScripts]
        static void OnReload()
        {
            Disconnect();
            Init();
        }


        public static void SetLEDStatus(SpaceNavigatorHID.LedStatus status)
        {
            Int32 value = status == SpaceNavigatorHID.LedStatus.On ? 1 : 0;
            spnav_cfg_set_led(value);
        }

        private static void Poll()
        {
            // Poll for events
            SpnavEventType result = spnav_poll_event(ref ev);

            if (result == SpnavEventType.SPNAV_EVENT_MOTION) // SPNAV_EVENT_MOTION
            {
                sliderX = ev.motion.x;
                sliderY = ev.motion.y;
                sliderZ = ev.motion.z;
                sliderRX = -ev.motion.rx;
                sliderRY = -ev.motion.ry;
                sliderRZ = -ev.motion.rz;
            }
            else
            {
                sliderX = 0;
                sliderY = 0;
                sliderZ = 0;
                sliderRX = 0;
                sliderRY = 0;
                sliderRZ = 0;
            }
        }

        private static void Connect()
        {
            int result = spnav_open();
            if (result == -1)
            {
                Debug.LogError($"Failed to connect to spacenavd");
                return;
            }

            isConnected = true;
            DebugLog("Connected to spacenavd");
        }

        private static void Disconnect()
        {
            if (isConnected)
            {
                spnav_close();
                isConnected = false;
                DebugLog("Disconnected from spacenavd");
            }
        }

        private static void DebugLog(string _message)
        {
#if SPACENAVIGATOR_DEBUG
            Debug.Log(_message);
#endif
        }

    }
}
