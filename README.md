# EFramework Asset for Unity

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-org/U3D.RES)

EFramework Asset for Unity 提供了一套完整的资源管理解决方案，实现了从资源打包发布到运行时加载的全流程管理，支持资源加载、引用计数、自动卸载等特性。

## 功能特性

- [XAsset.Core](Documentation~/XAsset.Core.md) 实现了资源管理器的自动初始化，提供了系统事件管理、异步加载处理器等功能
- [XAsset.Manifest](Documentation~/XAsset.Manifest.md) 提供了资源清单的管理功能，用于维护和管理资源包的依赖关系
- [XAsset.Bundle](Documentation~/XAsset.Bundle.md) 提供了资源包的管理功能，支持自动处理依赖关系，并通过引用计数管理资源包的生命周期
- [XAsset.Object](Documentation~/XAsset.Object.md) 通过引用计数机制跟踪资源（Prefab）实例（GameObject）的使用情况，确保实例能够被正确释放
- [XAsset.Resource](Documentation~/XAsset.Resource.md) 提供了 Unity 资源的加载与卸载，支持自动处理依赖资源的生命周期
- [XAsset.Scene](Documentation~/XAsset.Scene.md) 提供了 Unity 场景的加载与卸载，支持自动处理依赖资源的生命周期
- [XAsset.Const](Documentation~/XAsset.Const.md) 提供了一些常量定义和运行时环境控制，包括运行配置和标签生成等功能
- [XAsset.Prefs](Documentation~/XAsset.Prefs.md) 提供了运行时的首选项管理，用于控制运行模式、调试选项和资源路径等配置项
- [XAsset.Build](Documentation~/XAsset.Build.md) 提供了资源的构建工作流，支持资源的依赖分析及打包功能
- [XAsset.Publish](Documentation~/XAsset.Publish.md) 实现了资源包的发布工作流，用于将打包好的资源发布至对象存储服务（OSS）中

## 常见问题

更多问题，请查阅[问题反馈](CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](CHANGELOG.md)
- [贡献指南](CONTRIBUTING.md)
- [许可证](LICENSE.md)