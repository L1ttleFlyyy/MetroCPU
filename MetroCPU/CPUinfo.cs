using System;
using System.Text;

namespace OpenLibSys
{
    class CPUinfo
    {
        private Ols _ols;
        public int test;
        public bool LoadSucceeded { get; }
        public bool SST_support { get; }
        public uint[,] cpuid = new uint[16, 4];
        public int MaxCPUIDind { get; }
        public uint[,] cpuid_ex = new uint[32, 4];
        public int MaxCPUIDexind { get; }
        public bool IsCpuid { get; }
        public string Vendor { get; }
        public string Manufacturer { get; }
        public readonly string ErrorMessage = "No error";

        public CPUinfo()
        {
            _ols = new Ols();
            uint res = _ols.GetStatus();
            if (res != 0)
            {
                ErrorMessage = ((Ols.Status)res).ToString()+"\n"+((Ols.OlsDllStatus)_ols.GetDllStatus()).ToString();
                LoadSucceeded = false;
            }
            else
            {
                LoadSucceeded = true;
                IsCpuid = _ols.IsCpuid() > 0;
                _getCPUID();
                MaxCPUIDind = (int)cpuid[0, 0];
                _getCPUIDex();
                MaxCPUIDexind = (int)(cpuid_ex[0, 0] - 0x80000000);
                Vendor = _getVendor();
                Manufacturer = _getManufacturer();
                SST_support = BitsSlicer(cpuid[6, 0], 7, 7) > 0;
            }
        }


        public bool SST_enabled
        {
            get
            {
                if (SST_support)
                {
                    uint eax = 0, edx = 0;
                    if (_ols.Rdmsr(0x770, ref eax, ref edx) > 0)
                        return eax > 0 ? true : false;
                    else
                        return false;
                }
                else
                    return false;
            }
            set
            {
                if (value)
                    test = _ols.Wrmsr(0x770, 1, 0);
                else
                    test = _ols.Wrmsr(0x770, 0, 0);
            }
        }

        public string _getManufacturer()
        {
            StringBuilder sb = new StringBuilder();
            uint[] ex = new uint[4] { 0, 0, 0, 0 };
            _ols.Cpuid(0, ref ex[0], ref ex[1], ref ex[2], ref ex[3]);
            foreach (uint i in new uint[] { ex[1], ex[3], ex[2] })
                sb.Append(ExToString(i));
            return sb.ToString();
        }

        private string _getVendor()
        {
            StringBuilder sb = new StringBuilder();
            uint[] ex = new uint[4] { 0, 0, 0, 0 };
            foreach (uint ind in new uint[] { 0x80000002, 0x80000003, 0x80000004 })
            {
                _ols.Cpuid(ind, ref ex[0], ref ex[1], ref ex[2], ref ex[3]);
                foreach (uint i in ex)
                    sb.Append(ExToString(i));
            }
            return sb.ToString();

        }

        private void _getCPUID()
        {
            cpuid[0, 0] = 0;
            cpuid[0, 1] = 0;
            cpuid[0, 2] = 0;
            cpuid[0, 3] = 0;
            if (IsCpuid)
            {
                _ols.Cpuid(0, ref cpuid[0, 0], ref cpuid[0, 1], ref cpuid[0, 2], ref cpuid[0, 3]);
            }
            int maxind = Math.Min((int)cpuid[0, 0], 0xf);
            if (maxind > 0)
            {
                for (uint tmp = 1; tmp <= maxind; tmp++)
                {
                    _ols.Cpuid(tmp, ref cpuid[tmp, 0], ref cpuid[tmp, 1], ref cpuid[tmp, 2], ref cpuid[tmp, 3]);
                }
            }
        }

        private void _getCPUIDex()
        {
            cpuid_ex[0, 0] = 0;
            cpuid_ex[0, 1] = 0;
            cpuid_ex[0, 2] = 0;
            cpuid_ex[0, 3] = 0;
            if (IsCpuid)
            {
                _ols.Cpuid(0x80000000, ref cpuid_ex[0, 0], ref cpuid_ex[0, 1], ref cpuid_ex[0, 2], ref cpuid_ex[0, 3]);
            }
            int maxind = Math.Min((int)(cpuid_ex[0, 0] - 0x80000000), 0x1f);

            if (maxind > 0)
            {
                for (uint tmp = 1; tmp <= maxind; tmp++)
                {
                    _ols.Cpuid(tmp + 0x80000000, ref cpuid_ex[tmp, 0], ref cpuid_ex[tmp, 1], ref cpuid_ex[tmp, 2], ref cpuid_ex[tmp, 3]);
                }
            }
        }

        public uint BitsSlicer(uint exx, int Highest, int Lowest)
        {
            if (Highest < Lowest || Highest > 31 || Highest < 0 || Lowest > 31 || Lowest < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            uint head, tail;
            tail = exx / (uint)Math.Pow(2, Lowest);
            head = (uint)(exx / (ulong)Math.Pow(2, Highest + 1) * (ulong)Math.Pow(2, Highest + 1) / (uint)Math.Pow(2, Lowest));
            return tail - head;
        }

        public string ToBinary(uint data, int bits)
        {
            if (bits > 31 || bits < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            return Convert.ToString(data, 2).PadLeft(bits, '0');
        }

        private static string ExToString(uint i)
        {
            byte[] bytes = new byte[4];
            for (int j = 0; j < 4; j++)
            {
                bytes[j] = (byte)(i / Math.Pow(256, j));
            }
            return Encoding.ASCII.GetString(bytes);
        }
        
    }
}
