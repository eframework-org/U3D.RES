// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using static EFramework.Asset.XAsset;

public class TestXAssetPrefs
{
    [Test]
    public void Keys()
    {
        Assert.AreEqual(Prefs.BundleMode, "Asset/BundleMode");
        Assert.AreEqual(Prefs.ReferMode, "Asset/ReferMode");
        Assert.AreEqual(Prefs.DebugMode, "Asset/DebugMode");
        Assert.AreEqual(Prefs.SimulateMode, "Asset/SimulateMode@Editor");
        Assert.AreEqual(Prefs.SecretKey, "Asset/SecretKey");
        Assert.AreEqual(Prefs.OffsetFactor, "Asset/OffsetFactor");
        Assert.AreEqual(Prefs.AssetUri, "Asset/AssetUri");
        Assert.AreEqual(Prefs.LocalUri, "Asset/LocalUri");
        Assert.AreEqual(Prefs.RemoteUri, "Asset/RemoteUri");
    }

    [Test]
    public void Defaults()
    {
        Assert.AreEqual(Prefs.BundleModeDefault, true);
        Assert.AreEqual(Prefs.ReferModeDefault, true);
        Assert.AreEqual(Prefs.SecretKeyDefault, "${Env.Secret}");
        Assert.AreEqual(Prefs.OffsetFactorDefault, 4);
        Assert.AreEqual(Prefs.AssetUriDefault, "Patch@Assets.zip");
        Assert.AreEqual(Prefs.LocalUriDefault, "Assets");
        Assert.AreEqual(Prefs.RemoteUriDefault, "Builds/Patch/${Env.Author}/${Env.Version}/${Env.Platform}/Assets");
    }
}
#endif
