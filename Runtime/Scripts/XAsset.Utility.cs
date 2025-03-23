// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

namespace EFramework.Asset
{
    public partial class XAsset
    {
        /// <summary>
        /// XAsset.Utility 提供了资源加载的工具函数集，包括进度监控和状态查询等功能。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 进度监控：支持获取所有加载任务的总体进度
        /// - 状态查询：支持查询指定资源或全局加载状态
        ///
        /// 使用手册
        /// 1. 进度监控
        ///    - 功能说明：获取总体加载进度
        ///      函数返回：`float` 类型，范围为 `0~1`，`1` 表示全部加载完成
        ///      使用示例：
        ///      <code>
        ///      float progress = XAsset.Utility.Progress();
        ///      </code>
        ///
        /// 2. 状态查询
        ///    - 功能说明：检查全局加载状态
        ///      函数返回：`bool` 类型，`true` 表示有资源正在加载
        ///      使用示例：
        ///      <code>
        ///      bool isLoading = XAsset.Utility.Loading();
        ///      </code>
        ///
        ///    - 功能：检查指定资源加载状态
        ///      函数参数：
        ///        - `path`：资源路径（`string` 类型），为空时检查全局加载状态
        ///      函数返回：`bool` 类型，`true` 表示指定资源正在加载
        ///      使用示例：
        ///      <code>
        ///      bool isResourceLoading = XAsset.Utility.Loading("Resources/Example/Test.prefab");
        ///      </code>
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public partial class Utility
        {
            /// <summary>
            /// 获取当前所有加载任务的总体进度。计算方式为：
            /// 1. 统计所有资源、场景和资源包的加载任务数
            /// 2. 累加每个任务的当前进度
            /// 3. 计算平均进度作为整体进度
            /// </summary>
            /// <returns>返回 0~1 之间的进度值，1 表示全部加载完成</returns>
            public static float Progress()
            {
                var total = Resource.Loading.Count + Scene.Loading.Count + Bundle.Loading.Count;
                if (total == 0f) return 1; // 没有加载任务时返回完成状态
                var current = 0f;

                // 计算资源加载进度
                if (Resource.Loading.Count > 0)
                {
                    foreach (var item in Resource.Loading)
                    {
                        current += item.Value.Operation.progress;
                    }
                }
                if (Scene.Loading.Count > 0)
                {
                    foreach (var item in Scene.Loading)
                    {
                        current += item.Value.Operation.progress;
                    }
                }
                if (Bundle.Loading.Count > 0)
                {
                    foreach (var item in Bundle.Loading)
                    {
                        current += item.Value.Operation.progress;
                    }
                }
                return current / total; // 返回平均进度
            }

            /// <summary>
            /// 检查资源加载状态。可查询指定资源的加载状态，或检查是否有任何资源正在加载。
            /// </summary>
            /// <param name="path">资源路径。如果为空，则检查是否有任何资源在加载；否则检查指定资源是否在加载</param>
            /// <returns>当指定路径时，返回该资源是否正在加载；未指定路径时，返回是否有任何资源正在加载</returns>
            public static bool Loading(string path = "")
            {
                if (string.IsNullOrEmpty(path)) return Resource.Loading.Count > 0 || Scene.Loading.Count > 0; // 检查是否有任何资源正在加载
                else
                {
                    var ret = Resource.Loading.ContainsKey(path); // 先检查普通资源
                    if (ret) return ret;
                    return Scene.Loading.ContainsKey(path); // 再检查场景资源
                }
            }
        }
    }
}
