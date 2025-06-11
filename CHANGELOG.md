# 更新记录

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
