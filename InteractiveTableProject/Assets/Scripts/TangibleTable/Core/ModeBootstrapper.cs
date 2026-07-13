using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TangibleTable.Core
{
    /// <summary>
    /// 启动场景：按命令行参数 -mode table|projection 加载对应场景，
    /// 并按 -monitor N（1 基）把窗口主动搬到目标显示器——比只靠 Unity 的 -monitor
    /// 参数可靠（个别机器上该参数不生效，窗口都落在主屏）。
    /// </summary>
    public class ModeBootstrapper : MonoBehaviour
    {
        [SerializeField] private string _tableScene = "TableUI";
        [SerializeField] private string _projectionScene = "Projection";
        [SerializeField] private string _defaultMode = "projection";

        private void Start()
        {
            var mode = _defaultMode;
            var monitor = -1;
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals("-mode", StringComparison.OrdinalIgnoreCase))
                    mode = args[i + 1].Trim().ToLowerInvariant();
                if (args[i].Equals("-monitor", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(args[i + 1], out var parsed))
                    monitor = parsed;
            }

            var scene = mode == "table" ? _tableScene : _projectionScene;
            Debug.Log($"[ModeBootstrapper] mode={mode} monitor={monitor} → 加载场景 {scene}");
            StartCoroutine(MoveWindowThenLoad(monitor, scene));
        }

        private IEnumerator MoveWindowThenLoad(int monitor, string scene)
        {
            if (!Application.isEditor && monitor >= 1)
            {
                var displays = new List<DisplayInfo>();
                Screen.GetDisplayLayout(displays);
                Debug.Log($"[ModeBootstrapper] 检测到 {displays.Count} 个显示器。");
                if (monitor <= displays.Count)
                {
                    var target = displays[monitor - 1];
                    var move = Screen.MoveMainWindowTo(target, Vector2Int.zero);
                    yield return move;
                    Screen.SetResolution(target.width, target.height, FullScreenMode.FullScreenWindow);
                    Debug.Log($"[ModeBootstrapper] 窗口已移动到显示器 {monitor}（{target.width}x{target.height}）。");
                }
                else
                {
                    Debug.LogWarning($"[ModeBootstrapper] 显示器 {monitor} 不存在（共 {displays.Count} 个），留在主屏。");
                }
            }

            SceneManager.LoadScene(scene);
        }
    }
}
