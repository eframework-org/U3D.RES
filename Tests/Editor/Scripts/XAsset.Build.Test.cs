// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using EFramework.Utility;
using EFramework.Editor;
using System.IO;
using UnityEditor;
using System.Linq;
using static EFramework.Asset.Editor.XAsset;
using static EFramework.Asset.Editor.XAsset.Build;
using UnityEngine.TestTools;
using EFramework.Asset;

public class TestXAssetBuild : IPrebuildSetup
{
    void IPrebuildSetup.Setup()
    {
        // 创建处理器
        var handler = new Build() { ID = "Test/Build Test Assets" };

        // 设置测试环境
        var packagePath = XEditor.Utility.FindPackage().assetPath;

        // 使用包含路径
        var include = new string[] {
                    "Tests/Runtime/Resources/Bundle", "Tests/Runtime/Scenes" }.
            Select(path => XFile.PathJoin(packagePath, path)).ToArray();

        XPrefs.Asset.Set(Build.Prefs.MergeSingle, false);
        XPrefs.Asset.Set(Build.Prefs.MergeMaterial, true);
        XPrefs.Asset.Set(Build.Prefs.Include, include);
        XPrefs.Asset.Set(Build.Prefs.Exclude, new string[] { });

        var buildDir = XFile.NormalizePath(XPrefs.GetString(Build.Prefs.Output, Build.Prefs.OutputDefault).Eval(XEnv.Vars));
        var manifestFile = XFile.PathJoin(buildDir, XMani.Default);

        var report = XEditor.Tasks.Execute(handler);

        Assert.AreEqual(report.Result, XEditor.Tasks.Result.Succeeded, "资源构建应当成功");
        Assert.IsTrue(XFile.HasFile(manifestFile), "资源清单应当生成成功");

        var manifest = new XMani.Manifest();
        Assert.IsTrue(manifest.Read(manifestFile)(), "资源清单应当读取成功");

        foreach (var file in manifest.Files)
        {
            var path = XFile.PathJoin(buildDir, file.Name);
            Assert.IsTrue(XFile.HasFile(path), "文件应当存在于本地：" + file.Name);
            Assert.AreEqual(XFile.FileMD5(path), file.MD5, "文件MD5应当一致：" + file.Name);
            Assert.AreEqual(XFile.FileSize(path), file.Size, "文件大小应当一致：" + file.Name);
        }

        // 复制资源到本地
        if (XFile.HasDirectory(XAsset.Const.LocalPath)) XFile.DeleteDirectory(XAsset.Const.LocalPath);
        XFile.CopyDirectory(buildDir, XAsset.Const.LocalPath);
        Assert.IsTrue(XFile.HasDirectory(XAsset.Const.LocalPath));
    }

    [Test]
    public void Process() { }

    [TestCase(false, false, TestName = "不合并单包_不合并材质")]
    [TestCase(false, true, TestName = "不合并单包_合并材质")]
    [TestCase(true, true, TestName = "合并单包_合并材质")]
    [TestCase(true, false, TestName = "合并单包_不合并材质")]
    public void Merge(bool mergeSingle, bool mergeMaterial)
    {
        // 设置测试环境
        var packagePath = XEditor.Utility.FindPackage().assetPath;

        // 使用固定的包含路径
        var include = new string[] {
            "Tests/Runtime/Resources/Bundle",
            "Tests/Runtime/Scenes"
        }.Select(path => XFile.PathJoin(packagePath, path)).ToArray();

        XPrefs.Asset.Set(Prefs.MergeSingle, mergeSingle);
        XPrefs.Asset.Set(Prefs.MergeMaterial, mergeMaterial);
        XPrefs.Asset.Set(Prefs.Include, include);
        XPrefs.Asset.Set(Prefs.Exclude, new string[] { });

        // 执行依赖关系生成
        var dependency = GenDependency();

        // 验证依赖关系生成结果
        Assert.IsNotNull(dependency, "依赖关系字典不应为空");

        // 验证MergeSingle选项的效果
        var hasTestSingleBundle = dependency.Values.Any(k => k.Exists(e => e.Contains("TestSingle")));
        if (mergeSingle) Assert.IsFalse(hasTestSingleBundle, "启用MergeSingle时，TestSingle 资源不应有独立 Bundle");
        else Assert.IsTrue(hasTestSingleBundle, "未启用MergeSingle时，TestSingle 资源应有独立Bundle");

        // 验证MergeMaterial选项的效果
        var hasMaterialBundle = false;
        foreach (var kvp in dependency)
        {
            foreach (var asset in kvp.Value)
            {
                if (asset.Contains("TestSceneMat1.mat") || asset.Contains("TestSceneMat2.mat"))
                {
                    hasMaterialBundle = true;
                    break;
                }
            }
            if (hasMaterialBundle) break;
        }
        if (mergeMaterial) Assert.IsFalse(hasMaterialBundle, "启用MergeMaterial时，材质不应有独立Bundle");
        else Assert.IsTrue(hasMaterialBundle, "未启用MergeMaterial时，材质应有独立Bundle");
    }

    [TestCase(new string[] { "Tests/Runtime/Resources/Bundle/", "Tests/Runtime/Scenes/TestScene.unity" }, new string[] { }, TestName = "路径包含_无排除")]
    [TestCase(new string[] { "Tests/Runtime/Resources/Bundle/", "Tests/Runtime/Scenes/TestScene.unity" }, new string[] { "Tests/Runtime/Scenes/TestScene.unity" }, TestName = "路径包含_路径排除")]
    [TestCase(new string[] { "Tests/Runtime/Resources/**/*", "Tests/Runtime/Scenes/**/*.unity" }, new string[] { }, TestName = "通配包含_无排除")]
    [TestCase(new string[] { "Tests/Runtime/Resources/**/*", "Tests/Runtime/Scenes/**/*.unity" }, new string[] { "Tests/Runtime/Resources/**/*", "Tests/Runtime/Scenes/TestScene2.unity" }, TestName = "通配包含_通配排除")]
    public void Exclude(string[] include, string[] exclude)
    {
        // 设置测试环境
        var packagePath = XEditor.Utility.FindPackage().assetPath;

        // 将相对路径转换为绝对路径
        var absoluteIncludes = include.Select(path => XFile.PathJoin(packagePath, path)).ToArray();
        var absoluteExcludes = exclude.Select(path => XFile.PathJoin(packagePath, path)).ToArray();

        // 使用固定的合并选项
        XPrefs.Asset.Set(Prefs.MergeSingle, false);
        XPrefs.Asset.Set(Prefs.MergeMaterial, false);
        XPrefs.Asset.Set(Prefs.Include, absoluteIncludes);
        XPrefs.Asset.Set(Prefs.Exclude, absoluteExcludes);

        // 执行依赖关系生成
        var dependency = GenDependency();

        // 验证依赖关系生成结果
        Assert.IsNotNull(dependency, "依赖关系字典不应为空");

        // 验证Include选项的效果
        if (include.Length > 0)
        {
            // 验证包含规则是否正确应用
            bool allIncluded = true;
            foreach (var inc in include)
            {
                if (inc.Contains("*") || inc.Contains("?") || inc.Contains("["))
                {
                    // 对于特定的通配符模式进行验证
                    if (inc.Contains("*.unity"))
                    {
                        var hasUnityBundle = dependency.Values.Any(k => k.Exists(e => e.EndsWith(".unity")));
                        Assert.IsTrue(hasUnityBundle, "应包含场景Bundle");
                    }
                    continue;
                }

                var fullPath = XFile.PathJoin(packagePath, inc);
                if (XFile.HasFile(fullPath))
                {
                    // 单个文件
                    var found = false;
                    foreach (var kvp in dependency)
                    {
                        if (kvp.Value.Any(v => v.Contains(Path.GetFileName(fullPath))))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found && !exclude.Any(e => e.Contains(Path.GetFileName(fullPath))))
                    {
                        allIncluded = false;
                    }
                }
            }
            Assert.IsTrue(allIncluded, "所有指定资源都被包含");
        }

        // 验证Exclude选项的效果
        if (exclude.Length > 0)
        {
            // 验证排除的资源是否真的被排除
            foreach (var exc in exclude)
            {
                if (exc.Contains("*") || exc.Contains("?") || exc.Contains("["))
                {
                    // 对于特定的通配符模式进行验证
                    if (exc.Contains("*.unity"))
                    {
                        // 检查是否有匹配的unity文件被包含
                        string unityPattern = exc.Replace("*.unity", "");
                        bool hasUnityFile = false;

                        foreach (var kvp in dependency)
                        {
                            foreach (var v in kvp.Value)
                            {
                                if (v.Contains(unityPattern) && v.EndsWith(".unity"))
                                {
                                    hasUnityFile = true;
                                    break;
                                }
                            }
                            if (hasUnityFile) break;
                        }
                        Assert.IsFalse(hasUnityFile, $"通配符 {exc} 匹配的场景文件应被排除");
                    }
                    continue;
                }

                var fullPath = XFile.PathJoin(packagePath, exc);
                bool isExcluded = true;

                if (XFile.HasFile(fullPath))
                {
                    // 单个文件
                    foreach (var kvp in dependency)
                    {
                        if (kvp.Value.Any(v => v.Contains(Path.GetFileName(fullPath))))
                        {
                            isExcluded = false;
                            break;
                        }
                    }
                    Assert.IsTrue(isExcluded, $"文件 {Path.GetFileName(fullPath)} 应被排除");
                }
                else if (XFile.HasDirectory(fullPath))
                {
                    // 目录
                    foreach (var kvp in dependency)
                    {
                        foreach (var v in kvp.Value)
                        {
                            if (v.Contains(fullPath))
                            {
                                isExcluded = false;
                                break;
                            }
                        }
                        if (!isExcluded) break;
                    }
                    Assert.IsTrue(isExcluded, $"目录 {exc} 中的资源应被排除");
                }
            }
        }
    }

    [Test]
    public void Stash()
    {
        // 准备测试目录和文件
        var testDir = XFile.PathJoin(XEnv.ProjectPath, "Temp", "TestXAssetBuild");

        var testStashDir = XFile.PathJoin(testDir, "Stash");
        if (!XFile.HasDirectory(testStashDir)) XFile.CreateDirectory(testStashDir);
        var testStashDirMeta = XFile.PathJoin(testDir, "Stash.meta");
        XFile.SaveText(testStashDirMeta, "Test meta dir");

        var testStashFile = XFile.PathJoin(testStashDir, "TestStashFile.txt");
        XFile.SaveText(testStashFile, "Test content for stash function");
        var testStashFileMeta = XFile.PathJoin(testStashDir, "TestStashFile.txt.meta");
        XFile.SaveText(testStashFileMeta, "Test meta content");

        XPrefs.Asset.Set(Prefs.Stash, new string[] { testStashDir });

        try
        {
            // 执行Stash操作
            Build.Stash();

            // 验证原始文件已被移动
            Assert.IsFalse(XFile.HasFile(testStashFile), "原始文件应当被移动");
            Assert.IsFalse(XFile.HasFile(testStashFileMeta), "原始meta文件应当被移动");

            // 验证隐藏文件已创建
            var hiddenDir = $"{testStashDir}~";
            var hiddenDirMeta = XFile.PathJoin(Path.GetDirectoryName(testStashDir), "." + Path.GetFileName(testStashDir) + ".meta");
            Assert.IsTrue(XFile.HasDirectory(hiddenDir), "隐藏文件应当存在");
            Assert.IsTrue(XFile.HasFile(hiddenDirMeta), "隐藏meta文件应当存在");

            // 验证stashFile已创建并包含正确内容
            Assert.IsTrue(XFile.HasFile(stashFile), "暂存文件应当存在");
            var stashContent = XFile.OpenText(stashFile);
            Assert.IsTrue(stashContent.Contains(testStashDir), "暂存文件应当包含测试目录路径");

            // 执行Restore操作
            Assert.DoesNotThrow(() => Restore(), "恢复应当不抛出异常");

            // 验证文件已恢复
            Assert.IsTrue(XFile.HasFile(testStashFile), "原始文件应当恢复");
            Assert.IsTrue(XFile.HasFile(testStashFileMeta), "原始meta文件应当恢复");

            // 验证隐藏文件已被删除
            Assert.IsFalse(XFile.HasDirectory(hiddenDir), "隐藏文件应当被删除");
            Assert.IsFalse(XFile.HasFile(hiddenDirMeta), "隐藏meta文件应当被删除");

            // 验证stashFile已被删除
            Assert.IsFalse(XFile.HasFile(stashFile), "暂存文件应当被删除");
        }
        finally
        {
            // 清理测试文件和目录
            if (XFile.HasDirectory(testStashDir)) XFile.DeleteDirectory(testStashDir);
            if (XFile.HasFile(testStashDirMeta)) XFile.DeleteFile(testStashDirMeta);

            // 确保stashFile被删除
            if (XFile.HasFile(stashFile)) XFile.DeleteFile(stashFile);
        }
    }
}
#endif
