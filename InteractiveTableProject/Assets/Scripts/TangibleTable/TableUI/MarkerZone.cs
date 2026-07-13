using UnityEngine;
using UnityEngine.UI;

namespace TangibleTable.TableUI
{
    /// <summary>桌面界面上的一个命中区域（按钮），挂在带 RectTransform 的 UI 元素上。</summary>
    [RequireComponent(typeof(RectTransform))]
    public class MarkerZone : MonoBehaviour
    {
        [SerializeField] private string _zoneId = "A";
        [SerializeField] private Graphic _highlightTarget;
        [SerializeField] private Color _idleColor = new(1f, 1f, 1f, 0.15f);
        [SerializeField] private Color _activeColor = new(0.2f, 0.8f, 0.4f, 0.6f);

        public string ZoneId => _zoneId;
        public RectTransform Rect => (RectTransform)transform;

        private int _occupants;

        public void OnMarkerEnter()
        {
            _occupants++;
            Refresh();
        }

        public void OnMarkerExit()
        {
            _occupants = Mathf.Max(0, _occupants - 1);
            Refresh();
        }

        private void Awake()
        {
            if (_highlightTarget == null) _highlightTarget = GetComponent<Graphic>();
            Refresh();
        }

        private void Refresh()
        {
            if (_highlightTarget != null)
                _highlightTarget.color = _occupants > 0 ? _activeColor : _idleColor;
        }
    }
}
