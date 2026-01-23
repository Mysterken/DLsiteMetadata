# DLsite游戏管理器

一个功能强大的DLsite游戏信息抓取和管理Web应用，支持本地游戏库管理、收藏分类和智能推荐过滤。

## ✨ 主要特性

- 🎮 **智能爬虫**：自动抓取DLsite游戏详细信息，支持多语言
- 🚀 **速率控制**：自适应速率限制，避免被服务器封禁
- 📚 **游戏管理**：收藏、分类、屏蔽功能，轻松管理你的游戏库
- 🔍 **强大搜索**：多条件筛选和搜索，快速找到想要的游戏
- 📊 **统计分析**：直观的数据统计和可视化展示
- 🎨 **现代界面**：响应式设计，支持移动端访问
- ⚡ **批量操作**：支持批量抓取和管理游戏

## 🛠️ 技术栈

- **后端**：Python 3.8+ / Flask
- **数据库**：SQLite (可升级为PostgreSQL)
- **前端**：Bootstrap 5 + Vanilla JavaScript
- **爬虫**：Requests + BeautifulSoup4
- **其他**：SQLAlchemy, APScheduler

## 📦 安装步骤

### 1. 克隆仓库

```bash
git clone <repository-url>
cd DLsiteMetadata/web-app
```

### 2. 创建虚拟环境

```bash
python -m venv venv

# Windows
venv\Scripts\activate

# Linux/Mac
source venv/bin/activate
```

### 3. 安装依赖

```bash
pip install -r requirements.txt
```

### 4. 配置环境变量（可选）

创建 `.env` 文件：

```bash
SECRET_KEY=your-secret-key-here
DATABASE_URL=sqlite:///dlsite_games.db
```

### 5. 初始化数据库

首次运行会自动创建数据库和默认分类：

```bash
python app.py
```

### 6. 访问应用

打开浏览器访问：`http://localhost:5000`

## 🎯 使用指南

### 快速添加游戏

1. 在首页左侧的"快速添加"面板输入Work ID（如：RJ123456）
2. 点击"抓取游戏"按钮
3. 等待爬虫自动获取游戏信息并保存

### 批量抓取

1. 点击"批量抓取"按钮
2. 在文本框中每行输入一个Work ID
3. 选择语言（默认简体中文）
4. 点击"开始抓取"，系统会在后台自动处理
5. 在"任务管理"页面查看进度

### 游戏管理

- **收藏**：点击⭐图标将游戏加入收藏
- **标记拥有**：点击✓图标标记已购买的游戏
- **屏蔽**：点击🚫图标屏蔽不感兴趣的游戏
- **分类**：在游戏详情页添加到自定义分类

### 搜索和筛选

- 支持按游戏名称、RJ号、社团名搜索
- 可按收藏、拥有、屏蔽状态筛选
- 支持按分类筛选
- 多种排序方式：添加时间、发售时间、评分、销量等

## ⚙️ 配置说明

在 `config.py` 中可以调整以下配置：

### 爬虫速率控制

```python
RATE_LIMIT_CALLS = 1      # 每个时间窗口的请求次数
RATE_LIMIT_PERIOD = 2     # 时间窗口（秒）
REQUEST_TIMEOUT = 10      # 请求超时时间
RETRY_TIMES = 3           # 失败重试次数
```

**重要提示**：
- 默认配置为每2秒1次请求，这是相对保守的设置
- 如果需要加快速度，可以调整为 `RATE_LIMIT_PERIOD = 1.5`
- **不建议设置过快**，可能导致IP被封禁
- 系统会自动根据服务器响应调整速率

### 多语言支持

```python
DEFAULT_LANGUAGE = 'zh_CN'  # 默认语言
SUPPORTED_LANGUAGES = ['ja_JP', 'zh_CN', 'zh_TW', 'en_US', 'ko_KR']
```

### 分页设置

```python
GAMES_PER_PAGE = 20  # 每页显示的游戏数量
```

## 🔒 隐私和安全

- ✅ 所有数据存储在本地SQLite数据库
- ✅ 不收集或上传任何用户数据
- ✅ 图片可选本地保存或使用DLsite链接
- ✅ 支持自定义User-Agent避免被识别为爬虫

## 📊 API接口

### 游戏管理

```
GET    /api/games              # 获取游戏列表
GET    /api/games/<work_id>    # 获取游戏详情
PUT    /api/games/<work_id>    # 更新游戏信息
DELETE /api/games/<work_id>    # 删除游戏
```

### 爬虫操作

```
POST /api/scrape/search   # 搜索游戏
POST /api/scrape/game     # 抓取单个游戏
POST /api/scrape/batch    # 批量抓取
```

### 分类管理

```
GET    /api/categories              # 获取分类列表
POST   /api/categories              # 创建分类
DELETE /api/categories/<id>         # 删除分类
POST   /api/games/<id>/categories   # 添加到分类
DELETE /api/games/<id>/categories/<category_id>  # 从分类移除
```

## 🚧 常见问题

### Q: 抓取速度太慢？

A: 可以适当调整 `config.py` 中的 `RATE_LIMIT_PERIOD`，但不建议设置过快。建议使用批量抓取功能，让程序后台运行。

### Q: 无法访问某些游戏页面？

A: 部分成人内容需要登录DLsite账号。本工具目前不支持登录，只能抓取公开信息。

### Q: 图片无法显示？

A: 确保网络连接正常，DLsite的图片服务器可能有访问限制。可以启用本地图片保存功能。

### Q: 数据库如何备份？

A: 直接复制 `dlsite_games.db` 文件即可。建议定期备份。

### Q: 支持哪些Work ID格式？

A: 支持 RJ、RE、BJ、VJ 开头，6-8位数字的Work ID。

## 🔄 更新日志

### v1.0.0 (2024-01-23)

- 🎉 首次发布
- ✨ 支持游戏信息抓取
- ✨ 实现收藏、分类、屏蔽功能
- ✨ 添加速率限制保护
- ✨ 批量抓取任务管理
- ✨ 响应式Web界面

## 🤝 贡献

欢迎提交Issue和Pull Request！

## 📄 许可证

本项目仅供学习和个人使用，请勿用于商业用途。

## ⚠️ 免责声明

- 本工具仅用于个人游戏库管理
- 请遵守DLsite的服务条款和robots.txt
- 请勿过度频繁请求，避免对服务器造成压力
- 使用本工具产生的任何后果由使用者自行承担

## 📞 联系方式

如有问题或建议，欢迎提交Issue。

---

**享受游戏管理的乐趣！** 🎮✨
