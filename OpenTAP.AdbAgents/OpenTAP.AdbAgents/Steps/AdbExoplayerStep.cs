// Author:      Bruno Garcia Garcia <bgg@uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain

using System;
using System.Collections.Generic;
using OpenTap;
using Tap.Plugins.UMA.AdbAgents.Instruments;
using Tap.Plugins.UMA.AdbAgents.Results;
using Tap.Plugins.UMA.Extensions;

namespace Tap.Plugins.UMA.AdbAgents.Steps
{
    [Display("Adb Exoplayer", Groups: new string[] { "Adb Agents" })]
    public class AdbExoplayerStep : AdbAgentBaseStep
    {
        private static readonly string DEVICE_FILE = "sdcard/adb_exoplayer";
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
            this.LogcatThreshold = 25; // Increased by default because Co measurement points tend to be filtered
            Exolist = new Enabled<string>() { IsEnabled = true, Value = ExoplayerInstrument.AXINOM };

            KeyEvents = new List<KeyEvent>() {
                new KeyEvent(KeyEvent.ActionEnum.KeyPress, 62, 1, "Select exolist"),
                new KeyEvent(KeyEvent.ActionEnum.KeyPress, 66, 1, "Open exolist"),
                new KeyEvent(KeyEvent.ActionEnum.KeyPress, 20, 3, "Select Video"),
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
            // Parse the results, but do not publish them
            List<ExoplayerResult> results = parseResults<ExoplayerResult>(null, logcatOutput, startTime);

            // Separate by kind
            List<ExoplayerResult> measurementPoints = new List<ExoplayerResult>();
            List<ExoplayerResult> videoResults = new List<ExoplayerResult>();
            List<ExoplayerResult> audioResults = new List<ExoplayerResult>();

            foreach (ExoplayerResult result in results)
            {
                switch (result.Kind)
                {
                    case ExoplayerResult.KindEnum.MeasurementPoint: measurementPoints.Add(result); break;
                    case ExoplayerResult.KindEnum.Video: videoResults.Add(result); break;
                    case ExoplayerResult.KindEnum.Audio: audioResults.Add(result); break;
                }
            }


            if (measurementPoints.Count != 0)
            {
                foreach (ResultTable table in ExoplayerResultHandler.GetUserExperienceTables(measurementPoints)) { table.PublishToSource(Results); }
            }

            if (audioResults.Count != 0)
            {
                ExoplayerResultHandler.GetTableFromList("Exoplayer Audio", audioResults).PublishToSource(Results);
            }

            if (videoResults.Count != 0)
            {
                ExoplayerResultHandler.GetTableFromList("Exoplayer Video", videoResults).PublishToSource(Results);
            }
        }
    }
}
