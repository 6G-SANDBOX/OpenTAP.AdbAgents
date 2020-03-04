// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain
//
// This file cannot be modified or redistributed. This header cannot be removed.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using OpenTap;
using Tap.Plugins.UMA.AdbAgents.Instruments;
using Tap.Plugins.UMA.AdbAgents.Results;

namespace Tap.Plugins.UMA.AdbAgents.Steps
{
    [Display("Adb Resource Agent", Groups: new string[] { "UMA", "Agents" })]
    public class AdbResourceAgentStep : AdbAgentBaseStep
    {
        private static readonly string DEVICE_FILE = "sdcard/adb_resource_agent";
        private static readonly string AGENT_TAG = "resourceAgent.ResourceAgentTask";

        [Display("Agent", Order: 1.0)]
        public ResourceAgentInstrument Instrument { get; set; }

        public AdbResourceAgentStep() : base() { }

        public override void Run()
        {
            DoRun(Instrument.Adb, DEVICE_FILE, AGENT_TAG);
        }

        protected override void StartAgent()
        {
            Instrument.Start(DeviceId);
        }

        protected override void StopAgent()
        {
            Instrument.Stop(DeviceId);
        }

        protected override void ParseResults(string[] logcatOutput, DateTime startTime)
        {
            parseResults<ResourcesResult>("ADB Resource Agent", logcatOutput, startTime);
        }
    }
}
