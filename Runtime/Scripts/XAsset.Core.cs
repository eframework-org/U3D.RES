// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using EFramework.Utility;

namespace EFramework.Asset
{
    /// <summary>
    /// XAsset.Core 实现了资源管理器的自动初始化，提供了系统事件管理、异步加载处理器等功能。
    /// </summary>
    /// <remarks>
    /// <code>
    /// 功能特性
    /// - 自动初始化：自动加载资源清单并全局管理资源
    /// - 系统事件：定义资源系统生命周期中的关键事件
    /// - 引用计数：通过引用计数机制管理资源生命周期
    /// 
    /// 使用手册
    /// 1. 运行流程
    ///    以下流程图展示了资源管理器的运行时逻辑，包括初始化、资源加载、引用计数管理、事件触发和资源卸载的主要流程：
    ///    
    ///    资源加载流程：资源加载开始 --> 资源加载请求(OnPreLoadAsset) --> 资源加载模式 -->
    ///                 Bundle模式/Resources模式 --> 引用计数开始/直接加载 --> 
    ///                 增加Bundle引用计数 --> 加载目标资源 --> 资源加载完成(OnPostLoadAsset)
    ///    
    ///    场景加载流程：场景加载开始 --> 场景加载请求(OnPreLoadScene) --> 场景加载模式 -->
    ///                 Bundle模式/Resources模式 --> 引用计数开始/直接加载 --> 
    ///                 增加Bundle引用计数 --> 加载目标场景 --> 场景加载完成(OnPostLoadScene)
    ///                 
    ///    引用计数流程：引用计数开始 --> 资源引用操作 --> 是否手动保持引用 -->
    ///                 调用Object.Obtain(是)/自动管理引用(否) --> 设置引用计数为负值/实例化时计数为1 -->
    ///                 增加Bundle引用计数
    ///    
    ///    资源释放操作：释放开始 --> 是否手动释放 --> 调用Object.Release(是)/实例销毁时自动释放(否) -->
    ///                 设置引用计数为0/递减引用计数 --> 减少Bundle引用计数 --> 是否引用计数为0 -->
    ///                 卸载资源包(是)/保持资源包加载(否) --> 卸载依赖资源包(OnPostUnloadBundle)
    ///    
    ///    自动卸载流程：自动卸载开始 --> 场景卸载请求 --> 判断场景类型(主场景 isSubScene == false) -->
    ///                 引用模式检查(OnPreUnloadAll) --> 清理未使用对象(启用)/卸载所有场景(禁用) -->
    ///                 资源实例清理 --> 卸载所有场景 --> 清空场景记录(OnPostUnloadAll)
    ///    
    ///    关键流程说明：
    ///    1. 系统初始化
    ///       - 在编辑器模式和运行时，通过 [InitializeOnLoadMethod] 和 [RuntimeInitializeOnLoadMethod] 自动初始化
    ///       - 加载资源清单文件，为后续资源加载做准备
    ///       - 注册场景加载和卸载的回调函数，为资源生命周期管理奠定基础
    ///    
    ///    2. 资源加载流程
    ///       - 资源加载开始时触发 OnPreLoadAsset 事件，允许外部逻辑介入
    ///       - 根据当前配置确定资源加载模式（Bundle 模式或 Resources 模式）
    ///       - Bundle 模式下，自动进入引用计数流程，递增依赖资源包引用计数
    ///       - Resources 模式下，直接从 Unity 资源系统加载目标资源
    ///       - 加载完成后触发 OnPostLoadAsset 事件，提供加载后处理机会
    ///    
    ///    3. 场景加载流程
    ///       - 场景加载开始时触发 OnPreLoadScene 事件，允许预处理逻辑
    ///       - 根据当前配置确定场景加载模式（Bundle 模式或 Resources 模式）
    ///       - Bundle 模式下，自动进入引用计数流程，递增场景依赖的资源包引用计数
    ///       - Resources 模式下，直接从 Unity 场景系统加载目标场景
    ///       - 场景加载完成后触发 OnPostLoadScene 事件，同时记录已加载场景信息
    ///    
    ///    4. 引用计数流程
    ///       - 支持两种引用管理方式：手动保持引用和自动管理引用
    ///       - 手动保持引用：通过 Object.Obtain 方法将引用计数设置为负值，表示资源被显式持有
    ///       - 自动管理引用：实例化预制体时自动将引用计数设为 1，实例销毁时自动递减
    ///       - 两种引用方式都会增加对应 Bundle 的引用计数，确保依赖的资源包保持加载状态
    ///       - 引用计数变为0时，自动卸载相关资源包并级联处理依赖关系
    ///    
    ///    5. 资源释放操作
    ///       - 资源释放支持手动释放和自动释放两种方式
    ///       - 手动释放：调用 Object.Release 方法显式将引用计数重置为0
    ///       - 自动释放：GameObject 销毁时自动递减引用计数
    ///       - 递减操作会同步减少相关 Bundle 的引用计数
    ///       - 当 Bundle 引用计数降为 0 时，触发资源包卸载并释放依赖资源
    ///       - 资源包卸载完成后触发 OnPostUnloadBundle 事件
    ///    
    ///    6. 自动卸载流程
    ///       - 当场景卸载时（特别是主场景卸载），触发自动卸载流程
    ///       - 通过 isSubScene 标志判断是主场景还是子场景，主场景卸载时执行全局清理
    ///       - 主场景卸载触发 OnPreUnloadAll 事件，通知清理即将开始
    ///       - 根据系统配置检查引用模式，决定是否清理未使用对象
    ///       - 引用模式启用时，执行 Object.Cleanup 处理所有加载对象的引用状态
    ///       - 处理完成后，卸载所有场景资源并清空场景记录
    ///       - 流程最后触发 OnPostUnloadAll 事件，表示清理完成
    /// 
    /// 2. 事件类型
    ///    - 功能说明：定义资源生命周期中的关键事件
    ///    - 事件列表：
    ///      - OnPreLoadAsset：资源加载前
    ///      - OnPostLoadAsset：资源加载后
    ///      - OnPreLoadScene：场景加载前
    ///      - OnPostLoadScene：场景加载后
    ///      - OnPreUnloadAll：卸载所有资源前
    ///      - OnPostUnloadAll：卸载所有资源后
    ///      - OnPostUnloadBundle：资源包卸载后
    ///    - 使用示例：
    ///      XAsset.Event.Reg(XAsset.EventType.OnPreLoadAsset, (asset) => Debug.Log("资源加载前：" + asset));
    /// </code>
    /// 更多信息请参考模块文档。
    /// </remarks>
    public partial class XAsset
    {
        /// <summary>
        /// Callback 是资源加载完成后的回调类型，用于通知调用者资源已准备就绪。
        /// </summary>
        /// <param name="asset">加载完成的 Unity 资源对象</param>
        public delegate void Callback(UnityEngine.Object asset);

        /// <summary>
        /// Handler 是资源加载处理器，负责跟踪和管理异步资源加载的过程。
        /// 提供加载进度监控、事件通知等功能，支持场景和资源包的异步加载操作。
        /// </summary>
        public class Handler : IEnumerator
        {
            /// <summary>
            /// doneCount 表示当前已完成加载的资源数量。
            /// </summary>
            internal int doneCount;

            /// <summary>
            /// totalCount 表示需要加载的资源总数。
            /// </summary>
            internal int totalCount;

            /// <summary>
            /// Progress 获取当前加载进度（0-1之间的浮点数）。
            /// 对于单个资源，直接返回其加载进度；
            /// 对于多个资源，返回总体完成的百分比。
            /// </summary>
            public float Progress
            {
                get
                {
                    if (totalCount == 0 || totalCount == 1)
                    {
                        if (Request != null) return Request.progress;
                        else return 0f;
                    }
                    else
                    {
                        if (Request != null)
                        {
                            if (!Request.isDone) return (doneCount + Request.progress) / totalCount;
                            else return 1f;
                        }
                        else return doneCount / (float)totalCount;
                    }
                }
            }

            /// <summary>
            /// OnPreload 是资源开始加载前触发的事件，可用于执行预加载准备工作。
            /// </summary>
            public event Action OnPreload;

            /// <summary>
            /// OnPostload 是资源加载完成后触发的事件，可用于执行加载后的初始化操作。
            /// </summary>
            public event Action OnPostload;

            /// <summary>
            /// Request 是 Unity 的异步操作对象。
            /// 可能是 ResourceRequest 或 AssetBundleRequest，用于跟踪具体的加载进度。
            /// </summary>
            public AsyncOperation Request;

            /// <summary>
            /// Asset 获取加载完成的资源对象。
            /// 根据Operation类型的不同，可能返回 Resources 加载的资源或 AssetBundle 中的资源。
            /// </summary>
            public UnityEngine.Object Asset
            {
                get
                {
                    if (Request != null)
                    {
                        if (Request is ResourceRequest) return (Request as ResourceRequest).asset;
                        else if (Request is AssetBundleRequest) return (Request as AssetBundleRequest).asset;
                    }
                    return null;
                }
            }

            public object Current => null;

            /// <summary>
            /// Error 表示是否发生错误中断。
            /// </summary>
            public bool Error { get; internal set; }

            /// <summary>
            /// IsDone 检查资源是否已完成加载。
            /// </summary>
            public bool IsDone { get => Error || (Request != null && Request.isDone); }

            /// <summary>
            /// MoveNext 推进加载进程。
            /// 作为协程迭代器，在加载未完成时返回true，允许 Unity 继续执行异步加载。
            /// </summary>
            public bool MoveNext() { return !IsDone; }

            /// <summary>
            /// Reset 重置加载器状态，清空所有计数器和事件监听，使其可以重新用于新的加载任务。
            /// </summary>
            public void Reset() { doneCount = 0; totalCount = 0; OnPreload = null; OnPostload = null; Request = null; Error = false; }

            /// <summary>
            /// InvokePreload 触发预加载事件，并安全处理可能的异常。
            /// </summary>
            internal void InvokePreload()
            {
                try { OnPreload?.Invoke(); }
                catch (Exception e) { XLog.Panic(e); }
            }

            /// <summary>
            /// InvokePostload 触发加载完成事件，并安全处理可能的异常。
            /// </summary>
            internal void InvokePostload()
            {
                try { OnPostload?.Invoke(); }
                catch (Exception e) { XLog.Panic(e); }
            }
        }

        /// <summary>
        /// EventType 是资源系统的内置事件类型，定义了资源生命周期中的重要节点。
        /// 这些事件可用于在特定时机执行自定义逻辑。
        /// </summary>
        public enum EventType
        {
            /// <summary>
            /// OnPreLoadAsset 是资源加载前的事件。
            /// </summary>
            OnPreLoadAsset,

            /// <summary>
            /// OnPostLoadAsset 是资源加载完成后的事件。
            /// </summary>
            OnPostLoadAsset,

            /// <summary>
            /// OnPreLoadScene 是场景加载前的事件。
            /// </summary>
            OnPreLoadScene,

            /// <summary>
            /// OnPostLoadScene 是场景加载完成后的事件。
            /// </summary>
            OnPostLoadScene,

            /// <summary>
            /// OnPreUnloadAll 是开始卸载所有资源前的事件。
            /// </summary>
            OnPreUnloadAll,

            /// <summary>
            /// OnPostUnloadAll 是完成卸载所有资源后的事件。
            /// </summary>
            OnPostUnloadAll,

            /// <summary>
            /// OnPostUnloadBundle 是资源包卸载完成后的事件。
            /// </summary>
            OnPostUnloadBundle,
        }

        /// <summary>
        /// Event 是资源系统的内置事件管理器实例，用于处理资源生命周期相关的事件。
        /// </summary>
        public static readonly XEvent.Manager Event = new();

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        /// <summary>
        /// OnInit 是资源系统的初始化方法，在编辑器或运行时自动调用。
        /// 负责设置场景加载和卸载的回调，处理资源清理和依赖关系。
        /// </summary>
        internal static void OnInit()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
#endif

            Bundle.Initialize();

            SceneManager.sceneLoaded += (scene, mode) =>
            {
                scene.isSubScene = mode == LoadSceneMode.Additive;
                if (Const.BundleMode)
                {
                    if (!Scene.Loaded.ContainsKey(scene.name)) Scene.Loaded.Add(scene.name, scene.path);
                }
            };

            SceneManager.sceneUnloaded += scene =>
            {
                if (!scene.isSubScene)
                {
                    try { Event.Notify(EventType.OnPreUnloadAll, scene); }
                    catch (Exception e) { XLog.Panic(e); }

                    if (Const.BundleMode)
                    {
                        XLog.Info("XAsset.OnInit: start to cleanup by scene: {0} unloaded, cached bundle count: {1}.", scene.name, Bundle.Loaded.Count);
                        if (Const.ReferMode) Object.Cleanup();
                        XLog.Info("XAsset.OnInit: finish to cleanup by scene: {0} unloaded, cached bundle count: {1}.", scene.name, Bundle.Loaded.Count);

                        foreach (var kvp in Scene.Loaded) Scene.Unload(kvp.Value);
                        Scene.Loaded.Clear();
                    }
                    else Scene.Loaded.Clear();

                    try { Event.Notify(EventType.OnPostUnloadAll, scene); }
                    catch (Exception e) { XLog.Panic(e); }
                }
            };
        }
    }
}
