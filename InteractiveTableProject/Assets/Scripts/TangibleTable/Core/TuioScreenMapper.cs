using UnityEngine;

namespace TangibleTable.Core
{
    /// <summary>TUIO 归一化坐标(0~1, y 向下) 与屏幕像素坐标(y 向上) 的换算。</summary>
    public static class TuioScreenMapper
    {
        public static Vector2 ToScreen(Vector2 tuioPosition)
        {
            return new Vector2(tuioPosition.x * Screen.width, (1f - tuioPosition.y) * Screen.height);
        }
    }
}
