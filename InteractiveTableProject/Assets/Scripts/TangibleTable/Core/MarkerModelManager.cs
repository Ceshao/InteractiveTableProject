using System;
using System.Collections.Generic;
using TangibleTable.Config;
using TangibleTable.Presentation;
using TuioNet.Tuio11;
using TuioUnity.Common;
using TuioUnity.Tuio11;
using UnityEngine;

namespace TangibleTable.Core
{
    /// <summary>
    /// 输入/调度层：监听 TUIO 1.1 Object 的 Add/Remove 事件，
    /// 按 SymbolID 查 MarkerContentLibrary，实例化并驱动对应模型。
    /// </summary>
    public class MarkerModelManager : MonoBehaviour
    {
        [SerializeField] private TuioSessionBehaviour _tuioSession;
        [SerializeField] private MarkerContentLibrary _library;
        [SerializeField] private Camera _targetCamera;

        private readonly Dictionary<int, TrackedModelController> _controllersBySymbol = new();
        private readonly Dictionary<uint, int> _symbolBySession = new();

        private Tuio11Dispatcher Dispatcher => (Tuio11Dispatcher)_tuioSession.TuioDispatcher;

        private void Awake()
        {
            if (_targetCamera == null) _targetCamera = Camera.main;
        }

        private void OnEnable()
        {
            try
            {
                Dispatcher.OnObjectAdd += HandleObjectAdd;
                Dispatcher.OnObjectRemove += HandleObjectRemove;
            }
            catch (InvalidCastException exception)
            {
                Debug.LogError($"[MarkerModelManager] 请检查 Tuio Session 的 TUIO 版本是否为 1.1。{exception.Message}");
            }
        }

        private void OnDisable()
        {
            try
            {
                Dispatcher.OnObjectAdd -= HandleObjectAdd;
                Dispatcher.OnObjectRemove -= HandleObjectRemove;
            }
            catch (InvalidCastException exception)
            {
                Debug.LogError($"[MarkerModelManager] 请检查 Tuio Session 的 TUIO 版本是否为 1.1。{exception.Message}");
            }
        }

        private void HandleObjectAdd(object sender, Tuio11Object tuioObject)
        {
            var symbolId = (int)tuioObject.SymbolId;
            if (!_library.TryGetMapping(symbolId, out var mapping))
            {
                Debug.Log($"[MarkerModelManager] 未配置的识别图 ID: {symbolId}，忽略。");
                return;
            }

            if (_symbolBySession.ContainsValue(symbolId))
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

            _symbolBySession[tuioObject.SessionId] = symbolId;
            controller.OnMarkerFound(tuioObject);
        }

        private void HandleObjectRemove(object sender, Tuio11Object tuioObject)
        {
            if (_symbolBySession.Remove(tuioObject.SessionId, out var symbolId)
                && _controllersBySymbol.TryGetValue(symbolId, out var controller))
            {
                controller.OnMarkerLost();
            }
        }
    }
}
