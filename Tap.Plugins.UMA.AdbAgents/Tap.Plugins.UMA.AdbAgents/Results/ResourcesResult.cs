// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tap.Plugins.UMA.AdbAgents.Results
{
    public class ResourcesResult : ResultBase
    {
        private static readonly string CPU_RAM = $"CPU usage {FLOAT}% ; Ram used {INT}MBs ; Available Ram {INT}MBs";
        private static readonly string TX_RX = $"Packets Received {INT} ; Packets Transmitted {INT} ; Bytes Received {INT} ; Bytes Transmitted {INT}";
        private static readonly string NETWORK = @"Operator (.+) ; Network (.+) ; Cell ID (.+) ; LAC (.+) ; RSSI (.+) ; PSC (.+) ; RSRP (.+) ; SNR (.+) ; CQI (.+) ; RSRQ (.+)";

        private static Regex regex = new Regex(
            $"{DATETIME}.*<<< Elapsed time .* sec ; Timestamp {INT} ; {CPU_RAM} ; {TX_RX} ; ({NETWORK}) >>>.*",
            RegexOptions.Compiled);

        private static Regex networkRegex = new Regex(NETWORK, RegexOptions.Compiled);

        public static string[] COLUMNS = new string[] {
            "Timestamp", "Used CPU (%)", "Used RAM (MB)", "Available RAM (MB)", "Total RAM (MB)",
            "Used RAM (%)", "Packets Sent", "Packets Received", "Bytes Sent", "Bytes Received",
            "Operator", "Network", "Cell ID", "LAC", "RSSI", "PSC", "SNR", "RSRP", "RSRQ", "CQI"
        };

        public override string[] GetColumns() { return COLUMNS; }

        public double UsedCpuPerCent;
        public long UsedRam;
        public long AvailableRam;
        public long TotalRam;
        public double UsedRamPerCent;

        public long PacketsSent;
        public long PacketsReceived;
        public long BytesSent;
        public long BytesReceived;

        public string Operator;
        public string Network;
        public string CellId;
        public string Lac;
        public string Psc;
        public int? Rssi;
        public int? Rsrp;
        public double? Snr;
        public int? Cqi;
        public int? Rsrq;

        public ResourcesResult()
        {
            LogTime = DateTime.MinValue;
            Timestamp = 0;
            UsedCpuPerCent = UsedRamPerCent = 0.0;
            UsedRam = AvailableRam = TotalRam = PacketsSent = PacketsReceived = BytesSent = BytesReceived = 0;
            Operator = Network = CellId = Lac = Psc = "Unavailable";
            Rssi = Rsrp = Cqi = Rsrq = -1;
            Snr = -1.0;
            Valid = false;
        }

        public override void Parse(string line)
        {
            Match match = regex.Match(line);

            if (match.Success)
            {
                LogTime = logcatDate(match.Groups[1].Value);
                Timestamp = ulong.Parse(match.Groups[2].Value);
                UsedCpuPerCent = double.Parse(match.Groups[3].Value);
                UsedRam = long.Parse(match.Groups[6].Value);
                AvailableRam = long.Parse(match.Groups[7].Value);
                TotalRam = UsedRam + AvailableRam;
                UsedRamPerCent = ((double)UsedRam / TotalRam) * 100.0;
                PacketsReceived = long.Parse(match.Groups[8].Value);
                PacketsSent = long.Parse(match.Groups[9].Value);
                BytesReceived = long.Parse(match.Groups[10].Value);
                BytesSent = long.Parse(match.Groups[11].Value);

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
                Rssi = maybeInt(match.Groups[5].Value);
                Psc = match.Groups[6].Value;
                Rsrp = maybeInt(match.Groups[7].Value);
                Snr = maybeDouble(match.Groups[8].Value);
                Cqi = maybeInt(match.Groups[9].Value);
                Rsrq = maybeInt(match.Groups[10].Value);
                return true;
            }
            return false;
        }

        public override IConvertible GetValue(string column)
        {
            switch (column)
            {
                case "Timestamp": return Timestamp;
                case "Used CPU (%)": return UsedCpuPerCent;
                case "Used RAM (MB)": return UsedRam;
                case "Available RAM (MB)": return AvailableRam;
                case "Total RAM (MB)": return TotalRam;
                case "Used RAM (%)": return UsedRamPerCent;
                case "Packets Sent": return PacketsSent;
                case "Packets Received": return PacketsReceived;
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
