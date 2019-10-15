// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain
//
// This file cannot be modified or redistributed. This header cannot be removed.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTap;
using Tap.Plugins.UMA.Android.Instruments;

namespace Tap.Plugins.UMA.AdbAgents.Instruments
{
    [Display("Resource Agent", Groups: new string[] { "UMA", "Adb Agents" },
        Description: "Instrument for controlling the Adb Resource Agent")]
    public class ResourceAgentInstrument: Instrument
    {
        private const string PACKAGE = "com.uma.resourceAgent";
        private const string SERVICE = PACKAGE + "/.ResourceAgentService";
        private const string START = PACKAGE + ".START";
        private const string STOP = PACKAGE + ".STOP";

        [Display("Adb Instrument")]
        public AdbInstrument Adb { get; set; }

        public ResourceAgentInstrument() : base()
        {
            Name = "ADB_Res";
        }

        public void Start(string DeviceId = null)
        {
            Adb.ExecuteAdbCommand(parameters(START), DeviceId);
        }

        public void Stop(string DeviceId = null)
        {
            Adb.ExecuteAdbCommand(parameters(STOP), DeviceId);
        }

        private string parameters(string intent)
        {
            return $"shell am startservice -n {SERVICE} -a {intent} --user 0";
        }
    }
}
