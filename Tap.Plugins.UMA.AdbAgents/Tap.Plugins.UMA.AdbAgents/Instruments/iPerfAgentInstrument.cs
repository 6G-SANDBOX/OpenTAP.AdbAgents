// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain

using System;
using System.Collections.Generic;
using System.Linq;

using OpenTap;
using Tap.Plugins.UMA.Android.Instruments;

namespace Tap.Plugins.UMA.AdbAgents.Instruments
{
    [Display("iPerf Agent", Groups: new string[] { "UMA", "Adb Agents" },
             Description: "Instrument for controlling the Adb Ping Agent")]
    public class iPerfAgentInstrument : Instrument
    {
        public enum RoleEnum { Client, Server }

        private const string PACKAGE = "com.uma.iperf";
        private const string SERVICE = PACKAGE + "/.iPerfService";
        private const string ACTIVITY = PACKAGE + "/" + PACKAGE + ".iPerfActivity";
        private const string CLIENT_START = PACKAGE + ".CLIENTSTART";
        private const string CLIENT_STOP = PACKAGE + ".CLIENTSTOP";
        private const string SERVER_START = PACKAGE + ".SERVERSTART";
        private const string SERVER_STOP = PACKAGE + ".SERVERSTOP";
        private const string EXTRA = PACKAGE + ".PARAMETERS";
        private const string ACTIVITY_SINGLE_TOP = "0x20000000";
        private static readonly List<string> ignoredKeys = new List<string> { "-c", "-s", "-p", "-t", "-i", "-f", "-u" };

        [Display("Adb Instrument")]
        public AdbInstrument Adb { get; set; }

        public iPerfAgentInstrument() : base()
        {
            Name = "ADB_iPerf";
        }

        public void Start(RoleEnum role, string host, int port, int parallel, bool udp, string extra, string DeviceId = null)
        {
            string start = (role == RoleEnum.Client) ? CLIENT_START : SERVER_START;
            string cmd = iPerfParameters(role, host, port, parallel, udp, extra);

            Adb.ExecuteAdbCommand("shell am start -n " + ACTIVITY + " -f " + ACTIVITY_SINGLE_TOP, DeviceId);
            TapThread.Sleep(500);
            Adb.ExecuteAdbCommand(parameters(start, extras: cmd), DeviceId);
        }

        public void Stop(RoleEnum role, string DeviceId = null)
        {
            string stop = (role == RoleEnum.Client) ? CLIENT_STOP : SERVER_STOP;
            // Bring to top
            Adb.ExecuteAdbCommand("shell am start -n " + ACTIVITY + " -f " + ACTIVITY_SINGLE_TOP, DeviceId);
            TapThread.Sleep(500);
            // Send stop intent
            Adb.ExecuteAdbCommand(parameters(stop, extras: null), DeviceId);
        }

        private string parameters(string intent, string extras = null)
        {
            string res = $"shell am startservice -n {SERVICE} -a {intent}";
            if (extras != null) { res += $" -e {EXTRA} \"{extras}\""; }
            return res += " --user 0";
        }

        private string iPerfParameters(RoleEnum role, string host, int port, int parallel, bool udp, string extra)
        {
            Dictionary<string, string> dict = parsedParameters(role, host, port, parallel, udp, extra);
            List<string> parameters = new List<string>();

            foreach (var pair in dict)
            {
                parameters.Add(string.IsNullOrWhiteSpace(pair.Value) ? pair.Key : $"{pair.Key},{pair.Value}");
            }

            return string.Join(",", parameters);
        }

        private Dictionary<string, string> parsedParameters(RoleEnum role, string host, int port, int parallel, bool udp, string extra)
        {
            char[] semicolon = new char[] { ';' };
            char[] space = new char[] { ' ' };
            Dictionary<string, string> res = new Dictionary<string, string>();
            List<string> ignored = new List<string>();
            

            switch (role)
            {
                case RoleEnum.Client: res.Add("-c", host); break;
                case RoleEnum.Server: res.Add("-s", ""); break;
            }

            res.Add("-p", port.ToString());
            res.Add("-t", "999999");
            res.Add("-i", "1");
            res.Add("-f", "m");

            if (parallel > 1) { res.Add("-P", parallel.ToString()); }
            if (udp) { res.Add("-u", string.Empty); }

            foreach (string parameter in extra.Split(semicolon, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] tokens = parameter.Split(space, StringSplitOptions.RemoveEmptyEntries);
                string key = tokens[0].Trim();
                string value = tokens.Count() > 1 ? tokens[1].Trim() : "";

                if (ignoredKeys.Contains(key))
                {
                    ignored.Add(key);
                }
                else
                {
                    res.Add(key, value);
                }
            }

            if (ignored.Count != 0)
            {
                Log.Warning($"Found restricted extra parameters: {string.Join(", ", ignored)}. Ignored.");
            }

            return res;
        }
    }
}
