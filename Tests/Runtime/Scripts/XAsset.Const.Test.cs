// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using EFramework.Utility;
using static EFramework.Asset.XAsset;

public class TestXAssetConst
{
    [TestCase("Assets/Textures/MyTexture")]     // 正常路径
    [TestCase("Packages\\Test\\MyTexture.png")]   // 包含反斜杠
    [TestCase("Assets/Scenes/MyScene.unity")]   // 场景资源
    [TestCase("")]
    [TestCase(null)]
    public void Name(string path)
    {
        // Arrange
        // 清空缓存以确保测试的准确性
        Const.nameCache.Clear();

        // Act
        var name = Const.GetName(path);
        var expected = string.Empty;
        if (!string.IsNullOrEmpty(path))
        {
            var extension = System.IO.Path.GetExtension(path);
            if (path.StartsWith("Assets/")) path = path["Assets/".Length..];
            if (!string.IsNullOrEmpty(extension) && extension != ".unity") // 场景文件只能单独打包
            {
                path = path.Replace(extension, "");
            }
            expected = XFile.NormalizePath(path).ToLower().MD5() + Const.Extension;
        }

        // Assert
        Assert.AreEqual(expected, name, "生成的标签应符合预期格式。");
        if (!string.IsNullOrEmpty(path)) Assert.IsTrue(Const.nameCache.ContainsKey(path), "资源路径在首次调用后应被缓存。");
    }

    [TestCase(true, true, true)]
    [TestCase(false, false, false)]
    public void Mode(bool bundleMode, bool referMode, bool debugMode)
    {
        Const.bundleMode = bundleMode;
        Const.referMode = referMode;
        Const.debugMode = debugMode;

        // Assert
        Assert.AreEqual(Const.bundleMode, bundleMode, "当在偏好设置中设置时，BundleMode应为true。");
        Assert.AreEqual(Const.referMode, referMode, "当BundleMode和ReferMode在偏好设置中都设置时，ReferMode应为true。");
        Assert.AreEqual(Const.debugMode, debugMode, "当BundleMode和DebugMode在偏好设置中都设置时，DebugMode应为true。");
    }

    [Test]
    public void Path()
    {
        // Arrange
        var expectedLocalPath = XFile.PathJoin(XEnv.LocalPath, XPrefs.GetString(Prefs.LocalUri, Prefs.LocalUriDefault));

        // Act
        var actualLocalPath = Const.LocalPath;

        // Assert
        Assert.AreEqual(expectedLocalPath, actualLocalPath, "LocalPath应与预期路径匹配。");
        Assert.IsTrue(Const.bLocalPath, "获取LocalPath后，bLocalPath标志应被设置。");
    }
}
#endif
