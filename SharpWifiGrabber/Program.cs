using System;
using System.Runtime.InteropServices;
using System.Xml;

namespace SharpWifiGrabber
{
    class Program
    {
        static void Main(string[] args)
        {
            const int dwClientVersion = 2;
            IntPtr clientHandle = IntPtr.Zero;
            IntPtr pdwNegotiatedVersion = IntPtr.Zero;
            IntPtr pInterfaceList = IntPtr.Zero;
            WLAN_INTERFACE_INFO_LIST interfaceList;
            WLAN_PROFILE_INFO_LIST wifiProfileList;
            Guid InterfaceGuid;
            IntPtr pAvailableNetworkList = IntPtr.Zero;
            string wifiXmlProfile = null;
            IntPtr wlanAccess = IntPtr.Zero;
            IntPtr profileList = IntPtr.Zero;
            string profileName = "";

            try
            {
                // Open Wifi Handle
                WlanOpenHandle(dwClientVersion, IntPtr.Zero, out pdwNegotiatedVersion, ref clientHandle);

                // Find Wi-Fi interface GUID
                WlanEnumInterfaces(clientHandle, IntPtr.Zero, ref pInterfaceList);
                interfaceList = new WLAN_INTERFACE_INFO_LIST(pInterfaceList);
                InterfaceGuid = ((WLAN_INTERFACE_INFO)interfaceList.InterfaceInfo[0]).InterfaceGuid;
                // Get Wifi Profile
                WlanGetProfileList(clientHandle, InterfaceGuid, IntPtr.Zero, ref profileList);
                wifiProfileList = new WLAN_PROFILE_INFO_LIST(profileList);
                Console.WriteLine("");
                Banner();
                Console.WriteLine("Found {0} SSIDs: ", wifiProfileList.dwNumberOfItems);
                Console.WriteLine("============================");
                Console.WriteLine("");

                for (int i = 0; i < wifiProfileList.dwNumberOfItems; i++)
                {
                    try
                    {
                        profileName = (wifiProfileList.ProfileInfo[i]).strProfileName;
                        int decryptKey = 63; //https://docs.microsoft.com/en-us/windows/win32/nativewifi/wlan-profileschema-keymaterial-sharedkey-element
                        // Retrieve Wifi SSID Name and Passsword
                        WlanGetProfile(clientHandle, InterfaceGuid, profileName, IntPtr.Zero, out wifiXmlProfile, ref decryptKey, out wlanAccess);

                        XmlDocument xmlProfileXml = new XmlDocument();
                        xmlProfileXml.LoadXml(wifiXmlProfile);

                        XmlNodeList pathToSSID = xmlProfileXml.SelectNodes("//*[name()='WLANProfile']/*[name()='SSIDConfig']/*[name()='SSID']/*[name()='name']");
                        XmlNodeList pathToPassword = xmlProfileXml.SelectNodes("//*[name()='WLANProfile']/*[name()='MSM']/*[name()='security']/*[name()='sharedKey']/*[name()='keyMaterial']");


                        foreach (XmlNode ssid in pathToSSID)
                        {
                            Console.WriteLine("SSID: " + ssid.InnerText);
                            foreach (XmlNode password in pathToPassword)
                            {
                                Console.WriteLine("Password: " + password.InnerText);
                            }
                            Console.WriteLine("----------------------------");

                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                }

                // Close Wifi Handle
                WlanCloseHandle(clientHandle, IntPtr.Zero);

            }

            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

       private static void Banner()
       {
            Console.WriteLine(@"   _____ _                  __          ___  __ _  _____           _     _               ");
            Console.WriteLine(@"  / ____| |                 \ \        / (_)/ _(_)/ ____|         | |   | |              ");
            Console.WriteLine(@" | (___ | |__   __ _ _ __ _ _\ \  /\  / / _| |_ _| |  __ _ __ __ _| |__ | |__   ___ _ __ ");
            Console.WriteLine(@"  \___ \| '_ \ / _` | '__| '_ \ \/  \/ / | |  _| | | |_ | '__/ _` | '_ \| '_ \ / _ \ '__|");
            Console.WriteLine(@"  ____) | | | | (_| | |  | |_) \  /\  /  | | | | | |__| | | | (_| | |_) | |_) |  __/ |   ");
            Console.WriteLine(@" |_____/|_| |_|\__,_|_|  | .__/ \/  \/   |_|_| |_|\_____|_|  \__,_|_.__/|_.__/ \___|_|   ");
            Console.WriteLine(@"                         | |                                                             ");
            Console.WriteLine(@"                         |_|                                                             ");
            Console.WriteLine("                               v0.0.1                         							 ");
            Console.WriteLine("                                @r3n_hat                      							 ");
            Console.WriteLine("                                                              							 ");
            Console.WriteLine("                                                              							 ");
        }

        #region wlanapi PInvoke

        // https://docs.microsoft.com/en-us/windows/win32/api/wlanapi/nf-wlanapi-wlanopenhandle
        // https://www.pinvoke.net/default.aspx/wlanapi.WlanOpenHandle
        [DllImport("Wlanapi.dll")]
        public static extern int WlanOpenHandle(int dwClientVersion, IntPtr pReserved, [Out] out IntPtr pdwNegotiatedVersion, ref IntPtr ClientHandle);

        // https://docs.microsoft.com/en-us/windows/win32/api/wlanapi/nf-wlanapi-wlanclosehandle
        // https://www.pinvoke.net/default.aspx/wlanapi.WlanCloseHandle
        [DllImport("Wlanapi", EntryPoint = "WlanCloseHandle")]
        public static extern uint WlanCloseHandle([In] IntPtr hClientHandle, IntPtr pReserved);

        // https://docs.microsoft.com/en-us/windows/win32/api/wlanapi/nf-wlanapi-wlanenuminterfaces
        // https://www.pinvoke.net/default.aspx/wlanapi.wlanenuminterfaces

        [DllImport("Wlanapi", EntryPoint = "WlanEnumInterfaces")]
        public static extern uint WlanEnumInterfaces([In] IntPtr hClientHandle, IntPtr pReserved, ref IntPtr ppInterfaceList);

        // https://docs.microsoft.com/en-us/windows/win32/api/wlanapi/nf-wlanapi-wlangetprofile
        // https://www.pinvoke.net/default.aspx/wlanapi/wlangetprofile.html?diff=y

        [DllImport("wlanapi.dll", SetLastError = true)]
        public static extern uint WlanGetProfile([In] IntPtr clientHandle, [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid, [In, MarshalAs(UnmanagedType.LPWStr)] string profileName, [In] IntPtr pReserved, [Out, MarshalAs(UnmanagedType.LPWStr)] out string profileXml, [In, Out, Optional] ref int flags, [Out, Optional] out IntPtr pdwGrantedAccess);

        [DllImport("wlanapi.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public static extern uint WlanGetProfileList([In] IntPtr clientHandle, [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid, [In] IntPtr pReserved, ref IntPtr profileList);

        #endregion wlanapi PInvoke

        #region WiFi struct
        // https://www.codeproject.com/Articles/72105/Manage-WiFi-with-Native-API-WIFI-on-Windows-XP-SP2
        // https://github.com/jcwalker/WiFiProfileManagement/tree/88b2139c0c1c4ba1d759fbc5ac15f016d8f6f6ad
        [StructLayout(LayoutKind.Sequential)]
        public struct WLAN_INTERFACE_INFO_LIST
        {

            public int dwNumberofItems;
            public int dwIndex;
            public WLAN_INTERFACE_INFO[] InterfaceInfo;


            public WLAN_INTERFACE_INFO_LIST(IntPtr pList)
            {
                // The first 4 bytes are the number of WLAN_INTERFACE_INFO structures.
                dwNumberofItems = (int)Marshal.ReadInt64(pList, 0);
                // The next 4 bytes are the index of the current item in the unmanaged API.
                dwIndex = (int)Marshal.ReadInt64(pList, 4);
                // Construct the array of WLAN_INTERFACE_INFO structures.
                InterfaceInfo = new WLAN_INTERFACE_INFO[dwNumberofItems];
                for (int i = 0; i < dwNumberofItems; i++)
                {
                    // The offset of the array of structures is 8 bytes past the beginning. Then, take the index and multiply it by the number of bytes in the structure.
                    // the length of the WLAN_INTERFACE_INFO structure is 532 bytes - this was determined by doing a sizeof(WLAN_INTERFACE_INFO) in an unmanaged C++ app.
                    IntPtr pItemList = new IntPtr(pList.ToInt64() + (i * 532) + 8);
                    // Construct the WLAN_INTERFACE_INFO structure, marshal the unmanaged structure into it, then copy it to the array of structures.
                    WLAN_INTERFACE_INFO wii = new WLAN_INTERFACE_INFO();
                    wii = (WLAN_INTERFACE_INFO)Marshal.PtrToStructure(pItemList, typeof(WLAN_INTERFACE_INFO));
                    InterfaceInfo[i] = wii;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WLAN_INTERFACE_INFO
        {
            /// GUID->_GUID
            public Guid InterfaceGuid;

            /// WCHAR[256]
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strInterfaceDescription;

        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WLAN_PROFILE_INFO
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strProfileName;
            public WlanProfileFlags ProfileFLags;
        }

        [Flags]
        public enum WlanProfileFlags
        {
            AllUser = 0,
            GroupPolicy = 1,
            User = 2
        }

        public struct WLAN_PROFILE_INFO_LIST
        {
            public int dwNumberOfItems;
            public int dwIndex;
            public WLAN_PROFILE_INFO[] ProfileInfo;

            public WLAN_PROFILE_INFO_LIST(IntPtr ppProfileList)
            {
                dwNumberOfItems = (int)Marshal.ReadInt64(ppProfileList);
                dwIndex = (int)Marshal.ReadInt64(ppProfileList, 4);
                ProfileInfo = new WLAN_PROFILE_INFO[dwNumberOfItems];
                IntPtr ppProfileListTemp = new IntPtr(ppProfileList.ToInt64() + 8);

                for (int i = 0; i < dwNumberOfItems; i++)
                {
                    ppProfileList = new IntPtr(ppProfileListTemp.ToInt64() + i * Marshal.SizeOf(typeof(WLAN_PROFILE_INFO)));
                    ProfileInfo[i] = (WLAN_PROFILE_INFO)Marshal.PtrToStructure(ppProfileList, typeof(WLAN_PROFILE_INFO));
                }
            }
        }
        #endregion WiFi struct
    }
}