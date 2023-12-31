﻿// Author:      Bruno Garcia Garcia <bgg@uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain

using System;
using OpenTap;
using Tap.Plugins.UMA.AdbAgents.Instruments;
using Tap.Plugins.UMA.AdbAgents.Results;

namespace Tap.Plugins.UMA.AdbAgents.Steps
{
    [Display("Adb Resource Agent", Groups: new string[] { "Adb Agents" })]
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
