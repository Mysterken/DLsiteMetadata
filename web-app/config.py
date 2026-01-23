"""
DLsite Game Manager 配置文件
"""
import os

class Config:
    """应用配置"""

    # 基础配置
    SECRET_KEY = os.environ.get('SECRET_KEY') or 'dev-secret-key-change-in-production'
    BASE_DIR = os.path.abspath(os.path.dirname(__file__))

    # 数据库配置
    SQLALCHEMY_DATABASE_URI = os.environ.get('DATABASE_URL') or \
        f'sqlite:///{os.path.join(BASE_DIR, "dlsite_games.db")}'
    SQLALCHEMY_TRACK_MODIFICATIONS = False

    # DLsite爬虫配置
    DLSITE_BASE_URL = 'https://www.dlsite.com'
    DLSITE_SEARCH_URL = 'https://www.dlsite.com/maniax/fsr/=/language/jp/sex_category%5B0%5D/male/keyword/{keyword}/per_page/100/page/{page}'
    DLSITE_WORK_URL = 'https://www.dlsite.com/maniax/work/=/product_id/{work_id}.html'

    # 速率限制配置（避免被封）
    RATE_LIMIT_CALLS = 1  # 每个时间窗口的请求次数
    RATE_LIMIT_PERIOD = 2  # 时间窗口（秒）
    REQUEST_TIMEOUT = 10  # 请求超时时间（秒）
    RETRY_TIMES = 3  # 失败重试次数
    RETRY_DELAY = 5  # 重试延迟（秒）

    # 用户代理配置（模拟浏览器）
    USER_AGENTS = [
        'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
        'Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0',
        'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
    ]

    # 图片保存配置
    IMAGES_DIR = os.path.join(BASE_DIR, 'static', 'images', 'games')
    SAVE_IMAGES_LOCALLY = True  # 是否本地保存图片

    # 分页配置
    GAMES_PER_PAGE = 20

    # 语言配置
    DEFAULT_LANGUAGE = 'zh_CN'  # 默认中文
    SUPPORTED_LANGUAGES = ['ja_JP', 'zh_CN', 'zh_TW', 'en_US', 'ko_KR']
