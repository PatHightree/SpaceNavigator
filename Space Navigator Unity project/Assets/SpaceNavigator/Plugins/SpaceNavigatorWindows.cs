using UnityEngine;
using System;
using System.Runtime.InteropServices;
using _3Dconnexion;

namespace SpaceNavigatorDriver
{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

    class SpaceNavigatorWindows : SpaceNavigator
    {
        private const bool DebugLog = true;
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private const string templateTR = "TX: {1,-7}{0}TY: {2,-7}{0}TZ: {3,-7}{0}RX: {4,-7}{0}RY: {5,-7}{0}RZ: {6,-7}{0}P: {7}";
        private const string appName = "SpaceNavigator Unity Plugin";
        private IntPtr hMainWindow;
        private IntPtr oldWndProcPtr;
        private IntPtr newWndProcPtr;
        private WndProcDelegate newWndProc;
        private IntPtr devHdl = IntPtr.Zero;
        private SiApp.SpwRetVal res;
        private bool isrunning;

        private const float TransSensScale = 0.0001f, RotSensScale = 0.0008f;
        private int TX, TY, TZ, RX, RY, RZ;

        #region Setup

        /// <summary>
        /// Private constructor, prevents a default instance of the <see cref="SpaceNavigatorWindows" /> class from being created.
        /// </summary>
        private SpaceNavigatorWindows()
        {
            if (isrunning) return;

            hMainWindow = GetForegroundWindow();
            newWndProc = wndProc;
            newWndProcPtr = Marshal.GetFunctionPointerForDelegate(newWndProc);
            oldWndProcPtr = SetWindowLongPtr(hMainWindow, -4, newWndProcPtr);
            isrunning = true;

            InitializeSiApp();
        }

        private IntPtr wndProc(IntPtr _hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            TrackMouseEvents(msg, wParam, lParam);

            return CallWindowProc(oldWndProcPtr, _hWnd, msg, wParam, lParam);
        }

        private void InitializeSiApp()
        {
            res = SiApp.SiInitialize();
            if (res != SiApp.SpwRetVal.SPW_NO_ERROR)
                Debug.LogError("Initialize function failed");
            Debug.Log("SiInitialize" + res);

            SiApp.SiOpenData openData = new SiApp.SiOpenData();
            SiApp.SiOpenWinInit(ref openData, hMainWindow);
            if (openData.hWnd == IntPtr.Zero)
                Debug.LogError("Handle is empty");
            Debug.Log("SiOpenWinInit" + openData.hWnd + "(window handle)");

            devHdl = SiApp.SiOpen(appName, SiApp.SI_ANY_DEVICE, IntPtr.Zero, SiApp.SI_EVENT, ref openData);
            if (devHdl == IntPtr.Zero)
                Debug.LogError("Open returns empty device handle");
            Debug.Log("SiOpen" + devHdl + "(device handle)");
        }

        #endregion

        #region Update

        private void TrackMouseEvents(uint msg, IntPtr wParam, IntPtr lParam)
        {
            SiApp.SiGetEventData eventData = new SiApp.SiGetEventData();
            IntPtr eventPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SiApp.SiSpwEvent_JustType)));

            SiApp.SiGetEventWinInit(ref eventData, (int) msg, wParam, lParam);
            SiApp.SpwRetVal val = SiApp.SiGetEvent(devHdl, SiApp.SI_AVERAGE_EVENTS, ref eventData, eventPtr);

            if (val == SiApp.SpwRetVal.SI_IS_EVENT)
            {
                Debug.Log("SiGetEventWinInit: " + eventData.msg);

                SiApp.SiSpwEvent_JustType sEventType = PtrToStructure<SiApp.SiSpwEvent_JustType>(eventPtr);
                switch (sEventType.type)
                {
                    case SiApp.SiEventType.SI_MOTION_EVENT:
                        SiApp.SiSpwEvent_SpwData motionEvent = PtrToStructure<SiApp.SiSpwEvent_SpwData>(eventPtr);
                        TX = motionEvent.spwData.mData[0];
                        TY = motionEvent.spwData.mData[1];
                        TZ = motionEvent.spwData.mData[2];
                        RX = motionEvent.spwData.mData[3];
                        RY = motionEvent.spwData.mData[4];
                        RZ = motionEvent.spwData.mData[5];

                        if (DebugLog)
                        {
                            string motionData = string.Format(templateTR, "",
                                motionEvent.spwData.mData[0], motionEvent.spwData.mData[1], motionEvent.spwData.mData[2], // TX, TY, TZ
                                motionEvent.spwData.mData[3], motionEvent.spwData.mData[4], motionEvent.spwData.mData[5], // RX, RY, RZ
                                motionEvent.spwData.period); // Period (normally 16 ms)
                            Debug.Log("Motion event " + motionData);
                        }

                        break;

                    case SiApp.SiEventType.SI_ZERO_EVENT:
                        if (DebugLog)
                        {
                            Debug.Log("Zero event");
                        }
                        break;

                    case SiApp.SiEventType.SI_CMD_EVENT:
                        SiApp.SiSpwEvent_CmdEvent cmdEvent = PtrToStructure<SiApp.SiSpwEvent_CmdEvent>(eventPtr);
                        SiApp.SiCmdEventData cmd = cmdEvent.cmdEventData;
                        if (DebugLog)
                        {
                            Debug.LogFormat("SI_APP_EVENT: V3DCMD = {0}, pressed = {1}", cmd.functionNumber, cmd.pressed > 0);
                            Debug.LogFormat("V3DCMD event: V3DCMD = {0}, pressed = {1}", cmd.functionNumber, cmd.pressed > 0);
                        }
                        break;

                    case SiApp.SiEventType.SI_APP_EVENT:
                        SiApp.SiSpwEvent_AppCommand appEvent = PtrToStructure<SiApp.SiSpwEvent_AppCommand>(eventPtr);
                        SiApp.SiAppCommandData data = appEvent.appCommandData;
                        if (DebugLog)
                        {
                            Debug.LogFormat("SI_APP_EVENT: appCmdID = \"{0}\", pressed = {1}", data.id.appCmdID, data.pressed > 0);
                            Debug.LogFormat("App event: appCmdID = \"{0}\", pressed = {1}", data.id.appCmdID, data.pressed > 0);
                        }
                        break;
                }

                // if (DebugLog)
                // {
                //     Debug.LogFormat("SiGetEvent {0}({1})", sEventType.type, val);
                // }
            }

            Marshal.FreeHGlobal(eventPtr);
        }

        private T PtrToStructure<T>(IntPtr ptr) where T : struct
        {
            return (T) Marshal.PtrToStructure(ptr, typeof(T));
        }

        #endregion

        #region Public API

        public override Vector3 GetTranslation()
        {
            float sensitivity = Application.isPlaying ? Settings.PlayTransSens : Settings.TransSens[Settings.CurrentGear];
            return (new Vector3(
                      Settings.GetLock(DoF.Translation, Axis.X) ? 0 : (float) TX / 2000f,
                      Settings.GetLock(DoF.Translation, Axis.Y) ? 0 : (float) TY / 2000f,
                      Settings.GetLock(DoF.Translation, Axis.Z) ? 0 : -(float) TZ / 2000f) * (sensitivity * TransSensScale));
        }

        public override Quaternion GetRotation()
        {
            float sensitivity = Application.isPlaying ? Settings.PlayRotSens : Settings.RotSens;
            return (Quaternion.Euler(
                sensitivity * RotSensScale * (Settings.GetLock(DoF.Rotation, Axis.X) ? 0 : -(float) RX / 2000f),
                sensitivity * RotSensScale * (Settings.GetLock(DoF.Rotation, Axis.Y) ? 0 : -(float) RY / 2000f),
                sensitivity * RotSensScale * (Settings.GetLock(DoF.Rotation, Axis.Z) ? 0 : (float) RZ / 2000f)));
        }

        #endregion

        #region - Singleton -

        public static SpaceNavigatorWindows SubInstance
        {
            get { return _subInstance ?? (_subInstance = new SpaceNavigatorWindows()); }
        }

        private static SpaceNavigatorWindows _subInstance;

        #endregion - Singleton -

        #region - IDisposable -

        public override void Dispose()
        {
            Debug.Log("Uninstall Hook");
            if (!isrunning) return;
            SetWindowLongPtr(hMainWindow, -4, oldWndProcPtr);
            hMainWindow = IntPtr.Zero;
            oldWndProcPtr = IntPtr.Zero;
            newWndProcPtr = IntPtr.Zero;
            newWndProc = null;
            isrunning = false;
        }

        #endregion - IDisposable -
    }
#endif // UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
}