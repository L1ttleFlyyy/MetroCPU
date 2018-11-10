using System.Management;

namespace OpenLibSys
{
    class WMICPUinfo
    {
        public string Name { get; private set; }
        public string Manufacturer { get; private set; }
        public uint MaxClockSpeed { get; private set; }
        public uint NumberOfCores { get; private set; }
        public uint NumberOfLogicalProcessors { get; private set; }
        public string SocketDesignation { get; private set; }
        public string Description { get; private set; }
        public uint L1Cache { get; private set; }
        public uint L2Cache { get; private set; }
        public uint L3Cache { get; private set; }
        public string Family { get; private set; }
        public string Model { get; private set; }
        public string Stepping { get; private set; }

        public WMICPUinfo()
        {
            Refresh();
        }
        public void Refresh()
        {
            using (ManagementObject mo = new ManagementObject("Win32_Processor.DeviceID='CPU0'"))
            {
                Name = (string)mo["Name"];
                Manufacturer = (string)mo["Manufacturer"];
                MaxClockSpeed = (uint)mo["MaxClockSpeed"];
                NumberOfCores = (uint)mo["NumberOfCores"];
                NumberOfLogicalProcessors = (uint)mo["NumberOfLogicalProcessors"];
                SocketDesignation = (string)mo["SocketDesignation"];
                Description = (string)mo["Description"];
                int ind1, ind2,ind3,l;
                ind1 = Description.IndexOf("Family");
                ind2 = Description.IndexOf("Model");
                ind3 = Description.IndexOf("Stepping");
                l = Description.Length;
                Family = Description.Substring(ind1+7,ind2-ind1-8);
                Model = Description.Substring(ind2+6,ind3-ind2-7);
                Stepping = Description.Substring(ind3+9);
            }
            using (ManagementObject Mo = new ManagementObject("Win32_CacheMemory.DeviceID='Cache Memory 0'"))
            {
                L1Cache = (uint)Mo["InstalledSize"];
            }
            using (ManagementObject Mo = new ManagementObject("Win32_CacheMemory.DeviceID='Cache Memory 1'"))
            {
                L2Cache = (uint)Mo["InstalledSize"];
            }
            using (ManagementObject Mo = new ManagementObject("Win32_CacheMemory.DeviceID='Cache Memory 2'"))
            {
                L3Cache = (uint)Mo["InstalledSize"];
            }
        }
    }
}

