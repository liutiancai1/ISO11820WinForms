#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Modbus RTU Slave 模拟器 - 用于模拟 ISO11820 PID 温控器
需要安装: pip install pymodbus pyserial
"""

from pymodbus.server import StartSerialServer
from pymodbus.device import ModbusDeviceIdentification
from pymodbus.datastore import ModbusSequentialDataBlock, ModbusSlaveContext, ModbusServerContext
import time
import threading
import random

class TemperatureSimulator:
    """温度曲线模拟器"""
    def __init__(self, context):
        self.context = context
        self.slave_id = 1
        self.current_temp = 250  # 初始温度 25.0°C
        self.target_temp = 7500  # 目标温度 750.0°C
        self.pid_output = 0
        self.manual_output = 0
        self.control_mode = 3  # 3=手动, 4=PID
        self.is_heating = False
        self.elapsed_time = 0
        
    def start(self):
        """启动温度模拟线程"""
        self.thread = threading.Thread(target=self._simulate_loop, daemon=True)
        self.thread.start()
        print("温度模拟器已启动")
        
    def _simulate_loop(self):
        """模拟循环（每秒更新一次）"""
        while True:
            time.sleep(1)
            self.elapsed_time += 1
            
            # 读取主站写入的控制模式
            values = self.context[self.slave_id].getValues(3, 0x0038, count=1)
            if values:
                self.control_mode = values[0]
            
            # 读取手动输出值
            values = self.context[self.slave_id].getValues(3, 0x0002, count=1)
            if values:
                self.manual_output = values[0]
                
            # 判断是否加热
            self.is_heating = self.manual_output > 0 or self.control_mode == 4
            
            # 更新温度
            self._update_temperature()
            
            # 写入当前温度到寄存器 0x0102 (258)
            self.context[self.slave_id].setValues(3, 0x0102, [self.current_temp])
            
            # 写入PID输出到寄存器 0x0101 (257)
            self.context[self.slave_id].setValues(3, 0x0101, [self.pid_output])
            
            # 每10秒输出一次状态
            if self.elapsed_time % 10 == 0:
                print(f"[{self.elapsed_time}s] 温度: {self.current_temp/10:.1f}°C, "
                      f"模式: {'PID' if self.control_mode == 4 else '手动'}, "
                      f"输出: {self.manual_output}, "
                      f"PID输出: {self.pid_output}")
    
    def _update_temperature(self):
        """更新温度值"""
        if self.is_heating:
            if self.current_temp < self.target_temp - 50:
                # 升温阶段：约 5°C/秒
                heating_rate = 50 + random.randint(-5, 5)
                self.current_temp = min(self.current_temp + heating_rate, self.target_temp)
                # 模拟PID输出（升温时高输出）
                self.pid_output = min(25600, 20000 + random.randint(-1000, 1000))
            else:
                # 稳定阶段：750°C ± 0.5°C
                fluctuation = random.randint(-5, 5)
                self.current_temp = self.target_temp + fluctuation
                # 稳定时PID输出较低
                self.pid_output = 6400 + random.randint(-500, 500)
        else:
            # 不加热时缓慢降温
            if self.current_temp > 250:
                self.current_temp -= 5
                self.pid_output = 0

def run_server():
    """启动 Modbus RTU Slave 服务器"""
    
    # 创建数据存储区（寄存器地址 0~9999）
    store = ModbusSlaveContext(
        di=ModbusSequentialDataBlock(0, [0]*10000),  # Discrete Inputs
        co=ModbusSequentialDataBlock(0, [0]*10000),  # Coils
        hr=ModbusSequentialDataBlock(0, [0]*10000),  # Holding Registers
        ir=ModbusSequentialDataBlock(0, [0]*10000)   # Input Registers
    )
    
    context = ModbusServerContext(slaves={1: store}, single=False)
    
    # 初始化寄存器
    store.setValues(3, 0x0000, [7500])  # 目标温度 750°C
    store.setValues(3, 0x0002, [0])     # 手动输出 0%
    store.setValues(3, 0x0038, [3])     # 控制模式 手动
    store.setValues(3, 0x0101, [0])     # PID输出 0%
    store.setValues(3, 0x0102, [250])   # 当前温度 25.0°C
    
    # 启动温度模拟器
    simulator = TemperatureSimulator(context)
    simulator.start()
    
    # 设备标识
    identity = ModbusDeviceIdentification()
    identity.VendorName = 'ISO11820 Simulator'
    identity.ProductCode = 'PID-SIM'
    identity.VendorUrl = 'http://github.com'
    identity.ProductName = 'PID Temperature Controller Simulator'
    identity.ModelName = 'PID-750'
    identity.MajorMinorRevision = '1.0.0'
    
    print("=" * 60)
    print("ISO11820 PID 温控器模拟器")
    print("=" * 60)
    print("串口: COM6")
    print("波特率: 9600")
    print("数据位: 8")
    print("停止位: 1")
    print("校验位: None")
    print("Slave ID: 1")
    print("=" * 60)
    print("寄存器映射:")
    print("  0x0000 (0)   - 目标温度 (写)")
    print("  0x0002 (2)   - 手动输出 (写)")
    print("  0x0038 (56)  - 控制模式 (写)")
    print("  0x0101 (257) - PID输出 (读)")
    print("  0x0102 (258) - 当前温度 (读)")
    print("=" * 60)
    print("正在启动服务器...")
    
    # 启动服务器
    StartSerialServer(
        context=context,
        identity=identity,
        port='COM6',        # 虚拟串口
        baudrate=9600,
        bytesize=8,
        parity='N',
        stopbits=1,
        timeout=1
    )

if __name__ == "__main__":
    try:
        run_server()
    except KeyboardInterrupt:
        print("\n服务器已停止")
    except Exception as e:
        print(f"错误: {e}")
