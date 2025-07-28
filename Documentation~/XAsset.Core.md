# XAsset.Core

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-org/U3D.RES)

XAsset.Core 实现了资源管理器的自动初始化，提供了系统事件管理、异步加载处理器等功能。

## 功能特性

- 自动初始化：自动加载资源清单并全局管理资源
- 系统事件：定义资源系统生命周期中的关键事件
- 引用计数：通过引用计数机制管理资源生命周期

## 使用手册

### 1. 运行流程

以下流程图展示了资源管理器的运行时逻辑，包括资源/场景加载/卸载、引用计数管理、内置事件机制的主要流程：

```mermaid
stateDiagram-v2
    direction TB

    state 资源管理流程 {
        资源加载请求 --> 资源加载模式 : OnPreLoadResource
        
        资源加载模式 --> Bundle资源加载 : bundle = true
        资源加载模式 --> Resources资源加载 : bundle == false or resource = true
        
        Bundle资源加载 --> 手动管理引用 : obtain = true
        Bundle资源加载 --> 自动管理引用 : Refer.Awake
        Resources资源加载 --> 加载目标资源
        
        增加引用计数 --> 加载目标资源
        加载目标资源 --> 资源加载完成 : OnPostLoadResource

        资源加载完成 --> 资源卸载请求
        资源卸载请求 --> 手动管理释放: Unload
        资源卸载请求 --> 自动管理释放: Refer.OnDestroy
    }

    state 场景管理流程 {
        场景加载请求 --> 场景加载模式 : OnPreLoadScene
        
        场景加载模式 --> Bundle场景加载 : bundle = true
        场景加载模式 --> Resources场景加载 : bundle = false

        Bundle场景加载 --> 自动管理引用
        Resources场景加载 --> 加载目标场景
        
        增加引用计数 --> 加载目标场景
        加载目标场景 --> 场景加载完成 : OnPostLoadScene

        场景加载完成 --> 场景卸载请求 : SceneManager.sceneUnloaded
        场景卸载请求 --> 手动管理释放: Unload
        场景卸载请求 --> 自动管理释放 : isSubScene == false
    }

    state 依赖管理流程 {
        state 依赖引用流程 {
            手动管理引用 --> 增加引用计数 : bundle.Obtain
            自动管理引用 --> 增加引用计数 : bundle.Obtain
        }

        state 依赖释放流程 {
            手动管理释放 --> 减少引用计数 : bundle.Release
            自动管理释放 --> 减少引用计数 : bundle.Release
            
            减少引用计数 --> 引用计数为零
            引用计数为零 --> 卸载资源依赖 : OnPostUnloadBundle
        }
    }
```

### 2. 事件类型

- 功能说明：定义资源生命周期中的关键事件
- 事件列表：
  - `OnPreLoadResource`：资源加载前
  - `OnPostLoadResource`：资源加载后
  - `OnPreLoadScene`：场景加载前
  - `OnPostLoadScene`：场景加载后
  - `OnPostUnloadBundle`：资源包卸载后
- 使用示例：
```csharp
XAsset.Event.Reg(XAsset.EventType.OnPreLoadResource, (asset) => Debug.Log("资源加载前：" + asset));
```

## 常见问题

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)
