using System.Collections.Generic;
using TangibleTable.Comm;
using TangibleTable.Core;
using UnityEngine;

namespace TangibleTable.TableUI
{
    /// <summary>
    /// 把 marker 连续状态（每帧全量快照）与 zone 离散事件通过 UDP 回环转发给投影程序。
    /// 状态帧里也带每个 marker 当前所在 zone，接收端漏掉事件也能从状态推导，双保险。
    /// </summary>
    public class MarkerStateBroadcaster : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _markerSourceBehaviour; // 需实现 IMarkerSource
        [SerializeField] private MarkerZoneDetector _detector;
        [SerializeField] private string _host = "127.0.0.1";
        [SerializeField] private int _port = MarkerWireProtocol.DefaultPort;

        private IMarkerSource _source;
        private UdpJsonSender _sender;
        private readonly HashSet<ITrackedMarker> _markers = new();
        private readonly StateMessage _state = new();

        private void Awake()
        {
            _source = _markerSourceBehaviour as IMarkerSource;
            if (_source == null)
            {
                Debug.LogError($"[MarkerStateBroadcaster] {nameof(_markerSourceBehaviour)} 未实现 IMarkerSource，已禁用。", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_source == null) return;
            _sender = new UdpJsonSender(_host, _port);
            _source.MarkerAdded += HandleAdded;
            _source.MarkerRemoved += HandleRemoved;
            if (_detector != null)
            {
                _detector.ZoneEntered += HandleZoneEntered;
                _detector.ZoneExited += HandleZoneExited;
            }
        }

        private void OnDisable()
        {
            if (_source == null) return;
            _source.MarkerAdded -= HandleAdded;
            _source.MarkerRemoved -= HandleRemoved;
            if (_detector != null)
            {
                _detector.ZoneEntered -= HandleZoneEntered;
                _detector.ZoneExited -= HandleZoneExited;
            }
            _sender?.Dispose();
            _sender = null;
        }

        private void HandleAdded(ITrackedMarker marker) => _markers.Add(marker);
        private void HandleRemoved(ITrackedMarker marker) => _markers.Remove(marker);

        private void HandleZoneEntered(ITrackedMarker marker, MarkerZone zone)
            => SendEvent(MarkerWireProtocol.ZoneEnter, marker, zone);

        private void HandleZoneExited(ITrackedMarker marker, MarkerZone zone)
            => SendEvent(MarkerWireProtocol.ZoneExit, marker, zone);

        private void SendEvent(string name, ITrackedMarker marker, MarkerZone zone)
        {
            var message = new EventMessage
            {
                name = name,
                markerId = marker.SymbolId,
                zoneId = zone.ZoneId
            };
            _sender.Send(JsonUtility.ToJson(message));
        }

        private void LateUpdate()
        {
            _state.markers.Clear();
            foreach (var marker in _markers)
            {
                _state.markers.Add(new MarkerWire
                {
                    id = marker.SymbolId,
                    x = marker.Position.x,
                    y = marker.Position.y,
                    angle = marker.Angle,
                    zone = _detector != null ? _detector.CurrentZoneOf(marker)?.ZoneId ?? "" : ""
                });
            }
            _sender.Send(JsonUtility.ToJson(_state));
        }
    }
}
