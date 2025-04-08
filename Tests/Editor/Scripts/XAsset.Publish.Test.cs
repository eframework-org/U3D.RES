// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using EFramework.Editor;
using EFramework.Utility;
using EFramework.Asset.Editor;
using UnityEngine.Networking;
using UnityEngine.TestTools;
using UnityEngine;
using System.Text.RegularExpressions;

/// <summary>
/// XAsset.Publish 的单元测试类，验证资源发布过程的正确性。
/// </summary>
[PrebuildSetup(typeof(TestXAssetBuild))]
public class TestXAssetPublish
{
    [Test]
    public void Process()
    {
        // 设置测试环境
        XPrefs.Asset.Set(XAsset.Publish.Prefs.Host, "http://localhost:9000");
        XPrefs.Asset.Set(XAsset.Publish.Prefs.Bucket, "default");
        XPrefs.Asset.Set(XAsset.Publish.Prefs.Access, "admin");
        XPrefs.Asset.Set(XAsset.Publish.Prefs.Secret, "adminadmin");
        XPrefs.Asset.Set(XAsset.Publish.Prefs.LocalUri, "Assets");
        XPrefs.Asset.Set(XAsset.Publish.Prefs.RemoteUri, $"TestXAssetPublish/Builds-{XTime.GetMillisecond()}/Assets");

        // 创建处理器
        var handler = new XAsset.Publish() { ID = "Test/TestXAssetPublish" };

        // 执行发布
        LogAssert.Expect(LogType.Error, new Regex(@"<ERROR> Object does not exist.*"));
        LogAssert.Expect(LogType.Error, new Regex(@"XEditor\.Cmd\.Run: finish mc.*"));
        var report = XEditor.Tasks.Execute(handler);

        // 验证发布结果
        Assert.AreEqual(report.Result, XEditor.Tasks.Result.Succeeded, "资源发布应当成功");

        var manifestUrl = $"{XPrefs.Asset.GetString(XAsset.Publish.Prefs.Host)}/{XPrefs.Asset.GetString(XAsset.Publish.Prefs.Bucket)}/{XPrefs.Asset.GetString(XAsset.Publish.Prefs.RemoteUri)}/{XMani.Default}";
        var req = UnityWebRequest.Get(manifestUrl);
        req.timeout = 10;
        req.SendWebRequest();
        while (!req.isDone) { }
        Assert.IsTrue(req.responseCode == 200, "资源清单应当请求成功");

        var manifest = new XMani.Manifest();
        Assert.IsTrue(manifest.Parse(req.downloadHandler.text, out _), "资源清单应当读取成功");
    }
}
#endif
