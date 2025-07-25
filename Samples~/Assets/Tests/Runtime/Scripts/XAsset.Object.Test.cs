// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using EFramework.Asset;
using UnityEngine;
using static EFramework.Asset.XAsset;

public class TestXAssetObject
{
    private XAsset.Object testRefer;
    private Bundle testBundle;

    [SetUp]
    public void Setup()
    {
        Const.bundleMode = true;
        Bundle.Initialize();

        testBundle = new Bundle { Name = "TestBundle", Count = 0 };
        Bundle.Loaded.Add(testBundle.Name, testBundle);

        var tempObject = new GameObject("TestObject");
        tempObject.SetActive(false);
        testRefer = XAsset.Object.Watch(tempObject, testBundle.Name);
        tempObject.SetActive(true);
    }

    [TearDown]
    public void Reset()
    {
        if (testRefer != null)
        {
            UnityEngine.Object.DestroyImmediate(testRefer.gameObject);
        }
        testRefer = null;
        AssetBundle.UnloadAllAssetBundles(true);
        XAsset.Object.originalObjects.Clear();
        XAsset.Object.obtainedObjects.Clear();
        Bundle.Loaded.Clear();
        Const.bBundleMode = false;
        Bundle.Initialize();
    }

    [Test]
    public void Label()
    {
        // Arrange
        testRefer.label = null; // 确保label为空

        // Act
        var label = testRefer.Label;

        // Assert
        Assert.IsNotNull(label, "Label不应为空。");
        Assert.IsTrue(label.Contains("TestObject"), "Label应包含对象名称。");
        Assert.IsTrue(label.Contains("@"), "Label应包含@分隔符。");

        // 测试缓存功能
        var cachedLabel = testRefer.Label;
        Assert.AreEqual(label, cachedLabel, "对Label的后续调用应返回缓存值。");
    }

    [Test]
    public void OnAwake()
    {
        Assert.AreEqual(1, testBundle.Count, "游戏对象实例化之后的引用计数应当为1。");
    }

    [Test]
    public void OnDestroy()
    {
        var tempObject = UnityEngine.Object.Instantiate(testRefer);
        Assert.AreEqual(2, testBundle.Count, "游戏对象再次实例化之后的引用计数应当为2。");

        UnityEngine.Object.DestroyImmediate(tempObject);
        Assert.AreEqual(1, testBundle.Count, "游戏对象销毁之后的引用计数应当为1。");
    }

    [Test]
    public void Automatic()
    {
        var tempObject = new GameObject("TempObject");
        tempObject.SetActive(false);
        var tempRefer = XAsset.Object.Watch(tempObject, testBundle.Name);
        Assert.IsTrue(XAsset.Object.originalObjects.Contains(tempRefer), "游戏对象应当在原始的列表中。");

        XAsset.Object.Defer();
        Assert.IsFalse(XAsset.Object.originalObjects.Contains(tempRefer), "游戏对象不应当在原始的列表中。");
    }

    [Test]
    public void Manual()
    {
        XAsset.Object.Obtain(testRefer.gameObject);
        Assert.IsFalse(XAsset.Object.originalObjects.Contains(testRefer), "游戏对象不应当在原始的列表中。");
        Assert.IsTrue(XAsset.Object.obtainedObjects.Contains(testRefer), "游戏对象应当在被保持的列表中。");

        XAsset.Object.Release(testRefer.gameObject);
        Assert.IsFalse(XAsset.Object.originalObjects.Contains(testRefer), "游戏对象不应当在原始的列表中。");
        Assert.IsFalse(XAsset.Object.obtainedObjects.Contains(testRefer), "游戏对象不应当在被保持的列表中。");
    }
}
#endif
