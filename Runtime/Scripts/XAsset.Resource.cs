// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.Collections;
using System.Collections.Generic;
using EFramework.Utility;
using UnityEngine;

namespace EFramework.Asset
{
    public partial class XAsset
    {
        /// <summary>
        /// XAsset.Resource 提供了 Unity 资源的加载与卸载，支持自动处理依赖资源的生命周期。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 支持资源的加载与卸载
        /// - 自动处理资源依赖关系
        ///
        /// 使用手册
        /// 1. 同步加载
        ///    - 加载资源：根据当前模式从 `Resources` 或 `AssetBundle` 中加载资源
        ///      使用示例：
        ///      <code>
        ///      var asset = XAsset.Resource.Load("Resources/Example/Test.prefab", typeof(GameObject));
        ///      </code>
        ///
        ///    - 泛型加载：提供类型安全的资源加载方式
        ///      使用示例：
        ///      <code>
        ///      var asset = XAsset.Resource.Load&lt;GameObject&gt;("Resources/Example/Test.prefab");
        ///      </code>
        ///
        /// 2. 异步加载
        ///    - 加载资源：异步加载资源，适合加载大型资源
        ///      使用示例：
        ///      <code>
        ///      XAsset.Resource.LoadAsync("Resources/Example/Test.prefab", typeof(GameObject), (asset) =&gt;
        ///      {
        ///          Debug.Log("加载完成：" + asset.name);
        ///      });
        ///      </code>
        ///
        ///    - 泛型加载：提供类型安全的异步加载方式
        ///      使用示例：
        ///      <code>
        ///      XAsset.Resource.LoadAsync&lt;GameObject&gt;("Resources/Example/Test.prefab", (asset) =&gt;
        ///      {
        ///          Debug.Log("加载完成：" + asset.name);
        ///      });
        ///      </code>
        ///
        /// 3. 卸载资源：卸载指定路径的资源
        ///      使用示例：
        ///      <code>
        ///      XAsset.Resource.Unload("Resources/Example/Test.prefab");
        ///      </code>
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public partial class Resource
        {
            /// <summary>
            /// 异步加载任务的封装，用于跟踪单个资源的加载状态。
            /// 可以处理来自 Resources 和 AssetBundle 的异步加载请求。
            /// </summary>
            internal class Task
            {
                /// <summary>
                /// 正在加载的资源路径
                /// </summary>
                internal string Name;

                /// <summary>
                /// Unity的异步加载操作对象，可能是 ResourceRequest 或 AssetBundleRequest。
                /// </summary>
                internal AsyncOperation Operation;
            }

            /// <summary>
            /// Loading 是当前正在进行的资源加载任务列表，用于处理并发加载请求，
            /// 避免同一资源被重复加载。
            /// </summary>
            internal static readonly Dictionary<string, Task> Loading = new();

            /// <summary>
            /// Load 同步加载指定类型的资源。
            /// 根据当前模式和参数，从 Resources 目录或 AssetBundle 文件中加载资源。
            /// 在 Bundle 模式下会自动处理资源包的加载和依赖关系。
            /// </summary>
            /// <param name="path">资源在项目中的相对路径</param>
            /// <param name="type">要加载的资源类型</param>
            /// <param name="resource">是否强制从 Resources 加载，忽略当前模式设置</param>
            /// <returns>加载的资源对象，加载失败时返回null</returns>
            public static UnityEngine.Object Load(string path, Type type, bool resource = false)
            {
                try { Event.Notify(EventType.OnPreLoadAsset, path); }
                catch (Exception e) { XLog.Panic(e); }
                UnityEngine.Object asset = null;
                try
                {
                    var resIdx = path.IndexOf("Resources/");
                    if (!Const.BundleMode || resource || !Manifest.Main)
                    {
                        if (resIdx >= 0) path = path[(resIdx + 10)..];
                        asset = Resources.Load(path, type);
                    }
                    else
                    {
                        if (resIdx < 0) path = "Resources/" + path;
                        var index = path.LastIndexOf("/");
                        var assetName = path[(index + 1)..];
                        var bundleName = Const.GenTag(path);
                        var bundle = Bundle.Load(bundleName);
                        if (bundle != null) asset = bundle.Source.LoadAsset(assetName, type);
                        if (Const.ReferMode && asset is GameObject go)
                        {
                            go.AddComponent<Object>().Source = bundleName;
                        }
                    }
                }
                catch (Exception e) { throw e; }
                finally
                {
                    try { Event.Notify(EventType.OnPostLoadAsset, path); }
                    catch (Exception e) { XLog.Panic(e); }
                }
                return asset;
            }

            /// <summary>
            /// Load 同步加载指定类型的资源。
            /// 泛型版本的加载方法，提供更方便的类型安全的资源加载方式。
            /// </summary>
            /// <typeparam name="T">要加载的资源类型</typeparam>
            /// <param name="path">资源在项目中的相对路径</param>
            /// <param name="resource">是否强制从 Resources 加载</param>
            /// <returns>加载的资源对象，加载失败时返回null</returns>
            public static T Load<T>(string path, bool resource = false) where T : UnityEngine.Object { return Load(path, typeof(T), resource) as T; }

            /// <summary>
            /// LoadAsync 异步加载资源。
            /// 提供非阻塞的资源加载方式，适合加载大型资源。
            /// 可以通过返回的Handler监控加载进度，并在加载完成时得到通知。
            /// </summary>
            /// <param name="path">资源在项目中的相对路径</param>
            /// <param name="type">要加载的资源类型</param>
            /// <param name="callback">资源加载完成时的回调函数</param>
            /// <param name="resource">是否强制从 Resources 加载</param>
            /// <returns>用于跟踪加载进度的Handler对象</returns>
            public static Handler LoadAsync(string path, Type type, Callback callback = null, bool resource = false)
            {
                var handler = new Handler();
                XLoom.StartCR(LoadAsync(path, type, callback, handler, resource));
                return handler;
            }

            /// <summary>
            /// LoadAsync 异步加载指定类型的资源。
            /// 泛型版本的异步加载方法，提供类型安全的回调方式。
            /// </summary>
            /// <typeparam name="T">要加载的资源类型</typeparam>
            /// <param name="path">资源在项目中的相对路径</param>
            /// <param name="callback">资源加载完成时的类型安全回调</param>
            /// <param name="resource">是否强制从 Resources 加载</param>
            /// <returns>用于跟踪加载进度的Handler对象</returns>
            public static Handler LoadAsync<T>(string path, Action<T> callback = null, bool resource = false) where T : UnityEngine.Object { return LoadAsync(path, typeof(T), (asset) => callback?.Invoke(asset as T), resource); }

            /// <summary>
            /// LoadAsync 异步加载资源的内部实现。
            /// 处理实际的资源加载流程，包括依赖资源的加载、加载状态的跟踪和回调的触发。
            /// 
            /// 加载流程：
            /// 1. 根据当前模式选择加载方式（Resources/AssetBundle）
            /// 2. 处理并发加载请求，避免重复加载
            /// 3. 在 Bundle 模式下处理依赖关系
            /// 4. 触发加载完成事件和回调
            /// </summary>
            internal static IEnumerator LoadAsync(string path, Type type, Callback callback, Handler handler, bool resource = false)
            {
                try { Event.Notify(EventType.OnPreLoadAsset, path); }
                catch (Exception e) { XLog.Panic(e); }

                UnityEngine.Object asset = null;
                var resIdx = path.IndexOf("Resources/");
                if (!Const.BundleMode || resource || !Manifest.Main)
                {
                    if (resIdx >= 0) path = path[(resIdx + 10)..];
                    if (!Loading.TryGetValue(path, out var task))
                    {
                        var req = Resources.LoadAsync(path, type);
                        task = new Task() { Name = path, Operation = req };
                        handler.Operation = req;
                        handler.InvokePreload();
                        Loading.Add(path, task);
                        yield return new WaitUntil(() => task.Operation.isDone);
                        Loading.Remove(path);
                    }
                    else
                    {
                        handler.Operation = task.Operation;
                        handler.InvokePreload();
                        yield return new WaitUntil(() => task.Operation.isDone);
                    }
                    asset = (task.Operation as ResourceRequest).asset;

                    // 加载错误时仍旧回调，业务层可根据 handler.Error 判断是否加载成功
                    handler.Error = asset == null;
                    handler.InvokePostload();
                }
                else
                {
                    handler.totalCount++; // Load任务
                    if (resIdx < 0) path = "Resources/" + path;
                    var index = path.LastIndexOf("/");
                    var assetName = path[(index + 1)..];
                    var bundleName = Const.GenTag(path);
                    yield return XLoom.StartCR(Bundle.LoadAsync(bundleName, handler));
                    var bundleInfo = Bundle.Find(bundleName);
                    if (bundleInfo != null)
                    {
                        if (!Loading.TryGetValue(path, out var task))
                        {
                            var req = bundleInfo.Source.LoadAssetAsync(assetName, type);
                            task = new Task() { Name = path, Operation = req };
                            handler.Operation = req;
                            handler.InvokePreload();
                            Loading.Add(path, task);
                            yield return req;
                            asset = req.asset;
                            Loading.Remove(path);
                            handler.doneCount++;
                            if (Const.ReferMode && asset is GameObject go)
                            {
                                go.AddComponent<Object>().Source = bundleName;
                            }
                            handler.InvokePostload();
                        }
                        else
                        {
                            handler.Operation = task.Operation;
                            handler.InvokePreload();
                            yield return new WaitUntil(() => task.Operation.isDone);
                            handler.doneCount++;
                            if (Const.ReferMode && asset is GameObject go)
                            {
                                go.AddComponent<Object>().Source = bundleName;
                            }
                            handler.InvokePostload();
                        }
                        asset = (task.Operation as AssetBundleRequest).asset;
                    }
                    else
                    {
                        XLog.Error("XAsset.Resource.LoadAsync: async load error: {0}", bundleName);

                        // 加载错误时仍旧回调，业务层可根据 handler.Error 判断是否加载成功
                        handler.Error = true;
                        handler.InvokePostload();
                    }
                }

                try { Event.Notify(EventType.OnPostLoadAsset, path); }
                catch (Exception e) { XLog.Panic(e); }

                try { callback?.Invoke(asset); }
                catch (Exception e) { XLog.Panic(e); }
            }

            /// <summary>
            /// Unload 卸载指定路径的资源。
            /// 在 Bundle 模式下，会卸载对应的资源包。
            /// 注意：这个操作可能会影响到共享同一资源包的其他资源。
            /// </summary>
            /// <param name="path">要卸载的资源路径</param>
            public static void Unload(string path)
            {
                if (Const.BundleMode) Bundle.Unload(Const.GenTag(path));
            }

            /// <summary>
            /// IsLoading 检查资源的加载状态。
            /// </summary>
            /// <param name="path">资源路径</param>
            /// <returns>是否正在加载</returns>
            public static bool IsLoading(string path)
            {
                if (string.IsNullOrEmpty(path)) return false;
                else return Loading.ContainsKey(path);
            }
        }
    }
}
