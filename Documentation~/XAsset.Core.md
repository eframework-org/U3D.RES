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

    state 资源加载流程 {
        资源加载开始 --> 资源加载请求 : OnPreLoadAsset
        资源加载请求 --> 资源加载模式
        
        资源加载模式 --> Bundle资源加载 : Bundle模式
        资源加载模式 --> Resources资源加载 : Resources模式
        
        Bundle资源加载 --> 资源引用操作
        Resources资源加载 --> 加载目标资源
        
        资源引用操作 --> 加载目标资源
        加载目标资源 --> 资源加载完成 : OnPostLoadAsset
    }

    state 场景加载流程 {
        场景加载开始 --> 场景加载请求 : OnPreLoadScene
        场景加载请求 --> 场景加载模式
        
        场景加载模式 --> Bundle场景加载 : Bundle模式
        场景加载模式 --> Resources场景加载 : Resources模式

        Bundle场景加载 --> 资源引用操作
        Resources场景加载 --> 加载目标场景
        
        资源引用操作 --> 加载目标场景
        加载目标场景 --> 场景加载完成 : OnPostLoadScene
    }

    state 资源管理流程 {
        state 引用计数流程 {
            资源引用操作 --> 手动管理引用 : Object.Obtain
            资源引用操作 --> 自动管理引用 : Object.Watch

            手动管理引用 --> 资源释放操作
            自动管理引用 --> 资源释放操作
        }

        state 资源释放流程 {
            资源释放操作 --> 手动管理释放 : Object.Release
            资源释放操作 --> 自动管理释放 : Object.Defer
            
            手动管理释放 --> 减少引用计数
            自动管理释放 --> 减少引用计数
            
            减少引用计数 --> 引用计数为零
            引用计数为零 --> 卸载资源包 : OnPostUnloadBundle
        }

        state 自动卸载流程 {
            监听场景卸载 --> 场景卸载请求 : SceneManager.sceneUnloaded
            场景卸载请求 --> 判断场景类型 : 主场景 isSubScene == false
            判断场景类型 --> 引用模式检查 : OnPreUnloadAll
            
            引用模式检查 --> 资源释放操作 : 启用
            引用模式检查 --> 卸载所有场景 : 禁用

            卸载所有场景 --> 清空场景记录 : OnPostUnloadAll
        }
    }
```

### 2. 事件类型

- 功能说明：定义资源生命周期中的关键事件
- 事件列表：
  - `OnPreLoadAsset`：资源加载前
  - `OnPostLoadAsset`：资源加载后
  - `OnPreLoadScene`：场景加载前
  - `OnPostLoadScene`：场景加载后
  - `OnPreUnloadAll`：卸载所有资源前
  - `OnPostUnloadAll`：卸载所有资源后
  - `OnPostUnloadBundle`：资源包卸载后
- 使用示例：
```csharp
XAsset.Event.Reg(XAsset.EventType.OnPreLoadAsset, (asset) => Debug.Log("资源加载前：" + asset));
```

## 常见问题

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)
