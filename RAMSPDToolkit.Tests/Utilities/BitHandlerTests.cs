﻿using RAMSPDToolkit.Utilities;

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
    }
}
