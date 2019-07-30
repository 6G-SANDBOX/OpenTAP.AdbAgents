// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain
//
// This file cannot be modified or redistributed. This header cannot be removed.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Keysight.Tap;
using Tap.Plugins.UMA.Android.Instruments;

namespace Tap.Plugins.UMA.AdbAgents.Instruments
{
    [Display("Resource Agent", Groups: new string[] { "UMA", "Adb Agents" },
        Description: "Instrument for controlling the Adb Resource Agent")]
    [ShortName("ADB_Res")]
    public class ResourceAgentInstrument: Instrument
    {
        [Display("Adb Instrument")]
        public AdbInstrument Adb { get; set; }
    }
}
