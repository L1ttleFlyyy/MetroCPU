using System;
using System.Management;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenLibSys
{
    class WMICPUinfo
    {
        public string Name { get; private set; }
        public string Manufacturer { get; private set; }
        public uint MaxClockSpeed { get; private set; }
        public uint NumberOfCores { get; private set; }
        public uint NumberOfLogicalProcessors { get; private set; }

        public WMICPUinfo()
        {
            Refresh();
        }
        public void Refresh()
        {
            using (ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            {
                foreach (ManagementObject mo in mos.Get())
                {
                    Name = (string)mo["Name"];
                    Manufacturer = (string)mo["Manufacturer"];
                    MaxClockSpeed = (uint)mo["MaxClockSpeed"];
                    NumberOfCores = (uint)mo["NumberOfCores"];
                    NumberOfLogicalProcessors = (uint)mo["NumberOfLogicalProcessors"];
                }
            }
        }
    }
}

