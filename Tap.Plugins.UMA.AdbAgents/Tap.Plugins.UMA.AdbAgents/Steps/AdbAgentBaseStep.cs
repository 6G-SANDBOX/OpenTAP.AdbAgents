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
using Tap.Plugins.UMA.AdbAgents.Steps;
using Tap.Plugins.UMA.AdbAgents.Results;

namespace Tap.Plugins.UMA.AdbAgents.Steps
{
    public abstract class AdbAgentBaseStep : MeasurementStepBase
    {
        public enum ActionEnum { Start, Measure, Stop };

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

        public AdbAgentBaseStep()
        {
            Action = ActionEnum.Measure;
            MeasurementMode = WaitMode.Time;
            MeasurementTime = 10;
        }

        protected abstract void StartAgent();
        protected abstract void StopAgent();
        protected abstract void ParseResults(string[] logcatOutput, DateTime startTime);

        protected void DoRun(AdbInstrument adb, string deviceFile, string agentTag)
        {
            BackgroundLogcat logcat;
            // Prepare logcat
            if (Action == ActionEnum.Start || Action == ActionEnum.Measure)
            {
                logcat = adb.ExecuteBackgroundLogcat(deviceFile, DeviceId,
                    filter: LogcatFilter.CreateSingleTagFilter(agentTag, LogcatPriority.Info));

                // Set the created logcat as output
                if (Action == ActionEnum.Start)
                {
                    LogcatOutput = logcat;
                }

                StartAgent();
            }
            else
            {
                logcat = LogcatInput.Value;
            }

            // Measure
            if (Action == ActionEnum.Measure)
            {
                MeasurementWait();
            }

            // Retrieve results
            if (Action == ActionEnum.Measure || Action == ActionEnum.Stop)
            {
                StopAgent();

                logcat.Terminate();

                string[] res = adb.RetrieveLogcat(logcat, localFilename: null);

                ParseResults(res, logcat.StartTime);

                adb.DeleteExistingDeviceLogcatFiles(deviceFile, DeviceId);
            }
        }

        protected void parseResults<T>(string tableName, string[] logcat, DateTime startTime) where T : ResultBase, new()
        {
            T resultInstance = new T();
            string[] columnNames = resultInstance.GetColumns();

            Dictionary<string, List<IConvertible>> resultLists = new Dictionary<string, List<IConvertible>>();
            foreach (string column in columnNames)
            {
                resultLists[column] = new List<IConvertible>();
            }

            Log.Info($"Parsing {tableName} results from logcat (starting at {startTime.ToShortTimeString()})");
            int found = 0, ignored = 0;

            foreach (string line in logcat)
            {
                System.Diagnostics.Debug.WriteLine(line);
                ResourcesResult resources = new ResourcesResult(line);

                if (resources.Valid)
                {
                    if (resources.LogTime > startTime)
                    {
                        foreach (string column in columnNames)
                        {
                            resultLists[column].Add(resources.GetValue(column));
                        }
                        found++;
                    }
                    else { ignored++; }
                }
                else
                {
                    string message = $"Could not parse logcat line '{line}'";
                    if (line.EndsWith(">>>")) { Log.Warning(message); }
                    else { Log.Debug(message); }
                }
            }

            if (found > 0)
            {
                List<ResultColumn> resultColumns = new List<ResultColumn>();

                foreach (string column in columnNames)
                {
                    resultColumns.Add(new ResultColumn(column, resultLists[column].ToArray()));
                }

                ResultTable table = new ResultTable(tableName, resultColumns.ToArray());
                table.PublishToSource(Results);
                Log.Debug($"Published {found} results, {ignored} logcat lines ignored (previous to {startTime.ToShortTimeString()})");
            }
            else
            {
                Log.Warning($"No results retrieved, ignored {ignored} results (previous to {startTime.ToShortTimeString()}).");
            }
        }
    }
}
