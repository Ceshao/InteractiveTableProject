using UnityEngine;

namespace TangibleTable.Core
{
    /// <summary>
    /// TUIO 归一化坐标(0~1, y 向下) 与 Unity 世界坐标之间的换算。
    /// 桌面边缘对应相机视口边缘；depth 为沿相机朝向的距离，用于保持模型原有深度。
    /// </summary>
    public static class TuioWorldMapper
    {
        public static Vector3 ToWorld(Camera camera, Vector2 tuioPosition, float depth)
        {
            var viewport = new Vector3(tuioPosition.x, 1f - tuioPosition.y, depth);
            return camera.ViewportToWorldPoint(viewport);
        }

        public static float DepthOf(Camera camera, Vector3 worldPosition)
        {
            return Vector3.Dot(worldPosition - camera.transform.position, camera.transform.forward);
        }
    }
}
