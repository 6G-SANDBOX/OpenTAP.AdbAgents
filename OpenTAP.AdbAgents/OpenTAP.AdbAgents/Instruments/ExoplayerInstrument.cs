// Author:      Bruno Garcia Garcia <bgg@uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain

using System;
using System.Collections.Generic;

using OpenTap;
using Tap.Plugins.UMA.Android.Instruments;

namespace Tap.Plugins.UMA.AdbAgents.Instruments
{
    public class KeyEvent
    {
        public enum ActionEnum { KeyPress, Wait }

        [Display("Action", Order: 1.0)]
        public ActionEnum Action { get; set; }

        [Display("Key Code/Delay", Order: 1.0, Description: "https://developer.android.com/reference/android/view/KeyEvent.html")]
        public int Code { get; set; }

        [Display("Repeat", Order: 2.0)]
        public int Repeat { get; set; }

        [Display("Comment", Order: 3.0)]
        public string Comment { get; set; }

        public KeyEvent() { }

        public KeyEvent(ActionEnum action, int code, int repeat, string comment)
        {
            this.Action = action; this.Code = code; this.Repeat = repeat; this.Comment = comment;
        }
    }

    [Display("Exoplayer", Groups: new string[] { "Adb Agents" },
        Description: "Instrument for controlling the Exoplayer")]
    public class ExoplayerInstrument: Instrument
    {
        private const string PACKAGE = "com.google.android.exoplayer2.demo";
        private const string ACTIVITY = "SampleChooserActivity";
        public const string AXINOM = "https://raw.githubusercontent.com/Axinom/dash-test-vectors/master/axinom.exolist.json";

        [Display("Adb Instrument")]
        public AdbInstrument Adb { get; set; }

        public ExoplayerInstrument() : base()
        {
            Name = "Exoplayer";
        }

        public void Start(string DeviceId, string exolist = null, List<KeyEvent> keys = null)
        {
            string command = $"shell am start -n {PACKAGE}/.{ACTIVITY}";
            if (!string.IsNullOrWhiteSpace(exolist))
            {
                command += $" -d {exolist}";
            }

            Adb.ExecuteAdbCommand(command, DeviceId);
            TapThread.Sleep(3000);
            if (keys != null) { PressKeys(keys, DeviceId); }
        }

        public void Stop(string DeviceId)
        {
            Adb.ExecuteAdbCommand($"shell am force-stop {PACKAGE}", DeviceId);
            TapThread.Sleep(500);
        }

        public void PressKeys(List<KeyEvent> keys, string deviceId)
        {
            foreach (KeyEvent key in keys)
            {
                for (int i = 1; i <= key.Repeat; i++)
                {
                    switch (key.Action)
                    {
                        case KeyEvent.ActionEnum.KeyPress:
                            Log.Debug($"Pressing key {key.Code} (repeat {i}) '{key.Comment}'");
                            Adb.ExecuteAdbCommand($"shell input keyevent {key.Code}", deviceId);
                            TapThread.Sleep(500);
                            break;
                        case KeyEvent.ActionEnum.Wait:
                            Log.Debug($"Waiting {key.Code}ms (repeat {i}) '{key.Comment}'");
                            TapThread.Sleep(key.Code);
                            break;
                        default:
                            throw new ArgumentException($"Unrecognized KeyEvent.ActionEnum: {key.Action}");
                    }
                }
            }
        }
    }
}
