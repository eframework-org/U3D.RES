// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using static EFramework.Asset.XAsset;

[PrebuildSetup(typeof(TestXAssetBuild))]
public class TestXAssetResource
{
    [OneTimeSetUp]
    public void Init()
    {
        Const.bBundleMode = true;
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

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Load(bool bundleMode, bool referMode)
    {
        LogAssert.ignoreFailingMessages = true;

        Const.bBundleMode = true;
        Const.bundleMode = bundleMode;

        Const.bReferMode = true;
        Const.referMode = referMode;

        // Arrange
        var assetPath = "Assets/Tests/Runtime/Resources/Bundle/Prefab/TestCube";
        var notExistPath = "Assets/Tests/Runtime/Resources/Bundle/Prefab/NotExist";
        var bundleName = Const.GetName(assetPath);

        // 非泛型加载
        var asset1 = Resource.Load(assetPath, typeof(GameObject), obtain: true) as GameObject;
        Assert.IsNotNull(asset1, "加载的资产不应为空。");
        Assert.IsInstanceOf<GameObject>(asset1, "加载的资产应为 GameObject 类型。");
        if (bundleMode)
        {
            if (referMode) Assert.IsNotNull(asset1.GetComponent<Resource.Refer>(), "引用模式下 GameObject 实例上的 Resource.Refer 对象不应当为空。");
            // 使用 AssetBundle 模式加载时，Resource.Refer 组件会在源实例上保持，此处非 refer 模式下不作验证。
            // else Assert.IsNull(asset1.GetComponent<Resource.Refer>(), "非引用模式下 GameObject 实例上的 Resource.Refer 对象应当为空。");

            var bundleInfo = Bundle.Find(bundleName);
            Assert.AreEqual(1, bundleInfo.Count, "obtain = true 时引用计数应当为 1。");

            // 卸载
            Assert.DoesNotThrow(() => Resource.Unload(assetPath));
            Assert.AreEqual(0, bundleInfo.Count, "资源卸载后的引用计数应当仍为 0。");
        }

        // 泛型加载
        var asset2 = Resource.Load<GameObject>(assetPath, obtain: false);
        Assert.IsNotNull(asset2, "加载的资产不应为空。");
        Assert.IsInstanceOf<GameObject>(asset2, "加载的资产应为 GameObject 类型。");
        if (bundleMode)
        {
            var bundleInfo = Bundle.Find(bundleName);
            Assert.AreEqual(0, bundleInfo.Count, "obtain = false 时引用计数应当为 0。");
        }

        var notExistAsset = Resource.Load(notExistPath, typeof(GameObject));
        Assert.IsNull(notExistAsset, "加载的资产应为空。");

        LogAssert.ignoreFailingMessages = false;
    }

    [UnityTest]
    public IEnumerator LoadAsync()
    {
        LogAssert.ignoreFailingMessages = true;

        bool[] bundleModes = { true, false };
        bool[] referModes = { true, false };

        foreach (var bundleMode in bundleModes)
        {
            Const.bBundleMode = false;
            Const.bundleMode = bundleMode;

            foreach (var referMode in referModes)
            {
                Const.bReferMode = false;
                Const.referMode = referMode;

                // Arrange
                var assetPath = "Assets/Tests/Runtime/Resources/Bundle/Prefab/TestCube";
                var notExistPath = "Assets/Tests/Runtime/Resources/Bundle/Prefab/NotExist";
                var bundleName = Const.GetName(assetPath);

                // Act
                var handler1 = Resource.LoadAsync(assetPath, typeof(GameObject), origin =>
                {
                    var asset = origin as GameObject;
                    Assert.IsNotNull(asset, "加载的资产不应为空。");
                    Assert.IsInstanceOf<GameObject>(asset, "加载的资产应为 GameObject 类型。");
                    if (bundleMode)
                    {
                        if (referMode) Assert.IsNotNull(asset.GetComponent<Resource.Refer>(), "引用模式下 GameObject 实例上的 Resource.Refer 对象不应当为空。");
                        // 使用 AssetBundle 模式加载时，Resource.Refer 组件会在源实例上保持，此处非 refer 模式下不作验证。
                        // else Assert.IsNull(asset.GetComponent<Resource.Refer>(), "非引用模式下 GameObject 实例上的 Resource.Refer 对象应当为空。");

                        var bundleInfo = Bundle.Find(bundleName);
                        Assert.AreEqual(1, bundleInfo.Count, "obtain = true 时引用计数应当为 1。");

                        // 卸载
                        Assert.DoesNotThrow(() => Resource.Unload(assetPath));
                        Assert.AreEqual(0, bundleInfo.Count, "资源卸载后的引用计数应当仍为 0。");
                    }
                }, obtain: true);
                yield return handler1;

                // 测试泛型加载
                var handler2 = Resource.LoadAsync<GameObject>(assetPath, asset =>
                {
                    Assert.IsNotNull(asset, "加载的资产不应为空。");
                    Assert.IsInstanceOf<GameObject>(asset, "加载的资产应为 GameObject 类型。");
                    if (bundleMode)
                    {
                        var bundleInfo = Bundle.Find(bundleName);
                        Assert.AreEqual(0, bundleInfo.Count, "obtain = false 时引用计数应当为 0。");
                    }
                }, obtain: false);
                yield return handler2;

                var handler3 = Resource.LoadAsync(notExistPath, typeof(GameObject), asset =>
                {
                    Assert.IsNull(asset, "加载的资产应为空。");
                });
                yield return handler3;
            }
        }

        LogAssert.ignoreFailingMessages = false;
    }

    [Test]
    public void IsLoading()
    {
        Resource.Loading.Clear();

        Resource.Loading.Add("TestIsLoading", new Resource.Task());
        Assert.IsTrue(Resource.IsLoading("TestIsLoading"), "应当返回正在加载。");
        Assert.IsFalse(Resource.IsLoading(null), "应当返回未正在加载。");
        Assert.IsFalse(Resource.IsLoading(string.Empty), "应当返回未正在加载。");
        Assert.IsFalse(Resource.IsLoading("Invalid"), "应当返回未正在加载。");

        Resource.Loading.Clear();
    }
}
#endif
