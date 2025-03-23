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
    [TestCase("Assets/Textures/My Texture")]    // 包含空格
    [TestCase("Assets/Textures/My#Texture")]    // 包含#号
    [TestCase("Assets/Textures/My[Texture]")]   // 包含方括号
    [TestCase("Assets\\Textures\\MyTexture")]   // 包含反斜杠
    public void Tag(string path)
    {
        // Arrange
        var expectedTag = "assets_textures_mytexture.bundle";
        // 清空缓存以确保测试的准确性
        Const.tagCache.Clear();

        // Act
        var tag = Const.GenTag(path);

        // Assert
        Assert.AreEqual(expectedTag, tag, "生成的标签应符合预期格式。");
        Assert.IsTrue(Const.tagCache.ContainsKey(path), "资源路径在首次调用后应被缓存。");
        Assert.AreEqual(1, Const.tagCache.Count, "缓存应该只包含一个条目。");
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

    [Test]
    public void Escape()
    {
        // Assert
        Assert.AreEqual("_", Const.escapeChars["_"], "下划线应映射为下划线。");
        Assert.AreEqual("", Const.escapeChars[" "], "空格应映射为空字符串。");
        Assert.AreEqual("", Const.escapeChars["#"], "井号应映射为空字符串。");
        Assert.AreEqual("", Const.escapeChars["["], "左方括号应映射为空字符串。");
        Assert.AreEqual("", Const.escapeChars["]"], "右方括号应映射为空字符串。");
    }
}
#endif