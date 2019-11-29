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
using Tap.Plugins.UMA.Extensions;

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

            // TODO: Handle measurement points
            if (audioResults.Count != 0) { publishList("Exoplayer Audio", audioResults); }
            if (videoResults.Count != 0) { publishList("Exoplayer Video", videoResults); }
        }

        private void publishList(string name, List<ExoplayerResult> results)
        {
            List<ulong> timestamps = new List<ulong>();

            // Generate all column placeholders
            Dictionary<string, List<IConvertible>> columns = new Dictionary<string, List<IConvertible>>();

            foreach (ExoplayerResult result in results)
            {
                foreach (var key in result.ExtraValues.Keys)
                {
                    if (!columns.ContainsKey(key)) { columns[key] = new List<IConvertible>(); }
                }
            }

            // Fill the columns
            foreach (ExoplayerResult result in results)
            {
                List<string> missingKeys = new List<string>(columns.Keys);
                timestamps.Add(result.Timestamp);
                foreach (var keyValue in result.ExtraValues)
                {
                    columns[keyValue.Key].Add(keyValue.Value);
                    missingKeys.Remove(keyValue.Key);
                }

                foreach (string key in missingKeys)
                {
                    columns[key].Add(null);
                }
            }

            // Prepare columns, publish table
            List<ResultColumn> resultColumns = new List<ResultColumn>();
            resultColumns.Add(new ResultColumn("Timestamp", timestamps.ToArray()));
            foreach (var keyValue in columns)
            {
                resultColumns.Add(new ResultColumn(keyValue.Key, keyValue.Value.ToArray()));
            }

            ResultTable table = new ResultTable(name, resultColumns.ToArray());
            table.PublishToSource(Results);
        }
    }
}
