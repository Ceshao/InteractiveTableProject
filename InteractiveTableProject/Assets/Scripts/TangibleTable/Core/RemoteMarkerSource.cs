using System;
using System.Collections.Generic;
using TangibleTable.Comm;
using UnityEngine;

namespace TangibleTable.Core
{
    /// <summary>
    /// IMarkerSource 的远程实现：监听 TableUI 程序转发的 UDP JSON。
    /// 状态帧只含当前可见的 marker，超时未再出现即视为移除。
    /// </summary>
    public class RemoteMarkerSource : MonoBehaviour, IMarkerSource
    {
        [SerializeField] private int _port = MarkerWireProtocol.DefaultPort;
        [SerializeField, Min(0.1f)] private float _timeout = 0.5f;

        public event Action<ITrackedMarker> MarkerAdded;
        public event Action<ITrackedMarker> MarkerRemoved;
        /// <summary>离散事件（zone_enter / zone_exit），除连续状态外单独派发。</summary>
        public event Action<EventMessage> ZoneEventReceived;

        private UdpJsonReceiver _receiver;
        private readonly Dictionary<int, RemoteTrackedMarker> _byId = new();
        private readonly List<int> _staleIds = new();
        private string _listenError;
        private int _stateFrames;
        private int _events;
        private float _lastPacketTime = -1f;

        /// <summary>给现场诊断面板用的状态摘要。</summary>
        public string StatusText
        {
            get
            {
                if (_listenError != null) return $"端口 {_port} 监听失败：{_listenError}";
                var age = _lastPacketTime < 0f ? "从未收到" : $"{Time.unscaledTime - _lastPacketTime:F1}s 前";
                var markers = _byId.Count == 0 ? "无" : string.Join(", ", GetMarkerSummaries());
                return $"监听 UDP:{_port} | 状态帧 {_stateFrames} | 事件 {_events} | 最近数据 {age}\n当前方块: {markers}";
            }
        }

        private IEnumerable<string> GetMarkerSummaries()
        {
            foreach (var pair in _byId)
                yield return $"id={pair.Key}({pair.Value.Position.x:F2},{pair.Value.Position.y:F2})";
        }

        private void OnEnable()
        {
            try
            {
                _receiver = new UdpJsonReceiver(_port);
                _listenError = null;
            }
            catch (Exception exception)
            {
                _listenError = exception.Message;
                Debug.LogError($"[RemoteMarkerSource] 监听端口 {_port} 失败（是否有另一个实例占用？）：{exception.Message}");
            }
        }

        private void OnDisable()
        {
            _receiver?.Dispose();
            _receiver = null;
        }

        private void Update()
        {
            _receiver?.DrainTo(Handle);

            _staleIds.Clear();
            foreach (var pair in _byId)
                if (Time.unscaledTime - pair.Value.LastSeen > _timeout)
                    _staleIds.Add(pair.Key);

            foreach (var id in _staleIds)
            {
                var marker = _byId[id];
                _byId.Remove(id);
                MarkerRemoved?.Invoke(marker);
            }
        }

        private void Handle(string json)
        {
            _lastPacketTime = Time.unscaledTime;
            switch (MarkerWireProtocol.PeekType(json))
            {
                case "state":
                    _stateFrames++;
                    ApplyState(JsonUtility.FromJson<StateMessage>(json));
                    break;
                case "event":
                    _events++;
                    ZoneEventReceived?.Invoke(JsonUtility.FromJson<EventMessage>(json));
                    break;
            }
        }

        private void ApplyState(StateMessage state)
        {
            foreach (var wire in state.markers)
            {
                if (_byId.TryGetValue(wire.id, out var marker))
                {
                    marker.Apply(wire, Time.unscaledTime);
                }
                else
                {
                    marker = new RemoteTrackedMarker(wire.id);
                    marker.Apply(wire, Time.unscaledTime);
                    _byId.Add(wire.id, marker);
                    MarkerAdded?.Invoke(marker);
                }
            }
        }

        private sealed class RemoteTrackedMarker : ITrackedMarker
        {
            public RemoteTrackedMarker(int id) => SymbolId = id;

            public int SymbolId { get; }
            public Vector2 Position { get; private set; }
            public float Angle { get; private set; }
            public float LastSeen { get; private set; }

            public void Apply(MarkerWire wire, float now)
            {
                Position = new Vector2(wire.x, wire.y);
                Angle = wire.angle;
                LastSeen = now;
            }
        }
    }
}
