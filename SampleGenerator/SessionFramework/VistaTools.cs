using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using Utilities;

namespace SessionFramework
{
    public class VistaTools
    {
        static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [DllImport("shell32.dll", EntryPoint = "#680", CharSet = CharSet.Unicode)]
        public static extern bool IsUserAnAdmin();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle,
            UInt32 DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetTokenInformation(
            IntPtr TokenHandle,
            TOKEN_INFORMATION_CLASS TokenInformationClass,
            IntPtr TokenInformation,
            uint TokenInformationLength,
            out uint ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        public const uint TOKEN_QUERY = 0x0008;
        private const string UAC_REGISTRY = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
        private const string UAC_KEY = "EnableLUA";
        private readonly bool isVistaOS = (System.Environment.OSVersion.Version.Major >= 6);
        private readonly bool uacOnMode = false;
        private readonly bool isElevatedMode = false;
        private static VistaTools instance = null;

        private VistaTools()
        {
            if (isVistaOS)
            {
                TOKEN_ELEVATION_TYPE tokenElevationType = TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault;

                // You are on a Vista operating system!  Some extra checks need to be performed.  First,
                // UAC should be turn ON in all situations.  Second, if attempting to perform a -regserver
                // or an -unregserver operation, the process must be kicked off as an elevated administrator
                // type.
                uacOnMode = isUacTurnedOn();
                log.Debug("Vista or Newer OS with UAC: " + uacOnMode.ToString());

                tokenElevationType = getElevationType();
                string logType = "undefined";
                switch (tokenElevationType)
                {
                    case TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault:
                        logType = "User is \"standard\" user and/or UAC is disabled";
                        break;

                    case TOKEN_ELEVATION_TYPE.TokenElevationTypeFull:
                        logType = "UAC is enabled and running as elevated";
                        isElevatedMode = true;
                        break;

                    case TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited:
                        logType = "UAC is enabled but NOT running as elevated";
                        break;
                }

                log.Debug(logType);

                // Determine if UAC is turned ON... Note that there is a potential for error here.  If 
                // the user changed UAC but did NOT reboot the system, then the UAC status reported is
                // not reflective of the actual state of the machine.  Unfortunately, there is no way to 
                // test for this scenario...
                if (!uacOnMode)
                {
                    log.Info("UAC is currently turned OFF.  This is not a recommended state in which to\n" +
                                    "run the application.  You may experience spurious connectivity problems with\n" +
                                    "QuickBooks.  Please turn UAC on; rebooting the computer is a mandatory step!"
                                   );
                }
            }
            else
            {
                log.Debug("OS Major Version is: " + System.Environment.OSVersion.Version.Major);
            }
        }

        /// <summary>
        /// Returns and instance of the Singleton object
        /// </summary>
        /// <returns>The Singleton object</returns>
        public static VistaTools getInstance()
        {
            if (instance == null)
                instance = new VistaTools();

            return instance;
        }

        /// <summary>
        /// Determines if the OS is Vista or newer
        /// </summary>
        /// <returns>true if the OS is Vista or newer</returns>
        public bool isVista()
        {
            return isVistaOS;
        }

        /// <summary>
        /// Returns the Elevation status
        /// </summary>
        /// <returns>true if elevated</returns>
        public bool isElevated()
        {
            return isElevatedMode;
        }


        /// <summary>
        /// Ensures that there are no violations in Vista type OS
        ///   1. Can only have one argument if performing a (un)regserver operation
        ///   2. (un)regserver type operations must be run in elevated mode
        ///   3. Normal program operation may not be initiated in elevated mode
        /// </summary>
        /// <param name="regOp">true if a (un)regserver argument is being processed</param>
        /// <param name="args">the parameter list</param>
        /// <returns>true if there is an error</returns>
        public Status vistaModeCheck(bool regOp, string[] args)
        {
            if (!isVistaOS)
                return new Status("Not Vista OS", ErrorCode.False, 0);

            // Fake it a bit...
            int argLen = args.Length;
            if (argLen == 2)
            {
                if ((args[0] == "-silent") || (args[1] == "-silent"))
                    argLen = 1;
            }


            if (regOp && (argLen > 1))
            {
                // If we get here, it means that the user has a -regserver or -unregserver command line 
                // AND at least one other argument on the command line as well; this is not permissible.  
                // Due to the security model of Vista, (un)reg operations must be performed by the administrator
                // so that the CLASS_ID is added at global scope.  However, the application cannot
                // be run in this mode. (We are ignoring chaining of reg type operations as logically
                // this does not make any sense)
                string message = "Due to the Microsoft Vista security model, you cannot chain\n" +
                               "command line arguments such as \"-regserver -subscribe\".  The\n" +
                               "registry commands must be run with Administrator privileges and\n" +
                               "the subscription commands must be run with normal privileges.";
                return new Status(message, ErrorCode.True, 0);
            }

            if (regOp && !isElevatedMode)
            {
                // If we get here, it means that the user is trying to do a registration operation 
                // AND user is not elevated.  This will not work as the temporary registry is used.
                string message = "You may only perform the -regserver or -unregserver operations\n" +
                                "when you are running in elevated (admin) mode.";
                return new Status(message, ErrorCode.True, 0);
            }

            if (!regOp && isElevatedMode)
            {
                // If we get here, it means that the user is trying to run the application (i.e. no
                // (un)regserver type operations are being requested) but the application is being
                // run in elevated mode.  This is not permissible.
                // If we get here, it means that the user is trying to do a registration operation 
                // AND user is not elevated.  This will not work as the temporary registry is used.
                string message = "During normal operation (i.e. not performing a (un)regserver action)\n" +
                                "the process may not be run in elevated mode.";
                return new Status(message, ErrorCode.True, 0);
            }

            return new Status("Vista Check Completed", ErrorCode.False, 0);
        }

        /// <summary>
        /// Determines the status of UAC
        /// </summary>
        /// <returns>true if UAC is turned ON</returns>
        public bool isUacTurnedOn()
        {
            if (!isVistaOS)
                throw new VistaException("GetElevationType() can only be run on a Vista, or newer, operating system");

            RegistryKey uacKey = Registry.LocalMachine.OpenSubKey(UAC_REGISTRY) ?? throw new VistaException("Cannot find the UAC key: " + UAC_REGISTRY);
            if ((int)uacKey.GetValue(UAC_KEY) == 1)
                return true;
            else
                return false;
        }

        public TOKEN_ELEVATION_TYPE getElevationType()
        {
            if (!isVistaOS)
                throw new VistaException("GetElevationType() can only be run on a Vista, or newer, operating system");

            bool bRetVal = false;
            IntPtr hToken = IntPtr.Zero;
            IntPtr hProcess = GetCurrentProcess();

            if (hProcess == IntPtr.Zero)
                throw new VistaException("Error: unable to obtain the current process handle");

            bRetVal = OpenProcessToken(hProcess, TOKEN_QUERY, out hToken);

            if (!bRetVal)
                throw new VistaException("Error opening process token");

            try
            {
                TOKEN_ELEVATION_TYPE tet = TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault;

                UInt32 dwReturnLength = 0;
                UInt32 tetSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf((int)tet);
                IntPtr tetPtr = Marshal.AllocHGlobal((int)tetSize);
                try
                {

                    bRetVal = GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenElevationType, tetPtr, tetSize, out dwReturnLength);

                    if ((!bRetVal) | (tetSize != dwReturnLength))
                    {
                        throw new VistaException("Error getting token information");
                    }

                    tet = (TOKEN_ELEVATION_TYPE)Marshal.ReadInt32(tetPtr);
                }
                finally
                {
                    Marshal.FreeHGlobal(tetPtr);
                }

                return tet;
            }
            finally
            {
                CloseHandle(hToken);
            }
        }
    }


    public enum TOKEN_ELEVATION_TYPE
    {
        TokenElevationTypeDefault = 1,                  // User is "standard" user and/or UAC is disabled
        TokenElevationTypeFull = 2,                     // UAC is enabled and running as elevated
        TokenElevationTypeLimited = 3                   // UAC is enabled but NOT running as elevated
    }

    public enum TOKEN_INFORMATION_CLASS
    {
        TokenUser = 1,
        TokenGroups = 2,
        TokenPrivileges = 3,
        TokenOwner = 4,
        TokenPrimaryGroup = 5,
        TokenDefaultDacl = 6,
        TokenSource = 7,
        TokenType = 8,
        TokenImpersonationLevel = 9,
        TokenStatistics = 10,
        TokenRestrictedSids = 11,
        TokenSessionId = 12,
        TokenGroupsAndPrivileges = 13,
        TokenSessionReference = 14,
        TokenSandBoxInert = 15,
        TokenAuditPolicy = 16,
        TokenOrigin = 17,
        TokenElevationType = 18,
        TokenLinkedToken = 19,
        TokenElevation = 20,
        TokenHasRestrictions = 21,
        TokenAccessInformation = 22,
        TokenVirtualizationAllowed = 23,
        TokenVirtualizationEnabled = 24,
        TokenIntegrityLevel = 25,
        TokenUIAccess = 26,
        TokenMandatoryPolicy = 27,
        TokenLogonSid = 28,
        MaxTokenInfoClass = 29  // MaxTokenInfoClass should always be the last enum
    }
}
