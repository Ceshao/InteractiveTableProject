using System.Collections.Generic;
using TangibleTable.Config;
using TangibleTable.Presentation;
using UnityEngine;

namespace TangibleTable.Core
{
    /// <summary>
    /// 输入/调度层：监听 IMarkerSource 的 Add/Remove 事件，
    /// 按 SymbolID 查 MarkerContentLibrary，实例化并驱动对应模型。
    /// 数据源可以是 TuioMarkerSource（直连）或 RemoteMarkerSource（转发），场景里换组件即可。
    /// </summary>
    public class MarkerModelManager : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _markerSourceBehaviour; // 需实现 IMarkerSource
        [SerializeField] private MarkerContentLibrary _library;
        [SerializeField] private Camera _targetCamera;

        private IMarkerSource _source;
        private readonly Dictionary<int, TrackedModelController> _controllersBySymbol = new();
        private readonly Dictionary<ITrackedMarker, int> _symbolByMarker = new();

        private void Awake()
        {
            if (_targetCamera == null) _targetCamera = Camera.main;

            _source = _markerSourceBehaviour as IMarkerSource;
            if (_source == null)
            {
                Debug.LogError($"[MarkerModelManager] {nameof(_markerSourceBehaviour)} 未实现 IMarkerSource，已禁用。", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_source == null) return;
            _source.MarkerAdded += HandleMarkerAdded;
            _source.MarkerRemoved += HandleMarkerRemoved;
        }

        private void OnDisable()
        {
            if (_source == null) return;
            _source.MarkerAdded -= HandleMarkerAdded;
            _source.MarkerRemoved -= HandleMarkerRemoved;
        }

        private void HandleMarkerAdded(ITrackedMarker marker)
        {
            var symbolId = marker.SymbolId;
            if (!_library.TryGetMapping(symbolId, out var mapping))
            {
                Debug.Log($"[MarkerModelManager] 未配置的识别图 ID: {symbolId}，忽略。");
                return;
            }

            if (_symbolByMarker.ContainsValue(symbolId))
            {
                Debug.LogWarning($"[MarkerModelManager] 识别图 ID {symbolId} 已在追踪中，忽略重复放置。");
                return;
            }

            if (!_controllersBySymbol.TryGetValue(symbolId, out var controller))
            {
                var instance = Instantiate(mapping.modelPrefab);
                instance.name = $"{mapping.modelPrefab.name} [id={symbolId}]";
                controller = instance.AddComponent<TrackedModelController>();
                controller.Initialize(mapping, _targetCamera);
                _controllersBySymbol.Add(symbolId, controller);
            }

            _symbolByMarker[marker] = symbolId;
            controller.OnMarkerFound(marker);
        }

        private void HandleMarkerRemoved(ITrackedMarker marker)
        {
            if (_symbolByMarker.Remove(marker, out var symbolId)
                && _controllersBySymbol.TryGetValue(symbolId, out var controller))
            {
                controller.OnMarkerLost();
            }
        }
    }
}
