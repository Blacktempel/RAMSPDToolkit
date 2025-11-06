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

using BlackSharp.Core.Extensions;
using RAMSPDToolkit.Logging;
using RAMSPDToolkit.SPD.Interop;

namespace RAMSPDToolkit.SPD.Timings
{
    /// <summary>
    /// Contains timings specified by JEDEC standard.
    /// </summary>
    public sealed class DDR5Timings
    {
        #region Constructor

        internal DDR5Timings(DDR5AccessorBase accessor)
        {
            _Accessor = accessor;

            ReadSPDTimings();
        }

        #endregion

        #region Fields

        readonly DDR5AccessorBase _Accessor;

        #endregion

        #region Properties

        /// <summary>
        /// The minimum cycle time of the SDRAM device (tCKAVGmin).
        /// </summary>
        public decimal MinimumCycleTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// The maximum cycle time of the SDRAM device (tCKAVGmax).
        /// </summary>
        public decimal MaximumCycleTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// List of supported CAS latencies.
        /// </summary>
        public List<int> CASLatenciesSupported { get; private set; } = new List<int>();

        /// <summary>
        /// Minimum CAS Latency time (tAAmin).
        /// </summary>
        public decimal MinimumCASLatencyTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum RAS to CAS delay time (tRCDmin).
        /// </summary>
        public decimal MinimumRASToCASDelayTime { get; private set; } = decimal.Zero;

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
        /// Minimum Write Recovery time (tWRmin).
        /// </summary>
        public decimal MinimumWriteRecoveryTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// Normal Refresh Recovery Time (tRFC1).
        /// </summary>
        public decimal NormalRefreshRecoveryTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// Fine Granularity Refresh Recovery Time (tRFC2).
        /// </summary>
        public decimal FineGranularityRefreshRecoveryTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// Same Bank Refresh Recovery Time (tRFCsb).
        /// </summary>
        public decimal SameBankRefreshRecoveryTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// Normal Refresh Recovery Time, 3DS Different Logical Rank (tRFC1_dlr).
        /// </summary>
        public decimal NormalRefreshRecoveryTime_3DSDifferentLogicalRank { get; private set; } = decimal.Zero;

        /// <summary>
        /// Fine Granularity Refresh Recovery Time (tRFC2_dlr).
        /// </summary>
        public decimal FineGranularityRefreshRecoveryTime_3DSDifferentLogicalRank { get; private set; } = decimal.Zero;

        /// <summary>
        /// Same Bank Refresh Recovery Time (tRFCsb_dlr).
        /// </summary>
        public decimal SameBankRefreshRecoveryTime_3DSDifferentLogicalRank { get; private set; } = decimal.Zero;

        #endregion

        #region Private

        void ReadSPDTimings()
        {
            LogSimple.LogTrace("");
            LogSimple.LogTrace("SPD Timings:");

            ReadtCKAVGMin();

            ReadtCKAVGMax();

            ReadSupportedCASLatencies();

            ReadtAAMin();

            ReadtRCDMin();

            ReadtRPMin();

            ReadtRASMin();

            ReadtRCMin();

            ReadtWRMin();

            ReadtRFC1();

            ReadtRFC2();

            ReadtRFCsb();

            ReadtRFC1_dlr();

            ReadtRFC2_dlr();

            ReadtRFCsb_dlr();

            LogSimple.LogTrace("");
        }

        byte At(ushort Address)
        {
            return _Accessor.At(Address);
        }

        void ReadtCKAVGMin()
        {
            //Read the minimum cycle time from the SPD EEPROM
            byte LSB = At(DDR5Constants.SPD_DDR5_MIN_CYCLE_TIME);
            byte MSB = At(DDR5Constants.SPD_DDR5_MIN_CYCLE_TIME + 1);

            MinimumCycleTime = (MSB << 8 | LSB) * DDR5Constants.SPD_DDR5_TIMEBASE_FTB;

            LogSimple.LogTrace($"{nameof(MinimumCycleTime)} (tCKAvgMin) = {MinimumCycleTime} ns ({nameof(LSB)}: 0x{LSB:X2} | {nameof(MSB)}: 0x{MSB:X2})");
        }

        void ReadtCKAVGMax()
        {
            //Read the maximum cycle time from the SPD EEPROM
            byte LSB = At(DDR5Constants.SPD_DDR5_MAX_CYCLE_TIME);
            byte MSB = At(DDR5Constants.SPD_DDR5_MAX_CYCLE_TIME + 1);

            MaximumCycleTime = (MSB << 8 | LSB) * DDR5Constants.SPD_DDR5_TIMEBASE_FTB;

            LogSimple.LogTrace($"{nameof(MaximumCycleTime)} (tCKAvgMax) = {MaximumCycleTime} ns ({nameof(LSB)}: 0x{LSB:X2} | {nameof(MSB)}: 0x{MSB:X2})");
        }

        void ReadSupportedCASLatencies()
        {
            //Read supported CAS Latencies
            ulong casLatencies = 0;

            var casLatenciesRange = DDR5Constants.SPD_DDR5_SUPPORTED_CAS_LATENCIES_END
                                  - DDR5Constants.SPD_DDR5_SUPPORTED_CAS_LATENCIES_START
                                  + 1;

            for (int i = 0; i < casLatenciesRange; ++i)
            {
                //Read the CAS Latencies byte
                byte casByte = At((ushort)(DDR5Constants.SPD_DDR5_SUPPORTED_CAS_LATENCIES_START + i));

                //We shift the byte into the correct position
                casLatencies |= (uint)(casByte << (i * 8));
            }

            LogSimple.LogTrace($"CAS Latencies raw value: {casLatencies} (0x{casLatencies:X16}).");

            //Clear CASLatencies
            CASLatenciesSupported.Clear();

            //Now we iterate over CASLatencies to fill the SupportedCASLatencies list
            for (int i = 0; i < 40; ++i)
            {
                if ((casLatencies & 0x1) != 0)
                {
                    var casLatencySupported = DDR5Constants.SPD_DDR5_LOWEST_SUPPORTED_CAS_LATENCY + i * 2;

                    CASLatenciesSupported.Add(casLatencySupported);
                }

                casLatencies >>= 1;

                if (casLatencies == 0)
                {
                    break; //Exit early when there's nothing left to process
                }
            }

            LogSimple.LogTrace($"{nameof(CASLatenciesSupported)}: {StringExtensions.Join(", ", CASLatenciesSupported)}.");
        }

        void ReadtAAMin()
        {
            //Read the minimum CAS Latency time (tAAmin)
            byte LSB = At(DDR5Constants.SPD_DDR5_MIN_CAS_LATENCY);
            byte MSB = At(DDR5Constants.SPD_DDR5_MIN_CAS_LATENCY + 1);

            MinimumCASLatencyTime = (MSB << 8 | LSB) * DDR5Constants.SPD_DDR5_TIMEBASE_FTB;

            LogSimple.LogTrace($"{nameof(MinimumCASLatencyTime)} (tAAmin) = {MinimumCASLatencyTime} ns ({nameof(LSB)}: 0x{LSB:X2} | {nameof(MSB)}: 0x{MSB:X2})");
        }

        void ReadtRCDMin()
        {
            //Read the minimum RAS to CAS delay time (tRCDmin)
            byte LSB = At(DDR5Constants.SPD_DDR5_MIN_RAS_TO_CAS_DELAY);
            byte MSB = At(DDR5Constants.SPD_DDR5_MIN_RAS_TO_CAS_DELAY + 1);

            MinimumRASToCASDelayTime = (MSB << 8 | LSB) * DDR5Constants.SPD_DDR5_TIMEBASE_FTB;

            LogSimple.LogTrace($"{nameof(MinimumRASToCASDelayTime)} (tRCDmin) = {MinimumRASToCASDelayTime} ns ({nameof(LSB)}: 0x{LSB:X2} | {nameof(MSB)}: 0x{MSB:X2})");
        }

        void ReadtRPMin()
        {
            //Read the minimum Row Precharge Delay time (tRPmin)
            byte LSB = At(DDR5Constants.SPD_DDR5_MIN_ROW_PRECHARGE_DELAY);
            byte MSB = At(DDR5Constants.SPD_DDR5_MIN_ROW_PRECHARGE_DELAY + 1);

            MinimumRowPrechargeDelayTime = (MSB << 8 | LSB) * DDR5Constants.SPD_DDR5_TIMEBASE_FTB;

            LogSimple.LogTrace($"{nameof(MinimumRowPrechargeDelayTime)} (tRPmin) = {MinimumRowPrechargeDelayTime} ns ({nameof(LSB)}: 0x{LSB:X2} | {nameof(MSB)}: 0x{MSB:X2})");
        }

        void ReadtRASMin()
        {
            //Read the minimum Active to Precharge Delay time (tRASmin)
            byte LSB = At(DDR5Constants.SPD_DDR5_MIN_ACTIVE_TO_PRECHARGE_DELAY);
            byte MSB = At(DDR5Constants.SPD_DDR5_MIN_ACTIVE_TO_PRECHARGE_DELAY + 1);

            MinimumActiveToPrechargeDelayTime = (MSB << 8 | LSB) * DDR5Constants.SPD_DDR5_TIMEBASE_FTB;

            LogSimple.LogTrace($"{nameof(MinimumActiveToPrechargeDelayTime)} (tRASmin) = {MinimumActiveToPrechargeDelayTime} ns ({nameof(LSB)}: 0x{LSB:X2} | {nameof(MSB)}: 0x{MSB:X2})");
        }

        void ReadtRCMin()
        {
            //Read the minimum Active to Active/Refresh Delay time (tRCmin)
            byte LSB = At(DDR5Constants.SPD_DDR5_MIN_ACTIVE_TO_ACTIVE_DELAY);
            byte MSB = At(DDR5Constants.SPD_DDR5_MIN_ACTIVE_TO_ACTIVE_DELAY + 1);

            MinimumActiveToActiveRefreshDelayTime = (MSB << 8 | LSB) * DDR5Constants.SPD_DDR5_TIMEBASE_FTB;

            LogSimple.LogTrace($"{nameof(MinimumActiveToActiveRefreshDelayTime)} (tRCmin) = {MinimumActiveToActiveRefreshDelayTime} ns ({nameof(LSB)}: 0x{LSB:X2} | {nameof(MSB)}: 0x{MSB:X2})");
        }

        void ReadtWRMin()
        {
            //Read the minimum Write Recovery time (tWRmin)
            byte LSB = At(DDR5Constants.SPD_DDR5_MIN_WRITE_RECOVERY_TIME);
            byte MSB = At(DDR5Constants.SPD_DDR5_MIN_WRITE_RECOVERY_TIME + 1);

            MinimumWriteRecoveryTime = (MSB << 8 | LSB) * DDR5Constants.SPD_DDR5_TIMEBASE_FTB;

            LogSimple.LogTrace($"{nameof(MinimumWriteRecoveryTime)} (tWRmin) = {MinimumWriteRecoveryTime} ns ({nameof(LSB)}: 0x{LSB:X2} | {nameof(MSB)}: 0x{MSB:X2})");
        }

        void ReadtRFC1()
        {
            //Read the Normal Refresh Recovery Time (tRFC1)
            byte LSB = At(DDR5Constants.SPD_DDR5_NORMAL_REFRESH_RECOVERY_TIME);
            byte MSB = At(DDR5Constants.SPD_DDR5_NORMAL_REFRESH_RECOVERY_TIME + 1);

            NormalRefreshRecoveryTime = (MSB << 8 | LSB);

            LogSimple.LogTrace($"{nameof(NormalRefreshRecoveryTime)} (tRFC1) = {NormalRefreshRecoveryTime} ns ({nameof(LSB)}: 0x{LSB:X2} | {nameof(MSB)}: 0x{MSB:X2})");
        }

        void ReadtRFC2()
        {
            //Read the Fine Granularity Refresh Recovery Time (tRFC2)
            byte LSB = At(DDR5Constants.SPD_DDR5_FINE_GRANULARITY_REFRESH_RECOVERY_TIME);
            byte MSB = At(DDR5Constants.SPD_DDR5_FINE_GRANULARITY_REFRESH_RECOVERY_TIME + 1);

            FineGranularityRefreshRecoveryTime = (MSB << 8 | LSB);

            LogSimple.LogTrace($"{nameof(FineGranularityRefreshRecoveryTime)} (tRFC2) = {FineGranularityRefreshRecoveryTime} ns ({nameof(LSB)}: 0x{LSB:X2} | {nameof(MSB)}: 0x{MSB:X2})");
        }

        void ReadtRFCsb()
        {
            //Read the Same Bank Refresh Recovery Time (tRFCsb)
            byte LSB = At(DDR5Constants.SPD_DDR5_SAME_BANK_REFRESH_RECOVERY_TIME);
            byte MSB = At(DDR5Constants.SPD_DDR5_SAME_BANK_REFRESH_RECOVERY_TIME + 1);

            SameBankRefreshRecoveryTime = (MSB << 8 | LSB);

            LogSimple.LogTrace($"{nameof(SameBankRefreshRecoveryTime)} (tRFCsb) = {SameBankRefreshRecoveryTime} ns ({nameof(LSB)}: 0x{LSB:X2} | {nameof(MSB)}: 0x{MSB:X2})");
        }

        void ReadtRFC1_dlr()
        {
            //Normal Refresh Recovery Time, 3DS Different Logical Rank (tRFC1_dlr)
            byte LSB = At(DDR5Constants.SPD_DDR5_NORMAL_REFRESH_RECOVERY_TIME_DIFFRANK);
            byte MSB = At(DDR5Constants.SPD_DDR5_NORMAL_REFRESH_RECOVERY_TIME_DIFFRANK + 1);

            NormalRefreshRecoveryTime_3DSDifferentLogicalRank = (MSB << 8 | LSB);

            LogSimple.LogTrace($"{nameof(NormalRefreshRecoveryTime_3DSDifferentLogicalRank)} (tRFC1_dlr) = {NormalRefreshRecoveryTime_3DSDifferentLogicalRank} ns ({nameof(LSB)}: 0x{LSB:X2} | {nameof(MSB)}: 0x{MSB:X2})");
        }

        void ReadtRFC2_dlr()
        {
            //Fine Granularity Refresh Recovery Time (tRFC2_dlr)
            byte LSB = At(DDR5Constants.SPD_DDR5_FINE_GRANULARITY_REFRESH_RECOVERY_TIME_DIFFRANK);
            byte MSB = At(DDR5Constants.SPD_DDR5_FINE_GRANULARITY_REFRESH_RECOVERY_TIME_DIFFRANK + 1);

            FineGranularityRefreshRecoveryTime_3DSDifferentLogicalRank = (MSB << 8 | LSB);

            LogSimple.LogTrace($"{nameof(FineGranularityRefreshRecoveryTime_3DSDifferentLogicalRank)} (tRFC2_dlr) = {FineGranularityRefreshRecoveryTime_3DSDifferentLogicalRank} ns ({nameof(LSB)}: 0x{LSB:X2} | {nameof(MSB)}: 0x{MSB:X2})");
        }

        void ReadtRFCsb_dlr()
        {
            //Same Bank Refresh Recovery Time (tRFCsb_dlr)
            byte LSB = At(DDR5Constants.SPD_DDR5_SAME_BANK_REFRESH_RECOVERY_TIME_DIFFRANK);
            byte MSB = At(DDR5Constants.SPD_DDR5_SAME_BANK_REFRESH_RECOVERY_TIME_DIFFRANK + 1);

            SameBankRefreshRecoveryTime_3DSDifferentLogicalRank = (MSB << 8 | LSB);

            LogSimple.LogTrace($"{nameof(SameBankRefreshRecoveryTime_3DSDifferentLogicalRank)} (tRFCsb_dlr) = {SameBankRefreshRecoveryTime_3DSDifferentLogicalRank} ns ({nameof(LSB)}: 0x{LSB:X2} | {nameof(MSB)}: 0x{MSB:X2})");
        }

        #endregion
    }
}
