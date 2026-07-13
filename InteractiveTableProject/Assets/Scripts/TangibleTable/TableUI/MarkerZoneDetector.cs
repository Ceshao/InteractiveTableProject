using System;
using System.Collections.Generic;
using TangibleTable.Core;
using UnityEngine;

namespace TangibleTable.TableUI
{
    /// <summary>
    /// 命中判断：把每个 marker 的 TUIO 坐标换算成屏幕坐标，判定落在哪个 MarkerZone 内。
    /// 进入/离开需在去抖时间内保持稳定才触发事件，防止方块在按钮边缘抖动导致事件连发。
    /// </summary>
    public class MarkerZoneDetector : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _markerSourceBehaviour; // 需实现 IMarkerSource
        [SerializeField, Min(0f)] private float _debounce = 0.2f;
        [SerializeField] private Camera _uiCamera; // Overlay Canvas 留空即可

        public event Action<ITrackedMarker, MarkerZone> ZoneEntered;
        public event Action<ITrackedMarker, MarkerZone> ZoneExited;

        private IMarkerSource _source;
        private MarkerZone[] _zones;
        private readonly Dictionary<ITrackedMarker, Tracking> _tracking = new();

        private class Tracking
        {
            public MarkerZone Current;
            public MarkerZone Candidate;
            public float CandidateSince;
        }

        /// <summary>marker 当前判定所在的 zone，未命中返回 null。供广播器组装状态帧。</summary>
        public MarkerZone CurrentZoneOf(ITrackedMarker marker)
        {
            return _tracking.TryGetValue(marker, out var t) ? t.Current : null;
        }

        private void Awake()
        {
            _source = _markerSourceBehaviour as IMarkerSource;
            if (_source == null)
            {
                Debug.LogError($"[MarkerZoneDetector] {nameof(_markerSourceBehaviour)} 未实现 IMarkerSource，已禁用。", this);
                enabled = false;
                return;
            }
            _zones = FindObjectsOfType<MarkerZone>(true);
            if (_zones.Length == 0)
                Debug.LogWarning("[MarkerZoneDetector] 场景中没有 MarkerZone。");
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
            _tracking[marker] = new Tracking();
        }

        private void HandleRemoved(ITrackedMarker marker)
        {
            if (_tracking.Remove(marker, out var t) && t.Current != null)
            {
                t.Current.OnMarkerExit();
                ZoneExited?.Invoke(marker, t.Current);
            }
        }

        private void Update()
        {
            foreach (var pair in _tracking)
            {
                var marker = pair.Key;
                var t = pair.Value;
                var zone = ZoneAt(TuioScreenMapper.ToScreen(marker.Position));

                if (zone != t.Candidate)
                {
                    t.Candidate = zone;
                    t.CandidateSince = Time.unscaledTime;
                }

                if (t.Candidate != t.Current && Time.unscaledTime - t.CandidateSince >= _debounce)
                {
                    if (t.Current != null)
                    {
                        t.Current.OnMarkerExit();
                        ZoneExited?.Invoke(marker, t.Current);
                    }
                    t.Current = t.Candidate;
                    if (t.Current != null)
                    {
                        t.Current.OnMarkerEnter();
                        ZoneEntered?.Invoke(marker, t.Current);
                    }
                }
            }
        }

        private MarkerZone ZoneAt(Vector2 screenPoint)
        {
            foreach (var zone in _zones)
            {
                if (zone.isActiveAndEnabled
                    && RectTransformUtility.RectangleContainsScreenPoint(zone.Rect, screenPoint, _uiCamera))
                    return zone;
            }
            return null;
        }
    }
}
