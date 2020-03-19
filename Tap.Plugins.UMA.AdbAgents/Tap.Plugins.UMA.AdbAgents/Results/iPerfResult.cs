// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain

using System;
using System.Text.RegularExpressions;

namespace Tap.Plugins.UMA.AdbAgents.Results
{
    public class iPerfResult : ResultBase
    {
        private static Regex regex = new Regex(
            $"{DATETIME}.*<<< Timestamp: {INT} ; Output: (.*) >>>.*",
            RegexOptions.Compiled);

        private static Regex iperfRegex = new Regex(
            $@"\[(.*)] *{FLOAT} *- *{FLOAT} *sec *{FLOAT} *MBytes *{FLOAT} *Mbits/sec(.*)?",
            RegexOptions.Compiled);

        private static Regex udpRegex = new Regex(
            $@" *{FLOAT} *ms *{INT} */ *{INT} \({FLOAT}%\)",
            RegexOptions.Compiled);

        public static string[] COLUMNS = new string[] {
            "Timestamp", "Throughput (Mbps)", "Jitter (ms)", "Packet Loss (%)"
        };

        public override string[] GetColumns() { return COLUMNS; }

        public double Throughput;
        public double Jitter;
        public double PacketLoss;

        // Unpublished data
        public long Sequence;
        public long Lost;
        public long Sent;

        public iPerfResult()
        {
            LogTime = DateTime.MinValue;
            Timestamp = 0;
            Throughput = Jitter = PacketLoss = 0;
            Sequence = Lost = Sent = 0;

            Valid = false;
        }

        public static bool IsiPerfResult(string line)
        {
            return regex.IsMatch(line);
        }

        public static bool IsSumResult(string line)
        {
            return (IsiPerfResult(line) && line.Contains("[SUM]"));
        }

        public override void Parse(string line)
        {
            Match match = regex.Match(line);

            if (match.Success)
            {
                LogTime = logcatDate(match.Groups[1].Value);
                Timestamp = ulong.Parse(match.Groups[2].Value);

                Valid = parseiPerf(match.Groups[3].Value);
            }
        }

        private bool parseiPerf(string value)
        {
            Match match = iperfRegex.Match(value);

            if (match.Success)
            {
                Throughput = double.Parse(match.Groups[11].Value);
                Sequence = long.Parse(match.Groups[3].Value);

                return parseUdp(match.Groups[14].Value);
            }
            else { return false; }
        }

        private bool parseUdp(string value)
        {
            Match match = udpRegex.Match(value);

            if (match.Success)
            {
                Jitter = double.Parse(match.Groups[1].Value);
                PacketLoss = double.Parse(match.Groups[6].Value);

                Lost = long.Parse(match.Groups[4].Value);
                Sent = long.Parse(match.Groups[5].Value);
            }
            return true;
        }

        public override IConvertible GetValue(string column)
        {
            switch (column)
            {
                case "Timestamp": return Timestamp;
                case "Throughput (Mbps)": return Throughput;
                case "Jitter (ms)": return Jitter;
                case "Packet Loss (%)": return PacketLoss;
                // Unpublished data
                case "Sequence": return Sequence;
                case "Sent": return Sent;
                case "Lost": return Lost;
                default: throw new Exception($"Unrecognized column '{column}'");
            }
        }
    }
}
