// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using EFramework.Asset;
using static EFramework.Asset.XAsset;

[PrebuildSetup(typeof(TestXAssetBuild))]
public class TestXAssetScene
{
    internal const string TestScene = "Packages/org.eframework.u3d.res/Tests/Runtime/Scenes/TestScene";

    [OneTimeSetUp]
    public void Init()
    {
        Const.bundleMode = true;
        Manifest.Load();

        var scenes = EditorBuildSettings.scenes.ToList();
        var scene = new EditorBuildSettingsScene
        {
            guid = new GUID(AssetDatabase.AssetPathToGUID(TestScene)),
            path = TestScene,
            enabled = true
        };
        scenes.Add(scene);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        AssetBundle.UnloadAllAssetBundles(true);
        Bundle.Loaded.Clear();
        Const.bBundleMode = false;
        Manifest.Load();

        var scenes = EditorBuildSettings.scenes.ToList();
        scenes.RemoveAll(ele => ele.path == TestScene);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    [UnityTest]
    public IEnumerator Load()
    {
        LogAssert.ignoreFailingMessages = true;

        string[] sceneNames = { TestScene, "NotExist" };
        bool[] bundleModes = { true, false };
        foreach (var bundleMode in bundleModes)
        {
            Const.bundleMode = bundleMode;

            foreach (var sceneName in sceneNames)
            {
                var name = Path.GetFileNameWithoutExtension(sceneName);
                XAsset.Scene.Load(sceneName, LoadSceneMode.Additive);
                yield return null; // 允许 Unity 场景系统刷新

                var isLoaded = false;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.name == name && scene.isLoaded)
                    {
                        isLoaded = true;
                        break;
                    }
                }
                if (sceneName == "NotExist") Assert.IsFalse(isLoaded, "加载不存在的场景应当失败。");
                else Assert.IsTrue(isLoaded, "加载存在的场景应当成功。");
            }
        }

        LogAssert.ignoreFailingMessages = false;
    }

    [UnityTest]
    public IEnumerator LoadAsync()
    {
        LogAssert.ignoreFailingMessages = true;

        string[] sceneNames = { TestScene, "NotExist" };
        bool[] bundleModes = { true, false };
        foreach (var bundleMode in bundleModes)
        {
            Const.bundleMode = bundleMode;

            foreach (var sceneName in sceneNames)
            {
                var name = Path.GetFileNameWithoutExtension(sceneName);
                var handler = XAsset.Scene.LoadAsync(sceneName, callback: null, LoadSceneMode.Additive);
                yield return handler; // 等待 Unity 场景系统刷新

                var isLoaded = false;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.name == name && scene.isLoaded)
                    {
                        isLoaded = true;
                        break;
                    }
                }
                if (sceneName == "NotExist")
                {
                    Assert.IsFalse(isLoaded, "加载不存在的场景应当失败。");
                    Assert.IsTrue(handler.Error, "加载不存在的场景 handler.Error 应当为 true。");
                }
                else
                {
                    Assert.IsTrue(isLoaded, "加载存在的场景应当成功。");
                    Assert.IsFalse(handler.Error, "加载存在的场景 handler.Error 应当为 false。");
                    Assert.AreEqual(1f, handler.Progress, "加载存在的场景 handler.Progress 应当为 1f。");
                }
            }
        }

        LogAssert.ignoreFailingMessages = false;
    }
}
#endif
