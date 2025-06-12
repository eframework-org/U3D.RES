// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using EFramework.Editor;
using EFramework.Utility;

namespace EFramework.Asset.Editor
{
    public partial class XAsset
    {
        /// <summary>
        /// XAsset.Publish 实现了资源包的发布工作流，用于将打包好的资源发布至对象存储服务（OSS）中。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 首选项配置：提供首选项配置以自定义发布流程
        /// - 自动化流程：提供资源包发布任务的自动化执行
        /// 
        /// 使用手册
        /// 1. 首选项配置
        /// 
        /// 配置项说明：
        /// - 主机地址：`Asset/Publish/Host@Editor`，默认值为 `${Env.OssHost}`
        /// - 存储桶名：`Asset/Publish/Bucket@Editor`，默认值为 `${Env.OssBucket}`
        /// - 访问密钥：`Asset/Publish/Access@Editor`，默认值为 `${Env.OssAccess}`
        /// - 秘密密钥：`Asset/Publish/Secret@Editor`，默认值为 `${Env.OssSecret}`
        /// 
        /// 2. 自动化流程
        /// 
        /// 发布流程：
        /// - 读取发布配置 --> 获取远端清单 --> 对比本地清单 --> 发布差异文件
        /// 
        /// 发布规则：
        /// - 新增文件：`文件名@MD5`
        /// - 修改文件：`文件名@MD5`
        /// - 清单文件：`Manifest.db` 和 `Manifest.db@yyyy-MM-dd_HH-mm-ss`（用于版本回退）
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        [XEditor.Tasks.Worker(name: "Publish Assets", group: "Asset", runasync: true, priority: 102)]
        public class Publish : XEditor.Oss
        {
            /// <summary>
            /// 发布流程首选项设置类，包含 OSS 发布相关的配置选项。
            /// </summary>
            public class Prefs : Build.Prefs
            {
                /// <summary>
                /// OSS 主机的键名。
                /// </summary>
                public const string Host = "Asset/Publish/Host@Editor";

                /// <summary>
                /// OSS 主机的默认值。
                /// </summary>
                public const string HostDefault = "${Env.OssHost}";

                /// <summary>
                /// OSS 存储桶的键名。
                /// </summary>
                public const string Bucket = "Asset/Publish/Bucket@Editor";

                /// <summary>
                /// OSS 存储桶的默认值。
                /// </summary>
                public const string BucketDefault = "${Env.OssBucket}";

                /// <summary>
                /// OSS 访问密钥的键名。
                /// </summary>
                public const string Access = "Asset/Publish/Access@Editor";

                /// <summary>
                /// OSS 访问密钥的默认值。
                /// </summary>
                public const string AccessDefault = "${Env.OssAccess}";

                /// <summary>
                /// OSS 秘密密钥的键名。
                /// </summary>
                public const string Secret = "Asset/Publish/Secret@Editor";

                /// <summary>
                /// OSS 秘密密钥的默认值。
                /// </summary>
                public const string SecretDefault = "${Env.OssSecret}";

                /// <summary>
                /// 获取当前节的名称。
                /// </summary>
                public override string Section => "Asset";

                /// <summary>
                /// 获取当前优先级。
                /// </summary>
                public override int Priority => 102;

                /// <summary>
                /// 初始化 <see cref="Prefs"/> 类的新实例。
                /// </summary>
                public Prefs() { foldout = false; }

                /// <summary>
                /// 可视化设置界面。
                /// </summary>
                /// <remarks>
                /// <code>
                /// 界面元素：
                /// - Publish：发布选项折叠面板
                /// - Host：OSS 主机地址输入框
                /// - Bucket：存储桶名称输入框
                /// - Access：访问密钥输入框
                /// - Secret：秘密密钥输入框
                /// 
                /// 注意事项：
                /// - 仅在 Bundle 模式下可用
                /// - 使用环境变量支持配置注入
                /// - 支持运行时动态配置
                /// </code>
                /// </remarks>
                /// <param name="searchContext">搜索上下文</param>
                public override void OnVisualize(string searchContext)
                {
                    var bundleMode = Target.GetBool(BundleMode, BundleModeDefault);

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    var ocolor = GUI.color;
                    if (!bundleMode) GUI.color = Color.gray;
                    foldout = EditorGUILayout.Foldout(foldout, new GUIContent("Publish", "Assets Publish Options."));
                    if (foldout && bundleMode)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.BeginHorizontal();
                        Title("Host", "Oss Host Name");
                        Target.Set(Host, EditorGUILayout.TextField("", Target.GetString(Host, HostDefault)));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        Title("Bucket", "Oss Bucket Name");
                        Target.Set(Bucket, EditorGUILayout.TextField("", Target.GetString(Bucket, BucketDefault)));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        Title("Access", "Oss Access Key");
                        Target.Set(Access, EditorGUILayout.TextField("", Target.GetString(Access, AccessDefault)));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        Title("Secret", "Oss Secret Key");
                        Target.Set(Secret, EditorGUILayout.TextField("", Target.GetString(Secret, SecretDefault)));
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                    }
                    GUI.color = ocolor;
                    EditorGUILayout.EndVertical();
                }
            }

            /// <summary>
            /// 处理发布前的预处理逻辑。
            /// </summary>
            /// <remarks>
            /// <code>
            /// 处理流程：
            /// 1. 解析环境变量
            /// 2. 设置 OSS 连接参数
            /// 3. 配置本地和远程路径
            /// 
            /// 注意事项：
            /// - 支持环境变量注入
            /// - 自动处理路径配置
            /// - 继承基类预处理逻辑
            /// </code>
            /// </remarks>
            /// <param name="report">构建报告对象，用于记录处理过程中的信息</param>
            public override void Preprocess(XEditor.Tasks.Report report)
            {
                Host = XPrefs.GetString(Prefs.Host, Prefs.HostDefault).Eval(XPrefs.Asset, XEnv.Vars);
                Bucket = XPrefs.GetString(Prefs.Bucket, Prefs.BucketDefault).Eval(XPrefs.Asset, XEnv.Vars);
                Access = XPrefs.GetString(Prefs.Access, Prefs.AccessDefault).Eval(XPrefs.Asset, XEnv.Vars);
                Secret = XPrefs.GetString(Prefs.Secret, Prefs.SecretDefault).Eval(XPrefs.Asset, XEnv.Vars);
                base.Preprocess(report);
                Local = XFile.PathJoin(Temp, XPrefs.GetString(Prefs.LocalUri, Prefs.LocalUriDefault));
                Remote = XPrefs.GetString(Prefs.RemoteUri, Prefs.RemoteUriDefault).Eval(XPrefs.Asset, XEnv.Vars);
            }

            /// <summary>
            /// 处理发布过程中的逻辑。
            /// </summary>
            /// <remarks>
            /// <code>
            /// 处理流程：
            /// 1. 获取本地资源清单
            /// 2. 获取远程资源清单
            /// 3. 比较资源差异
            /// 4. 复制更新的资源
            /// 5. 执行资源发布
            /// 
            /// 注意事项：
            /// - 支持增量发布
            /// - 自动备份清单文件
            /// - 处理资源版本冲突
            /// - 保留历史版本记录
            /// </code>
            /// </remarks>
            /// <param name="report">构建报告对象，用于记录处理过程中的信息</param>
            public override void Process(XEditor.Tasks.Report report)
            {
                var root = XFile.NormalizePath(XPrefs.GetString(Prefs.Output, Prefs.OutputDefault).Eval(XEnv.Vars));

                var remoteMani = new XMani.Manifest();
                var tempFile = Path.GetTempFileName();
                var task = XEditor.Cmd.Run(bin: Bin, args: new string[] { "get", $"\"{Alias}/{Bucket}/{Remote}/{XMani.Default}\"", tempFile });
                task.Wait();
                if (task.Result.Code != 0)
                {
                    XLog.Warn("XAsset.Publish.Process: get remote mainifest failed: {0}", task.Result.Error);
                }
                else
                {
                    remoteMani.Read(tempFile);
                    if (!string.IsNullOrEmpty(remoteMani.Error)) XLog.Warn("XAsset.Publish.Process: parse remote mainifest failed: {0}", remoteMani.Error);
                }

                var localMani = new XMani.Manifest();
                localMani.Read(XFile.PathJoin(root, XMani.Default));
                if (!string.IsNullOrEmpty(localMani.Error)) XLog.Warn("XAsset.Publish.Process: parse local mainifest failed: {0}", remoteMani.Error);
                else
                {
                    var diff = remoteMani.Compare(localMani);
                    var files = new List<string[]>();
                    for (var i = 0; i < diff.Added.Count; i++) { files.Add(new string[] { XFile.PathJoin(root, diff.Added[i].Name), diff.Added[i].MD5 }); }
                    for (var i = 0; i < diff.Modified.Count; i++) { files.Add(new string[] { XFile.PathJoin(root, diff.Modified[i].Name), diff.Modified[i].MD5 }); }
                    if (diff.Added.Count > 0 || diff.Modified.Count > 0)
                    {
                        var maniFile = XFile.PathJoin(root, XMani.Default);
                        files.Add(new string[] { maniFile, "" });
                        files.Add(new string[] { maniFile, XTime.Format(XTime.GetTimestamp(), "yyyy-MM-dd_HH-mm-ss") });
                    }
                    if (files.Count == 0)
                    {
                        XLog.Debug("XAsset.Publish.Process: diff files is zero, no need to publish.");
                        return;
                    }
                    else
                    {
                        foreach (var kvp in files)
                        {
                            var file = kvp[0];
                            var md5 = kvp[1];
                            var src = file;
                            var dst = XFile.PathJoin(Local, Path.GetRelativePath(root, file));
                            if (string.IsNullOrEmpty(md5) == false) dst += "@" + md5; // file@md5
                            var dir = Path.GetDirectoryName(dst);
                            if (XFile.HasDirectory(dir) == false) XFile.CreateDirectory(dir);
                            XFile.CopyFile(src, dst);
                        }
                    }

                    base.Process(report);
                }
            }
        }
    }
}
