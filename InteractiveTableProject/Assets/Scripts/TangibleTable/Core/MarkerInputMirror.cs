using UnityEngine;

namespace TangibleTable.Core
{
    /// <summary>
    /// X 轴镜像开关：现场 reacTIVision.xml 配了 invert="x"（已部署的旧程序依赖它，不能删），
    /// 我们的程序在数据入口再镜像一次抵消。F2 切换（MirrorToggleHotkey），PlayerPrefs 记忆。
    /// </summary>
    public static class MarkerInputMirror
    {
        private const string PrefsKey = "TangibleTable.InvertX";

        public static bool InvertX { get; private set; } = PlayerPrefs.GetInt(PrefsKey, 0) == 1;

        public static void Toggle()
        {
            InvertX = !InvertX;
            PlayerPrefs.SetInt(PrefsKey, InvertX ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
