/**
 * Utilities.cs
 * 
 * Thunder Aerospace Corporation's library for the Kerbal Space Program, by Taranis Elsu
 * 
 * (C) Copyright 2013, Taranis Elsu
 * 
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 * 
 * This code is licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 3.0)
 * creative commons license. See <http://creativecommons.org/licenses/by-nc-sa/3.0/legalcode>
 * for full details.
 * 
 * Attribution — You are free to modify this code, so long as you mention that the resulting
 * work is based upon or adapted from this code.
 * 
 * Non-commercial - You may not use this work for commercial purposes.
 * 
 * Share Alike — If you alter, transform, or build upon this work, you may distribute the
 * resulting work only under the same or similar license to the CC BY-NC-SA 3.0 license.
 * 
 * Note that Thunder Aerospace Corporation is a ficticious entity created for entertainment
 * purposes. It is in no way meant to represent a real entity. Any similarity to a real entity
 * is purely coincidental.
 */

using KSP.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KolonyTools
{
    public static class Utilities
    {
        const double SECONDS_PER_MINUTE = 60.0;
        const double MINUTES_PER_HOUR = 60.0;
        static double HOURS_PER_DAY = (GameSettings.KERBIN_TIME) ? 6.0 : 24.0;
        public static double SECONDS_PER_DAY = SECONDS_PER_MINUTE*MINUTES_PER_HOUR*HOURS_PER_DAY;

        public static double ToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        public static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        public static Rect EnsureVisible(Rect pos, float min = 16.0f)
        {
            float xMin = min - pos.width;
            float xMax = Screen.width - min;
            float yMin = min - pos.height;
            float yMax = Screen.height - min;

            pos.x = Mathf.Clamp(pos.x, xMin, xMax);
            pos.y = Mathf.Clamp(pos.y, yMin, yMax);

            return pos;
        }

        public static Rect EnsureCompletelyVisible(Rect pos)
        {
            float xMin = 0;
            float xMax = Screen.width - pos.width;
            float yMin = 0;
            float yMax = Screen.height - pos.height;

            pos.x = Mathf.Clamp(pos.x, xMin, xMax);
            pos.y = Mathf.Clamp(pos.y, yMin, yMax);

            return pos;
        }

        public static Rect ClampToScreenEdge(Rect pos)
        {
            float topSeparation = Math.Abs(pos.y);
            float bottomSeparation = Math.Abs(Screen.height - pos.y - pos.height);
            float leftSeparation = Math.Abs(pos.x);
            float rightSeparation = Math.Abs(Screen.width - pos.x - pos.width);

            if (topSeparation <= bottomSeparation && topSeparation <= leftSeparation && topSeparation <= rightSeparation)
            {
                pos.y = 0;
            }
            else if (leftSeparation <= topSeparation && leftSeparation <= bottomSeparation && leftSeparation <= rightSeparation)
            {
                pos.x = 0;
            }
            else if (bottomSeparation <= topSeparation && bottomSeparation <= leftSeparation && bottomSeparation <= rightSeparation)
            {
                pos.y = Screen.height - pos.height;
            }
            else if (rightSeparation <= topSeparation && rightSeparation <= bottomSeparation && rightSeparation <= leftSeparation)
            {
                pos.x = Screen.width - pos.width;
            }

            return pos;
        }

        public static Texture2D LoadImage<T>(string filename)
        {
            if (File.Exists<T>(filename))
            {
                var bytes = File.ReadAllBytes<T>(filename);
                Texture2D texture = new Texture2D(16, 16, TextureFormat.ARGB32, false);
                texture.LoadImage(bytes);
                return texture;
            }
            else
            {
                return null;
            }
        }

        public static bool GetValue(ConfigNode config, string name, bool currentValue)
        {
            bool newValue;
            if (config.HasValue(name) && bool.TryParse(config.GetValue(name), out newValue))
            {
                return newValue;
            }
            else
            {
                return currentValue;
            }
        }

        public static int GetValue(ConfigNode config, string name, int currentValue)
        {
            int newValue;
            if (config.HasValue(name) && int.TryParse(config.GetValue(name), out newValue))
            {
                return newValue;
            }
            else
            {
                return currentValue;
            }
        }

        public static float GetValue(ConfigNode config, string name, float currentValue)
        {
            float newValue;
            if (config.HasValue(name) && float.TryParse(config.GetValue(name), out newValue))
            {
                return newValue;
            }
            else
            {
                return currentValue;
            }
        }

        public static double GetValue(ConfigNode config, string name, double currentValue)
        {
            double newValue;
            if (config.HasValue(name) && double.TryParse(config.GetValue(name), out newValue))
            {
                return newValue;
            }
            else
            {
                return currentValue;
            }
        }

        public static string GetValue(ConfigNode config, string name, string currentValue)
        {
            if (config.HasValue(name))
            {
                return config.GetValue(name);
            }
            else
            {
                return currentValue;
            }
        }

        public static T GetValue<T>(ConfigNode config, string name, T currentValue) where T : IComparable, IFormattable, IConvertible
        {
            if (config.HasValue(name))
            {
                string stringValue = config.GetValue(name);
                if (Enum.IsDefined(typeof(T), stringValue))
                {
                    return (T)Enum.Parse(typeof(T), stringValue);
                }
            }

            return currentValue;
        }

        public static double ShowTextField(string label, GUIStyle labelStyle, double currentValue, int maxLength, GUIStyle editStyle, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle);
            GUILayout.FlexibleSpace();
            string result = GUILayout.TextField(currentValue.ToString(), maxLength, editStyle, options);
            GUILayout.EndHorizontal();

            double newValue;
            if (double.TryParse(result, out newValue))
            {
                return newValue;
            }
            else
            {
                return currentValue;
            }
        }

        public static double ShowTextField(double currentValue, int maxLength, GUIStyle style, params GUILayoutOption[] options)
        {
            double newValue;
            string result = GUILayout.TextField(currentValue.ToString(), maxLength, style, options);
            if (double.TryParse(result, out newValue))
            {
                return newValue;
            }
            else
            {
                return currentValue;
            }
        }

        public static float ShowTextField(float currentValue, int maxLength, GUIStyle style, params GUILayoutOption[] options)
        {
            float newValue;
            string result = GUILayout.TextField(currentValue.ToString(), maxLength, style, options);
            if (float.TryParse(result, out newValue))
            {
                return newValue;
            }
            else
            {
                return currentValue;
            }
        }

        public static bool ShowToggle(string label, GUIStyle labelStyle, bool currentValue)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle);
            GUILayout.FlexibleSpace();
            bool result = GUILayout.Toggle(currentValue, "");
            GUILayout.EndHorizontal();

            return result;
        }

        public static string FormatTime(double value, int numDecimals = 0)
        {


            string sign = "";
            if (value < 0.0)
            {
                sign = "-";
                value = -value;
            }

            double seconds = value;

            long minutes = (long)(seconds / SECONDS_PER_MINUTE);
            seconds -= (long)(minutes * SECONDS_PER_MINUTE);

            long hours = (long)(minutes / MINUTES_PER_HOUR);
            minutes -= (long)(hours * MINUTES_PER_HOUR);

            long days = (long)(hours / HOURS_PER_DAY);
            hours -= (long)(days * HOURS_PER_DAY);

            if (days > 0)
            {
                return sign + days.ToString("#0") + "d "
                    + hours.ToString("00") + ":"
                    + minutes.ToString("00") + ":"
                    + seconds.ToString("00");
            }
            else if (hours > 0)
            {
                return sign + hours.ToString("#0") + ":"
                    + minutes.ToString("00") + ":"
                    + seconds.ToString("00");
            }
            else
            {
                string format = "00";
                if (numDecimals > 0)
                {
                    format += "." + new String('0', numDecimals);
                }

                return sign + minutes.ToString("#0") + ":"
                    + seconds.ToString(format);
            }
        }

        public static string FormatValue(double value, int numDecimals = 2)
        {
            string sign = "";
            if (value < 0.0)
            {
                sign = "-";
                value = -value;
            }

            string format = "0";
            if (numDecimals > 0)
            {
                format += "." + new String('0', numDecimals);
            }

            if (value > 1000000000.0)
            {
                return sign + (value / 1000000000.0).ToString(format) + " G";
            }
            else if (value > 1000000.0)
            {
                return sign + (value / 1000000.0).ToString(format) + " M";
            }
            else if (value > 1000.0)
            {
                return sign + (value / 1000.0).ToString(format) + " k";
            }
            else if (value < 0.000000001)
            {
                return sign + (value * 1000000000.0).ToString(format) + " n";
            }
            else if (value < 0.000001)
            {
                return sign + (value * 1000000.0).ToString(format) + " µ";
            }
            else if (value < 0.001)
            {
                return sign + (value * 1000.0).ToString(format) + " m";
            }
            else
            {
                return sign + value.ToString(format) + " ";
            }
        }

        public static string GetDllVersion<T>(T t)
        {
            System.Reflection.Assembly assembly = t.GetType().Assembly;
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        public static GUIStyle GetVersionStyle()
        {
            GUIStyle versionStyle = new GUIStyle(GUI.skin.label);
            versionStyle.alignment = TextAnchor.MiddleLeft;
            versionStyle.fontSize = 10;
            versionStyle.fontStyle = FontStyle.Normal;
            versionStyle.normal.textColor = Color.white;
            versionStyle.margin.top = 0;
            versionStyle.margin.bottom = 0;
            versionStyle.padding.top = 0;
            versionStyle.padding.bottom = 0;
            versionStyle.wordWrap = false;
            return versionStyle;
        }
        //return a day-hour-minute-seconds-time format for the delivery time
        public static string DeliveryTimeString(double deliveryTime, double currentTime)
        {
            int days;
            int hours;
            int minutes;
            int seconds;

            double time;
            if (deliveryTime > currentTime)
                time = deliveryTime - currentTime;
            else
                time = currentTime - deliveryTime;


            days = (int)Math.Floor(time / 21600);
            time = time - (days * 21600);

            hours = (int)Math.Floor(time / 3600);
            time = time - (hours * 3600);

            minutes = (int)Math.Floor(time / 60);
            time = time - (minutes * 60);

            seconds = (int)Math.Floor(time);

            if (deliveryTime > currentTime)
                return (days + "d" + hours + "h" + minutes + "m" + seconds + "s");
            else
                return ("-" + days + "d" + hours + "h" + minutes + "m" + seconds + "s");

        }

    }
}
