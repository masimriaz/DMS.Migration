// Data Migration Platform - Main JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // ============================================
    // Sidebar Toggle
    // ============================================
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('sidebar');
    const mainContent = document.getElementById('mainContent');
    
    if (sidebarToggle && sidebar && mainContent) {
        sidebarToggle.addEventListener('click', function() {
            sidebar.classList.toggle('collapsed');
            mainContent.classList.toggle('sidebar-collapsed');
            
            // Save state to localStorage
            const isCollapsed = sidebar.classList.contains('collapsed');
            localStorage.setItem('sidebarCollapsed', isCollapsed);
        });

        // Restore sidebar state from localStorage
        const sidebarCollapsed = localStorage.getItem('sidebarCollapsed');
        if (sidebarCollapsed === 'true') {
            sidebar.classList.add('collapsed');
            mainContent.classList.add('sidebar-collapsed');
        }
    }

    // ============================================
    // Mobile Sidebar Toggle
    // ============================================
    const mobileSidebarToggle = document.getElementById('mobileSidebarToggle');
    if (mobileSidebarToggle && sidebar) {
        mobileSidebarToggle.addEventListener('click', function() {
            sidebar.classList.toggle('show');
        });

        // Close sidebar on mobile when clicking outside
        document.addEventListener('click', function(event) {
            if (window.innerWidth < 992) {
                if (!sidebar.contains(event.target) && !mobileSidebarToggle.contains(event.target)) {
                    sidebar.classList.remove('show');
                }
            }
        });
    }

    // ============================================
    // Auto-refresh Status Pills
    // ============================================
    function autoRefreshJobs() {
        const runningStatusElements = document.querySelectorAll('.status-running');
        if (runningStatusElements.length > 0) {
            // Only refresh if there are running jobs
            console.log('Auto-refresh: Found running jobs');
            // In a real app, you would make an AJAX call here to refresh the job status
            // For now, we'll just log it
            setTimeout(autoRefreshJobs, 10000); // Refresh every 10 seconds
        }
    }

    // Start auto-refresh if on Jobs or Dashboard page
    if (window.location.pathname === '/Jobs' || window.location.pathname === '/' || window.location.pathname === '/Home') {
        setTimeout(autoRefreshJobs, 10000);
    }

    // ============================================
    // Toast Notification Helper (also in _ToastHost.cshtml)
    // ============================================
    window.showToast = function(message, type = 'info') {
        const toastContainer = document.getElementById('toastContainer');
        if (!toastContainer) return;

        const toastId = 'toast-' + Date.now();
        const iconClass = {
            'success': 'bi-check-circle-fill text-success',
            'error': 'bi-x-circle-fill text-danger',
            'warning': 'bi-exclamation-triangle-fill text-warning',
            'info': 'bi-info-circle-fill text-primary'
        }[type] || 'bi-info-circle-fill text-primary';

        const toastTitle = {
            'success': 'Success',
            'error': 'Error',
            'warning': 'Warning',
            'info': 'Information'
        }[type] || 'Notification';

        const toastHtml = `
            <div class="toast" id="${toastId}" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="toast-header">
                    <i class="bi ${iconClass} me-2"></i>
                    <strong class="me-auto">${toastTitle}</strong>
                    <small>Just now</small>
                    <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
                <div class="toast-body">
                    ${message}
                </div>
            </div>
        `;

        toastContainer.insertAdjacentHTML('beforeend', toastHtml);
        const toastElement = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastElement, { autohide: true, delay: 5000 });
        toast.show();

        toastElement.addEventListener('hidden.bs.toast', function () {
            toastElement.remove();
        });
    };

    // ============================================
    // Confirmation Modals
    // ============================================
    window.confirmAction = function(title, message, onConfirm) {
        const modalId = 'dynamicConfirmModal';
        let modal = document.getElementById(modalId);
        
        if (!modal) {
            const modalHtml = `
                <div class="modal fade" id="${modalId}" tabindex="-1" aria-hidden="true">
                    <div class="modal-dialog modal-dialog-centered">
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title" id="${modalId}Label">${title}</h5>
                                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                            </div>
                            <div class="modal-body">${message}</div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                                <button type="button" class="btn btn-primary" id="${modalId}Confirm">Confirm</button>
                            </div>
                        </div>
                    </div>
                </div>
            `;
            document.body.insertAdjacentHTML('beforeend', modalHtml);
            modal = document.getElementById(modalId);
        }

        const confirmBtn = document.getElementById(modalId + 'Confirm');
        const bootstrapModal = new bootstrap.Modal(modal);
        
        confirmBtn.onclick = function() {
            onConfirm();
            bootstrapModal.hide();
        };

        bootstrapModal.show();
    };

    // ============================================
    // Form Validation
    // ============================================
    const forms = document.querySelectorAll('.needs-validation');
    Array.from(forms).forEach(function(form) {
        form.addEventListener('submit', function(event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    });

    // ============================================
    // Search Box Enhancement
    // ============================================
    const searchBox = document.querySelector('.search-box input');
    if (searchBox) {
        searchBox.addEventListener('keypress', function(event) {
            if (event.key === 'Enter') {
                event.preventDefault();
                const searchTerm = this.value;
                console.log('Search for:', searchTerm);
                // Implement search functionality here
                showToast('Search functionality coming soon', 'info');
            }
        });
    }

    // ============================================
    // Initialize Tooltips
    // ============================================
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // ============================================
    // Initialize Popovers
    // ============================================
    const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });
});
