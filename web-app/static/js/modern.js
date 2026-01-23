/**
 * DLsite游戏管理器 - 现代化JavaScript
 */

// ==================== Toast通知系统 ====================
function showToast(title, message, type = 'info') {
    const container = document.getElementById('toast-container');
    const toast = document.createElement('div');
    toast.className = `toast-modern toast-${type} fade-in`;

    const iconMap = {
        success: 'check-circle-fill',
        error: 'x-circle-fill',
        warning: 'exclamation-triangle-fill',
        info: 'info-circle-fill'
    };

    const colorMap = {
        success: 'var(--color-success)',
        error: 'var(--color-danger)',
        warning: 'var(--color-warning)',
        info: 'var(--color-info)'
    };

    toast.innerHTML = `
        <div style="display: flex; align-items: start; gap: 0.75rem;">
            <i class="bi bi-${iconMap[type]}" style="font-size: 1.25rem; color: ${colorMap[type]};"></i>
            <div style="flex: 1;">
                <div style="font-weight: 600; margin-bottom: 0.25rem;">${title}</div>
                <div style="font-size: 0.875rem; color: var(--text-secondary);">${message}</div>
            </div>
            <button onclick="this.parentElement.parentElement.remove()"
                    style="background: none; border: none; cursor: pointer; color: var(--text-tertiary); padding: 0;">
                <i class="bi bi-x-lg"></i>
            </button>
        </div>
    `;

    container.appendChild(toast);

    // 3秒后自动移除
    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(-100px)';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// ==================== 统计信息加载 ====================
async function loadStats() {
    try {
        const response = await axios.get('/api/stats');
        if (response.data.success) {
            const stats = response.data.stats;
            const totalCount = document.getElementById('total-count');
            if (totalCount) {
                totalCount.textContent = stats.total_games;
            }
        }
    } catch (error) {
        console.error('加载统计失败:', error);
    }
}

// ==================== 分类加载 ====================
async function loadCategories() {
    try {
        const response = await axios.get('/api/categories');
        if (response.data.success) {
            const categories = response.data.categories;

            // 更新分类筛选下拉框
            const categoryFilter = document.getElementById('category-filter');
            if (categoryFilter) {
                const currentValue = categoryFilter.value;
                categoryFilter.innerHTML = '<option value="">全部分类</option>' +
                    categories.map(cat => `
                        <option value="${cat.id}">${cat.name} (${cat.game_count})</option>
                    `).join('');
                categoryFilter.value = currentValue;
            }

            // 更新游戏详情页的分类选择
            const addCategorySelect = document.getElementById('add-category-select');
            if (addCategorySelect) {
                addCategorySelect.innerHTML = '<option value="">选择分类...</option>' +
                    categories.map(cat => `
                        <option value="${cat.id}">${cat.name}</option>
                    `).join('');
            }
        }
    } catch (error) {
        console.error('加载分类失败:', error);
    }
}

// ==================== 工具函数 ====================

// 格式化日期
function formatDate(dateString) {
    if (!dateString) return '未知';
    const date = new Date(dateString);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

// 格式化时间
function formatDateTime(dateString) {
    if (!dateString) return '未知';
    const date = new Date(dateString);
    return date.toLocaleString('zh-CN', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
    });
}

// 防抖函数
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// 节流函数
function throttle(func, limit) {
    let inThrottle;
    return function(...args) {
        if (!inThrottle) {
            func.apply(this, args);
            inThrottle = true;
            setTimeout(() => inThrottle = false, limit);
        }
    };
}

// 复制到剪贴板
async function copyToClipboard(text) {
    try {
        await navigator.clipboard.writeText(text);
        showToast('成功', '已复制到剪贴板', 'success');
    } catch (err) {
        console.error('复制失败:', err);
        showToast('失败', '复制失败', 'error');
    }
}

// 验证Work ID
function isValidWorkId(workId) {
    return /^(RJ|RE|BJ|VJ)\d{6,8}$/i.test(workId);
}

// 提取Work ID
function extractWorkId(text) {
    const match = text.match(/(RJ|RE|BJ|VJ)\d{6,8}/i);
    return match ? match[0].toUpperCase() : null;
}

// 格式化文件大小
function formatFileSize(bytes) {
    if (!bytes || bytes === 0) return '未知';
    const units = ['B', 'KB', 'MB', 'GB'];
    let size = bytes;
    let unitIndex = 0;
    while (size >= 1024 && unitIndex < units.length - 1) {
        size /= 1024;
        unitIndex++;
    }
    return `${size.toFixed(2)} ${units[unitIndex]}`;
}

// 图片懒加载
function initLazyLoad() {
    const images = document.querySelectorAll('img[loading="lazy"]');
    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const image = entry.target;
                    image.src = image.dataset.src || image.src;
                    image.classList.add('fade-in');
                    observer.unobserve(image);
                }
            });
        });

        images.forEach(img => imageObserver.observe(img));
    } else {
        // 降级处理
        images.forEach(img => {
            img.src = img.dataset.src || img.src;
        });
    }
}

// ==================== Axios配置 ====================
axios.defaults.timeout = 30000;
axios.defaults.headers.common['Content-Type'] = 'application/json';

// 请求拦截器
axios.interceptors.request.use(
    config => {
        return config;
    },
    error => {
        return Promise.reject(error);
    }
);

// 响应拦截器
axios.interceptors.response.use(
    response => {
        return response;
    },
    error => {
        if (error.response) {
            const status = error.response.status;
            const message = error.response.data?.error || '请求失败';

            switch (status) {
                case 400:
                    showToast('请求错误', message, 'warning');
                    break;
                case 404:
                    showToast('未找到', message, 'warning');
                    break;
                case 500:
                    showToast('服务器错误', message, 'error');
                    break;
                default:
                    showToast('错误', message, 'error');
            }
        } else if (error.request) {
            showToast('网络错误', '无法连接到服务器', 'error');
        } else {
            showToast('错误', error.message, 'error');
        }

        return Promise.reject(error);
    }
);

// ==================== 键盘快捷键 ====================
document.addEventListener('keydown', function(e) {
    // Ctrl+K 或 Cmd+K：聚焦搜索框
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        const searchInput = document.getElementById('search-input');
        if (searchInput) {
            searchInput.focus();
        }
    }

    // ESC：关闭模态框
    if (e.key === 'Escape') {
        const modals = document.querySelectorAll('.modal-backdrop');
        modals.forEach(modal => {
            if (modal.style.display !== 'none') {
                modal.style.display = 'none';
            }
        });
    }
});

// ==================== 页面可见性API ====================
document.addEventListener('visibilitychange', function() {
    if (!document.hidden) {
        // 页面重新可见时刷新统计
        loadStats();
    }
});

// ==================== 性能优化 ====================
// 使用requestIdleCallback进行非关键任务
if ('requestIdleCallback' in window) {
    requestIdleCallback(() => {
        initLazyLoad();
    });
} else {
    setTimeout(() => {
        initLazyLoad();
    }, 1000);
}

// ==================== 导出全局函数 ====================
window.showToast = showToast;
window.loadStats = loadStats;
window.loadCategories = loadCategories;
window.formatDate = formatDate;
window.formatDateTime = formatDateTime;
window.debounce = debounce;
window.throttle = throttle;
window.copyToClipboard = copyToClipboard;
window.isValidWorkId = isValidWorkId;
window.extractWorkId = extractWorkId;
window.formatFileSize = formatFileSize;
window.initLazyLoad = initLazyLoad;

// ==================== 页面加载完成 ====================
window.addEventListener('load', function() {
    console.log('DLsite游戏管理器（现代版）已加载');

    // 添加页面进入动画
    document.body.style.opacity = '0';
    setTimeout(() => {
        document.body.style.transition = 'opacity 0.5s';
        document.body.style.opacity = '1';
    }, 100);
});

// ==================== 性能监控（开发环境） ====================
if (performance && performance.timing) {
    window.addEventListener('load', function() {
        setTimeout(() => {
            const timing = performance.timing;
            const loadTime = timing.loadEventEnd - timing.navigationStart;
            console.log(`页面加载时间: ${loadTime}ms`);
        }, 0);
    });
}
