// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain

using System;
using System.Globalization;

namespace Tap.Plugins.UMA.AdbAgents.Results
{
    public abstract class ResultBase
    {
        protected static readonly string DATETIME = @"(\d+-\d+ \d+:\d+:\d+.\d+)";
        protected static readonly string FLOAT = @"((\d+)([.,]\d+)*)";
        protected static readonly string INT = @"(\d+)";

        public abstract string[] GetColumns();

        public bool Valid;
        public DateTime LogTime;
        public ulong Timestamp;

        public abstract void Parse(string line);

        protected static int? maybeInt(string value)
        {
            if (int.TryParse(value, out int result)) { return result; }
            return null;
        }

        protected static double? maybeDouble(string value)
        {
            if (double.TryParse(value, out double result)) { return result; }
            return null;
        }

        protected DateTime logcatDate(string value)
        {
            return DateTime.ParseExact(value, "MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }

        public abstract IConvertible GetValue(string column);
    }
}
