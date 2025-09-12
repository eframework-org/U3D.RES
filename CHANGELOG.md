# 更新记录

## [0.1.4] - 2025-09-12
### 修复
- 修复 Scene 模块响应 SceneManager.sceneLoaded 时侵入式修改 isSubScene 的问题

## [0.1.3] - 2025-08-21
### 修复
- 修复 Bundle 模块同步和异步加载并发引起的结果不确定的问题

## [0.1.2] - 2025-07-28
### 变更
- 内聚 Object 模块的功能至 Resource 模块，统一管理资源包的引用和释放
- 移除冗余的内部事件：OnPreUnloadAll、OnPostUnloadAll
- 变更内部事件的名称：OnPreLoadAsset -> OnPreLoadResource、OnPostLoadAsset -> OnPostLoadResource
- 移除 Core 模块对 Scene 模块的管理，取消 Bundle 模块的自动初始化

### 修复
- 修复 Object 模块持有的原始 GameObject 列表引发的场景切换闪退问题

## [0.1.1] - 2025-07-25
### 修复
- 修复 Object 模块 Watch 函数重复监听的问题
- 修复 Object 模块 Obtain 函数的逻辑错误
- 修复 Bundle 模块 Obtain 引用计数错误的问题

## [0.1.0] - 2025-07-25
### 变更
- 重构 Object 模块的实现，通过 Original 和 Obtained 实例列表管理资源包的引用与释放
- 重构 Object.Test 模块若干单元测试的用例

## [0.0.9] - 2025-07-07
### 变更
- 修改 Asset/Build/Output@Editor 的默认值为 Builds/Patch/${Env.Platform}/Assets
- 修改 Asset/Build/Include@Editor 的默认值为 ["Assets/Resources/Bundle", "Assets/Scenes/**/*.unity"]
- 修改 Asset/RemoteUri 的默认值为 Builds/Patch/${Env.Author}/${Env.Version}/${Env.Platform}/Assets
- 修改 Const.Manifest 的默认值为 Assets
- 新增 Build Assets 和 Publish Assets 任务配置项的 GUI 面板显示
- 移除 Utility 的 Loading 函数并重构为 Resource/Scene.IsLoading
- 移除 Manifest 模块，耦合其功能至 Bundle 模块中
- 修改 Const.GenTag 函数为 Const.GetName
- 重构 Asset Bundle 文件的命名方式，避免文件名过长同时降低可读性
- 优化 Publish Assets 推送流程的文件清单版本记录命名规则

### 修复
- 修复取消资源构建引起的 Asset Bundle 文件偏移错乱的问题
- 修复 XAsset.Handler 资源加载进度计算错误的问题

## [0.0.8] - 2025-06-17
### 变更
- 移除 XAsset.Utility.Progress 函数（该指标无实际意义）

### 修复
- 修复 XAsset.Bundle 同步和异步并发加载引发的 Bundle 加载失败问题（same files is already loaded.）

## [0.0.7] - 2025-06-12
### 变更
- 公开 XAsset.Build 和 XAsset.Publish 的首选项字段访问

### 修复
- 修复 XAsset.Object 引发的 Untiy Player Crash（CG -> MarkAllDependencies）

## [0.0.6] - 2025-06-11
### 变更
- 修改 Asset/Build/Output@Editor 的默认值为 Builds/Patch/Assets/${Env.Channel}/${Env.Platform}
- 新增 XAsset.Const.Manifest 字段，使得业务层可以自定义主 AssetBundleManifest 的文件名

### 新增
- 新增 Asset/Build/Streaming/Assets 配置项控制构建时拷贝资源至 StreamingAssets

## [0.0.5] - 2025-06-09
### 变更
- 优化加载资源和场景的错误处理逻辑
- 公开 XAsset.Const.Extension 字段，使得业务层可以自定义拓展名

### 修复
- 修复 XAsset 首选项编辑器在 Unity 2021 版本的数组序列化问题
- 修复 XAsset.Handler.Progress 进度计算错误的问题

### 新增
- 新增 XAsset.Handler.Error 字段表示是否发生错误中断
- 新增 Asset/OffsetFactor 配置项用于对 Bundle 文件进行偏移计算
- 新增 [DeepWiki](https://deepwiki.com) 智能索引，方便开发者快速查找相关文档

## [0.0.4] - 2025-04-02
### 修复
- 修复了 Windows 平台出包时资源没有正确打到包里的问题

## [0.0.3] - 2025-03-31
### 变更
- 更新依赖库版本

## [0.0.2] - 2025-03-26
### 变更
- 更新依赖库版本

## [0.0.1] - 2025-03-23
### 新增
- 首次发布
