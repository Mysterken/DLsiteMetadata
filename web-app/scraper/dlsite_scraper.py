"""
DLsite游戏信息爬虫
基于原C#版本的逻辑，使用Python实现
"""
import re
import time
import random
import logging
from datetime import datetime
from typing import Dict, List, Optional
from urllib.parse import urljoin

import requests
from bs4 import BeautifulSoup

from .rate_limiter import AdaptiveRateLimiter

logger = logging.getLogger(__name__)


class DLsiteScraper:
    """
    DLsite游戏信息爬虫类
    支持多语言、自适应速率限制、自动重试
    """

    # 支持的语言代码
    SUPPORTED_LANGUAGES = {
        'ja_JP': 'ja-jp',
        'zh_CN': 'zh-cn',
        'zh_TW': 'zh-tw',
        'en_US': 'en-us',
        'ko_KR': 'ko-kr',
    }

    # Work ID正则表达式
    WORK_ID_PATTERN = re.compile(r'(RJ|RE|BJ|VJ)\d{6,8}', re.IGNORECASE)

    def __init__(self, config):
        """
        初始化爬虫

        Args:
            config: 配置对象
        """
        self.config = config
        self.session = requests.Session()

        # 设置默认请求头
        self.session.headers.update({
            'User-Agent': random.choice(config.USER_AGENTS),
            'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8',
            'Accept-Language': 'zh-CN,zh;q=0.9,en;q=0.8,ja;q=0.7',
            'Accept-Encoding': 'gzip, deflate, br',
            'Connection': 'keep-alive',
            'Upgrade-Insecure-Requests': '1',
        })

        # 初始化速率限制器
        self.rate_limiter = AdaptiveRateLimiter(
            initial_calls=config.RATE_LIMIT_CALLS,
            initial_period=config.RATE_LIMIT_PERIOD,
            min_period=1,
            max_period=10
        )

    def _make_request(self, url: str, max_retries: int = None) -> Optional[requests.Response]:
        """
        发起HTTP请求，带重试和速率限制

        Args:
            url: 请求URL
            max_retries: 最大重试次数

        Returns:
            响应对象，失败返回None
        """
        if max_retries is None:
            max_retries = self.config.RETRY_TIMES

        for attempt in range(max_retries):
            try:
                # 速率限制
                self.rate_limiter.wait()

                # 随机切换User-Agent
                self.session.headers['User-Agent'] = random.choice(self.config.USER_AGENTS)

                # 发起请求
                response = self.session.get(
                    url,
                    timeout=self.config.REQUEST_TIMEOUT,
                    allow_redirects=True
                )

                # 检查响应状态
                if response.status_code == 200:
                    self.rate_limiter.on_success()
                    return response
                elif response.status_code in [429, 503]:
                    logger.warning(f"请求被限制 (状态码: {response.status_code}), URL: {url}")
                    self.rate_limiter.on_error(response.status_code)
                    time.sleep(self.config.RETRY_DELAY * (attempt + 1))
                elif response.status_code == 404:
                    logger.error(f"页面不存在 (404): {url}")
                    return None
                else:
                    logger.warning(f"请求失败 (状态码: {response.status_code}), URL: {url}")
                    self.rate_limiter.on_error(response.status_code)

            except requests.exceptions.Timeout:
                logger.warning(f"请求超时 (尝试 {attempt + 1}/{max_retries}): {url}")
            except requests.exceptions.RequestException as e:
                logger.error(f"请求异常 (尝试 {attempt + 1}/{max_retries}): {url}, 错误: {e}")

            # 重试延迟
            if attempt < max_retries - 1:
                time.sleep(self.config.RETRY_DELAY * (attempt + 1))

        logger.error(f"请求失败，已达最大重试次数: {url}")
        return None

    def extract_work_id(self, text: str) -> Optional[str]:
        """
        从文本中提取Work ID

        Args:
            text: 包含Work ID的文本

        Returns:
            Work ID，未找到返回None
        """
        match = self.WORK_ID_PATTERN.search(text)
        return match.group(0).upper() if match else None

    def scrape_game_page(self, work_id: str, language: str = 'zh_CN') -> Optional[Dict]:
        """
        爬取单个游戏页面的详细信息

        Args:
            work_id: 游戏的Work ID (如: RJ123456)
            language: 页面语言

        Returns:
            游戏信息字典，失败返回None
        """
        # 确保work_id格式正确
        work_id = work_id.upper()
        if not self.WORK_ID_PATTERN.match(work_id):
            logger.error(f"无效的Work ID: {work_id}")
            return None

        # 构建URL
        lang_code = self.SUPPORTED_LANGUAGES.get(language, 'zh-cn')
        url = f"https://www.dlsite.com/maniax/work/=/product_id/{work_id}.html?locale={lang_code}"

        logger.info(f"正在爬取游戏: {work_id} ({language})")

        # 发起请求
        response = self._make_request(url)
        if not response:
            return None

        # 解析HTML
        soup = BeautifulSoup(response.text, 'lxml')

        # 提取游戏信息
        game_data = {
            'work_id': work_id,
            'dlsite_url': url,
            'scraped_at': datetime.utcnow().isoformat(),
        }

        try:
            # 标题
            title_elem = soup.select_one('#work_name')
            if title_elem:
                game_data['title'] = title_elem.get_text(strip=True)

            # 英文标题
            title_en_elem = soup.select_one('.work_name_en')
            if title_en_elem:
                game_data['title_en'] = title_en_elem.get_text(strip=True)

            # 社团名称
            circle_elem = soup.select_one('span[class*="maker_name"] a')
            if circle_elem:
                game_data['circle'] = circle_elem.get_text(strip=True)

            # 描述
            desc_elem = soup.select_one('.work_parts_container .work_parts_area')
            if desc_elem:
                game_data['description'] = desc_elem.get_text(strip=True)

            # 评分
            rating_elem = soup.select_one('.work_rating .average_count')
            if rating_elem:
                try:
                    game_data['rating'] = float(rating_elem.get_text(strip=True))
                except ValueError:
                    pass

            # 评论数
            review_elem = soup.select_one('.work_rating .review_count')
            if review_elem:
                try:
                    review_text = review_elem.get_text(strip=True)
                    game_data['review_count'] = int(re.search(r'\d+', review_text).group())
                except (ValueError, AttributeError):
                    pass

            # 销量
            sale_elem = soup.select_one('.work_buy .dl_count strong')
            if sale_elem:
                try:
                    sale_text = sale_elem.get_text(strip=True).replace(',', '')
                    game_data['sale_count'] = int(sale_text)
                except ValueError:
                    pass

            # 图片
            # 封面图
            cover_elem = soup.select_one('div.product-slider-data li:first-child img')
            if cover_elem:
                game_data['cover_url'] = cover_elem.get('src') or cover_elem.get('data-src')

            # 样图
            sample_images = []
            for img in soup.select('.product-slider-data li img'):
                img_url = img.get('src') or img.get('data-src')
                if img_url:
                    sample_images.append(img_url)
            game_data['sample_images'] = sample_images

            # 图标（使用第一张图的缩略图）
            if sample_images:
                game_data['icon_url'] = sample_images[0]

            # 解析表格信息
            table_info = self._parse_table_info(soup, language)
            game_data.update(table_info)

            # 标签/类型
            genres = []
            for tag in soup.select('.main_genre a, .sub_genre a'):
                genre_text = tag.get_text(strip=True)
                if genre_text:
                    genres.append(genre_text)
            game_data['genres'] = genres

            logger.info(f"成功爬取游戏: {work_id}")
            return game_data

        except Exception as e:
            logger.error(f"解析游戏页面失败 ({work_id}): {e}", exc_info=True)
            return None

    def _parse_table_info(self, soup: BeautifulSoup, language: str) -> Dict:
        """
        解析游戏详情表格信息

        Args:
            soup: BeautifulSoup对象
            language: 页面语言

        Returns:
            表格信息字典
        """
        info = {}

        # 定义表头关键词映射（多语言支持）
        keywords = {
            'release_date': ['販売日', '贩卖日', '发售日', 'Release date', '출시일'],
            'update_date': ['更新日', 'Update date', '업데이트'],
            'series': ['シリーズ名', '系列名', 'Series name', '시리즈'],
            'author': ['作者', 'Author', '저자'],
            'scenario': ['シナリオ', '剧本', 'Scenario', '시나리오'],
            'illustration': ['イラスト', '插画', 'Illustration', '일러스트'],
            'voice': ['声優', '声优', 'Voice Actor', '성우'],
            'music': ['音楽', '音乐', 'Music', '음악'],
            'age_rating': ['年齢指定', '年龄限制', 'Age', '연령'],
            'file_size': ['ファイル容量', '文件大小', 'File size', '파일'],
            'file_format': ['ファイル形式', '文件格式', 'File format', '파일형식'],
        }

        # 查找表格
        table = soup.select_one('#work_outline')
        if not table:
            return info

        # 遍历表格行
        for row in table.select('tr'):
            th = row.select_one('th')
            td = row.select_one('td')

            if not th or not td:
                continue

            th_text = th.get_text(strip=True)

            # 发布日期
            if any(kw in th_text for kw in keywords['release_date']):
                date_text = td.get_text(strip=True)
                parsed_date = self._parse_date(date_text)
                if parsed_date:
                    info['release_date'] = parsed_date

            # 更新日期
            elif any(kw in th_text for kw in keywords['update_date']):
                date_text = td.get_text(strip=True)
                parsed_date = self._parse_date(date_text)
                if parsed_date:
                    info['update_date'] = parsed_date

            # 系列
            elif any(kw in th_text for kw in keywords['series']):
                info['series'] = td.get_text(strip=True)

            # 作者
            elif any(kw in th_text for kw in keywords['author']):
                authors = [a.get_text(strip=True) for a in td.select('a')]
                info['authors'] = authors

            # 剧本
            elif any(kw in th_text for kw in keywords['scenario']):
                scenario_writers = [a.get_text(strip=True) for a in td.select('a')]
                info['scenario_writers'] = scenario_writers

            # 插画
            elif any(kw in th_text for kw in keywords['illustration']):
                illustrators = [a.get_text(strip=True) for a in td.select('a')]
                info['illustrators'] = illustrators

            # 配音
            elif any(kw in th_text for kw in keywords['voice']):
                voice_actors = [a.get_text(strip=True) for a in td.select('a')]
                info['voice_actors'] = voice_actors

            # 音乐
            elif any(kw in th_text for kw in keywords['music']):
                music_creators = [a.get_text(strip=True) for a in td.select('a')]
                info['music_creators'] = music_creators

            # 年龄分级
            elif any(kw in th_text for kw in keywords['age_rating']):
                info['age_rating'] = td.get_text(strip=True)

            # 文件大小
            elif any(kw in th_text for kw in keywords['file_size']):
                info['file_size'] = td.get_text(strip=True)

            # 文件格式
            elif any(kw in th_text for kw in keywords['file_format']):
                info['file_format'] = td.get_text(strip=True)

        return info

    def _parse_date(self, date_text: str) -> Optional[str]:
        """
        解析日期字符串，支持多种格式

        Args:
            date_text: 日期文本

        Returns:
            ISO格式日期字符串 (YYYY-MM-DD)，解析失败返回None
        """
        # 移除多余的空白和时间部分
        date_text = re.sub(r'\s+\d{2}:\d{2}.*', '', date_text.strip())

        # 日期格式列表
        date_formats = [
            '%Y年%m月%d日',  # 中文格式
            '%Y/%m/%d',      # 日文格式
            '%Y-%m-%d',      # ISO格式
            '%b %d, %Y',     # 英文格式 (Jan 01, 2024)
            '%d/%m/%Y',      # 欧洲格式
        ]

        for fmt in date_formats:
            try:
                dt = datetime.strptime(date_text, fmt)
                return dt.date().isoformat()
            except ValueError:
                continue

        logger.warning(f"无法解析日期: {date_text}")
        return None

    def search_games(self, keyword: str, page: int = 1, max_results: int = 100) -> List[Dict]:
        """
        搜索游戏

        Args:
            keyword: 搜索关键词
            page: 页码
            max_results: 最大结果数

        Returns:
            游戏列表
        """
        # 构建搜索URL
        url = f"https://www.dlsite.com/maniax/fsr/=/language/jp/sex_category%5B0%5D/male/keyword/{keyword}/per_page/{max_results}/page/{page}"

        logger.info(f"搜索游戏: {keyword} (页: {page})")

        # 发起请求
        response = self._make_request(url)
        if not response:
            return []

        # 解析HTML
        soup = BeautifulSoup(response.text, 'lxml')

        games = []
        for item in soup.select('.search_result_img_box_inner'):
            try:
                # Work ID
                link = item.select_one('a')
                if not link:
                    continue

                work_id = self.extract_work_id(link.get('href', ''))
                if not work_id:
                    continue

                # 标题
                title_elem = item.select_one('.work_name a')
                title = title_elem.get_text(strip=True) if title_elem else ''

                # 社团
                circle_elem = item.select_one('.maker_name a')
                circle = circle_elem.get_text(strip=True) if circle_elem else ''

                # 图片
                img_elem = item.select_one('img')
                img_url = img_elem.get('src') or img_elem.get('data-src') if img_elem else None

                games.append({
                    'work_id': work_id,
                    'title': title,
                    'circle': circle,
                    'icon_url': img_url,
                })

            except Exception as e:
                logger.error(f"解析搜索结果项失败: {e}")
                continue

        logger.info(f"搜索到 {len(games)} 个游戏")
        return games

    def get_rate_info(self) -> Dict:
        """获取当前速率限制信息"""
        return self.rate_limiter.get_current_rate()
