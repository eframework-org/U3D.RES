// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using UnityEngine;
using EFramework.Utility;

namespace EFramework.Asset
{
    public partial class XAsset
    {
        /// <summary>
        /// XAsset.Manifest 提供了资源清单的管理功能，用于维护和管理资源包的依赖关系。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 依赖管理：通过清单文件追踪和管理资源包的依赖关系
        /// - 自动加载：根据当前平台自动加载对应的清单文件
        /// - 内存管理：支持清单资源包的正确卸载，避免内存泄漏
        ///
        /// 使用手册
        /// 1. 加载清单：加载当前平台对应的资源清单文件
        ///      详细描述：
        ///        - 如果存在旧的清单文件，会先卸载它
        ///        - 加载成功后，清单文件会被缓存到 `Main` 属性中
        ///      使用示例：
        ///      <code>
        ///      XAsset.Manifest.Load();
        ///      </code>
        ///
        /// 2. 清单属性：获取当前加载的资源清单实例
        ///      使用示例：
        ///      <code>
        ///      var manifest = XAsset.Manifest.Main;
        ///      </code>
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public partial class Manifest
        {
            /// <summary>
            /// Unity的资源包清单对象，包含了所有资源包的依赖关系信息。
            /// 在资源加载时会用它来确定需要加载哪些依赖资源。
            /// </summary>
            public static AssetBundleManifest Main { get; internal set; }

            /// <summary>
            /// 当前加载的清单资源包。这个资源包包含了清单文件本身，
            /// 需要在使用完毕后正确卸载以避免内存泄漏。
            /// </summary>
            internal static AssetBundle Bundle;

            /// <summary>
            /// 加载资源清单文件。这个方法会根据当前平台加载对应的清单文件，
            /// 如果存在旧的清单会先卸载它。在Bundle模式下，这个清单文件
            /// 对于资源的正确加载是必需的。
            /// </summary>
            public static void Load()
            {
                if (Const.BundleMode)
                {
                    try
                    {
                        if (Bundle)
                        {
                            Bundle.Unload(true);
                            XLog.Notice("XAsset.Manifest.Load: previous manifest has been unloaded.");
                        }
                    }
                    catch (Exception e) { XLog.Panic(e, "XAsset.Manifest.Load: unload manifest failed."); }

                    var file = XFile.PathJoin(Const.LocalPath, XEnv.Platform.ToString());
                    if (XFile.HasFile(file))
                    {
                        try
                        {
                            Bundle = AssetBundle.LoadFromFile(file);
                            Main = Bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                            XLog.Notice("XAsset.Manifest.Load: load <a href=\"file:///{0}\">{1}</a> succeed.", file, file);
                        }
                        catch (Exception e) { XLog.Panic(e, "XAsset.Manifest.Load: load <a href=\"file:///{0}\">{1}</a> failed.".Format(file, file)); }
                    }
                    else XLog.Warn("XAsset.Manifest.Load: load failed because of non exist file: {0}.", file);
                }
            }
        }
    }
}