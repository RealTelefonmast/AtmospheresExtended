using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAE
{
    internal static class TLog
    {
        public static void Error(string msg, string tag = null)
        {
            Log.Error($"{"[TAE]".Colorize(TColor.BlueHighlight)} {msg}");
        }

        public static void ErrorOnce(string msg, int id)
        {
            Log.ErrorOnce($"{"[TAE]".Colorize(TColor.BlueHighlight)} {msg}", id);
        }

        public static void Warning(string msg)
        {
            Log.Warning($"{"[TAE]".Colorize(TColor.BlueHighlight)} {msg}");
        }

        public static void Message(string msg, Color color)
        {
            Log.Message($"{"[TAE]".Colorize(color)} {msg}");
        }

        public static void Message(string msg)
        {
            Log.Message($"{"[TAE]".Colorize(TColor.BlueHighlight)} {msg}");
        }

        public static void Debug(string msg, bool flag = true)
        {
            if (flag)
            {
                Log.Message($"{"[TAE-Debug]".Colorize(TColor.Green)} {msg}");
            }
        }
    }
}
