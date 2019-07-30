﻿// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2020 Universidad de Málaga (University of Málaga), Spain
//
// This file cannot be modified or redistributed. This header cannot be removed.

using System;

namespace Tap.Plugins.UMA.Extensions
{
    public static class DateTimeExtensions
    {
        public static long ToUnixTimestamp(this DateTime datetime)
        {
            DateTimeOffset offset = new DateTimeOffset(datetime);
            return offset.ToUnixTimeMilliseconds();
        }

        public static long ToUnixUtcTimestamp(this DateTime datetime)
        {
            DateTimeOffset offset = new DateTimeOffset(datetime.ToUniversalTime());
            return offset.ToUnixTimeMilliseconds();
        }
    }
}
