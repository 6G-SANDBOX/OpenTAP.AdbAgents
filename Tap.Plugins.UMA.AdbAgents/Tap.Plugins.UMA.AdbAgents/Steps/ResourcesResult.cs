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

        private static readonly string CPU_RAM = $"CPU usage {FLOAT}% ; Ram used {INT}MBs ; Available Ram {INT}MBs";
        private static readonly string TX_RX = $"Packets Received {INT} ; Packets Transmitted {INT} ; Bytes Received {INT} ; Bytes Transmitted {INT}";
        private static readonly string NETWORK = @"Operator (.+) ; Network (.+) ; Cell ID (.+) ; LAC (.+) ; RSSI (.+) ; PSC (.+) ; RSRP (.+) ; SNR (.+) ; CQI (.+) ; RSRQ (.+)";

        private static Regex regex = new Regex(
            $"{DATETIME}.*<<< Elapsed time .* sec ; Timestamp {INT} ; {CPU_RAM} ; {TX_RX} ; ({NETWORK}) >>>.*",
            RegexOptions.Compiled);

        private static Regex networkRegex = new Regex(NETWORK, RegexOptions.Compiled);

        public static string[] COLUMNS = new string[] {
            "Timestamp", "Used CPU Per Cent", "Used RAM", "Available RAM", "Total RAM",
            "Used RAM Per Cent", "Packets Sent", "PacketsReceived", "Bytes Sent", "Bytes Received",
            "Operator", "Network", "Cell ID", "LAC", "RSSI", "PSC", "SNR", "RSRP", "RSRQ", "CQI"
        };

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

        public string Operator;
        public string Network;
        public string CellId;
        public string Lac;
        public int Rssi;
        public int Psc;
        public int Rsrp;
        public int Snr;
        public int Cqi;
        public int Rsrq;

        public ResourcesResult(string line)
        {
            LogTime = DateTime.MinValue;
            Timestamp = 0;
            UsedCpuPerCent = UsedRamPerCent = 0.0;
            UsedRam = AvailableRam = TotalRam = PacketsSent = PacketsReceived = BytesSent = BytesReceived = 0;
            Operator = Network = CellId = Lac = "Unavailable";
            Rssi = Psc = Rsrp = Snr = Cqi = Rsrq = -1;
            Valid = false;

            Match match = regex.Match(line);

            if (match.Success)
            {
                LogTime = DateTime.ParseExact(match.Groups[1].Value, "MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                Timestamp = ulong.Parse(match.Groups[2].Value);
                UsedCpuPerCent = double.Parse(match.Groups[3].Value);
                UsedRam = int.Parse(match.Groups[6].Value);
                AvailableRam = int.Parse(match.Groups[7].Value);
                TotalRam = UsedRam + AvailableRam;
                UsedRamPerCent = ((double)UsedRam / TotalRam) * 100.0;
                PacketsReceived = int.Parse(match.Groups[8].Value);
                PacketsSent = int.Parse(match.Groups[9].Value);
                BytesReceived = int.Parse(match.Groups[10].Value);
                BytesSent = int.Parse(match.Groups[11].Value);

                Valid = parseNetwork(match.Groups[12].Value);
            }
        }

        private bool parseNetwork(string line)
        {
            Match match = networkRegex.Match(line);

            if (match.Success)
            {
                Operator = match.Groups[1].Value;
                Network = match.Groups[2].Value;
                CellId = match.Groups[3].Value;
                Lac = match.Groups[4].Value;
                Rssi = maybeInt(match.Groups[5].Value, int.MinValue);
                Psc = maybeInt(match.Groups[6].Value, int.MinValue);
                Rsrp = maybeInt(match.Groups[7].Value, int.MinValue);
                Snr = maybeInt(match.Groups[8].Value, int.MinValue);
                Cqi = maybeInt(match.Groups[9].Value, int.MinValue);
                Rsrq = maybeInt(match.Groups[10].Value, int.MinValue);
                return true;
            }
            return false;
        }

        private int maybeInt(string value, int n_a)
        {
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return n_a;
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
                case "Operator": return Operator;
                case "Network": return Network;
                case "Cell ID": return CellId;
                case "LAC": return Lac;
                case "RSSI": return Rssi;
                case "PSC": return Psc;
                case "SNR": return Snr;
                case "RSRP": return Rsrp;
                case "RSRQ": return Rsrq;
                case "CQI": return Cqi;
                default: throw new Exception($"Unrecognized column '{column}'");
            }
        }
    }
}
