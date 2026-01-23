"""
数据库模型定义
"""
from datetime import datetime
from flask_sqlalchemy import SQLAlchemy

db = SQLAlchemy()


class Game(db.Model):
    """游戏信息表"""
    __tablename__ = 'games'

    id = db.Column(db.Integer, primary_key=True)
    work_id = db.Column(db.String(20), unique=True, nullable=False, index=True)  # RJ号码

    # 基本信息
    title = db.Column(db.String(500), nullable=False)
    title_en = db.Column(db.String(500))
    circle = db.Column(db.String(200))  # 社团名称
    description = db.Column(db.Text)
    release_date = db.Column(db.Date)
    update_date = db.Column(db.Date)

    # 分类信息
    series = db.Column(db.String(200))  # 系列
    genres = db.Column(db.Text)  # 类型标签，逗号分隔
    age_rating = db.Column(db.String(20))  # 年龄分级

    # 评分和统计
    rating = db.Column(db.Float)  # 评分 0-5
    review_count = db.Column(db.Integer, default=0)
    sale_count = db.Column(db.Integer, default=0)  # 销量

    # 人员信息（JSON格式存储）
    authors = db.Column(db.Text)  # 作者列表
    illustrators = db.Column(db.Text)  # 画师列表
    voice_actors = db.Column(db.Text)  # CV列表
    scenario_writers = db.Column(db.Text)  # 剧本列表
    music_creators = db.Column(db.Text)  # 音乐列表

    # 图片和链接
    icon_url = db.Column(db.String(500))
    cover_url = db.Column(db.String(500))
    sample_images = db.Column(db.Text)  # 样图URL列表，逗号分隔
    dlsite_url = db.Column(db.String(500))

    # 其他信息
    file_size = db.Column(db.String(50))
    file_format = db.Column(db.String(200))  # 文件格式
    product_format = db.Column(db.String(200))  # 产品格式
    misc_info = db.Column(db.Text)  # 其他信息

    # 用户数据
    is_favorite = db.Column(db.Boolean, default=False)  # 是否收藏
    is_blocked = db.Column(db.Boolean, default=False)  # 是否屏蔽
    is_owned = db.Column(db.Boolean, default=False)  # 是否拥有
    user_rating = db.Column(db.Integer)  # 用户自定义评分 1-5
    user_notes = db.Column(db.Text)  # 用户备注

    # 时间戳
    created_at = db.Column(db.DateTime, default=datetime.utcnow)
    updated_at = db.Column(db.DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    last_scraped_at = db.Column(db.DateTime)  # 最后爬取时间

    # 关系
    categories = db.relationship('GameCategory', back_populates='game', cascade='all, delete-orphan')

    def to_dict(self):
        """转换为字典格式"""
        return {
            'id': self.id,
            'work_id': self.work_id,
            'title': self.title,
            'title_en': self.title_en,
            'circle': self.circle,
            'description': self.description,
            'release_date': self.release_date.isoformat() if self.release_date else None,
            'update_date': self.update_date.isoformat() if self.update_date else None,
            'series': self.series,
            'genres': self.genres.split(',') if self.genres else [],
            'age_rating': self.age_rating,
            'rating': self.rating,
            'review_count': self.review_count,
            'sale_count': self.sale_count,
            'authors': self.authors.split(',') if self.authors else [],
            'illustrators': self.illustrators.split(',') if self.illustrators else [],
            'voice_actors': self.voice_actors.split(',') if self.voice_actors else [],
            'scenario_writers': self.scenario_writers.split(',') if self.scenario_writers else [],
            'music_creators': self.music_creators.split(',') if self.music_creators else [],
            'icon_url': self.icon_url,
            'cover_url': self.cover_url,
            'sample_images': self.sample_images.split(',') if self.sample_images else [],
            'dlsite_url': self.dlsite_url,
            'file_size': self.file_size,
            'file_format': self.file_format,
            'product_format': self.product_format,
            'is_favorite': self.is_favorite,
            'is_blocked': self.is_blocked,
            'is_owned': self.is_owned,
            'user_rating': self.user_rating,
            'user_notes': self.user_notes,
            'categories': [cat.category.name for cat in self.categories],
            'created_at': self.created_at.isoformat() if self.created_at else None,
            'updated_at': self.updated_at.isoformat() if self.updated_at else None,
        }


class Category(db.Model):
    """分类表"""
    __tablename__ = 'categories'

    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(100), unique=True, nullable=False)
    description = db.Column(db.String(500))
    color = db.Column(db.String(20), default='#007bff')  # 分类颜色
    created_at = db.Column(db.DateTime, default=datetime.utcnow)

    # 关系
    games = db.relationship('GameCategory', back_populates='category', cascade='all, delete-orphan')

    def to_dict(self):
        return {
            'id': self.id,
            'name': self.name,
            'description': self.description,
            'color': self.color,
            'game_count': len(self.games)
        }


class GameCategory(db.Model):
    """游戏-分类关联表"""
    __tablename__ = 'game_categories'

    id = db.Column(db.Integer, primary_key=True)
    game_id = db.Column(db.Integer, db.ForeignKey('games.id'), nullable=False)
    category_id = db.Column(db.Integer, db.ForeignKey('categories.id'), nullable=False)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)

    # 关系
    game = db.relationship('Game', back_populates='categories')
    category = db.relationship('Category', back_populates='games')

    __table_args__ = (
        db.UniqueConstraint('game_id', 'category_id', name='unique_game_category'),
    )


class ScrapingTask(db.Model):
    """爬取任务表"""
    __tablename__ = 'scraping_tasks'

    id = db.Column(db.Integer, primary_key=True)
    task_type = db.Column(db.String(50), nullable=False)  # search, single, batch
    status = db.Column(db.String(20), default='pending')  # pending, running, completed, failed
    parameters = db.Column(db.Text)  # JSON格式存储参数
    total_items = db.Column(db.Integer, default=0)
    completed_items = db.Column(db.Integer, default=0)
    failed_items = db.Column(db.Integer, default=0)
    error_message = db.Column(db.Text)
    started_at = db.Column(db.DateTime)
    completed_at = db.Column(db.DateTime)
    created_at = db.Column(db.DateTime, default=datetime.utcnow)

    def to_dict(self):
        return {
            'id': self.id,
            'task_type': self.task_type,
            'status': self.status,
            'parameters': self.parameters,
            'total_items': self.total_items,
            'completed_items': self.completed_items,
            'failed_items': self.failed_items,
            'progress': round(self.completed_items / self.total_items * 100, 2) if self.total_items > 0 else 0,
            'error_message': self.error_message,
            'started_at': self.started_at.isoformat() if self.started_at else None,
            'completed_at': self.completed_at.isoformat() if self.completed_at else None,
            'created_at': self.created_at.isoformat() if self.created_at else None,
        }
