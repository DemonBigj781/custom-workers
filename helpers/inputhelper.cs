using System;
using System.Reflection;
using UnityEngine;

namespace CustomWorkers;

internal static class InputHelper
{
    private static readonly Type? InputLegacyType = Type.GetType("UnityEngine.Input, UnityEngine.InputLegacyModule");
    private static readonly MethodInfo? GetKeyDownMethod = InputLegacyType?.GetMethod("GetKeyDown", new[] { typeof(KeyCode) });

    internal static bool IsKeyPressed(KeyCode key)
    {
        if (GetKeyDownMethod == null)
        {
            return false;
        }

        object? result = GetKeyDownMethod.Invoke(null, new object[] { key });
        return result is bool pressed && pressed;
    }
}
