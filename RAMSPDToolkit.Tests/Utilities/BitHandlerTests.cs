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

using RAMSPDToolkit.Utilities;

namespace RAMSPDToolkit.Tests.Utilities
{
    [TestClass]
    public class BitHandlerTests
    {
        [TestMethod]
        public void IsBitSetByte()
        {
            byte b = 128;

            Assert.IsTrue(BitHandler.IsBitSet(b, 7));
        }

        [TestMethod]
        public void IsBitSetWord()
        {
            const ushort w = 49152;

            Assert.IsTrue(BitHandler.IsBitSet(w, 14));
            Assert.IsTrue(BitHandler.IsBitSet(w, 15));
        }

        [TestMethod]
        public void SwapBits()
        {
            const byte b = 15;
            const byte expected = 240;

            Assert.AreEqual(expected, BitHandler.SwapBits(b));
        }

        [TestMethod]
        public void UnsetBit()
        {
            const byte b = 24;
            const byte expected = 8;

            Assert.AreEqual(expected, BitHandler.UnsetBit(b, 4));
        }

        [TestMethod]
        public void GetBits()
        {
            Assert.AreEqual(8, BitHandler.GetBits<sbyte>(-120, 0, 3));
            Assert.AreEqual(2, BitHandler.GetBits<byte>(82, 0, 1));

            Assert.AreEqual(8, BitHandler.GetBits<short>(-32000, 12, 15));
            Assert.AreEqual(190, BitHandler.GetBits<ushort>(48751, 8, 15));

            Assert.AreEqual(36832, BitHandler.GetBits<int>(-2140500000, 0, 15));
            Assert.AreEqual(15U, BitHandler.GetBits<uint>(4150123000, 28, 31));

            Assert.AreEqual(2, BitHandler.GetBits<long>(-7349082349183495411, 48, 55));
            Assert.AreEqual(19UL, BitHandler.GetBits<ulong>(11349082349183495411, 0, 4));
        }

        [TestMethod]
        public void ExtractBits()
        {
            Assert.AreEqual(7, BitHandler.ExtractBits<long>(-7349082349183495411, 0, 2, 3));
            Assert.AreEqual(15UL, BitHandler.ExtractBits<ulong>(11349082349183495411, 0, 1, 4, 5));
        }
    }
}
