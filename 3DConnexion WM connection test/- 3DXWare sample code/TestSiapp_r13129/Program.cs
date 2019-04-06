///////////////////////////////////////////////////////////////////////////////////
// Copyright notice:
//   Copyright (c) 3Dconnexion. All rights reserved. 
// 
// This file and source code are an integral part of the "3Dconnexion
// Software Development Kit", including all accompanying documentation,
// and is protected by intellectual property laws. All use of the
// 3Dconnexion Software Development Kit is subject to the License
// Agreement found in the "LicenseAgreementSDK.txt" file.
// All rights not expressly granted by 3Dconnexion are reserved.
//

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace TestSiapp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
