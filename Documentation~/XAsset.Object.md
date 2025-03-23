# XAsset.Object

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)

XAsset.Object 通过引用计数机制跟踪资源（Prefab）实例（GameObject）的使用情况，确保实例能够被正确释放。

## 功能特性

- 引用计数管理：通过引用计数跟踪资源使用情况
- 全局实例追踪：统一管理和清理全局资源实例列表

## 使用手册

### 1. 释放引用
- 功能说明：释放指定游戏对象持有的资源
- 方法签名：
```csharp
public static void Release(GameObject go)
```
- 使用示例：
```csharp
XAsset.Object.Release(testGameObject);
```

### 2. 保持引用
- 功能说明：手动引用指定游戏对象的资源，防止资源被卸载
- 方法签名：
```csharp
public static void Obtain(GameObject go)
```
- 使用示例：
```csharp
XAsset.Object.Obtain(testGameObject);
```

### 3. 清理实例
- 功能说明：清理所有已加载的资源对象，释放未被引用的资源
- 方法签名：
```csharp
public static void Cleanup()
```
- 使用示例：
```csharp
XAsset.Object.Cleanup();
```

## 常见问题

### 1. 为什么资源没有被卸载？
- 检查引用计数是否已降为 0
- 确保没有其他对象手动引用该资源

### 2. 如何避免资源泄漏？
- 在不再需要资源时，调用 `Release` 方法释放引用
- 使用 `Cleanup` 方法清理未被引用的资源

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)