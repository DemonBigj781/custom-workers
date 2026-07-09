using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using I2.Loc;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CustomWorkers;

internal static class LogHelper
{
    private static ManualLogSource? runtimeLogger;
    private static readonly System.Type? ScreenCaptureType = System.Type.GetType("UnityEngine.ScreenCapture, UnityEngine.ScreenCaptureModule");
    private static readonly MethodInfo? CaptureScreenshotMethod = ScreenCaptureType?.GetMethod("CaptureScreenshot", new[] { typeof(string) });

    internal static void SetRuntimeLogger(ManualLogSource logger)
    {
        runtimeLogger = logger;
    }

    internal static void LogRuntimeDebug(string message)
    {
        runtimeLogger?.LogInfo(TimestampHelper.Stamp(message));
    }

    internal static string CaptureScreenshot(ManualLogSource logger, string snapshotName)
    {
        string pluginDirectory = GetRunArtifactDirectory();
        Directory.CreateDirectory(pluginDirectory);
        string outputPath = Path.Combine(pluginDirectory, $"phone-screenshot-{snapshotName}.png");
        if (CaptureScreenshotMethod != null)
        {
            CaptureScreenshotMethod.Invoke(null, new object[] { outputPath });
            logger.LogInfo($"Custom Workers requested screenshot capture to {outputPath}.");
        }
        else
        {
            logger.LogWarning("Custom Workers could not access Unity ScreenCapture module for screenshot export.");
        }

        return outputPath;
    }

    internal static void WriteGeneratedWorkerJson(ManualLogSource logger, IReadOnlyList<GeneratedWorkerLogEntry> entries)
    {
        try
        {
            string pluginDirectory = GetRunArtifactDirectory();
            Directory.CreateDirectory(pluginDirectory);
            string outputPath = Path.Combine(pluginDirectory, GeneratedWorkerLogRules.FileName);
            File.WriteAllText(outputPath, GeneratedWorkerLogRules.BuildJson(entries));
            logger.LogInfo($"Custom Workers wrote generated worker log to {outputPath}.");
        }
        catch (System.Exception ex)
        {
            logger.LogError($"Custom Workers failed to write generated worker log: {ex}");
        }
    }

    internal static void WriteWorkerRosterJson(ManualLogSource logger, string snapshotName, IReadOnlyList<WorkerData> entries, IReadOnlyDictionary<int, GeneratedWorkerAppearance> generatedAppearances)
    {
        try
        {
            string pluginDirectory = GetRunArtifactDirectory();
            Directory.CreateDirectory(pluginDirectory);
            string outputPath = Path.Combine(pluginDirectory, WorkerRosterDebugRules.GetFileName(snapshotName));
            File.WriteAllText(outputPath, WorkerRosterDebugRules.BuildJson(entries, generatedAppearances));
            logger.LogInfo($"Custom Workers wrote worker roster snapshot '{snapshotName}' to {outputPath}.");
        }
        catch (System.Exception ex)
        {
            logger.LogError($"Custom Workers failed to write worker roster snapshot '{snapshotName}': {ex}");
        }
    }

    internal static void WriteCustomerRosterJson(ManualLogSource logger, string snapshotName, IReadOnlyList<Customer> customers)
    {
        try
        {
            string pluginDirectory = GetRunArtifactDirectory();
            Directory.CreateDirectory(pluginDirectory);
            string outputPath = Path.Combine(pluginDirectory, CustomerRosterDebugRules.GetFileName(snapshotName));
            File.WriteAllText(outputPath, CustomerRosterDebugRules.BuildJson(customers));
            logger.LogInfo($"Custom Workers wrote customer roster snapshot '{snapshotName}' to {outputPath}.");
        }
        catch (System.Exception ex)
        {
            logger.LogError($"Custom Workers failed to write customer roster snapshot '{snapshotName}': {ex}");
        }
    }

    internal static void WriteHireScreenLayoutHtml(
        ManualLogSource logger,
        string snapshotName,
        HireWorkerScreen screen,
        IReadOnlyList<WorkerData> workerDataList,
        IReadOnlyDictionary<int, GeneratedWorkerAppearance> generatedAppearances,
        FieldInfo maxPosXField,
        FieldInfo scrollEndPosParentField,
        FieldInfo canEvaluateMaxScrollPosField,
        FieldInfo maxPosFoundField,
        FieldInfo maxPosAccurateFoundField,
        FieldInfo posXField,
        FieldInfo lerpPosXField)
    {
        try
        {
            string pluginDirectory = GetRunArtifactDirectory();
            Directory.CreateDirectory(pluginDirectory);
            string outputPath = Path.Combine(pluginDirectory, $"app-layout-{snapshotName}.html");
            File.WriteAllText(
                outputPath,
                BuildHireScreenLayoutHtml(
                    screen,
                    workerDataList,
                    generatedAppearances,
                    maxPosXField,
                    scrollEndPosParentField,
                    canEvaluateMaxScrollPosField,
                    maxPosFoundField,
                    maxPosAccurateFoundField,
                    posXField,
                    lerpPosXField));
            logger.LogInfo($"Custom Workers wrote app layout snapshot '{snapshotName}' to {outputPath}.");
        }
        catch (System.Exception ex)
        {
            logger.LogError($"Custom Workers failed to write app layout snapshot '{snapshotName}': {ex}");
        }
    }

    internal static void WriteUiAssetInventoryHtml(ManualLogSource logger, string snapshotName, Transform root)
    {
        try
        {
            string pluginDirectory = GetRunArtifactDirectory();
            Directory.CreateDirectory(pluginDirectory);
            string outputPath = Path.Combine(pluginDirectory, $"ui-assets-{snapshotName}.html");
            File.WriteAllText(outputPath, BuildUiAssetInventoryHtml(root));
            logger.LogInfo($"Custom Workers wrote UI asset inventory '{snapshotName}' to {outputPath}.");
        }
        catch (System.Exception ex)
        {
            logger.LogError($"Custom Workers failed to write UI asset inventory '{snapshotName}': {ex}");
        }
    }

    internal static void LogHireWorkerDiagnostics(ManualLogSource logger, HireWorkerScreen screen, FieldInfo maxPosXField)
    {
        int panelCount = screen.m_HireWorkerPanelUIList?.Count ?? 0;
        int controllerRowCount = screen.m_ControllerScreenUIExtension?.m_ControllerBtnColumnList?.Count ?? 0;
        string maxPosText = "n/a";
        if (maxPosXField?.GetValue(screen) is float maxPos)
        {
            maxPosText = maxPos.ToString("0.##");
        }

        logger.LogInfo($"Custom Workers hire diagnostics: panels={panelCount}, controllerRows={controllerRowCount}, maxPosX={maxPosText}");

        if (screen.m_HireWorkerPanelUIList == null)
        {
            return;
        }

        int diagnosticStartIndex = System.Math.Max(0, panelCount - 8);
        if (panelCount >= 10)
        {
            diagnosticStartIndex = System.Math.Min(diagnosticStartIndex, 8);
        }

        for (int index = diagnosticStartIndex; index < panelCount; index++)
        {
            HireWorkerPanelUI panel = screen.m_HireWorkerPanelUIList[index];
            bool panelActive = panel != null && panel.IsActive();
            string panelName = panel?.m_NameText != null ? panel.m_NameText.text : "<none>";
            string iconResolution = "<none>";
            if (panel?.m_IconImage?.sprite?.texture != null)
            {
                Texture2D texture = panel.m_IconImage.sprite.texture;
                iconResolution = $"{texture.width}x{texture.height}";
            }

            logger.LogInfo($"Custom Workers hire panel[{index}] active={panelActive} name={panelName} iconTex={iconResolution}");
        }

        if (screen.m_ControllerScreenUIExtension != null)
        {
            logger.LogInfo(
                $"Custom Workers controller state: currentX={screen.m_ControllerScreenUIExtension.m_CurrentCtrlBtnXIndex} currentY={screen.m_ControllerScreenUIExtension.m_CurrentCtrlBtnYIndex}");
        }
    }

    internal static void LogSliderLifecycle(
        ManualLogSource logger,
        string phase,
        MonoBehaviour screen,
        FieldInfo maxPosXField,
        FieldInfo scrollEndPosParentField,
        FieldInfo canEvaluateMaxScrollPosField,
        FieldInfo maxPosFoundField,
        FieldInfo maxPosAccurateFoundField,
        FieldInfo posXField,
        FieldInfo lerpPosXField)
    {
        string screenName = screen != null ? screen.gameObject.name : "<null>";
        string screenType = screen != null ? screen.GetType().Name : "<null>";
        string endParentName = "<none>";
        string maxPosText = "n/a";
        string posXText = "n/a";
        string lerpPosText = "n/a";
        string canEvaluateText = "n/a";
        string maxFoundText = "n/a";
        string maxAccurateText = "n/a";

        if (scrollEndPosParentField?.GetValue(screen) is GameObject endParent && endParent != null)
        {
            endParentName = endParent.name;
        }

        if (maxPosXField?.GetValue(screen) is float maxPos)
        {
            maxPosText = maxPos.ToString("0.##");
        }

        if (posXField?.GetValue(screen) is float posX)
        {
            posXText = posX.ToString("0.##");
        }

        if (lerpPosXField?.GetValue(screen) is float lerpPos)
        {
            lerpPosText = lerpPos.ToString("0.##");
        }

        if (canEvaluateMaxScrollPosField?.GetValue(screen) is bool canEvaluate)
        {
            canEvaluateText = canEvaluate.ToString();
        }

        if (maxPosFoundField?.GetValue(screen) is bool maxFound)
        {
            maxFoundText = maxFound.ToString();
        }

        if (maxPosAccurateFoundField?.GetValue(screen) is bool maxAccurateFound)
        {
            maxAccurateText = maxAccurateFound.ToString();
        }

        logger.LogInfo(
            $"Custom Workers slider {phase}: screen={screenName} type={screenType} endParent={endParentName} maxPosX={maxPosText} posX={posXText} lerpPosX={lerpPosText} canEvaluate={canEvaluateText} maxFound={maxFoundText} maxAccurate={maxAccurateText}");
    }

    internal static string GetRunArtifactDirectory()
    {
        return Path.Combine(Paths.PluginPath, "CustomWorkers", $"run-{TimestampHelper.GetRunId()}");
    }

    private static string BuildHireScreenLayoutHtml(
        HireWorkerScreen screen,
        IReadOnlyList<WorkerData> workerDataList,
        IReadOnlyDictionary<int, GeneratedWorkerAppearance> generatedAppearances,
        FieldInfo maxPosXField,
        FieldInfo scrollEndPosParentField,
        FieldInfo canEvaluateMaxScrollPosField,
        FieldInfo maxPosFoundField,
        FieldInfo maxPosAccurateFoundField,
        FieldInfo posXField,
        FieldInfo lerpPosXField)
    {
        var html = new StringBuilder(32768);
        Transform assetRoot = screen.transform.root != null ? screen.transform.root : screen.transform;
        Transform? canvasGrp = assetRoot.Find("CanvasGrp");
        Transform reportRoot = canvasGrp ?? (screen.m_ScreenGroup != null ? screen.m_ScreenGroup.transform : screen.transform);
        string screenName = screen.gameObject.name;
        string endParentName = scrollEndPosParentField?.GetValue(screen) is GameObject endParent && endParent != null ? endParent.name : "<none>";
        List<HireWorkerPanelUI>? panels = screen.m_HireWorkerPanelUIList;
        int panelCount = panels?.Count ?? 0;
        int activePanelCount = 0;
        for (int index = 0; index < panelCount; index++)
        {
            HireWorkerPanelUI? activePanel = panels![index];
            if (activePanel != null && activePanel.IsActive())
            {
                activePanelCount++;
            }
        }

        html.AppendLine("<!doctype html>");
        html.AppendLine("<html><head><meta charset=\"utf-8\"><title>Custom Workers App Layout</title>");
        html.AppendLine("<style>");
        html.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;background:#111;color:#eee;margin:16px}");
        html.AppendLine("h1,h2{margin:0 0 12px} h2{margin-top:24px}");
        html.AppendLine("table{border-collapse:collapse;width:100%;margin:12px 0} th,td{border:1px solid #444;padding:6px 8px;font-size:12px;vertical-align:top}");
        html.AppendLine("th{background:#222} .mono{font-family:Consolas,Menlo,monospace} .swatch{display:inline-block;width:12px;height:12px;border:1px solid #666;vertical-align:middle;margin-right:6px}");
        html.AppendLine(".summary{display:grid;grid-template-columns:repeat(2,minmax(240px,1fr));gap:8px}");
        html.AppendLine(".card{background:#181818;border:1px solid #333;padding:10px}");
        html.AppendLine("</style></head><body>");
        html.Append("<h1>Custom Workers App Layout Snapshot</h1>");
        html.Append("<div class=\"summary\">");
        bool isManagedClone = screen.GetComponent<CustomWorkersHireScreenMarker>() != null;
        AppendSummaryCard(html, "Screen", $"<div><b>Name:</b> {EscapeHtml(screenName)}</div><div><b>Managed Clone:</b> {(isManagedClone ? "true" : "false")}</div><div><b>Panels:</b> {panelCount}</div><div><b>Active Panels:</b> {activePanelCount}</div><div><b>Worker Data Entries:</b> {workerDataList.Count}</div>");
        AppendSummaryCard(html, "Slider", $"<div><b>End Parent:</b> {EscapeHtml(endParentName)}</div><div><b>MaxPosX:</b> {FormatField(maxPosXField, screen)}</div><div><b>PosX:</b> {FormatField(posXField, screen)}</div><div><b>LerpPosX:</b> {FormatField(lerpPosXField, screen)}</div><div><b>CanEvaluate:</b> {FormatField(canEvaluateMaxScrollPosField, screen)}</div><div><b>MaxFound:</b> {FormatField(maxPosFoundField, screen)}</div><div><b>MaxAccurate:</b> {FormatField(maxPosAccurateFoundField, screen)}</div>");
        html.Append("</div>");

        html.AppendLine("<h2>Worker Panels</h2>");
        html.AppendLine("<table><thead><tr><th>Panel</th><th>Active</th><th>Displayed Name</th><th>WorkerData Name</th><th>Generated</th><th>Actually Hired</th><th>Icon</th><th>PurchaseBtn</th><th>LockBtn</th><th>HiredText</th><th>ButtonState</th><th>UI Consistency</th><th>Panel Path</th></tr></thead><tbody>");
        for (int index = 0; index < panelCount; index++)
        {
            HireWorkerPanelUI? panel = screen.m_HireWorkerPanelUIList[index];
            string displayedName = panel?.m_NameText != null ? panel.m_NameText.text : "<none>";
            string workerName = index < workerDataList.Count ? workerDataList[index].GetName() : "<out-of-range>";
            string generated = generatedAppearances.ContainsKey(index) ? "true" : "false";
            string iconText = "missing";
            if (panel?.m_IconImage?.sprite?.texture != null)
            {
                Texture texture = panel.m_IconImage.sprite.texture;
                iconText = $"present ({texture.width}x{texture.height})";
            }

            bool actuallyHired = index < workerDataList.Count && CPlayerData.GetIsWorkerHired(index);
            string purchaseState = panel?.m_PurchaseBtn != null ? BoolState(panel.m_PurchaseBtn.activeSelf, panel.m_PurchaseBtn.activeInHierarchy) : "<none>";
            string lockState = panel?.m_LockPurchaseBtn != null ? BoolState(panel.m_LockPurchaseBtn.activeSelf, panel.m_LockPurchaseBtn.activeInHierarchy) : "<none>";
            string hiredState = panel?.m_HiredText != null ? BoolState(panel.m_HiredText.activeSelf, panel.m_HiredText.activeInHierarchy) : "<none>";
            string buttonState = DescribeButtonState(panel?.m_PurchaseBtn);
            string uiConsistency = DescribeHireUiConsistency(panel, actuallyHired);

            html.Append("<tr>");
            html.Append("<td>").Append(index).Append("</td>");
            html.Append("<td>").Append(panel != null && panel.IsActive() ? "true" : "false").Append("</td>");
            html.Append("<td>").Append(EscapeHtml(displayedName)).Append("</td>");
            html.Append("<td>").Append(EscapeHtml(workerName)).Append("</td>");
            html.Append("<td>").Append(generated).Append("</td>");
            html.Append("<td>").Append(actuallyHired ? "true" : "false").Append("</td>");
            html.Append("<td>").Append(EscapeHtml(iconText)).Append("</td>");
            html.Append("<td>").Append(EscapeHtml(purchaseState)).Append("</td>");
            html.Append("<td>").Append(EscapeHtml(lockState)).Append("</td>");
            html.Append("<td>").Append(EscapeHtml(hiredState)).Append("</td>");
            html.Append("<td>").Append(EscapeHtml(buttonState)).Append("</td>");
            html.Append("<td>").Append(EscapeHtml(uiConsistency)).Append("</td>");
            html.Append("<td class=\"mono\">").Append(EscapeHtml(panel != null ? GetTransformPath(panel.transform) : "<null>")).Append("</td>");
            html.AppendLine("</tr>");
        }

        html.AppendLine("</tbody></table>");

        html.AppendLine("<h2>Display Hierarchy</h2>");
        html.Append(BuildUiHierarchyHtml(reportRoot));

        html.AppendLine("<h2>Appearance Mockup</h2>");
        html.AppendLine("<div class=\"card\">Experimental and currently disabled. Use the hierarchy, asset inventory, geometry, interaction, and localization tables below as the source of truth.</div>");

        html.AppendLine("<h2>Asset Inventory</h2>");
        html.AppendLine("<table><thead><tr><th>Parent Body</th><th>Parent Path</th><th>Path</th><th>Active</th><th>Components</th><th>Rect</th><th>Image</th><th>Interaction</th><th>Text</th><th>Localization</th></tr></thead><tbody>");

        Transform[] transforms = reportRoot != null ? reportRoot.GetComponentsInChildren<Transform>(true) : new Transform[0];
        string previousParentPath = string.Empty;
        for (int index = 0; index < transforms.Length; index++)
        {
            Transform transform = transforms[index];
            GameObject gameObject = transform.gameObject;
            Component[] components = gameObject.GetComponents<Component>();
            Image image = gameObject.GetComponent<Image>();
            TMP_Text text = gameObject.GetComponent<TMP_Text>();
            string parentPath = GetParentPath(transform);
            string parentBody = GetParentBodyName(transform);

            if (!string.Equals(previousParentPath, parentPath, System.StringComparison.Ordinal))
            {
                html.Append("<tr><td colspan=\"10\" style=\"background:#1b1b1b;font-weight:700\">Group: ").Append(EscapeHtml(parentPath)).AppendLine("</td></tr>");
                previousParentPath = parentPath;
            }

            html.Append("<tr>");
            html.Append("<td>").Append(EscapeHtml(parentBody)).Append("</td>");
            html.Append("<td class=\"mono\">").Append(EscapeHtml(parentPath)).Append("</td>");
            html.Append("<td class=\"mono\">").Append(EscapeHtml(GetTransformPath(transform))).Append("</td>");
            html.Append("<td>").Append(EscapeHtml(BoolState(gameObject.activeSelf, gameObject.activeInHierarchy))).Append("</td>");
            html.Append("<td>").Append(EscapeHtml(DescribeComponents(components))).Append("</td>");
            html.Append("<td>").Append(EscapeHtml(DescribeRectState(transform))).Append("</td>");
            html.Append("<td>").Append(DescribeImageHtml(image)).Append("</td>");
            html.Append("<td>").Append(EscapeHtml(DescribeInteractionState(gameObject))).Append("</td>");
            html.Append("<td>").Append(EscapeHtml(DescribeTextState(gameObject, text))).Append("</td>");
            html.Append("<td>").Append(EscapeHtml(DescribeLocalizationState(gameObject))).Append("</td>");
            html.AppendLine("</tr>");
        }

        html.AppendLine("</tbody></table></body></html>");
        return html.ToString();
    }

    private static string BuildUiAssetInventoryHtml(Transform root)
    {
        var html = new StringBuilder(65536);
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html><head><meta charset=\"utf-8\"><title>Custom Workers UI Asset Inventory</title>");
        html.AppendLine("<style>");
        html.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;background:#111;color:#eee;margin:16px}");
        html.AppendLine("h1{margin:0 0 12px} table{border-collapse:collapse;width:100%;margin:12px 0}");
        html.AppendLine("th,td{border:1px solid #444;padding:6px 8px;font-size:12px;vertical-align:top}");
        html.AppendLine("th{background:#222;position:sticky;top:0} .mono{font-family:Consolas,Menlo,monospace}");
        html.AppendLine(".swatch{display:inline-block;width:12px;height:12px;border:1px solid #666;vertical-align:middle;margin-right:6px}");
        html.AppendLine(".mockup{position:relative;width:420px;height:760px;background:#0e0e0e;border:1px solid #333;overflow:hidden;margin:16px 0}");
        html.AppendLine(".mock-layer{position:absolute;box-sizing:border-box;overflow:hidden;white-space:nowrap;text-overflow:ellipsis;font-size:9px;line-height:1.2;padding:1px 2px}");
        html.AppendLine("</style></head><body>");
        html.Append("<h1>Custom Workers UI Asset Inventory</h1>");
        html.Append("<div><b>Root:</b> ").Append(EscapeHtml(GetTransformPath(root))).Append("</div>");
        html.AppendLine("<h2>Appearance Mockup</h2>");
        html.AppendLine("<div class=\"card\">Experimental and currently disabled. Use the hierarchy and asset table below as the source of truth.</div>");
        html.AppendLine("<h2>Display Hierarchy</h2>");
        html.Append(BuildUiHierarchyHtml(root));
        html.AppendLine("<table><thead><tr><th>Parent Body</th><th>Parent Path</th><th>Path</th><th>Active</th><th>Components</th><th>Rect</th><th>Image</th><th>Interaction</th><th>Text</th><th>Localization</th></tr></thead><tbody>");

        Transform[] transforms = root != null ? root.GetComponentsInChildren<Transform>(true) : new Transform[0];
        string previousParentPath = string.Empty;
        for (int index = 0; index < transforms.Length; index++)
        {
            Transform transform = transforms[index];
            GameObject gameObject = transform.gameObject;
            Component[] components = gameObject.GetComponents<Component>();
            Image image = gameObject.GetComponent<Image>();
            Button button = gameObject.GetComponent<Button>();
            TMP_Text text = gameObject.GetComponent<TMP_Text>();
            string parentPath = GetParentPath(transform);
            string parentBody = GetParentBodyName(transform);

            if (!string.Equals(previousParentPath, parentPath, System.StringComparison.Ordinal))
            {
                html.Append("<tr><td colspan=\"10\" style=\"background:#1b1b1b;font-weight:700\">Group: ").Append(EscapeHtml(parentPath)).AppendLine("</td></tr>");
                previousParentPath = parentPath;
            }

            html.Append("<tr>");
            html.Append("<td>").Append(EscapeHtml(parentBody)).Append("</td>");
            html.Append("<td class=\"mono\">").Append(EscapeHtml(parentPath)).Append("</td>");
            html.Append("<td class=\"mono\">").Append(EscapeHtml(GetTransformPath(transform))).Append("</td>");
            html.Append("<td>").Append(EscapeHtml(BoolState(gameObject.activeSelf, gameObject.activeInHierarchy))).Append("</td>");
            html.Append("<td>").Append(EscapeHtml(DescribeComponents(components))).Append("</td>");
            html.Append("<td>").Append(EscapeHtml(DescribeRectState(transform))).Append("</td>");
            html.Append("<td>").Append(DescribeImageHtml(image)).Append("</td>");
            html.Append("<td>").Append(EscapeHtml(DescribeInteractionState(gameObject))).Append("</td>");
            html.Append("<td>").Append(EscapeHtml(DescribeTextState(gameObject, text))).Append("</td>");
            html.Append("<td>").Append(EscapeHtml(DescribeLocalizationState(gameObject))).Append("</td>");
            html.AppendLine("</tr>");
        }

        html.AppendLine("</tbody></table></body></html>");
        return html.ToString();
    }

    // Appearance mockup intentionally disabled for now. The current reconstruction was not
    // accurate enough to trust, so the hierarchy and asset tables are the authoritative view.

    private static string BuildUiHierarchyHtml(Transform root)
    {
        if (root == null)
        {
            return "<div>No hierarchy available.</div>";
        }

        var html = new StringBuilder(8192);
        html.Append("<div class=\"card mono\"><pre>");
        AppendHierarchyNode(html, root, 0);
        html.Append("</pre></div>");
        return html.ToString();
    }

    private static void AppendHierarchyNode(StringBuilder html, Transform node, int depth)
    {
        if (node == null)
        {
            return;
        }

        GameObject gameObject = node.gameObject;
        Component[] components = gameObject.GetComponents<Component>();
        for (int i = 0; i < depth; i++)
        {
            html.Append("  ");
        }

        html.Append("- ")
            .Append(EscapeHtml(node.name))
            .Append(" [active=")
            .Append(gameObject.activeSelf ? "true" : "false")
            .Append("/hier=")
            .Append(gameObject.activeInHierarchy ? "true" : "false")
            .Append("] [components=")
            .Append(EscapeHtml(DescribeComponents(components)))
            .Append("]");

        if (node is RectTransform rect)
        {
            html.Append(" [rect=")
                .Append(EscapeHtml(DescribeRectState(rect)))
                .Append("]");
        }

        html.AppendLine();
        for (int childIndex = 0; childIndex < node.childCount; childIndex++)
        {
            AppendHierarchyNode(html, node.GetChild(childIndex), depth + 1);
        }
    }

    private static void AppendSummaryCard(StringBuilder html, string title, string body)
    {
        html.Append("<div class=\"card\"><h2>").Append(EscapeHtml(title)).Append("</h2>").Append(body).Append("</div>");
    }

    private static string FormatField(FieldInfo? field, object instance)
    {
        object? value = field?.GetValue(instance);
        return value != null ? EscapeHtml(value.ToString() ?? string.Empty) : "n/a";
    }

    private static string GetTransformPath(Transform transform)
    {
        if (transform == null)
        {
            return "<null>";
        }

        var segments = new Stack<string>();
        Transform current = transform;
        while (current != null)
        {
            segments.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", segments.ToArray());
    }

    private static int GetHierarchyDepth(Transform transform)
    {
        int depth = 0;
        Transform current = transform;
        while (current != null)
        {
            depth++;
            current = current.parent;
        }

        return depth;
    }

    private static string EscapeHtml(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }

    private static string DescribeHireUiConsistency(HireWorkerPanelUI? panel, bool actuallyHired)
    {
        if (panel == null)
        {
            return "panel-missing";
        }

        bool purchaseActive = panel.m_PurchaseBtn != null && panel.m_PurchaseBtn.activeSelf;
        bool lockActive = panel.m_LockPurchaseBtn != null && panel.m_LockPurchaseBtn.activeSelf;
        bool hiredActive = panel.m_HiredText != null && panel.m_HiredText.activeSelf;

        if (actuallyHired)
        {
            return hiredActive && !purchaseActive ? "matches-hired-state" : "mismatch-hired-state";
        }

        if (lockActive)
        {
            return "locked-or-level-gated";
        }

        return purchaseActive ? "matches-purchasable-state" : "missing-purchase-state";
    }

    private static string BoolState(bool activeSelf, bool activeInHierarchy)
    {
        return $"self={activeSelf} hierarchy={activeInHierarchy}";
    }

    private static string DescribeButtonState(GameObject? buttonObject)
    {
        if (buttonObject == null)
        {
            return "<none>";
        }

        Button? button = buttonObject.GetComponent<Button>();
        Image? image = buttonObject.GetComponent<Image>();
        TMP_Text? text = buttonObject.GetComponentInChildren<TMP_Text>(true);
        string interactable = button != null ? button.interactable.ToString() : "n/a";
        string targetGraphic = button?.targetGraphic != null ? button.targetGraphic.gameObject.name : "<none>";
        string imageRaycast = image != null ? image.raycastTarget.ToString() : "n/a";
        string label = text != null ? text.text : "<none>";
        return $"interactable={interactable} targetGraphic={targetGraphic} imageRaycast={imageRaycast} label={label}";
    }

    private static string DescribeInteractionState(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return string.Empty;
        }

        Button button = gameObject.GetComponent<Button>();
        Selectable selectable = gameObject.GetComponent<Selectable>();
        Graphic graphic = gameObject.GetComponent<Graphic>();
        string buttonState = DescribeButtonState(gameObject);
        string mouse = "no";
        if (button != null)
        {
            mouse = button.interactable && graphic != null && graphic.raycastTarget && gameObject.activeInHierarchy ? "yes" : "no";
        }
        else if (graphic != null)
        {
            mouse = graphic.raycastTarget && gameObject.activeInHierarchy ? "possible" : "no";
        }

        string path = GetTransformPath(gameObject.transform).ToLowerInvariant();
        string controller = "unknown";
        string focusVisibility = "always-or-unknown";
        if (path.Contains("controllerselectoruigrp") || path.Contains("controllerselectoruipos") || path.Contains("ctrlbtnselecthighlight"))
        {
            controller = "yes";
            focusVisibility = "controller-focus-only-likely";
        }
        else if (selectable != null || button != null)
        {
            controller = "possible";
        }

        return $"mouseClickable={mouse}; controllerSelectable={controller}; focusVisibility={focusVisibility}; {buttonState}";
    }

    private static string DescribeRectState(Transform transform)
    {
        if (transform is not RectTransform rect)
        {
            return "not-rect-transform";
        }

        Vector2 size = rect.rect.size;
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[2]) * 0.5f;
        return $"size=({size.x:0.##},{size.y:0.##}) anchored=({rect.anchoredPosition.x:0.##},{rect.anchoredPosition.y:0.##}) center=({center.x:0.##},{center.y:0.##},{center.z:0.##}) pivot=({rect.pivot.x:0.##},{rect.pivot.y:0.##}) vertices=[bl=({corners[0].x:0.##},{corners[0].y:0.##},{corners[0].z:0.##}), tl=({corners[1].x:0.##},{corners[1].y:0.##},{corners[1].z:0.##}), tr=({corners[2].x:0.##},{corners[2].y:0.##},{corners[2].z:0.##}), br=({corners[3].x:0.##},{corners[3].y:0.##},{corners[3].z:0.##})]";
    }

    private static string DescribeTextState(GameObject gameObject, TMP_Text ownText)
    {
        string selfText = ownText != null ? ownText.text : string.Empty;
        TMP_Text[] texts = gameObject != null ? gameObject.GetComponentsInChildren<TMP_Text>(true) : new TMP_Text[0];
        if (texts.Length == 0)
        {
            return selfText;
        }

        var parts = new List<string>(texts.Length);
        for (int index = 0; index < texts.Length; index++)
        {
            TMP_Text text = texts[index];
            if (text == null)
            {
                continue;
            }

            string value = text.text ?? string.Empty;
            if (value.Length == 0)
            {
                continue;
            }

            parts.Add(text.gameObject.name + "=" + value);
        }

        string subtreeText = string.Join(" | ", parts.ToArray());
        if (selfText.Length == 0)
        {
            return subtreeText;
        }

        return $"self={selfText}; subtree={subtreeText}";
    }

    private static string DescribeLocalizationState(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return string.Empty;
        }

        Localize localize = gameObject.GetComponent<Localize>();
        if (localize == null)
        {
            return string.Empty;
        }

        string primaryTerm = localize.Term ?? string.Empty;
        string secondaryTerm = localize.SecondaryTerm ?? string.Empty;
        string primaryTranslation = primaryTerm.Length > 0 ? LocalizationManager.GetTranslation(primaryTerm, FixForRTL: false) : string.Empty;
        string secondaryTranslation = secondaryTerm.Length > 0 ? LocalizationManager.GetTranslation(secondaryTerm, FixForRTL: false) : string.Empty;
        return $"term={primaryTerm}; translation={primaryTranslation}; secondaryTerm={secondaryTerm}; secondaryTranslation={secondaryTranslation}";
    }

    private static string GetParentPath(Transform transform)
    {
        if (transform?.parent == null)
        {
            return "<root>";
        }

        return GetTransformPath(transform.parent);
    }

    private static string GetParentBodyName(Transform transform)
    {
        if (transform == null)
        {
            return "<null>";
        }

        HireWorkerPanelUI panel = transform.GetComponentInParent<HireWorkerPanelUI>();
        if (panel != null)
        {
            return panel.gameObject.name;
        }

        return transform.parent != null ? transform.parent.name : "<root>";
    }

    private static string DescribeComponents(Component[] components)
    {
        if (components == null || components.Length == 0)
        {
            return string.Empty;
        }

        var names = new List<string>(components.Length);
        for (int index = 0; index < components.Length; index++)
        {
            Component component = components[index];
            if (component == null)
            {
                names.Add("<missing>");
            }
            else
            {
                names.Add(component.GetType().Name);
            }
        }

        return string.Join(", ", names.ToArray());
    }

    private static string DescribeImageHtml(Image image)
    {
        if (image == null)
        {
            return string.Empty;
        }

        Color color = image.color;
        string spriteName = image.sprite != null ? image.sprite.name : "<none>";
        string colorText = $"rgba({(int)(color.r * 255f)}, {(int)(color.g * 255f)}, {(int)(color.b * 255f)}, {color.a:0.###})";
        return $"<span class=\"swatch\" style=\"background:rgba({(int)(color.r * 255f)},{(int)(color.g * 255f)},{(int)(color.b * 255f)},{color.a.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)})\"></span>{EscapeHtml(colorText)} sprite={EscapeHtml(spriteName)} raycast={image.raycastTarget}";
    }
}
