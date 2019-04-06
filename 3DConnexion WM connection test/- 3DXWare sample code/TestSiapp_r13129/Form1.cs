///////////////////////////////////////////////////////////////////////////////////
// Copyright notice:
//   Copyright (c) 2016 3Dconnexion. All rights reserved.
//
// This file and source code are an integral part of the "3Dconnexion
// Software Development Kit", including all accompanying documentation,
// and is protected by intellectual property laws. All use of the
// 3Dconnexion Software Development Kit is subject to the License
// Agreement found in the "LicenseAgreementSDK.txt" file.
// All rights not expressly granted by 3Dconnexion are reserved.
//

using _3Dconnexion;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows.Forms;
using TestSiapp.Properties;

namespace TestSiapp
{
  public partial class Form1 : Form
  {
    #region Variables

    private const string appName = "Test Siapp";
    private const string logFileName = "log.txt";
    private const string templateTR = "TX: {1,-7}{0}TY: {2,-7}{0}TZ: {3,-7}{0}RX: {4,-7}{0}RY: {5,-7}{0}RZ: {6,-7}{0}P: {7}";

    private IntPtr hWnd = IntPtr.Zero;
    private IntPtr devHdl = IntPtr.Zero;
    private SiApp.SpwRetVal res;

    #endregion Variables

    public Form1()
    {
      InitializeComponent();

      hWnd = Handle;

      // Prepare log file
      File.Create(logFileName).Close();

      // Initialize the driver
      InitializeSiApp();

      // Export the application commands to the driver
      ExportApplicationCommands();

      // Set the buttonbank / action set to use on the 3Dconnexion devices
      SiApp.SiAppCmdActivateActionSet(devHdl, @"ACTION_SET_ID");
    }

    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    protected override void WndProc(ref Message msg)
    {
      TrackMouseEvents(msg);

      base.WndProc(ref msg);
    }

    private void Form1_FormClosed(object sender, FormClosedEventArgs e)
    {
      CloseSiApp();
    }

    #region Methods

    private void InitializeSiApp()
    {
      res = SiApp.SiInitialize();
      if (res != SiApp.SpwRetVal.SPW_NO_ERROR)
        MessageBox.Show("Initialize function failed");
      Log("SiInitialize", res.ToString());

      SiApp.SiOpenData openData = new SiApp.SiOpenData();
      SiApp.SiOpenWinInit(ref openData, hWnd);
      if (openData.hWnd == IntPtr.Zero)
        MessageBox.Show("Handle is empty");
      Log("SiOpenWinInit", openData.hWnd + "(window handle)");

      devHdl = SiApp.SiOpen(appName, SiApp.SI_ANY_DEVICE, IntPtr.Zero, SiApp.SI_EVENT, ref openData);
      if (devHdl == IntPtr.Zero)
        MessageBox.Show("Open returns empty device handle");
      Log("SiOpen", devHdl + "(device handle)");
    }

    private void CloseSiApp()
    {
      SiApp.SpwRetVal res = SiApp.SiClose(devHdl);
      Log("SiClose", res.ToString());
      int r = SiApp.SiTerminate();
      Log("SiTerminate", r.ToString());
    }

    private void TrackMouseEvents(Message msg)
    {
      SiApp.SiGetEventData eventData = new SiApp.SiGetEventData();
      IntPtr eventPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SiApp.SiSpwEvent_JustType)));

      SiApp.SiGetEventWinInit(ref eventData, msg.Msg, msg.WParam, msg.LParam);
      SiApp.SpwRetVal val = SiApp.SiGetEvent(devHdl, SiApp.SI_AVERAGE_EVENTS, ref eventData, eventPtr);

      if (val == SiApp.SpwRetVal.SI_IS_EVENT)
      {
        Log("SiGetEventWinInit", eventData.msg.ToString());

        SiApp.SiSpwEvent_JustType sEventType = PtrToStructure<SiApp.SiSpwEvent_JustType>(eventPtr);
        switch (sEventType.type)
        {
          case SiApp.SiEventType.SI_MOTION_EVENT:
            SiApp.SiSpwEvent_SpwData motionEvent = PtrToStructure<SiApp.SiSpwEvent_SpwData>(eventPtr);
            string motionData = string.Format(templateTR, "",
              motionEvent.spwData.mData[0], motionEvent.spwData.mData[1], motionEvent.spwData.mData[2], // TX, TY, TZ
              motionEvent.spwData.mData[3], motionEvent.spwData.mData[4], motionEvent.spwData.mData[5], // RX, RY, RZ
              motionEvent.spwData.period); // Period (normally 16 ms)
            Print("Motion event " + motionData);
            break;

          case SiApp.SiEventType.SI_ZERO_EVENT:
            Print("Zero event");
            break;

          case SiApp.SiEventType.SI_CMD_EVENT:
            SiApp.SiSpwEvent_CmdEvent cmdEvent = PtrToStructure<SiApp.SiSpwEvent_CmdEvent>(eventPtr);
            SiApp.SiCmdEventData cmd = cmdEvent.cmdEventData;
            Log("SI_APP_EVENT: ", string.Format("V3DCMD = {0}, pressed = {1}", cmd.functionNumber, cmd.pressed > 0));
            Print("V3DCMD event: V3DCMD = {0}, pressed = {1}", cmd.functionNumber, cmd.pressed > 0);
            break;

          case SiApp.SiEventType.SI_APP_EVENT:
            SiApp.SiSpwEvent_AppCommand appEvent = PtrToStructure<SiApp.SiSpwEvent_AppCommand>(eventPtr);
            SiApp.SiAppCommandData data = appEvent.appCommandData;
            Log("SI_APP_EVENT: ", string.Format("appCmdID = \"{0}\", pressed = {1}", data.id.appCmdID, data.pressed > 0));
            Print("App event: appCmdID = \"{0}\", pressed = {1}", data.id.appCmdID, data.pressed > 0);
            break;
        }

        Log("SiGetEvent", string.Format("{0}({1})", sEventType.type, val));
      }

      Marshal.FreeHGlobal(eventPtr);
    }

    /// <summary>
    /// Method to export the application commands to 3dxware so that they can be assigned to
    /// 3Dconnexion device buttons.
    /// </summary>
    private void ExportApplicationCommands()
    {
      string imagesPath = string.Empty;
      string resDllPath = string.Empty;
      string dllName = "3DxService.dll";

      string homePath = Get3DxWareHomeDirectory();

      if (!string.IsNullOrEmpty(homePath))
      {
        imagesPath = Path.Combine(homePath, @"Cfg\Images\3DxService\{0}");
        resDllPath = Path.Combine(homePath, @"en-US\" + dllName);
      }

      using (ActionCache cache = new ActionCache())
      {
        // An actionset can also be considered to be a buttonbank, a menubar, or a set of toolbars
        ActionNode buttonBank = cache.Add(new ActionSet("ACTION_SET_ID", "Custom action set"));

        // Add a couple of categiores / menus / tabs to the buttonbank/menubar/toolbar
        ActionNode fileNode = buttonBank.Add(new Category("CAT_ID_FILE", "File"));
        ActionNode editNode = buttonBank.Add(new Category("CAT_ID_EDIT", "Edit"));

        // Add menu items to the menus. When the button on the 3D mouse is pressed the id will be sent to the application
        // in the SI_APP_EVENT event structure in the SiAppCmdID.appCmdID field

        // Add menu items / actions which use bitmaps loaded in memory of this application
        fileNode.Add(new _3Dconnexion.Action("ID_OPEN", "Open", "Open file", ImageItem.FromImage(Resources.Open)));
        fileNode.Add(new _3Dconnexion.Action("ID_CLOSE", "Close", "Close file", ImageItem.FromImage(Resources.Сlose)));
        fileNode.Add(new _3Dconnexion.Action("ID_EXIT", "Exit", "Exit program", ImageItem.FromImage(Resources.Exit)));

        // Export a menu item / action using a bitmap from an external dll resource
        fileNode.Add(new _3Dconnexion.Action("ID_ABOUT", "About", "Info about the program", ImageItem.FromResource(resDllPath, "#172", "#2")));

        // Add menu items / actions using bitmaps located on the harddrive
        editNode.Add(new _3Dconnexion.Action("ID_CUT", "Cut", "Shortcut is Ctrl + X", ImageItem.FromFile(string.Format(imagesPath, "Macro_Cut.png"))));
        editNode.Add(new _3Dconnexion.Action("ID_COPY", "Copy", "Shortcut is Ctrl + C", ImageItem.FromFile(string.Format(imagesPath, "Macro_Copy.png"))));
        editNode.Add(new _3Dconnexion.Action("ID_PASTE", "Paste", "Shortcut is Ctrl + V", ImageItem.FromFile(string.Format(imagesPath, "Macro_Paste.png"))));

        // Add a menu item without an image associated with it
        editNode.Add(new _3Dconnexion.Action("ID_UNDO", "Undo", "Shortcut is Ctrl + Z"));

        // Now add an image and associate it with the menu item ID_UNDO by using the same id as the menu item / action
        cache.Add(ImageItem.FromFile(string.Format(imagesPath, "Macro_Undo.png"), 0, @"ID_UNDO"));
        try
        {
          // Write the complete cache to the driver
          cache.SaveTo3Dxware(devHdl);
        }
        catch (Exception e)
        {
          Log("cache.SaveTo3Dxware", e.Message);
        }
      }
    }

    private string Get3DxWareHomeDirectory()
    {
      string softwareKeyName = string.Empty;
      string homeDirectory = string.Empty;

      if (IntPtr.Size == 8)
      {
        softwareKeyName = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\3Dconnexion\3DxWare";
      }
      else
      {
        softwareKeyName = @"HKEY_LOCAL_MACHINE\SOFTWARE\3Dconnexion\3DxWare";
      }

      object regValue = Microsoft.Win32.Registry.GetValue(softwareKeyName, "Home Directory", null);
      if (regValue != null)
      {
        homeDirectory = regValue.ToString();
      }

      return homeDirectory;
    }

    private T PtrToStructure<T>(IntPtr ptr) where T : struct
    {
      return (T)Marshal.PtrToStructure(ptr, typeof(T));
    }

    #endregion Methods

    #region Logging

    private void Print(string format, params object[] args)
    {
      Print(string.Format(format, args));
    }

    private void Print(string message)
    {
      textBox1.AppendText(string.Format("{0}{1}", message, Environment.NewLine));
    }

    private void Log(string functionName, string result)
    {
      string msg = string.Format("{0}: Function {1} returns {2}{3}", DateTime.Now, functionName, result, Environment.NewLine);
      try
      {
        File.AppendAllText(logFileName, msg);
      }
      catch { };
    }

    #endregion Logging
  }
}