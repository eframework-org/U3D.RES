// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using UnityEngine;
using EFramework.Utility;
using System.Collections.Generic;

namespace EFramework.Asset
{
    public partial class XAsset : MonoBehaviour
    {
        /// <summary>
        /// XAsset.Object 用于跟踪资源（Prefab）实例（GameObject）的使用情况，确保资源依赖包被正确释放。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 引用计数管理：跟踪游戏对象的生命周期
        /// - 全局实例追踪：管理全局资源的引用释放
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
            /// originalObjects 是用于跟踪原始对象的列表，确保对象可以正确地自动释放其资源包引用。
            /// </summary>
            internal static readonly List<Object> originalObjects = new();

            /// <summary>
            /// obtainedObjects 是用于跟踪保持对象的列表，确保对象可以正确地进行资源包引用与释放。
            /// </summary>
            internal static readonly List<Object> obtainedObjects = new();

            /// <summary>
            /// Watch 监听指定游戏对象的生命周期，确保对象在被销毁前进行资源包释放。
            /// </summary>
            /// <param name="originalObject">要监听的游戏对象。</param>
            /// <param name="bundleName">资源包名称，用于定位资源包实例。</param>
            internal static Object Watch(GameObject originalObject, string bundleName)
            {
                if (originalObject && Const.ReferMode)
                {
                    if (!originalObject.TryGetComponent<Object>(out var refer)) refer = originalObject.AddComponent<Object>();
                    refer.Source = bundleName;
                    originalObjects.Add(refer);
                    return refer;
                }
                return null;
            }

            /// <summary>
            /// Defer 处理原始游戏对象的资源包引用的释放。
            /// </summary>
            internal static void Defer()
            {
                if (Const.ReferMode)
                {
                    foreach (var refer in originalObjects)
                    {
                        var bundle = Bundle.Find(refer.Source);
                        bundle?.Release(Const.DebugMode ? $"[XAsset.Object.Defer: {refer.Label}]" : "");
                        XLog.Info("XAsset.Object.Defer: release {0} by {1}.", refer.Source, refer.Label);
                    }
                    originalObjects.Clear();
                }
            }

            /// <summary>
            /// Obtain 引用指定游戏对象的资源包。
            /// </summary>
            /// <param name="originalObject">要引用资源包的游戏对象，建议使用未实例化的源对象，避免过度引用导致资源包计数异常。</param>
            public static void Obtain(GameObject originalObject)
            {
                if (originalObject && Const.ReferMode)
                {
                    var refer = originalObject.GetComponent<Object>();
                    if (refer)
                    {
                        if (!originalObjects.Contains(refer))
                        {
                            XLog.Error("XAsset.Object.Obtain: {0} is not tracked in the original object list.", refer.Label);
                            return;
                        }

                        originalObjects.Remove(refer);
                        obtainedObjects.Add(refer);

                        var bundle = Bundle.Find(refer.Source);
                        bundle?.Obtain(Const.DebugMode ? $"[XAsset.Object.Obtain: {refer.Label}]" : "");

                        XLog.Info("XAsset.Object.Obtain: obtain {0} by {1}.", refer.Source, refer.Label);
                    }
                }
            }

            /// <summary>
            /// Release 释放指定游戏对象的资源包。
            /// </summary>
            /// <param name="originalObject">要释放资源包的游戏对象，建议使用未实例化的源对象，避免过度释放导致资源包提早卸载。</param>
            public static void Release(GameObject originalObject)
            {
                if (originalObject && Const.ReferMode)
                {
                    var refer = originalObject.GetComponent<Object>();
                    if (refer)
                    {
                        if (!obtainedObjects.Contains(refer))
                        {
                            XLog.Error("XAsset.Object.Release: {0} is not tracked in the obtained object list.", refer.Label);
                            return;
                        }

                        obtainedObjects.Remove(refer);

                        var bundle = Bundle.Find(refer.Source);
                        bundle?.Release(Const.DebugMode ? $"[XAsset.Object.Release: {refer.Label}]" : "");
                        XLog.Info("XAsset.Object.Release: release {0} by {1}.", refer.Source, refer.Label);
                    }
                }
            }
        }
    }
}
