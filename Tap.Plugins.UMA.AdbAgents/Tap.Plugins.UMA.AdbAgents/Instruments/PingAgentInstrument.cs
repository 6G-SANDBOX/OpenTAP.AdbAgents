// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain

using OpenTap;
using Tap.Plugins.UMA.Android.Instruments;

namespace Tap.Plugins.UMA.AdbAgents.Instruments
{
    [Display("Ping Agent", Groups: new string[] { "UMA", "Adb Agents" },
        Description: "Instrument for controlling the Adb Ping Agent")]
    public class PingAgentInstrument : Instrument
    {
        private const string PACKAGE = "com.uma.ping";
        private const string SERVICE = PACKAGE + "/.PingService";
        private const string ACTIVITY = PACKAGE + "/" + PACKAGE + ".PingActivity";
        private const string START = PACKAGE + ".START";
        private const string STOP = PACKAGE + ".STOP";
        private const string EXTRA = PACKAGE + ".PARAMETERS";
        private const string ACTIVITY_SINGLE_TOP = "0x20000000";

        [Display("Adb Instrument")]
        public AdbInstrument Adb { get; set; }

        public PingAgentInstrument() : base()
        {
            Name = "ADB_Ping";
        }

        public void Start(string target, int ttl, string DeviceId)
        {
            Adb.ExecuteAdbCommand("shell am start -n " + ACTIVITY + " -f " + ACTIVITY_SINGLE_TOP, DeviceId);
            TapThread.Sleep(500);
            Adb.ExecuteAdbCommand(parameters(START, extras(target, ttl)), DeviceId);
        }

        public void Stop(string DeviceId)
        {
            // Bring to top
            Adb.ExecuteAdbCommand("shell am start -n " + ACTIVITY + " -f " + ACTIVITY_SINGLE_TOP, DeviceId);
            TapThread.Sleep(500);
            // Send stop intent
            Adb.ExecuteAdbCommand(parameters(STOP, extras: null), DeviceId);
        }

        private string parameters(string intent, string extras = null)
        {
            string res = $"shell am startservice -n {SERVICE} -a {intent}";
            if (extras != null) { res += $" -e {EXTRA} \"{extras}\""; }
            return res += " --user 0";
        }

        private string extras(string target, int ttl)
        {
            return $"target={target},ttl={ttl}";
        }
    }
}
