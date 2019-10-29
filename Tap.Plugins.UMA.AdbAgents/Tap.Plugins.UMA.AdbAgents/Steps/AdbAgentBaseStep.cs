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
using Tap.Plugins.UMA.Android.Instruments;
using Tap.Plugins.UMA.Android.Instruments.Logcat;
using Tap.Plugins.UMA.Extensions;
using Tap.Plugins.UMA.AdbAgents.Steps;
using Tap.Plugins.UMA.AdbAgents.Results;

namespace Tap.Plugins.UMA.AdbAgents.Steps
{
    [AllowAnyChild]
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

        [Display("Action", Order: 2.0)]
        public ActionEnum Action { get; set; }

        [Display("Logcat Threshold", Order: 2.1,
            Description: "Extra seconds of logcat output to include as valid results for this execution.\n" +
                         "Do not set this to a value larger enought that it might include messages generated\n" +
                         "in a previous execution of this step. [Default 15s, usually requires a minimum of 10s]")]
        [Unit("s")]
        [EnabledIf("Action", ActionEnum.Start, ActionEnum.Measure, HideIfDisabled = true)]
        public int LogcatThreshold { get; set; }

        [Display("Background Logcat", Order: 2.1)]
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
            LogcatThreshold = 15;
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
                adb.DeleteExistingDeviceLogcatFiles(deviceFile, DeviceId);

                logcat = adb.ExecuteBackgroundLogcat(deviceFile, DeviceId,
                    filter: LogcatFilter.CreateSingleTagFilter(agentTag, LogcatPriority.Info));

                logcat.StartTime = logcat.StartTime.AddSeconds(-LogcatThreshold);

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
            }
        }

        protected List<T> parseResults<T>(string tableName, string[] logcat, DateTime startTime) where T : ResultBase, new()
        {
            string[] columnNames = new T().GetColumns();
            List<T> results = new List<T>();

            Dictionary<string, List<IConvertible>> resultLists = new Dictionary<string, List<IConvertible>>();
            foreach (string column in columnNames)
            {
                resultLists[column] = new List<IConvertible>();
            }

            Log.Info($"Parsing {tableName} results from logcat (starting at {startTime.ToLongTimeString()})");
            int ignored = 0;

            foreach (string line in logcat)
            {
                Log.Debug(line);
                T result = new T();
                result.Parse(line);

                if (result.Valid)
                {
                    if (result.LogTime > startTime)
                    {
                        foreach (string column in columnNames)
                        {
                            resultLists[column].Add(result.GetValue(column));
                        }
                        results.Add(result);
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

            if (results.Count > 0)
            {
                List<ResultColumn> resultColumns = new List<ResultColumn>();

                foreach (string column in columnNames)
                {
                    resultColumns.Add(new ResultColumn(column, resultLists[column].ToArray()));
                }

                ResultTable table = new ResultTable(tableName, resultColumns.ToArray());
                table.PublishToSource(Results);
                Log.Debug($"Published {results.Count} results, {ignored} logcat lines ignored (previous to {startTime.ToLongTimeString()})");
            }
            else
            {
                Log.Warning($"No results retrieved, ignored {ignored} results (previous to {startTime.ToLongTimeString()}).");
            }
            return results;
        }
    }
}
