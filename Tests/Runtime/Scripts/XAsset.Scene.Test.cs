// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;
using UnityEngine;
using System.Text.RegularExpressions;
using System;
using EFramework.Asset;
using static EFramework.Asset.XAsset;
using UnityEngine.SceneManagement;

[PrebuildSetup(typeof(TestXAssetBuild))]
public class TestXAssetScene
{
    [SetUp]
    public void Setup()
    {
        Const.bundleMode = true;
        Manifest.Load();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Load(bool bundleMode)
    {
        var sceneName = "Packages/org.eframework.u3d.res/Tests/Runtime/Scenes/TestScene";
        Const.bundleMode = bundleMode;
        var sceneTag = Const.GenTag(sceneName + "_unity");

        // Assert
        if (bundleMode)
        {
            XAsset.Scene.Load(sceneName, LoadSceneMode.Additive);
            Assert.IsTrue(Bundle.Loaded.ContainsKey(sceneTag));
            XAsset.Scene.Unload(sceneName);
            Assert.IsFalse(Bundle.Loaded.ContainsKey(sceneTag));
        }
        else
        {
            LogAssert.Expect(LogType.Error, new Regex(@"Scene 'TestScene' couldn't be loaded because it has not been added to the .*"));
            XAsset.Scene.Load(sceneName, LoadSceneMode.Additive);
            Assert.IsFalse(Bundle.Loaded.ContainsKey(sceneTag));
        }
    }

    [UnityTest]
    public IEnumerator LoadAsync()
    {
        // Arrange
        var sceneName = "Packages/org.eframework.u3d.res/Tests/Runtime/Scenes/TestScene";
        bool[] bundleModes = { true, false };
        foreach (var mode in bundleModes)
        {
            Const.bundleMode = mode;

            if (!mode)
            {
                LogAssert.Expect(LogType.Error, new Regex(@"Scene 'TestScene' couldn't be loaded because it has not been added to the .*"));
                LogAssert.Expect(LogType.Exception, new Regex(@"NullReferenceException: Object reference not set to an instance of an object.*"));
                try
                {
                    XAsset.Scene.LoadAsync(sceneName, () => { }, LoadSceneMode.Additive);
                }
                catch (Exception e)
                {
                    Assert.IsTrue(e is NullReferenceException);
                }
            }
            else
            {
                var isLoaded = false;
                // Act
                yield return XAsset.Scene.LoadAsync(sceneName, () =>
                {
                    // Assert
                    isLoaded = true;
                    XAsset.Scene.Unload(sceneName);
                }, LoadSceneMode.Additive);

                // Assert
                Assert.IsTrue(isLoaded, "场景应该被加载");
            }
        }
    }
}
#endif