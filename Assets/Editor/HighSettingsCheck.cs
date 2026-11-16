using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Golf;
using Golf.Rendering;
using Golf.States;
using Golf.View;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class HighSettingsCheck
{
    private const string Marker = "Golf.Codex.HighSettingsCheck.2";
    private const string Playing = Marker + ".playing";
    private static double s_editorDeadline;
    private static GraphicRaycaster s_testRaycaster;

    static HighSettingsCheck()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        if (!SessionState.GetBool(Marker, false))
        {
            s_editorDeadline = EditorApplication.timeSinceStartup + 35;
            EditorApplication.update += Open;
        }
    }

    private static void Open()
    {
        if (EditorApplication.isUpdating || EditorApplication.isCompiling)
            return;
        if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.timeSinceStartup < s_editorDeadline)
            return;
        EditorApplication.update -= Open;
        SessionState.SetBool(Marker, true);
        if (SceneManager.GetActiveScene().name != "GamePlay")
        {
            Debug.Log($"GOLF_HIGH_SETTINGS_CHECK SKIP opened scene is {SceneManager.GetActiveScene().name}");
            return;
        }
        if (EditorApplication.isPlaying)
        {
            RunInOpenSession();
            return;
        }
        SessionState.SetBool(Playing, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Playing, false))
            return;
        if (state == PlayModeStateChange.EnteredPlayMode)
            EditorApplication.delayCall += Run;
        else if (state == PlayModeStateChange.ExitingPlayMode)
            SessionState.SetBool(Playing, false);
    }

    private static void Run()
    {
        int oldQuality = QualitySettings.GetQualityLevel();
        try
        {
            var menu = Object.FindFirstObjectByType<MainMenuView>(FindObjectsInactive.Include);
            var settings = Object.FindFirstObjectByType<SettingsView>(FindObjectsInactive.Include);
            var settingsButton = ButtonField(menu, "m_settingsButton");
            var high = ButtonField(settings, "m_highButton");
            var low = ButtonField(settings, "m_lowButton");
            var back = ButtonField(settings, "m_backButton");
            Click(settingsButton);
            Require(settings.isActiveAndEnabled, "Settings did not open from the main menu");
            CheckSelection(settings);

            Click(back);
            Click(settingsButton);
            RequireQuality("Mobile High", "Mobile_RPAsset_High");
            Require(!high.interactable, "Reopened settings lost the active preset");
            Click(low);
            RequireQuality("Mobile Low", "Mobile_RPAsset_Low");
            Click(high);
            RequireQuality("Mobile High", "Mobile_RPAsset_High");
            Debug.Log("GOLF_HIGH_SETTINGS_CHECK PASS actual menu, header repro, High selection and SSR, Low and High reselection");
        }
        catch (Exception error)
        {
            Debug.LogError($"GOLF_HIGH_SETTINGS_CHECK FAIL {error}");
        }
        finally
        {
            QualitySettings.SetQualityLevel(oldQuality, true);
            EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
        }
    }

    private static void RunInOpenSession()
    {
        int oldQuality = QualitySettings.GetQualityLevel();
        var root = new GameObject("Temporary settings check", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
        SettingsState state = null;
        try
        {
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -32760;
            s_testRaycaster = root.AddComponent<GraphicRaycaster>();
            var source = Object.FindFirstObjectByType<SettingsView>(FindObjectsInactive.Include);
            var settings = Object.Instantiate(source, root.transform, false);
            state = new SettingsState(new StateMachine(), settings);
            state.Enter();
            state.Activate();
            CheckSelection(settings);
            Click(ButtonField(settings, "m_lowButton"));
            RequireQuality("Mobile Low", "Mobile_RPAsset_Low");
            Click(ButtonField(settings, "m_highButton"));
            RequireQuality("Mobile High", "Mobile_RPAsset_High");
            Debug.Log("GOLF_HIGH_SETTINGS_CHECK PASS isolated menu copy, header repro, High selection and SSR");
        }
        catch (Exception error)
        {
            Debug.LogError($"GOLF_HIGH_SETTINGS_CHECK FAIL {error}");
        }
        finally
        {
            state?.Deactivate();
            s_testRaycaster = null;
            Object.DestroyImmediate(root);
            QualitySettings.SetQualityLevel(oldQuality, true);
        }
    }

    private static void CheckSelection(SettingsView settings)
    {
        var high = ButtonField(settings, "m_highButton");
        var medium = ButtonField(settings, "m_mediumButton");
        var low = ButtonField(settings, "m_lowButton");
        var header = settings.transform.Find("Graphics_Text").GetComponent<TMP_Text>();
        Require(!header.raycastTarget, "Graphics header still accepts rays after menu initialization");
        header.raycastTarget = true;
        var originalTarget = Hit(high);
        header.raycastTarget = false;
        Require(originalTarget == header.gameObject, $"Expected the old header to intercept High, got {originalTarget?.name}");
        Debug.Log("GOLF_HIGH_SETTINGS_CHECK original High tap intercepted by Graphics_Text");

        var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/Mobile_Renderer.asset");
        var feature = rendererData.rendererFeatures.OfType<MobileSsrFeature>().Single();
        Require(feature.isActive, "SSR renderer feature is disabled");
        if (medium.interactable)
            Click(medium);
        RequireQuality("Mobile", "Mobile_RPAsset");
        CheckRender(feature, false);

        Click(high);
        RequireQuality("Mobile High", "Mobile_RPAsset_High");
        Require(!high.interactable && medium.interactable && low.interactable, "The High button was not marked selected");
        CheckRender(feature, true);
    }

    private static Button ButtonField(object instance, string field) =>
        (Button)instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);

    private static void RequireQuality(string name, string pipeline)
    {
        Require(QualitySettings.names[QualitySettings.GetQualityLevel()] == name && GraphicsSettings.currentRenderPipeline.name == pipeline,
            $"Unexpected quality or pipeline, expected {name} and {pipeline}");
        Debug.Log($"GOLF_HIGH_SETTINGS_CHECK selected {name}, pipeline {pipeline}");
    }

    private static GameObject Hit(Button button) => Hit(button, out _);

    private static GameObject Hit(Button button, out PointerEventData pointer)
    {
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)button.transform;
        var canvas = button.GetComponentInParent<Canvas>().rootCanvas;
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var screen = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
        pointer = new PointerEventData(EventSystem.current) { position = screen, button = PointerEventData.InputButton.Left };
        var hits = new List<RaycastResult>();
        if (s_testRaycaster != null)
            s_testRaycaster.Raycast(pointer, hits);
        else
            EventSystem.current.RaycastAll(pointer, hits);
        return hits.Count > 0 ? hits[0].gameObject : null;
    }

    private static void Click(Button button)
    {
        var target = Hit(button, out var pointer);
        Require(button.IsInteractable() && target != null && ExecuteEvents.GetEventHandler<IPointerClickHandler>(target) == button.gameObject,
            $"Click on {button.name} hit {target?.name}");
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

    private static void CheckRender(MobileSsrFeature feature, bool expectReflection)
    {
        bool oldState = feature.isActive;
        var root = new GameObject("SSR render check") { hideFlags = HideFlags.HideAndDontSave };
        var target = new RenderTexture(480, 270, 24, RenderTextureFormat.ARGB32) { antiAliasing = expectReflection ? 2 : 1 };
        var readback = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        var gray = new Material(unlit) { color = new Color(0.14f, 0.14f, 0.14f) };
        var red = new Material(unlit) { color = new Color(1f, 0.025f, 0.025f) };
        try
        {
            var cameraObject = new GameObject("SSR test camera");
            cameraObject.transform.SetParent(root.transform);
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.backgroundColor = Color.black;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.cullingMask = 1 << 30;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 40f;
            camera.fieldOfView = 50f;
            var center = new Vector3(1000f, 0f, 1000f);
            camera.transform.position = center + new Vector3(0f, 2.6f, -5f);
            camera.transform.LookAt(center + new Vector3(0f, 0.65f, 1.4f));

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.SetParent(root.transform);
            floor.transform.position = center;
            floor.layer = 30;
            var floorRenderer = floor.GetComponent<Renderer>();
            floorRenderer.sharedMaterial = gray;
            floorRenderer.renderingLayerMask = 3;

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(root.transform);
            cube.transform.position = center + new Vector3(0f, 0.8f, 2f);
            cube.transform.localScale = new Vector3(1.4f, 1.6f, 1.4f);
            cube.layer = 30;
            cube.GetComponent<Renderer>().sharedMaterial = red;

            feature.SetActive(false);
            var before = Render(camera, target, readback);
            feature.SetActive(true);
            var after = Render(camera, target, readback);

            int directPixels = 0;
            int floorPixels = 0;
            int reflectedPixels = 0;
            for (int i = 0; i < before.Length; i++)
            {
                var sample = before[i];
                var withSsr = after[i];
                if (sample.r > 180 && sample.g < 80)
                    directPixels++;
                if (sample.r > 10 && sample.r < 180 && Math.Abs(sample.r - sample.g) <= 2)
                {
                    floorPixels++;
                    if (withSsr.r > sample.r + 3 && withSsr.r > withSsr.g + 4)
                        reflectedPixels++;
                }
            }
            Debug.Log($"GOLF_HIGH_SETTINGS_CHECK render {GraphicsSettings.currentRenderPipeline.name}: reflected={reflectedPixels}");
            Require(directPixels > 100 && floorPixels > 100 && (expectReflection ? reflectedPixels > 30 : reflectedPixels == 0),
                "SSR did not match the selected quality preset");
        }
        finally
        {
            feature.SetActive(oldState);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(readback);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(gray);
            Object.DestroyImmediate(red);
        }
    }

    private static Color32[] Render(Camera camera, RenderTexture target, Texture2D readback)
    {
        var active = RenderTexture.active;
        try
        {
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            readback.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0, false);
            readback.Apply();
            return readback.GetPixels32();
        }
        finally
        {
            RenderTexture.active = active;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
