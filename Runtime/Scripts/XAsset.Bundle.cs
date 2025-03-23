// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
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
            /// 资源包的名称，用于在资源系统中唯一标识一个资源包。
            /// </summary>
            public string Name { get; internal set; }

            /// <summary>
            /// Unity 的 AssetBundle 对象，包含实际的资源数据。
            /// </summary>
            public AssetBundle Source { get; internal set; }

            /// <summary>
            /// 资源包的引用计数，用于追踪资源包的使用情况。当计数为 0 时，资源包可以被安全卸载。
            /// </summary>
            public int Count { get; internal set; }

            /// <summary>
            /// 增加资源包的引用计数。同时会增加所有依赖资源包的引用计数。
            /// </summary>
            /// <param name="from">引用来源的描述，用于调试时追踪资源的使用情况</param>
            /// <returns>增加后的引用计数</returns>
            public int Obtain(string from = "")
            {
                var deps = Manifest.Main.GetAllDependencies(Name);
                foreach (var dep in deps)
                {
                    if (Name == dep) continue;
                    if (Loaded.TryGetValue(dep, out var db))
                    {
                        db.Count++;
                        if (Const.DebugMode) XLog.Debug("XAsset.Bundle.Obtain: dep-ab: {0}, ref-count: {1}, cached-ab: {2}, from: {3}", db.Name, db.Count, Loaded.Count, from);
                    }
                }
                Count++;
                if (Const.DebugMode) XLog.Debug("XAsset.Bundle.Obtain: main-ab: {0}, ref-count: {1}, cached-ab: {2}, from: {3}", Name, Count, Loaded.Count, from);
                return Count;
            }

            /// <summary>
            /// 减少资源包的引用计数。当计数降为 0 时，会自动卸载资源包及其不再被引用的依赖资源。
            /// </summary>
            /// <param name="from">引用来源的描述，用于调试时追踪资源的使用情况</param>
            /// <returns>减少后的引用计数</returns>
            public int Release(string from = "")
            {
                var deps = Manifest.Main.GetAllDependencies(Name);
                if (deps != null || deps.Length > 0)
                {
                    for (var i = 0; i < deps.Length; i++)
                    {
                        var dep = deps[i];
                        if (dep == Name) continue;
                        if (Loaded.TryGetValue(dep, out var db))
                        {
                            db.Count--;
                            if (Const.DebugMode) XLog.Debug("XAsset.Bundle.Release: dep-ab: {0}, ref-count: {1}, cached-ab: {2}, from: {3}", db.Name, db.Count, Loaded.Count, from);
                            if (db.Source == null) Loaded.Remove(dep);
                            else if (db.Count <= 0)
                            {
                                db.Source.Unload(true);
                                Loaded.Remove(dep);
                                if (Const.DebugMode) XLog.Debug("XAsset.Bundle.Release: unload dep-ab: {0}, cached-ab: {1}, from: {2}", db.Name, Loaded.Count, from);
                                try { Event.Notify(EventType.OnPostUnloadBundle, db.Source); }
                                catch (Exception e) { XLog.Panic(e); }
                            }
                        }
                    }
                }
                Count--;
                if (Const.DebugMode) XLog.Debug("XAsset.Bundle.Release: main-ab: {0}, ref-count: {1}, cached-ab: {2}, from: {3}", Name, Count, Loaded.Count, from);
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
                    if (Const.DebugMode) XLog.Debug("XAsset.Bundle.Release: unload main-ab: {0}, cached-ab: {1}, from: {2}", Name, Loaded.Count, from);
                }
                return Count;
            }
        }

        public partial class Bundle
        {
            /// <summary>
            /// 表示一个异步加载任务，用于跟踪资源包的加载状态和进度。
            /// </summary>
            internal class Task
            {
                /// <summary>
                /// 正在加载的资源包名称。
                /// </summary>
                internal string Name;

                /// <summary>
                /// Unity 的异步加载操作对象。
                /// </summary>
                internal AssetBundleCreateRequest Operation;
            }

            /// <summary>
            /// 记录当前正在加载的资源包，用于处理并发加载请求。
            /// </summary>
            internal static Dictionary<string, Task> Loading = new();

            /// <summary>
            /// 缓存已加载的资源包，避免重复加载相同的资源。
            /// </summary>
            internal static Dictionary<string, Bundle> Loaded = new();

            /// <summary>
            /// 同步加载资源包。如果资源包已加载，则直接返回缓存的实例。否则会加载资源包及其所有依赖。
            /// </summary>
            /// <param name="name">要加载的资源包名称</param>
            /// <returns>加载的资源包，如果加载失败则返回 null</returns>
            public static Bundle Load(string name)
            {
                if (!Loaded.TryGetValue(name, out var info))
                {
                    var deps = Manifest.Main.GetAllDependencies(name);
                    if (deps != null || deps.Length > 0)
                    {
                        for (var i = 0; i < deps.Length; i++)
                        {
                            var dep = deps[i];
                            if (!Loaded.TryGetValue(dep, out var tmp))
                            {
                                var path = XFile.PathJoin(Const.LocalPath, dep);
                                var bundle = AssetBundle.LoadFromFile(path);
                                tmp = new Bundle() { Name = dep, Source = bundle, Count = 1 };
                                Loaded.Add(dep, tmp);
                            }
                            else tmp.Obtain(Const.DebugMode ? $"[Sync.Load.1: {name}]" : "");
                        }
                    }

                    var file = XFile.PathJoin(Const.LocalPath, name);
                    if (XFile.HasFile(file))
                    {
                        var bundle = AssetBundle.LoadFromFile(file);
                        info = new Bundle() { Name = name, Source = bundle, Count = 1 };
                        Loaded.Add(name, info);
                        return info;
                    }
                    else return null;
                }
                else
                {
                    info.Obtain(Const.DebugMode ? $"[Sync.Load.2: {name}]" : "");
                    return info;
                }
            }

            /// <summary>
            /// 异步加载资源包。支持同时加载多个资源包，并通过 handler 参数报告加载进度。
            /// </summary>
            /// <param name="name">要加载的资源包名称</param>
            /// <param name="handler">用于跟踪和报告加载进度的处理器</param>
            /// <returns>异步加载的协程对象</returns>
            public static IEnumerator LoadAsync(string name, Handler handler)
            {
                if (!Loaded.TryGetValue(name, out Bundle info))
                {
                    var deps = Manifest.Main.GetAllDependencies(name);
                    handler.totalCount += deps.Length + 1; // Self and Dependency
                    if (deps != null || deps.Length > 0)
                    {
                        for (var i = 0; i < deps.Length; i++)
                        {
                            var dep = deps[i];
                            if (!Loaded.TryGetValue(dep, out var info2))
                            {
                                if (!Loading.TryGetValue(dep, out var task))
                                {
                                    var path = XFile.PathJoin(Const.LocalPath, dep);
                                    var req = AssetBundle.LoadFromFileAsync(path);
                                    task = new Task() { Name = dep, Operation = req };
                                    Loading.Add(dep, task);
                                    yield return new WaitUntil(() => task.Operation.isDone);
                                    Loading.Remove(dep);
                                }
                                else yield return new WaitUntil(() => task.Operation.isDone);

                                if (!Loaded.TryGetValue(dep, out var dbundle))
                                {
                                    if (task.Operation.assetBundle == null) XLog.Error("XAsset.Bundle.LoadAsync: async load error: {0}", dep);
                                    else
                                    {
                                        var bundle = new Bundle() { Name = dep, Source = task.Operation.assetBundle, Count = 1 };
                                        Loaded.Add(dep, bundle);
                                    }
                                }
                                else dbundle.Obtain(Const.DebugMode ? $"[Async.Load.1: {name}]" : "");
                            }
                            else info2.Obtain(Const.DebugMode ? $"[Async.Load.2: {name}]" : "");
                            handler.doneCount++;
                        }
                    }

                    if (!Loaded.TryGetValue(name, out var info3))
                    {
                        if (!Loading.TryGetValue(name, out var task))
                        {
                            var path = XFile.PathJoin(Const.LocalPath, name);
                            var req = AssetBundle.LoadFromFileAsync(path);
                            task = new Task() { Name = name, Operation = req };
                            Loading.Add(name, task);
                            yield return new WaitUntil(() => task.Operation.isDone);
                            Loading.Remove(name);
                        }
                        else yield return new WaitUntil(() => task.Operation.isDone);
                        if (task.Operation.assetBundle == null) XLog.Error("XAsset.Bundle.LoadAsync: async load error: {0}", name);
                        else
                        {
                            if (Loaded.TryGetValue(name, out var dbundle) == false)
                            {
                                info3 = new Bundle() { Name = name, Source = task.Operation.assetBundle, Count = 1 };
                                Loaded.Add(name, info3);
                            }
                            else dbundle.Obtain(Const.DebugMode ? $"[Async.Load.3: {name}]" : "");
                        }
                    }
                    else info3.Obtain(Const.DebugMode ? $"[Async.Load.4: {name}]" : "");
                    handler.doneCount++;
                }
                else info.Obtain(Const.DebugMode ? $"[Async.Load.5: {name}]" : "");
                yield return 0;
            }

            /// <summary>
            /// 卸载指定的资源包。这会减少资源包的引用计数，当计数为 0 时自动释放资源。
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
            /// 在已加载的资源包中查找指定名称的资源包。这个操作不会触发加载过程。
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
