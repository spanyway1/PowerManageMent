using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace PowerSchemeManager
{
    class Program
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Guid
        {
            public uint Data1;
            public ushort Data2;
            public ushort Data3;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Data4;
        }

        struct PowerScheme
        {
            public Guid Guid;
            public string Name;
        }

        [DllImport("PowrProf.dll", SetLastError = true)]
        static extern uint PowerEnumerate(IntPtr RootPowerKey, IntPtr SchemeGuid, IntPtr SubGroupOfPowerSettingsGuid, uint AccessFlags, uint Index, IntPtr Buffer, ref uint BufferSize);

        [DllImport("PowrProf.dll", SetLastError = true)]
        static extern uint PowerReadFriendlyName(IntPtr RootPowerKey, ref Guid SchemeGuid, IntPtr SubGroupOfPowerSettingsGuid, IntPtr PowerSettingGuid, IntPtr Buffer, ref uint BufferSize);

        [DllImport("PowrProf.dll", SetLastError = true)]
        static extern uint PowerSetActiveScheme(IntPtr RootPowerKey, ref Guid SchemeGuid);

        const uint ACCESS_SCHEME = 16; // 0x10

        static string GuidToString(Guid guid)
        {
            return string.Format("{0:X8}-{1:X4}-{2:X4}-{3:X2}{4:X2}-{5:X2}{6:X2}{7:X2}{8:X2}{9:X2}{10:X2}",
                guid.Data1, guid.Data2, guid.Data3,
                guid.Data4[0], guid.Data4[1], guid.Data4[2], guid.Data4[3],
                guid.Data4[4], guid.Data4[5], guid.Data4[6], guid.Data4[7]);
        }

        static List<PowerScheme> ListPowerSchemes()
        {
            List<PowerScheme> schemes = new List<PowerScheme>();
            uint index = 0;
            uint bufferSize = 0;
            IntPtr buffer = IntPtr.Zero;
            Guid schemeGuid = new Guid();

            while (PowerEnumerate(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ACCESS_SCHEME, index, IntPtr.Zero, ref bufferSize) != 259) // 259 is ERROR_NO_MORE_ITEMS
            {
                buffer = Marshal.AllocHGlobal((int)bufferSize);
                if (PowerEnumerate(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ACCESS_SCHEME, index, buffer, ref bufferSize) == 0)
                {
                    schemeGuid = (Guid)Marshal.PtrToStructure(buffer, typeof(Guid));
                    uint nameSize = 1024;
                    IntPtr nameBuffer = Marshal.AllocHGlobal((int)nameSize);
                    if (PowerReadFriendlyName(IntPtr.Zero, ref schemeGuid, IntPtr.Zero, IntPtr.Zero, nameBuffer, ref nameSize) == 0)
                    {
                        string friendlyName = Marshal.PtrToStringUni(nameBuffer);
                        schemes.Add(new PowerScheme { Guid = schemeGuid, Name = friendlyName });
                    }
                    Marshal.FreeHGlobal(nameBuffer);
                }
                Marshal.FreeHGlobal(buffer);
                index++;
            }
            return schemes;
        }

        static void SetActivePowerScheme(Guid schemeGuid)
        {
            if (PowerSetActiveScheme(IntPtr.Zero, ref schemeGuid) == 0)
            {
                Console.WriteLine("Successfully set the active power scheme.");
            }
            else
            {
                Console.WriteLine("Failed to set the active power scheme.");
            }
        }

        static void Main(string[] args)
        {
            List<PowerScheme> schemes = ListPowerSchemes();

            Console.WriteLine("Available Power Schemes:");
            for (int i = 0; i < schemes.Count; i++)
            {
                Console.WriteLine($"{i}: {schemes[i].Name}");
            }

            Console.Write("Select a power scheme by entering the corresponding number: ");
            int selection;
            if (int.TryParse(Console.ReadLine(), out selection) && selection >= 0 && selection < schemes.Count)
            {
                SetActivePowerScheme(schemes[selection].Guid);
            }
            else
            {
                Console.WriteLine("Invalid selection.");
            }
            Console.WriteLine("Finished");
        }
    }
}
