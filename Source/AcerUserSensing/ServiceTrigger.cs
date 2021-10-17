using System;
using System.Runtime.InteropServices;
using ContextSensingClient;

namespace AcerUserSensing
{
    public class ServiceTrigger
    {
        private string serviceName = @"IntelContextService";
         private Guid customTriggerGuid = new Guid("{F8445F8F-18D5-4076-A2C0-9D1669782993}");
        private enum TriggerType { Start, Stop }
        private uint SC_MANAGER_CONNECT = 0x00001;
        private uint SERVICE_CONFIG_TRIGGER_INFO = 8;
        private Logger logger = new Logger();


        internal bool IsEnabled()
        {
            bool isEnabled = false;
            IntPtr serviceManager = OpenSCManager(null, null, SC_MANAGER_CONNECT); // Ony OpenSCManager gives read-only access
            if (serviceManager != null)
            {
                IntPtr service = OpenService(serviceManager, serviceName, SC_MANAGER_CONNECT);
                if (service != null)
                {
                    int bytesNeeded = -1;
                    QueryServiceConfig2(service, SERVICE_CONFIG_TRIGGER_INFO, IntPtr.Zero, 0, out bytesNeeded);
                    if (bytesNeeded > 0)
                    {
                        IntPtr buf = Marshal.AllocHGlobal(bytesNeeded);
                        if (QueryServiceConfig2(service, SERVICE_CONFIG_TRIGGER_INFO, buf, bytesNeeded, out bytesNeeded))
                        {
                            SERVICE_TRIGGER_INFO triggerInfo = (SERVICE_TRIGGER_INFO)Marshal.PtrToStructure(buf, typeof(SERVICE_TRIGGER_INFO));
                            logger.Info("Trigger count", triggerInfo.cTriggers);
                            isEnabled = triggerInfo.cTriggers > 0;
                        }
                        Marshal.FreeHGlobal(buf);
                    }
                    CloseServiceHandle(service);
                }
                CloseServiceHandle(serviceManager);
            }
            return isEnabled;
        }

        private uint SendEvent(TriggerType tiggerType)
        {
            long handle = 0;
            uint errorLevel = EventRegister(ref customTriggerGuid, IntPtr.Zero, IntPtr.Zero, ref handle);
            if (errorLevel != 0)
            {
                logger.Error("EventRegister error", errorLevel);
                return errorLevel;
            }
            errorLevel = EventWriteString(handle, 0, 0, tiggerType.ToString());
            if (errorLevel != 0)
            {
                logger.Error("EventWriteString error", errorLevel);
            }
            EventUnregister(handle);
            return errorLevel;
        }

        internal uint Start()
        {
            return SendEvent(TriggerType.Start);
        }

        internal uint Stop()
        {
            return SendEvent(TriggerType.Stop);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SERVICE_TRIGGER_INFO
        {
            public uint cTriggers;
            public IntPtr pTriggers;
            public IntPtr pReserved;
        }

        [DllImport("Advapi32.dll", SetLastError = true)]
        private static extern uint EventRegister(ref Guid guid, [Optional] IntPtr EnableCallback, [Optional] IntPtr CallbackContext, [In][Out] ref long RegHandle);

        [DllImport("Advapi32.dll", SetLastError = true)]
        private static extern uint EventWriteString(long handle, byte level, ulong keyword, [MarshalAs(UnmanagedType.LPWStr)]string str);

        [DllImport("Advapi32.dll", SetLastError = true)]
        private static extern uint EventUnregister(long RegHandle);

        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenSCManager(string machineName, string databaseName, uint access);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool QueryServiceConfig2(IntPtr handle, UInt32 infoLevel, IntPtr buffer, int bufSize, out int bytesNeeded);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseServiceHandle(IntPtr hSCObject);


    }
}
