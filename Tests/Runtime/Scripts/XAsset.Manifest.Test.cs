// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static EFramework.Asset.XAsset;

[PrebuildSetup(typeof(TestXAssetBuild))]
public class TestXAssetManifest
{
    [OneTimeSetUp]
    public void Init()
    {
        Const.bundleMode = true;
    }

    [SetUp]
    public void Setup()
    {
        AssetBundle.UnloadAllAssetBundles(true);
        Manifest.Main = null;
        Manifest.Bundle = null;
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Load(bool bundleMode)
    {
        // Arrange
        Const.bundleMode = bundleMode;
        Manifest.Load();

        // Act
        var manifest = Manifest.Main;
        var bundle = Manifest.Bundle;

        // Assert
        if (bundleMode)
        {
            Assert.IsNotNull(manifest, "Manifest应该被加载且不为空。");
            Assert.IsNotNull(bundle, "Manifest应该被加载且不为空。");
        }
        else
        {
            Assert.IsNull(Manifest.Main, "在非BundleMode模式下，Manifest应保持为空。");
            Assert.IsNull(Manifest.Bundle, "在非BundleMode模式下，Bundle应保持为空。");
        }
    }
}
#endif