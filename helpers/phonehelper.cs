using System.Reflection;
using HarmonyLib;

namespace CustomWorkers;

internal static class PhoneHelper
{
    private static readonly MethodInfo? OpenChildScreenMethod = AccessTools.Method(typeof(UIScreenBase), "OpenChildScreen");
    private static readonly MethodInfo? SetPhoneButtonRaycastEnableMethod = AccessTools.Method(typeof(UI_PhoneScreen), "SetPhoneButtonRaycastEnable");
    private static readonly FieldInfo? CurrentChildScreenField = AccessTools.Field(typeof(UIScreenBase), "m_CurrentChildScreen");

    internal static UIScreenBase? GetCurrentChild(UI_PhoneScreen? phoneScreen)
    {
        return CurrentChildScreenField?.GetValue(phoneScreen) as UIScreenBase;
    }

    internal static string GetCurrentChildName(UI_PhoneScreen? phoneScreen)
    {
        return GetCurrentChild(phoneScreen)?.name ?? "<none>";
    }

    internal static void OpenChildScreen(UI_PhoneScreen? phoneScreen, UIScreenBase childScreen)
    {
        OpenChildScreenMethod?.Invoke(phoneScreen, new object[] { childScreen });
    }

    internal static void RestorePhoneStateAfterChildClose(UI_PhoneScreen? phoneScreen)
    {
        PhoneManager.SetCanClosePhone(canClose: true);
        if (phoneScreen != null)
        {
            SetPhoneButtonRaycastEnableMethod?.Invoke(phoneScreen, new object[] { true });
            CurrentChildScreenField?.SetValue(phoneScreen, null);
        }
    }

    internal static void SetCurrentChildAndLockClose(UI_PhoneScreen? phoneScreen, UIScreenBase childScreen)
    {
        if (phoneScreen == null)
        {
            return;
        }

        CurrentChildScreenField?.SetValue(phoneScreen, childScreen);
        childScreen.SetParentScreen(phoneScreen);
        PhoneManager.SetCanClosePhone(canClose: false);
        SetPhoneButtonRaycastEnableMethod?.Invoke(phoneScreen, new object[] { false });
    }

    internal static void SetPhoneCloseState(UI_PhoneScreen? phoneScreen, bool canClose, bool enableRaycast)
    {
        PhoneManager.SetCanClosePhone(canClose);
        if (phoneScreen != null)
        {
            SetPhoneButtonRaycastEnableMethod?.Invoke(phoneScreen, new object[] { enableRaycast });
        }
    }
}
