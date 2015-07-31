using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace GetClusterInfo
{
    class Program
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">0 params = list all drives and cluster sizes, 1 param = root letter of a drive</param>
        static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("Reading Cluster Size Info...");
            var driveList = new List<string>(args.Length == 0 ? DriveInfo.GetDrives()
                .Where(d => d.IsReady && (d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable || d.DriveType == DriveType.CDRom))
                .Select(d => d.Name.ElementAt(0).ToString()) : new string[] { args[0].Replace(":", "").Replace("\\", "") });
            
            foreach (var drive in driveList)
            {
                try
                {
                    var clusterSize = DriveLibrary.GetReadableClusterSize(drive);
                    Console.WriteLine(@"Drive {0}: Cluster Size - {1}", new string[] { drive, clusterSize.ToString() });
                }
                catch(Exception ex)
                {
                    Console.WriteLine(@"Drive {0}: Error {1}", new string[] { drive, ex.Message });
                }
            }

        }
    }

    class Kernel32
    {
        // MSDN Documentation: https://msdn.microsoft.com/en-us/library/windows/desktop/aa364935(v=vs.85).aspx
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool GetDiskFreeSpace(string lpRootPathName,
            out uint lpSectorsPerCluster,
            out uint lpBytesPerSector,
            out uint lpNumberOfFreeClusters,
            out uint lpTotalNumberOfClusters);
    }

    class DriveLibrary
    {
        /// <summary>
        /// Gets the size of a cluster for the drive selected.
        /// </summary>
        /// <param name="driveLetter"></param>
        /// <returns>cluster size in bytes</returns>
        private static long GetClusterSizeInBytes(string driveLetter)
        {
            long clusterSize = 0;
            long driveSize = new DriveInfo(driveLetter).TotalSize;

            uint sectors, sectorBytes, freeClusters, totalClusters;
            Kernel32.GetDiskFreeSpace(string.Concat(driveLetter, ":\\"), out sectors, out sectorBytes, out freeClusters, out totalClusters);

            clusterSize = driveSize / totalClusters;

            return clusterSize;
        }

        /// <summary>
        /// Sanity-check version of GetClusterSizeInBytes that makes sure that system is returning consistent values.
        /// </summary>
        /// <param name="driveLetter"></param>
        /// <returns>cluster size in bytes</returns>
        public static long GetDriveClusterSizeInBytes(string driveLetter)
        {
            long clusterSize = GetClusterSizeInBytes(driveLetter);

            for (int i = 0; i < 2; i++)
            {
                if (GetClusterSizeInBytes(driveLetter) != clusterSize)
                    throw new ApplicationException(string.Format("GetDriveClusterSize for drive {0} returned inconsistent cluster size values", new string[] { driveLetter }));
            }

            return clusterSize;
        }

        /// <summary>
        /// Returns legible cluster size as a string (eg 4 KB as opposed to 4096 Bytes)
        /// </summary>
        /// <param name="driveLetter"></param>
        /// <returns></returns>
        public static string GetReadableClusterSize(string driveLetter)
        {
            long bytes = GetDriveClusterSizeInBytes(driveLetter);

            if (bytes < 1024)
                return bytes + " B";
            else if (bytes < Math.Pow(1024, 2))
                return (bytes / 1024) + " KB";
            else if (bytes < Math.Pow(1024, 3)) // highly unlikely as it would be a huge waste of space.
                return (bytes / 1024 / 1024) + " MB";
            else
                return bytes + " B";
        }

    }
}
