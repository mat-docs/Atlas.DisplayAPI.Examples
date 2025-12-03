// <copyright file="DelimiterFormat.cs" company="Motion Applied Ltd.">
// Copyright (c) Motion Applied Ltd.</copyright>

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