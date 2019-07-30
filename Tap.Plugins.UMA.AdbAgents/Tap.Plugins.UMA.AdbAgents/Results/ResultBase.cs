// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain
//
// This file cannot be modified or redistributed. This header cannot be removed.

using System;

namespace Tap.Plugins.UMA.AdbAgents.Results
{
    public abstract class ResultBase
    {
        public abstract string[] GetColumns();

        public bool Valid;
        public DateTime LogTime;
        public ulong Timestamp;

        protected static int? maybeInt(string value)
        {
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return null;
        }

        public abstract IConvertible GetValue(string column);
    }
}
