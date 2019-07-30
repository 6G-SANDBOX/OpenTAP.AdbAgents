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
    public class AdbResourceAgentStep : TestStep
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
                logcat.Terminate();

                string[] res = Instrument.Adb.RetrieveLogcat(logcat, localFilename: null);

                parseResults(res);

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

        public struct ResourcesResult
        {
            private static readonly string FLOAT = @"((\d+)([.,]\d+)?)";
            private static readonly string INT = @"(\d+)";

            private static Regex regex = new Regex(
                $".*<<< Elapsed time .* sec ; Timestamp {INT} ; CPU usage {FLOAT}% ; Ram used {INT}MBs ; Available Ram {INT}MBs ; Packets Received {INT} ; Packets Transmitted {INT} ; Bytes Received {INT} ; Bytes Transmitted {INT} >>>.*", 
                RegexOptions.Compiled);

            public static string[] COLUMNS = new string[] {
                "Timestamp", "Used CPU Per Cent", "Used RAM", "Available RAM", "Total RAM",
                "Used RAM Per Cent", "Packets Sent", "PacketsReceived", "Bytes Sent", "Bytes Received" };

            public bool Valid;
            public ulong Timestamp;
            public double UsedCpuPerCent;
            public int UsedRam;
            public int AvailableRam;
            public int TotalRam;
            public double UsedRamPerCent;
            public int PacketsSent;
            public int PacketsReceived;
            public int BytesSent;
            public int BytesReceived;

            public ResourcesResult(string line)
            {
                Match match = regex.Match(line);

                if (match.Success)
                {
                    Timestamp = ulong.Parse(match.Groups[1].Value);
                    UsedCpuPerCent = double.Parse(match.Groups[2].Value);
                    UsedRam = int.Parse(match.Groups[5].Value);
                    AvailableRam = int.Parse(match.Groups[6].Value);
                    TotalRam = UsedRam + AvailableRam;
                    UsedRamPerCent = (TotalRam / 100.0) * UsedRam;
                    PacketsReceived = int.Parse(match.Groups[7].Value);
                    PacketsSent = int.Parse(match.Groups[8].Value);
                    BytesReceived = int.Parse(match.Groups[9].Value); 
                    BytesSent = int.Parse(match.Groups[10].Value);
                    Valid = true;
                }
                else
                {
                    Timestamp = 0;
                    UsedCpuPerCent = UsedRamPerCent = 0.0;
                    UsedRam = AvailableRam = TotalRam = PacketsSent = PacketsReceived = BytesSent = BytesReceived = 0;
                    Valid = false;
                }
            }

            public IConvertible GetValue(string column)
            {
                switch (column)
                {
                    case "Timestamp": return Timestamp;
                    case "Used CPU Per Cent": return UsedCpuPerCent;
                    case "Used RAM": return UsedRam;
                    case "Available RAM": return AvailableRam;
                    case "Total RAM": return TotalRam;
                    case "Used RAM Per Cent": return UsedRamPerCent;
                    case "Packets Sent": return PacketsSent;
                    case "PacketsReceived": return PacketsReceived;
                    case "Bytes Sent": return BytesSent;
                    case "Bytes Received": return BytesReceived;
                    default: throw new Exception($"Unrecognized column '{column}'");
                }
            }
        }

        private void parseResults(string[] logcat)
        {
            Dictionary<string, List<IConvertible>> resultLists = new Dictionary<string, List<IConvertible>>();
            foreach (string column in ResourcesResult.COLUMNS)
            {
                resultLists[column] = new List<IConvertible>();
            }

            foreach (string line in logcat)
            {
                ResourcesResult resources = new ResourcesResult(line);

                if (resources.Valid)
                {
                    foreach (string column in ResourcesResult.COLUMNS)
                    {
                        resultLists[column].Add(resources.GetValue(column));
                    }
                }
                else
                {
                    Log.Warning($"Could not parse logcat line '{line}'");
                }
            }

            List<ResultColumn> resultColumns = new List<ResultColumn>();

            foreach (string column in ResourcesResult.COLUMNS)
            {
                resultColumns.Add(new ResultColumn(column, resultLists[column].ToArray()));
            }

            ResultTable table = new ResultTable("ADB Resource Agent", resultColumns.ToArray());
            table.PublishToSource(Results);
        }
    }
}
