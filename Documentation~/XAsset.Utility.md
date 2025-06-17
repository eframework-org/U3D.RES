# XAsset.Utility

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-org/U3D.RES)

XAsset.Utility 提供了资源加载的工具函数集，包括进度监控和状态查询等功能。

## 功能特性

- 进度监控：支持获取所有加载任务的总体进度
- 状态查询：支持查询指定资源或全局加载状态

## 使用手册

### 1. 状态查询

#### 1.1 检查全局加载状态
- 功能说明：检查是否有任何资源正在加载
- 函数返回：`bool` 类型，`true` 表示有资源正在加载
- 使用示例：
```csharp
bool isLoading = XAsset.Utility.Loading();
```

#### 1.2 检查指定资源加载状态
- 功能说明：检查指定路径的资源是否正在加载
- 函数参数：
  - `path`：资源路径（`string` 类型），为空时检查全局加载状态
- 函数返回：`bool` 类型，`true` 表示指定资源正在加载
- 使用示例：
```csharp
bool isResourceLoading = XAsset.Utility.Loading("Resources/Example/Test.prefab");
```

## 常见问题

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)