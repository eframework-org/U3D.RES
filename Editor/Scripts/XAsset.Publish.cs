// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
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
        public class Publish : XEditor.Oss, XEditor.Tasks.Panel.IOnGUI
        {
            /// <summary>
            /// Prefs 是发布流程首选项设置类，包含 OSS 发布相关的配置选项。
            /// </summary>
            public class Prefs : Build.Prefs
            {
                /// <summary>
                /// Host 是 OSS 主机的键名。
                /// </summary>
                public const string Host = "Asset/Publish/Host@Editor";

                /// <summary>
                /// HostDefault 是 OSS 主机的默认值。
                /// </summary>
                public const string HostDefault = "${Env.OssHost}";

                /// <summary>
                /// Bucket 是 OSS 存储桶的键名。
                /// </summary>
                public const string Bucket = "Asset/Publish/Bucket@Editor";

                /// <summary>
                /// BucketDefault 是 OSS 存储桶的默认值。
                /// </summary>
                public const string BucketDefault = "${Env.OssBucket}";

                /// <summary>
                /// Access 是 OSS 访问密钥的键名。
                /// </summary>
                public const string Access = "Asset/Publish/Access@Editor";

                /// <summary>
                /// AccessDefault 是 OSS 访问密钥的默认值。
                /// </summary>
                public const string AccessDefault = "${Env.OssAccess}";

                /// <summary>
                /// Secret 是 OSS 秘密密钥的键名。
                /// </summary>
                public const string Secret = "Asset/Publish/Secret@Editor";

                /// <summary>
                /// SecretDefault 是 OSS 秘密密钥的默认值。
                /// </summary>
                public const string SecretDefault = "${Env.OssSecret}";

                /// <summary>
                /// Section 获取面板章节的名称。
                /// </summary>
                public override string Section => "Asset";

                /// <summary>
                /// Priority 获取面板显示的优先级。
                /// </summary>
                public override int Priority => 102;

                /// <summary>
                /// 初始化 <see cref="Prefs"/> 类的新实例。
                /// </summary>
                public Prefs() { foldout = false; }

                /// <summary>
                /// serialized 是可序列化的对象。
                /// </summary>
                [NonSerialized] SerializedObject serialized;

                /// <summary>
                /// OnVisualize 绘制可视化界面。
                /// </summary>
                /// <param name="searchContext">搜索上下文</param>
                public override void OnVisualize(string searchContext)
                {
                    var taskPanel = searchContext == "Task Runner";
                    serialized ??= new SerializedObject(this);
                    serialized.Update();

                    var ocolor = GUI.color;
                    var bundleMode = Target.GetBool(BundleMode, BundleModeDefault);
                    if (!taskPanel)
                    {
                        if (!bundleMode) GUI.color = Color.gray;
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        foldout = EditorGUILayout.Foldout(foldout, new GUIContent("Publish", "Assets Publish Options."));
                    }
                    else foldout = true;
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
                    else if (foldout && !bundleMode) EditorGUILayout.HelpBox("Bundle Mode is Disabled.", MessageType.None);
                    GUI.color = ocolor;

                    if (!taskPanel) EditorGUILayout.EndVertical();
                }
            }

            internal Prefs prefsPanel;
            void XEditor.Tasks.Panel.IOnGUI.OnGUI()
            {
                if (prefsPanel == null)
                {
                    prefsPanel = ScriptableObject.CreateInstance<Prefs>();
                    prefsPanel.Target = XPrefs.Asset;
                }
                prefsPanel.OnVisualize("Task Runner");
            }

            /// <summary>
            /// Preprocess 处理发布前的预处理逻辑。
            /// </summary>
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
            /// Process 处理发布过程中的逻辑。
            /// </summary>
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
