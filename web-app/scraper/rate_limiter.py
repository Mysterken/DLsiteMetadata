"""
速率限制器 - 防止被DLsite封禁
"""
import time
from collections import deque
from threading import Lock


class RateLimiter:
    """
    令牌桶算法实现的速率限制器
    确保不会因为请求过快而被DLsite封禁IP
    """

    def __init__(self, calls_per_period=1, period=2):
        """
        初始化速率限制器

        Args:
            calls_per_period: 每个时间周期内允许的请求次数
            period: 时间周期（秒）
        """
        self.calls_per_period = calls_per_period
        self.period = period
        self.calls = deque()
        self.lock = Lock()

    def __call__(self, func):
        """装饰器用法"""
        def wrapper(*args, **kwargs):
            self.wait()
            return func(*args, **kwargs)
        return wrapper

    def wait(self):
        """
        等待直到可以发起新请求
        """
        with self.lock:
            now = time.time()

            # 移除过期的请求记录
            while self.calls and self.calls[0] < now - self.period:
                self.calls.popleft()

            # 如果达到限制，等待
            if len(self.calls) >= self.calls_per_period:
                sleep_time = self.period - (now - self.calls[0])
                if sleep_time > 0:
                    time.sleep(sleep_time)
                    # 重新清理过期记录
                    now = time.time()
                    while self.calls and self.calls[0] < now - self.period:
                        self.calls.popleft()

            # 记录当前请求
            self.calls.append(now)

    def reset(self):
        """重置速率限制器"""
        with self.lock:
            self.calls.clear()


class AdaptiveRateLimiter(RateLimiter):
    """
    自适应速率限制器
    根据服务器响应自动调整请求速率
    """

    def __init__(self, initial_calls=1, initial_period=2, min_period=1, max_period=10):
        super().__init__(initial_calls, initial_period)
        self.min_period = min_period
        self.max_period = max_period
        self.error_count = 0
        self.success_count = 0

    def on_success(self):
        """请求成功时调用 - 可以尝试加快速率"""
        self.success_count += 1
        self.error_count = 0

        # 连续成功10次，尝试缩短周期
        if self.success_count >= 10:
            with self.lock:
                self.period = max(self.min_period, self.period * 0.9)
                self.success_count = 0

    def on_error(self, status_code=None):
        """请求失败时调用 - 减慢速率"""
        self.error_count += 1
        self.success_count = 0

        # 如果遇到429或503，立即大幅减慢
        if status_code in [429, 503]:
            with self.lock:
                self.period = min(self.max_period, self.period * 2)
        # 连续失败3次，适度减慢
        elif self.error_count >= 3:
            with self.lock:
                self.period = min(self.max_period, self.period * 1.5)
                self.error_count = 0

    def get_current_rate(self):
        """获取当前速率信息"""
        return {
            'calls_per_period': self.calls_per_period,
            'period': self.period,
            'requests_per_minute': (self.calls_per_period / self.period) * 60
        }
