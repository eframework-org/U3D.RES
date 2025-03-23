// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using static EFramework.Asset.XAsset;

[PrebuildSetup(typeof(TestXAssetBuild))]
public class TestXAssetResource
{
    private GameObject testGameObject;

    [SetUp]
    public void Setup()
    {
        Manifest.Main = null;
        Manifest.Bundle = null;
        Manifest.Load();
        testGameObject = new GameObject("TestObject");
    }

    [TearDown]
    public void Reset()
    {
        GameObject.Destroy(testGameObject);
        AssetBundle.UnloadAllAssetBundles(true);
        Bundle.Loaded.Clear();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Load(bool bundleMode)
    {
        Const.bundleMode = bundleMode;
        // Arrange
        var path = "Packages/org.eframework.u3d.res/Tests/Runtime/Resources/Bundle/Prefab/TestCube";
        var notExistPath = "Packages/org.eframework.u3d.res/Tests/Runtime/Resources/Bundle/Prefab/NotExist";

        // Act
        var asset1 = Resource.Load(path, typeof(GameObject));

        // Assert
        Assert.IsNotNull(asset1, "加载的资产不应为空。");
        Assert.IsInstanceOf<GameObject>(asset1, "加载的资产应为GameObject类型。");
        Assert.DoesNotThrow(() => Resource.Unload(path));

        // 测试泛型加载
        var asset2 = Resource.Load<GameObject>(path);
        Assert.IsNotNull(asset2, "加载的资产不应为空。");
        Assert.IsInstanceOf<GameObject>(asset2, "加载的资产应为GameObject类型。");
        Assert.DoesNotThrow(() => Resource.Unload(path));

        var notExistAsset = Resource.Load(notExistPath, typeof(GameObject));
        Assert.IsNull(notExistAsset, "加载的资产应为空。");
    }

    [UnityTest]
    public IEnumerator LoadAsync()
    {
        bool[] bundleModes = { true, false };
        foreach (var bundleMode in bundleModes)
        {
            Const.bundleMode = bundleMode;
            // Arrange
            var path = "Packages/org.eframework.u3d.res/Tests/Runtime/Resources/Bundle/Prefab/TestCube";
            var notExistPath = "Packages/org.eframework.u3d.res/Tests/Runtime/Resources/Bundle/Prefab/NotExist";

            // Act
            var handler1 = Resource.LoadAsync(path, typeof(GameObject), (asset) =>
            {
                // Assert
                Assert.IsNotNull(asset, "加载的资产不应为空。");
                Assert.IsInstanceOf<GameObject>(asset, "加载的资产应为GameObject类型。");
                Assert.DoesNotThrow(() => Resource.Unload(path));
            });
            yield return handler1;

            // 测试泛型加载
            var handler2 = Resource.LoadAsync<GameObject>(path, (asset) =>
            {
                // Assert
                Assert.IsNotNull(asset, "加载的资产不应为空。");
                Assert.IsInstanceOf<GameObject>(asset, "加载的资产应为GameObject类型。");
                Assert.DoesNotThrow(() => Resource.Unload(path));
            });
            yield return handler2;

            var handler3 = Resource.LoadAsync(notExistPath, typeof(GameObject), (asset) =>
            {
                Assert.IsNull(asset, "加载的资产应为空。");
            });
            yield return handler3;
        }
    }
}
#endif
