using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Golf.GamePlay;
using Golf.Rendering;
using Golf.Source;
using Golf.States;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class GolfRenderArtifactCheck
{
    private const string Stage = @"C:\Users\vital\Documents\Codex\2026-11-10\6-3-c-n1-n2-1-6\work\golf-ssr-artifact\";
    private const string Marker = "Golf.ScreenArtifactCheck";
    private static GameRound s_round;
    private static Camera s_camera;
    private static UniversalAdditionalCameraData s_cameraData;
    private static MobileSsrFeature s_ssr;
    private static UniversalRenderPipelineAsset s_pipeline;
    private static bool s_originalSsr;
    private static bool s_originalPost;
    private static bool s_originalMsaa;
    private static int s_originalQuality;
    private static float s_originalTimeScale;
    private static int s_frame;
    private static int s_index = -1;
    private static string s_file;
    private static bool s_requested;
    private static double s_deadline;
    private static readonly string[] Names =
    {
        "01-high-ssr-post-msaa", "02-high-no-ssr-post-msaa", "03-high-ssr-no-post-msaa",
        "04-high-no-ssr-no-post-msaa", "05-high-ssr-post-no-msaa", "06-high-no-ssr-post-no-msaa",
        "07-high-ssr-post-msaa-repeat"
    };

    static GolfRenderArtifactCheck()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
    }

    [MenuItem("Tools/Golf/Capture full Game View SSR comparison %#F10")]
    private static void Run()
    {
        File.WriteAllText(Stage + "check.txt", "START\n");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Log("FAIL Unity is already playing");
            return;
        }

        SessionState.SetBool(Marker, true);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Marker, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            try
            {
                s_originalQuality = QualitySettings.GetQualityLevel();
                s_originalTimeScale = Time.timeScale;
                s_camera = Camera.main;
                s_cameraData = s_camera.GetUniversalAdditionalCameraData();
                s_originalPost = s_cameraData.renderPostProcessing;
                s_originalMsaa = s_camera.allowMSAA;
                s_ssr = AssetDatabase.LoadAllAssetsAtPath("Assets/Settings/Mobile_Renderer.asset").OfType<MobileSsrFeature>().Single();
                s_originalSsr = s_ssr.isActive;
                s_pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset_High.asset");
                int high = Array.IndexOf(QualitySettings.names, "Mobile High");
                if (high < 0) throw new Exception("Missing High preset");
                QualitySettings.SetQualityLevel(high, true);
                s_round = Object.FindFirstObjectByType<GameRound>();
                Get<StateMachine>(Object.FindFirstObjectByType<Bootstrap>(), "m_stateMachine").ChangeState<GamePlayState>();
                EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
                s_deadline = EditorApplication.timeSinceStartup + 40d;
                EditorApplication.update += Tick;
                Log("PLAY " + SystemInfo.graphicsDeviceType + " pipeline=" + GraphicsSettings.currentRenderPipeline.name + " assetMsaa=" + s_pipeline.msaaSampleCount
                    + " cameraMsaa=" + s_camera.allowMSAA + " cameraCount=" + Camera.allCamerasCount + " width=" + Screen.width + " height=" + Screen.height);
            }
            catch (Exception error) { Finish("FAIL " + error); }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Marker, false);
            Log("EDIT MODE");
        }
    }

    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup >= s_deadline)
                throw new Exception("Timed out waiting for screen capture at index " + s_index);
            if (s_index < 0)
            {
                Car car = Get<Car>(s_round, "m_currentCar");
                if (car == null || !car.isReady) return;
                Time.timeScale = 0f;
                ConfigureNext();
                return;
            }

            if (Time.frameCount < s_frame + 6) return;

            if (!s_requested)
            {
                s_requested = true;
                s_frame = Time.frameCount;
                ScreenCapture.CaptureScreenshot(s_file);
                return;
            }

            if (!File.Exists(s_file)) return;
            Log("SAVED " + Names[s_index] + " bytes=" + new FileInfo(s_file).Length);
            if (s_index == Names.Length - 1)
                Finish("ALL CAPTURED");
            else
                ConfigureNext();
        }
        catch (Exception error) { Finish("FAIL " + error); }
    }

    private static void ConfigureNext()
    {
        s_index++;
        s_ssr.SetActive(s_index == 0 || s_index == 2 || s_index == 4 || s_index == 6);
        s_cameraData.renderPostProcessing = s_index != 2 && s_index != 3;
        s_camera.allowMSAA = s_index != 4 && s_index != 5;
        s_file = Stage + Names[s_index] + ".png";
        s_requested = false;
        s_frame = Time.frameCount;
    }

    private static void Finish(string text)
    {
        EditorApplication.update -= Tick;
        if (s_camera != null) s_camera.allowMSAA = s_originalMsaa;
        if (s_cameraData != null) s_cameraData.renderPostProcessing = s_originalPost;
        if (s_ssr != null) s_ssr.SetActive(s_originalSsr);
        QualitySettings.SetQualityLevel(s_originalQuality, true);
        Time.timeScale = s_originalTimeScale;
        Log(text);
        EditorApplication.ExitPlaymode();
    }

    private static T Get<T>(object instance, string field)
    {
        return (T)instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
    }

    private static void Log(string text)
    {
        File.AppendAllText(Stage + "check.txt", text + Environment.NewLine);
    }
}
