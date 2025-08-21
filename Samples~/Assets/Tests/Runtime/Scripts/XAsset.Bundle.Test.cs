// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EFramework.Asset;
using static EFramework.Asset.XAsset;

[PrebuildSetup(typeof(TestXAssetBuild))]
public class TestXAssetBundle
{
    [SetUp]
    public void Setup()
    {
        Const.bBundleMode = true;
        Const.bundleMode = true;
        Bundle.Initialize();
    }

    [TearDown]
    public void Reset()
    {
        AssetBundle.UnloadAllAssetBundles(true);
        Bundle.Loaded.Clear();
        Const.bBundleMode = false;
        Bundle.Initialize();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Initialize(bool bundleMode)
    {
        Const.bBundleMode = true;
        Const.bundleMode = bundleMode;
        Bundle.Initialize();
        if (bundleMode) Assert.IsNotNull(Bundle.Manifest, "Bundle 模式下 Manifest 应该被加载且不为空。");
        else Assert.IsNull(Bundle.Manifest, "非 Bundle 模式下 Manifest 应保持为空。");
    }

    [Test]
    public void Obtain()
    {
        // Arrange
        var bundle = new Bundle { Name = "TestBundle", Count = 0 };
        Bundle.Loaded.Add(bundle.Name, bundle);

        // Act
        int count1 = bundle.Obtain();
        int count2 = bundle.Obtain();

        // Assert
        Assert.AreEqual(1, count1, "Obtain应该增加引用计数。");
        Assert.AreEqual(2, count2, "Obtain应该增加引用计数。");
    }

    [Test]
    public void Release()
    {
        // Arrange
        AssetBundle assetBundle = null;
        var bundleName = Const.GetName("Assets/Tests/Runtime/Resources/Bundle/Prefab/TestCube.prefab");
        var bundle = Bundle.Load(bundleName);
        XAsset.Event.Reg(XAsset.EventType.OnPostUnloadBundle, (AssetBundle ab) => { assetBundle = ab; });

        // Act
        bundle.Obtain();
        int count = bundle.Release();

        // Assert
        Assert.AreEqual(0, count, "Release应该减少引用计数。");
        Assert.IsFalse(Bundle.Loaded.ContainsKey(bundle.Name), "当计数为零时，Bundle应该从Loaded中移除。");
        Assert.IsNotNull(assetBundle, "当bundle被释放时，应该通知事件。");
    }

    [Test]
    public void Load()
    {
        // Act
        var bundleName = Const.GetName("Assets/Tests/Runtime/Resources/Bundle/Prefab/TestCube.prefab");
        // 检查初始状态
        Assert.IsNull(Bundle.Find(bundleName), "初始状态下Bundle不应该被加载。");
        var bundle = Bundle.Load(bundleName);

        // 测试加载不存在的bundle
        LogAssert.Expect(LogType.Error, new Regex(".*Unable to open archive.*"));
        LogAssert.Expect(LogType.Error, new Regex(".*Failed to read data for the AssetBundle.*"));
        LogAssert.Expect(LogType.Error, new Regex(".*sync load main bundle error.*"));
        var noneBundleName = "non/existent/bundle";
        var noneBundle = Bundle.Load(noneBundleName);

        // Assert 
        Assert.IsNotNull(bundle, "加载的bundle不应为空。");
        Assert.AreEqual(bundleName, bundle.Name, "加载的bundle名称应匹配。");
        Assert.IsNull(noneBundle, "加载不存在的bundle应返回null。");
        Assert.IsFalse(Bundle.Loaded.ContainsKey(noneBundleName), "不存在的bundle不应被添加到Loaded字典中。");
    }

    [UnityTest]
    public IEnumerator LoadAsync()
    {
        // Act
        var bundleName = Const.GetName("Assets/Tests/Runtime/Resources/Bundle/Prefab/TestCube.prefab");
        yield return Bundle.LoadAsync(bundleName);
        var bundle = Bundle.Find(bundleName);
        Assert.IsNotNull(bundle, "加载的bundle不应为空。");
        Assert.AreEqual(bundleName, bundle.Name, "加载的bundle名称应匹配。");
    }

    [UnityTest]
    public IEnumerator LoadConcurrent()
    {
        var bundleName = Const.GetName("Assets/Tests/Runtime/Resources/Bundle/Prefab/TestCube.prefab");
        for (var i = 0; i < 100; i++)
        {
            Setup();

            var iter1 = Bundle.LoadAsync(bundleName);
            iter1.MoveNext(); // 进入异步加载队列

            var iter2 = Bundle.LoadAsync(bundleName);
            iter2.MoveNext();

            var iter3 = Bundle.LoadAsync(bundleName);
            iter3.MoveNext();

            var bundle = Bundle.Load(bundleName);

            yield return iter1;
            yield return iter2;
            yield return iter3;

            Assert.IsNotNull(bundle, "加载的bundle不应为空。");
            Assert.AreEqual(bundleName, bundle.Name, "加载的bundle名称应匹配。");

            Reset();
        }
    }

    [Test]
    public void Find()
    {
        Assert.IsTrue(Bundle.Find("TestBundle") == null);

        var bundle = new Bundle { Name = "TestBundle", Count = 1 };
        Bundle.Loaded.Add(bundle.Name, bundle);

        Assert.IsTrue(Bundle.Find("TestBundle") != null);
    }
}
#endif
