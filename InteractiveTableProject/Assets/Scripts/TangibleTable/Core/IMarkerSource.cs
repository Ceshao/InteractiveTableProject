using System;
using UnityEngine;

namespace TangibleTable.Core
{
    /// <summary>一个正在被追踪的识别图的活句柄，属性随帧更新，消费方在 Update 里轮询即可。</summary>
    public interface ITrackedMarker
    {
        int SymbolId { get; }
        /// <summary>TUIO 归一化坐标（0~1，y 向下）。</summary>
        Vector2 Position { get; }
        /// <summary>旋转角，弧度。</summary>
        float Angle { get; }
    }

    /// <summary>识别图数据源：TUIO 直连（TableUI 端）或远程转发（Projection 端），消费方无需关心来源。</summary>
    public interface IMarkerSource
    {
        event Action<ITrackedMarker> MarkerAdded;
        event Action<ITrackedMarker> MarkerRemoved;
    }
}
