// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain
//
// This file cannot be modified or redistributed. This header cannot be removed.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using OpenTap;
using Tap.Plugins.UMA.AdbAgents.Instruments;
using Tap.Plugins.UMA.AdbAgents.Results;

using RoleEnum = Tap.Plugins.UMA.AdbAgents.Instruments.iPerfAgentInstrument.RoleEnum;

namespace Tap.Plugins.UMA.AdbAgents.Steps
{
    [Display("Adb iPerf Agent", Groups: new string[] { "UMA", "Agents" })]
    public class AdbiPerfAgentStep : AdbAgentBaseStep
    {
        private static readonly string DEVICE_FILE = "sdcard/adb_iperf_agent.log";
        private static readonly string AGENT_TAG_CLIENT = "iperf.Client";
        private static readonly string AGENT_TAG_SERVER = "iperf.Server";

        [Display("Agent", Order: 1.0)]
        public iPerfAgentInstrument Instrument { get; set; }

        #region Parameters

        [XmlIgnore]
        public bool HasParameters { get { return (Action == ActionEnum.Start || Action == ActionEnum.Measure); } }

        [Display("Role", Group: "Parameters", Order: 2.0)]
        public RoleEnum Role { get; set; }

        [EnabledIf("HasParameters", true, HideIfDisabled = true)]
        [EnabledIf("Role", RoleEnum.Client, HideIfDisabled = true)]
        [Display("Host", Group: "Parameters", Order: 2.1)]
        public string Host { get; set; }

        [EnabledIf("HasParameters", true, HideIfDisabled = true)]
        [Display("Port", Group: "Parameters", Order: 2.2)]
        public int Port { get; set; }

        [EnabledIf("HasParameters", true, HideIfDisabled = true)]
        [Display("Parallel", Group: "Parameters", Order: 2.3)]
        public int Parallel { get; set; }

        [EnabledIf("HasParameters", true, HideIfDisabled = true)]
        [Display("Extra Parameters", Group: "Parameters", Order: 2.4,
            Description: "Extra parameters to pass to iPerf. Separate parameters with ';', separate keys/values with space\n" +
                         "Example: '-P 4; -u; -B 192.168.2.1'")]
        public string ExtraParameters { get; set; }

        #region Measurement

        // Measurement properties have order 50.0 and 50.1
        public override bool HideMeasurement { get { return Action != ActionEnum.Measure; } }

        #endregion

        #endregion

        public AdbiPerfAgentStep() : base()
        {
            Action = ActionEnum.Measure;
            MeasurementMode = WaitMode.Time;
            MeasurementTime = 4.0;
            Parallel = 1;

            Role = RoleEnum.Client;
            Host = "127.0.0.1";
            Port = 5001;
            ExtraParameters = string.Empty;

            Rules.Add(() => (Parallel > 0), "Must be greater than 0, use 1 to disable parallel.", "Parallel");
        }

        public override void Run()
        {
            DoRun(Instrument.Adb, DEVICE_FILE, (Role == RoleEnum.Client) ? AGENT_TAG_CLIENT : AGENT_TAG_SERVER);
        }

        protected override void StartAgent()
        {
            Instrument.Start(Role, Host, Port, Parallel, ExtraParameters);
        }

        protected override void StopAgent()
        {
            Instrument.Stop(Role);
        }

        protected override void ParseResults(string[] logcatOutput, DateTime startTime)
        {
            parseResults<iPerfResult>("ADB iPerf Agent", filterLogcat(logcatOutput), startTime);
        }

        private string[] filterLogcat(string[] logcatOutput)
        {
            List<string> res = new List<string>();
            bool isParallel = (Parallel > 1);

            foreach (string line in logcatOutput)
            {
                if (isParallel == iPerfResult.IsSumResult(line))
                {
                    res.Add(line);
                }
            }

            return res.ToArray();
        }
    }
}
