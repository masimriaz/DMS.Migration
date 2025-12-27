// Connections Wizard Navigation and Validation
(function () {
    let currentStep = 1;
    const totalSteps = 5;

    document.addEventListener('DOMContentLoaded', function () {
        initWizard();
        attachEventHandlers();
        updateReviewData();
    });

    function initWizard() {
        const stepValue = parseInt(document.getElementById('currentStep')?.value || '1');
        if (stepValue > 1) {
            navigateToStep(stepValue, false);
        } else {
            showStep(1);
        }
    }

    function showStep(step) {
        // Hide all steps
        document.querySelectorAll('.wizard-step').forEach(el => {
            el.style.display = 'none';
        });

        // Show current step
        const currentStepEl = document.querySelector(`.wizard-step[data-step="${step}"]`);
        if (currentStepEl) {
            currentStepEl.style.display = 'block';
        }

        // Update stepper UI
        document.querySelectorAll('.stepper-item').forEach((item, index) => {
            const stepNum = index + 1;
            item.classList.remove('active', 'completed');

            if (stepNum < step) {
                item.classList.add('completed');
            } else if (stepNum === step) {
                item.classList.add('active');
            }
        });

        document.querySelectorAll('.stepper-line').forEach((line, index) => {
            const stepNum = index + 1;
            line.classList.toggle('completed', stepNum < step);
        });

        // Update buttons
        document.getElementById('btnPrevious').disabled = step === 1;

        if (step === totalSteps) {
            document.getElementById('btnNext').style.display = 'none';
            document.getElementById('btnFinish').style.display = 'inline-block';
        } else {
            document.getElementById('btnNext').style.display = 'inline-block';
            document.getElementById('btnFinish').style.display = 'none';
        }

        // Update hidden field
        const currentStepInput = document.getElementById('currentStep');
        if (currentStepInput) {
            currentStepInput.value = step;
        }

        // Update review data when reaching review step
        if (step === 5) {
            updateReviewData();
        }

        currentStep = step;
        window.scrollTo(0, 0);
    }

    function navigateToStep(step, validate = true) {
        if (validate && !validateStep(currentStep)) {
            return;
        }
        showStep(step);
    }

    function validateStep(step) {
        const stepEl = document.querySelector(`.wizard-step[data-step="${step}"]`);
        if (!stepEl) return true;

        const inputs = stepEl.querySelectorAll('input[required], select[required], textarea[required]');
        let isValid = true;

        inputs.forEach(input => {
            if (input.type === 'radio') {
                const radioGroup = stepEl.querySelectorAll(`input[name="${input.name}"]`);
                const isChecked = Array.from(radioGroup).some(radio => radio.checked);
                if (!isChecked) {
                    isValid = false;
                    showError(input, 'Please select an option');
                }
            } else if (!input.value.trim()) {
                isValid = false;
                input.classList.add('is-invalid');
                showError(input, 'This field is required');
            } else {
                input.classList.remove('is-invalid');
                clearError(input);
            }
        });

        if (!isValid) {
            showToast('Please fill in all required fields', 'error');
        }

        return isValid;
    }

    function showError(input, message) {
        clearError(input);
        const feedback = document.createElement('div');
        feedback.className = 'invalid-feedback';
        feedback.textContent = message;
        input.parentNode.appendChild(feedback);
        input.classList.add('is-invalid');
    }

    function clearError(input) {
        const feedback = input.parentNode.querySelector('.invalid-feedback');
        if (feedback) {
            feedback.remove();
        }
        input.classList.remove('is-invalid');
    }

    function attachEventHandlers() {
        // Next button
        const btnNext = document.getElementById('btnNext');
        if (btnNext) {
            btnNext.addEventListener('click', () => {
                if (currentStep < totalSteps) {
                    navigateToStep(currentStep + 1);
                }
            });
        }

        // Previous button
        const btnPrevious = document.getElementById('btnPrevious');
        if (btnPrevious) {
            btnPrevious.addEventListener('click', () => {
                if (currentStep > 1) {
                    navigateToStep(currentStep - 1, false);
                }
            });
        }

        // Save Draft button
        const btnSaveDraft = document.getElementById('btnSaveDraft');
        if (btnSaveDraft) {
            btnSaveDraft.addEventListener('click', () => saveDraft());
        }

        // Finish button
        const btnFinish = document.getElementById('btnFinish');
        if (btnFinish) {
            btnFinish.addEventListener('click', () => saveConnection());
        }

        // Test Connection button
        const btnTestConnection = document.getElementById('btnTestConnection');
        if (btnTestConnection) {
            btnTestConnection.addEventListener('click', () => testConnection());
        }

        // Clear validation on input
        document.querySelectorAll('input, select, textarea').forEach(input => {
            input.addEventListener('input', function () {
                this.classList.remove('is-invalid');
                clearError(this);
            });
        });
    }

    async function saveDraft() {
        const formData = gatherFormData();
        formData.Status = 0; // Draft

        try {
            const response = await fetch('/Connections/Save', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(formData)
            });

            const result = await response.json();

            if (result.success) {
                showToast('Draft saved successfully', 'success');
                document.getElementById('connectionId').value = result.connectionId;
                document.getElementById('isEditMode').value = 'true';
            } else {
                showToast(result.message || 'Failed to save draft', 'error');
            }
        } catch (error) {
            console.error('Save draft error:', error);
            showToast('An error occurred while saving', 'error');
        }
    }

    async function saveConnection() {
        if (!validateStep(currentStep)) {
            return;
        }

        const formData = gatherFormData();
        formData.Status = 1; // Verified

        try {
            const response = await fetch('/Connections/Save', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(formData)
            });

            const result = await response.json();

            if (result.success) {
                showToast('Connection saved successfully', 'success');
                setTimeout(() => {
                    window.location.href = '/Connections';
                }, 1500);
            } else {
                showToast(result.message || 'Failed to save connection', 'error');
            }
        } catch (error) {
            console.error('Save error:', error);
            showToast('An error occurred while saving', 'error');
        }
    }

    async function testConnection() {
        const connectionId = document.getElementById('connectionId').value;

        if (!connectionId || connectionId === '0') {
            showToast('Please save as draft first before testing', 'warning');
            return;
        }

        const btnTest = document.getElementById('btnTestConnection');
        const progressDiv = document.getElementById('verificationProgress');
        const resultDiv = document.getElementById('verificationResult');

        btnTest.disabled = true;
        progressDiv.style.display = 'block';
        resultDiv.style.display = 'none';

        try {
            const response = await fetch(`/Connections/Verify?id=${connectionId}`, {
                method: 'POST'
            });

            const result = await response.json();

            progressDiv.style.display = 'none';
            resultDiv.style.display = 'block';

            const statusDiv = document.getElementById('verificationStatus');
            const diagnosticsDiv = document.getElementById('verificationDiagnostics');

            if (result.success) {
                statusDiv.innerHTML = `
                    <div class="alert alert-success">
                        <i class="bi bi-check-circle me-2"></i>
                        <strong>Success!</strong> Connection verified successfully.
                    </div>
                `;
                diagnosticsDiv.innerHTML = `
                    <div class="card bg-light">
                        <div class="card-body">
                            <h6 class="card-title">Diagnostics</h6>
                            <p class="mb-0">${result.diagnostics || 'Connection test passed.'}</p>
                        </div>
                    </div>
                `;
                showToast('Connection verified successfully', 'success');
            } else {
                statusDiv.innerHTML = `
                    <div class="alert alert-danger">
                        <i class="bi bi-x-circle me-2"></i>
                        <strong>Failed!</strong> Connection verification failed.
                    </div>
                `;
                diagnosticsDiv.innerHTML = `
                    <div class="card bg-light border-danger">
                        <div class="card-body">
                            <h6 class="card-title text-danger">Error Details</h6>
                            <p class="mb-0">${result.errorMessage || result.message || 'Unknown error'}</p>
                        </div>
                    </div>
                `;
                showToast('Connection verification failed', 'error');
            }
        } catch (error) {
            console.error('Test connection error:', error);
            progressDiv.style.display = 'none';
            showToast('An error occurred during testing', 'error');
        } finally {
            btnTest.disabled = false;
        }
    }

    function gatherFormData() {
        return {
            Id: parseInt(document.getElementById('connectionId')?.value || '0'),
            TenantId: parseInt(document.getElementById('tenantId')?.value || '1'),
            Name: document.getElementById('name')?.value || '',
            Description: document.getElementById('description')?.value || '',
            Role: parseInt(document.querySelector('input[name="Role"]:checked')?.value || document.getElementById('role')?.value || '1'),
            Type: parseInt(document.querySelector('input[name="Type"]:checked')?.value || '1'),
            EndpointUrl: document.getElementById('endpointUrl')?.value || '',
            AuthenticationMode: document.getElementById('authMode')?.value || '',
            Username: document.getElementById('username')?.value || '',
            Password: document.getElementById('password')?.value || '',
            ThrottlingProfile: parseInt(document.getElementById('throttlingProfile')?.value || '1'),
            PreserveAuthorship: document.getElementById('preserveAuthorship')?.checked || false,
            PreserveTimestamps: document.getElementById('preserveTimestamps')?.checked || false,
            ReplaceIllegalCharacters: document.getElementById('replaceIllegalCharacters')?.checked || false,
            CurrentStep: currentStep,
            IsEditMode: document.getElementById('isEditMode')?.value === 'true'
        };
    }

    function updateReviewData() {
        const formData = gatherFormData();

        document.getElementById('reviewName').textContent = formData.Name || '-';

        const roleNames = { 1: 'Source', 2: 'Target' };
        document.getElementById('reviewRole').textContent = roleNames[formData.Role] || '-';

        const typeNames = { 1: 'SharePoint On-Prem', 2: 'SharePoint Online', 3: 'OneDrive for Business', 4: 'File Share' };
        document.getElementById('reviewType').textContent = typeNames[formData.Type] || '-';

        document.getElementById('reviewEndpoint').textContent = formData.EndpointUrl || '-';
        document.getElementById('reviewAuth').textContent = formData.AuthenticationMode || '-';

        const throttlingNames = { 1: 'Normal', 2: 'Insane' };
        document.getElementById('reviewThrottling').textContent = throttlingNames[formData.ThrottlingProfile] || '-';

        const options = [];
        if (formData.PreserveAuthorship) options.push('Preserve Authorship');
        if (formData.PreserveTimestamps) options.push('Preserve Timestamps');
        if (formData.ReplaceIllegalCharacters) options.push('Replace Illegal Characters');
        document.getElementById('reviewOptions').textContent = options.length > 0 ? options.join(', ') : 'None';
    }

    // Expose functions globally if needed
    window.wizardNavigateToStep = navigateToStep;
})();
