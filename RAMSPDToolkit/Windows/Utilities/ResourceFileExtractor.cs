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

using System.Diagnostics;
using System.IO.Compression;

namespace RAMSPDToolkit.Windows.Utilities
{
    internal static class ResourceFileExtractor
    {
        #region Public

        public static bool ExtractResourceFileToFilePath(string filePath, string resourceFile)
        {
            string resourceName = $"{nameof(RAMSPDToolkit)}.Resources.{resourceFile}";

            var assemblyWithDriverResource = typeof(ResourceFileExtractor).Assembly;

            long requiredLength = 0;

            try
            {
                using FileStream target = new(filePath, FileMode.Create);

                using var gzipStream = GetResourceFileGZipStream(resourceFile);

                gzipStream.CopyTo(target);

                requiredLength = target.Length;
            }
            catch (Exception)
            {
                return false;
            }

            if (ValidateUnzippedFile(filePath, requiredLength))
            {
                return true;
            }

            //Ensure the file is written to the file system - wait for it
            var sw = new Stopwatch();
            sw.Start();

            while (sw.ElapsedMilliseconds < 2000)
            {
                if (ValidateUnzippedFile(filePath, requiredLength))
                {
                    return true;
                }

                Thread.Yield();
            }

            return false;
        }

        public static GZipStream GetResourceFileGZipStream(string resourceFile)
        {
            string resourceName = $"{nameof(RAMSPDToolkit)}.Resources.{resourceFile}";

            var assemblyWithDriverResource = typeof(ResourceFileExtractor).Assembly;

            try
            {
                Stream stream = assemblyWithDriverResource.GetManifestResourceStream(resourceName);

                //Resource is good
                if (stream != null)
                {
                    return new GZipStream(stream, CompressionMode.Decompress);
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        static bool ValidateUnzippedFile(string filePath, long requiredLength)
        {
            try
            {
                return File.Exists(filePath) && new FileInfo(filePath).Length == requiredLength;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion
    }
}
