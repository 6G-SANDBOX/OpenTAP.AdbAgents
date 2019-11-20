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
    [Display("Adb Exoplayer", Groups: new string[] { "UMA", "Agents" })]
    public class AdbExoplayerStep : AdbAgentBaseStep
    {
        private static readonly string DEVICE_FILE = "sdcard/adb_exoplayer.log";
        private static readonly string AGENT_TAG = "TriangleInstr";

        [Display("Agent", Order: 1.0)]
        public ExoplayerInstrument Instrument { get; set; }

        [EnabledIf("Action", ActionEnum.Start, ActionEnum.Measure, HideIfDisabled = true)]
        [Display("Exolist", Group: "Configuration", Order: 51.0)]
        public Enabled<string> Exolist { get; set; }

        [EnabledIf("Action", ActionEnum.Start, ActionEnum.Measure, HideIfDisabled = true)]
        [Display("Key Events", Group: "Configuration", Order: 51.1,
            Description: "List of keys to automatically press after requesting the use of the exolist.\n" + 
                         "Key codes: https://developer.android.com/reference/android/view/KeyEvent.html"
            )]
        public List<KeyEvent> KeyEvents { get; set; }

        public AdbExoplayerStep() : base()
        {
            Exolist = new Enabled<string>() { IsEnabled = true, Value = ExoplayerInstrument.AXINOM };

            KeyEvents = new List<KeyEvent>() {
                new KeyEvent(KeyEvent.ActionEnum.KeyPress, 62, 1, "Select exolist"),
                new KeyEvent(KeyEvent.ActionEnum.KeyPress, 66, 1, "Open exolist"),
                new KeyEvent(KeyEvent.ActionEnum.KeyPress, 20, 2, "Select Video"),
                new KeyEvent(KeyEvent.ActionEnum.KeyPress, 66, 1, "Start video"),
            };
        }

        public override void Run()
        {
            DoRun(Instrument.Adb, DEVICE_FILE, AGENT_TAG);
        }

        protected override void StartAgent()
        {
            Instrument.Start(DeviceId, Exolist.IsEnabled ? Exolist.Value : null, KeyEvents);
        }

        protected override void StopAgent()
        {
            Instrument.Stop(DeviceId);
        }

        protected override void ParseResults(string[] logcatOutput, DateTime startTime)
        {
            //List<PingResult> results = parseResults<PingResult>("ADB Ping Agent", logcatOutput, startTime);

            // Additional processing for aggregated measurements
            List<string> columns = new List<string>() { "Timestamp", "Total", "Success", "Failed", "Success Ratio", "Failed Ratio" };
            
            Results.Publish("ADB Ping Agent Aggregated", columns, 1,1,1,1,1,1,1);
        }
    }
}
