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
using Tap.Plugins.UMA.Android.Instruments;
using Tap.Plugins.UMA.Android.Instruments.Logcat;
using Tap.Plugins.UMA.Extensions;

namespace Tap.Plugins.UMA.AdbAgents.Steps
{
    [Display("Adb Resource Agent", Groups: new string[] { "UMA", "Agents" })]
    public class AdbResourceAgentStep : MeasurementStepBase
    {
        private static readonly string DEVICE_FILE = "sdcard/adb_res_mon.log";
        private static readonly string AGENT_TAG = "resourceAgent.ResourceAgentTask";

        public enum ActionEnum {
            Start,
            Measure,
            Stop
        };

        [Display("Instrument")]
        public ResourceAgentInstrument Instrument { get; set; }

        [Display(Name: "Device ID",
            Description: "The ID of the device to which this command will be sent.\n" +
                "This is not required for commands that are not sent to\n" +
                "a device (e.g. 'adb devices'), or when a single device\n" +
                "is connected.",
            Order: 1.1)]
        public string DeviceId { get; set; }

        [Display("Action")]
        public ActionEnum Action { get; set; }

        [Display("Background Logcat")]
        [EnabledIf("Action", ActionEnum.Stop, HideIfDisabled = true)]
        public Input<BackgroundLogcat> LogcatInput { get; set; }

        [Output]
        [XmlIgnore]
        public BackgroundLogcat LogcatOutput { get; set; }

        public override bool HideMeasurement { get { return Action != ActionEnum.Measure; } }

        public AdbResourceAgentStep() { }

        public override void Run()
        {
            BackgroundLogcat logcat;
            // Prepare logcat
            if (Action == ActionEnum.Start || Action == ActionEnum.Measure)
            {
                logcat = Instrument.Adb.ExecuteBackgroundLogcat(DEVICE_FILE, DeviceId,
                    filter: LogcatFilter.CreateSingleTagFilter(AGENT_TAG, LogcatPriority.Info));

                // Set the created logcat as output
                if (Action == ActionEnum.Start)
                {
                    LogcatOutput = logcat;
                }

                Instrument.Start(DeviceId);
            }
            else
            {
                logcat = LogcatInput.Value;
            }

            // Measure
            if (Action == ActionEnum.Measure)
            {
                TestPlan.Sleep(10000);
            }

            // Retrieve results
            if (Action == ActionEnum.Measure || Action == ActionEnum.Stop)
            {
                Instrument.Stop(DeviceId);

                logcat.Terminate();

                string[] res = Instrument.Adb.RetrieveLogcat(logcat, localFilename: null);

                parseResults(res, logcat.StartTime);

                Instrument.Adb.DeleteExistingDeviceLogcatFiles(DEVICE_FILE, DeviceId);
            }
        }

        private string buildArguments()
        {
            LogcatCommandBuilder builder = new LogcatCommandBuilder();
            builder.Filter = LogcatFilter.CreateSingleTagFilter(AGENT_TAG, LogcatPriority.Info);
            builder.Buffer = LogcatBuffer.Main;
            builder.Format = LogcatFormat.Threadtime;
            builder.Filename = DEVICE_FILE;
            builder.DumpAndExit = true;
            return builder.Build();
        }

        protected void logOutput(AdbCommandResult result)
        {
            Log.Debug("Command was {0}", result.Success ? "succesful" : "not sucessful");

            Log.Debug("------ Start of adb output ------");
            foreach (string line in result.Output)
            {
                Log.Debug(line);
            }
            Log.Debug("------- End of adb output -------");
        }

        private void parseResults(string[] logcat, DateTime startTime)
        {
            Dictionary<string, List<IConvertible>> resultLists = new Dictionary<string, List<IConvertible>>();
            foreach (string column in ResourcesResult.COLUMNS)
            {
                resultLists[column] = new List<IConvertible>();
            }

            Log.Info($"Parsing adb Resource Agent results from logcat (from {startTime.ToShortTimeString()})");
            int found = 0, ignored = 0;

            foreach (string line in logcat)
            {
                System.Diagnostics.Debug.WriteLine(line);
                ResourcesResult resources = new ResourcesResult(line);

                if (resources.Valid)
                {
                    if (resources.LogTime > startTime)
                    {
                        foreach (string column in ResourcesResult.COLUMNS)
                        {
                            resultLists[column].Add(resources.GetValue(column));
                        }
                        found++;
                    }
                    else { ignored++; }
                }
                else
                {
                    Log.Warning($"Could not parse logcat line '{line}'");
                }
            }

            if (found > 0)
            {
                List<ResultColumn> resultColumns = new List<ResultColumn>();

                foreach (string column in ResourcesResult.COLUMNS)
                {
                    resultColumns.Add(new ResultColumn(column, resultLists[column].ToArray()));
                }

                ResultTable table = new ResultTable("ADB Resource Agent", resultColumns.ToArray());
                table.PublishToSource(Results);
                Log.Debug($"Published {found} results ({ignored} logcat lines ignored)");
            }
            else
            {
                Log.Warning($"No results retrieved, ignored {ignored} results.");
            }
        }
    }
}
