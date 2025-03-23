// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using UnityEngine;
using EFramework.Utility;

namespace EFramework.Asset
{
    public partial class XAsset
    {
        /// <summary>
        /// XAsset.Prefs 提供了运行时的首选项管理，用于控制运行模式、调试选项和资源路径等配置项。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 运行模式配置：支持 AssetBundle 和 Resources 模式切换
        /// - 调试选项管理：支持调试模式和模拟模式的切换
        /// - 资源路径配置：支持配置内置、本地和远端资源路径
        /// - 可视化配置界面：在 Unity 编辑器中提供直观的设置面板
        ///
        /// 使用手册
        /// 1. 运行模式
        ///    - 配置项：Bundle 模式
        ///      配置键：`Asset/BundleMode`
        ///      默认值：`true`
        ///      功能说明：控制是否启用 AssetBundle 模式，启用后将从打包的资源文件加载资源
        ///
        ///    - 配置项：引用计数模式
        ///      配置键：`Asset/ReferMode`
        ///      默认值：`true`
        ///      功能说明：控制是否启用引用计数模式，启用后会自动跟踪资源引用，确保资源正确释放
        ///
        /// 2. 调试选项
        ///    - 配置项：调试模式
        ///      配置键：`Asset/DebugMode`
        ///      默认值：`false`
        ///      功能说明：控制是否启用调试模式，启用后会输出详细的资源加载和释放日志
        ///
        ///    - 配置项：模拟模式
        ///      配置键：`Asset/SimulateMode`
        ///      默认值：`false`
        ///      功能说明：控制是否启用模拟模式，仅在编辑器中可用，模拟 AssetBundle 的资源加载行为
        ///
        /// 3. 资源路径
        ///    - 配置项：内置资源路径
        ///      配置键：`Asset/AssetUri`
        ///      默认值：`Patch@Assets.zip`
        ///      功能说明：设置资源包的内置路径，用于打包时的处理
        ///
        ///    - 配置项：本地资源路径
        ///      配置键：`Asset/LocalUri`
        ///      默认值：`Assets`
        ///      功能说明：设置资源包的本地路径，用于运行时的加载
        ///
        ///    - 配置项：远端资源路径
        ///      配置键：`Asset/RemoteUri`
        ///      默认值：`${Prefs.Update/PatchUri}/Assets`
        ///      功能说明：设置资源包的远端路径，用于运行时的下载
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        public partial class Prefs : XPrefs.IPanel
        {
            /// <summary>
            /// Bundle 模式开关的配置键。
            /// 启用后将从打包的资源文件加载资源，否则使用Resources加载。
            /// </summary>
            public const string BundleMode = "Asset/BundleMode";

            /// <summary>
            /// Bundle模式的默认值，默认开启以支持资源打包加载
            /// </summary>
            public const bool BundleModeDefault = true;

            /// <summary>
            /// 引用计数模式的配置键。
            /// 启用后会自动跟踪资源引用，确保资源正确释放。
            /// </summary>
            public const string ReferMode = "Asset/ReferMode";

            /// <summary>
            /// 引用计数模式的默认值，默认开启以防止资源泄漏
            /// </summary>
            public const bool ReferModeDefault = true;

            /// <summary>
            /// 调试模式的配置键。
            /// 启用后会输出详细的资源加载和释放日志。
            /// </summary>
            public const string DebugMode = "Asset/DebugMode";

            /// <summary>
            /// 编辑器模拟模式的配置键。
            /// 在编辑器中可以模拟Bundle模式的资源加载，方便测试。
            /// </summary>
            public const string SimulateMode = "Asset/SimulateMode@Editor";

            /// <summary>
            /// 资源包文件名的配置键。
            /// 用于指定打包后的资源文件名称。
            /// </summary>
            public const string AssetUri = "Asset/AssetUri";

            /// <summary>
            /// 资源包的默认文件名
            /// </summary>
            public const string AssetUriDefault = "Patch@Assets.zip";

            /// <summary>
            /// 本地资源路径的配置键。
            /// 指定资源文件在本地存储的相对路径。
            /// </summary>
            public const string LocalUri = "Asset/LocalUri";

            /// <summary>
            /// 本地资源的默认存储路径
            /// </summary>
            public const string LocalUriDefault = "Assets";

            /// <summary>
            /// 远程资源地址的配置键。
            /// 用于指定资源更新的远程服务器地址。
            /// </summary>
            public const string RemoteUri = "Asset/RemoteUri";

            /// <summary>
            /// 远程资源的默认下载地址，支持变量求值。
            /// </summary>
            public const string RemoteUriDefault = "${Prefs.Update/PatchUri}/Assets";

#if UNITY_EDITOR
            /// <summary>
            /// 在配置面板中的分类名称
            /// </summary>
            public override string Section => "Asset";

            /// <summary>
            /// 在配置面板中的工具提示。
            /// </summary>
            public override string Tooltip => "Preferences of Asset.";

            /// <summary>
            /// 在配置面板中的显示顺序。
            /// </summary>
            public override int Priority => 100;

            /// <summary>
            /// 控制配置面板中分组的展开/折叠状态
            /// </summary>
            [SerializeField] protected bool foldout;

            /// <summary>
            /// 绘制配置界面，提供资源系统各项设置的可视化编辑功能。
            /// 包括运行模式切换、调试选项设置、资源路径配置等。
            /// 当Bundle模式关闭时，相关选项会自动置灰。
            /// </summary>
            /// <param name="searchContext">用于过滤配置项的搜索文本</param>
            public override void OnVisualize(string searchContext)
            {
                var bundleMode = Target.GetBool(BundleMode, BundleModeDefault);

                UnityEditor.EditorGUILayout.BeginVertical(UnityEditor.EditorStyles.helpBox);
                UnityEditor.EditorGUILayout.BeginHorizontal();
                Title("Bundle", "Switch to AssetBundle/Resources Mode.");
                bundleMode = UnityEditor.EditorGUILayout.Toggle(bundleMode);
                Target.Set(BundleMode, bundleMode);

                var ocolor = GUI.color;
                if (!bundleMode) GUI.color = Color.gray;

                Title("Refer", "Auto Manage References.");
                var referMode = UnityEditor.EditorGUILayout.Toggle(Target.GetBool(ReferMode, ReferModeDefault));
                if (bundleMode) Target.Set(ReferMode, referMode);
                UnityEditor.EditorGUILayout.EndHorizontal();

                UnityEditor.EditorGUILayout.BeginHorizontal();
                Title("Debug", "Switch to Debug/Release Mode.");
                var debugMode = UnityEditor.EditorGUILayout.Toggle(Target.GetBool(DebugMode));
                if (bundleMode) Target.Set(DebugMode, debugMode);

                Title("Simulate", "Simulate to Load AssetBundle.");
                var simulateMode = UnityEditor.EditorGUILayout.Toggle(Target.GetBool(SimulateMode));
                if (bundleMode) Target.Set(SimulateMode, simulateMode);
                UnityEditor.EditorGUILayout.EndHorizontal();

                UnityEditor.EditorGUILayout.BeginHorizontal();
                Title("Asset", "Asset Uri of Assets.");
                var assetFile = UnityEditor.EditorGUILayout.TextField("", Target.GetString(AssetUri, AssetUriDefault));
                if (bundleMode) Target.Set(AssetUri, assetFile);
                UnityEditor.EditorGUILayout.EndHorizontal();

                UnityEditor.EditorGUILayout.BeginHorizontal();
                Title("Local", "Local Uri of Assets.");
                var localPath = UnityEditor.EditorGUILayout.TextField("", Target.GetString(LocalUri, LocalUriDefault));
                if (bundleMode) Target.Set(LocalUri, localPath);
                UnityEditor.EditorGUILayout.EndHorizontal();

                UnityEditor.EditorGUILayout.BeginHorizontal();
                Title("Remote", "Remote Uri of Assets.");
                var remoteUri = UnityEditor.EditorGUILayout.TextField("", Target.GetString(RemoteUri, RemoteUriDefault));
                if (bundleMode) Target.Set(RemoteUri, remoteUri);
                UnityEditor.EditorGUILayout.EndHorizontal();

                GUI.color = ocolor;
                UnityEditor.EditorGUILayout.EndVertical();
            }
#endif
        }
    }
}
