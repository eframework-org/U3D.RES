// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using EFramework.Asset;
using UnityEngine;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;
using static EFramework.Asset.XAsset;
using EFramework.Utility;

public class TestXAssetObject
{
    private GameObject testGameObject;
    private XAsset.Object testObject;

    [OneTimeSetUp]
    public void Init()
    {
        Const.bundleMode = true;
        Bundle.Initialize();
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        AssetBundle.UnloadAllAssetBundles(true);
        Bundle.Loaded.Clear();
        Const.bBundleMode = false;
        Bundle.Initialize();
    }

    [SetUp]
    public void Setup()
    {
        testGameObject = new GameObject("TestObject");
        testGameObject.SetActive(false);
        testObject = testGameObject.AddComponent<XAsset.Object>();
        testObject.Source = "TestBundle";
        var bundle = new Bundle { Name = "TestBundle", Count = 0 };
        Bundle.Loaded.Add(bundle.Name, bundle);
        testGameObject.SetActive(true);
    }

    [TearDown]
    public void Reset()
    {
        if (testGameObject != null)
        {
            GameObject.Destroy(testGameObject);
            testGameObject = null;
        }
        XAsset.Object.Loaded.Clear();
        Bundle.Loaded.Clear();
        testObject = null;
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
    public void OnDestroy()
    {
        // Arrange
        Assert.AreEqual(1, testObject.Count, "初始计数应为1。");

        // Act
        // 销毁对象
        GameObject.DestroyImmediate(testGameObject);
        testGameObject = null;

        // Assert
        Assert.AreEqual(0, testObject.Count, "OnDestroy后计数应减至0。");
    }

    [Test]
    public void OnAwake()
    {
        // Arrange
        GameObject tempGO = new GameObject("TempObject");
        tempGO.SetActive(false);
        XAsset.Object tempObj = tempGO.AddComponent<XAsset.Object>();
        tempObj.Source = "NonExistentBundle"; // 设置一个不存在的资源包

        // Act
        tempGO.SetActive(true); // 触发Awake

        // Assert
        Assert.AreEqual(1, tempObj.Count, "即使资源包不存在，计数仍应增加到1。");

        // Cleanup
        GameObject.DestroyImmediate(tempGO);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Obtain(bool referMode)
    {
        // Arrange
        Const.bReferMode = false;
        XPrefs.Asset.Set(Prefs.ReferMode, referMode);

        // 准备测试对象
        testObject.Count = 0; // 设置Count为0，使其可以被获取

        // Act
        XAsset.Object.Obtain(testGameObject);

        // Assert
        Assert.AreEqual(referMode, testObject.Count == -1, "Obtain后计数应设置为-1。");

        if (referMode)
        {
            testObject.Count = 1; // 设置Count为1，使其不能被获取
            LogAssert.Expect(LogType.Error, new Regex(@".*can not obtain object on an instantiated asset.*"));
            XAsset.Object.Obtain(testGameObject);
            Assert.AreEqual(1, testObject.Count, "当对实例化资产调用Obtain时，计数应保持为1。");
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Release(bool referMode)
    {
        Const.bReferMode = false;
        // Arrange
        XPrefs.Asset.Set(Prefs.ReferMode, referMode);

        // Act
        testObject.Obtain();

        // Assert
        XAsset.Object.Release(testGameObject);
        Assert.AreEqual(referMode, testObject.Count == 0, "当ReferMode为true时，计数应为0。");

        // 测试不能被释放的情况
        if (referMode)
        {
            testObject.Count = 1; // 设置Count为1，使其不能被释放
            LogAssert.Expect(LogType.Error, new Regex(@".*can not unload object on an instantiated asset.*"));
            XAsset.Object.Release(testGameObject);
            Assert.AreEqual(1, testObject.Count, "当对实例化资产调用Release时，计数应保持为1。");
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Cleanup(bool debugMode)
    {
        // 测试Scene.Loaded
        XAsset.Object.Cleanup();
        Assert.AreEqual(XAsset.Object.Loaded.Count, 0);

        // 准备Object
        var testGameObject = new GameObject("TestObject");
        testGameObject.SetActive(false);
        var testObject1 = testGameObject.AddComponent<XAsset.Object>(); // Count = -1
        var testObject2 = testGameObject.AddComponent<XAsset.Object>(); // Count = 0
        var testObject3 = testGameObject.AddComponent<XAsset.Object>(); // Count > 0
        testObject1.Source = "TestBundle1";
        testObject2.Source = "TestBundle2";
        testObject3.Source = "TestBundle3";
        var bundle1 = new XAsset.Bundle { Name = "TestBundle1", Count = 0 };
        var bundle2 = new XAsset.Bundle { Name = "TestBundle2", Count = 0 };
        var bundle3 = new XAsset.Bundle { Name = "TestBundle3", Count = 0 };
        XAsset.Bundle.Loaded.Add(bundle1.Name, bundle1);
        XAsset.Bundle.Loaded.Add(bundle2.Name, bundle2);
        XAsset.Bundle.Loaded.Add(bundle3.Name, bundle3);
        testGameObject.SetActive(true);

        XAsset.Const.debugMode = debugMode;
        testObject1.Count = -1;
        testObject2.Count = 1;
        testObject3.Count = 2;
        bundle1.Count = -1;
        bundle2.Count = 0;
        bundle3.Count = 1;
        if (debugMode)
        {
            XAsset.Object.Cleanup();
            Assert.IsTrue(testObject1.Count == -1);
            Assert.IsTrue(testObject2.Count == 1);
            Assert.IsTrue(testObject3.Count == 2);
        }
        else
        {
            XAsset.Object.Cleanup();
            Assert.IsTrue(bundle1.Count == -1);
            Assert.IsTrue(bundle2.Count == -1);
            Assert.IsTrue(bundle3.Count == -1);
        }
        Assert.IsTrue(XAsset.Object.Loaded.Count == 0);
    }
}
#endif
