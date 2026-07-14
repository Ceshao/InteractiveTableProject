using System;
using System.Collections.Generic;
using UnityEngine;

namespace TangibleTable.Comm
{
    /// <summary>
    /// TableUI → Projection 的 UDP JSON 消息定义。
    /// state：每帧全量快照，丢一帧由下一帧自动纠正；
    /// event：zone_enter / zone_exit 离散事件。
    /// </summary>
    [Serializable]
    public class MarkerWire
    {
        public int id;
        public float x;      // TUIO 归一化坐标（0~1），y 向下
        public float y;
        public float angle;  // 弧度
        public string zone;  // 当前所在按钮区 ID，空串表示不在任何区
    }

    [Serializable]
    public class StateMessage
    {
        public string type = "state";
        public List<MarkerWire> markers = new();
    }

    [Serializable]
    public class EventMessage
    {
        public string type = "event";
        public string name;      // zone_enter / zone_exit
        public int markerId;
        public string zoneId;
    }

    [Serializable]
    internal class TypeProbe
    {
        public string type;
    }

    public static class MarkerWireProtocol
    {
        public const int DefaultPort = 3334;
        public const string ZoneEnter = "zone_enter";
        public const string ZoneExit = "zone_exit";

        public static string PeekType(string json)
        {
            try { return JsonUtility.FromJson<TypeProbe>(json)?.type; }
            catch { return null; }
        }

        /// <summary>
        /// 命令行 -port N 覆盖端口（现场 3334 被占用时不用重新打包）。
        /// 无参数或解析失败时返回 fallback（Inspector 序列化值）。
        /// </summary>
        public static int ResolvePort(int fallback)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (!args[i].Equals("-port", StringComparison.OrdinalIgnoreCase)) continue;
                if (int.TryParse(args[i + 1], out var port) && port >= 1 && port <= 65535)
                    return port;
                Debug.LogWarning($"[MarkerWireProtocol] -port 参数无效：\"{args[i + 1]}\"，改用默认端口 {fallback}。");
                return fallback;
            }
            return fallback;
        }
    }
}
