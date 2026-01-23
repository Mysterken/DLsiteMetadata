"""
DLsite Game Manager - Flask Web应用
"""
import os
import json
import logging
from datetime import datetime
from threading import Thread

from flask import Flask, render_template, request, jsonify
from flask_cors import CORS

from config import Config
from models import db, Game, Category, GameCategory, ScrapingTask
from scraper import DLsiteScraper

# 配置日志
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# 创建Flask应用
app = Flask(__name__)
app.config.from_object(Config)

# 初始化扩展
CORS(app)
db.init_app(app)

# 初始化爬虫
scraper = DLsiteScraper(Config)


# ==================== 初始化数据库 ====================
def init_database():
    """初始化数据库和默认数据"""
    with app.app_context():
        db.create_all()

        # 创建默认分类
        default_categories = [
            {'name': '想玩', 'description': '感兴趣的游戏', 'color': '#007bff'},
            {'name': '在玩', 'description': '正在游玩的游戏', 'color': '#28a745'},
            {'name': '已通关', 'description': '已经通关的游戏', 'color': '#6c757d'},
            {'name': '精品', 'description': '精品推荐游戏', 'color': '#ffc107'},
            {'name': '待评价', 'description': '需要评价的游戏', 'color': '#17a2b8'},
        ]

        for cat_data in default_categories:
            existing = Category.query.filter_by(name=cat_data['name']).first()
            if not existing:
                category = Category(**cat_data)
                db.session.add(category)

        db.session.commit()
        logger.info("数据库初始化完成")


# ==================== Web页面路由 ====================
@app.route('/')
def index():
    """首页 - 游戏列表"""
    return render_template('index.html')


@app.route('/game/<work_id>')
def game_detail(work_id):
    """游戏详情页"""
    return render_template('game_detail.html', work_id=work_id)


@app.route('/collection')
def collection():
    """收藏管理页"""
    return render_template('collection.html')


@app.route('/tasks')
def tasks():
    """任务管理页"""
    return render_template('tasks.html')


# ==================== API路由 - 游戏管理 ====================
@app.route('/api/games', methods=['GET'])
def get_games():
    """获取游戏列表（支持分页和筛选）"""
    try:
        # 获取查询参数
        page = request.args.get('page', 1, type=int)
        per_page = request.args.get('per_page', Config.GAMES_PER_PAGE, type=int)
        search = request.args.get('search', '')
        category_id = request.args.get('category_id', type=int)
        is_favorite = request.args.get('is_favorite', type=lambda v: v.lower() == 'true')
        is_blocked = request.args.get('is_blocked', type=lambda v: v.lower() == 'true')
        is_owned = request.args.get('is_owned', type=lambda v: v.lower() == 'true')
        sort_by = request.args.get('sort_by', 'created_at')
        order = request.args.get('order', 'desc')

        # 构建查询
        query = Game.query

        # 搜索过滤
        if search:
            query = query.filter(
                db.or_(
                    Game.title.contains(search),
                    Game.work_id.contains(search),
                    Game.circle.contains(search)
                )
            )

        # 状态过滤
        if is_favorite is not None:
            query = query.filter(Game.is_favorite == is_favorite)
        if is_blocked is not None:
            query = query.filter(Game.is_blocked == is_blocked)
        if is_owned is not None:
            query = query.filter(Game.is_owned == is_owned)

        # 分类过滤
        if category_id:
            query = query.join(GameCategory).filter(GameCategory.category_id == category_id)

        # 排序
        if order == 'desc':
            query = query.order_by(db.desc(getattr(Game, sort_by)))
        else:
            query = query.order_by(db.asc(getattr(Game, sort_by)))

        # 分页
        pagination = query.paginate(page=page, per_page=per_page, error_out=False)

        return jsonify({
            'success': True,
            'games': [game.to_dict() for game in pagination.items],
            'total': pagination.total,
            'page': page,
            'per_page': per_page,
            'pages': pagination.pages
        })

    except Exception as e:
        logger.error(f"获取游戏列表失败: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


@app.route('/api/games/<work_id>', methods=['GET'])
def get_game(work_id):
    """获取单个游戏详情"""
    try:
        game = Game.query.filter_by(work_id=work_id.upper()).first()
        if not game:
            return jsonify({'success': False, 'error': '游戏不存在'}), 404

        return jsonify({
            'success': True,
            'game': game.to_dict()
        })

    except Exception as e:
        logger.error(f"获取游戏详情失败: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


@app.route('/api/games/<work_id>', methods=['PUT'])
def update_game(work_id):
    """更新游戏信息（用户数据）"""
    try:
        game = Game.query.filter_by(work_id=work_id.upper()).first()
        if not game:
            return jsonify({'success': False, 'error': '游戏不存在'}), 404

        data = request.get_json()

        # 更新用户数据
        if 'is_favorite' in data:
            game.is_favorite = data['is_favorite']
        if 'is_blocked' in data:
            game.is_blocked = data['is_blocked']
        if 'is_owned' in data:
            game.is_owned = data['is_owned']
        if 'user_rating' in data:
            game.user_rating = data['user_rating']
        if 'user_notes' in data:
            game.user_notes = data['user_notes']

        db.session.commit()

        return jsonify({
            'success': True,
            'game': game.to_dict()
        })

    except Exception as e:
        logger.error(f"更新游戏信息失败: {e}", exc_info=True)
        db.session.rollback()
        return jsonify({'success': False, 'error': str(e)}), 500


@app.route('/api/games/<work_id>', methods=['DELETE'])
def delete_game(work_id):
    """删除游戏"""
    try:
        game = Game.query.filter_by(work_id=work_id.upper()).first()
        if not game:
            return jsonify({'success': False, 'error': '游戏不存在'}), 404

        db.session.delete(game)
        db.session.commit()

        return jsonify({'success': True})

    except Exception as e:
        logger.error(f"删除游戏失败: {e}", exc_info=True)
        db.session.rollback()
        return jsonify({'success': False, 'error': str(e)}), 500


# ==================== API路由 - 爬虫操作 ====================
@app.route('/api/scrape/search', methods=['POST'])
def scrape_search():
    """搜索游戏"""
    try:
        data = request.get_json()
        keyword = data.get('keyword', '')
        page = data.get('page', 1)
        max_results = data.get('max_results', 100)

        if not keyword:
            return jsonify({'success': False, 'error': '请提供搜索关键词'}), 400

        games = scraper.search_games(keyword, page, max_results)

        return jsonify({
            'success': True,
            'games': games,
            'count': len(games)
        })

    except Exception as e:
        logger.error(f"搜索游戏失败: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


@app.route('/api/scrape/game', methods=['POST'])
def scrape_game():
    """爬取单个游戏"""
    try:
        data = request.get_json()
        work_id = data.get('work_id', '').upper()
        language = data.get('language', Config.DEFAULT_LANGUAGE)

        if not work_id:
            return jsonify({'success': False, 'error': '请提供Work ID'}), 400

        # 爬取游戏信息
        game_data = scraper.scrape_game_page(work_id, language)
        if not game_data:
            return jsonify({'success': False, 'error': '爬取失败'}), 500

        # 保存到数据库
        game = Game.query.filter_by(work_id=work_id).first()
        if not game:
            game = Game(work_id=work_id)
            db.session.add(game)

        # 更新游戏信息
        game.title = game_data.get('title', '')
        game.title_en = game_data.get('title_en')
        game.circle = game_data.get('circle')
        game.description = game_data.get('description')
        game.series = game_data.get('series')
        game.age_rating = game_data.get('age_rating')
        game.rating = game_data.get('rating')
        game.review_count = game_data.get('review_count', 0)
        game.sale_count = game_data.get('sale_count', 0)
        game.icon_url = game_data.get('icon_url')
        game.cover_url = game_data.get('cover_url')
        game.dlsite_url = game_data.get('dlsite_url')
        game.file_size = game_data.get('file_size')
        game.file_format = game_data.get('file_format')

        # 处理日期
        if game_data.get('release_date'):
            game.release_date = datetime.fromisoformat(game_data['release_date']).date()
        if game_data.get('update_date'):
            game.update_date = datetime.fromisoformat(game_data['update_date']).date()

        # 处理列表字段
        if game_data.get('genres'):
            game.genres = ','.join(game_data['genres'])
        if game_data.get('authors'):
            game.authors = ','.join(game_data['authors'])
        if game_data.get('illustrators'):
            game.illustrators = ','.join(game_data['illustrators'])
        if game_data.get('voice_actors'):
            game.voice_actors = ','.join(game_data['voice_actors'])
        if game_data.get('scenario_writers'):
            game.scenario_writers = ','.join(game_data['scenario_writers'])
        if game_data.get('music_creators'):
            game.music_creators = ','.join(game_data['music_creators'])
        if game_data.get('sample_images'):
            game.sample_images = ','.join(game_data['sample_images'])

        game.last_scraped_at = datetime.utcnow()

        db.session.commit()

        return jsonify({
            'success': True,
            'game': game.to_dict()
        })

    except Exception as e:
        logger.error(f"爬取游戏失败: {e}", exc_info=True)
        db.session.rollback()
        return jsonify({'success': False, 'error': str(e)}), 500


@app.route('/api/scrape/batch', methods=['POST'])
def scrape_batch():
    """批量爬取游戏（后台任务）"""
    try:
        data = request.get_json()
        work_ids = data.get('work_ids', [])
        language = data.get('language', Config.DEFAULT_LANGUAGE)

        if not work_ids:
            return jsonify({'success': False, 'error': '请提供Work ID列表'}), 400

        # 创建任务
        task = ScrapingTask(
            task_type='batch',
            parameters=json.dumps({'work_ids': work_ids, 'language': language}),
            total_items=len(work_ids),
            status='pending'
        )
        db.session.add(task)
        db.session.commit()

        # 启动后台任务
        thread = Thread(target=run_batch_scraping, args=(task.id,))
        thread.daemon = True
        thread.start()

        return jsonify({
            'success': True,
            'task_id': task.id,
            'message': '批量爬取任务已创建'
        })

    except Exception as e:
        logger.error(f"创建批量爬取任务失败: {e}", exc_info=True)
        db.session.rollback()
        return jsonify({'success': False, 'error': str(e)}), 500


def run_batch_scraping(task_id):
    """执行批量爬取任务"""
    with app.app_context():
        task = ScrapingTask.query.get(task_id)
        if not task:
            return

        try:
            task.status = 'running'
            task.started_at = datetime.utcnow()
            db.session.commit()

            params = json.loads(task.parameters)
            work_ids = params['work_ids']
            language = params.get('language', Config.DEFAULT_LANGUAGE)

            for work_id in work_ids:
                try:
                    # 爬取游戏
                    game_data = scraper.scrape_game_page(work_id, language)
                    if game_data:
                        # 保存到数据库（简化版）
                        game = Game.query.filter_by(work_id=work_id).first()
                        if not game:
                            game = Game(work_id=work_id)
                            db.session.add(game)

                        # 更新基本信息
                        for key, value in game_data.items():
                            if hasattr(game, key) and value is not None:
                                if isinstance(value, list):
                                    setattr(game, key, ','.join(map(str, value)))
                                else:
                                    setattr(game, key, value)

                        game.last_scraped_at = datetime.utcnow()
                        db.session.commit()

                        task.completed_items += 1
                    else:
                        task.failed_items += 1

                except Exception as e:
                    logger.error(f"爬取游戏失败 ({work_id}): {e}")
                    task.failed_items += 1

                db.session.commit()

            task.status = 'completed'
            task.completed_at = datetime.utcnow()
            db.session.commit()

        except Exception as e:
            logger.error(f"批量爬取任务失败: {e}", exc_info=True)
            task.status = 'failed'
            task.error_message = str(e)
            db.session.commit()


# ==================== API路由 - 分类管理 ====================
@app.route('/api/categories', methods=['GET'])
def get_categories():
    """获取所有分类"""
    try:
        categories = Category.query.all()
        return jsonify({
            'success': True,
            'categories': [cat.to_dict() for cat in categories]
        })

    except Exception as e:
        logger.error(f"获取分类列表失败: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


@app.route('/api/categories', methods=['POST'])
def create_category():
    """创建新分类"""
    try:
        data = request.get_json()
        name = data.get('name', '')
        description = data.get('description', '')
        color = data.get('color', '#007bff')

        if not name:
            return jsonify({'success': False, 'error': '请提供分类名称'}), 400

        # 检查是否已存在
        existing = Category.query.filter_by(name=name).first()
        if existing:
            return jsonify({'success': False, 'error': '分类已存在'}), 400

        category = Category(name=name, description=description, color=color)
        db.session.add(category)
        db.session.commit()

        return jsonify({
            'success': True,
            'category': category.to_dict()
        })

    except Exception as e:
        logger.error(f"创建分类失败: {e}", exc_info=True)
        db.session.rollback()
        return jsonify({'success': False, 'error': str(e)}), 500


@app.route('/api/categories/<int:category_id>', methods=['DELETE'])
def delete_category(category_id):
    """删除分类"""
    try:
        category = Category.query.get(category_id)
        if not category:
            return jsonify({'success': False, 'error': '分类不存在'}), 404

        db.session.delete(category)
        db.session.commit()

        return jsonify({'success': True})

    except Exception as e:
        logger.error(f"删除分类失败: {e}", exc_info=True)
        db.session.rollback()
        return jsonify({'success': False, 'error': str(e)}), 500


@app.route('/api/games/<work_id>/categories', methods=['POST'])
def add_game_to_category(work_id):
    """将游戏添加到分类"""
    try:
        data = request.get_json()
        category_id = data.get('category_id')

        game = Game.query.filter_by(work_id=work_id.upper()).first()
        if not game:
            return jsonify({'success': False, 'error': '游戏不存在'}), 404

        category = Category.query.get(category_id)
        if not category:
            return jsonify({'success': False, 'error': '分类不存在'}), 404

        # 检查是否已添加
        existing = GameCategory.query.filter_by(
            game_id=game.id,
            category_id=category_id
        ).first()

        if existing:
            return jsonify({'success': False, 'error': '游戏已在该分类中'}), 400

        game_category = GameCategory(game_id=game.id, category_id=category_id)
        db.session.add(game_category)
        db.session.commit()

        return jsonify({'success': True})

    except Exception as e:
        logger.error(f"添加游戏到分类失败: {e}", exc_info=True)
        db.session.rollback()
        return jsonify({'success': False, 'error': str(e)}), 500


@app.route('/api/games/<work_id>/categories/<int:category_id>', methods=['DELETE'])
def remove_game_from_category(work_id, category_id):
    """从分类中移除游戏"""
    try:
        game = Game.query.filter_by(work_id=work_id.upper()).first()
        if not game:
            return jsonify({'success': False, 'error': '游戏不存在'}), 404

        game_category = GameCategory.query.filter_by(
            game_id=game.id,
            category_id=category_id
        ).first()

        if not game_category:
            return jsonify({'success': False, 'error': '游戏不在该分类中'}), 404

        db.session.delete(game_category)
        db.session.commit()

        return jsonify({'success': True})

    except Exception as e:
        logger.error(f"从分类中移除游戏失败: {e}", exc_info=True)
        db.session.rollback()
        return jsonify({'success': False, 'error': str(e)}), 500


# ==================== API路由 - 任务管理 ====================
@app.route('/api/tasks', methods=['GET'])
def get_tasks():
    """获取所有任务"""
    try:
        tasks = ScrapingTask.query.order_by(ScrapingTask.created_at.desc()).all()
        return jsonify({
            'success': True,
            'tasks': [task.to_dict() for task in tasks]
        })

    except Exception as e:
        logger.error(f"获取任务列表失败: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


@app.route('/api/tasks/<int:task_id>', methods=['GET'])
def get_task(task_id):
    """获取任务详情"""
    try:
        task = ScrapingTask.query.get(task_id)
        if not task:
            return jsonify({'success': False, 'error': '任务不存在'}), 404

        return jsonify({
            'success': True,
            'task': task.to_dict()
        })

    except Exception as e:
        logger.error(f"获取任务详情失败: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


# ==================== API路由 - 统计信息 ====================
@app.route('/api/stats', methods=['GET'])
def get_stats():
    """获取统计信息"""
    try:
        total_games = Game.query.count()
        favorite_games = Game.query.filter_by(is_favorite=True).count()
        blocked_games = Game.query.filter_by(is_blocked=True).count()
        owned_games = Game.query.filter_by(is_owned=True).count()

        return jsonify({
            'success': True,
            'stats': {
                'total_games': total_games,
                'favorite_games': favorite_games,
                'blocked_games': blocked_games,
                'owned_games': owned_games,
            }
        })

    except Exception as e:
        logger.error(f"获取统计信息失败: {e}", exc_info=True)
        return jsonify({'success': False, 'error': str(e)}), 500


# ==================== 启动应用 ====================
if __name__ == '__main__':
    # 初始化数据库
    init_database()

    # 启动应用
    app.run(
        host='0.0.0.0',
        port=5000,
        debug=True
    )
