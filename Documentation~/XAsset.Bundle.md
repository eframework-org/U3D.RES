# XAsset.Bundle

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)

XAsset.Bundle 提供了资源包的管理功能，支持自动处理依赖关系，并通过引用计数管理资源包的生命周期。

## 功能特性

- 资源包管理：支持同步和异步加载资源包，支持缓存
- 引用计数管理：自动处理依赖关系，通过引用计数管理资源的生命周期

## 使用手册

### 1. 资源包管理

#### 1.1 同步加载
- 功能说明：加载指定的资源包及其所有依赖资源包
- 函数参数：
  - `name`：资源包名称（`string` 类型）
- 函数返回：加载的资源包（`Bundle` 类型），如果加载失败则返回 `null`
- 使用示例：
```csharp
var bundle = XAsset.Bundle.Load("example.bundle");
```

#### 1.2 异步加载
- 功能说明：异步加载指定的资源包及其所有依赖资源包
- 函数参数：
  - `name`：资源包名称（`string` 类型）
  - `handler`：用于跟踪和报告加载进度的处理器（`Handler` 类型）
- 函数返回：异步加载的协程对象
- 使用示例：
```csharp
var handler = new Handler();
yield return XAsset.Bundle.LoadAsync("example.bundle", handler);
```

#### 1.3 查找加载
- 功能说明：在已加载的资源包中查找指定名称的资源包
- 函数参数：
  - `name`：资源包名称（`string` 类型）
- 函数返回：找到的资源包（`Bundle` 类型），如果未找到则返回 `null`
- 使用示例：
```csharp
var bundle = XAsset.Bundle.Find("example.bundle");
```

### 2. 引用计数管理

#### 2.1 增加引用
- 功能说明：增加资源包的引用计数，同时增加所有依赖资源包的引用计数
- 函数参数：
  - `from`：引用来源的描述（`string` 类型），用于调试时追踪资源的使用情况
- 函数返回：增加后的引用计数（`int` 类型）
- 使用示例：
```csharp
int count = bundle.Obtain();
```

#### 2.2 减少引用
- 功能说明：减少资源包的引用计数，当计数为 0 时自动卸载资源包及其不再被引用的依赖资源
- 函数参数：
  - `from`：引用来源的描述（`string` 类型），用于调试时追踪资源的使用情况
- 函数返回：减少后的引用计数（`int` 类型）
- 使用示例：
```csharp
int count = bundle.Release();
```

#### 2.3 卸载资源
- 功能说明：卸载指定的资源包，减少其引用计数，当计数为 0 时释放资源
- 函数参数：
  - `name`：资源包名称（`string` 类型）
- 使用示例：
```csharp
XAsset.Bundle.Unload("example.bundle");
```

## 常见问题

### 1. 为什么资源包没有被卸载？
- 确保引用计数已降为 0
- 检查是否有其他资源包依赖该资源包

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)