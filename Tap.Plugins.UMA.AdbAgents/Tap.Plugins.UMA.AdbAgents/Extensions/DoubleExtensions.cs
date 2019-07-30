﻿// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2020 Universidad de Málaga (University of Málaga), Spain
//
// This file cannot be modified or redistributed. This header cannot be removed.

using System;

namespace Tap.Plugins.UMA.Extensions
{
    public static class DoubleExtensions
    {
        public static DateTime ToDateTime(this double timestamp)
        {
            DateTimeOffset offset = DateTimeOffset.FromUnixTimeMilliseconds((long)(timestamp * 1000));
            return offset.UtcDateTime;
        }
    }
}