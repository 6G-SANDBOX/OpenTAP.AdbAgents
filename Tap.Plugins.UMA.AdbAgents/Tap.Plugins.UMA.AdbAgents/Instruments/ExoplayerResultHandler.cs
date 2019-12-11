// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain
//
// This file cannot be modified or redistributed. This header cannot be removed.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using OpenTap;

namespace Tap.Plugins.UMA.AdbAgents.Results
{
    public static class ExoplayerResultHandler
    {
        private static Tuple<string, string, string>[] delayDefinitions = new Tuple<string, string, string>[] {
            new Tuple<string, string, string>(
                "Time to load first media frame", "Media File Playback - Start", "Media File Playback - First Picture"),
            new Tuple<string, string, string>(
                "Open the AUT", "App Initialization Start - Login Not Required", "App Started")
        };

        private class MeasurementPoint
        {
            public ulong Timestamp;
            public string Name;

            public MeasurementPoint(ExoplayerResult result)
            {
                this.Timestamp = result.Timestamp;
                this.Name = result.MeasurementPoint;
            }
        }

        public static ResultTable GetTableFromList(string name, List<ExoplayerResult> results)
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

            // Prepare columns, return table
            List<ResultColumn> resultColumns = new List<ResultColumn>();
            resultColumns.Add(new ResultColumn("Timestamp", timestamps.ToArray()));
            foreach (var keyValue in columns)
            {
                resultColumns.Add(new ResultColumn(keyValue.Key, keyValue.Value.ToArray()));
            }

            return new ResultTable(name, resultColumns.ToArray());
        }

        public static List<ResultTable> GetUserExperienceTables(List<ExoplayerResult> results)
        {
            List<ResultTable> res = new List<ResultTable>();
            List<MeasurementPoint> measurementPoints = results.Select((r) => new MeasurementPoint(r)).ToList();

            foreach (var definition in delayDefinitions)
            {
                string name = definition.Item1;
                string start = definition.Item2;
                string end = definition.Item3;
                List<Tuple<ulong, double>> delayTuples = getDelays(start, end, measurementPoints);

                if (delayTuples.Count != 0)
                {
                    List<ulong> timestamps = new List<ulong>();
                    List<double> delays = new List<double>();

                    foreach (var item in delayTuples)
                    {
                        timestamps.Add(item.Item1);
                        delays.Add(item.Item2);
                    }

                    ResultTable table = new ResultTable(name, new ResultColumn[] {
                        new ResultColumn("Timestamp", timestamps.ToArray()),
                        new ResultColumn("Delay", delays.ToArray())
                    });

                    res.Add(table);
                }
            }

            return res;
        }

        private static List<Tuple<ulong, double>> getDelays(string start, string end, List<MeasurementPoint> points)
        {
            List<Tuple<ulong, double>> res = new List<Tuple<ulong, double>>();

            ulong? startTime = null;

            foreach (MeasurementPoint point in points)
            {
                if (point.Name == start)
                {
                    startTime = point.Timestamp;
                }
                else if (point.Name == end)
                {
                    if (startTime.HasValue)
                    {
                        res.Add(new Tuple<ulong, double>(
                            (startTime.Value + point.Timestamp) / 2,
                            (point.Timestamp - startTime.Value) / 1000.0 ));

                        startTime = null;
                    }
                }
            }
            return res;
        }
    }
}
