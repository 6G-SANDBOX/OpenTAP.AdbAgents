// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2022 Universidad de Málaga (University of Málaga), Spain
//
// This file cannot be modified or redistributed. This header cannot be removed.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tap.Plugins.UMA.AdbAgents.Results
{
    public class ExoplayerResult : ResultBase
    {
        public enum KindEnum { MeasurementPoint, Video, Audio }

        private static readonly string MEAS_POINT_TIME = @"(\d+-\d+-\d+T\d+:\d+:\d+.\d+)";
        private static Regex regex = new Regex( $@"{DATETIME}.*TriangleInstr: {MEAS_POINT_TIME}\s(.*)", RegexOptions.Compiled);
        private static Regex exoplayerInfo = new Regex( @"""(audio|video)/(.*)\((.*)\)""", RegexOptions.Compiled);
        private static char[] space = new char[] { ' ' };
        private static char[] colon = new char[] { ':' };
        private static char[] x = new char[] { 'x' };
        private static char[] tab = new char[] { '\t' };

        public string UseCase;
        public string Feature;
        public string MeasurementPoint;
        public KindEnum Kind;
        public Dictionary<string, IConvertible> ExtraValues;
        
        public ExoplayerResult()
        {
            LogTime = DateTime.MinValue;
            Timestamp = 0;
            UseCase = Feature = MeasurementPoint = string.Empty;
            ExtraValues = new Dictionary<string, IConvertible>();

            Valid = false;
        }

        public override void Parse(string line)
        {
            Match match = regex.Match(line);

            if (match.Success)
            {
                LogTime = logcatDate(match.Groups[1].Value);
                Timestamp = measPointTimestamp(match.Groups[2].Value);
                
                Valid = parseType(match.Groups[3].Value);
            }
        }

        private ulong measPointTimestamp(string value)
        {
            DateTime datetime = DateTime.ParseExact(value, "yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
            DateTimeOffset offset = new DateTimeOffset(datetime);
            return (ulong)offset.ToUnixTimeMilliseconds();
        }

        private bool parseType(string message)
        {
            string[] pieces = message.Split(new char[] { '\t' });
            if (pieces.Length >= 3)
            {
                UseCase = pieces[0];
                Feature = pieces[1];
                MeasurementPoint = pieces[2];

                switch (UseCase)
                {
                    case "Co":
                    case "Cs": Kind = KindEnum.MeasurementPoint; break;
                    case "Custom":
                        if (Feature == "ExoplayerInfo" && MeasurementPoint == "Video Information")
                        {
                            Kind = KindEnum.Video;
                        }
                        else if (Feature == "ExoplayerInfo" && MeasurementPoint == "Audio Information")
                        {
                            Kind = KindEnum.Audio;
                        }
                        else
                        {
                            return false;
                        }
                        break;
                    default: return false;
                }

                if (pieces.Length >= 4)
                {
                    // Handle the rest of the values (any extra parameter on measurement points or video/audio information)
                    return parseMessage(pieces.Skip(3).ToArray());
                }
                else { return true; }
            }
            return false;
        }

        public bool parseMessage(params string[] values)
        {
            if (Kind == KindEnum.MeasurementPoint)
            {
                for (int i = 1; i <= values.Length; i++)
                {
                    ExtraValues[$"Value {i}"] = values[i-1];
                }
                return true;
            }
            else
            {
                if (values.Length != 0)
                {
                    Match match = exoplayerInfo.Match(values[0]);

                    if (match.Success)
                    {
                        ExtraValues["codec"] = match.Groups[2].Value;

                        return parseExtras(match.Groups[3].Value);
                    }
                }
                return false;
            }
        }

        private bool parseExtras(string values)
        {
            string[] extras = values.Split(space, StringSplitOptions.RemoveEmptyEntries);

            foreach (string extra in extras)
            {
                try
                {
                    string[] pieces = extra.Split(colon);
                    string key = pieces[0];
                    string strValue = pieces[1];

                    if (Kind == KindEnum.Video && key == "r")
                    {
                        pieces = strValue.Split(x);
                        ulong w = ulong.Parse(pieces[0]);
                        ulong h = ulong.Parse(pieces[1]);
                        ExtraValues["width"] = w;
                        ExtraValues["height"] = h;
                        ExtraValues["pixel count"] = w * h;
                    }
                    else
                    {
                        double? dValue = maybeDouble(strValue);
                        ExtraValues[key] = dValue.HasValue ? (IConvertible)dValue.Value : strValue;
                    }
                }
                catch { return false; }
            }
            return true;
        }

        #region Unused 
        // Since there are multiple kinds of values this logic should not be used

        public override string[] GetColumns() { return new string[] { }; }

        public override IConvertible GetValue(string column) { throw new Exception("You are not supposed to use ExoplayerResult.GetValue()"); }

        #endregion
    }
}
