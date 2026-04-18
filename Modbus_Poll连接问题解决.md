# Modbus Poll 连接问题解决

## 问题：显示 "No Connection"

### 解决步骤

#### 1. 连接串口
- 点击菜单：**Connection → Connect**
- 或按快捷键：**F3**

#### 2. 配置连接参数

在弹出的对话框中设置：

```
┌─────────────────────────────────────────────┐
│ Connection Setup                            │
├─────────────────────────────────────────────┤
│ Connection:     [Serial Port]        ▼      │
│ Serial Port:    [COM6]               ▼      │  ← 选择 COM6
│ Baud:           [9600]               ▼      │
│ Data Bits:      [8]                  ▼      │
│ Parity:         [None]               ▼      │
│ Stop Bits:      [1]                  ▼      │
│ Mode:           [RTU]                ▼      │  ← 必须是 RTU
│                                              │
│ Response timeout: [1000] ms                 │
│                                              │
│         [  OK  ]        [ Cancel ]           │
└─────────────────────────────────────────────┘
```

#### 3. 点击 OK

连接成功后，状态栏会从 "No Connection" 变为：
```
Connected to COM6, 9600, 8, N, 1, RTU
```

---

## 如果无法连接 COM6

### 检查1：虚拟串口是否存在

打开 **设备管理器**：
1. 按 `Win + X`，选择 "设备管理器"
2. 展开 "端口 (COM 和 LPT)"
3. 查看是否有 COM6

如果没有 COM6：

#### 方法A：创建虚拟串口（com0com）

1. 打开 "开始菜单" → 搜索 "Setup Command Prompt"
2. 右键 "以管理员身份运行"
3. 输入命令：
```cmd
install PortName=COM5 PortName=COM6
```

4. 验证：
```cmd
list
```

#### 方法B：使用其他端口

如果 COM6 被占用，可以使用其他端口：

1. 创建不同的虚拟串口对：
```cmd
install PortName=COM10 PortName=COM11
```

2. 修改配置：
   - Modbus Poll 连接 COM11
   - `appsettings.json` 中 `PidPort` 改为 COM10

---

## 检查2：端口是否被占用

### 使用 PowerShell 检查

```powershell
# 查看所有串口
[System.IO.Ports.SerialPort]::GetPortNames()

# 尝试打开 COM6
$port = New-Object System.IO.Ports.SerialPort COM6,9600,None,8,One
try {
    $port.Open()
    Write-Host "COM6 可用" -ForegroundColor Green
    $port.Close()
} catch {
    Write-Host "COM6 被占用或不存在: $_" -ForegroundColor Red
}
```

### 关闭占用端口的程序

如果 COM6 被占用：
1. 关闭 ISO11820 程序
2. 关闭其他可能使用串口的程序
3. 重新连接 Modbus Poll

---

## 检查3：Modbus Poll 配置

### 确认 Slave 模式已启用

1. 菜单：**Setup → Slave Definition** (F8)
2. 确认已勾选：**☑ Enable Slave Simulation**
3. Slave ID 设置为：**1**

---

## 完整测试流程

### 1. 确认虚拟串口存在
```cmd
# 打开命令提示符
mode COM5
mode COM6
```

如果显示端口信息，说明端口存在。

### 2. 按顺序启动

```
步骤1: 启动 Modbus Poll
   ↓
步骤2: Connection → Connect (F3)
   ↓
步骤3: 选择 COM6, 9600, RTU
   ↓
步骤4: Setup → Slave Definition (F8)
   ↓
步骤5: 勾选 Enable Slave Simulation
   ↓
步骤6: 启动 ISO11820 程序
```

### 3. 验证连接

连接成功的标志：
- ✅ 状态栏显示 "Connected to COM6..."
- ✅ 状态栏不再显示 "No Connection"
- ✅ Tx 和 Err 计数器开始变化

---

## 常见错误

### 错误1：Access Denied (访问被拒绝)
**原因**：端口被其他程序占用
**解决**：
1. 关闭 ISO11820 程序
2. 重新连接 Modbus Poll
3. 再启动 ISO11820 程序

### 错误2：Port Not Found (端口未找到)
**原因**：虚拟串口未创建
**解决**：按照上面的方法创建虚拟串口

### 错误3：Invalid Port (无效端口)
**原因**：选择了不存在的端口
**解决**：在设备管理器中确认可用的端口号

---

## 快速诊断命令

### 检查虚拟串口
```cmd
# 列出所有串口
mode

# 查看 com0com 配置
cd "C:\Program Files (x86)\com0com"
setupc list
```

### 重新创建虚拟串口
```cmd
# 删除旧的
setupc remove 0

# 创建新的
setupc install PortName=COM5 PortName=COM6
```

---

## 推荐配置

### 最稳定的配置

```
虚拟串口对: COM5 ↔ COM6
ISO11820:   COM5
Modbus Poll: COM6
波特率:     9600
模式:       RTU
Slave ID:   1
```

### 备用配置（如果 COM5/6 不可用）

```
虚拟串口对: COM10 ↔ COM11
ISO11820:   COM10
Modbus Poll: COM11
```

---

## 测试连接的简单方法

### 使用 Python 测试

```python
import serial

# 测试 COM6
try:
    port = serial.Serial('COM6', 9600, timeout=1)
    print(f"✓ COM6 可用")
    port.close()
except Exception as e:
    print(f"✗ COM6 不可用: {e}")
```

### 使用 C# 测试

```csharp
using System.IO.Ports;

try
{
    var port = new SerialPort("COM6", 9600, Parity.None, 8, StopBits.One);
    port.Open();
    Console.WriteLine("✓ COM6 可用");
    port.Close();
}
catch (Exception ex)
{
    Console.WriteLine($"✗ COM6 不可用: {ex.Message}");
}
```

---

## 联系支持

如果以上方法都无法解决，请提供：
1. 设备管理器中的端口列表截图
2. Modbus Poll 的错误消息
3. `setupc list` 命令的输出
