using System;
using System.Collections.Generic;
using TangibleTable.Comm;
using TangibleTable.Core;
using UnityEngine;

namespace TangibleTable.Presentation
{
    [Serializable]
    public class ZoneExhibit
    {
        public string zoneId;
        public GameObject exhibitPrefab;
    }

    /// <summary>
    /// 投影端：收到 zone_enter 事件后切换展品（居中显示在舞台锚点），
    /// 展品旋转持续跟随触发它的那个方块（连续状态驱动）。zone_exit 不做处理，展品保持展示。
    /// </summary>
    public class ExhibitSwitcher : MonoBehaviour
    {
        [SerializeField] private RemoteMarkerSource _remoteSource;
        [SerializeField] private Transform _stage; // 展品锚点；留空则用世界原点
        [SerializeField] private List<ZoneExhibit> _exhibits = new();
        [SerializeField, Min(0f)] private float _rotationSmoothTime = 0.1f;

        private readonly Dictionary<string, Exhibit> _instances = new();
        private readonly Dictionary<int, ITrackedMarker> _markersById = new();
        private Exhibit _active;
        private ITrackedMarker _driver;

        private class Exhibit
        {
            public GameObject Root;
            public Quaternion BaseRotation;
        }

        private void OnEnable()
        {
            _remoteSource.MarkerAdded += HandleMarkerAdded;
            _remoteSource.MarkerRemoved += HandleMarkerRemoved;
            _remoteSource.ZoneEventReceived += HandleZoneEvent;
        }

        private void OnDisable()
        {
            _remoteSource.MarkerAdded -= HandleMarkerAdded;
            _remoteSource.MarkerRemoved -= HandleMarkerRemoved;
            _remoteSource.ZoneEventReceived -= HandleZoneEvent;
        }

        private void HandleMarkerAdded(ITrackedMarker marker) => _markersById[marker.SymbolId] = marker;

        private void HandleMarkerRemoved(ITrackedMarker marker)
        {
            _markersById.Remove(marker.SymbolId);
            if (_driver == marker) _driver = null; // 展品停在当前角度
        }

        private void HandleZoneEvent(EventMessage message)
        {
            if (message.name != MarkerWireProtocol.ZoneEnter) return;

            var config = _exhibits.Find(e => e.zoneId == message.zoneId);
            if (config == null || config.exhibitPrefab == null)
            {
                Debug.Log($"[ExhibitSwitcher] zone {message.zoneId} 未配置展品，忽略。");
                return;
            }

            Show(config);
            _markersById.TryGetValue(message.markerId, out _driver);
        }

        private void Show(ZoneExhibit config)
        {
            if (!_instances.TryGetValue(config.zoneId, out var exhibit))
            {
                var position = _stage != null ? _stage.position : Vector3.zero;
                var root = Instantiate(config.exhibitPrefab, position, config.exhibitPrefab.transform.rotation);
                root.name = $"{config.exhibitPrefab.name} [zone={config.zoneId}]";
                exhibit = new Exhibit { Root = root, BaseRotation = root.transform.rotation };
                _instances.Add(config.zoneId, exhibit);
            }

            if (_active == exhibit) return;
            if (_active != null) _active.Root.SetActive(false);
            _active = exhibit;
            _active.Root.SetActive(true);
        }

        private void Update()
        {
            if (_active == null || _driver == null) return;

            // 转盘式：方块转角 → 展品绕世界竖直轴旋转（与 TrackedModelController 同步手感）
            var angleDeg = Mathf.Rad2Deg * _driver.Angle;
            var targetRotation = Quaternion.AngleAxis(angleDeg, Vector3.up) * _active.BaseRotation;
            var t = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(_rotationSmoothTime, 1e-4f));
            _active.Root.transform.rotation = Quaternion.Slerp(_active.Root.transform.rotation, targetRotation, t);
        }
    }
}
