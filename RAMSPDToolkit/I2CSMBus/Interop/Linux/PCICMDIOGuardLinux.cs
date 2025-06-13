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

using RAMSPDToolkit.PCI.Linux;
using RAMSPDToolkit.Utilities;

namespace RAMSPDToolkit.I2CSMBus.Interop.Shared
{
    /// <summary>
    /// Automatically enables and disables I/O for I801 and Piix4 SMBus controller via modifying PCICMD - PCI Command Register by using <see cref="IDisposable"/>.
    /// </summary>
    internal class PCICMDIOGuardLinux : IDisposable
    {
        #region Constructor

        public PCICMDIOGuardLinux(string pciConfigPath, ushort pciCMDOriginal)
        {
            _PCIConfigPath = pciConfigPath;
            _PCICMDOriginal = pciCMDOriginal;

            //I/O is not enabled
            if (!BitHandler.IsBitSet(pciCMDOriginal, SharedConstants.PCI_CMD_IO_BIT))
            {
                //Enable I/O
                _PCICMDModified = (ushort)(_PCICMDOriginal | SharedConstants.PCI_CMD_IO_BIT);
                PCIConfigAccessor.WritePCIConfig(_PCIConfigPath, SharedConstants.PCICMD, _PCICMDModified.Value);
            }
        }

        #endregion

        #region Fields

        readonly string _PCIConfigPath;
        readonly ushort _PCICMDOriginal;
        readonly ushort? _PCICMDModified;

        #endregion

        #region Public

        public void Dispose()
        {
            //Check if PCICMD was modified
            if (_PCICMDModified.HasValue)
            {
                //Reset back to original
                PCIConfigAccessor.WritePCIConfig(_PCIConfigPath, SharedConstants.PCICMD, _PCICMDOriginal);
            }
        }

        #endregion
    }
}
