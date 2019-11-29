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
    }
}
