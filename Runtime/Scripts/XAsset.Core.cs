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
    ///    系统初始化 --> 加载资源清单 --> 注册场景事件
    ///    
    ///    资源加载流程：触发OnPreLoadAsset --> 选择加载模式(Bundle/Resources) --> 
    ///                 Bundle模式递增引用计数或Resources直接加载 --> 加载目标资源 --> 触发OnPostLoadAsset
    ///    
    ///    场景加载流程：触发OnPreLoadScene --> 选择加载模式(Bundle/Resources) --> 
    ///                 Bundle模式递增引用计数或Resources直接加载 --> 加载目标场景 --> 触发OnPostLoadScene
    ///                 
    ///    引用计数流程：手动保持引用(Object.Obtain,计数为负值)或自动管理引用(实例化,计数为1) --> 
    ///                 增加Bundle引用计数 --> 手动释放(Object.Release)或实例销毁 --> 
    ///                 递减引用计数 --> 计数为0时卸载资源包及依赖
    ///    
    ///    自动卸载流程：场景卸载 --> 判断场景类型 --> 触发OnPreUnloadAll --> 
    ///                 引用模式检查 --> 清理未使用对象 --> 卸载场景资源 --> 触发OnPostUnloadAll
    /// 
    /// 2. 事件类型
    ///    - OnPreLoadAsset：资源加载前
    ///    - OnPostLoadAsset：资源加载后
    ///    - OnPreLoadScene：场景加载前
    ///    - OnPostLoadScene：场景加载后
    ///    - OnPreUnloadAll：卸载所有资源前
    ///    - OnPostUnloadAll：卸载所有资源后
    ///    - OnPostUnloadBundle：资源包卸载后
    ///    
    ///    使用示例：
    ///    XAsset.Event.Reg(XAsset.EventType.OnPreLoadAsset, (asset) => Debug.Log("资源加载前：" + asset));
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
