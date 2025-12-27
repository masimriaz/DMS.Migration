// Toast Notification System for DMS Migration Platform
// Uses Bootstrap 5 compatible toast markup with AdminLTE styling

(function () {
    'use strict';

    // Toast icon and color mapping
    const toastConfig = {
        success: { icon: 'fas fa-check-circle', bg: 'bg-success', color: 'text-white' },
        error: { icon: 'fas fa-exclamation-circle', bg: 'bg-danger', color: 'text-white' },
        warning: { icon: 'fas fa-exclamation-triangle', bg: 'bg-warning', color: 'text-dark' },
        info: { icon: 'fas fa-info-circle', bg: 'bg-info', color: 'text-white' }
    };

    function showToast(type, title, message) {
        const config = toastConfig[type] || toastConfig.info;
        const toastId = 'toast-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);

        const toastHtml = `
            <div id="${toastId}" class="toast ${config.bg} ${config.color}" role="alert" aria-live="assertive" aria-atomic="true" data-delay="5000" style="min-width: 300px; margin-bottom: 10px;">
                <div class="toast-header ${config.bg} ${config.color}">
                    <i class="${config.icon} mr-2"></i>
                    <strong class="mr-auto">${escapeHtml(title)}</strong>
                    <button type="button" class="ml-2 mb-1 close ${config.color}" data-dismiss="toast" aria-label="Close">
                        <span aria-hidden="true">&times;</span>
                    </button>
                </div>
                <div class="toast-body">
                    ${escapeHtml(message)}
                </div>
            </div>
        `;

        const container = document.getElementById('toast-container');
        if (container) {
            container.insertAdjacentHTML('beforeend', toastHtml);

            const toastElement = document.getElementById(toastId);
            if (toastElement) {
                // Use jQuery if available (AdminLTE compatibility)
                if (typeof $ !== 'undefined') {
                    $(toastElement).toast('show');
                    $(toastElement).on('hidden.bs.toast', function () {
                        $(this).remove();
                    });
                } else {
                    // Fallback to vanilla JS (Bootstrap 5 native)
                    const bsToast = new bootstrap.Toast(toastElement);
                    bsToast.show();
                    toastElement.addEventListener('hidden.bs.toast', function () {
                        this.remove();
                    });
                }
            }
        }
    }

    function escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, m => map[m]);
    }

    // Process toasts from TempData
    function processToasts() {
        const toastDataElement = document.getElementById('toast-data');
        if (toastDataElement) {
            try {
                const toasts = JSON.parse(toastDataElement.textContent);
                toasts.forEach(toast => {
                    showToast(toast.Type || toast.type, toast.Title || toast.title, toast.Message || toast.message);
                });
                toastDataElement.remove();
            } catch (e) {
                console.error('Error parsing toast data:', e);
            }
        }
    }

    // Expose showToast globally for programmatic use
    window.showToast = showToast;

    // Auto-process toasts on page load
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', processToasts);
    } else {
        processToasts();
    }
})();
