using System.Collections.Generic;
using TangibleTable.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TangibleTable.TableUI
{
    /// <summary>
    /// 在桌面界面上为每个 marker 画一个跟随圆环光标（含 ID 标签），
    /// 用于现场直观校验坐标映射是否对齐。挂在 Canvas 下的全屏节点上。
    /// </summary>
    public class MarkerCursorLayer : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _markerSourceBehaviour; // 需实现 IMarkerSource
        [SerializeField] private Sprite _cursorSprite;
        [SerializeField] private float _size = 120f;
        [SerializeField] private Color _color = new(0.3f, 0.9f, 1f, 0.9f);

        private IMarkerSource _source;
        private readonly Dictionary<ITrackedMarker, RectTransform> _cursors = new();

        private void Awake()
        {
            _source = _markerSourceBehaviour as IMarkerSource;
            if (_source == null)
            {
                Debug.LogError($"[MarkerCursorLayer] {nameof(_markerSourceBehaviour)} 未实现 IMarkerSource，已禁用。", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_source == null) return;
            _source.MarkerAdded += HandleAdded;
            _source.MarkerRemoved += HandleRemoved;
        }

        private void OnDisable()
        {
            if (_source == null) return;
            _source.MarkerAdded -= HandleAdded;
            _source.MarkerRemoved -= HandleRemoved;
        }

        private void HandleAdded(ITrackedMarker marker)
        {
            var go = new GameObject($"Cursor [id={marker.SymbolId}]", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            rect.sizeDelta = new Vector2(_size, _size);

            var image = go.GetComponent<Image>();
            image.sprite = _cursorSprite;
            image.color = _color;
            image.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.SetParent(rect, false);
            labelRect.sizeDelta = new Vector2(_size, _size * 0.5f);

            var label = labelGo.GetComponent<Text>();
            label.text = marker.SymbolId.ToString();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = Mathf.RoundToInt(_size * 0.35f);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;

            _cursors[marker] = rect;
        }

        private void HandleRemoved(ITrackedMarker marker)
        {
            if (_cursors.Remove(marker, out var rect) && rect != null)
                Destroy(rect.gameObject);
        }

        private void Update()
        {
            foreach (var pair in _cursors)
            {
                var screen = TuioScreenMapper.ToScreen(pair.Key.Position);
                pair.Value.position = new Vector3(screen.x, screen.y, 0f);
                pair.Value.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Rad2Deg * pair.Key.Angle);
            }
        }
    }
}
