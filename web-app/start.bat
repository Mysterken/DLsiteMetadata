@echo off
chcp 65001 >nul
echo ======================================
echo   DLsite游戏管理器
echo ======================================
echo.

REM 检查Python是否安装
python --version >nul 2>&1
if errorlevel 1 (
    echo 错误: 未找到Python，请先安装Python 3.8或更高版本
    pause
    exit /b 1
)

REM 检查虚拟环境
if not exist "venv" (
    echo 创建虚拟环境...
    python -m venv venv
)

REM 激活虚拟环境
echo 激活虚拟环境...
call venv\Scripts\activate.bat

REM 安装/更新依赖
echo 检查依赖...
pip install -q -r requirements.txt

REM 创建必要的目录
if not exist "static\images\games" mkdir static\images\games
if not exist "logs" mkdir logs

REM 启动应用
echo.
echo ======================================
echo 启动Web服务器...
echo 访问地址: http://localhost:5000
echo 按 Ctrl+C 停止服务器
echo ======================================
echo.

python app.py
pause
