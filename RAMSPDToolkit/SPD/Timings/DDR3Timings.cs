/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * Copyright (c) 2026 Florian K.
 *
 * Code inspiration, improvements and fixes are from, but not limited to, following projects:
 * LibreHardwareMonitor; Linux Kernel; OpenRGB; WinRing0 (QCute)
 */

using BlackSharp.Core.Extensions;
using RAMSPDToolkit.Logging;
using RAMSPDToolkit.SPD.Interop;

namespace RAMSPDToolkit.SPD.Timings
{
    /// <summary>
    /// Contains DDR3 timings specified by JEDEC standard.
    /// </summary>
    public sealed class DDR3Timings
    {
        #region Constructor

        internal DDR3Timings(DDR3AccessorBase accessor)
        {
            _Accessor = accessor;

            ReadSPDTimings();
        }

        #endregion

        #region Fields

        readonly DDR3AccessorBase _Accessor;

        #endregion

        #region Properties

        /// <summary>
        /// Medium time base in nanoseconds.
        /// </summary>
        public decimal MediumTimeBase { get; private set; } = decimal.Zero;

        /// <summary>
        /// Fine time base in nanoseconds.
        /// </summary>
        public decimal FineTimeBase { get; private set; } = decimal.Zero;

        /// <summary>
        /// The minimum cycle time of the SDRAM device (tCKmin).
        /// </summary>
        public decimal MinimumCycleTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// List of supported CAS latencies.
        /// </summary>
        public List<int> CASLatenciesSupported { get; private set; } = new List<int>();

        /// <summary>
        /// Minimum CAS Latency time (tAAmin).
        /// </summary>
        public decimal MinimumCASLatencyTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum Write Recovery time (tWRmin).
        /// </summary>
        public decimal MinimumWriteRecoveryTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum RAS to CAS delay time (tRCDmin).
        /// </summary>
        public decimal MinimumRASToCASDelayTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum Activate to Activate delay time (tRRDmin).
        /// </summary>
        public decimal MinimumActivateToActivateDelayTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum Row Precharge Delay time (tRPmin).
        /// </summary>
        public decimal MinimumRowPrechargeDelayTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum Active to Precharge Delay time (tRASmin).
        /// </summary>
        public decimal MinimumActiveToPrechargeDelayTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum Active to Active/Refresh Delay time (tRCmin).
        /// </summary>
        public decimal MinimumActiveToActiveRefreshDelayTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum Refresh Recovery Delay time (tRFCmin).
        /// </summary>
        public decimal MinimumRefreshRecoveryDelayTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum Write to Read time (tWTRmin).
        /// </summary>
        public decimal MinimumWriteToReadTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum Read to Precharge time (tRTPmin).
        /// </summary>
        public decimal MinimumReadToPrechargeTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum Four Activate Window time (tFAWmin).
        /// </summary>
        public decimal MinimumFourActivateWindowTime { get; private set; } = decimal.Zero;

        #endregion

        #region Private

        void ReadSPDTimings()
        {
            LogSimple.LogTrace("");
            LogSimple.LogTrace("SPD Timings:");

            if (!ReadTimeBases())
            {
                LogSimple.LogTrace("DDR3 SPD time base data is invalid.");
                LogSimple.LogTrace("");
                return;
            }

            ReadtCKMin();
            ReadSupportedCASLatencies();
            ReadtAAMin();
            ReadtWRMin();
            ReadtRCDMin();
            ReadtRRDMin();
            ReadtRPMin();
            ReadtRASMin();
            ReadtRCMin();
            ReadtRFCMin();
            ReadtWTRMin();
            ReadtRTPMin();
            ReadtFAWMin();

            LogSimple.LogTrace("");
        }

        byte At(ushort address)
        {
            return _Accessor.At(address);
        }

        bool ReadTimeBases()
        {
            byte fineTimeBase         = At(DDR3Constants.SPD_DDR3_FINE_TIMEBASE_DIVIDEND_DIVISOR);
            byte fineTimeBaseDividend = (byte)(fineTimeBase >> 4);
            byte fineTimeBaseDivisor  = (byte)(fineTimeBase & 0x0F);

            byte mediumTimeBaseDividend = At(DDR3Constants.SPD_DDR3_MEDIUM_TIMEBASE_DIVIDEND);
            byte mediumTimeBaseDivisor  = At(DDR3Constants.SPD_DDR3_MEDIUM_TIMEBASE_DIVISOR);

            if (fineTimeBaseDivisor == 0 || mediumTimeBaseDivisor == 0)
            {
                return false;
            }

            FineTimeBase   = fineTimeBaseDividend / (decimal)fineTimeBaseDivisor / 1000M;
            MediumTimeBase = mediumTimeBaseDividend / (decimal)mediumTimeBaseDivisor;

            LogSimple.LogTrace($"{nameof(MediumTimeBase)} = {MediumTimeBase} ns.");
            LogSimple.LogTrace($"{nameof(FineTimeBase  )} = {FineTimeBase  } ns.");

            return true;
        }

        decimal CalculateTiming(byte mediumTimeBaseValue)
        {
            return mediumTimeBaseValue * MediumTimeBase;
        }

        decimal CalculateTiming(byte mediumTimeBaseValue, ushort fineTimeBaseAddress)
        {
            int fineTimeBaseValue = unchecked((sbyte)At(fineTimeBaseAddress));

            return mediumTimeBaseValue * MediumTimeBase + fineTimeBaseValue * FineTimeBase;
        }

        decimal CalculateTiming(ushort mediumTimeBaseValue)
        {
            return mediumTimeBaseValue * MediumTimeBase;
        }

        void ReadtCKMin()
        {
            byte mediumTimeBaseValue = At(DDR3Constants.SPD_DDR3_MIN_CYCLE_TIME_MTB);

            MinimumCycleTime = CalculateTiming(mediumTimeBaseValue, DDR3Constants.SPD_DDR3_MIN_CYCLE_TIME_FTB);

            LogSimple.LogTrace($"{nameof(MinimumCycleTime)} (tCKmin) = {MinimumCycleTime} ns.");
        }

        void ReadSupportedCASLatencies()
        {
            ushort casLatencies = (ushort)((At(DDR3Constants.SPD_DDR3_SUPPORTED_CAS_LATENCIES_MSB) << 8)
                                         | At(DDR3Constants.SPD_DDR3_SUPPORTED_CAS_LATENCIES_LSB));

            CASLatenciesSupported.Clear();

            for (int i = 0; i < DDR3Constants.SPD_DDR3_CAS_LATENCY_BITS; ++i)
            {
                if ((casLatencies & (1 << i)) != 0)
                {
                    CASLatenciesSupported.Add(DDR3Constants.SPD_DDR3_LOWEST_CAS_LATENCY + i);
                }
            }

            LogSimple.LogTrace($"{nameof(CASLatenciesSupported)}: {StringExtensions.Join(", ", CASLatenciesSupported)}.");
        }

        void ReadtAAMin()
        {
            byte mediumTimeBaseValue = At(DDR3Constants.SPD_DDR3_MIN_CAS_LATENCY_MTB);

            MinimumCASLatencyTime = CalculateTiming(mediumTimeBaseValue, DDR3Constants.SPD_DDR3_MIN_CAS_LATENCY_FTB);

            LogSimple.LogTrace($"{nameof(MinimumCASLatencyTime)} (tAAmin) = {MinimumCASLatencyTime} ns.");
        }

        void ReadtWRMin()
        {
            MinimumWriteRecoveryTime = CalculateTiming(At(DDR3Constants.SPD_DDR3_MIN_WRITE_RECOVERY_TIME_MTB));

            LogSimple.LogTrace($"{nameof(MinimumWriteRecoveryTime)} (tWRmin) = {MinimumWriteRecoveryTime} ns.");
        }

        void ReadtRCDMin()
        {
            byte mediumTimeBaseValue = At(DDR3Constants.SPD_DDR3_MIN_RAS_TO_CAS_DELAY_MTB);

            MinimumRASToCASDelayTime = CalculateTiming(mediumTimeBaseValue, DDR3Constants.SPD_DDR3_MIN_RAS_TO_CAS_DELAY_FTB);

            LogSimple.LogTrace($"{nameof(MinimumRASToCASDelayTime)} (tRCDmin) = {MinimumRASToCASDelayTime} ns.");
        }

        void ReadtRRDMin()
        {
            MinimumActivateToActivateDelayTime = CalculateTiming(At(DDR3Constants.SPD_DDR3_MIN_ACTIVATE_TO_ACTIVATE_DELAY_MTB));

            LogSimple.LogTrace($"{nameof(MinimumActivateToActivateDelayTime)} (tRRDmin) = {MinimumActivateToActivateDelayTime} ns.");
        }

        void ReadtRPMin()
        {
            byte mediumTimeBaseValue = At(DDR3Constants.SPD_DDR3_MIN_ROW_PRECHARGE_DELAY_MTB);

            MinimumRowPrechargeDelayTime = CalculateTiming(mediumTimeBaseValue, DDR3Constants.SPD_DDR3_MIN_ROW_PRECHARGE_DELAY_FTB);

            LogSimple.LogTrace($"{nameof(MinimumRowPrechargeDelayTime)} (tRPmin) = {MinimumRowPrechargeDelayTime} ns.");
        }

        void ReadtRASMin()
        {
            ushort mediumTimeBaseValue = (ushort)(((At(DDR3Constants.SPD_DDR3_tRAS_AND_tRC_UPPER_NIBBLES) & 0x0F) << 8)
                                                | At(DDR3Constants.SPD_DDR3_MIN_ACTIVE_TO_PRECHARGE_DELAY_MTB));

            MinimumActiveToPrechargeDelayTime = CalculateTiming(mediumTimeBaseValue);

            LogSimple.LogTrace($"{nameof(MinimumActiveToPrechargeDelayTime)} (tRASmin) = {MinimumActiveToPrechargeDelayTime} ns.");
        }

        void ReadtRCMin()
        {
            ushort mediumTimeBaseValue = (ushort)(((At(DDR3Constants.SPD_DDR3_tRAS_AND_tRC_UPPER_NIBBLES) & 0xF0) << 4)
                                                | At(DDR3Constants.SPD_DDR3_MIN_ACTIVE_TO_ACTIVE_DELAY_MTB));
            int fineTimeBaseValue = unchecked((sbyte)At(DDR3Constants.SPD_DDR3_MIN_ACTIVE_TO_ACTIVE_DELAY_FTB));

            MinimumActiveToActiveRefreshDelayTime = mediumTimeBaseValue * MediumTimeBase + fineTimeBaseValue * FineTimeBase;

            LogSimple.LogTrace($"{nameof(MinimumActiveToActiveRefreshDelayTime)} (tRCmin) = {MinimumActiveToActiveRefreshDelayTime} ns.");
        }

        void ReadtRFCMin()
        {
            ushort mediumTimeBaseValue = (ushort)((At(DDR3Constants.SPD_DDR3_MIN_REFRESH_RECOVERY_DELAY_MSB) << 8)
                                                | At(DDR3Constants.SPD_DDR3_MIN_REFRESH_RECOVERY_DELAY_LSB));

            MinimumRefreshRecoveryDelayTime = CalculateTiming(mediumTimeBaseValue);

            LogSimple.LogTrace($"{nameof(MinimumRefreshRecoveryDelayTime)} (tRFCmin) = {MinimumRefreshRecoveryDelayTime} ns.");
        }

        void ReadtWTRMin()
        {
            MinimumWriteToReadTime = CalculateTiming(At(DDR3Constants.SPD_DDR3_MIN_WRITE_TO_READ_DELAY_MTB));

            LogSimple.LogTrace($"{nameof(MinimumWriteToReadTime)} (tWTRmin) = {MinimumWriteToReadTime} ns.");
        }

        void ReadtRTPMin()
        {
            MinimumReadToPrechargeTime = CalculateTiming(At(DDR3Constants.SPD_DDR3_MIN_READ_TO_PRECHARGE_MTB));

            LogSimple.LogTrace($"{nameof(MinimumReadToPrechargeTime)} (tRTPmin) = {MinimumReadToPrechargeTime} ns.");
        }

        void ReadtFAWMin()
        {
            ushort mediumTimeBaseValue = (ushort)(((At(DDR3Constants.SPD_DDR3_MIN_FOUR_ACTIVATE_UPPER_NIBBLE) & 0x0F) << 8)
                                                | At(DDR3Constants.SPD_DDR3_MIN_FOUR_ACTIVATE_WINDOW_DELAY_MTB));

            MinimumFourActivateWindowTime = CalculateTiming(mediumTimeBaseValue);

            LogSimple.LogTrace($"{nameof(MinimumFourActivateWindowTime)} (tFAWmin) = {MinimumFourActivateWindowTime} ns.");
        }

        #endregion
    }
}
