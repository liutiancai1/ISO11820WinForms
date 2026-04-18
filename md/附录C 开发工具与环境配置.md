# 附录C 开发工具与环境配置

> 工欲善其事，必先利其器。本附录指导搭建开发本教材案例所需的全部软硬件环境，每个工具均标注在前14章中首次使用的章节。

---

## C.1 开发环境

### Visual Studio 2022

> **首次使用**：第1章

| 配置项 | 操作 |
|--------|------|
| **下载** | [Visual Studio官网](https://visualstudio.microsoft.com/) → Community版（免费） |
| **工作负载** | ✅ .NET桌面开发（C#, WinForms） |
| | ✅ 数据存储和处理（SQL Server工具，可选） |

**推荐插件**：

| 插件 | 用途 | 对应章节 |
|------|------|----------|
| GitHub Extension | 版本控制 | 第6章 |
| Markdown Editor | 编写文档 | 第7章 |
| CodeMaid | 代码清理 | 第12章 |

### .NET 6 SDK

| 环境 | 需要安装的版本 |
|------|--------------|
| **开发机** | .NET 6 SDK（包含Runtime + 编译工具） |
| **部署机** | .NET Desktop Runtime 6.0.x（第11章） |

---

## C.2 数据库环境

> **首次使用**：第5章（数据库设计）

### SQL Server Express

| 配置项 | 推荐设置 | 说明 |
|--------|----------|------|
| **版本** | SQL Server Express 2019/2022 | 免费，适合单机 |
| **认证模式** | 混合模式（SQL + Windows） | 第11章部署需要SQL认证 |
| **管理工具** | SSMS 或 Azure Data Studio | 表结构查看和SQL调试 |

### 连接字符串（appsettings.json）

```json
{
  "ConnectionStrings": {
    "ISO11820": "Server=.\\SQLEXPRESS;Database=ISO11820;User Id=sa;Password=***;TrustServerCertificate=true;"
  }
}
```

> ⚠️ **注意**：回顾第6章和第11章——生产密码不要提交到Git仓库，开发时可使用`appsettings.Development.json`。

---

## C.3 版本控制工具

> **首次使用**：第6章

### Git安装与配置

```bash
# 安装后执行初始配置
git config --global user.name "Your Name"
git config --global user.email "your.email@example.com"
git config --global core.quotepath false   # 解决中文乱码
```

### 常用命令速查

| 命令 | 用途 | 对应第6章内容 |
|------|------|-------------|
| `git init` | 初始化仓库 | 创建项目 |
| `git add .` | 暂存所有文件 | — |
| `git commit -m "feat: ..."` | 提交 | 约定式提交 |
| `git checkout -b feature/xxx` | 创建特性分支 | Git Flow |
| `git merge feature/xxx` | 合并分支 | PR合并 |
| `git log --oneline -10` | 查看最近10条提交 | — |

---

## C.4 串口调试工具

> **首次使用**：第5章（通信模块设计）

### 虚拟串口

| 工具 | 功能 | 用途 |
|------|------|------|
| **VSPD** / **com0com** | 成对创建虚拟串口（COM1↔COM2） | 无真实硬件时的开发调试 |

**配置方法**：应用程序连接`COM1`，模拟器连接`COM2`，数据双向透传。

### Modbus调试工具

| 工具 | 角色 | 用途 | 对应章节 |
|------|------|------|----------|
| **Modbus Poll** | Master（主机） | 测试真实硬件 | 第5章联调 |
| **Modbus Slave** | Slave（从机） | 模拟温控器 | 第7章Mock |

> 📘 **知识拓展：项目内置仿真器**
>
> 本教材在`Services/SensorSimulator.cs`中提供了内置仿真器（第9章SIL仿真），无需外部工具即可运行。但手动操作一遍Modbus Poll/Slave对理解通信协议非常有帮助。

---

## C.5 NuGet包依赖清单

> **对应章节**：第4章（技术选型）

以下是项目使用的核心NuGet包：

| 包名 | 版本 | 用途 | 对应章节 |
|------|------|------|----------|
| `NModbus4` | 2.x | Modbus RTU通信 | 第4-5章 |
| `OxyPlot.WindowsForms` | 2.x | 实时曲线绘制 | 第8章 |
| `Serilog` + `Serilog.Sinks.File` | 3.x | 结构化日志 | 第5章 + 第12章 |
| `ClosedXML` | 0.x | Excel报告生成 | 第8章 |
| `NPOI` | 2.x | Word报告模板 | `ReportTemplateEngine.cs` |
| `Microsoft.EntityFrameworkCore` | 6.x | ORM数据库访问 | 第5章 |
| `Emgu.CV` | 4.x | 计算机视觉（火焰检测） | `FlameAnalyzer.cs` |

**安装方式**（在VS的包管理器控制台）：

```powershell
Install-Package NModbus4
Install-Package OxyPlot.WindowsForms
Install-Package Serilog.Sinks.File
```
