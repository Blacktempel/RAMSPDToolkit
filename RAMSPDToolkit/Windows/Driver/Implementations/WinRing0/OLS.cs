//-----------------------------------------------------------------------------
//     Author : hiyohiyo
//       Mail : hiyohiyo@crystalmark.info
//        Web : http://openlibsys.org/
//    License : The modified BSD license
//
//                     Copyright 2007-2009 OpenLibSys.org. All rights reserved.
//-----------------------------------------------------------------------------
// This is support library for WinRing0 1.3.x.
//
// Code adjustments and additions by Florian K.

#if !RELEASE_NDD

using RAMSPDToolkit.Logging;
using RAMSPDToolkit.Windows.Driver.Implementations.WinRing0.Enums;
using RAMSPDToolkit.Windows.Driver.Interfaces;
using RAMSPDToolkit.Windows.Interop;
using RAMSPDToolkit.Windows.Utilities;

namespace RAMSPDToolkit.Windows.Driver.Implementations.WinRing0
{
    /// <summary>
    /// Driver access class. Extracts driver and handles driver interaction.
    /// </summary>
    internal sealed class OLS : IDisposable, IWinRing0Driver
    {
        #region Constructor

        public OLS()
        {
            if (!ExtractDriver())
            {
                LogSimple.LogError($"{nameof(OLS)} {nameof(ExtractDriver)} failed - OLS Status {_Status}.");
                return;
            }

            if (!LoadLibraryFunctions())
            {
                LogSimple.LogError($"{nameof(OLS)} {nameof(LoadLibraryFunctions)} failed - OLS Status {_Status}.");
                return;
            }

            if (InitializeOls() == 0)
            {
                _Status = OLSStatus.DLL_INITIALIZE_ERROR;
                LogSimple.LogError($"{nameof(OLS)} {nameof(InitializeOls)} failed - OLS Status {_Status} | DLL Status {GetDllStatus()}.");
            }
        }

        ~OLS()
        {
            Dispose();
        }

        #endregion

        #region Fields

        bool _Disposed;

        IntPtr _Module;

        OLSStatus _Status;

        #region Delegates


        //-----------------------------------------------------------------------------
        // DLL Information
        //-----------------------------------------------------------------------------
        public delegate uint _GetDllStatus();
        public delegate uint _GetDllVersion(ref byte major, ref byte minor, ref byte revision, ref byte release);
        public delegate uint _GetDriverVersion(ref byte major, ref byte minor, ref byte revision, ref byte release);
        public delegate uint _GetDriverType();

        public delegate int _InitializeOls();
        public delegate void _DeinitializeOls();

        public _GetDllStatus GetDllStatus;
        public _GetDriverType GetDriverType;
        public _GetDllVersion GetDllVersion;
        public _GetDriverVersion GetDriverVersion;

        public _InitializeOls InitializeOls;
        public _DeinitializeOls DeinitializeOls;

        //-----------------------------------------------------------------------------
        // CPU
        //-----------------------------------------------------------------------------
        public delegate bool _IsCpuid();
        public delegate bool _IsMsr();
        public delegate bool _IsTsc();
        public delegate bool _Hlt();
        public delegate bool _HltTx(UIntPtr threadAffinityMask);
        public delegate bool _HltPx(UIntPtr processAffinityMask);
        public delegate bool _Rdmsr(uint index, ref uint eax, ref uint edx);
        public delegate bool _RdmsrTx(uint index, ref uint eax, ref uint edx, UIntPtr threadAffinityMask);
        public delegate bool _RdmsrPx(uint index, ref uint eax, ref uint edx, UIntPtr processAffinityMask);
        public delegate bool _Wrmsr(uint index, uint eax, uint edx);
        public delegate bool _WrmsrTx(uint index, uint eax, uint edx, UIntPtr threadAffinityMask);
        public delegate bool _WrmsrPx(uint index, uint eax, uint edx, UIntPtr processAffinityMask);
        public delegate bool _Rdpmc(uint index, ref uint eax, ref uint edx);
        public delegate bool _RdpmcTx(uint index, ref uint eax, ref uint edx, UIntPtr threadAffinityMask);
        public delegate bool _RdpmcPx(uint index, ref uint eax, ref uint edx, UIntPtr processAffinityMask);
        public delegate bool _Cpuid(uint index, ref uint eax, ref uint ebx, ref uint ecx, ref uint edx);
        public delegate bool _CpuidTx(uint index, ref uint eax, ref uint ebx, ref uint ecx, ref uint edx, UIntPtr threadAffinityMask);
        public delegate bool _CpuidPx(uint index, ref uint eax, ref uint ebx, ref uint ecx, ref uint edx, UIntPtr processAffinityMask);
        public delegate bool _Rdtsc(ref uint eax, ref uint edx);
        public delegate bool _RdtscTx(ref uint eax, ref uint edx, UIntPtr threadAffinityMask);
        public delegate bool _RdtscPx(ref uint eax, ref uint edx, UIntPtr processAffinityMask);

        public _IsCpuid IsCpuid;
        public _IsMsr IsMsr;
        public _IsTsc IsTsc;
        public _Hlt Hlt;
        public _HltTx HltTx;
        public _HltPx HltPx;
        public _Rdmsr Rdmsr;
        public _RdmsrTx RdmsrTx;
        public _RdmsrPx RdmsrPx;
        public _Wrmsr Wrmsr;
        public _WrmsrTx WrmsrTx;
        public _WrmsrPx WrmsrPx;
        public _Rdpmc Rdpmc;
        public _RdpmcTx RdpmcTx;
        public _RdpmcPx RdpmcPx;
        public _Cpuid Cpuid;
        public _CpuidTx CpuidTx;
        public _CpuidPx CpuidPx;
        public _Rdtsc Rdtsc;
        public _RdtscTx RdtscTx;
        public _RdtscPx RdtscPx;

        //-----------------------------------------------------------------------------
        // I/O
        //-----------------------------------------------------------------------------
        public delegate byte _ReadIoPortByte(ushort port);
        public delegate ushort _ReadIoPortWord(ushort port);
        public delegate uint _ReadIoPortDword(ushort port);
        public _ReadIoPortByte ReadIoPortByte;
        public _ReadIoPortWord ReadIoPortWord;
        public _ReadIoPortDword ReadIoPortDword;

        public delegate bool _ReadIoPortByteEx(ushort port, ref byte value);
        public delegate bool _ReadIoPortWordEx(ushort port, ref ushort value);
        public delegate bool _ReadIoPortDwordEx(ushort port, ref uint value);
        public _ReadIoPortByteEx ReadIoPortByteEx;
        public _ReadIoPortWordEx ReadIoPortWordEx;
        public _ReadIoPortDwordEx ReadIoPortDwordEx;

        public delegate void _WriteIoPortByte(ushort port, byte value);
        public delegate void _WriteIoPortWord(ushort port, ushort value);
        public delegate void _WriteIoPortDword(ushort port, uint value);
        public _WriteIoPortByte WriteIoPortByte;
        public _WriteIoPortWord WriteIoPortWord;
        public _WriteIoPortDword WriteIoPortDword;

        public delegate bool _WriteIoPortByteEx(ushort port, byte value);
        public delegate bool _WriteIoPortWordEx(ushort port, ushort value);
        public delegate bool _WriteIoPortDwordEx(ushort port, uint value);
        public _WriteIoPortByteEx WriteIoPortByteEx;
        public _WriteIoPortWordEx WriteIoPortWordEx;
        public _WriteIoPortDwordEx WriteIoPortDwordEx;

        //-----------------------------------------------------------------------------
        // PCI
        //-----------------------------------------------------------------------------
        public delegate void _SetPciMaxBusIndex(byte max);
        public _SetPciMaxBusIndex SetPciMaxBusIndex;

        public delegate byte _ReadPciConfigByte(uint pciAddress, byte regAddress);
        public delegate ushort _ReadPciConfigWord(uint pciAddress, byte regAddress);
        public delegate uint _ReadPciConfigDword(uint pciAddress, byte regAddress);
        public _ReadPciConfigByte ReadPciConfigByte;
        public _ReadPciConfigWord ReadPciConfigWord;
        public _ReadPciConfigDword ReadPciConfigDword;

        public delegate bool _ReadPciConfigByteEx(uint pciAddress, uint regAddress, ref byte value);
        public delegate bool _ReadPciConfigWordEx(uint pciAddress, uint regAddress, ref ushort value);
        public delegate bool _ReadPciConfigDwordEx(uint pciAddress, uint regAddress, ref uint value);
        public _ReadPciConfigByteEx ReadPciConfigByteEx;
        public _ReadPciConfigWordEx ReadPciConfigWordEx;
        public _ReadPciConfigDwordEx ReadPciConfigDwordEx;

        public delegate void _WritePciConfigByte(uint pciAddress, byte regAddress, byte value);
        public delegate void _WritePciConfigWord(uint pciAddress, byte regAddress, ushort value);
        public delegate void _WritePciConfigDword(uint pciAddress, byte regAddress, uint value);
        public _WritePciConfigByte WritePciConfigByte;
        public _WritePciConfigWord WritePciConfigWord;
        public _WritePciConfigDword WritePciConfigDword;

        public delegate bool _WritePciConfigByteEx(uint pciAddress, uint regAddress, byte value);
        public delegate bool _WritePciConfigWordEx(uint pciAddress, uint regAddress, ushort value);
        public delegate bool _WritePciConfigDwordEx(uint pciAddress, uint regAddress, uint value);
        public _WritePciConfigByteEx WritePciConfigByteEx;
        public _WritePciConfigWordEx WritePciConfigWordEx;
        public _WritePciConfigDwordEx WritePciConfigDwordEx;

        public delegate uint _FindPciDeviceById(ushort vendorId, ushort deviceId, byte index);
        public delegate uint _FindPciDeviceByClass(byte baseClass, byte subClass, byte programIf, byte index);
        public _FindPciDeviceById FindPciDeviceById;
        public _FindPciDeviceByClass FindPciDeviceByClass;

        //-----------------------------------------------------------------------------
        // Physical Memory (unsafe)
        //-----------------------------------------------------------------------------
#if _PHYSICAL_MEMORY_SUPPORT
        public unsafe delegate uint _ReadDmiMemory(byte* buffer, uint count, uint unitSize);
        public _ReadDmiMemory ReadDmiMemory;

        public unsafe delegate uint _ReadPhysicalMemory(UIntPtr address, byte* buffer, uint count, uint unitSize);
        public unsafe delegate uint _WritePhysicalMemory(UIntPtr address, byte* buffer, uint count, uint unitSize);

        public _ReadPhysicalMemory ReadPhysicalMemory;
        public _WritePhysicalMemory WritePhysicalMemory;
#endif

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// File path of driver DLL file.
        /// </summary>
        public string FilePathDLL { get; private set; }

        /// <summary>
        /// File path of driver SYS file.
        /// </summary>
        public string FilePathSYS { get; private set; }

        /// <summary>
        /// Current <see cref="Enums.OLSStatus"/>.
        /// </summary>
        public OLSStatus OLSStatus
        {
            get { return _Status; }
        }

        /// <summary>
        /// Boolean value to indicate whether driver is open and loaded.
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return _Module != IntPtr.Zero
                    && OLSStatus == OLSStatus.NO_ERROR;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_Disposed)
            {
                if (_Module != IntPtr.Zero)
                {
                    DeinitializeOls();
                    Kernel32.FreeLibrary(_Module);
                    _Module = IntPtr.Zero;

                    File.Delete(FilePathDLL);
                    File.Delete(FilePathSYS);
                }

                _Disposed = true;
            }
        }

        #endregion

        #region IGenericDriver

        bool IDriver.Load()
        {
            return OLSStatus == OLSStatus.NO_ERROR
                && GetDllStatus() == 0;
        }

        void IDriver.Unload()
        {
            Dispose();
        }

        byte IGenericDriver.ReadIoPortByte(ushort port)
        {
            return ReadIoPortByte(port);
        }

        ushort IGenericDriver.ReadIoPortWord(ushort port)
        {
            return ReadIoPortWord(port);
        }

        uint IGenericDriver.ReadIoPortDword(ushort port)
        {
            return ReadIoPortDword(port);
        }

        bool IGenericDriver.ReadIoPortByteEx(ushort port, ref byte value)
        {
            return ReadIoPortByteEx(port, ref value);
        }

        bool IGenericDriver.ReadIoPortWordEx(ushort port, ref ushort value)
        {
            return ReadIoPortWordEx(port, ref value);
        }

        bool IGenericDriver.ReadIoPortDwordEx(ushort port, ref uint value)
        {
            return ReadIoPortDwordEx(port, ref value);
        }

        void IGenericDriver.WriteIoPortByte(ushort port, byte value)
        {
            WriteIoPortByte(port, value);
        }

        void IGenericDriver.WriteIoPortWord(ushort port, ushort value)
        {
            WriteIoPortWord(port, value);
        }

        void IGenericDriver.WriteIoPortDword(ushort port, uint value)
        {
            WriteIoPortDword(port, value);
        }

        bool IGenericDriver.WriteIoPortByteEx(ushort port, byte value)
        {
            return WriteIoPortByteEx(port, value);
        }

        bool IGenericDriver.WriteIoPortWordEx(ushort port, ushort value)
        {
            return WriteIoPortWordEx(port, value);
        }

        bool IGenericDriver.WriteIoPortDwordEx(ushort port, uint value)
        {
            return WriteIoPortDwordEx(port, value);
        }

        uint IGenericDriver.FindPciDeviceById(ushort vendorId, ushort deviceId, byte index)
        {
            return FindPciDeviceById(vendorId, deviceId, index);
        }

        uint IGenericDriver.FindPciDeviceByClass(byte baseClass, byte subClass, byte programIf, byte index)
        {
            return FindPciDeviceByClass(baseClass, subClass, programIf, index);
        }

        byte IGenericDriver.ReadPciConfigByte(uint pciAddress, byte regAddress)
        {
            return ReadPciConfigByte(pciAddress, regAddress);
        }

        ushort IGenericDriver.ReadPciConfigWord(uint pciAddress, byte regAddress)
        {
            return ReadPciConfigWord(pciAddress, regAddress);
        }

        uint IGenericDriver.ReadPciConfigDword(uint pciAddress, byte regAddress)
        {
            return ReadPciConfigDword(pciAddress, regAddress);
        }

        bool IGenericDriver.ReadPciConfigByteEx(uint pciAddress, uint regAddress, ref byte value)
        {
            return ReadPciConfigByteEx(pciAddress, regAddress, ref value);
        }

        bool IGenericDriver.ReadPciConfigWordEx(uint pciAddress, uint regAddress, ref ushort value)
        {
            return ReadPciConfigWordEx(pciAddress, regAddress, ref value);
        }

        bool IGenericDriver.ReadPciConfigDwordEx(uint pciAddress, uint regAddress, ref uint value)
        {
            return ReadPciConfigDwordEx(pciAddress, regAddress, ref value);
        }

        void IGenericDriver.WritePciConfigByte(uint pciAddress, byte regAddress, byte value)
        {
            WritePciConfigByte(pciAddress, regAddress, value);
        }

        void IGenericDriver.WritePciConfigWord(uint pciAddress, byte regAddress, ushort value)
        {
            WritePciConfigWord(pciAddress, regAddress, value);
        }

        void IGenericDriver.WritePciConfigDword(uint pciAddress, byte regAddress, uint value)
        {
            WritePciConfigDword(pciAddress, regAddress, value);
        }

        bool IGenericDriver.WritePciConfigByteEx(uint pciAddress, uint regAddress, byte value)
        {
            return WritePciConfigByteEx(pciAddress, regAddress, value);
        }

        bool IGenericDriver.WritePciConfigWordEx(uint pciAddress, uint regAddress, ushort value)
        {
            return WritePciConfigWordEx(pciAddress, regAddress, value);
        }

        bool IGenericDriver.WritePciConfigDwordEx(uint pciAddress, uint regAddress, uint value)
        {
            return WritePciConfigDwordEx(pciAddress, regAddress, value);
        }

        #endregion

        #region Public

        // PCI Device Address to Bus Number
        public uint PciGetBus(uint address)
        {
            return ((address >> 8) & 0xFF);
        }

        // PCI Device Address to Device Number
        public uint PciGetDev(uint address)
        {
            return ((address >> 3) & 0x1F);
        }

        // PCI Device Address to Function Number
        public uint PciGetFunc(uint address)
        {
            return (address & 7);
        }

        #endregion

        #region Private

        bool ExtractDriver()
        {
            //Check for Windows
            if (!Software.OperatingSystem.IsWindows())
            {
                return false;
            }

            string driverFileName;

            //Check for 32-bit process
            if (IntPtr.Size == 4)
            {
                driverFileName = OLSConstants.DriverFileName32Bit;
            }
            else //64-bit
            {
                driverFileName = OLSConstants.DriverFileName64Bit;
            }

            //Extract sys driver
            FilePathSYS = Path.ChangeExtension(driverFileName, ".sys");

            var resourceFileSYS = Environment.Is64BitOperatingSystem ? "WinRing0x64.sys.gz" : "WinRing0.sys.gz";
            if (!ResourceFileExtractor.ExtractResourceFileToFilePath(FilePathSYS, resourceFileSYS))
            {
                _Status = OLSStatus.DLL_RESOURCE_NOT_FOUND;
                return false;
            }

            //Extract dll driver
            FilePathDLL = Path.ChangeExtension(driverFileName, ".dll");

            var resourceFileDLL = Environment.Is64BitOperatingSystem ? "WinRing0x64.dll.gz" : "WinRing0.dll.gz";
            if (!ResourceFileExtractor.ExtractResourceFileToFilePath(FilePathDLL, resourceFileDLL))
            {
                _Status = OLSStatus.DLL_RESOURCE_NOT_FOUND;
                return false;
            }

            return true;
        }

        bool LoadLibraryFunctions()
        {
            if (!File.Exists(FilePathDLL))
            {
                return false;
            }

            _Module = Kernel32.LoadLibrary(FilePathDLL);

            if (_Module == IntPtr.Zero)
            {
                _Status = OLSStatus.DLL_NOT_FOUND;
                return false;
            }

            GetDllStatus     = DynamicLoader.GetDelegate<_GetDllStatus    >(_Module, "GetDllStatus"    );
            GetDllVersion    = DynamicLoader.GetDelegate<_GetDllVersion   >(_Module, "GetDllVersion"   );
            GetDriverVersion = DynamicLoader.GetDelegate<_GetDriverVersion>(_Module, "GetDriverVersion");
            GetDriverType    = DynamicLoader.GetDelegate<_GetDriverType   >(_Module, "GetDriverType"   );

            InitializeOls   = DynamicLoader.GetDelegate<_InitializeOls  >(_Module, "InitializeOls"  );
            DeinitializeOls = DynamicLoader.GetDelegate<_DeinitializeOls>(_Module, "DeinitializeOls");

            IsCpuid = DynamicLoader.GetDelegate<_IsCpuid>(_Module, "IsCpuid");
            IsMsr   = DynamicLoader.GetDelegate<_IsMsr  >(_Module, "IsMsr"  );
            IsTsc   = DynamicLoader.GetDelegate<_IsTsc  >(_Module, "IsTsc"  );
            Hlt     = DynamicLoader.GetDelegate<_Hlt    >(_Module, "Hlt"    );
            HltTx   = DynamicLoader.GetDelegate<_HltTx  >(_Module, "HltTx"  );
            HltPx   = DynamicLoader.GetDelegate<_HltPx  >(_Module, "HltPx"  );
            Rdmsr   = DynamicLoader.GetDelegate<_Rdmsr  >(_Module, "Rdmsr"  );
            RdmsrTx = DynamicLoader.GetDelegate<_RdmsrTx>(_Module, "RdmsrTx");
            RdmsrPx = DynamicLoader.GetDelegate<_RdmsrPx>(_Module, "RdmsrPx");
            Wrmsr   = DynamicLoader.GetDelegate<_Wrmsr  >(_Module, "Wrmsr"  );
            WrmsrTx = DynamicLoader.GetDelegate<_WrmsrTx>(_Module, "WrmsrTx");
            WrmsrPx = DynamicLoader.GetDelegate<_WrmsrPx>(_Module, "WrmsrPx");
            Rdpmc   = DynamicLoader.GetDelegate<_Rdpmc  >(_Module, "Rdpmc"  );
            RdpmcTx = DynamicLoader.GetDelegate<_RdpmcTx>(_Module, "RdpmcTx");
            RdpmcPx = DynamicLoader.GetDelegate<_RdpmcPx>(_Module, "RdpmcPx");
            Cpuid   = DynamicLoader.GetDelegate<_Cpuid  >(_Module, "Cpuid"  );
            CpuidTx = DynamicLoader.GetDelegate<_CpuidTx>(_Module, "CpuidTx");
            CpuidPx = DynamicLoader.GetDelegate<_CpuidPx>(_Module, "CpuidPx");
            Rdtsc   = DynamicLoader.GetDelegate<_Rdtsc  >(_Module, "Rdtsc"  );
            RdtscTx = DynamicLoader.GetDelegate<_RdtscTx>(_Module, "RdtscTx");
            RdtscPx = DynamicLoader.GetDelegate<_RdtscPx>(_Module, "RdtscPx");

            ReadIoPortByte    = DynamicLoader.GetDelegate<_ReadIoPortByte   >(_Module, "ReadIoPortByte"   );
            ReadIoPortWord    = DynamicLoader.GetDelegate<_ReadIoPortWord   >(_Module, "ReadIoPortWord"   );
            ReadIoPortDword   = DynamicLoader.GetDelegate<_ReadIoPortDword  >(_Module, "ReadIoPortDword"  );
            ReadIoPortByteEx  = DynamicLoader.GetDelegate<_ReadIoPortByteEx >(_Module, "ReadIoPortByteEx" );
            ReadIoPortWordEx  = DynamicLoader.GetDelegate<_ReadIoPortWordEx >(_Module, "ReadIoPortWordEx" );
            ReadIoPortDwordEx = DynamicLoader.GetDelegate<_ReadIoPortDwordEx>(_Module, "ReadIoPortDwordEx");

            WriteIoPortByte    = DynamicLoader.GetDelegate<_WriteIoPortByte   >(_Module, "WriteIoPortByte"   );
            WriteIoPortWord    = DynamicLoader.GetDelegate<_WriteIoPortWord   >(_Module, "WriteIoPortWord"   );
            WriteIoPortDword   = DynamicLoader.GetDelegate<_WriteIoPortDword  >(_Module, "WriteIoPortDword"  );
            WriteIoPortByteEx  = DynamicLoader.GetDelegate<_WriteIoPortByteEx >(_Module, "WriteIoPortByteEx" );
            WriteIoPortWordEx  = DynamicLoader.GetDelegate<_WriteIoPortWordEx >(_Module, "WriteIoPortWordEx" );
            WriteIoPortDwordEx = DynamicLoader.GetDelegate<_WriteIoPortDwordEx>(_Module, "WriteIoPortDwordEx");

            SetPciMaxBusIndex     = DynamicLoader.GetDelegate<_SetPciMaxBusIndex    >(_Module, "SetPciMaxBusIndex"    );
            ReadPciConfigByte     = DynamicLoader.GetDelegate<_ReadPciConfigByte    >(_Module, "ReadPciConfigByte"    );
            ReadPciConfigWord     = DynamicLoader.GetDelegate<_ReadPciConfigWord    >(_Module, "ReadPciConfigWord"    );
            ReadPciConfigDword    = DynamicLoader.GetDelegate<_ReadPciConfigDword   >(_Module, "ReadPciConfigDword"   );
            ReadPciConfigByteEx   = DynamicLoader.GetDelegate<_ReadPciConfigByteEx  >(_Module, "ReadPciConfigByteEx"  );
            ReadPciConfigWordEx   = DynamicLoader.GetDelegate<_ReadPciConfigWordEx  >(_Module, "ReadPciConfigWordEx"  );
            ReadPciConfigDwordEx  = DynamicLoader.GetDelegate<_ReadPciConfigDwordEx >(_Module, "ReadPciConfigDwordEx" );
            WritePciConfigByte    = DynamicLoader.GetDelegate<_WritePciConfigByte   >(_Module, "WritePciConfigByte"   );
            WritePciConfigWord    = DynamicLoader.GetDelegate<_WritePciConfigWord   >(_Module, "WritePciConfigWord"   );
            WritePciConfigDword   = DynamicLoader.GetDelegate<_WritePciConfigDword  >(_Module, "WritePciConfigDword"  );
            WritePciConfigByteEx  = DynamicLoader.GetDelegate<_WritePciConfigByteEx >(_Module, "WritePciConfigByteEx" );
            WritePciConfigWordEx  = DynamicLoader.GetDelegate<_WritePciConfigWordEx >(_Module, "WritePciConfigWordEx" );
            WritePciConfigDwordEx = DynamicLoader.GetDelegate<_WritePciConfigDwordEx>(_Module, "WritePciConfigDwordEx");
            FindPciDeviceById     = DynamicLoader.GetDelegate<_FindPciDeviceById    >(_Module, "FindPciDeviceById"    );
            FindPciDeviceByClass  = DynamicLoader.GetDelegate<_FindPciDeviceByClass >(_Module, "FindPciDeviceByClass" );

#if _PHYSICAL_MEMORY_SUPPORT
            ReadDmiMemory       = DynamicLoader.GetDelegate<_ReadDmiMemory      >(_Module, "ReadDmiMemory"      );
            ReadPhysicalMemory  = DynamicLoader.GetDelegate<_ReadPhysicalMemory >(_Module, "ReadPhysicalMemory" );
            WritePhysicalMemory = DynamicLoader.GetDelegate<_WritePhysicalMemory>(_Module, "WritePhysicalMemory");
#endif

            if (AreLoadedFunctionsValid())
            {
                return true;
            }
            else
            {
                _Status = OLSStatus.DLL_INCORRECT_VERSION;

                return false;
            }
        }

        bool AreLoadedFunctionsValid()
        {
            return    GetDllStatus     != null
                   && GetDllVersion    != null
                   && GetDriverVersion != null
                   && GetDriverType    != null

                   && InitializeOls   != null
                   && DeinitializeOls != null

                   && IsCpuid != null
                   && IsMsr   != null
                   && IsTsc   != null
                   && Hlt     != null
                   && HltTx   != null
                   && HltPx   != null
                   && Rdmsr   != null
                   && RdmsrTx != null
                   && RdmsrPx != null
                   && Wrmsr   != null
                   && WrmsrTx != null
                   && WrmsrPx != null
                   && Rdpmc   != null
                   && RdpmcTx != null
                   && RdpmcPx != null
                   && Cpuid   != null
                   && CpuidTx != null
                   && CpuidPx != null
                   && Rdtsc   != null
                   && RdtscTx != null
                   && RdtscPx != null

                   && ReadIoPortByte    != null
                   && ReadIoPortWord    != null
                   && ReadIoPortDword   != null
                   && ReadIoPortByteEx  != null
                   && ReadIoPortWordEx  != null
                   && ReadIoPortDwordEx != null

                   && WriteIoPortByte    != null
                   && WriteIoPortWord    != null
                   && WriteIoPortDword   != null
                   && WriteIoPortByteEx  != null
                   && WriteIoPortWordEx  != null
                   && WriteIoPortDwordEx != null

                   && SetPciMaxBusIndex     != null
                   && ReadPciConfigByte     != null
                   && ReadPciConfigWord     != null
                   && ReadPciConfigDword    != null
                   && ReadPciConfigByteEx   != null
                   && ReadPciConfigWordEx   != null
                   && ReadPciConfigDwordEx  != null
                   && WritePciConfigByte    != null
                   && WritePciConfigWord    != null
                   && WritePciConfigDword   != null
                   && WritePciConfigByteEx  != null
                   && WritePciConfigWordEx  != null
                   && WritePciConfigDwordEx != null
                   && FindPciDeviceById     != null
                   && FindPciDeviceByClass  != null

#if _PHYSICAL_MEMORY_SUPPORT
                   && ReadDmiMemory       != null
                   && ReadPhysicalMemory  != null
                   && WritePhysicalMemory != null
#endif
                   ;
        }

        #endregion
    }
}

#endif
