

using System;
using TeleCore;
using UnityEngine;
using Verse;

namespace TAC;

internal static class TLog
{
    public static void Error(string msg, string tag = null)
    {
#if IS_TELE_DEBUG
        Console.WriteLine($"{msg}");
#else
        Log.Error($"{"[TAE]".Colorize(TColor.BlueHighlight)} {msg}");
#endif
    }

    public static void ErrorOnce(string msg, int id)
    {
#if IS_TELE_DEBUG
            Console.WriteLine($"{msg}");
#else
        Log.ErrorOnce($"{"[TAE]".Colorize(TColor.BlueHighlight)} {msg}", id);
#endif
    }

    public static void Warning(string msg)
    {
#if IS_TELE_DEBUG
            Console.WriteLine($"{msg}");
#else
        Log.Warning($"{"[TAE]".Colorize(TColor.BlueHighlight)} {msg}");
#endif
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