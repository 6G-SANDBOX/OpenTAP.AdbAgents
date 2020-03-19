// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain

using System;
using System.Text.RegularExpressions;

namespace Tap.Plugins.UMA.AdbAgents.Results
{
    public class PingResult : ResultBase
    {
        private static Regex regex = new Regex(
            $"{DATETIME}.*<<< Timestamp: {INT} ; Time:{INT} ; Delay:(.*) >>>.*",
            RegexOptions.Compiled);

        public static string[] COLUMNS = new string[] {
            "Timestamp", "ICMP Seq", "Success", "Delay (ms)"
        };

        public override string[] GetColumns() { return COLUMNS; }

        public long IcmpSeq;
        public bool Success;
        public double? Delay;

        public PingResult()
        {
            LogTime = DateTime.MinValue;
            Timestamp = 0;
            Success = false;
            Delay = null;
            Valid = false;
        }

        public override void Parse(string line)
        {
            Match match = regex.Match(line);

            if (match.Success)
            {
                LogTime = logcatDate(match.Groups[1].Value);
                Timestamp = ulong.Parse(match.Groups[2].Value);
                IcmpSeq = long.Parse(match.Groups[3].Value) + 1;
                Delay = maybeDouble(match.Groups[4].Value);

                // We expect one try per second, if the delay is longer we can consider that the request 
                // was lost (also, the agent probably returned Double.MAX_VALUE)
                if (Delay.HasValue && Delay > 1100.0) { Delay = null; }
                Success = (Delay != null);

                Valid = true;
            }
        }

        public override IConvertible GetValue(string column)
        {
            switch (column)
            {
                case "Timestamp": return Timestamp;
                case "ICMP Seq": return IcmpSeq;
                case "Success": return Success;
                case "Delay (ms)": return Delay;
                default: throw new Exception($"Unrecognized column '{column}'");
            }
        }
    }
}
