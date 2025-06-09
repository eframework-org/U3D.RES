// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using EFramework.Utility;

namespace EFramework.Asset
{
    public partial class XAsset
    {
        /// <summary>
        /// XAsset.Scene 提供了 Unity 场景的加载与卸载，支持自动处理依赖资源的生命周期。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 支持场景的加载与卸载
        /// - 自动处理资源依赖关系
        ///
        /// 使用手册
        /// 1. 同步加载：同步加载场景，在 `Bundle` 模式下会自动加载场景对应的资源包
        ///      使用示例：
        ///      <code>
        ///      XAsset.Scene.Load("Scenes/TestScene", LoadSceneMode.Single);
        ///      </code>
        ///
        /// 2. 异步加载：异步加载场景，适合加载大型场景，避免加载过程阻塞主线程
        ///      使用示例：
        ///      <code>
        ///      XAsset.Scene.LoadAsync("Scenes/TestScene", () =>
        ///      {
        ///          Debug.Log("场景加载完成");
        ///      });
        ///      </code>
        ///
        /// 3. 卸载场景：卸载指定场景，在 `Bundle` 模式下会同时卸载场景对应的资源包
        ///      使用示例：
        ///      <code>
        ///      XAsset.Scene.Unload("Scenes/TestScene");
        ///      </code>
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public partial class Scene
        {
            /// <summary>
            /// 场景加载任务，用于跟踪异步加载过程中的场景状态。
            /// </summary>
            internal class Task
            {
                internal string Name; // 场景名称，用于标识和查找场景。

                internal AsyncOperation Operation; // Unity 场景加载的异步操作对象。
            }

            /// <summary>
            /// 记录当前正在加载的场景，用于防止重复加载同一场景。
            /// </summary>
            internal static readonly Dictionary<string, Task> Loading = new();

            /// <summary>
            /// 已加载场景的记录表。
            /// </summary>
            internal static readonly Dictionary<string, string> Loaded = new();

            /// <summary>
            /// 同步加载场景。在 Bundle 模式下会自动加载场景对应的资源包。
            /// </summary>
            /// <param name="nameOrPath">场景名称或完整路径，支持从 Assets 目录下的相对路径加载</param>
            /// <param name="loadMode">场景加载模式：Single 会卸载当前场景，Additive 则保留当前场景</param>
            public static void Load(string nameOrPath, LoadSceneMode loadMode = LoadSceneMode.Single)
            {
                var sname = nameOrPath.Contains("/") ? Path.GetFileNameWithoutExtension(nameOrPath) : nameOrPath;
                try { Event.Notify(EventType.OnPreLoadScene, sname); }
                catch (Exception e) { XLog.Panic(e); }

                XLog.Info("XAsset.Scene.Load: start to load {0}, cached-ab: {1}", sname, Bundle.Loaded.Count);
                try
                {
                    if (Const.BundleMode && Manifest.Main)
                    {
                        string bname;
                        if (nameOrPath.Contains("/"))
                        {
                            if (nameOrPath.StartsWith("Assets/")) nameOrPath = nameOrPath["Assets/".Length..];
                            if (!nameOrPath.EndsWith(".unity")) nameOrPath += "_unity";
                            bname = Const.GenTag(nameOrPath);
                        }
                        else bname = $"scenes_{sname}_unity{Const.Extension}".ToLower();
                        if (Bundle.Load(bname) == null) XLog.Error("XAsset.Scene.Load: can not load scene caused by nil scene bundle file.");
                        else SceneManager.LoadScene(sname, loadMode);
                    }
                    else SceneManager.LoadScene(sname, loadMode);
                }
                catch (Exception e) { throw e; }
                finally
                {
                    XLog.Info("XAsset.Scene.Load: finish to load {0}, cached-ab: {1}", sname, Bundle.Loaded.Count);
                    try { Event.Notify(EventType.OnPostLoadScene, sname); }
                    catch (Exception e) { XLog.Panic(e); }
                }
            }

            /// <summary>
            /// 异步加载场景。适用于加载大型场景，避免加载过程阻塞主线程。
            /// </summary>
            /// <param name="nameOrPath">场景名称或完整路径</param>
            /// <param name="callback">场景加载完成后的回调函数</param>
            /// <param name="loadMode">场景加载模式：Single 或 Additive</param>
            /// <returns>用于跟踪加载进度的Handler对象</returns>
            public static Handler LoadAsync(string nameOrPath, Action callback = null, LoadSceneMode loadMode = LoadSceneMode.Single)
            {
                Handler handler = new Handler();
                if (callback != null) handler.OnPostload += callback;
                XLoom.StartCR(LoadAsync(nameOrPath, handler, loadMode));
                return handler;
            }

            /// <summary>
            /// 场景异步加载的内部实现。处理场景加载的具体流程：
            /// 1. 在Bundle模式下先加载场景资源包
            /// 2. 通过SceneManager加载场景
            /// 3. 处理加载回调和事件通知
            /// </summary>
            /// <param name="nameOrPath">场景名称或路径。</param>
            /// <param name="handler">场景加载处理程序。</param>
            /// <param name="loadMode">加载模式（单场景或多场景）。</param>
            /// <returns>协程。</returns>
            internal static IEnumerator LoadAsync(string nameOrPath, Handler handler, LoadSceneMode loadMode = LoadSceneMode.Single)
            {
                var sname = nameOrPath.Contains("/") ? Path.GetFileNameWithoutExtension(nameOrPath) : nameOrPath;
                try { Event.Notify(EventType.OnPreLoadScene, sname); }
                catch (Exception e) { XLog.Panic(e); }

                XLog.Info("XAsset.Scene.LoadAsync: start to load {0}, cached-ab: {1}", nameOrPath, Bundle.Loaded.Count);
                if (Const.BundleMode && Manifest.Main)
                {
                    handler.totalCount++; // Load任务
                    string bname;
                    if (nameOrPath.Contains("/"))
                    {
                        if (nameOrPath.StartsWith("Assets/")) nameOrPath = nameOrPath["Assets/".Length..];
                        if (!nameOrPath.EndsWith(".unity")) nameOrPath += "_unity";
                        bname = Const.GenTag(nameOrPath);
                    }
                    else bname = $"scenes_{sname}_unity{Const.Extension}".ToLower();
                    yield return XLoom.StartCR(Bundle.LoadAsync(bname, handler));
                    if (Bundle.Find(bname) != null)
                    {
                        if (!Loading.TryGetValue(sname, out var task))
                        {
                            var req = SceneManager.LoadSceneAsync(sname, loadMode);
                            task = new Task() { Name = sname, Operation = req };
                            handler.Operation = req;
                            handler.InvokePreload();
                            Loading.Add(sname, task);
                            yield return new WaitUntil(() => task.Operation.isDone);
                            Loading.Remove(sname);
                            handler.doneCount++;
                            XLog.Info("XAsset.Scene.LoadAsync: finish to load {0}, cached-ab: {1}", sname, Bundle.Loaded.Count);
                            handler.InvokePostload();
                        }
                        else
                        {
                            handler.Operation = task.Operation;
                            handler.InvokePreload();
                            yield return new WaitUntil(() => task.Operation.isDone);
                            handler.doneCount++;
                            XLog.Info("XAsset.Scene.LoadAsync: finish to load {0}, cached-ab: {1}", sname, Bundle.Loaded.Count);
                            handler.InvokePostload();
                        }
                    }
                    else
                    {
                        XLog.Error("XAsset.Scene.LoadAsync: async load error: {0}", sname);

                        // 加载错误时仍旧回调，业务层可根据 handler.Error 判断是否加载成功
                        handler.Error = true;
                        handler.InvokePostload();
                    }
                }
                else
                {
                    if (!Loading.TryGetValue(sname, out var task))
                    {
                        var req = SceneManager.LoadSceneAsync(sname, loadMode);
                        if (req != null)
                        {
                            task = new Task() { Name = sname, Operation = req };
                            handler.Operation = req;
                            handler.InvokePreload();
                            Loading.Add(sname, task);
                            yield return new WaitUntil(() => task.Operation.isDone);
                            Loading.Remove(sname);
                            handler.InvokePostload();
                        }
                        else
                        {
                            // 等待下一帧进行错误处理，避免业务层未获取到 handler 的实例
                            yield return null;

                            // 加载错误时仍旧回调，业务层可根据 handler.Error 判断是否加载成功
                            handler.Error = true;
                            handler.InvokePostload();
                        }
                    }
                    else
                    {
                        handler.Operation = task.Operation;
                        handler.InvokePreload();
                        yield return new WaitUntil(() => task.Operation.isDone);
                        handler.InvokePostload();
                    }
                }

                try { Event.Notify(EventType.OnPostLoadScene, sname); }
                catch (Exception e) { XLog.Panic(e); }
            }

            /// <summary>
            /// 卸载指定场景。在 Bundle 模式下会同时卸载场景对应的资源包。
            /// </summary>
            /// <param name="nameOrPath">要卸载的场景名称或路径</param>
            public static void Unload(string nameOrPath)
            {
                var sname = nameOrPath.Contains("/") ? Path.GetFileNameWithoutExtension(nameOrPath) : nameOrPath;
                if (Const.BundleMode && Manifest.Main)
                {
                    XLog.Info("XAsset.Scene.Unload: start to unload {0}, cached-ab: {1}", sname, Bundle.Loaded.Count);
                    string bname;
                    if (nameOrPath.Contains("/"))
                    {
                        if (nameOrPath.StartsWith("Assets/")) nameOrPath = nameOrPath["Assets/".Length..];
                        if (!nameOrPath.EndsWith(".unity")) nameOrPath += "_unity";
                        bname = Const.GenTag(nameOrPath);
                    }
                    else bname = $"scenes_{sname}_unity{Const.Extension}".ToLower();
                    Bundle.Unload(bname);
                    XLog.Info("XAsset.Scene.Unload: finish to unload {0}, cached-ab: {1}", sname, Bundle.Loaded.Count);
                }
            }
        }
    }
}
