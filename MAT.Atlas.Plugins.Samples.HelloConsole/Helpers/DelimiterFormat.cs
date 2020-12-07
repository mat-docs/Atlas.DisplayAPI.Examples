// <copyright file="DelimiterFormat.cs" company="McLaren Applied Technologies Ltd.">
// Copyright (c) McLaren Applied Technologies Ltd.</copyright>

namespace MAT.Atlas.Plugins.Samples.HelloConsole.Helpers
{
    internal enum DelimiterFormat
    {
        Csv,
        Tab
    }

    internal static class DelimiterFormatExtensions
    {
        public static string GetString(this DelimiterFormat delimiterFormat)
        {
            if (delimiterFormat == DelimiterFormat.Csv)
            {
                return ",";
            }

            if (delimiterFormat == DelimiterFormat.Tab)
            {
                return "\t";
            }

            return string.Empty;
        }
    }
}