using TangibleTable.Core;
using UnityEngine;

namespace TangibleTable.Presentation
{
    /// <summary>
    /// 投影端左上角的现场诊断面板：端口监听状态、收包计数、当前方块列表。
    /// F1 键显示/隐藏。POC 阶段默认显示，验收通过后可在场景里关掉。
    /// </summary>
    public class ProjectionDebugOverlay : MonoBehaviour
    {
        [SerializeField] private RemoteMarkerSource _source;
        [SerializeField] private bool _visible = true;

        private GUIStyle _style;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible || _source == null) return;
            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                normal = { textColor = Color.white },
                wordWrap = true,
            };
            var text = $"[投影诊断 F1 隐藏]\n{_source.StatusText}";
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(10f, 10f, 980f, 140f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(24f, 18f, 960f, 128f), text, _style);
        }
    }
}
