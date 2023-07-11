// Author:      Bruno Garcia Garcia <bgg@uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain

using OpenTap;

namespace Tap.Plugins.UMA.AdbAgents.Steps
{
    public abstract class MeasurementStepBase : TestStep
    {
        public enum WaitMode { Time, Children }

        [EnabledIf("HideMeasurement", false, HideIfDisabled = true)]
        [Display("Wait Mode", Group: "Measurement", Order: 50.0)]
        public WaitMode MeasurementMode { get; set; }

        [EnabledIf("HideMeasurement", false, HideIfDisabled = true)]
        [EnabledIf("MeasurementMode", WaitMode.Time, HideIfDisabled = true)]
        [Display("Time", Group: "Measurement", Order: 50.1)]
        [Unit("s")]
        public double MeasurementTime { get; set; }

        public abstract bool HideMeasurement { get; }

        public void MeasurementWait()
        {
            switch (MeasurementMode)
            {
                case WaitMode.Time:
                    TapThread.Sleep((int)(MeasurementTime * 1000));
                    break;
                default:
                    RunChildSteps();
                    break;
            }
        }
    }
}
