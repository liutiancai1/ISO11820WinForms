# 附录B 工业软件相关标准与规范

> 本附录汇集前14章涉及的核心标准和编码规范，作为开发本教材案例项目的参考手册。

---

## B.1 ISO 11820:2020 标准核心条款

> **对应章节**：第3章（需求映射）、第13章（完整案例）

本教材案例基于**ISO 11820:2020 Reaction to fire tests — Non-combustibility test**。

### 试验装置要求（Clause 4）

| 组件 | 要求 | 软件数据结构 |
|------|------|-------------|
| 加热炉 | 维持750±5℃ | PID控制目标值 |
| 炉内热电偶 | 直径1.5mm，位置固定 | `T_furnace`（`Sensor.cs`） |
| 样内热电偶 | 位于试样中心 | `T_center`（`Sensor.cs`） |
| 样面热电偶 | 接触试样表面 | `T_surface`（`Sensor.cs`） |

### 试验程序（Clause 7）

| 步骤 | 标准要求 | 软件实现 | 代码位置 |
|------|----------|----------|----------|
| 校准 | 10分钟内漂移<2℃ | `CheckDrift()`漂移算法 | `Core/TestMaster1.cs` |
| 终止 | 记录满60分钟或温度平衡 | 状态机`Recording`→`Complete` | `Core/TestMaster1.cs` |

### 结果判定（Clause 8）

材料被判定为**不燃**，需同时满足：

| 指标 | 限值 | 软件实现 |
|------|------|----------|
| 炉内平均温升 | ΔT ≤ 50℃ | `CheckTerminateCriteria` |
| 表面平均温升 | ΔT ≤ 50℃ | `CheckTerminateCriteria` |
| 持续火焰时间 | t ≤ 20s | `FlameAnalyzer.cs` |
| 质量损失率 | Δm ≤ 50% | 手动输入 + 自动计算 |

---

## B.2 Modbus RTU 通信协议

> **对应章节**：第4章（技术选型）、第5章（通信模块设计）

### 常用功能码

| 功能码 | 名称 | 用途 | 项目中的使用 |
|:---|:---|:---|:---|
| **03** | Read Holding Registers | 读保持寄存器 | 读温度设定值 |
| **04** | Read Input Registers | 读输入寄存器 | 读实时温度 |
| **06** | Write Single Register | 写单个寄存器 | 写PID参数 |

### 报文帧结构

```
+----------+----------+------------------+------------+
| 地址域   | 功能码    |      数据域       | CRC校验    |
| 1 Byte   | 1 Byte   |     N Bytes      | 2 Bytes    |
+----------+----------+------------------+------------+
```

> ⚠️ **注意：字节序陷阱**
>
> Modbus标准使用**Big-Endian**（高位在前），但部分PLC使用Little-Endian。项目中`DaqWorker.cs`的`DoHardwareWork()`方法需要注意大小端转换。

---

## B.3 C#编码规范

> **对应章节**：第7章（代码审查清单）、第12章（代码异味）

### 命名规范

| 类型 | 命名风格 | 项目中的示例 |
|------|----------|-------------|
| 类名/方法名 | `PascalCase` | `ApparatusManipulator`, `StartTesting` |
| 变量/参数 | `camelCase` | `sensorData`, `targetTemp` |
| 私有字段 | `_camelCase` | `_serialPort`, `_isScanning` |
| 常量 | `PascalCase` | `MaxRetryCount` |
| 接口 | `IPascalCase` | `ITestMaster`（`Core/ITestMaster.cs`） |

### 注释规范

所有`public`方法和属性必须写XML文档注释：

```csharp
/// <summary>
/// 计算指定时间窗口内的温度漂移量。
/// </summary>
/// <param name="windowMinutes">滑动窗口长度（分钟）</param>
/// <returns>漂移量（℃）</returns>
public double CalculateDrift(int windowMinutes) { ... }
```

### 异常处理规范（第5章）

```csharp
// ❌ 严禁吞掉异常
catch (Exception) { }

// ✅ 记录日志后重新抛出或降级处理
catch (Exception ex)
{
    Log.Error(ex, "读取传感器失败");
    throw; // 或进行降级处理
}
```

---

## B.4 界面设计规范

> **对应章节**：第8章 工业软件界面设计与实现

### 工业软件色彩规范（参考ISO 9241）

| 状态 | 颜色 | 色值 | 项目中的使用 |
|------|------|------|-------------|
| 🟢 正常/运行 | 绿色 | `#32CD32` | 通信状态灯 |
| 🟡 警告/就绪 | 黄色 | `#FFD700` | Ready状态 |
| 🔴 故障/停止 | 红色 | `#FF4500` | 超温报警 |
| ⚪ 离线/未知 | 灰色 | `#A9A9A9` | 串口未连接 |

### 布局原则

| 原则 | 要求 | 项目中的体现 |
|------|------|-------------|
| F型布局 | 关键信息放左上或顶部 | `MainForm`温度显示在顶部 |
| 大按钮 | 主操作按钮≥60px高度 | 适配触摸屏（第11章部署环境） |
| 防误触 | 危险操作需二次确认 | "停止加热"弹出确认对话框 |

---

## B.5 版本控制规范

> **对应章节**：第6章 软件配置管理与版本控制

### 语义化版本号

| 格式 | 含义 | 示例 |
|------|------|------|
| `Major.Minor.Patch` | 主版本.次版本.修订号 | `v1.0.0` → `v1.0.1`（修Bug） → `v1.1.0`（加功能） |

### Git分支策略

| 分支 | 用途 | 合并方式 |
|------|------|----------|
| `master` | 仅存放可发布的稳定版本 | 通过PR合并 |
| `develop` | 日常开发集成 | 特性分支合入 |
| `feature/xxx` | 每人独立的开发分支 | 完成后PR到develop |

### .gitignore必须项

```
bin/
obj/
*.user
appsettings.Development.json   # 第11章：生产密码不入库
```
