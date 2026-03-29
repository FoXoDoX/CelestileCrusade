using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class DisplayDiagnostics : MonoBehaviour
{
    private void Start()
    {
        PrintDiagnostics("START");
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.f12Key.wasPressedThisFrame)
        {
            PrintDiagnostics("RUNTIME");
        }

        if (keyboard.f11Key.wasPressedThisFrame)
        {
            int nw = Display.main.systemWidth;
            int nh = Display.main.systemHeight;
            Screen.SetResolution(nw, nh, FullScreenMode.FullScreenWindow);
            Debug.Log($"[DIAG] Forced native: {nw}x{nh}");
        }

        if (keyboard.f10Key.wasPressedThisFrame)
        {
            Screen.SetResolution(2560, 1440, FullScreenMode.FullScreenWindow);
            Debug.Log("[DIAG] Forced 2560x1440 FullScreenWindow");
        }

        if (keyboard.f9Key.wasPressedThisFrame)
        {
            Screen.SetResolution(2560, 1440, FullScreenMode.Windowed);
            Debug.Log("[DIAG] Forced 2560x1440 Windowed");
        }

        if (keyboard.f8Key.wasPressedThisFrame)
        {
            // Пробуем Exclusive Fullscreen для сравнения
            Screen.SetResolution(2560, 1440, FullScreenMode.ExclusiveFullScreen);
            Debug.Log("[DIAG] Forced 2560x1440 ExclusiveFullScreen");
        }

        if (keyboard.f7Key.wasPressedThisFrame)
        {
            // Пробуем MaximizedWindow
            Screen.SetResolution(2560, 1440, FullScreenMode.MaximizedWindow);
            Debug.Log("[DIAG] Forced 2560x1440 MaximizedWindow");
        }
    }

    private void PrintDiagnostics(string context)
    {
        Debug.Log($"=== DIAGNOSTICS [{context}] ===");
        Debug.Log($"Screen: {Screen.width}x{Screen.height}");
        Debug.Log($"FullScreen Mode: {Screen.fullScreenMode}");
        Debug.Log($"FullScreen: {Screen.fullScreen}");
        Debug.Log($"Current Resolution: {Screen.currentResolution}");
        Debug.Log($"Native: {Display.main.systemWidth}x{Display.main.systemHeight}");
        Debug.Log($"DPI: {Screen.dpi}");
        Debug.Log($"Color Space: {QualitySettings.activeColorSpace}");

        // URP Asset
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset != null)
        {
            Debug.Log($"URP HDR: {urpAsset.supportsHDR}");
            Debug.Log($"URP MSAA: {urpAsset.msaaSampleCount}");
            Debug.Log($"URP Render Scale: {urpAsset.renderScale}");
            Debug.Log($"URP Upscaling Filter: {urpAsset.upscalingFilter}");
        }

        // Все камеры
        foreach (var cam in Camera.allCameras)
        {
            var additionalData = cam.GetComponent<UniversalAdditionalCameraData>();
            string aaValue = additionalData != null
                ? additionalData.antialiasing.ToString()
                : "N/A";
            string ppValue = additionalData != null
                ? additionalData.renderPostProcessing.ToString()
                : "N/A";

            Debug.Log($"Camera '{cam.name}': " +
                      $"HDR={cam.allowHDR}, " +
                      $"MSAA={cam.allowMSAA}, " +
                      $"Rect={cam.rect}, " +
                      $"PixelRect={cam.pixelRect}, " +
                      $"TargetTexture={cam.targetTexture?.name ?? "null"}, " +
                      $"AA={aaValue}, " +
                      $"PostProcess={ppValue}");
        }

        // Все Canvas
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            Debug.Log($"Canvas '{canvas.name}': " +
                      $"RenderMode={canvas.renderMode}, " +
                      $"ScaleMode={scaler?.uiScaleMode.ToString() ?? "N/A"}, " +
                      $"ScaleFactor={canvas.scaleFactor}, " +
                      $"SortingOrder={canvas.sortingOrder}");
        }

        // Все Volume
        foreach (var vol in FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            Debug.Log($"Volume '{vol.name}': " +
                      $"IsGlobal={vol.isGlobal}, " +
                      $"Weight={vol.weight}, " +
                      $"Profile={vol.profile?.name ?? "null"}, " +
                      $"Active={vol.gameObject.activeInHierarchy}");

            if (vol.profile != null)
            {
                foreach (var component in vol.profile.components)
                {
                    Debug.Log($"  -> {component.GetType().Name}: active={component.active}");
                }
            }
        }

        // Ambient Light
        Debug.Log($"Ambient Mode: {RenderSettings.ambientMode}");
        Debug.Log($"Ambient Color: {RenderSettings.ambientLight}");
        Debug.Log($"Ambient Intensity: {RenderSettings.ambientIntensity}");

        // 2D Lighting
        var lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        Debug.Log($"Light2D count: {lights.Length}");
        foreach (var light in lights)
        {
            Debug.Log($"Light2D '{light.name}': " +
                      $"Type={light.lightType}, " +
                      $"Intensity={light.intensity}, " +
                      $"Color={light.color}, " +
                      $"Enabled={light.enabled}, " +
                      $"Active={light.gameObject.activeInHierarchy}");
        }

        Debug.Log($"=== END [{context}] ===");
    }
}