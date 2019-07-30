// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain
//
// This file cannot be modified or redistributed. This header cannot be removed.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tap.Plugins.UMA.AdbAgents.Steps
{
    public struct ResourcesResult
    {
        private static readonly string DATETIME = @"(\d+-\d+ \d+:\d+:\d+.\d+)";
        private static readonly string FLOAT = @"((\d+)([.,]\d+)*)";
        private static readonly string INT = @"(\d+)";

        private static Regex regex = new Regex(
            $"{DATETIME}.*<<< Elapsed time .* sec ; Timestamp {INT} ; CPU usage {FLOAT}% ; Ram used {INT}MBs ; Available Ram {INT}MBs ; Packets Received {INT} ; Packets Transmitted {INT} ; Bytes Received {INT} ; Bytes Transmitted {INT} >>>.*",
            RegexOptions.Compiled);

        public static string[] COLUMNS = new string[] {
                "Timestamp", "Used CPU Per Cent", "Used RAM", "Available RAM", "Total RAM",
                "Used RAM Per Cent", "Packets Sent", "PacketsReceived", "Bytes Sent", "Bytes Received" };

        public bool Valid;
        public DateTime LogTime;
        public ulong Timestamp;
        public double UsedCpuPerCent;
        public int UsedRam;
        public int AvailableRam;
        public int TotalRam;
        public double UsedRamPerCent;
        public int PacketsSent;
        public int PacketsReceived;
        public int BytesSent;
        public int BytesReceived;

        public ResourcesResult(string line)
        {
            Match match = regex.Match(line);

            if (match.Success)
            {
                LogTime = DateTime.ParseExact(match.Groups[1].Value, "MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                Timestamp = ulong.Parse(match.Groups[2].Value);
                UsedCpuPerCent = double.Parse(match.Groups[3].Value);
                UsedRam = int.Parse(match.Groups[6].Value);
                AvailableRam = int.Parse(match.Groups[7].Value);
                TotalRam = UsedRam + AvailableRam;
                UsedRamPerCent = (TotalRam / 100.0) * UsedRam;
                PacketsReceived = int.Parse(match.Groups[8].Value);
                PacketsSent = int.Parse(match.Groups[9].Value);
                BytesReceived = int.Parse(match.Groups[10].Value);
                BytesSent = int.Parse(match.Groups[11].Value);
                Valid = true;
            }
            else
            {
                LogTime = DateTime.MinValue;
                Timestamp = 0;
                UsedCpuPerCent = UsedRamPerCent = 0.0;
                UsedRam = AvailableRam = TotalRam = PacketsSent = PacketsReceived = BytesSent = BytesReceived = 0;
                Valid = false;
            }
        }

        public IConvertible GetValue(string column)
        {
            switch (column)
            {
                case "Timestamp": return Timestamp;
                case "Used CPU Per Cent": return UsedCpuPerCent;
                case "Used RAM": return UsedRam;
                case "Available RAM": return AvailableRam;
                case "Total RAM": return TotalRam;
                case "Used RAM Per Cent": return UsedRamPerCent;
                case "Packets Sent": return PacketsSent;
                case "PacketsReceived": return PacketsReceived;
                case "Bytes Sent": return BytesSent;
                case "Bytes Received": return BytesReceived;
                default: throw new Exception($"Unrecognized column '{column}'");
            }
        }
    }

}
