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
    private XAsset.Object testObject;
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
        testObject = tempObject.AddComponent<XAsset.Object>();
        testObject.Source = testBundle.Name;
        tempObject.SetActive(true);
    }

    [TearDown]
    public void Reset()
    {
        if (testObject != null)
        {
            UnityEngine.Object.DestroyImmediate(testObject.gameObject);
        }
        testObject = null;
        AssetBundle.UnloadAllAssetBundles(true);
        Bundle.Loaded.Clear();
        Const.bBundleMode = false;
        Bundle.Initialize();
    }

    [Test]
    public void Label()
    {
        // Arrange
        testObject.label = null; // 确保label为空

        // Act
        var label = testObject.Label;

        // Assert
        Assert.IsNotNull(label, "Label不应为空。");
        Assert.IsTrue(label.Contains("TestObject"), "Label应包含对象名称。");
        Assert.IsTrue(label.Contains("@"), "Label应包含@分隔符。");

        // 测试缓存功能
        var cachedLabel = testObject.Label;
        Assert.AreEqual(label, cachedLabel, "对Label的后续调用应返回缓存值。");
    }

    [Test]
    public void OnAwake()
    {
        Assert.AreEqual(1, testBundle.Count, "游戏对象实例化之后的引用计数应当为1");
    }

    [Test]
    public void OnDestroy()
    {
        var tempObject = UnityEngine.Object.Instantiate(testObject);
        Assert.AreEqual(2, testBundle.Count, "游戏对象再次实例化之后的引用计数应当为2");

        UnityEngine.Object.DestroyImmediate(tempObject);
        Assert.AreEqual(1, testBundle.Count, "游戏对象销毁之后的引用计数应当为1");
    }

    [Test]
    public void Manual()
    {
        XAsset.Object.Obtain(testObject.gameObject);
        Assert.AreEqual(2, testBundle.Count, "引用指定游戏对象的资源包后的计数应当为2");

        XAsset.Object.Release(testObject.gameObject);
        Assert.AreEqual(1, testBundle.Count, "释放指定游戏对象的资源包后的计数应当为1");
    }
}
#endif
