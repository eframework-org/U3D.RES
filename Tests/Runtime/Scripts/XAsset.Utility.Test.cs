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
