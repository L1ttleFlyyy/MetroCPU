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
        public string L1Cache { get; private set; }
        public string L2Cache { get; private set; }
        public string L3Cache { get; private set; }
        public string Family { get; private set; }
        public string Model { get; private set; }
        public string Stepping { get; private set; }
        public string CPUIcon { get; private set; }

        public WMICPUinfo()
        {
            Refresh();
        }

        private string CheckMB(uint number)
        {
            if (number < 1024)
            {
                return $"{number} KB";
            }
            else
                return $"{number>>10} MB";
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
                L1Cache = CheckMB((uint)Mo["InstalledSize"]);
            }
            using (ManagementObject Mo = new ManagementObject("Win32_CacheMemory.DeviceID='Cache Memory 1'"))
            {
                L2Cache = CheckMB((uint)Mo["InstalledSize"]);
            }
            using (ManagementObject Mo = new ManagementObject("Win32_CacheMemory.DeviceID='Cache Memory 2'"))
            {
                L3Cache = CheckMB((uint)Mo["InstalledSize"]);
            }

            switch(Manufacturer)
            {
                case "GenuineIntel":
                    Manufacturer = "Intel";
                    if (Name.Contains("Core"))
                    {
                        if (Name.Contains("i5"))
                            CPUIcon = "imagesrc/i5.png";
                        else if (Name.Contains("i7"))
                            CPUIcon = "imagesrc/i7.png";
                        else if (Name.Contains("i3"))
                            CPUIcon = "imagesrc/i3.png";
                        else if (Name.Contains("i9"))
                            CPUIcon = "imagesrc/i9.png";
                        else
                            CPUIcon = "imagesrc/GenericIntel.png";
                    }
                    else if (Name.Contains("Xeon"))
                    {
                        CPUIcon = "imagesrc/xeon.png";
                    }
                    else
                        CPUIcon = "imagesrc/GenericIntel.png";
                    break;
                case "AuthenticAMD":
                    Manufacturer = "AMD";
                    if(Name.Contains("Ryzen"))
                    {
                        if (Name.Contains("Threadripper"))
                            CPUIcon = "imagesrc/threadripper.png";
                        else
                        {
                            var tmp = Name.Substring(Name.IndexOf("Ryzen")+6,1);
                            switch(tmp)
                            {
                                case "3":
                                    CPUIcon = "imagesrc/r3.png";
                                    break;
                                case "5":
                                    CPUIcon = "imagesrc/r5.png";
                                    break;
                                case "7":
                                    CPUIcon = "imagesrc/r7.png";
                                    break;
                                default:
                                    CPUIcon = "imagesrc/GenericAMD.png";
                                    break;
                            }
                        }
                    }
                    else if(Name.Contains("EPYC"))
                    {
                        CPUIcon = "imagesrc/EPYC.png";
                    }
                    else
                        CPUIcon = "imagesrc/GenericAMD.png";
                    break;
                default:
                    Manufacturer = "Unknown";
                    CPUIcon = "CPU.ico";
                    break;
            }
        }
    }
}

