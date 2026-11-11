# 模板安装和使用

## 安装模板

```bash
cd CSharp-Backend-Template
dotnet new install .
dotnet new wt-backend '-?'
```

更新模板后，重新执行安装命令即可覆盖。

## 创建项目

```bash
mkdir Acme.Order.Backend
cd Acme.Order.Backend
dotnet new wt-backend -n Acme.Order.Backend
```

`-n` 会统一替换 `.sln`、`.csproj`、项目目录、`RootNamespace`、C# Namespace、Docker 项目路径、启动 DLL 和默认服务名称。

## 可选服务

```bash
# 仅 S3/MinIO 文件存储
dotnet new wt-backend -n Acme.Api --FileStorage true

# 仅阿里云短信
dotnet new wt-backend -n Acme.Api --SMS true

# 两者都生成
dotnet new wt-backend -n Acme.Api --FileStorage true --SMS true
```

未选择的服务不会生成对应源码目录、配置段或 NuGet 依赖。选择后，代码位于 `Services/FileStorage` 或 `Services/SMS`，必填配置会在启动时强制校验。

未选择 SMS 时，手机验证码登录仍可使用，验证码会通过 Warning 日志输出，仅供本地测试。

## 运行配置

仓库只提交 `appsettings.sample.json`。实际运行使用被 Git 忽略的 `appsettings.json`：

```bash
cp BackendTemplate/appsettings.sample.json BackendTemplate/appsettings.json
dotnet run --project BackendTemplate
```

也可以把配置放在项目外：

```bash
dotnet run --project BackendTemplate -- \
  --configFile /absolute/path/appsettings.json
```

配置优先级为命令行参数高于环境变量，环境变量高于 JSON。嵌套配置使用双下划线：

```bash
export Database__Provider=PostgreSql
export Database__ConnectionString='Host=db.example.com;Port=5432;Database=app;Username=app;Password=change-me'
```

不要提交真实连接串、AccessKey、短信密钥或 SQLite 数据文件。

## 数据库

SQLite 配置：

```json
{
  "Database": {
    "Provider": "Sqlite",
    "ConnectionString": "Data Source=/tmp/backend-template.db"
  }
}
```

PostgreSQL 配置：

```json
{
  "Database": {
    "Provider": "PostgreSql",
    "ConnectionString": "Host=localhost;Port=5432;Database=app;Username=app;Password=change-me"
  }
}
```

SQLite 模式在 `BuildApp` 时执行 `EnsureCreated`，适合本地和集成测试。PostgreSQL 不会自动建库或迁移，部署前应显式执行 EF Core Migration。

## Docker

镜像不包含任何 `appsettings*.json`。运行时挂载配置：

```bash
docker build -t backend-template .
docker run --rm -p 8080:8080 \
  -v /absolute/path/appsettings.json:/etc/backend-template/appsettings.json:ro \
  backend-template
```

数据库作为外部服务使用；模板不提供 Compose，也不在镜像内启动数据库服务。

## 部署脚本样例

生成项目包含 `deploy.sh`。脚本中的 `SERVICE_NAME` 会由模板参数自动替换；执行前需要手工填写 SSH 主机占位符：

```bash
SERVER_NAME="where-test"
```

`SERVER_NAME` 可以是主机名，也可以是 `~/.ssh/config` 中的别名。占位符为空时脚本会直接退出，不会执行构建、上传或远程操作。
