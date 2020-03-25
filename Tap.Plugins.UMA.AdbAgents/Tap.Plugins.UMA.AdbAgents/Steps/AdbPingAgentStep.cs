﻿// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain

using System;
using System.Collections.Generic;
using OpenTap;
using Tap.Plugins.UMA.AdbAgents.Instruments;
using Tap.Plugins.UMA.AdbAgents.Results;
using Tap.Plugins.UMA.Extensions;

namespace Tap.Plugins.UMA.AdbAgents.Steps
{
    [Display("Adb Ping Agent", Groups: new string[] { "UMA", "Agents" })]
    public class AdbPingAgentStep : AdbAgentBaseStep
    {
        private static readonly string DEVICE_FILE = "sdcard/adb_ping_agent";
        private static readonly string AGENT_TAG = "ping.Report";

        [Display("Agent", Order: 1.0)]
        public PingAgentInstrument Instrument { get; set; }

        [EnabledIf("Action", ActionEnum.Start, ActionEnum.Measure, HideIfDisabled = true)]
        [Display("Target", Group: "Ping", Order: 51.0)]
        public string Target { get; set; }

        [EnabledIf("Action", ActionEnum.Start, ActionEnum.Measure, HideIfDisabled = true)]
        [Display("TTL", Group: "Ping", Order: 51.1)]
        public int Ttl { get; set; }

        public AdbPingAgentStep() : base()
        {
            Target = "www.google.com";
            Ttl = 128;
        }

        public override void Run()
        {
            DoRun(Instrument.Adb, DEVICE_FILE, AGENT_TAG);
        }

        protected override void StartAgent()
        {
            Instrument.Start(Target, Ttl, DeviceId);
        }

        protected override void StopAgent()
        {
            Instrument.Stop(DeviceId);
        }

        protected override void ParseResults(string[] logcatOutput, DateTime startTime)
        {
            List<PingResult> results = parseResults<PingResult>("ADB Ping Agent", logcatOutput, startTime);

            // Additional processing for aggregated measurements
            List<string> columns = new List<string>() { "Timestamp", "Total", "Success", "Failed", "Success Ratio", "Failed Ratio" };
            int success = 0;
            ulong timestamp = 0;
            int total = results.Count;
            
            foreach (PingResult result in results)
            {
                if (result.Success) { success++; }
                timestamp += result.Timestamp;
            }

            double successRatio = total != 0 ? (double)success / (double)total : 0;
            timestamp = total != 0 ? (ulong)(timestamp / (ulong)total) : (ulong)startTime.ToUnixUtcTimestamp();

            Results.Publish("ADB Ping Agent Aggregated",  columns, 
                timestamp, total, success, total - success, successRatio, 1.0-successRatio);
        }
    }
}
