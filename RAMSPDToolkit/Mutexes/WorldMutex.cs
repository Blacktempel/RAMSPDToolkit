/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Copyright (c) 2025 Florian K.
 *
 * Code inspiration, improvements and fixes are from, but not limited to, following projects:
 * LibreHardwareMonitor; Linux Kernel; OpenRGB; WinRing0 (QCute)
 */

using RAMSPDToolkit.I2CSMBus.Interop;
using RAMSPDToolkit.I2CSMBus.Interop.Shared;
using RAMSPDToolkit.I2CSMBus.Interop.Shared.Structures;
using RAMSPDToolkit.Logging;
using System.Runtime.InteropServices;
using System.Security;

using OS = RAMSPDToolkit.Software.OperatingSystem;

namespace RAMSPDToolkit.Mutexes
{
    /// <summary>
    /// Create / Open a mutex with appropiate protection.
    /// </summary>
    internal sealed class WorldMutex
    {
        #region Constructor

        public WorldMutex(string mutexName)
        {
            if (!OS.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }

            //Create mutex
            _WorldMutex = CreateWorldMutex(mutexName);

            LogSimple.LogTrace($"Created {nameof(WorldMutex)} with name '{MutexName}'.");
        }

        ~WorldMutex()
        {
            //Cleanup for Handle
            Kernel32.CloseHandle(_WorldMutex);
        }

        #endregion

        #region Constants

        internal const int SECURITY_DESCRIPTOR_REVISION = 1;
        internal const int SECURITY_WORLD_RID = 0x00;

        internal static readonly byte[] SECURITY_WORLD_SID_AUTHORITY = new byte[] { 0, 0, 0, 0, 0, 1 };

        internal const int ACL_REVISION = 2;

        internal const uint MUTANT_QUERY_STATE = 0x0001;
        internal const uint READ_CONTROL = 0x00020000;
        internal const uint SYNCHRONIZE = 0x00100000;
        internal const uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        internal const uint MUTANT_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | MUTANT_QUERY_STATE;

        #endregion

        #region Fields

        /// <summary>
        /// Mutex Handle.
        /// </summary>
        IntPtr _WorldMutex;

        #endregion

        #region Properties

        /// <summary>
        /// Name of internal mutex.
        /// </summary>
        public string MutexName { get; private set; }

        #endregion

        #region Public

        /// <summary>
        /// Lock the mutex.
        /// </summary>
        public void Lock()
        {
            if (_WorldMutex != IntPtr.Zero)
            {
                Kernel32.WaitForSingleObject(_WorldMutex, I2CConstants.INFINITE_TIME);
            }
        }

        /// <summary>
        /// Unlock the mutex.
        /// </summary>
        public void Unlock()
        {
            if (_WorldMutex != IntPtr.Zero)
            {
                Kernel32.ReleaseMutex(_WorldMutex);
            }
        }

        #endregion

        #region Private

        /// <summary>
        /// Create / Open a mutex with appropiate protection.
        /// </summary>
        /// <param name="name">Name of mutex to create.</param>
        [SecurityCritical]
        IntPtr CreateWorldMutex(string name)
        {
            //Setup security descriptor
            AdvApi32.InitializeSecurityDescriptor(out var sdb, SECURITY_DESCRIPTOR_REVISION);

            var swa = new SidIdentifierAuthority();
            swa.Value = SECURITY_WORLD_SID_AUTHORITY; //World access

            int aclSize = Marshal.SizeOf<ACL>() * 32;
            IntPtr acl = Marshal.AllocHGlobal(aclSize);

            if (AdvApi32.AllocateAndInitializeSid
                         (
                             ref swa,                            //SID identifier authority
                             1,                                  //Sub authority count
                             SECURITY_WORLD_RID, //Sub authority 0
                             0,                                  //Sub authority 1
                             0,                                  //Sub authority 2
                             0,                                  //Sub authority 3
                             0,                                  //Sub authority 4
                             0,                                  //Sub authority 5
                             0,                                  //Sub authority 6
                             0,                                  //Sub authority 7
                             out var sid                         //Returned SID
                         )
               &&
                AdvApi32.InitializeAcl(acl, (uint)aclSize, ACL_REVISION) //ACL setup OK and
               &&
                AdvApi32.AddAccessAllowedAce(acl, ACL_REVISION, MUTANT_ALL_ACCESS, sid) //ACE setup OK ?
               )
            {
                AdvApi32.SetSecurityDescriptorDacl(ref sdb, true, acl, false); //Yes, setup world access
            }
            else
            {
                AdvApi32.SetSecurityDescriptorDacl(ref sdb, true, IntPtr.Zero, false); //else setup with default
            }

            IntPtr sdbPtr = Marshal.AllocHGlobal(Marshal.SizeOf(sdb));
            Marshal.StructureToPtr(sdb, sdbPtr, false);

            var sab = new SECURITY_ATTRIBUTES(); //Setup security attributes block
            sab.nLength = (uint)Marshal.SizeOf(sdb);
            sab.bInheritHandle = false;
            sab.lpSecurityDescriptor = sdbPtr;

            var mutexName = $"Global\\{name}";

            IntPtr mutexHandle = IntPtr.Zero;

            if
            (
                (mutexHandle = Kernel32.CreateMutex(ref sab, false, mutexName)) != IntPtr.Zero || //Create / open with Global\ unprotected or
                (mutexHandle = Kernel32.OpenMutex(READ_CONTROL | //Open with Global\ protected or (probably Aquasuite)
                                                  MUTANT_QUERY_STATE |
                                                  SYNCHRONIZE, false, mutexName)) != IntPtr.Zero ||
                (mutexHandle = Kernel32.CreateMutex(ref sab, false, name)) != IntPtr.Zero //Create / open with no prefix unprotected ?
            )
            {
                MutexName = mutexName;
            }

            Marshal.FreeHGlobal(acl); //Free acl
            Marshal.FreeHGlobal(sdbPtr); //Free sbd

            if (sid != IntPtr.Zero) //Need to free the SID ?
            {
                AdvApi32.FreeSid(sid); //Yes, free it
            }

            return mutexHandle; //Return the handle
        }

        #endregion
    }
}
