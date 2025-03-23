// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System.Collections.Generic;
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
        /// - 引用计数管理：通过引用计数跟踪资源使用情况
        /// - 全局实例追踪：统一管理和清理全局资源实例列表
        ///
        /// 使用手册
        /// 1. 释放引用：释放指定游戏对象持有的资源
        ///      使用示例：
        ///      <code>
        ///      XAsset.Object.Release(testGameObject);
        ///      </code>
        ///
        /// 2. 保持引用：手动引用指定游戏对象的资源，防止资源被卸载
        ///      使用示例：
        ///      <code>
        ///      XAsset.Object.Obtain(testGameObject);
        ///      </code>
        ///
        /// 3. 清理实例：清理所有已加载的资源对象，释放未被引用的资源
        ///      使用示例：
        ///      <code>
        ///      XAsset.Object.Cleanup();
        ///      </code>
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public partial class Object : MonoBehaviour
        {
            /// <summary>
            /// 资源在 Bundle 中的原始路径，用于定位和加载资源包。
            /// </summary>
            [SerializeField]
            internal string Source;

            /// <summary>
            /// 资源的引用计数。
            /// -1：通过 Obtain 方法手动引用
            /// 0：未被引用
            /// >0：被场景或预制体实例引用的次数
            /// </summary>
            internal int Count;

            /// <summary>
            /// 用于调试的对象标签，格式为"对象名@哈希码"。
            /// 即使对象被销毁也能保持唯一标识。
            /// </summary>
            internal string label;
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
            /// 创建新的资源包装器，并将其加入到全局跟踪列表中。
            /// </summary>
            internal Object() { Loaded.Add(this); }

            /// <summary>
            /// Unity 对象唤醒时的初始化，设置初始引用计数并加载对应的资源包。
            /// 这通常发生在预制体实例化或场景加载时。
            /// </summary>
            internal void Awake()
            {
                Count = 1;
                var bundle = Bundle.Find(Source);
                bundle?.Obtain(Const.DebugMode ? $"[XAsset.Object.Awake: {Label}]" : "");
            }

            /// <summary>
            /// Unity 对象销毁时的清理，减少引用计数。
            /// 当引用计数降为0时，相关的资源包可能会被卸载。
            /// </summary>
            internal void OnDestroy() { Count--; }

            /// <summary>
            /// 手动增加对资源实例的引用。这会将引用计数设为-1，表示实例被手动持有。
            /// 通常用于需要长期持有实例的场景，如 UI 界面或常驻对象。
            /// </summary>
            internal void Obtain()
            {
                var bundle = Bundle.Find(Source);
                if (bundle != null)
                {
                    Count = -1;
                    bundle.Obtain(Const.DebugMode ? $"[XAsset.Object.Obtain: {Label}]" : "");
                }
            }

            /// <summary>
            /// 手动释放对资源实例的引用。这会清除手动引用状态，允许实例在不再需要时被卸载。
            /// 应当在确保不再需要实例时调用此方法。
            /// </summary>
            internal void Release()
            {
                var bundle = Bundle.Find(Source);
                if (bundle != null)
                {
                    Count = 0;
                    bundle.Release(Const.DebugMode ? $"[XAsset.Object.Release: {Label}]" : "");
                }
            }
        }

        public partial class Object : MonoBehaviour
        {
            /// <summary>
            /// 当前已加载的所有资源实例列表，用于全局实例追踪和管理。
            /// </summary>
            internal static readonly List<Object> Loaded = new();

            /// <summary>
            /// 释放指定游戏对象持有的资源。只能用于未实例化的资源，
            /// 比如从 Resources 或 AssetBundle 直接加载的对象。
            /// </summary>
            /// <param name="go">要释放资源的游戏对象</param>
            public static void Release(GameObject go)
            {
                if (go && Const.ReferMode)
                {
                    var obj = go.GetComponent<Object>();
                    if (obj)
                    {
                        if (obj.Count > 0)
                        {
                            XLog.Error("XAsset.Object.Release: can not unload object on an instantiated asset: {0}", obj.Source);
                        }
                        else
                        {
                            obj.Release();
                            XLog.Info("XAsset.Object.Release: {0}", obj.Source);
                        }
                    }
                }
            }

            /// <summary>
            /// 手动引用指定游戏对象的资源。只能用于未实例化的资源，
            /// 通常用于需要确保资源不被卸载的场景。
            /// </summary>
            /// <param name="go">要引用资源的游戏对象</param>
            public static void Obtain(GameObject go)
            {
                if (go && Const.ReferMode)
                {
                    var obj = go.GetComponent<Object>();
                    if (obj)
                    {
                        if (obj.Count > 0)
                        {
                            XLog.Error("XAsset.Object.Obtain: can not obtain object on an instantiated asset: {0}", obj.Source);
                        }
                        else
                        {
                            obj.Obtain();
                            XLog.Info("XAsset.Object.Obtain: {0}", obj.Source);
                        }
                    }
                }
            }

            /// <summary>
            /// 清理所有已加载的资源实例。这个过程会分两个阶段进行：
            /// 1. 处理未被引用的资源（Count=0）
            /// 2. 处理所有被引用的资源（Count>0）
            /// 
            /// 在调试模式下，会提供详细的资源清理日志，帮助跟踪资源的释放过程。
            /// 如果发现资源在被依赖包卸载后仍然存活，会给出警告信息。
            /// </summary>
            public static void Cleanup()
            {
                foreach (var obj in Loaded)
                {
                    var bundle = Bundle.Find(obj.Source);
                    if (bundle != null)
                    {
                        if (Const.DebugMode)
                        {
                            if (obj.Count <= -1) { }  // Obtained
                            else if (obj.Count == 0) bundle.Release($"[Refer.GC.Phase1: {obj.Label}]");
                            else
                            {
                                for (var i = 0; i < obj.Count; i++)
                                {
                                    if (bundle.Release($"[Refer.GC.Phase2: {obj.Label}]") <= 0)
                                    {
                                        if (obj != null)
                                        {
                                            XLog.Warn("XAsset.Object.Cleanup: object of {0} is still alive, but dep-ab: {1} was unloaded, use XAsset.Object.Obtain to keep reference.", obj.Label, obj.Source);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (obj.Count <= -1) { }  // Obtained
                            else if (obj.Count == 0) bundle.Release();
                            else for (var i = 0; i < obj.Count; i++) bundle.Release();
                        }
                    }
                }
                Loaded.Clear();
            }
        }
    }
}
