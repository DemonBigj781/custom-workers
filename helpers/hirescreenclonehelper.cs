using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace CustomWorkers;

internal sealed class CustomWorkersHireScreenMarker : MonoBehaviour
{
    internal bool StartObserved { get; private set; }

    private void Start()
    {
        StartObserved = true;
    }
}

internal static class HireScreenCloneHelper
{
    private static readonly FieldInfo ScrollEndPosParentField = AccessTools.Field(typeof(GenericSliderScreen), "m_ScrollEndPosParent");
    private static readonly FieldInfo MaxPosXField = AccessTools.Field(typeof(GenericSliderScreen), "m_MaxPosX");
    private static readonly FieldInfo CanEvaluateMaxScrollPosField = AccessTools.Field(typeof(GenericSliderScreen), "m_CanEvaluateMaxScrollPos");
    private static readonly FieldInfo MaxPosFoundField = AccessTools.Field(typeof(GenericSliderScreen), "m_MaxPosFound");
    private static readonly FieldInfo MaxPosAccurateFoundField = AccessTools.Field(typeof(GenericSliderScreen), "m_MaxPosAccurateFound");
    private static readonly FieldInfo PosXField = AccessTools.Field(typeof(GenericSliderScreen), "m_PosX");
    private static readonly FieldInfo LerpPosXField = AccessTools.Field(typeof(GenericSliderScreen), "m_LerpPosX");
    private static readonly MethodInfo EvaluateScrollerMethod = AccessTools.Method(typeof(GenericSliderScreen), "EvaluateActiveRestockUIScroller");
    private static readonly MethodInfo ExtendWorkerRosterMethod = AccessTools.Method(typeof(Plugin), "ExtendWorkerRoster");

    private static HireWorkerScreen? clonedHireWorkerScreen;

    internal static bool IsManagedClone(HireWorkerScreen? screen)
    {
        return screen != null && screen.GetComponent<CustomWorkersHireScreenMarker>() != null;
    }

    internal static HireWorkerScreen? EnsureClone(PhoneManager? phoneManager, out bool createdThisCall)
    {
        createdThisCall = false;
        if (clonedHireWorkerScreen != null)
        {
            return clonedHireWorkerScreen;
        }

        if (phoneManager?.m_HireWorkerScreen == null)
        {
            return null;
        }

        GameObject original = phoneManager.m_HireWorkerScreen.gameObject;
        GameObject clone = Object.Instantiate(original, original.transform.parent);
        clone.name = "CustomWorkers_HireWorkerScreenClone";
        clonedHireWorkerScreen = clone.GetComponent<HireWorkerScreen>();
        if (clonedHireWorkerScreen == null)
        {
            Object.Destroy(clone);
            return null;
        }

        clone.AddComponent<CustomWorkersHireScreenMarker>();
        ApplyCloneTheme(clonedHireWorkerScreen);
        ArcRecruiterAssetLayerHelper.ApplyOwnedAssets(clonedHireWorkerScreen, GetGeneratedAppearanceMap(), GetLogger(), IsLiveHireTemplateReady(phoneManager.m_HireWorkerScreen));
        LogInfo("Created managed ARC Recruiter hire screen clone.");
        createdThisCall = true;
        if (clonedHireWorkerScreen.m_ScreenGroup != null)
        {
            clonedHireWorkerScreen.m_ScreenGroup.SetActive(false);
        }

        return clonedHireWorkerScreen;
    }

    internal static HireWorkerScreen? EnsureClone(PhoneManager? phoneManager)
    {
        return EnsureClone(phoneManager, out _);
    }

    internal static void PrewarmCloneIfPossible(UI_PhoneScreen? phoneScreen)
    {
        if (KillSwitchHelper.TripIfDisabled(KillSwitchHelper.IsArcUiEnabled(), "ArcUi.PrewarmClone", GetLogger()))
        {
            return;
        }

        PhoneManager? phoneManager = CSingleton<PhoneManager>.Instance;
        HireWorkerScreen? clone = EnsureClone(phoneManager, out bool createdThisCall);
        if (clone == null)
        {
            return;
        }

        CustomWorkersHireScreenMarker? marker = clone.GetComponent<CustomWorkersHireScreenMarker>();
        LogInfo($"Prewarm clone check: createdThisCall={createdThisCall} startObserved={marker?.StartObserved} screenGroupSelf={clone.m_ScreenGroup?.activeSelf} screenGroupHierarchy={clone.m_ScreenGroup?.activeInHierarchy}");
        if (phoneScreen != null && marker != null && !marker.StartObserved)
        {
            clone.StopAllCoroutines();
            clone.StartCoroutine(ObservePrewarmLifecycle(clone));
        }
    }

    internal static bool OpenClone(UI_PhoneScreen phoneScreen)
    {
        if (KillSwitchHelper.TripIfDisabled(KillSwitchHelper.IsArcUiEnabled(), "ArcUi.OpenClone", GetLogger()))
        {
            return false;
        }

        PhoneManager? phoneManager = CSingleton<PhoneManager>.Instance;
        HireWorkerScreen? clone = EnsureClone(phoneManager, out _);
        if (phoneScreen == null || clone == null)
        {
            LogWarning("Failed to open ARC Recruiter clone: phone screen or clone was null.");
            return false;
        }

        CustomWorkersHireScreenMarker? marker = clone.GetComponent<CustomWorkersHireScreenMarker>();
        if (marker != null && !marker.StartObserved)
        {
            LogInfo("ARC Recruiter clone not ready yet; waiting for Start() before first open.");
            clone.StopAllCoroutines();
            clone.StartCoroutine(WaitForCloneReadyAndOpen(phoneScreen, clone, marker));
            return true;
        }

        OpenCloneInternal(phoneScreen, clone);
        return true;
    }

    private static System.Collections.IEnumerator ObservePrewarmLifecycle(HireWorkerScreen clone)
    {
        yield return null;
        CustomWorkersHireScreenMarker? marker = clone.GetComponent<CustomWorkersHireScreenMarker>();
        LogInfo($"Prewarm clone lifecycle observed after one frame: startObserved={marker?.StartObserved} screenGroupSelf={clone.m_ScreenGroup?.activeSelf} screenGroupHierarchy={clone.m_ScreenGroup?.activeInHierarchy}");
    }

    private static System.Collections.IEnumerator WaitForCloneReadyAndOpen(UI_PhoneScreen phoneScreen, HireWorkerScreen clone, CustomWorkersHireScreenMarker marker)
    {
        int attempts = 0;
        while (!marker.StartObserved && attempts < 8)
        {
            attempts++;
            yield return null;
        }

        LogInfo($"Clone readiness wait finished: attempts={attempts} startObserved={marker.StartObserved}");
        OpenCloneInternal(phoneScreen, clone);
    }

    private static void OpenCloneInternal(UI_PhoneScreen phoneScreen, HireWorkerScreen clone)
    {

        PrepareCloneForOpen(clone);
        LogCloneScreenState("pre-open", phoneScreen, clone);
        SoundManager.PlayAudio("SFX_ButtonLightTap", 0.15f);
        PhoneHelper.OpenChildScreen(phoneScreen, clone);
        PhoneHelper.SetPhoneCloseState(phoneScreen, canClose: false, enableRaycast: false);

        LogCloneScreenState("post-open-immediate", phoneScreen, clone);
        clone.StopAllCoroutines();
        clone.StartCoroutine(StabilizeAfterOpen(clone));
        LogInfo($"Opened ARC Recruiter clone with {clone.m_HireWorkerPanelUIList?.Count ?? 0} panels.");
    }

    private static System.Collections.IEnumerator StabilizeAfterOpen(HireWorkerScreen screen)
    {
        yield return null;
        LogCloneScreenState("post-open-frame1", CSingleton<PhoneManager>.Instance?.m_UI_PhoneScreen, screen);
        WriteLiveRuntimeSnapshots("post-open-frame1", screen);
        yield return null;
        LogCloneScreenState("post-open-frame2", CSingleton<PhoneManager>.Instance?.m_UI_PhoneScreen, screen);
        WriteLiveRuntimeSnapshots("post-open-frame2", screen);

        PrepareCloneForOpen(screen, writeSnapshots: false);
        RebindCloseButtons(screen);

        if (screen.m_HireWorkerPanelUIList != null && screen.m_HireWorkerPanelUIList.Count > 0)
        {
            ScrollHelper.RefreshExpandedHireWorkerLayout(screen, ScrollEndPosParentField, EvaluateScrollerMethod);
        }

        EnsureVisibleCloneState(screen);
        LogCloneScreenState("post-open-stabilized", CSingleton<PhoneManager>.Instance?.m_UI_PhoneScreen, screen);
        WriteLiveRuntimeSnapshots("post-open-stabilized", screen);
        PhoneOverhaulAppHelper.NotifyCloneStabilized();
        LogInfo("Stabilized ARC Recruiter clone after open.");
    }

    internal static void ExportCurrentDebugSnapshots(BepInEx.Logging.ManualLogSource logger)
    {
        ExportCurrentDebugSnapshots(logger, "manual");
    }

    internal static void RestorePhoneStateAfterCloneClose(UI_PhoneScreen? phoneScreen)
    {
        PhoneHelper.RestorePhoneStateAfterChildClose(phoneScreen);
    }

    internal static void ExportCurrentDebugSnapshots(BepInEx.Logging.ManualLogSource logger, string snapshotName)
    {
        WorkerManager? manager = CSingleton<WorkerManager>.Instance;
        if (manager?.m_WorkerDataList != null)
        {
            LogHelper.WriteWorkerRosterJson(logger, snapshotName, manager.m_WorkerDataList, GetGeneratedAppearanceMap());
        }

        CustomerManager customerManager = CSingleton<CustomerManager>.Instance;
        FieldInfo customerListField = HarmonyLib.AccessTools.Field(typeof(CustomerManager), "m_CustomerList");
        if (customerManager != null && customerListField?.GetValue(customerManager) is List<Customer> customers)
        {
            LogHelper.WriteCustomerRosterJson(logger, snapshotName, customers);
        }

        HireWorkerScreen? clone = EnsureClone(CSingleton<PhoneManager>.Instance);
        if (clone?.m_ScreenGroup != null && manager?.m_WorkerDataList != null)
        {
            LogHelper.WriteHireScreenLayoutHtml(
                logger,
                snapshotName,
                clone,
                manager.m_WorkerDataList,
                GetGeneratedAppearanceMap(),
                MaxPosXField,
                ScrollEndPosParentField,
                CanEvaluateMaxScrollPosField,
                MaxPosFoundField,
                MaxPosAccurateFoundField,
                PosXField,
                LerpPosXField);

            Transform assetRoot = clone.transform.root != null ? clone.transform.root : clone.transform;
            Transform canvasGrp = assetRoot.Find("CanvasGrp");
            LogHelper.WriteUiAssetInventoryHtml(logger, "manual", canvasGrp != null ? canvasGrp : assetRoot);
        }
    }

    private static void PrepareCloneForOpen(HireWorkerScreen screen)
    {
        PrepareCloneForOpen(screen, writeSnapshots: true);
    }

    private static void PrepareCloneForOpen(HireWorkerScreen screen, bool writeSnapshots)
    {
        WorkerManager? manager = CSingleton<WorkerManager>.Instance;
        if (manager?.m_WorkerDataList == null || screen?.m_HireWorkerPanelUIList == null)
        {
            LogWarning("Skipped clone preparation because worker manager or panel list was unavailable.");
            return;
        }

        bool liveTemplateReady = IsLiveHireTemplateReady(CSingleton<PhoneManager>.Instance?.m_HireWorkerScreen);
        LogInfo($"Clone prepare checkpoint: rosterCount={manager.m_WorkerDataList.Count} generatedCount={GetGeneratedAppearanceMap().Count} runtimeWorkerCount={WorkerManager.GetWorkerList()?.Count ?? -1} liveTemplateReady={liveTemplateReady} assetSource={ArcRecruiterAssetLayerHelper.GetAssetLoadSummary()}.");

        if (GetGeneratedAppearanceMap().Count == 0)
        {
            int rosterCountBefore = manager.m_WorkerDataList.Count;
            if (KillSwitchHelper.IsRosterExtensionEnabled())
            {
                ExtendWorkerRosterMethod?.Invoke(null, new object[] { manager });
            }
            else
            {
                LogWarning("Skipped clone-triggered roster extension because roster mode is disabled.");
            }
            bool extended = manager.m_WorkerDataList.Count > rosterCountBefore || GetGeneratedAppearanceMap().Count > 0;
            LogInfo($"Clone prepare roster extension check: extended={extended} rosterCount={manager.m_WorkerDataList.Count} generatedCount={GetGeneratedAppearanceMap().Count}.");
        }

        HireHelper.EnsureHireWorkerPanels(screen);
        int visibleWorkerCount = manager.m_WorkerDataList.Count;
        BepInEx.Logging.ManualLogSource? manualLogSource = null;
        foreach (BepInEx.Logging.ILogSource source in BepInEx.Logging.Logger.Sources)
        {
            if (source is BepInEx.Logging.ManualLogSource candidate && candidate.SourceName == "Custom Workers")
            {
                manualLogSource = candidate;
                break;
            }
        }

        if (writeSnapshots && manualLogSource != null)
        {
            LogHelper.WriteWorkerRosterJson(manualLogSource, "arc-recruiter-open", manager.m_WorkerDataList, GetGeneratedAppearanceMap());
            CustomerManager customerManager = CSingleton<CustomerManager>.Instance;
            FieldInfo customerListField = HarmonyLib.AccessTools.Field(typeof(CustomerManager), "m_CustomerList");
            if (customerManager != null && customerListField?.GetValue(customerManager) is List<Customer> customers)
            {
                LogHelper.WriteCustomerRosterJson(manualLogSource, "arc-recruiter-open", customers);
            }

            LogHelper.WriteHireScreenLayoutHtml(
                manualLogSource,
                "arc-recruiter-open",
                screen,
                manager.m_WorkerDataList,
                GetGeneratedAppearanceMap(),
                MaxPosXField,
                ScrollEndPosParentField,
                CanEvaluateMaxScrollPosField,
                MaxPosFoundField,
                MaxPosAccurateFoundField,
                PosXField,
                LerpPosXField);

        }

        for (int index = 0; index < screen.m_HireWorkerPanelUIList.Count; index++)
        {
            HireWorkerPanelUI panel = screen.m_HireWorkerPanelUIList[index];
            if (panel == null)
            {
                continue;
            }

            if (index < visibleWorkerCount)
            {
                panel.Init(screen, index);
                RebindHireButton(panel, index);
                panel.SetActive(true);
            }
            else
            {
                panel.SetActive(false);
            }
        }

        if (screen.m_HireWorkerPanelUIList.Count > 0)
        {
            ScrollHelper.RefreshExpandedHireWorkerLayout(screen, ScrollEndPosParentField, EvaluateScrollerMethod);
        }

        ArcRecruiterAssetLayerHelper.ApplyOwnedAssets(screen, GetGeneratedAppearanceMap(), manualLogSource ?? GetLogger(), liveTemplateReady);
        ControlHelper.RebindControllerButtons(screen, manualLogSource ?? GetLogger());
        RebindCloseButtons(screen);

        LogInfo($"Prepared ARC Recruiter clone for open with visibleWorkerCount={visibleWorkerCount} and panelCount={screen.m_HireWorkerPanelUIList.Count}.");
    }

    private static void RebindHireButton(HireWorkerPanelUI panel, int workerIndex)
    {
        if (panel?.m_PurchaseBtn == null)
        {
            LogWarning($"Worker panel {workerIndex} had no purchase button to rebind.");
            return;
        }

        Button? button = FindPrimaryButton(panel.m_PurchaseBtn.transform);
        if (button == null)
        {
            LogWarning($"Worker panel {workerIndex} purchase button had no Button component anywhere under purchase button root.");
            return;
        }

        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener((UnityAction)(() => OnArcHireRequested(panel, workerIndex, button)));
        button.interactable = true;
        if (button.targetGraphic != null)
        {
            button.targetGraphic.raycastTarget = true;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
        }

        LogInfo($"Rebound hire button for worker panel {workerIndex}. {InputTrackingHelper.DescribeButton(button)}");
    }

    internal static void OnArcHireRequested(HireWorkerPanelUI panel, int workerIndex, Button? liveButton)
    {
        WorkerData? workerData = null;
        try
        {
            workerData = WorkerManager.GetWorkerData(workerIndex);
        }
        catch
        {
        }

        string workerName = workerData != null ? workerData.GetName() : "<unknown>";
        bool buttonActive = liveButton != null && liveButton.gameObject.activeInHierarchy;
        bool interactable = liveButton != null && liveButton.interactable;
        string buttonPath = liveButton != null ? InputTrackingHelper.GetTransformPath(liveButton.transform) : "<none>";
        LogInfo($"ARC hire click received: index={workerIndex} workerName={workerName} buttonActive={buttonActive} interactable={interactable} buttonPath={buttonPath}");
        panel.OnPressHireButton();
        bool nowHired = CPlayerData.GetIsWorkerHired(workerIndex);
        LogInfo($"ARC hire click handled: index={workerIndex} workerName={workerName} nowHired={nowHired} coin={CPlayerData.m_CoinAmountDouble}");
    }

    private static void RebindCloseButtons(HireWorkerScreen screen)
    {
        if (screen?.m_ScreenGroup == null)
        {
            return;
        }

        Button[] buttons = screen.m_ScreenGroup.GetComponentsInChildren<Button>(true);
        for (int index = 0; index < buttons.Length; index++)
        {
            Button button = buttons[index];
            if (button == null)
            {
                continue;
            }

            string name = button.gameObject.name.ToLowerInvariant();
            if (!name.Contains("close") && !name.Equals("x") && !name.Contains("back"))
            {
                continue;
            }

            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener((UnityAction)(() => OnArcCloseRequested(screen)));
            button.interactable = true;
            if (button.targetGraphic != null)
            {
                button.targetGraphic.raycastTarget = true;
            }

            Image? image = button.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
            }

            LogInfo($"Rebound close button '{button.gameObject.name}'. {InputTrackingHelper.DescribeButton(button)}");
        }
    }

    internal static void OnArcCloseRequested(HireWorkerScreen? screen)
    {
        if (screen == null)
        {
            return;
        }

        UI_PhoneScreen? phoneScreen = CSingleton<PhoneManager>.Instance?.m_UI_PhoneScreen;
        LogInfo($"ARC close requested for screen={screen.name} phoneScreen={(phoneScreen != null ? phoneScreen.name : "<null>")} currentChild={PhoneHelper.GetCurrentChildName(phoneScreen)}");
        try
        {
            screen.CloseScreen();
        }
        finally
        {
            RestorePhoneStateAfterCloneClose(phoneScreen);
            PhoneOverhaulAppHelper.NotifyCloneClosed();
            LogInfo("ARC close wrapper restored phone state after close request.");
        }
    }

    private static Button? FindPrimaryButton(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Transform? btnRaycast = root.Find("BtnRaycast");
        if (btnRaycast != null)
        {
            Button? raycastButton = btnRaycast.GetComponent<Button>();
            if (raycastButton != null)
            {
                return raycastButton;
            }
        }

        Button directButton = root.GetComponent<Button>();
        if (directButton != null)
        {
            return directButton;
        }

        return root.GetComponentInChildren<Button>(true);
    }

    private static IReadOnlyDictionary<int, GeneratedWorkerAppearance> GetGeneratedAppearanceMap()
    {
        FieldInfo? field = AccessTools.Field(typeof(Plugin), "GeneratedWorkerAppearances");
        return field?.GetValue(null) as IReadOnlyDictionary<int, GeneratedWorkerAppearance>
            ?? new Dictionary<int, GeneratedWorkerAppearance>();
    }

    private static void ApplyCloneTheme(HireWorkerScreen screen)
    {
        if (screen?.m_ScreenGroup == null)
        {
            return;
        }

        Image[] images = screen.m_ScreenGroup.GetComponentsInChildren<Image>(true);
        for (int index = 0; index < images.Length; index++)
        {
            Image image = images[index];
            if (image == null)
            {
                continue;
            }

            string name = image.gameObject.name.ToLowerInvariant();
            if (name.Contains("close") || name == "x" || name.Contains("exit"))
            {
                continue;
            }

            if (IsUnderHireWorkerPanel(image.transform))
            {
                continue;
            }

            if (name.Contains("icon") || name.Contains("clipboard") || name.Contains("sprite"))
            {
                image.gameObject.SetActive(false);
                continue;
            }

            if (name.Contains("header") || name.Contains("footer") || name.Contains("topheader") || name.Contains("bottomfooter"))
            {
                Color color = image.color;
                image.color = new Color(0f, 0f, 0f, color.a);
            }
        }
    }

    private static bool IsLiveHireTemplateReady(HireWorkerScreen? screen)
    {
        if (screen == null)
        {
            return false;
        }

        return screen.m_ScreenGroup != null
            && screen.m_HireWorkerPanelUIList != null
            && screen.m_HireWorkerPanelUIList.Count > 0;
    }

    private static bool IsUnderHireWorkerPanel(Transform transform)
    {
        return transform.GetComponentInParent<HireWorkerPanelUI>() != null;
    }

    private static void EnsureVisibleCloneState(HireWorkerScreen screen)
    {
        UI_PhoneScreen? phoneScreen = CSingleton<PhoneManager>.Instance?.m_UI_PhoneScreen;
        UIScreenBase? currentChild = PhoneHelper.GetCurrentChild(phoneScreen);
        if (screen.m_ScreenGroup != null && currentChild == screen && !screen.m_ScreenGroup.activeSelf)
        {
            LogWarning("Detected inactive ARC Recruiter clone while it is still the current phone child; restoring screen group and phone close state.");
            screen.m_ScreenGroup.SetActive(true);
            PhoneHelper.SetPhoneCloseState(phoneScreen, canClose: false, enableRaycast: false);
        }

        if (phoneScreen != null && screen.m_ScreenGroup != null && screen.m_ScreenGroup.activeInHierarchy && currentChild != screen)
        {
            LogWarning($"Detected ARC Recruiter clone visible while phoneCurrentChild was '{currentChild?.name ?? "<none>"}'; restoring current child binding.");
            PhoneHelper.SetCurrentChildAndLockClose(phoneScreen, screen);
        }
    }

    private static void WriteLiveRuntimeSnapshots(string phase, HireWorkerScreen screen)
    {
        BepInEx.Logging.ManualLogSource? logger = GetLogger();
        List<WorkerData>? workerDataList = CSingleton<WorkerManager>.Instance?.m_WorkerDataList;
        if (logger == null || workerDataList == null)
        {
            return;
        }

        LogHelper.WriteHireScreenLayoutHtml(
            logger,
            $"arc-recruiter-{phase}",
            screen,
            workerDataList,
            GetGeneratedAppearanceMap(),
            MaxPosXField,
            ScrollEndPosParentField,
            CanEvaluateMaxScrollPosField,
            MaxPosFoundField,
            MaxPosAccurateFoundField,
            PosXField,
            LerpPosXField);
    }

    private static void LogCloneScreenState(string phase, UI_PhoneScreen? phoneScreen, HireWorkerScreen? screen)
    {
        if (screen == null)
        {
            LogInfo($"Clone screen state {phase}: screen=<null>");
            return;
        }

        int panelCount = screen.m_HireWorkerPanelUIList?.Count ?? -1;
        int activePanelCount = 0;
        if (screen.m_HireWorkerPanelUIList != null)
        {
            for (int index = 0; index < screen.m_HireWorkerPanelUIList.Count; index++)
            {
                HireWorkerPanelUI? panel = screen.m_HireWorkerPanelUIList[index];
                if (panel != null && panel.gameObject.activeInHierarchy)
                {
                    activePanelCount++;
                }
            }
        }

        GameObject? screenGroup = screen.m_ScreenGroup;
        string currentChild = phoneScreen != null ? PhoneHelper.GetCurrentChildName(phoneScreen) : "<unknown>";

        LogInfo(
            $"Clone screen state {phase}: screen={screen.name} screenGroupSelf={screenGroup?.activeSelf} screenGroupHierarchy={screenGroup?.activeInHierarchy} panelCount={panelCount} activePanels={activePanelCount} phoneCurrentChild={currentChild}");
    }

    private static void LogInfo(string message)
    {
        BepInEx.Logging.ManualLogSource? logger = GetLogger();
        if (logger != null)
        {
            logger.LogInfo($"Custom Workers {message}");
        }
        else
        {
            Debug.Log($"[Custom Workers] {message}");
        }
    }

    private static void LogWarning(string message)
    {
        BepInEx.Logging.ManualLogSource? logger = GetLogger();
        if (logger != null)
        {
            logger.LogWarning($"Custom Workers {message}");
        }
        else
        {
            Debug.LogWarning($"[Custom Workers] {message}");
        }
    }

    private static BepInEx.Logging.ManualLogSource? GetLogger()
    {
        foreach (BepInEx.Logging.ILogSource source in BepInEx.Logging.Logger.Sources)
        {
            if (source is BepInEx.Logging.ManualLogSource candidate && candidate.SourceName == "Custom Workers")
            {
                return candidate;
            }
        }

        return null;
    }
}
