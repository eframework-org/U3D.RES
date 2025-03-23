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
public class TestXAssetUtility
{
    [SetUp]
    public void Setup()
    {
        Resource.Loading.Clear();
        Scene.Loading.Clear();
        Bundle.Loading.Clear();
    }

    [UnityTest]
    public IEnumerator Progress()
    {
        // 测试没有资源加载时的进度
        var progress = Utility.Progress();
        Assert.AreEqual(1f, progress);

        // 测试场景加载时的进度
        var handle = Resource.LoadAsync("Packages/org.eframework.u3d.res/Tests/Runtime/Resources/Bundle/Prefab/TestCube", typeof(GameObject));
        yield return null;
        progress = Utility.Progress();
        Assert.IsTrue(progress <= 1f);

        // 测试资源加载完成时的进度
        yield return handle;
        progress = Utility.Progress();
        Assert.AreEqual(1f, progress);
    }

    [Test]
    public void Loading()
    {
        Resource.Loading.Add("TestLoading", new Resource.Task());
        var isLoading = Utility.Loading();
        Assert.IsTrue(isLoading);
        Assert.IsTrue(Utility.Loading("TestLoading"));
        Assert.IsFalse(Utility.Loading("NoLoading"));

        Resource.Loading.Clear();
        isLoading = Utility.Loading();
        Assert.IsFalse(isLoading, "当没有资源加载时，Loading()应返回false");
    }
}
#endif