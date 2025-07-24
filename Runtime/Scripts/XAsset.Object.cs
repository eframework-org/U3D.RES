// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using UnityEngine;
using EFramework.Utility;

namespace EFramework.Asset
{
    public partial class XAsset : MonoBehaviour
    {
        /// <summary>
        /// XAsset.Object 通过引用计数机制跟踪资源（Prefab）实例（GameObject）的使用情况，确保实例能够被正确释放。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 保持引用：引用指定游戏对象的资源包
        /// - 释放引用：释放指定游戏对象的资源包
        ///
        /// 使用手册
        /// 1. 保持引用
        /// - 功能说明：引用指定游戏对象的资源包
        /// - 注意事项：建议使用未实例化的源对象，避免过度引用导致资源包计数异常
        /// - 方法签名：
        ///   public static void Release(GameObject gameObject)
        ///
        /// 2. 释放引用
        /// - 功能说明：释放指定游戏对象的资源包
        /// - 注意事项：建议使用未实例化的源对象，避免过度释放导致资源包提早卸载
        /// - 方法签名：
        ///   public static void Obtain(GameObject gameObject)
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public partial class Object : MonoBehaviour
        {
            /// <summary>
            /// Source 是资源在 Bundle 中的原始路径，用于定位资源包实例。
            /// </summary>
            [SerializeField]
            internal string Source;

            internal string label;
            /// <summary>
            /// Label 是用于调试的对象标签，格式为"对象名@哈希码"。
            /// 即使对象被销毁也能保持唯一标识。
            /// </summary>
            internal string Label
            {
                get
                {
                    if (string.IsNullOrEmpty(label))
                    {
                        if (this) label = $"{name}@{GetHashCode()}";
                        else label = $"null@{GetHashCode()}"; // 处理被销毁的对象
                    }
                    return label;
                }
            }

            /// <summary>
            /// Awake 在 Unity 对象初始化时调用。
            /// </summary>
            internal void Awake()
            {
                if (Const.ReferMode)
                {
                    var bundle = Bundle.Find(Source);
                    bundle?.Obtain(Const.DebugMode ? $"[XAsset.Object.Awake: {Label}]" : "");
                }
            }

            /// <summary>
            /// OnDestroy 在 Unity 对象销毁时的调用。
            /// </summary>
            internal void OnDestroy()
            {
                if (Const.ReferMode)
                {
                    var bundle = Bundle.Find(Source);
                    bundle?.Release(Const.DebugMode ? $"[XAsset.Object.OnDestroy: {Label}]" : "");
                }
            }

            /// <summary>
            /// Obtain 引用指定游戏对象的资源包。
            /// </summary>
            /// <param name="gameObject">要引用资源包的游戏对象，建议使用未实例化的源对象，避免过度引用导致资源包计数异常。</param>
            public static void Obtain(GameObject gameObject)
            {
                if (gameObject && Const.ReferMode)
                {
                    var refer = gameObject.GetComponent<Object>();
                    if (refer)
                    {
                        var bundle = Bundle.Find(refer.Source);
                        bundle?.Obtain(Const.DebugMode ? $"[XAsset.Object.Obtain: {refer.Label}]" : "");
                        XLog.Info("XAsset.Object.Obtain: {0}", refer.Source);
                    }
                }
            }

            /// <summary>
            /// Release 释放指定游戏对象的资源包。
            /// </summary>
            /// <param name="gameObject">要释放资源包的游戏对象，建议使用未实例化的源对象，避免过度释放导致资源包提早卸载。</param>
            public static void Release(GameObject gameObject)
            {
                if (gameObject && Const.ReferMode)
                {
                    var refer = gameObject.GetComponent<Object>();
                    if (refer)
                    {
                        var bundle = Bundle.Find(refer.Source);
                        bundle?.Release(Const.DebugMode ? $"[XAsset.Object.Release: {refer.Label}]" : "");
                        XLog.Info("XAsset.Object.Release: {0}", refer.Source);
                    }
                }
            }
        }
    }
}
