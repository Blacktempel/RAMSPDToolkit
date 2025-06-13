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

using RAMSPDToolkit.I2CSMBus.Interop.Shared.Structures;
using System.Runtime.InteropServices;

namespace RAMSPDToolkit.I2CSMBus.Interop.Shared
{
    internal static class AdvApi32
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool InitializeSecurityDescriptor(out SECURITY_DESCRIPTOR securityDescriptor, uint dwRevision);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool AllocateAndInitializeSid(
                ref SidIdentifierAuthority pIdentifierAuthority,
                byte nSubAuthorityCount,
                int dwSubAuthority0, int dwSubAuthority1,
                int dwSubAuthority2, int dwSubAuthority3,
                int dwSubAuthority4, int dwSubAuthority5,
                int dwSubAuthority6, int dwSubAuthority7,
                out IntPtr pSid);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool InitializeAcl(IntPtr acl, uint aclLength, uint aclRevision);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool AddAccessAllowedAce(IntPtr acl, uint dwAceRevision, uint AccessMask, IntPtr sid);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool SetSecurityDescriptorDacl(ref SECURITY_DESCRIPTOR sdb, bool bDaclPresent, IntPtr acl, bool bDaclDefaulted);

        [DllImport("advapi32.dll")]
        public static extern IntPtr FreeSid(IntPtr pSid);
    }
}
