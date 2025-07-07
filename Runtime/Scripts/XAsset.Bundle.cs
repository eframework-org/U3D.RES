// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EFramework.Utility;

namespace EFramework.Asset
{
    public partial class XAsset
    {
        /// <summary>
        /// XAsset.Bundle 提供了资源包的管理功能，支持自动处理依赖关系，并通过引用计数管理资源包的生命周期。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 资源包管理：支持同步和异步加载资源包，支持缓存
        /// - 引用计数管理：自动处理依赖关系，通过引用计数管理资源的生命周期
        ///
        /// 使用手册
        /// 1. 资源包管理
        ///    - 同步加载：加载指定的资源包及其所有依赖资源包
        ///      使用示例：
        ///      <code>
        ///      var bundle = XAsset.Bundle.Load("example.bundle");
        ///      </code>
        ///
        ///    - 异步加载：异步加载指定的资源包及其所有依赖资源包
        ///      使用示例：
        ///      <code>
        ///      var handler = new Handler();
        ///      yield return XAsset.Bundle.LoadAsync("example.bundle", handler);
        ///      </code>
        ///
        ///    - 查找加载：在已加载的资源包中查找指定名称的资源包
        ///      使用示例：
        ///      <code>
        ///      var bundle = XAsset.Bundle.Find("example.bundle");
        ///      </code>
        ///
        /// 2. 引用计数管理
        ///    - 增加引用：增加资源包的引用计数，同时增加所有依赖资源包的引用计数
        ///      使用示例：
        ///      <code>
        ///      int count = bundle.Obtain();
        ///      </code>
        ///
        ///    - 减少引用：减少资源包的引用计数，当计数为 0 时自动卸载资源包及其不再被引用的依赖资源
        ///      使用示例：
        ///      <code>
        ///      int count = bundle.Release();
        ///      </code>
        ///
        ///    - 卸载资源：卸载指定的资源包，减少其引用计数，当计数为 0 时释放资源
        ///      使用示例：
        ///      <code>
        ///      XAsset.Bundle.Unload("example.bundle");
        ///      </code>
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public partial class Bundle
        {
            /// <summary>
            /// Name 是资源包的名称，用于在资源系统中唯一标识一个资源包。
            /// </summary>
            public string Name { get; internal set; }

            /// <summary>
            /// Source 是 Unity 的 AssetBundle 对象，包含实际的资源数据。
            /// </summary>
            public AssetBundle Source { get; internal set; }

            /// <summary>
            /// Count 是资源包的引用计数，用于追踪资源包的使用情况。当计数为 0 时，资源包可以被安全卸载。
            /// </summary>
            public int Count { get; internal set; }

            /// <summary>
            /// Obtain 增加资源包的引用计数，同时会增加所有依赖资源包的引用计数。
            /// </summary>
            /// <param name="from">引用来源的描述，用于调试时追踪资源的使用情况</param>
            /// <returns>增加后的引用计数</returns>
            public int Obtain(string from = "")
            {
                var dependencies = Manifest.GetAllDependencies(Name);
                foreach (var dependency in dependencies)
                {
                    if (Name == dependency) continue;
                    if (Loaded.TryGetValue(dependency, out var dependBundle))
                    {
                        dependBundle.Count++;
                        if (Const.DebugMode) XLog.Debug("XAsset.Bundle.Obtain: depend bundle: {0}, reference count: {1}, cached bundle count: {2}, obtain from: {3}.", dependBundle.Name, dependBundle.Count, Loaded.Count, from);
                    }
                }
                Count++;
                if (Const.DebugMode) XLog.Debug("XAsset.Bundle.Obtain: main bundle: {0}, reference count: {1}, cached bundle count: {2}, obtain from: {3}.", Name, Count, Loaded.Count, from);
                return Count;
            }

            /// <summary>
            /// Release 减少资源包的引用计数，当计数降为 0 时，会自动卸载资源包及其不再被引用的依赖资源。
            /// </summary>
            /// <param name="from">引用来源的描述，用于调试时追踪资源的使用情况</param>
            /// <returns>减少后的引用计数</returns>
            public int Release(string from = "")
            {
                var dependencies = Manifest.GetAllDependencies(Name);
                if (dependencies != null && dependencies.Length > 0)
                {
                    for (var i = 0; i < dependencies.Length; i++)
                    {
                        var dependency = dependencies[i];
                        if (dependency == Name) continue;
                        if (Loaded.TryGetValue(dependency, out var dependBundle))
                        {
                            dependBundle.Count--;
                            if (Const.DebugMode) XLog.Debug("XAsset.Bundle.Release: depend bundle: {0}, reference count: {1}, cached bundle count: {2}, release from: {3}", dependBundle.Name, dependBundle.Count, Loaded.Count, from);
                            if (dependBundle.Source == null) Loaded.Remove(dependency);
                            else if (dependBundle.Count <= 0)
                            {
                                dependBundle.Source.Unload(true);
                                Loaded.Remove(dependency);
                                if (Const.DebugMode) XLog.Debug("XAsset.Bundle.Release: unload depend bundle: {0}, reference count: {1}, release from: {2}", dependBundle.Name, Loaded.Count, from);
                                try { Event.Notify(EventType.OnPostUnloadBundle, dependBundle.Source); }
                                catch (Exception e) { XLog.Panic(e); }
                            }
                        }
                    }
                }
                Count--;
                if (Const.DebugMode) XLog.Debug("XAsset.Bundle.Release: main bundle: {0}, reference count: {1}, cached bundle count: {2}, release from: {3}", Name, Count, Loaded.Count, from);
                if (Source == null)
                {
                    Loaded.Remove(Name);
                }
                else if (Count <= 0)
                {
                    try { Event.Notify(EventType.OnPostUnloadBundle, Source); }
                    catch (Exception e) { XLog.Panic(e); }
                    Source.Unload(true);
                    Loaded.Remove(Name);
                    if (Const.DebugMode) XLog.Debug("XAsset.Bundle.Release: unload main bundle: {0}, cached bundle count: {1}, release from: {2}", Name, Loaded.Count, from);
                }
                return Count;
            }
        }

        public partial class Bundle
        {
            /// <summary>
            /// Task 是异步加载任务，用于跟踪资源包的加载状态和进度。
            /// </summary>
            internal class Task
            {
                /// <summary>
                /// Name 是正在加载的资源包名称。
                /// </summary>
                internal string Name;

                /// <summary>
                /// Bundle 是加载的 AssetBundle 对象。
                /// </summary>
                internal AssetBundle Bundle;

                /// <summary>
                /// IsDone 表示是否完成加载。
                /// </summary>
                internal bool IsDone;

                /// <summary>
                /// OnPostload 用于监听加载完成事件。
                /// </summary>
                internal event Action OnPostload;

                /// <summary>
                /// InvokePostOnLoad 通知加载完成事件。
                /// </summary>
                internal void InvokePostOnLoad() { OnPostload?.Invoke(); }
            }

            /// <summary>
            /// Manifest 是 Bundle 依赖的清单。
            /// </summary>
            internal static AssetBundleManifest Manifest;

            /// <summary>
            /// manifestBundle 保持了 AssetBundleManifest 所在 Bundle 的引用。
            /// </summary>
            private static AssetBundle manifestBundle;

            /// <summary>
            /// Loading 记录当前正在加载的资源包，用于处理并发加载请求。
            /// </summary>
            internal static Dictionary<string, Task> Loading = new();

            /// <summary>
            /// Loaded 缓存已加载的资源包，避免重复加载相同的资源。
            /// </summary>
            internal static Dictionary<string, Bundle> Loaded = new();

            /// <summary>
            /// Initialize 初始化 Bundle 的清单文件，如果存在旧的清单会先卸载它。
            /// 仅适用于 Bundle 模式，这个清单文件对于资源的正确加载是必需的。
            /// </summary>
            public static void Initialize()
            {
                if (Const.BundleMode)
                {
                    try
                    {
                        if (manifestBundle)
                        {
                            manifestBundle.Unload(true);
                            XLog.Notice("XAsset.Bundle.Initialize: previous manifest has been unloaded.");
                        }
                    }
                    catch (Exception e) { XLog.Panic(e, "XAsset.Bundle.Initialize: unload manifest failed."); }

                    var file = XFile.PathJoin(Const.LocalPath, Const.GetName(Const.Manifest));
                    if (XFile.HasFile(file))
                    {
                        try
                        {
                            manifestBundle = AssetBundle.LoadFromFile(file, 0, Const.GetOffset(Path.GetFileName(file)));
                            Manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                            XLog.Notice("XAsset.Bundle.Initialize: load <a href=\"file:///{0}\">{1}</a> succeeded.", Path.GetFullPath(file), Path.GetRelativePath(XEnv.ProjectPath, file));
                        }
                        catch (Exception e) { XLog.Panic(e, "XAsset.Bundle.Initialize: load <a href=\"file:///{0}\">{1}</a> failed.".Format(Path.GetFullPath(file), Path.GetRelativePath(XEnv.ProjectPath, file))); }
                    }
                    else XLog.Warn("XAsset.Bundle.Initialize: load failed because of non exist file: {0}.", Path.GetRelativePath(XEnv.ProjectPath, file));
                }
                else Manifest = null;
            }

            /// <summary>
            /// Load 同步加载资源包。如果资源包已加载，则直接返回缓存的实例，否则会加载资源包及其所有依赖。
            /// </summary>
            /// <param name="name">要加载的资源包名称</param>
            /// <returns>加载的资源包，如果加载失败则返回 null</returns>
            public static Bundle Load(string name)
            {
                if (!Loaded.TryGetValue(name, out var bundleInfo))
                {
                    var dependencies = Manifest.GetAllDependencies(name);
                    if (dependencies != null && dependencies.Length > 0)
                    {
                        var breakDependency = -1;
                        for (var i = 0; i < dependencies.Length; i++)
                        {
                            var dependency = dependencies[i];
                            if (!Loaded.TryGetValue(dependency, out var dependBundle))
                            {
                                if (!Loading.TryGetValue(dependency, out var task))
                                {
                                    task = new Task() { Name = dependency };
                                    Loading.Add(dependency, task);

                                    var path = XFile.PathJoin(Const.LocalPath, dependency);
                                    var bundle = AssetBundle.LoadFromFile(path, 0, Const.GetOffset(dependency));
                                    if (bundle == null)
                                    {
                                        XLog.Error("XAsset.Bundle.Load: sync load depend bundle error: {0}", dependency);
                                        breakDependency = i;

                                        task.Bundle = null;
                                        task.IsDone = true;
                                        task.InvokePostOnLoad();
                                        Loading.Remove(dependency);
                                        break;
                                    }
                                    else
                                    {
                                        dependBundle = new Bundle() { Name = dependency, Source = bundle, Count = 1 };
                                        Loaded.Add(dependency, dependBundle);

                                        task.Bundle = bundle;
                                        task.IsDone = true;
                                        task.InvokePostOnLoad();
                                        Loading.Remove(dependency);
                                    }
                                }
                                else
                                {
                                    // 异步加载完成的回调
                                    task.OnPostload += () =>
                                    {
                                        if (Loaded.TryGetValue(task.Name, out var dependBundle))
                                        {
                                            dependBundle.Obtain(Const.DebugMode ? $"[Sync.Load.1: {name}]" : "");
                                        }
                                    };
                                }
                            }
                            else dependBundle.Obtain(Const.DebugMode ? $"[Sync.Load.2: {name}]" : "");
                        }

                        // 如果有任何一个依赖加载失败，则解除对已加载依赖的引用
                        if (breakDependency >= 0)
                        {
                            for (var i = 0; i < breakDependency; i++)
                            {
                                var dependency = dependencies[i];
                                if (Loaded.TryGetValue(dependency, out var dependBundle))
                                {
                                    dependBundle.Release(Const.DebugMode ? $"[Sync.Load.3: {name}]" : "");
                                }
                            }

                            return null; // 不再执行后续流程
                        }
                    }

                    var bundleFile = XFile.PathJoin(Const.LocalPath, name);
                    var mainBundle = AssetBundle.LoadFromFile(bundleFile, 0, Const.GetOffset(name));
                    if (mainBundle == null)
                    {
                        XLog.Error("XAsset.Bundle.Load: sync load main bundle error: {0}", name);

                        // 如果主包加载失败，则解除对所有依赖的引用
                        if (dependencies != null && dependencies.Length > 0)
                        {
                            for (var i = 0; i < dependencies.Length; i++)
                            {
                                var dependency = dependencies[i];
                                if (Loaded.TryGetValue(dependency, out var tmp))
                                {
                                    tmp.Release(Const.DebugMode ? $"[Sync.Load.4: {name}]" : "");
                                }
                            }
                        }

                        return null; // 不再执行后续流程
                    }
                    else
                    {
                        bundleInfo = new Bundle() { Name = name, Source = mainBundle, Count = 1 };
                        Loaded.Add(name, bundleInfo);
                        return bundleInfo;
                    }
                }
                else
                {
                    bundleInfo.Obtain(Const.DebugMode ? $"[Sync.Load.5: {name}]" : "");
                    return bundleInfo;
                }
            }

            /// <summary>
            /// LoadAsync 异步加载资源包。支持同时加载多个资源包，并通过 handler 参数报告加载进度。
            /// </summary>
            /// <param name="name">要加载的资源包名称</param>
            /// <param name="handler">用于跟踪和报告加载进度的处理器</param>
            /// <returns>异步加载的协程对象</returns>
            public static IEnumerator LoadAsync(string name, Handler handler)
            {
                if (!Loaded.TryGetValue(name, out Bundle info))
                {
                    var dependencies = Manifest.GetAllDependencies(name);
                    handler.totalCount += dependencies.Length + 1; // Self and Dependency
                    if (dependencies != null && dependencies.Length > 0)
                    {
                        var breakDependency = -1;
                        for (var i = 0; i < dependencies.Length; i++)
                        {
                            var dependency = dependencies[i];
                            if (!Loaded.TryGetValue(dependency, out var dependBundle))
                            {
                                if (!Loading.TryGetValue(dependency, out var task))
                                {
                                    task = new Task() { Name = dependency };
                                    Loading.Add(dependency, task);

                                    var file = XFile.PathJoin(Const.LocalPath, dependency);
                                    var request = AssetBundle.LoadFromFileAsync(file, 0, Const.GetOffset(dependency));
                                    yield return request;

                                    task.Bundle = request.assetBundle;
                                    task.IsDone = true;
                                    task.InvokePostOnLoad();
                                    Loading.Remove(dependency);
                                }
                                else yield return new WaitUntil(() => task.IsDone);

                                if (!Loaded.TryGetValue(dependency, out dependBundle))
                                {
                                    if (task.Bundle == null)
                                    {
                                        XLog.Error("XAsset.Bundle.LoadAsync: async load depend bundle error: {0}", dependency);
                                        breakDependency = i;
                                        break;
                                    }
                                    else
                                    {
                                        var bundle = new Bundle() { Name = dependency, Source = task.Bundle, Count = 1 };
                                        Loaded.Add(dependency, bundle);
                                    }
                                }
                                else dependBundle.Obtain(Const.DebugMode ? $"[Async.Load.1: {name}]" : "");
                            }
                            else dependBundle.Obtain(Const.DebugMode ? $"[Async.Load.2: {name}]" : "");
                            handler.doneCount++;
                        }

                        // 如果有任何一个依赖加载失败，则解除对已加载依赖的引用
                        if (breakDependency >= 0)
                        {
                            for (var i = 0; i < breakDependency; i++)
                            {
                                var dependency = dependencies[i];
                                if (Loaded.TryGetValue(dependency, out var dependBundle))
                                {
                                    dependBundle.Release(Const.DebugMode ? $"[Async.Load.3: {name}]" : "");
                                }
                            }

                            yield break; // 不再执行后续流程
                        }
                    }

                    if (!Loaded.TryGetValue(name, out var mainBundle))
                    {
                        if (!Loading.TryGetValue(name, out var task))
                        {
                            task = new Task() { Name = name };
                            Loading.Add(name, task);

                            var file = XFile.PathJoin(Const.LocalPath, name);
                            var request = AssetBundle.LoadFromFileAsync(file, 0, Const.GetOffset(name));
                            yield return request;

                            task.Bundle = request.assetBundle;
                            task.IsDone = true;
                            task.InvokePostOnLoad();
                            Loading.Remove(name);
                        }
                        else yield return new WaitUntil(() => task.IsDone);
                        if (task.Bundle == null)
                        {
                            XLog.Error("XAsset.Bundle.LoadAsync: async load main bundle error: {0}", name);

                            // 如果主包加载失败，则解除对所有依赖的引用
                            if (dependencies != null && dependencies.Length > 0)
                            {
                                for (var i = 0; i < dependencies.Length; i++)
                                {
                                    var dependency = dependencies[i];
                                    if (Loaded.TryGetValue(dependency, out var dependBundle))
                                    {
                                        dependBundle.Release(Const.DebugMode ? $"[Async.Load.4: {name}]" : "");
                                    }
                                }
                            }

                            yield break;  // 不再执行后续流程
                        }
                        else
                        {
                            if (!Loaded.TryGetValue(name, out mainBundle))
                            {
                                mainBundle = new Bundle() { Name = name, Source = task.Bundle, Count = 1 };
                                Loaded.Add(name, mainBundle);
                            }
                            else mainBundle.Obtain(Const.DebugMode ? $"[Async.Load.5: {name}]" : "");
                        }
                    }
                    else mainBundle.Obtain(Const.DebugMode ? $"[Async.Load.6: {name}]" : "");
                    handler.doneCount++;
                }
                else info.Obtain(Const.DebugMode ? $"[Async.Load.7: {name}]" : "");
            }

            /// <summary>
            /// Unload 卸载指定的资源包。这会减少资源包的引用计数，当计数为 0 时自动释放资源。
            /// </summary>
            /// <param name="name">要卸载的资源包名称</param>
            public static void Unload(string name)
            {
                if (Loaded.TryGetValue(name, out var info))
                {
                    info.Release(Const.DebugMode ? $"[Sync.Unload: {name}]" : "");
                }
            }

            /// <summary>
            /// Find 在已加载的资源包中查找指定名称的资源包。
            /// </summary>
            /// <param name="name">要查找的资源包名称</param>
            /// <returns>找到的资源包，如果未找到则返回 null</returns>
            public static Bundle Find(string name)
            {
                Loaded.TryGetValue(name, out var info);
                return info;
            }
        }
    }
}
