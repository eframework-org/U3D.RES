# XAsset.Const

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-org/U3D.RES)

XAsset.Const 提供了一些常量定义和运行时环境控制，包括运行配置和 Bundle 名称生成、偏移计算等功能。

## 功能特性

- 运行配置：提供 Bundle 模式、调试模式和资源路径等配置
- 名称生成：提供资源路径的处理并生成归一化的 Bundle 名称
- 偏移计算：根据首选项中配置的 Bundle 偏移因子计算文件的偏移

## 使用手册

### 1. 运行配置

#### 1.1 Bundle 模式
- 配置说明：控制是否启用 Bundle 模式
- 依赖条件：编辑器模式下需要启用模拟模式
- 访问方式：
```csharp
var isBundleMode = XAsset.Const.BundleMode;
```

#### 1.2 引用计数模式
- 配置说明：控制是否启用引用计数模式
- 依赖条件：仅在 Bundle 模式下可用
- 访问方式：
```csharp
var isReferMode = XAsset.Const.ReferMode;
```

#### 1.3 调试模式
- 配置说明：控制是否启用调试模式
- 依赖条件：仅在 Bundle 模式下可用
- 访问方式：
```csharp
var isDebugMode = XAsset.Const.DebugMode;
```

#### 1.4 本地路径
- 配置说明：获取资源文件的本地存储路径
- 访问方式：
```csharp
var localPath = XAsset.Const.LocalPath;
```

### 2. 名称生成

#### 2.1 默认扩展名
```csharp
XAsset.Const.Extension = ".bundle";
```

#### 2.2 生成规则
生成资源名称的规则如下：
1. 将资源路径的 `Assets/` 剔除
2. 获取资源的拓展名，若为空或 `.unity` 则不处理，否则剔除拓展名
3. 归一化路径并转为全小写，避免路径的大小写问题
4. 对归一化的路径进行 `MD5` 求值并追加 `XAsset.Const.Extension` 扩展名

使用示例：
```csharp
var assetPath = "Resources/Example/Test.prefab";
var bundleName = XAsset.Const.GetName(assetPath);
```

### 3. 偏移计算

#### 3.1 偏移因子
- 配置说明：控制 Bundle 文件偏移的计算因子
- 依赖条件：通过首选项配置 `XPrefs.GetInt(Prefs.OffsetFactor)`
- 默认值：从 `Prefs.OffsetFactorDefault` 获取

#### 3.2 计算方法
偏移计算的规则如下：
1. 如果 Bundle 名称为空，返回 0
2. 如果偏移因子小于等于 0，返回 0
3. 计算公式：`(bundleName.Length % offsetFactor + 1) * 28`

使用示例：
```csharp
var bundleName = XAsset.Const.GetName(assetPath);
var offset = XAsset.Const.GetOffset(bundleName);
```

## 常见问题

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)
