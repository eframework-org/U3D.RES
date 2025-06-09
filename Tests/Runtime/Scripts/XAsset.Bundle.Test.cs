// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Text.RegularExpressions;
using EFramework.Asset;
using static EFramework.Asset.XAsset;

[PrebuildSetup(typeof(TestXAssetBuild))]
public class TestXAssetBundle
{
    [SetUp]
    public void Setup()
    {
        Const.bundleMode = true;
        Manifest.Load();
    }

    [TearDown]
    public void Reset()
    {
        AssetBundle.UnloadAllAssetBundles(true);
        Bundle.Loaded.Clear();
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
        Assert.AreEqual(2, bundle.Count, "最终计数应该为2。");
    }

    [Test]
    public void Release()
    {
        // Arrange
        AssetBundle assetBundle = null;
        string bundleName = "packages_org.eframework.u3d.res_tests_runtime_resources_bundle_prefab_testcube.bundle";
        var bundle = Bundle.Load(bundleName);
        XAsset.Event.Reg(XAsset.EventType.OnPostUnloadBundle, (AssetBundle ab) => { assetBundle = ab; });

        // Act
        int count = bundle.Release();

        // Assert
        Assert.AreEqual(0, count, "Release应该减少引用计数。");
        Assert.IsFalse(Bundle.Loaded.ContainsKey(bundle.Name), "当计数为零时，Bundle应该从Loaded中移除。");
        Assert.IsNotNull(assetBundle, "当bundle被释放时，应该通知事件。");

        Bundle.Unload(bundle.Name);
    }

    [Test]
    public void LoadAndUnload()
    {
        // Act
        var bundleName = "packages_org.eframework.u3d.res_tests_runtime_resources_bundle_prefab_testcube.bundle";
        // 检查初始状态
        Assert.IsNull(Bundle.Find(bundleName), "初始状态下Bundle不应该被加载。");
        var bundle = Bundle.Load(bundleName);

        // 测试加载不存在的bundle
        LogAssert.Expect(LogType.Error, new Regex(".*Unable to open archive.*"));
        LogAssert.Expect(LogType.Error, new Regex(".*Failed to read data for the AssetBundle.*"));
        LogAssert.Expect(LogType.Error, new Regex(".*sync load main-ab error.*"));
        var noneBundleName = "non_existent_bundle.bundle";
        var noneBundle = Bundle.Load(noneBundleName);

        // Assert 
        Assert.IsNotNull(bundle, "加载的bundle不应为空。");
        Assert.AreEqual(bundleName, bundle.Name, "加载的bundle名称应匹配。");
        Assert.IsNull(noneBundle, "加载不存在的bundle应返回null。");
        Assert.IsFalse(Bundle.Loaded.ContainsKey(noneBundleName), "不存在的bundle不应被添加到Loaded字典中。");

        Bundle.Unload(bundle.Name);
        Assert.IsTrue(Bundle.Loaded.Count == 0, "卸载后，Bundle应从Loaded中移除。");
    }

    [UnityTest]
    public IEnumerator LoadAsync()
    {
        // Act
        var bundleName = "packages_org.eframework.u3d.res_tests_runtime_resources_bundle_prefab_testcube.bundle";
        var handler = new Handler();
        handler.OnPostload += () =>
        {
            // Assert
            Assert.IsNotNull(handler.Asset, "加载的bundle不应为空。");
            Assert.AreEqual(bundleName, handler.Asset.name, "加载的bundle名称应匹配。");
            Bundle.Unload(bundleName);
        };
        yield return Bundle.LoadAsync(bundleName, handler);
    }

    [Test]
    public void LoadSame()
    {
        // Act
        var bundleName = "packages_org.eframework.u3d.res_tests_runtime_resources_bundle_prefab_testcube.bundle";
        var bundle1 = Bundle.Load(bundleName);
        var bundle2 = Bundle.Load(bundleName);

        // Assert
        Assert.IsNotNull(bundle1, "第一次加载的bundle不应为空。");
        Assert.IsNotNull(bundle2, "第二次加载的bundle不应为空。");
        Assert.AreSame(bundle1, bundle2, "两个引用应指向同一个bundle对象。");
        Assert.AreEqual(2, bundle1.Count, "两次加载后引用计数应为2。");

        // Cleanup
        Bundle.Unload(bundleName);
        Assert.AreEqual(1, Bundle.Find(bundleName).Count, "第一次卸载后引用计数应为1。");
        Bundle.Unload(bundleName);
        Assert.IsNull(Bundle.Find(bundleName), "第二次卸载后bundle应完全卸载。");
    }

    [Test]
    public void Find()
    {
        // Arrange
        var bundle = new Bundle { Name = "TestBundle", Count = 1 };
        Bundle.Loaded.Add(bundle.Name, bundle);

        // Assert
        Assert.IsTrue(Bundle.Find("TestBundle") != null);

        Bundle.Unload(bundle.Name);
        Assert.IsTrue(Bundle.Find("TestBundle") == null);
    }
}
#endif
