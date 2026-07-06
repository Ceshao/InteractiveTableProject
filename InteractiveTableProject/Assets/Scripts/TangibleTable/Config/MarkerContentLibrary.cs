using System;
using System.Collections.Generic;
using UnityEngine;

namespace TangibleTable.Config
{
    public enum MarkerLostPolicy
    {
        KeepInPlace,    // 丢失后停在原地（默认，方块常驻桌面）
        Hide,           // 丢失立即隐藏
        HideAfterDelay  // 丢失超过 hideDelay 秒后隐藏
    }

    [Serializable]
    public class MarkerMapping
    {
        public int symbolId;
        public GameObject modelPrefab;
        public MarkerLostPolicy lostPolicy = MarkerLostPolicy.KeepInPlace;
        [Min(0f)] public float smoothTime = 0.1f;
        [Min(0f)] public float hideDelay = 1f;
        [Tooltip("转动方块时模型绕竖直轴同步旋转（转盘式）")]
        public bool syncRotation = true;
    }

    [CreateAssetMenu(fileName = "MarkerContentLibrary", menuName = "TangibleTable/Marker Content Library")]
    public class MarkerContentLibrary : ScriptableObject
    {
        [SerializeField] private List<MarkerMapping> _mappings = new();

        public bool TryGetMapping(int symbolId, out MarkerMapping mapping)
        {
            foreach (var m in _mappings)
            {
                if (m.symbolId == symbolId)
                {
                    mapping = m;
                    return true;
                }
            }
            mapping = null;
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seen = new HashSet<int>();
            foreach (var m in _mappings)
            {
                if (!seen.Add(m.symbolId))
                    Debug.LogWarning($"[MarkerContentLibrary] 重复的 symbolId: {m.symbolId}", this);
                if (m.modelPrefab == null)
                    Debug.LogWarning($"[MarkerContentLibrary] symbolId {m.symbolId} 未指定 modelPrefab", this);
            }
        }
#endif
    }
}
