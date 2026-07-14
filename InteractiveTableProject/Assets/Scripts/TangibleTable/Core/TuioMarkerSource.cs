using System;
using System.Collections.Generic;
using TuioNet.Tuio11;
using TuioUnity.Common;
using TuioUnity.Tuio11;
using UnityEngine;

namespace TangibleTable.Core
{
    /// <summary>IMarkerSource 的 TUIO 直连实现：包装 TuioSessionBehaviour 的 Object 事件。</summary>
    public class TuioMarkerSource : MonoBehaviour, IMarkerSource
    {
        [SerializeField] private TuioSessionBehaviour _tuioSession;

        public event Action<ITrackedMarker> MarkerAdded;
        public event Action<ITrackedMarker> MarkerRemoved;

        private readonly Dictionary<uint, TuioTrackedMarker> _bySession = new();

        private Tuio11Dispatcher Dispatcher => (Tuio11Dispatcher)_tuioSession.TuioDispatcher;

        private void OnEnable()
        {
            try
            {
                Dispatcher.OnObjectAdd += HandleAdd;
                Dispatcher.OnObjectRemove += HandleRemove;
            }
            catch (InvalidCastException exception)
            {
                Debug.LogError($"[TuioMarkerSource] 请检查 Tuio Session 的 TUIO 版本是否为 1.1。{exception.Message}");
            }
        }

        private void OnDisable()
        {
            try
            {
                Dispatcher.OnObjectAdd -= HandleAdd;
                Dispatcher.OnObjectRemove -= HandleRemove;
            }
            catch (InvalidCastException) { }
        }

        private void HandleAdd(object sender, Tuio11Object tuioObject)
        {
            var marker = new TuioTrackedMarker(tuioObject);
            _bySession[tuioObject.SessionId] = marker;
            MarkerAdded?.Invoke(marker);
        }

        private void HandleRemove(object sender, Tuio11Object tuioObject)
        {
            if (_bySession.Remove(tuioObject.SessionId, out var marker))
                MarkerRemoved?.Invoke(marker);
        }

        private sealed class TuioTrackedMarker : ITrackedMarker
        {
            private readonly Tuio11Object _obj;

            public TuioTrackedMarker(Tuio11Object obj) => _obj = obj;

            public int SymbolId => (int)_obj.SymbolId;

            // 在数据入口做 X 镜像，桌面光标、按钮命中、转发投影的坐标一次性全部修正
            public Vector2 Position => new(
                MarkerInputMirror.InvertX ? 1f - _obj.Position.X : _obj.Position.X,
                _obj.Position.Y);

            public float Angle => _obj.Angle;
        }
    }
}
