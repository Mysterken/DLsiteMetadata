#!/bin/bash

# DLsite游戏管理器启动脚本

echo "======================================"
echo "  DLsite游戏管理器"
echo "======================================"
echo ""

# 检查Python版本
python_version=$(python3 --version 2>&1 | grep -oP '\d+\.\d+')
required_version="3.8"

if [ "$(printf '%s\n' "$required_version" "$python_version" | sort -V | head -n1)" != "$required_version" ]; then
    echo "错误: 需要Python 3.8或更高版本"
    echo "当前版本: Python $python_version"
    exit 1
fi

# 检查虚拟环境
if [ ! -d "venv" ]; then
    echo "创建虚拟环境..."
    python3 -m venv venv
fi

# 激活虚拟环境
echo "激活虚拟环境..."
source venv/bin/activate

# 安装/更新依赖
echo "检查依赖..."
pip install -q -r requirements.txt

# 创建必要的目录
mkdir -p static/images/games
mkdir -p logs

# 启动应用
echo ""
echo "======================================"
echo "启动Web服务器..."
echo "访问地址: http://localhost:5000"
echo "按 Ctrl+C 停止服务器"
echo "======================================"
echo ""

python app.py
