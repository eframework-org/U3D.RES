# XAsset.Manifest

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-org/U3D.RES)

XAsset.Manifest 提供了资源清单的管理功能，用于维护和管理资源包的依赖关系。

## 功能特性

- 依赖管理：通过清单文件追踪和管理资源包的依赖关系
- 自动加载：根据当前平台自动加载对应的清单文件
- 内存管理：支持清单资源包的正确卸载，避免内存泄漏

## 使用手册

### 1. 加载清单

- 功能说明：加载当前平台对应的资源清单文件
- 详细描述：
  - 如果存在旧的清单文件，会先卸载它
  - 加载成功后，清单文件会被缓存到 `Main` 属性中
- 使用示例：
```csharp
XAsset.Manifest.Load();
```

### 2. 清单属性

- 功能说明：获取当前加载的资源清单实例
- 使用示例：
```csharp
var manifest = XAsset.Manifest.Main;
```

## 常见问题

### 1. 为什么清单文件加载失败？
- 检查清单文件是否存在于指定路径
- 确保当前运行模式为 Bundle 模式

### 2. 什么时候应该重载清单？
- 当资源热更后，如果有 AssetBundle 文件变更，应当重新加载清单

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)