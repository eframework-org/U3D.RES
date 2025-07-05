// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.FileSystemGlobbing;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;
using EFramework.Editor;
using EFramework.Utility;
using Const = EFramework.Asset.XAsset.Const;

namespace EFramework.Asset.Editor
{
    public partial class XAsset
    {
        /// <summary>
        /// XAsset.Build 提供了资源的构建工作流，支持资源的依赖分析及打包功能。
        /// </summary>
        /// <remarks>
        /// <code>
        /// 功能特性
        /// - 首选项配置：提供首选项配置以自定义构建流程
        /// - 自动化流程：提供资源包构建任务的自动化执行
        /// 
        /// 使用手册
        /// 1. 首选项配置
        /// 
        /// 配置项说明：
        /// - 输出路径：`Asset/Build/Output@Editor`，默认值为 `Builds/Patch/${Env.Platform}/Assets`
        /// - 包含路径：`Asset/Build/Include@Editor`，默认值为 `["Assets/Resources/Bundle", "Assets/Scenes/**/*.unity"]`
        /// - 排除路径：`Asset/Build/Exclude@Editor`，默认值为 `[]`
        /// - 暂存路径：`Asset/Build/Stash@Editor`，默认值为 `["Assets/Resources/Bundle"]`
        /// - 合并材质：`Asset/Build/Merge/Material@Editor`，默认值为 `true`
        /// - 合并单包：`Asset/Build/Merge/Single@Editor`，默认值为 `false`
        /// - 拷贝资源：`Asset/Build/Streaming/Assets@Editor`，默认值为 `true`
        /// 
        /// 2. 自动化流程
        /// 
        /// 构建流程：
        /// - 分析依赖 --> 打包资源 --> 生成清单
        /// 
        /// 构建产物：
        /// - `*.bundle`：资源包文件，格式为 `path_to_assets.bundle`
        /// - `Manifest.db`：资源包清单，格式为 `名称|MD5|大小`
        /// 
        /// 注意事项：
        /// - 场景文件(.unity)需要单独打包
        /// - 支持自定义 Bundle 名称
        /// - 可配置材质和单资源的合并策略
        /// </code>
        /// 更多信息请参考模块文档。
        /// </remarks>
        [XEditor.Tasks.Worker(name: "Build Assets", group: "Asset", priority: 101)]
        public class Build : XEditor.Tasks.Worker,
            XEditor.Tasks.Panel.IOnGUI,
            XEditor.Event.Internal.OnPreprocessBuild,
            XEditor.Event.Internal.OnPostprocessBuild
        {
            /// <summary>
            /// Prefs 是构建的配置管理器，提供资源打包相关的配置项和界面化设置功能。
            /// 包含输出路径、资源包含/排除规则、资源暂存设置以及合并选项等配置。
            /// </summary>
            public class Prefs : Asset.XAsset.Prefs
            {
                // 输出路径配置
                public const string Output = "Asset/Build/Output@Editor";
                public const string OutputDefault = "Builds/Patch/${Env.Platform}/Assets";

                // 资源路径配置
                public const string Include = "Asset/Build/Include@Editor";
                public static readonly string[] IncludeDefault = new string[] { "Assets/Resources/Bundle", "Assets/Scenes/**/*.unity" };
                public const string Exclude = "Asset/Build/Exclude@Editor";
                public const string Stash = "Asset/Build/Stash@Editor";
                public static readonly string[] StashDefault = new string[] { "Assets/Resources/Bundle" };

                // 资源合并配置
                public const string MergeMaterial = "Asset/Build/Merge/Material@Editor";
                public const bool MergeMaterialDefault = true;
                public const string MergeSingle = "Asset/Build/Merge/Single@Editor";
                public const bool MergeSingleDefault = false;

                // 资源拷贝配置
                public const string StreamingAssets = "Asset/Build/Streaming/Assets";
                public const bool StreamingAssetsDefault = true;

                public override string Section => "Asset";
                public override int Priority => 101;

                [SerializeField] internal string[] include;
                [SerializeField] internal string[] exclude;
                [SerializeField] internal string[] stash;
                [NonSerialized] SerializedObject serialized;

                public Prefs() { foldout = false; }

                /// <summary>
                /// OnVisualize 在编辑器中绘制配置界面，方便用户可视化管理构建设置。
                /// 提供输出路径、打包选项、资源规则等配置的编辑功能。
                /// </summary>
                /// <param name="searchContext">搜索上下文。</param>
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
                        foldout = EditorGUILayout.Foldout(foldout, new GUIContent("Build", "Assets Build Options."));
                    }
                    else foldout = true;
                    if (foldout && bundleMode)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        EditorGUILayout.BeginHorizontal();
                        Title("Output", "Output Path of AssetBundle.");
                        Target.Set(Output, EditorGUILayout.TextField("", Target.GetString(Output, OutputDefault)));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        Title("Options");

                        Title("Material", "Merge Material into Bundle for Collecting Shader Variants.");
                        Target.Set(MergeMaterial, EditorGUILayout.Toggle(Target.GetBool(MergeMaterial, MergeMaterialDefault)));

                        Title("Single", "Merge Single Raw Bundle into Main Bundle.");
                        Target.Set(MergeSingle, EditorGUILayout.Toggle(Target.GetBool(MergeSingle, MergeSingleDefault)));

                        Title("Streaming", "Copy Assets Patch into Streaming Assets.", 70);
                        Target.Set(StreamingAssets, EditorGUILayout.Toggle(Target.GetBool(StreamingAssets, StreamingAssetsDefault)));
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        include = Target.GetStrings(Include, IncludeDefault);
                        EditorGUILayout.PropertyField(serialized.FindProperty("include"), new GUIContent("Include"));
                        if (GUILayout.Button(new GUIContent("?", "Learn more about File Globbing"), GUILayout.Width(20))) Application.OpenURL("https://learn.microsoft.com/zh-cn/dotnet/core/extensions/file-globbing");
                        if (serialized.ApplyModifiedProperties()) Target.Set(Include, include);
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        exclude = Target.GetStrings(Exclude, new string[] { });
                        EditorGUILayout.PropertyField(serialized.FindProperty("exclude"), new GUIContent("Exclude"));
                        if (GUILayout.Button(new GUIContent("?", "Learn more about File Globbing"), GUILayout.Width(20))) Application.OpenURL("https://learn.microsoft.com/zh-cn/dotnet/core/extensions/file-globbing");
                        if (serialized.ApplyModifiedProperties()) Target.Set(Exclude, exclude);
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        stash = Target.GetStrings(Stash, StashDefault);
                        EditorGUILayout.PropertyField(serialized.FindProperty("stash"), new GUIContent("Stash"));
                        if (serialized.ApplyModifiedProperties()) Target.Set(Stash, stash);
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.EndVertical();
                    }
                    else if (foldout && !bundleMode) EditorGUILayout.HelpBox("Bundle Mode is Disabled.", MessageType.None);
                    GUI.color = ocolor;

                    if (!taskPanel) EditorGUILayout.EndVertical();
                }
            }

            internal static string stashFile { get => XFile.PathJoin(XEnv.ProjectPath, "Library", "AssetStash.db"); }
            internal static string dependencyFile { get => XFile.PathJoin(XEnv.ProjectPath, "Library", "AssetDependency.db"); }
            internal string buildDir;

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
            /// Preprocess 构建前的准备工作，包括创建输出目录和备份当前资源清单。
            /// </summary>
            /// <param name="report">构建报告对象</param>
            public override void Preprocess(XEditor.Tasks.Report report)
            {
                buildDir = XFile.NormalizePath(XPrefs.GetString(Prefs.Output, Prefs.OutputDefault).Eval(XEnv.Vars));
                if (string.IsNullOrEmpty(buildDir)) throw new ArgumentNullException("Prefs.Build.Output is empty.");
                if (!XFile.HasDirectory(buildDir)) XFile.CreateDirectory(buildDir);

                var maniFile = XFile.PathJoin(buildDir, XMani.Default);
                var tmpManiFile = maniFile + ".tmp";
                if (XFile.HasFile(maniFile)) XFile.CopyFile(maniFile, tmpManiFile);
            }

            /// <summary>
            /// Process 执行资源构建过程，分析依赖关系并生成 AssetBundle 文件。
            /// </summary>
            /// <param name="report">构建报告对象</param>
            public override void Process(XEditor.Tasks.Report report)
            {
                var bundles = GenDependency();
                var builds = new List<AssetBundleBuild>();
                foreach (var kvp in bundles)
                {
                    var build = new AssetBundleBuild
                    {
                        assetBundleName = kvp.Key,
                        assetNames = kvp.Value.ToArray()
                    };
                    builds.Add(build);
                }

                try
                {
                    if (BuildPipeline.BuildAssetBundles(buildDir, builds.ToArray(),
                        BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.AssetBundleStripUnityVersion, EditorUserBuildSettings.activeBuildTarget) == null)
                    {
                        report.Error = "BuildPipeline.BuildAssetBundles returns nil.";
                    }
                }
                catch (Exception e) { XLog.Panic(e); report.Error = e.Message; }
            }

            /// <summary>
            /// Postprocess 构建后的收尾工作，生成新的资源清单并输出构建报告。
            /// </summary>
            /// <param name="report">构建报告对象</param>
            public override void Postprocess(XEditor.Tasks.Report report)
            {
                GenManifest(report);
                GenSummary(report);
            }

            /// <summary>
            /// GenDependency 分析项目资源并生成依赖关系图，支持自定义打包规则和资源合并策略。
            /// 会处理场景文件、材质球等特殊资源，确保正确的打包顺序和依赖关系。
            /// </summary>
            /// <returns>资源依赖关系字典，键为Bundle名称，值为资源路径列表</returns>
            public static Dictionary<string, List<string>> GenDependency()
            {
                var buildBundles = new Dictionary<string, List<string>>();
                var buildTime = XTime.GetTimestamp();

                try
                {
                    var visited = new List<string>();
                    var fileBundles = new Dictionary<string, List<string>>();
                    var dirBundles = new Dictionary<string, List<string>>();
                    var customBundles = new Dictionary<string, List<string>>();
                    var refCountMap = new Dictionary<string, int>();
                    var sourceAssets = new List<string>();

                    // 处理 Include 规则
                    var includes = XPrefs.GetStrings(Prefs.Include, Prefs.IncludeDefault);
                    if (includes != null && includes.Length > 0)
                    {
                        foreach (var temp in includes)
                        {
                            if (temp.IndexOfAny(new char[] { '*', '?', '[' }) >= 0)
                            {
                                var tempAssets = new List<string>();
                                var rootDir = temp.Split('*', '?', '[')[0].TrimEnd('/', '\\');
                                var partten = temp[rootDir.Length..].TrimStart('/', '\\');

                                if (string.IsNullOrEmpty(rootDir)) rootDir = "Assets";
                                XEditor.Utility.CollectAssets(rootDir, tempAssets, ".cs", ".js", ".meta", ".tpsheet", ".DS_Store", ".gitkeep", ".variant", ".hlsl", ".cginc", ".shadersubgraph");

                                var matcher = new Matcher();
                                matcher.AddInclude(partten);

                                foreach (var asset in tempAssets)
                                {
                                    var relativeAsset = XFile.NormalizePath(Path.GetRelativePath(rootDir, asset));
                                    if (matcher.Match(relativeAsset).HasMatches) sourceAssets.Add(asset);
                                }
                            }
                            else if (XFile.HasFile(temp)) sourceAssets.Add(temp);
                            else if (XFile.HasDirectory(temp))
                            {
                                XEditor.Utility.CollectAssets(temp, sourceAssets, ".cs", ".js", ".meta", ".tpsheet", ".DS_Store", ".gitkeep", ".variant", ".hlsl", ".cginc", ".shadersubgraph");
                            }
                        }
                    }

                    // 处理 Exclude 规则
                    var excludes = XPrefs.GetStrings(Prefs.Exclude);
                    if (excludes != null && excludes.Length > 0)
                    {
                        for (var i = 0; i < sourceAssets.Count;)
                        {
                            var asset = sourceAssets[i];
                            var remove = false;

                            foreach (var exclude in excludes)
                            {
                                if (exclude.IndexOfAny(new char[] { '*', '?', '[' }) >= 0)
                                {
                                    var rootDir = exclude.Split('*', '?', '[')[0];
                                    if (asset.StartsWith(rootDir)) // 判断文件是否匹配根目录
                                    {
                                        var partten = exclude[rootDir.Length..];
                                        var matcher = new Matcher();
                                        matcher.AddInclude(partten);

                                        var relativeAsset = XFile.NormalizePath(Path.GetRelativePath(rootDir, asset));
                                        if (matcher.Match(relativeAsset).HasMatches)
                                        {
                                            remove = true;
                                            break;
                                        }
                                    }
                                }
                                else if (exclude == asset)  // 文件路径完全匹配
                                {
                                    remove = true;
                                    break;
                                }
                            }

                            if (remove)
                            {
                                sourceAssets.RemoveAt(i);
                                XLog.Debug("XAsset.Build.GenDependency: {0} has been ignored by matcher.", asset);
                            }
                            else i++;
                        }
                    }

                    var dependAssets = XEditor.Utility.CollectDependency(sourceAssets);
                    for (int i = 0; i < sourceAssets.Count; i++)
                    {
                        var asset = sourceAssets[i];
                        visited.Add(asset);
                        var assetImporter = AssetImporter.GetAtPath(asset);
                        if (assetImporter)
                        {
                            var bundleName = Const.GetName(asset.StartsWith("Assets/") ? asset["Assets/".Length..] : asset);
                            if (bundleName.Contains(".unity")) bundleName = bundleName.Replace(".unity", "_unity"); // 场景文件只能单独打包
                            else bundleName = bundleName.Replace(Path.GetExtension(asset), "");
                            if (!fileBundles.TryGetValue(bundleName, out var deps)) { deps = new List<string>(); fileBundles.Add(bundleName, deps); }
                            if (!deps.Contains(asset)) deps.Add(asset);
                        }
                    }

                    var keys = dependAssets.Keys.ToList(); // Prefer to process scene to elimate material deps.
                    keys.Sort((a1, a2) =>
                    {
                        var b1 = a1.EndsWith(".unity") ? 0 : 1;
                        var b2 = a2.EndsWith(".unity") ? 0 : 1;
                        if (b1 > b2) return 1;
                        else if (b1 == b2) return 0;
                        else return -1;
                    });
                    foreach (var key in keys)
                    {
                        var assets = dependAssets[key];
                        for (int j = 0; j < assets.Count; j++)
                        {
                            var asset = assets[j];
                            if (asset.EndsWith(".hlsl") || asset.EndsWith(".cginc") || asset.EndsWith(".shadersubgraph")) continue;
                            if (visited.Contains(asset) == false)
                            {
                                visited.Add(asset);
                                if (asset.Contains("Editor/"))
                                {
                                    XLog.Warn("XAsset.Build.GenDependency: ignore editor asset deps: {0}.", asset);
                                    continue;
                                }
                                else
                                {
                                    var temp = asset[..asset.LastIndexOf("/")];
                                    var assetImporter = AssetImporter.GetAtPath(asset);
                                    if (assetImporter)
                                    {
                                        string bundleName;
                                        List<string> deps;
                                        if (!string.IsNullOrEmpty(assetImporter.assetBundleName))
                                        {
                                            bundleName = assetImporter.assetBundleName;
                                            XLog.Debug("XAsset.Build.GenDependency: using custom bundle name: {0} for asset: {1}.", bundleName, asset);
                                            if (!customBundles.TryGetValue(bundleName, out deps)) { deps = new List<string>(); customBundles.Add(bundleName, deps); }
                                            if (!deps.Contains(asset)) deps.Add(asset);
                                        }
                                        else
                                        {
                                            var skip = assetImporter is ShaderImporter || asset.EndsWith(".shadergraph"); // 强制合并材质，避免着色器变体丢失
                                            if (!skip && XPrefs.GetBool(Prefs.MergeMaterial, Prefs.MergeMaterialDefault)) skip = asset.EndsWith(".mat") && key.EndsWith(".unity");
                                            if (skip) continue;
                                            else
                                            {
                                                var dir = XFile.NormalizePath(Path.GetDirectoryName(asset));
                                                bundleName = Const.GetName(dir.StartsWith("Assets/") ? dir["Assets/".Length..] : dir);
                                                if (!dirBundles.TryGetValue(bundleName, out deps)) { deps = new List<string>(); dirBundles.Add(bundleName, deps); }
                                                if (!deps.Contains(asset)) deps.Add(asset);
                                            }
                                        }
                                    }
                                }
                            }
                            if (!refCountMap.TryGetValue(asset, out var count)) refCountMap.Add(asset, count);
                            else refCountMap[asset] = count + 1;
                        }
                    }

                    if (XPrefs.GetBool(Prefs.MergeSingle, Prefs.MergeSingleDefault)) // 若业务层依赖depth为1（间接引用）的包，则会引起异常（如：fairygui.uipanel的bundle）
                    {
                        var deletes = new List<string>();
                        foreach (var kvp in dirBundles)     // 若引用的多个资源都只出现在该包中，也会合并
                        {
                            var sig = true;
                            foreach (var dep in kvp.Value)
                            {
                                if (refCountMap[dep] > 0) { sig = false; break; }
                            }
                            if (sig) deletes.Add(kvp.Key);
                        }

                        foreach (var k in deletes)
                        {
                            dirBundles.Remove(k);
                            XLog.Debug("XAsset.Build.GenDependency: merged raw single asset '{0}'.", k);
                        }
                    }
                    else XLog.Debug("XAsset.Build.GenDependency: ignore to merge raw single bundles.");

                    foreach (var kvp in fileBundles) buildBundles.Add(kvp.Key, kvp.Value);
                    foreach (var kvp in dirBundles) buildBundles.Add(kvp.Key, kvp.Value);
                    foreach (var kvp in customBundles) buildBundles.Add(kvp.Key, kvp.Value);

                    if (XFile.HasFile(dependencyFile)) XFile.DeleteFile(dependencyFile);
                    using var fs = File.Open(dependencyFile, FileMode.Create);
                    using var sw = new StreamWriter(fs);
                    foreach (var kvp in buildBundles)
                    {
                        sw.WriteLine($"bundle: {kvp.Key}");
                        var assets = kvp.Value;
                        for (var j = 0; j < assets.Count; j++)
                        {
                            var asset = assets[j];
                            if (asset.Contains("Editor/") == false && asset != kvp.Key) sw.WriteLine($"  asset: {asset}");
                        }
                    }
                    sw.Flush();
                    fs.Flush();
                }
                catch (Exception e)
                {
                    if (XFile.HasFile(dependencyFile)) XFile.DeleteFile(dependencyFile);
                    XLog.Panic(e);
                }
                XLog.Debug("XAsset.Build.GenDependency: generate <a href=\"file:///{0}\">{1}</a> done, elapsed {2}s.", Path.GetFullPath(dependencyFile), Path.GetRelativePath(XEnv.ProjectPath, dependencyFile), XTime.GetTimestamp() - buildTime);
                return buildBundles;
            }

            /// <summary>
            /// GenManifest 根据构建结果生成资源清单文件，记录每个资源包的信息（如MD5、大小等）。
            /// 这个清单文件将用于运行时的资源加载和版本检查。
            /// </summary>
            /// <param name="report">构建报告对象</param>
            private void GenManifest(XEditor.Tasks.Report report)
            {
                if (report.Result != XEditor.Tasks.Result.Succeeded) return;

                var abManifestFilePath = XFile.PathJoin(buildDir, Const.Manifest);
                var manifestFilePath = abManifestFilePath + ".manifest";
                var assetManifestFilePath = XFile.PathJoin(buildDir, XMani.Default);
                if (XFile.HasFile(assetManifestFilePath)) XFile.DeleteFile(assetManifestFilePath);
                if (XFile.HasFile(abManifestFilePath) == false)
                {
                    report.Error = "No asset bundle manifest file.";
                    XLog.Error("XAsset.Build.GenManifest: no asset bundle manifest file.");
                    return;
                }

                var bundle = AssetBundle.LoadFromFile(abManifestFilePath);
                var manifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                if (manifest == null)
                {
                    report.Error = "Null asset bundle manifest.";
                    XLog.Error("XAsset.Build.GenManifest: null asset bundle manifest.");
                    return;
                }

                var fs = new FileStream(assetManifestFilePath, FileMode.OpenOrCreate);
                var sw = new StreamWriter(fs);
                // write ab manifest file;
                var manifestMD5 = XFile.FileMD5(abManifestFilePath);
                var manifestSize = XFile.FileSize(abManifestFilePath);
                sw.WriteLine(Const.Manifest + "|" + manifestMD5 + "|" + manifestSize);
                var lines = File.ReadAllLines(manifestFilePath);
                var abs = new List<string>();
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.StartsWith("      Name: "))
                    {
                        line = line.Replace("      Name: ", "");
                        line = line.Trim();
                        abs.Add(line);
                    }
                }
                var abs2 = new List<string>();
                var count = 0;
                while (abs.Count > 0)
                {
                    for (var i = 0; i < abs.Count;)
                    {
                        var ab = abs[i];
                        var deps = manifest.GetAllDependencies(ab);
                        if (deps.Length == count)
                        {
                            abs.RemoveAt(i);
                            abs2.Add(ab);
                        }
                        else i++;
                    }
                    count++;
                }
                for (var i = 0; i < abs2.Count; i++)
                {
                    var ab = abs2[i];
                    var filePath = XFile.PathJoin(buildDir, ab);
                    var size = XFile.FileSize(filePath);
                    var md5 = XFile.FileMD5(filePath);
                    sw.WriteLine(ab + "|" + md5 + "|" + size);
                }
                sw.Close();
                fs.Close();
                bundle.Unload(true);
            }

            /// <summary>
            /// GenSummary 生成构建报告，记录资源变更情况并清理无效的资源文件。
            /// 通过对比新旧清单，可以了解此次构建的具体改动。
            /// </summary>
            /// <param name="report">构建报告对象，用于记录构建过程中的信息</param>
            private void GenSummary(XEditor.Tasks.Report report)
            {
                var tmpFile = XFile.PathJoin(buildDir, XMani.Default + ".tmp");
                var tmpMani = new XMani.Manifest(tmpFile);
                if (XFile.HasFile(tmpFile))
                {
                    tmpMani.Read();
                    XFile.DeleteFile(XFile.PathJoin(buildDir, XMani.Default + ".tmp"));
                }
                if (report.Result != XEditor.Tasks.Result.Succeeded) return;

                var maniFile = XFile.PathJoin(buildDir, XMani.Default);
                var mani = new XMani.Manifest(maniFile);
                mani.Read();

                var diff = tmpMani.Compare(mani);
                for (var i = 0; i < diff.Modified.Count; i++)
                {
                    var fi = diff.Modified[i];
                    XLog.Debug("XAsset.Build.GenSummary: {0} has been modified.", fi.Name);
                }

                for (var i = 0; i < diff.Added.Count; i++)
                {
                    var fi = diff.Added[i];
                    XLog.Debug("XAsset.Build.GenSummary: {0} has been added.", fi.Name);
                }

                for (var i = 0; i < diff.Deleted.Count; i++)
                {
                    var fi = diff.Deleted[i];
                    var file = XFile.PathJoin(buildDir, fi.Name);
                    var mfile = file + ".manifest";
                    XFile.DeleteFile(file);
                    XFile.DeleteFile(mfile);
                    XLog.Debug("XAsset.Build.GenSummary: {0} has been deleted.", fi.Name);
                }

                try
                {
                    var files = Directory.GetFiles(buildDir);
                    for (var i = 0; i < files.Length; i++)
                    {
                        var f = files[i];
                        var n = Path.GetFileName(f);
                        var e = Path.GetExtension(f);
                        if (n == XMani.Default || e == ".manifest") continue;
                        if (mani.Files.Find((e) => { return e.Name == n; }) == null)
                        {
                            XFile.DeleteFile(f);
                            XLog.Warn("XAsset.Build.GenSummary: invalid {0} has been deleted.", f);
                        }
                    }
                }
                catch (Exception e) { XLog.Panic(e); }

                var dirty = false;
                foreach (var fi in mani.Files)
                {
                    if (GenOffset(fi)) dirty = true;
                }
                if (dirty) XFile.SaveText(maniFile, mani.ToString());

                XLog.Debug("XAsset.Build.GenSummary: {0} asset(s) has been modified, {1} asset(s) has been added, {2} asset(s) has been deleted.", diff.Modified.Count, diff.Added.Count, diff.Deleted.Count);
            }

            /// <summary>
            /// GenOffset 用于处理 Bundle 文件的偏移。
            /// </summary>
            /// <param name="fi">Bundle 文件信息</param>
            private bool GenOffset(XMani.FileInfo fi)
            {
                var offsetFactor = XPrefs.GetInt(Prefs.OffsetFactor, Prefs.OffsetFactorDefault);
                if (offsetFactor <= 0) return false;

                var src = XFile.PathJoin(buildDir, fi.Name);
                var dst = src + ".tmp";
                if (XFile.HasFile(dst)) XFile.DeleteFile(dst);

                var prefix = new byte[28];
                // 根据文件名长度计算 offsetCount，且至少偏移一个单元
                var offsetCount = fi.Name.Length % offsetFactor + 1;

                using (var fs = new FileStream(src, FileMode.Open, FileAccess.Read))
                {
                    // 先读 prefix
                    var readPrefix = new byte[28];
                    int readLen = fs.Read(readPrefix, 0, readPrefix.Length);
                    if (readLen != prefix.Length) return false; // 文件太小，不处理

                    Buffer.BlockCopy(readPrefix, 0, prefix, 0, prefix.Length);

                    // 再读 offsetCount * prefix.Length 用于比对
                    var totalCheckSize = prefix.Length * offsetCount;
                    var checkData = new byte[totalCheckSize];
                    readLen = fs.Read(checkData, 0, totalCheckSize);

                    var alreadyOffset = true;
                    if (readLen < totalCheckSize) alreadyOffset = false;
                    else
                    {
                        for (int i = 0; i < offsetCount; i++)
                        {
                            for (int j = 0; j < prefix.Length; j++)
                            {
                                if (checkData[i * prefix.Length + j] != prefix[j])
                                {
                                    alreadyOffset = false;
                                    break;
                                }
                            }
                            if (!alreadyOffset) break;
                        }
                    }

                    if (alreadyOffset) return false;
                }

                // 如果未偏移则执行偏移写入
                var buffer = new byte[1024];
                using (var fs = new FileStream(src, FileMode.Open, FileAccess.Read))
                using (var sw = new FileStream(dst, FileMode.Create, FileAccess.Write))
                {
                    // 读取原始的 prefix
                    fs.Read(prefix, 0, prefix.Length);

                    // 写入原始的 prefix
                    sw.Write(prefix, 0, prefix.Length);

                    // 写入偏移的 prefix
                    for (var i = 0; i < offsetCount; i++) sw.Write(prefix, 0, prefix.Length);

                    int bytesRead;
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0) sw.Write(buffer, 0, bytesRead);

                    sw.Flush();
                }

                XFile.DeleteFile(src);
                Directory.Move(dst, src);
                XFile.DeleteFile(dst);
                fi.Size = XFile.FileSize(src);
                fi.MD5 = XFile.FileMD5(src);
                XLog.Debug("XAsset.Build.GenOffset: add {0} prefix offset into {1}.", offsetCount, fi.Name);
                return true;
            }

            /// <summary>
            /// XEditor.Event.Internal.OnPreprocessBuild.Process 是构建开始前的预处理，主要处理平台相关的资源复制工作。
            /// 对于移动平台，会将资源打包成 zip 文件以便于分发。
            /// </summary>
            /// <param name="args">构建参数数组</param>
            void XEditor.Event.Internal.OnPreprocessBuild.Process(params object[] args)
            {
                if (!XPrefs.GetBool(Prefs.BundleMode, Prefs.BundleModeDefault))
                {
                    XLog.Debug("XAsset.Build.OnPreprocessBuild: ignore to preprocess in non-bundle mode.");
                    return;
                }

                Stash();

                if (XPrefs.GetBool(Prefs.StreamingAssets, Prefs.StreamingAssetsDefault))
                {
                    var srcDir = XFile.NormalizePath(XPrefs.GetString(Prefs.Output, Prefs.OutputDefault).Eval(XEnv.Vars));
                    if (!XFile.HasDirectory(srcDir))
                    {
                        XLog.Warn("XAsset.Build.OnPreprocessBuild: ignore to streaming asset(s) because of non-exists dir: {0}.", srcDir);
                    }
                    else
                    {
                        if (XEnv.Platform == XEnv.PlatformType.Android || XEnv.Platform == XEnv.PlatformType.iOS)
                        {
                            var dstDir = XFile.PathJoin(XEnv.ProjectPath, "Temp", XPrefs.GetString(Prefs.LocalUri, Prefs.LocalUriDefault));
                            var srcZip = XFile.PathJoin(XEnv.ProjectPath, "Temp", XPrefs.GetString(Prefs.AssetUri, Prefs.AssetUriDefault));
                            var dstZip = XFile.PathJoin(XEnv.AssetPath, XPrefs.GetString(Prefs.AssetUri, Prefs.AssetUriDefault));

                            if (XFile.HasDirectory(dstDir)) XFile.DeleteDirectory(dstDir);
                            XFile.CopyDirectory(srcDir, dstDir, ".manifest");
                            XEditor.Utility.ZipDirectory(dstDir, srcZip);
                            XFile.CopyFile(srcZip, dstZip);

                            AssetDatabase.Refresh();
                        }
                        else
                        {
                            XEditor.Event.Decode<BuildReport>(out var report, args);
                            var outputDir = Path.GetDirectoryName(report.summary.outputPath);
                            var outputName = Path.GetFileNameWithoutExtension(report.summary.outputPath);
                            var dstDir = XFile.PathJoin(outputDir, outputName + "_Data", "Local", XPrefs.GetString(Prefs.LocalUri, Prefs.LocalUriDefault));
                            XFile.CopyDirectory(srcDir, dstDir, ".manifest");
                        }
                        XLog.Debug("XAsset.Build.OnPreprocessBuild: streaming asset(s) from <a href=\"file:///{0}\">{1}</a>.", Path.GetFullPath(srcDir), srcDir);
                    }
                }
            }

            /// <summary>
            /// XEditor.Event.Internal.OnPostprocessBuild.Process 是构建完成后的后处理，负责恢复暂存的资源并清理临时文件。
            /// 确保构建过程不会影响项目的正常开发。
            /// </summary>
            /// <param name="args">构建参数数组</param>
            void XEditor.Event.Internal.OnPostprocessBuild.Process(params object[] args)
            {
                if (!XPrefs.GetBool(Prefs.BundleMode, Prefs.BundleModeDefault))
                {
                    XLog.Debug("XAsset.Build.OnPostprocessBuild: ignore to postprocess in non-bundle mode.");
                    return;
                }

                Restore();

                if (XPrefs.GetBool(Prefs.StreamingAssets, Prefs.StreamingAssetsDefault))
                {
                    if (XEnv.Platform == XEnv.PlatformType.Android)
                    {
                        var dstZip = XFile.PathJoin(XEnv.AssetPath, "Patch@Assets.zip");
                        if (XFile.HasFile(dstZip))
                        {
                            XFile.DeleteFile(dstZip);
                            AssetDatabase.Refresh();
                        }
                    }
                }
            }

            /// <summary>
            /// Stash 将指定资源暂时移动到临时位置，用于构建过程中的资源管理。
            /// 会同时处理资源文件及其对应的 meta 文件，并记录暂存信息。
            /// </summary>
            public static void Stash()
            {
                try
                {
                    if (XFile.HasFile(stashFile)) XFile.DeleteFile(stashFile);
                    using var fs = File.Open(stashFile, FileMode.Create);
                    using var sw = new StreamWriter(fs);

                    var stashes = XPrefs.GetStrings(Prefs.Stash, Prefs.StashDefault).OrderByDescending(asset => asset.Length).ToList();
                    foreach (var stash in stashes)
                    {
                        var src = XFile.NormalizePath(Path.GetRelativePath(XEnv.ProjectPath, stash));
                        var dst = XFile.HasFile(src) ? XFile.PathJoin(Path.GetDirectoryName(src), "." + Path.GetFileName(src)) : $"{src}~";
                        if (XFile.HasDirectory(src) || XFile.HasFile(src))
                        {
                            if (XFile.HasDirectory(dst)) XFile.DeleteDirectory(dst);
                            if (XFile.HasFile(dst)) XFile.DeleteFile(dst);

                            FileUtil.MoveFileOrDirectory(src, dst);

                            var srcm = XFile.PathJoin(Path.GetDirectoryName(src), Path.GetFileName(src) + ".meta");
                            var dstm = XFile.PathJoin(Path.GetDirectoryName(src), "." + Path.GetFileName(src) + ".meta");
                            if (XFile.HasFile(srcm))
                            {
                                if (XFile.HasFile(dstm)) XFile.DeleteFile(dstm);
                                FileUtil.MoveFileOrDirectory(srcm, dstm);
                            }

                            AssetDatabase.Refresh();
                            sw.WriteLine(stash);
                            XLog.Debug("XAsset.Build.Stash: stashed asset {0} to {1}.", src, dst);
                        }
                        else XLog.Warn("XAsset.Build.Stash: stashed asset {0} not found.", src);
                    }

                    sw.Flush();
                    fs.Flush();
                }
                catch (Exception e) { XLog.Panic(e); }
            }

            /// <summary>
            /// Restore 将暂存的资源恢复到原始位置。
            /// 为了避免资源丢失，不会主动删除目标位置的文件，如果恢复过程中出现问题，会提示用户手动处理。
            /// </summary>
            [InitializeOnLoadMethod]
            public static void Restore()
            {
                if (XFile.HasFile(stashFile))
                {
                    try
                    {
                        var stashes = File.ReadAllLines(stashFile);
                        foreach (var stash in stashes)
                        {
                            var src = XFile.NormalizePath(Path.GetRelativePath(XEnv.ProjectPath, stash));
                            var dst = XFile.PathJoin(Path.GetDirectoryName(src), "." + Path.GetFileName(src));
                            if (!XFile.HasFile(dst)) dst = $"{src}~";
                            if (XFile.HasDirectory(dst) || XFile.HasFile(dst))
                            {
                                // 不主动删除，避免资源丢失，抛异常用户自行处理
                                // if (XFile.HasDirectory(dst)) XFile.DeleteDirectory(dst);
                                // if (XFile.HasFile(dst)) XFile.DeleteFile(dst);
                                FileUtil.MoveFileOrDirectory(dst, src);

                                var srcm = XFile.PathJoin(Path.GetDirectoryName(src), Path.GetFileName(src) + ".meta");
                                var dstm = XFile.PathJoin(Path.GetDirectoryName(src), "." + Path.GetFileName(src) + ".meta");
                                if (XFile.HasFile(dstm))
                                {
                                    // if (XFile.HasFile(srcm)) XFile.DeleteFile(srcm); // 同上
                                    FileUtil.MoveFileOrDirectory(dstm, srcm);
                                }

                                AssetDatabase.Refresh();
                                XLog.Debug("XAsset.Build.Restore: popuped asset {0} from {1}.", dst, src);
                            }
                        }
                    }
                    catch (Exception e) { XLog.Panic(e); }
                    finally { XFile.DeleteFile(stashFile); }
                }
            }
        }
    }
}
