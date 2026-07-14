using System.Collections.Generic;
using TangibleTable.Core;
using TangibleTable.Presentation;
using TangibleTable.TableUI;
using TuioNet.Common;
using TuioUnity.Common;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TangibleTable.EditorTools
{
    /// <summary>
    /// 一键构建 POC 三场景：Bootstrap（按 -mode 分流）、TableUI（桌面屏）、Projection（投影）。
    /// 可重复执行，每次全量重建并覆盖保存。
    /// </summary>
    public static class PocSceneBuilder
    {
        private const string ScenesDir = "Assets/Scenes";
        private const string BootstrapPath = ScenesDir + "/Bootstrap.unity";
        private const string TableUiPath = ScenesDir + "/TableUI.unity";
        private const string ProjectionPath = ScenesDir + "/Projection.unity";
        private const string LibraryPath = "Assets/Settings/MarkerContentLibrary.asset";

        private static readonly string[] ZoneIds = { "A", "B", "C" };

        [MenuItem("TangibleTable/构建 POC 场景（Bootstrap + TableUI + Projection）")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            BuildBootstrapScene();
            BuildTableUiScene();
            BuildProjectionScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapPath, true),
                new EditorBuildSettingsScene(TableUiPath, true),
                new EditorBuildSettingsScene(ProjectionPath, true),
            };

            AssetDatabase.SaveAssets();
            Debug.Log("[PocSceneBuilder] 三个场景已生成并加入 Build Settings。");
        }

        // ---------- Bootstrap ----------

        private static void BuildBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Bootstrapper", typeof(ModeBootstrapper));
            // 空场景也要有相机，避免加载瞬间黑屏报警告
            CreateCamera(new Color(0f, 0f, 0f));
            EditorSceneManager.SaveScene(scene, BootstrapPath);
        }

        // ---------- TableUI ----------

        private static void BuildTableUiScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera(new Color(0.05f, 0.07f, 0.12f));

            // TUIO 接入（配置按项目记忆固定为 UDP/127.0.0.1/3333/Tuio11）
            var sessionGo = new GameObject("Tuio Session");
            var session = sessionGo.AddComponent<TuioSessionBehaviour>();
            session.TuioVersion = TuioVersion.Tuio11;
            session.ConnectionType = TuioConnectionType.UDP;
            session.UdpPort = 3333;
            SetString(session, "_ipAddress", "127.0.0.1");

            var sourceGo = new GameObject("Marker Source");
            var source = sourceGo.AddComponent<TuioMarkerSource>();
            SetRef(source, "_tuioSession", session);

            // Canvas + 三个按钮区
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            CreateText(canvasGo.transform, "Title", "方块放到按钮上切换投影展品",
                new Vector2(0f, 440f), new Vector2(1600f, 100f), 44, new Color(1f, 1f, 1f, 0.85f));

            var zonePositions = new[] { new Vector2(-620f, -120f), new Vector2(0f, -120f), new Vector2(620f, -120f) };
            for (var i = 0; i < ZoneIds.Length; i++)
                CreateZone(canvasGo.transform, ZoneIds[i], zonePositions[i]);

            // 命中判断
            var detectorGo = new GameObject("Zone Detector");
            var detector = detectorGo.AddComponent<MarkerZoneDetector>();
            SetRef(detector, "_markerSourceBehaviour", source);

            // 方块光标层（铺满 Canvas）
            var cursorGo = new GameObject("Cursor Layer", typeof(RectTransform));
            var cursorRect = (RectTransform)cursorGo.transform;
            cursorRect.SetParent(canvasGo.transform, false);
            cursorRect.anchorMin = Vector2.zero;
            cursorRect.anchorMax = Vector2.one;
            cursorRect.offsetMin = Vector2.zero;
            cursorRect.offsetMax = Vector2.zero;
            var cursorLayer = cursorGo.AddComponent<MarkerCursorLayer>();
            SetRef(cursorLayer, "_markerSourceBehaviour", source);
            SetRef(cursorLayer, "_cursorSprite", AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"));

            // 状态与事件转发
            var broadcasterGo = new GameObject("State Broadcaster");
            var broadcaster = broadcasterGo.AddComponent<MarkerStateBroadcaster>();
            SetRef(broadcaster, "_markerSourceBehaviour", source);
            SetRef(broadcaster, "_detector", detector);

            // 现场适配：F2 切换 X 镜像（reacTIVision invert="x" 的场地）
            new GameObject("Mirror Hotkey", typeof(MirrorToggleHotkey));

            EditorSceneManager.SaveScene(scene, TableUiPath);
        }

        private static void CreateZone(Transform canvas, string zoneId, Vector2 anchoredPosition)
        {
            var go = new GameObject($"Zone {zoneId}", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvas, false);
            rect.sizeDelta = new Vector2(440f, 440f);
            rect.anchoredPosition = anchoredPosition;

            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.15f);
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;

            var zone = go.AddComponent<MarkerZone>();
            SetString(zone, "_zoneId", zoneId);
            SetRef(zone, "_highlightTarget", image);

            CreateText(rect, "Label", $"{zoneId} 区",
                Vector2.zero, new Vector2(420f, 200f), 48, Color.white);
        }

        // ---------- Projection ----------

        private static void BuildProjectionScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 库里的模型 Prefab 自带大缩放（包围盒约 6m 高），相机拉远拉高才能完整取景
            var camera = CreateCamera(new Color(0.12f, 0.14f, 0.18f));
            camera.transform.position = new Vector3(0f, 3.5f, -10f);
            camera.transform.rotation = Quaternion.identity;

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var remoteGo = new GameObject("Remote Marker Source");
            var remote = remoteGo.AddComponent<RemoteMarkerSource>();

            // 方块放上桌面 → 按配置表出现对应模型，跟随移动与旋转（与旧示例场景同款行为，数据源换成远程转发）
            var library = AssetDatabase.LoadAssetAtPath<ScriptableObject>(LibraryPath);
            if (library == null)
                Debug.LogWarning($"[PocSceneBuilder] 未找到 {LibraryPath}，MarkerModelManager 的配置表引用为空，需手动指定。");

            var managerGo = new GameObject("Marker Model Manager");
            var manager = managerGo.AddComponent<MarkerModelManager>();
            SetRef(manager, "_markerSourceBehaviour", remote);
            SetRef(manager, "_library", library);
            SetRef(manager, "_targetCamera", camera);

            // 现场诊断面板：端口状态 + 收包计数 + 当前方块，F1 隐藏
            var overlayGo = new GameObject("Debug Overlay");
            var overlay = overlayGo.AddComponent<ProjectionDebugOverlay>();
            SetRef(overlay, "_source", remote);

            EditorSceneManager.SaveScene(scene, ProjectionPath);
        }

        // ---------- 通用 ----------

        private static Camera CreateCamera(Color background)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            go.AddComponent<AudioListener>();
            return camera;
        }

        private static void CreateText(Transform parent, string name, string content,
            Vector2 anchoredPosition, Vector2 size, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
