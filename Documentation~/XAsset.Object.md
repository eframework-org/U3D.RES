# XAsset.Object

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-org/U3D.RES)

XAsset.Object 通过引用计数机制跟踪资源（Prefab）实例（GameObject）的使用情况，确保实例能够被正确释放。

## 功能特性

- 引用计数管理：跟踪游戏对象的生命周期
- 全局实例追踪：管理全局资源的引用释放

## 使用手册

### 1. 保持引用
- 功能说明：引用指定游戏对象的资源包
- 注意事项：建议使用未实例化的源对象，避免过度引用导致资源包计数异常
- 方法签名：
```csharp
public static void Obtain(GameObject gameObject)
```

### 2. 释放引用
- 功能说明：释放指定游戏对象的资源包
- 注意事项：建议使用未实例化的源对象，避免过度释放导致资源包提早卸载
- 方法签名：
```csharp
public static void Release(GameObject gameObject)
```

## 常见问题

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)
