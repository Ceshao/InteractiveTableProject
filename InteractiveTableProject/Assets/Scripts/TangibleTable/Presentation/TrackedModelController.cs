using TangibleTable.Config;
using TangibleTable.Core;
using UnityEngine;

namespace TangibleTable.Presentation
{
    /// <summary>
    /// 表现层：让模型跟随一个 ITrackedMarker（活句柄，来源可以是 TUIO 直连或远程转发）。
    /// 由 MarkerModelManager 在实例化 Prefab 时挂载并驱动，Prefab 本身无需预挂任何脚本。
    /// </summary>
    public class TrackedModelController : MonoBehaviour
    {
        private ITrackedMarker _marker;
        private MarkerMapping _mapping;
        private Camera _camera;
        private float _depth;
        private Vector3 _velocity;
        private float _lostTime;
        private bool _isTracking;
        private Quaternion _baseRotation;

        public void Initialize(MarkerMapping mapping, Camera camera)
        {
            _mapping = mapping;
            _camera = camera;
            _depth = TuioWorldMapper.DepthOf(camera, transform.position);
            _baseRotation = transform.rotation;
        }

        public void OnMarkerFound(ITrackedMarker marker)
        {
            _marker = marker;
            _isTracking = true;
            gameObject.SetActive(true);
        }

        public void OnMarkerLost()
        {
            _marker = null;
            _isTracking = false;
            _lostTime = Time.time;
            if (_mapping.lostPolicy == MarkerLostPolicy.Hide)
                gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_isTracking)
            {
                var target = TuioWorldMapper.ToWorld(_camera, _marker.Position, _depth);
                transform.position = Vector3.SmoothDamp(
                    transform.position, target, ref _velocity, _mapping.smoothTime);

                if (_mapping.syncRotation)
                {
                    // 转盘式：方块在桌面上的转角 → 模型绕世界竖直轴旋转
                    var angleDeg = Mathf.Rad2Deg * _marker.Angle;
                    var targetRotation = Quaternion.AngleAxis(angleDeg, Vector3.up) * _baseRotation;
                    var t = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(_mapping.smoothTime, 1e-4f));
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
                }
            }
            else if (_mapping != null
                     && _mapping.lostPolicy == MarkerLostPolicy.HideAfterDelay
                     && Time.time - _lostTime > _mapping.hideDelay)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
