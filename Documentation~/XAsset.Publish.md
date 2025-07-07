# XAsset.Publish

[![Version](https://img.shields.io/npm/v/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)
[![Downloads](https://img.shields.io/npm/dm/org.eframework.u3d.res)](https://www.npmjs.com/package/org.eframework.u3d.res)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-org/U3D.RES)

XAsset.Publish 实现了资源包的发布工作流，用于将打包好的资源发布至对象存储服务（OSS）中。

## 功能特性

- 首选项配置：提供首选项配置以自定义发布流程
- 自动化流程：提供资源包发布任务的自动化执行

## 使用手册

### 1. 首选项配置

| 配置项 | 配置键 | 默认值 | 功能说明 |
|--------|--------|--------|----------|
| 主机地址 | `Asset/Publish/Host@Editor` | `${Env.OssHost}` | OSS 服务地址 |
| 存储桶名 | `Asset/Publish/Bucket@Editor` | `${Env.OssBucket}` | OSS 存储桶名 |
| 访问密钥 | `Asset/Publish/Access@Editor` | `${Env.OssAccess}` | OSS 访问密钥 |
| 秘密密钥 | `Asset/Publish/Secret@Editor` | `${Env.OssSecret}` | OSS 秘密密钥 |

关联配置项：`Asset/LocalUri`、`Asset/RemoteUri`

以上配置项均可在 `Tools/EFramework/Preferences/Asset/Publish` 首选项编辑器中进行可视化配置。

### 2. 自动化流程

#### 2.1 本地环境

本地开发环境可以使用 MinIO 作为对象存储服务：

1. 安装服务：

```bash
# 启动 MinIO 容器
docker run -d --name minio -p 9000:9000 -p 9090:9090 --restart=always \
  -e "MINIO_ACCESS_KEY=admin" -e "MINIO_SECRET_KEY=adminadmin" \
  minio/minio server /data --console-address ":9090" --address ":9000"
```

2. 服务配置：
   - 控制台：http://localhost:9090
   - API：http://localhost:9000
   - 凭证：
     - Access Key：admin
     - Secret Key：adminadmin
   - 存储：创建 `default` 存储桶并设置公开访问权限

3. 首选项配置：
   ```
   Asset/Publish/Host@Editor = http://localhost:9000
   Asset/Publish/Bucket@Editor = default
   Asset/Publish/Access@Editor = admin
   Asset/Publish/Secret@Editor = adminadmin
   ```

#### 2.2 发布流程

```mermaid
stateDiagram-v2
    direction LR
    读取发布配置 --> 获取远端清单
    获取远端清单 --> 对比本地清单
    对比本地清单 --> 发布差异文件
```

发布时根据清单对比结果进行增量上传：
- 新增文件：`文件名@MD5`
- 修改文件：`文件名@MD5`
- 清单文件：`Manifest.db` 和 `Manifest.db@MD5`（用于版本记录）

## 常见问题

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可证](../LICENSE.md)
