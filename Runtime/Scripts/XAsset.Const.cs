// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System.Collections.Generic;
using UnityEngine;
using EFramework.Utility;

namespace EFramework.Asset
{
    public partial class XAsset
    {
        /// <summary>
        /// XAsset.Const 提供了一些常量定义和运行时环境控制，包括运行配置和标签生成等功能。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 运行配置：提供 Bundle 模式、调试模式和资源路径等配置
        /// - 标签生成：提供资源路径的标准化处理和标签生成
        ///
        /// 使用手册
        /// 1. 运行配置
        ///    - 配置项：Bundle 模式
        ///      配置说明：控制是否启用 Bundle 模式
        ///      依赖条件：编辑器模式下需要启用模拟模式
        ///      访问方式：
        ///      <code>
        ///      bool isBundleMode = XAsset.Const.BundleMode;
        ///      </code>
        ///
        ///    - 配置项：引用计数模式
        ///      配置说明：控制是否启用引用计数模式
        ///      依赖条件：仅在 Bundle 模式下可用
        ///      访问方式：
        ///      <code>
        ///      bool isReferMode = XAsset.Const.ReferMode;
        ///      </code>
        ///
        ///    - 配置项：调试模式
        ///      配置说明：控制是否启用调试模式
        ///      依赖条件：仅在 Bundle 模式下可用
        ///      访问方式：
        ///      <code>
        ///      bool isDebugMode = XAsset.Const.DebugMode;
        ///      </code>
        ///
        ///    - 配置项：本地路径
        ///      配置说明：获取资源文件的本地存储路径
        ///      访问方式：
        ///      <code>
        ///      string localPath = XAsset.Const.LocalPath;
        ///      </code>
        ///
        /// 2. 标签生成
        ///    - 配置项：默认扩展名
        ///      <code>
        ///      public const string Extension = ".bundle";
        ///      </code>
        ///
        ///    - 配置项：生成规则
        ///      生成资源的标签名称，规则如下：
        ///      1. 将路径分隔符替换为下划线
        ///      2. 移除特殊字符
        ///      3. 转换为小写
        ///      4. 添加 `.bundle` 扩展名
        ///
        ///      使用示例：
        ///      <code>
        ///      string assetPath = "Resources/Example/Test.prefab";
        ///      string tag = XAsset.Const.GenTag(assetPath);
        ///      // 输出: assets_example_test.bundle
        ///      </code>
        ///
        ///      特殊字符处理规则：
        ///      <code>
        ///      {
        ///          "_" -> "_"  // 保留
        ///          " " -> ""   // 移除
        ///          "#" -> ""   // 移除
        ///          "[" -> ""   // 移除
        ///          "]" -> ""   // 移除
        ///      }
        ///      </code>
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public partial class Const
        {
            #region 运行配置
            /// <summary>
            /// Manifest 表示 AssetBundleManifest 的默认文件名。
            /// </summary>
            public static string Manifest = "Assets";

            /// <summary>
            /// Bundle 模式的初始化标记。
            /// </summary>
            internal static bool bBundleMode;

            /// <summary>
            /// Bundle 模式的当前状态。
            /// </summary>
            internal static bool bundleMode;

            /// <summary>
            /// 获取资源系统当前是否运行在 Bundle 模式下。Bundle 模式下会从打包的资源文件中加载资源，
            /// 而不是直接从 Assets 目录加载。这个设置会受到编辑器状态和模拟模式的影响。
            /// </summary>
            public static bool BundleMode
            {
                get
                {
                    if (bBundleMode == false)
                    {
                        bBundleMode = true;
                        bundleMode = XPrefs.GetBool(Prefs.BundleMode, Prefs.BundleModeDefault) && (!Application.isEditor || XPrefs.GetBool(Prefs.SimulateMode));
                    }
                    return bundleMode;
                }
            }

            /// <summary>
            /// 引用模式的初始化标记
            /// </summary>
            internal static bool bReferMode;

            /// <summary>
            /// 引用模式的当前状态
            /// </summary>
            internal static bool referMode;

            /// <summary>
            /// 获取是否启用了引用计数模式。在引用计数模式下，系统会跟踪资源的使用情况，
            /// 只有当资源不再被任何对象引用时才会被卸载。这个模式需要 Bundle 模式开启才能生效。
            /// </summary>
            public static bool ReferMode
            {
                get
                {
                    if (bReferMode == false)
                    {
                        bReferMode = true;
                        referMode = BundleMode && XPrefs.GetBool(Prefs.ReferMode, Prefs.ReferModeDefault);
                    }
                    return referMode;
                }
            }

            /// <summary>
            /// 调试模式的初始化标记
            /// </summary>
            internal static bool bDebugMode;

            /// <summary>
            /// 调试模式的当前状态
            /// </summary>
            internal static bool debugMode;

            /// <summary>
            /// 获取是否启用了调试模式。调试模式下会输出详细的日志信息，
            /// 帮助开发者追踪资源加载和卸载的过程。这个模式同样需要 Bundle 模式开启才能生效。
            /// </summary>
            public static bool DebugMode
            {
                get
                {
                    if (bDebugMode == false)
                    {
                        bDebugMode = true;
                        debugMode = BundleMode && XPrefs.GetBool(Prefs.DebugMode);
                    }
                    return debugMode;
                }
            }

            /// <summary>
            /// 本地路径的初始化标记
            /// </summary>
            internal static bool bLocalPath;

            /// <summary>
            /// 缓存的本地路径
            /// </summary>
            internal static string localPath;

            /// <summary>
            /// 获取资源文件的本地存储路径。这个路径用于存放下载或解压后的资源文件，
            /// 路径格式会根据当前平台和配置自动调整。
            /// </summary>
            public static string LocalPath
            {
                get
                {
                    if (bLocalPath == false)
                    {
                        bLocalPath = true;
                        localPath = XFile.PathJoin(XEnv.LocalPath, XPrefs.GetString(Prefs.LocalUri, Prefs.LocalUriDefault));
                    }
                    return localPath;
                }
            }
            #endregion

            #region 标签生成
            /// <summary>
            /// 资源包文件的扩展名，用于标识打包后的资源文件。
            /// </summary>
            public static string Extension = ".bundle";

            /// <summary>
            /// 资源名称转换规则表，定义了如何处理资源路径中的特殊字符。
            /// 这些规则会在生成资源包名称时使用，确保生成的名称符合文件系统要求。
            /// </summary>
            internal static readonly Dictionary<string, string> escapeChars = new() {
                { "_", "_" },
                { " ", "" },
                { "#", "" },
                { "[", "" },
                { "]", "" }
            };

            /// <summary>
            /// 资源标签缓存，用于提高重复标签生成的性能。
            /// 避免对相同的资源路径重复进行标签转换计算。
            /// </summary>
            internal static readonly Dictionary<string, string> tagCache = new();

            /// <summary>
            /// 根据资源路径生成唯一的资源标签。这个标签将用作资源包的文件名，
            /// 会自动处理路径中的特殊字符并确保生成的名称符合文件系统规范。
            /// </summary>
            /// <param name="assetPath">需要转换的资源路径</param>
            /// <returns>转换后的资源标签，已处理特殊字符并添加扩展名</returns>
            public static string GenTag(string assetPath)
            {
                if (tagCache.TryGetValue(assetPath, out var tag)) return tag;
                else
                {
                    var bundleName = assetPath.Replace("/", "_").Replace("\\", "_");
                    foreach (var item in escapeChars) bundleName = bundleName.Replace(item.Key, item.Value);
                    bundleName += Extension;
                    bundleName = bundleName.ToLower();
                    tagCache[assetPath] = bundleName;
                    return bundleName;
                }
            }

            internal static bool bOffsetFactor;
            internal static int offsetFactor;

            /// <summary>
            /// GetOffset 根据首选项中配置的 Bundle 偏移配置进行文件的偏移计算。
            /// </summary>
            /// <param name="bundleName">需要计算文件偏移的 Bundle 名称</param>
            /// <returns>Bundle 文件偏移</returns>
            public static ulong GetOffset(string bundleName)
            {
                if (string.IsNullOrEmpty(bundleName)) return 0;
                if (!bOffsetFactor)
                {
                    bOffsetFactor = true;
                    offsetFactor = XPrefs.GetInt(Prefs.OffsetFactor, Prefs.OffsetFactorDefault);
                }
                if (offsetFactor <= 0) return 0;
                return (ulong)((bundleName.Length % offsetFactor + 1) * 28);
            }
            #endregion
        }
    }
}