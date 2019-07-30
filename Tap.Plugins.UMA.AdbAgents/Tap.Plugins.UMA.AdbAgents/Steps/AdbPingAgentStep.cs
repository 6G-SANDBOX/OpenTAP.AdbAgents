// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain
//
// This file cannot be modified or redistributed. This header cannot be removed.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Keysight.Tap;
using Tap.Plugins.UMA.AdbAgents.Instruments;
using Tap.Plugins.UMA.AdbAgents.Results;

namespace Tap.Plugins.UMA.AdbAgents.Steps
{
    [Display("Adb Ping Agent", Groups: new string[] { "UMA", "Agents" })]
    public class AdbPingAgentStep : AdbAgentBaseStep
    {
        private static readonly string DEVICE_FILE = "sdcard/adb_ping_agent.log";
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
            parseResults<PingResult>("ADB Ping Agent", logcatOutput, startTime);
        }
    }
}
