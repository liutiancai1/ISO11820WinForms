# Modbus Poll + 虚拟串口模拟配置指南

本指南详细说明如何使用 **Modbus Poll** 和 **虚拟串口软件** 模拟 ISO11820 建筑材料不燃性试验系统的 PID 温控器通讯。

---

## 1. 环境准备

### 1.1 所需软件

| 软件 | 用途 | 下载地址 |
|------|------|----------|
| **Modbus Poll** | Modbus 主站/从站模拟器 | https://www.modbustools.com/modbus_poll.html |
| **Virtual Serial Port Driver (VSPD)** 或 **com0com** | 创建虚拟串口对 | VSPD: https://www.eltima.com/vspd/ <br> com0com: https://sourceforge.net/projects/com0com/ |

### 1.2 虚拟串口配置

1. **安装虚拟串口软件** (以 VSPD 为例)
2. **创建虚拟串口对**：
   - 打开 VSPD 管理器
   - 添加一对虚拟串口，例如 **COM5 ↔ COM6**
   - 这两个端口相互连通，发送到 COM5 的数据会从 COM6 读出

```
┌─────────────────────────────────────────────────────────┐
│                    虚拟串口连接示意                      │
├─────────────────────────────────────────────────────────┤
│                                                          │
│   ISO11820 程序                    Modbus Poll           │
│   ┌──────────┐                    ┌──────────┐          │
│   │          │    虚拟串口对      │          │          │
│   │  PidPort ├──── COM5 ↔ COM6 ───┤ Slave    │          │
│   │  (主站)  │                    │  (从站)  │          │
│   └──────────┘                    └──────────┘          │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

---

## 2. 项目配置修改

### 2.1 修改 appsettings.json

编辑项目根目录下的 `appsettings.json`，将 `PidPort` 改为虚拟串口：

```json
{
  "Hardware": {
    "PidPort": "COM5",
    "PowerPort": "COM2",
    "SensorPort": "COM3",
    "ConstPower": 2048,
    "PidTemperature": 750
  },
  "Simulation": {
    "EnableSimulation": false,
    "SimulateSensors": true,
    "SimulatePidController": false
  }
}
```

> [!IMPORTANT]
> - `PidPort` 设置为虚拟串口的一端 (COM5)
> - `SimulatePidController` 必须设为 `false`，否则程序会使用内置仿真
> - `SimulateSensors` 可以设为 `true`，这样只需要模拟 PID 控制器

---

## 3. Modbus Poll 配置

### 3.1 通讯参数设置

打开 **Modbus Poll**，进行以下配置：

**Connection → Connect (F3)**：

| 参数 | 设置值 |
|------|--------|
| Connection | Serial Port |
| Port | **COM6** (虚拟串口的另一端) |
| Mode | **RTU** |
| Baud Rate | **9600** |
| Data Bits | **8** |
| Stop Bits | **1** |
| Parity | **None** |
| Response Timeout | 1000 ms |

### 3.2 切换为 Slave 模式

> [!WARNING]
> Modbus Poll 默认是 Master 模式，需要切换到 **Slave 模式**！

1. 菜单选择 **File → New** 创建新窗口
2. 选择 **Setup → Slave Definition (F8)**
3. 配置从站参数

---

## 4. 寄存器地址表

### 4.1 项目使用的 Modbus 寄存器

根据 `ApparatusManipulator.cs` 代码分析，PID 控制器使用以下寄存器：

| 寄存器地址 (Hex) | 寄存器地址 (Dec) | 功能 | 读/写 | 数据说明 |
|------------------|------------------|------|-------|----------|
| `0x0000` | 0 | 目标温度设定值 (SV) | 写 | 温度 × 10，如 750°C = 7500 |
| `0x0002` | 2 | 手动输出比例 (MV) | 写 | 0~25600 对应 0%~100% |
| `0x0038` | 56 | 控制模式 | 写 | 3=手动, 4=PID自动 |
| `0x0101` | 257 | PID输出值 | 读 | 0~25600 对应 0%~100% |
| `0x0102` | 258 | 当前温度 (PV) | 读 | 温度 × 10，如 745.2°C = 7452 |

### 4.2 Modbus Poll 窗口配置

在 Modbus Poll 中创建以下显示窗口：

**窗口1 - 读取寄存器 (程序会读取这些值)**：

| 设置项 | 值 |
|--------|-----|
| Slave ID | 1 |
| Function | 03 Read Holding Registers |
| Address | 256 |
| Quantity | 10 |

**窗口2 - 写入寄存器 (程序会写入这些值)**：

| 设置项 | 值 |
|--------|-----|
| Slave ID | 1 |
| Function | 03 Read Holding Registers |
| Address | 0 |
| Quantity | 60 |

---

## 5. 模拟测试步骤

### 5.1 启动顺序

```
步骤1: 启动虚拟串口软件，确认 COM5↔COM6 已创建
   ↓
步骤2: 启动 Modbus Poll，连接 COM6，配置为 Slave 模式
   ↓
步骤3: 在 Modbus Poll 中预设寄存器初值
   ↓
步骤4: 启动 ISO11820 程序
   ↓
步骤5: 观察 Modbus Poll 中的数据变化
```

### 5.2 预设寄存器初值

在启动程序前，在 Modbus Poll 的 Slave 窗口中设置以下初值：

| 地址 | 初值 | 说明 |
|------|------|------|
| 257 (0x0101) | 0 | PID输出初始为0 |
| 258 (0x0102) | 250 | 初始温度25.0°C |

### 5.3 动态模拟温度变化

在试验过程中，可以手动修改寄存器值模拟温度变化：

1. **模拟升温过程**：
   - 逐步增加地址 258 的值：250 → 2000 → 4000 → 6000 → 7450
   - 每次增加间隔约10秒

2. **模拟温度稳定**：
   - 保持地址 258 的值在 7450~7550 之间小幅波动

3. **观察程序写入**：
   - 地址 0 (目标温度) 应显示 7500
   - 地址 2 (手动输出) 会根据加热阶段变化
   - 地址 56 (控制模式) 会在 3/4 之间切换

---

## 6. 常见问题排查

### 6.1 通讯超时

**现象**：程序日志显示 "Modbus通信超时"

**检查**：
1. 虚拟串口对是否正确创建
2. Modbus Poll 是否连接到正确的端口 (COM6)
3. 波特率等参数是否匹配 (9600, 8N1)
4. Modbus Poll 是否处于 Slave 模式

### 6.2 数据不更新

**现象**：Modbus Poll 中看不到程序写入的数据

**检查**：
1. 确认 `appsettings.json` 中 `SimulatePidController` 为 `false`
2. 确认程序连接的是虚拟串口 (COM5)

### 6.3 CRC 校验错误

**现象**：Modbus Poll 显示 CRC 错误

**原因**：可能是数据传输中断或参数不匹配

**解决**：重新检查通讯参数设置

---

## 7. 高级用法：自动化脚本模拟

如果需要自动模拟温度曲线，可以使用 Modbus Poll 的脚本功能或编写独立的 Modbus Slave 程序。

### 7.1 温度曲线模拟公式

```
// 升温阶段 (0~150秒): 温度从25°C升至750°C
if (时间 < 150) {
    温度 = 25 + 时间 * 4.83  // 约4.83°C/秒
}
// 稳定阶段 (150秒后): 750°C ± 0.5°C
else {
    温度 = 750 + Random(-0.5, 0.5)
}
寄存器[258] = 温度 * 10
```

---

## 8. 参考资料

- [FluentModbus 库文档](https://github.com/Apollo3zehn/FluentModbus)
- [Modbus RTU 协议规范](https://modbus.org/specs.php)
- 项目代码：`Core/ApparatusManipulator.cs`

