﻿// Author:      Bruno Garcia Garcia <bgg@uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain

using OpenTap;
using Tap.Plugins.UMA.Android.Instruments;

namespace Tap.Plugins.UMA.AdbAgents.Instruments
{
    [Display("Resource Agent", Groups: new string[] { "Adb Agents" },
        Description: "Instrument for controlling the Adb Resource Agent")]
    public class ResourceAgentInstrument: Instrument
    {
        private const string PACKAGE = "com.uma.resourceAgent";
        private const string SERVICE = PACKAGE + "/.ResourceAgentService";
        private const string ACTIVITY = PACKAGE + "/" + PACKAGE + ".ResourceAgentActivity";
        private const string START = PACKAGE + ".START";
        private const string STOP = PACKAGE + ".STOP";
        private const string ACTIVITY_SINGLE_TOP = "0x20000000";

        [Display("Adb Instrument")]
        public AdbInstrument Adb { get; set; }

        public ResourceAgentInstrument() : base()
        {
            Name = "ADB_Res";
        }

        public void Start(string DeviceId)
        {
            Adb.ExecuteAdbCommand("shell am start -n " + ACTIVITY + " -f " + ACTIVITY_SINGLE_TOP, DeviceId);
            TapThread.Sleep(500);
            Adb.ExecuteAdbCommand(parameters(START), DeviceId);
        }

        public void Stop(string DeviceId)
        {
            // Bring to top
            Adb.ExecuteAdbCommand("shell am start -n " + ACTIVITY + " -f " + ACTIVITY_SINGLE_TOP, DeviceId);
            TapThread.Sleep(500);
            // Send stop intent
            Adb.ExecuteAdbCommand(parameters(STOP), DeviceId);
        }

        private string parameters(string intent)
        {
            return $"shell am startservice -n {SERVICE} -a {intent} --user 0";
        }
    }
}
