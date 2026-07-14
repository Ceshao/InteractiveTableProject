using TangibleTable.Core;
using UnityEngine;

namespace TangibleTable.TableUI
{
    /// <summary>
    /// F2 切换 X 轴镜像（适配现场 reacTIVision invert="x" 的部署）。
    /// 切换后提示 3 秒；镜像开启期间左下角常驻小标记，现场一眼能看出当前状态。
    /// </summary>
    public class MirrorToggleHotkey : MonoBehaviour
    {
        private const float HintSeconds = 3f;

        private GUIStyle _style;
        private float _hintUntil;

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.F2)) return;
            MarkerInputMirror.Toggle();
            _hintUntil = Time.unscaledTime + HintSeconds;
            Debug.Log($"[MirrorToggleHotkey] X 镜像已{(MarkerInputMirror.InvertX ? "开启" : "关闭")}。");
        }

        private void OnGUI()
        {
            var showHint = Time.unscaledTime < _hintUntil;
            if (!showHint && !MarkerInputMirror.InvertX) return;

            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                normal = { textColor = Color.white },
            };
            var text = showHint
                ? $"X 镜像:{(MarkerInputMirror.InvertX ? "开" : "关")}(F2 切换)"
                : "X 镜像:开";
            var rect = new Rect(10f, Screen.height - 60f, 460f, 44f);
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x + 14f, rect.y + 6f, rect.width - 28f, rect.height - 12f), text, _style);
        }
    }
}
