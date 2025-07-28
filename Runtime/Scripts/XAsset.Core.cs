// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.Collections;
using UnityEngine;
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
    /// 
    /// 以下流程图展示了资源管理器的运行时逻辑，包括资源/场景加载/卸载、引用计数管理、内置事件机制的主要流程：
    /// 
    /// state 资源加载流程 {
    ///     资源加载开始 --> 资源加载请求 : OnPreLoadAsset
    ///     资源加载请求 --> 资源加载模式
    ///  
    ///     资源加载模式 --> Bundle资源加载 : Bundle模式
    ///     资源加载模式 --> Resources资源加载 : Resources模式
    ///  
    ///     Bundle资源加载 --> 资源引用操作
    ///     Resources资源加载 --> 加载目标资源
    ///  
    ///     资源引用操作 --> 加载目标资源
    ///     加载目标资源 --> 资源加载完成 : OnPostLoadAsset
    /// }
    ///
    /// state 场景加载流程 {
    ///     场景加载开始 --> 场景加载请求 : OnPreLoadScene
    ///     场景加载请求 --> 场景加载模式
    ///  
    ///     场景加载模式 --> Bundle场景加载 : Bundle模式
    ///     场景加载模式 --> Resources场景加载 : Resources模式
    ///
    ///     Bundle场景加载 --> 资源引用操作
    ///     Resources场景加载 --> 加载目标场景
    ///  
    ///     资源引用操作 --> 加载目标场景
    ///     加载目标场景 --> 场景加载完成 : OnPostLoadScene
    /// }
    ///
    /// state 资源管理流程 {
    ///     state 引用计数流程 {
    ///         资源引用操作 --> 手动管理引用 : Object.Obtain
    ///         资源引用操作 --> 自动管理引用 : Object.Watch/Awake
    ///
    ///         手动管理引用 --> 资源释放操作
    ///         自动管理引用 --> 资源释放操作
    ///     }
    ///
    ///     state 资源释放流程 {
    ///         资源释放操作 --> 手动管理释放 : Object.Release
    ///         资源释放操作 --> 自动管理释放 : Object.Defer/OnDestroy
    ///      
    ///         手动管理释放 --> 减少引用计数
    ///         自动管理释放 --> 减少引用计数
    ///      
    ///         减少引用计数 --> 引用计数为零
    ///         引用计数为零 --> 卸载资源包 : OnPostUnloadBundle
    ///     }
    ///
    ///     state 自动卸载流程 {
    ///         监听场景卸载 --> 场景卸载请求 : SceneManager.sceneUnloaded
    ///         场景卸载请求 --> 判断场景类型 : 主场景 isSubScene == false
    ///         判断场景类型 --> 引用模式检查 : OnPreUnloadAll
    ///      
    ///         引用模式检查 --> 资源释放操作 : 启用
    ///         引用模式检查 --> 卸载所有场景 : 禁用
    ///
    ///         卸载所有场景 --> 清空场景记录 : OnPostUnloadAll
    ///     }
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
            OnPreLoadResource,

            /// <summary>
            /// OnPostLoadAsset 是资源加载完成后的事件。
            /// </summary>
            OnPostLoadResource,

            /// <summary>
            /// OnPreLoadScene 是场景加载前的事件。
            /// </summary>
            OnPreLoadScene,

            /// <summary>
            /// OnPostLoadScene 是场景加载完成后的事件。
            /// </summary>
            OnPostLoadScene,

            /// <summary>
            /// OnPostUnloadBundle 是资源包卸载完成后的事件。
            /// </summary>
            OnPostUnloadBundle,
        }

        /// <summary>
        /// Event 是资源系统的内置事件管理器实例，用于处理资源生命周期相关的事件。
        /// </summary>
        public static readonly XEvent.Manager Event = new();
    }
}
