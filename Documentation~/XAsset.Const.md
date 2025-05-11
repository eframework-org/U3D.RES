# XAsset.Const

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-org/U3D.RES)

XAsset.Const 提供了一些常量定义和运行时环境控制，包括运行配置和标签生成等功能。

## 功能特性

- 运行配置：提供 Bundle 模式、调试模式和资源路径等配置
- 标签生成：提供资源路径的标准化处理和标签生成

## 使用手册

### 1. 运行配置

#### 1.1 Bundle 模式
- 配置说明：控制是否启用 Bundle 模式
- 依赖条件：编辑器模式下需要启用模拟模式
- 访问方式：
```csharp
bool isBundleMode = XAsset.Const.BundleMode;
```

#### 1.2 引用计数模式
- 配置说明：控制是否启用引用计数模式
- 依赖条件：仅在 Bundle 模式下可用
- 访问方式：
```csharp
bool isReferMode = XAsset.Const.ReferMode;
```

#### 1.3 调试模式
- 配置说明：控制是否启用调试模式
- 依赖条件：仅在 Bundle 模式下可用
- 访问方式：
```csharp
bool isDebugMode = XAsset.Const.DebugMode;
```

#### 1.4 本地路径
- 配置说明：获取资源文件的本地存储路径
- 访问方式：
```csharp
string localPath = XAsset.Const.LocalPath;
```

### 2. 标签生成

#### 2.1 默认扩展名
```csharp
public const string Extension = ".bundle";
```

#### 2.2 生成规则
生成资源的标签名称，规则如下：
1. 将路径分隔符替换为下划线
2. 移除特殊字符
3. 转换为小写
4. 添加 `.bundle` 扩展名

使用示例：
```csharp
string assetPath = "Resources/Example/Test.prefab";
string tag = XAsset.Const.GenTag(assetPath);
// 输出: assets_example_test.bundle
```

特殊字符处理规则：
```csharp
{
    "_" -> "_"  // 保留
    " " -> ""   // 移除
    "#" -> ""   // 移除
    "[" -> ""   // 移除
    "]" -> ""   // 移除
}
```

## 常见问题

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)