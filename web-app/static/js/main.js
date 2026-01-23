/**
 * DLsite游戏管理器 - 主JavaScript文件
 */

// API基础URL
const API_BASE_URL = '';

// Toast通知
function showToast(title, message, type = 'info') {
    const toastElement = document.getElementById('notification-toast');
    const toastTitle = document.getElementById('toast-title');
    const toastMessage = document.getElementById('toast-message');

    // 设置内容
    toastTitle.textContent = title;
    toastMessage.textContent = message;

    // 设置样式
    toastElement.className = 'toast';
    switch (type) {
        case 'success':
            toastElement.classList.add('bg-success', 'text-white');
            break;
        case 'danger':
        case 'error':
            toastElement.classList.add('bg-danger', 'text-white');
            break;
        case 'warning':
            toastElement.classList.add('bg-warning', 'text-dark');
            break;
        default:
            toastElement.classList.add('bg-info', 'text-white');
    }

    // 显示Toast
    const toast = new bootstrap.Toast(toastElement, {
        autohide: true,
        delay: 3000
    });
    toast.show();
}

// 加载统计信息
async function loadStats() {
    try {
        const response = await axios.get('/api/stats');
        if (response.data.success) {
            const stats = response.data.stats;
            const totalCount = document.getElementById('total-count');
            if (totalCount) {
                totalCount.innerHTML = `
                    <i class="bi bi-collection"></i> 总计: ${stats.total_games} |
                    <i class="bi bi-star-fill"></i> ${stats.favorite_games} |
                    <i class="bi bi-check-circle-fill"></i> ${stats.owned_games}
                `;
            }
        }
    } catch (error) {
        console.error('加载统计失败:', error);
    }
}

// 加载分类列表
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
        showToast('失败', '复制失败', 'danger');
    }
}

// 确认对话框
function confirmAction(message, callback) {
    if (confirm(message)) {
        callback();
    }
}

// 图片加载错误处理
function handleImageError(img, fallbackUrl = '/static/images/placeholder.jpg') {
    img.onerror = null; // 防止无限循环
    img.src = fallbackUrl;
}

// 设置axios默认配置
axios.defaults.timeout = 30000; // 30秒超时
axios.defaults.headers.common['Content-Type'] = 'application/json';

// 请求拦截器
axios.interceptors.request.use(
    config => {
        // 可以在这里添加loading状态
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
            // 服务器返回错误
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
                    showToast('服务器错误', message, 'danger');
                    break;
                default:
                    showToast('错误', message, 'danger');
            }
        } else if (error.request) {
            // 请求已发送但没有收到响应
            showToast('网络错误', '无法连接到服务器', 'danger');
        } else {
            // 其他错误
            showToast('错误', error.message, 'danger');
        }

        return Promise.reject(error);
    }
);

// 工具函数：验证Work ID
function isValidWorkId(workId) {
    return /^(RJ|RE|BJ|VJ)\d{6,8}$/i.test(workId);
}

// 工具函数：提取Work ID
function extractWorkId(text) {
    const match = text.match(/(RJ|RE|BJ|VJ)\d{6,8}/i);
    return match ? match[0].toUpperCase() : null;
}

// 工具函数：格式化文件大小
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

// 工具函数：高亮搜索关键词
function highlightText(text, keyword) {
    if (!keyword) return text;

    const regex = new RegExp(`(${keyword})`, 'gi');
    return text.replace(regex, '<mark>$1</mark>');
}

// 页面加载完成后执行
document.addEventListener('DOMContentLoaded', function() {
    console.log('DLsite游戏管理器已加载');

    // 初始化所有tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function(tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // 初始化所有popovers
    const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function(popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });
});

// 键盘快捷键
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
        const modals = document.querySelectorAll('.modal.show');
        modals.forEach(modal => {
            const bsModal = bootstrap.Modal.getInstance(modal);
            if (bsModal) {
                bsModal.hide();
            }
        });
    }
});

// 导出全局函数
window.showToast = showToast;
window.loadStats = loadStats;
window.loadCategories = loadCategories;
window.formatDate = formatDate;
window.formatDateTime = formatDateTime;
window.debounce = debounce;
window.throttle = throttle;
window.copyToClipboard = copyToClipboard;
window.confirmAction = confirmAction;
window.handleImageError = handleImageError;
window.isValidWorkId = isValidWorkId;
window.extractWorkId = extractWorkId;
window.formatFileSize = formatFileSize;
window.highlightText = highlightText;
