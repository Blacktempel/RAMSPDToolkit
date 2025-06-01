﻿/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Copyright (c) 2025 Florian K.
 *
 * Code inspiration, improvements and fixes are from, but not limited to, following projects:
 * LibreHardwareMonitor; Linux Kernel; OpenRGB; WinRing0 (QCute)
 */

namespace RAMSPDToolkit.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="System.Runtime.InteropServices.Marshal"/>.<br/>
    /// </summary>
    /// <remarks>Implementations are based on Microsoft source code (referencesource).</remarks>
    public static class MarshalExtensions
    {
        /// <summary>
        /// Reads a 16-bit signed integer from unmanaged memory.
        /// </summary>
        /// <param name="ptr">The address in unmanaged memory from which to read.</param>
        /// <returns>The 16-bit signed integer read from unmanaged memory.</returns>
        /// <exception cref="AccessViolationException">ptr is not a recognized format. -or- ptr is null. -or- ptr is invalid.</exception>
        [System.Security.SecurityCritical]
        public static ushort ReadUInt16(IntPtr ptr)
        {
            return ReadUInt16(ptr, 0);
        }

        /// <summary>
        /// Reads a 16-bit signed integer at a given offset from unmanaged memory.
        /// </summary>
        /// <param name="ptr">The base address in unmanaged memory from which to read.</param>
        /// <param name="ofs">An additional byte offset, which is added to the ptr parameter before reading.</param>
        /// <returns>The 16-bit signed integer read from unmanaged memory at the given offset.</returns>
        /// <exception cref="AccessViolationException">Base address (ptr) plus offset byte (ofs) produces a null or invalid address.</exception>
        [System.Security.SecurityCritical]
        public static unsafe ushort ReadUInt16(IntPtr ptr, int ofs)
        {
            try
            {
                byte* addr = (byte*)ptr + ofs;
                if ((unchecked((int)addr) & 0x1) == 0)
                {
                    // aligned read
                    return *((ushort*)addr);
                }
                else
                {
                    // unaligned read
                    ushort val;
                    byte* valPtr = (byte*)&val;
                    valPtr[0] = addr[0];
                    valPtr[1] = addr[1];
                    return val;
                }
            }
            catch (NullReferenceException)
            {
                // this method is documented to throw AccessViolationException on any AV
                throw new AccessViolationException();
            }
        }

        /// <summary>
        /// Writes a 16-bit integer value to unmanaged memory.
        /// </summary>
        /// <param name="ptr">The address in unmanaged memory to write to.</param>
        /// <param name="val">The value to write.</param>
        /// <exception cref="AccessViolationException">ptr is not a recognized format. -or- ptr is null. -or- ptr is invalid.</exception>
        [System.Security.SecurityCritical]
        public static void WriteUInt16(IntPtr ptr, ushort val)
        {
            WriteUInt16(ptr, 0, val);
        }

        /// <summary>
        /// Writes a 16-bit signed integer value to unmanaged memory at a specified offset.
        /// </summary>
        /// <param name="ptr">The base address in the native heap to write to.</param>
        /// <param name="ofs">An additional byte offset, which is added to the ptr parameter before writing.</param>
        /// <param name="val">The value to write.</param>
        /// <exception cref="AccessViolationException">Base address (ptr) plus offset byte (ofs) produces a null or invalid address.</exception>
        [System.Security.SecurityCritical]
        public static unsafe void WriteUInt16(IntPtr ptr, int ofs, ushort val)
        {
            try
            {
                byte* addr = (byte*)ptr + ofs;
                if ((unchecked((int)addr) & 0x1) == 0)
                {
                    // aligned write
                    *((ushort*)addr) = val;
                }
                else
                {
                    // unaligned write
                    byte* valPtr = (byte*)&val;
                    addr[0] = valPtr[0];
                    addr[1] = valPtr[1];
                }
            }
            catch (NullReferenceException)
            {
                // this method is documented to throw AccessViolationException on any AV
                throw new AccessViolationException();
            }
        }
    }
}
