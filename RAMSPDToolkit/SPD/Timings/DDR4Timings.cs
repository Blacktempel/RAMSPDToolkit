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
    public sealed class DDR4Timings
    {
        #region Constructor

        internal DDR4Timings(DDR4AccessorBase accessor)
        {
            _Accessor = accessor;

            ReadSPDTimings();
        }

        #endregion

        #region Fields

        readonly DDR4AccessorBase _Accessor;

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
        /// Minimum Refresh Recovery Delay time 1 (tRFC1min).
        /// </summary>
        public decimal MinimumRefreshRecoveryDelayTime1 { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum Refresh Recovery Delay time 2 (tRFC2min).
        /// </summary>
        public decimal MinimumRefreshRecoveryDelayTime2 { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum Refresh Recovery Delay time 4 (tRFC4min).
        /// </summary>
        public decimal MinimumRefreshRecoveryDelayTime4 { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum Four Activate Window time (tFAWmin).
        /// </summary>
        public decimal MinimumFourActivateWindowTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum Activate to Activate Delate time (tRRD_smin) different bank group.
        /// </summary>
        public decimal MinimumActivateToActivateDelay_DiffGroup { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum Activate to Activate Delate time (tRRD_lmin) same bank group.
        /// </summary>
        public decimal MinimumActivateToActivateDelay_SameGroup { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum CAS to CAS Delay time (tCCD_lmin) same bank group.
        /// </summary>
        public decimal MinimumCASToCASDelay_SameGroup { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum Write Recovery time (tWRmin).
        /// </summary>
        public decimal MinimumWriteRecoveryTime { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum Write to Read Time (tWTR_smin) different bank group.
        /// </summary>
        public decimal MinimumWriteToReadTime_DiffGroup { get; private set; } = decimal.Zero;

        /// <summary>
        /// Minimum Write to Read Time (tWTR_lmin) same bank group.
        /// </summary>
        public decimal MinimumWriteToReadTime_SameGroup { get; private set; } = decimal.Zero;

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

            ReadtRFC1Min();

            ReadtRFC2Min();

            ReadtRFC4Min();

            ReadtFAWMin();

            ReadtRRD_smin();

            ReadtRRD_lmin();

            ReadtCCD_lmin();

            ReadtWRMin();

            ReadtWTR_smin();

            ReadtWTR_lmin();

            LogSimple.LogTrace("");
        }

        byte At(ushort Address)
        {
            return _Accessor.At(Address);
        }

        void ReadtCKAVGMin()
        {
            //Read the minimum cycle time from the SPD EEPROM
            byte MTB = At(DDR4Constants.SPD_DDR4_MIN_CYCLE_TIME_MTB);
            int FTB = unchecked((sbyte)At(DDR4Constants.SPD_DDR4_MIN_CYCLE_TIME_FTB));

            MinimumCycleTime = MTB * DDR4Constants.SPD_DDR4_TIMEBASE_MTB + FTB * DDR4Constants.SPD_DDR4_TIMEBASE_FTB;

            LogSimple.LogTrace($"{nameof(MinimumCycleTime)} (tckAVGmin) = {MinimumCycleTime} ns ({nameof(MTB)}: 0x{MTB:X2} | {nameof(FTB)}: 0x{FTB:X4})");
        }

        void ReadtCKAVGMax()
        {
            //Read the maximum cycle time from the SPD EEPROM
            byte MTB = At(DDR4Constants.SPD_DDR4_MAX_CYCLE_TIME_MTB);
            int FTB = unchecked((sbyte)At(DDR4Constants.SPD_DDR4_MAX_CYCLE_TIME_FTB));

            MaximumCycleTime = MTB * DDR4Constants.SPD_DDR4_TIMEBASE_MTB + FTB * DDR4Constants.SPD_DDR4_TIMEBASE_FTB;

            LogSimple.LogTrace($"{nameof(MaximumCycleTime)} (tckAVGmax) = {MaximumCycleTime} ns ({nameof(MTB)}: 0x{MTB:X2} | {nameof(FTB)}: 0x{FTB:X4})");
        }

        void ReadSupportedCASLatencies()
        {
            //Read supported CAS Latencies
            //First read the fourth byte as it states the CAS Latencies range
            uint casLatencies = 0;
            int lowestCASLatency = 0;

            var casLatenciesRange = DDR4Constants.SPD_DDR4_SUPPORTED_CAS_LATENCIES_END
                                  - DDR4Constants.SPD_DDR4_SUPPORTED_CAS_LATENCIES_START
                                  + 1;

            for (int i = 0; i < casLatenciesRange; ++i)
            {
                //Read the CAS Latencies byte
                byte casByte = At((ushort)(DDR4Constants.SPD_DDR4_SUPPORTED_CAS_LATENCIES_START + i));

                //For the first three bytes, we shift the byte into the correct position
                if (i < 3)
                {
                    //Shift the byte into the correct position
                    casLatencies |= (uint)(casByte << (i * 8));
                }
                else //For the fourth byte, we verify the CAS latencies range and shift into position
                {
                    //Or into CASLatencies, but only the first 6 bits
                    casLatencies |= (uint)((casByte & 0x3F) << (i * 8));

                    //Check if the 7th bit is set
                    //When the bit 7 of the 4th byte is cleared, the range of latencies is 7-36
                    //When the bit 7 of the 4th byte is set, the range of latencies is 23-52
                    lowestCASLatency = ((casByte & 0x80) == 0) ? 7 : 23;
                }
            }

            LogSimple.LogTrace($"CAS Latencies raw value: 0x{casLatencies:X8}");

            //Clear CASLatencies
            CASLatenciesSupported.Clear();

            //Now we iterate over CASLatencies to fill the SupportedCASLatencies list
            for (int i = 0; i < 30; ++i)
            {
                if ((casLatencies & 0x1) != 0)
                {
                    var casLatencySupported = lowestCASLatency + i;

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
            //Read minimum CAS Latency time from the SPD EEPROM
            byte MTB = At(DDR4Constants.SPD_DDR4_MIN_CAS_LATENCY_MTB);
            int FTB = unchecked((sbyte)At(DDR4Constants.SPD_DDR4_MIN_CAS_LATENCY_FTB));

            MinimumCASLatencyTime = MTB * DDR4Constants.SPD_DDR4_TIMEBASE_MTB + FTB * DDR4Constants.SPD_DDR4_TIMEBASE_FTB;

            LogSimple.LogTrace($"{nameof(MinimumCASLatencyTime)} (tAAmin) = {MinimumCASLatencyTime} ns ({nameof(MTB)}: 0x{MTB:X2} | {nameof(FTB)}: 0x{FTB:X4})");
        }

        void ReadtRCDMin()
        {
            //Read minimum RAS to CAS delay time from the SPD EEPROM
            byte MTB = At(DDR4Constants.SPD_DDR4_MIN_RAS_TO_CAS_DELAY_MTB);
            int FTB = unchecked((sbyte)At(DDR4Constants.SPD_DDR4_MIN_RAS_TO_CAS_DELAY_FTB));

            MinimumRASToCASDelayTime = MTB * DDR4Constants.SPD_DDR4_TIMEBASE_MTB + FTB * DDR4Constants.SPD_DDR4_TIMEBASE_FTB;

            LogSimple.LogTrace($"{nameof(MinimumRASToCASDelayTime)} (tRCDmin) = {MinimumRASToCASDelayTime} ns ({nameof(MTB)}: 0x{MTB:X2} | {nameof(FTB)}: 0x{FTB:X4})");
        }

        void ReadtRPMin()
        {
            //Read minimum Row Precharge Delay time from the SPD EEPROM
            byte MTB = At(DDR4Constants.SPD_DDR4_MIN_ROW_PRECHARGE_DELAY_MTB);
            int FTB = unchecked((sbyte)At(DDR4Constants.SPD_DDR4_MIN_ROW_PRECHARGE_DELAY_FTB));

            MinimumRowPrechargeDelayTime = MTB * DDR4Constants.SPD_DDR4_TIMEBASE_MTB + FTB * DDR4Constants.SPD_DDR4_TIMEBASE_FTB;

            LogSimple.LogTrace($"{nameof(MinimumRowPrechargeDelayTime)} (tRPmin) = {MinimumRowPrechargeDelayTime} ns ({nameof(MTB)}: 0x{MTB:X2} | {nameof(FTB)}: 0x{FTB:X4})");
        }

        void ReadtRASMin()
        {
            //Read minimum Active to Precharge Delay time from the SPD EEPROM
            ushort MTB_long = (ushort)(((At(DDR4Constants.SPD_DDR4_tRAS_AND_tRC_UPPER_NIBBLES) & 0x0F) << 8) | At(DDR4Constants.SPD_DDR4_MIN_ACTIVE_TO_PRECHARGE_DELAY_MTB));

            MinimumActiveToPrechargeDelayTime = MTB_long * DDR4Constants.SPD_DDR4_TIMEBASE_MTB;

            LogSimple.LogTrace($"{nameof(MinimumActiveToPrechargeDelayTime)} (tRASmin) = {MinimumActiveToPrechargeDelayTime} ns ({nameof(MTB_long)}: 0x{MTB_long:X4})");
        }

        void ReadtRCMin()
        {
            // Read minimum Active to Active/Refresh Delay time from the SPD EEPROM
            ushort MTB_long = (ushort)(((At(DDR4Constants.SPD_DDR4_tRAS_AND_tRC_UPPER_NIBBLES) << 4) & 0xF00) | At(DDR4Constants.SPD_DDR4_MIN_ACTIVE_TO_ACTIVE_DELAY_MTB));
            int FTB = unchecked((sbyte)At(DDR4Constants.SPD_DDR4_MIN_ACTIVE_TO_ACTIVE_DELAY_FTB));

            MinimumActiveToActiveRefreshDelayTime = MTB_long * DDR4Constants.SPD_DDR4_TIMEBASE_MTB + FTB * DDR4Constants.SPD_DDR4_TIMEBASE_FTB;

            LogSimple.LogTrace($"{nameof(MinimumActiveToActiveRefreshDelayTime)} (tRCmin) = {MinimumActiveToActiveRefreshDelayTime} ns ({nameof(MTB_long)}: 0x{MTB_long:X4} | {nameof(FTB)}: 0x{FTB:X4})");
        }

        void ReadtRFC1Min()
        {
            //Read minimum Refresh Recovery Delay time 1 (tRFC1min) from the SPD EEPROM
            ushort MTB_long = (ushort)((At(DDR4Constants.SPD_DDR4_MIN_REFRESH_RECOVERY_DELAY1_MSB) << 8) | At(DDR4Constants.SPD_DDR4_MIN_REFRESH_RECOVERY_DELAY1_LSB));

            MinimumRefreshRecoveryDelayTime1 = MTB_long * DDR4Constants.SPD_DDR4_TIMEBASE_MTB;

            LogSimple.LogTrace($"{nameof(MinimumRefreshRecoveryDelayTime1)} (tRFC1min) = {MinimumRefreshRecoveryDelayTime1} ns ({nameof(MTB_long)}: 0x{MTB_long:X4})");
        }

        void ReadtRFC2Min()
        {
            //Read minimum Refresh Recovery Delay time 2 (tRFC2min) from the SPD EEPROM
            ushort MTB_long = (ushort)((At(DDR4Constants.SPD_DDR4_MIN_REFRESH_RECOVERY_DELAY2_MSB) << 8) | At(DDR4Constants.SPD_DDR4_MIN_REFRESH_RECOVERY_DELAY2_LSB));

            MinimumRefreshRecoveryDelayTime2 = MTB_long * DDR4Constants.SPD_DDR4_TIMEBASE_MTB;

            LogSimple.LogTrace($"{nameof(MinimumRefreshRecoveryDelayTime2)} (tRFC2min) = {MinimumRefreshRecoveryDelayTime2} ns ({nameof(MTB_long)}: 0x{MTB_long:X4})");
        }

        void ReadtRFC4Min()
        {
            //Read minimum Refresh Recovery Delay time 4 (tRFC4min) from the SPD EEPROM
            ushort MTB_long = (ushort)((At(DDR4Constants.SPD_DDR4_MIN_REFRESH_RECOVERY_DELAY4_MSB) << 8) | At(DDR4Constants.SPD_DDR4_MIN_REFRESH_RECOVERY_DELAY4_LSB));

            MinimumRefreshRecoveryDelayTime4 = MTB_long * DDR4Constants.SPD_DDR4_TIMEBASE_MTB;

            LogSimple.LogTrace($"{nameof(MinimumRefreshRecoveryDelayTime4)} (tRFC4min) = {MinimumRefreshRecoveryDelayTime4} ns ({nameof(MTB_long)}: 0x{MTB_long:X4})");
        }

        void ReadtFAWMin()
        {
            //Read minimum Four Activate Window time (tFAWmin) from the SPD EEPROM
            ushort MTB_long = (ushort)((At(DDR4Constants.SPD_DDR4_MIN_FOUR_ACTIVATE_UPPER_NIBBLE) << 8) | At(DDR4Constants.SPD_DDR4_MIN_FOUR_ACTIVATE_WINDOW_DELAY_MTB));

            MinimumFourActivateWindowTime = MTB_long * DDR4Constants.SPD_DDR4_TIMEBASE_MTB;

            LogSimple.LogTrace($"{nameof(MinimumFourActivateWindowTime)} (tFAWmin) = {MinimumFourActivateWindowTime} ns ({nameof(MTB_long)}: 0x{MTB_long:X4})");
        }

        void ReadtRRD_smin()
        {
            //Read minimum Activate to Activate Delay time (tRRD_smin) different bank group from the SPD EEPROM
            byte MTB = At(DDR4Constants.SPD_DDR4_ACTIVATE_TO_ACTIVATE_DELAY_DIFF_MTB);
            int FTB = unchecked((sbyte)At(DDR4Constants.SPD_DDR4_ACTIVATE_TO_ACTIVATE_DELAY_DIFF_FTB));

            MinimumActivateToActivateDelay_DiffGroup = MTB * DDR4Constants.SPD_DDR4_TIMEBASE_MTB + FTB * DDR4Constants.SPD_DDR4_TIMEBASE_FTB;

            LogSimple.LogTrace($"{nameof(MinimumActivateToActivateDelay_DiffGroup)} (tRRD_smin) = {MinimumActivateToActivateDelay_DiffGroup} ns ({nameof(MTB)}: 0x{MTB:X2} | {nameof(FTB)}: 0x{FTB:X4})");
        }

        void ReadtRRD_lmin()
        {
            //Read minimum Activate to Activate Delay time (tRRD_lmin) same bank group from the SPD EEPROM
            byte MTB = At(DDR4Constants.SPD_DDR4_ACTIVATE_TO_ACTIVATE_DELAY_SAME_MTB);
            int FTB = unchecked((sbyte)At(DDR4Constants.SPD_DDR4_ACTIVATE_TO_ACTIVATE_DELAY_SAME_FTB));

            MinimumActivateToActivateDelay_SameGroup = MTB * DDR4Constants.SPD_DDR4_TIMEBASE_MTB + FTB * DDR4Constants.SPD_DDR4_TIMEBASE_FTB;

            LogSimple.LogTrace($"{nameof(MinimumActivateToActivateDelay_SameGroup)} (tRRD_lmin) = {MinimumActivateToActivateDelay_SameGroup} ns ({nameof(MTB)}: 0x{MTB:X2} | {nameof(FTB)}: 0x{FTB:X4})");
        }

        void ReadtCCD_lmin()
        {
            //Read minimum CAS to CAS Delay time (tCCD_lmin) same bank group from the SPD EEPROM
            byte MTB = At(DDR4Constants.SPD_DDR4_CAS_TO_CAS_DELAY_SAME_MTB);
            int FTB = unchecked((sbyte)At(DDR4Constants.SPD_DDR4_CAS_TO_CAS_DELAY_SAME_FTB));

            MinimumCASToCASDelay_SameGroup = MTB * DDR4Constants.SPD_DDR4_TIMEBASE_MTB + FTB * DDR4Constants.SPD_DDR4_TIMEBASE_FTB;

            LogSimple.LogTrace($"{nameof(MinimumCASToCASDelay_SameGroup)} (tCCD_lmin) = {MinimumCASToCASDelay_SameGroup} ns ({nameof(MTB)}: 0x{MTB:X2} | {nameof(FTB)}: 0x{FTB:X4})");
        }

        void ReadtWRMin()
        {
            //Read minimum Write Recovery time (tWRmin) from the SPD EEPROM
            ushort MTB_long = (ushort)((At(DDR4Constants.SPD_DDR4_MINIMUM_WRITE_RECOVERY_UPPER_NIBBLE) << 8) | At(DDR4Constants.SPD_DDR4_MINIMUM_WRITE_RECOVERY_TIME_MTB));

            MinimumWriteRecoveryTime = MTB_long * DDR4Constants.SPD_DDR4_TIMEBASE_MTB;

            LogSimple.LogTrace($"{nameof(MinimumWriteRecoveryTime)} (tWRmin) = {MinimumWriteRecoveryTime} ns ({nameof(MTB_long)}: 0x{MTB_long:X4})");
        }

        void ReadtWTR_smin()
        {
            //Read minimum Write to Read Time (tWTR_smin) different bank group from the SPD EEPROM
            ushort MTB_long = (ushort)(((At(DDR4Constants.SPD_DDR4_MINIMUM_WRITE_TO_READ_DELAY_UPPER_NIBBLES) << 8) & 0xF) | At(DDR4Constants.SPD_DDR4_MINIMUM_WRITE_TO_READ_DELAY_DIFF_MTB));

            MinimumWriteToReadTime_DiffGroup = MTB_long * DDR4Constants.SPD_DDR4_TIMEBASE_MTB;

            LogSimple.LogTrace($"{nameof(MinimumWriteToReadTime_DiffGroup)} (tWTR_smin) = {MinimumWriteToReadTime_DiffGroup} ns ({nameof(MTB_long)}: 0x{MTB_long:X4})");
        }

        void ReadtWTR_lmin()
        {
            //Read minimum Write to Read Time (tWTR_lmin) same bank group from the SPD EEPROM
            ushort MTB_long = (ushort)(((At(DDR4Constants.SPD_DDR4_MINIMUM_WRITE_TO_READ_DELAY_UPPER_NIBBLES) << 4) & 0xF) | At(DDR4Constants.SPD_DDR4_MINIMUM_WRITE_TO_READ_DELAY_SAME_MTB));

            MinimumWriteToReadTime_SameGroup = MTB_long * DDR4Constants.SPD_DDR4_TIMEBASE_MTB;

            LogSimple.LogTrace($"{nameof(MinimumWriteToReadTime_SameGroup)} (tWTR_lmin) = {MinimumWriteToReadTime_SameGroup} ns ({nameof(MTB_long)}: 0x{MTB_long:X4})");
        }

        #endregion
    }
}
