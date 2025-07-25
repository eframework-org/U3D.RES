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
public class TestXAssetCore
{
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

    [UnityTest]
    public IEnumerator Handler()
    {
        LogAssert.ignoreFailingMessages = true;

        bool[] bundleModes = { true, false };
        foreach (var bundleMode in bundleModes)
        {
            Const.bundleMode = bundleMode;
            // 测试Progress
            var handler = new Handler
            {
                totalCount = 5,
                doneCount = 2
            };
            var progress = handler.Progress;
            Assert.AreEqual(0.4f, progress, "Progress应该被正确计算。");

            // 测试IsDone
            handler = Resource.LoadAsync("Assets/Tests/Runtime/Resources/Bundle/Prefab/TestCube", typeof(GameObject));
            Assert.IsFalse(handler.IsDone, "当Operation未完成时，IsDone应为false。");
            yield return handler;
            Assert.IsTrue(handler.IsDone, "当Operation完成时，IsDone应为true。");

            handler = Resource.LoadAsync("NotExist", typeof(GameObject));
            yield return handler;
            Assert.IsTrue(handler.Error, "当加载不存在的资源时，Error应为true。");

            // 测试MoveNext
            handler = Resource.LoadAsync("Assets/Tests/Runtime/Resources/Bundle/Prefab/TestCube", typeof(GameObject));
            Assert.AreNotEqual(handler.MoveNext(), handler.IsDone, "当Operation不为空时，MoveNext应为true。");
            yield return handler;

            // 测试Preload 和 Postload
            handler = new Handler();
            bool preloadWasCalled = false;
            bool postloadWasCalled = false;
            handler.OnPreload += () => preloadWasCalled = true;
            handler.OnPostload += () => postloadWasCalled = true;
            yield return Resource.LoadAsync("Assets/Tests/Runtime/Resources/Bundle/Prefab/TestCube", typeof(GameObject), null, handler);
            Assert.IsTrue(preloadWasCalled, "OnPreload事件应被调用。");
            Assert.IsTrue(postloadWasCalled, "OnPostload事件应被调用。");

            // 测试Reset
            handler.Reset();
            Assert.AreEqual(0, handler.doneCount, "doneCount应重置为0。");
            Assert.AreEqual(0, handler.totalCount, "totalCount应重置为0。");
            Assert.IsNull(handler.Request, "Request应重置为null。");
            preloadWasCalled = false;
            postloadWasCalled = false;
            handler.InvokePreload();
            handler.InvokePostload();
            Assert.IsFalse(preloadWasCalled, "OnPreload事件不应被调用。");
            Assert.IsFalse(postloadWasCalled, "OnPostload事件不应被调用。");
        }

        LogAssert.ignoreFailingMessages = false;
    }
}
#endif
