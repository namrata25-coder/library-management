// Lightweight modal + UI helpers (no backend calls — UI only)

document.addEventListener('DOMContentLoaded', function () {
  fetch('/api/leads')
    .then(response => {
      if (!response.ok) throw new Error(`API request failed: ${response.status}`);
      return response.json();
    })
    .then(() => console.debug('Loaded leads from /api/leads'))
    .catch(error => console.error('Failed to load leads from /api/leads', error));
});

function openModal(id) {
  const el = document.getElementById(id);
  if (el) el.classList.add('show');
}

function closeModal(id) {
  const el = document.getElementById(id);
  if (el) el.classList.remove('show');
}

// Close modal when clicking the backdrop itself
document.addEventListener('click', function (e) {
  if (e.target.classList && e.target.classList.contains('modal-backdrop-custom')) {
    e.target.classList.remove('show');
  }
});

// Close modal with Escape key
document.addEventListener('keydown', function (e) {
  if (e.key === 'Escape') {
    document.querySelectorAll('.modal-backdrop-custom.show').forEach(m => m.classList.remove('show'));
  }
});

// Toggle inline "add" forms within modals (e.g., Add Call, Add Follow-up)
function toggleAddForm(id) {
  const el = document.getElementById(id);
  if (!el) return;
  el.style.display = (el.style.display === 'none' || !el.style.display) ? 'block' : 'none';
}

// Simple client-side table filter used on Assigned List page
function filterAssignedTable() {
  const search = (document.getElementById('searchInput')?.value || '').toLowerCase();
  const status = document.getElementById('statusFilter')?.value || '';
  const employee = document.getElementById('employeeFilter')?.value || '';
  const date = document.getElementById('dateFilter')?.value || '';

  document.querySelectorAll('#assignedTable tbody tr').forEach(row => {
    const text = row.innerText.toLowerCase();
    const rowStatus = row.getAttribute('data-status') || '';
    const rowEmployee = row.getAttribute('data-employee') || '';
    const rowDate = row.getAttribute('data-date') || '';

    const matchesSearch = !search || text.includes(search);
    const matchesStatus = !status || rowStatus === status;
    const matchesEmployee = !employee || rowEmployee === employee;
    const matchesDate = !date || rowDate === date;

    row.style.display = (matchesSearch && matchesStatus && matchesEmployee && matchesDate) ? '' : 'none';
  });
}

// Sidebar toggle for small screens
function toggleSidebar() {
  document.querySelector('.sidebar')?.classList.toggle('show-mobile');
}

// ===== Add / Edit Lead modal (shared by Lead List and Assigned List) =====

function setLeadFormField(fieldName, value) {
  const el = document.querySelector(`#leadForm [name="${fieldName}"]`);
  if (el) el.value = value ?? '';
}

// Opens the modal blank, ready to create a brand-new lead.
function openAddLeadModal() {
  const form = document.getElementById('leadForm');
  if (form) form.reset();
  setLeadFormField('Input.LeadId', '');
  document.getElementById('leadModalTitle').innerText = 'Add New Lead';
  document.getElementById('leadModalSubtitle').innerText = 'Capture full lead details to add them to your pipeline';
  document.getElementById('leadSaveBtnLabel').innerText = 'Save Lead';
  openModal('addLeadModal');
}

// Opens the modal pre-filled with an existing lead's data for editing.
function openEditLeadModal(lead) {
  setLeadFormField('Input.LeadId', lead.leadId);
  setLeadFormField('Input.LeadName', lead.leadName);
  setLeadFormField('Input.CompanyName', lead.companyName);
  setLeadFormField('Input.MobileNumber', lead.mobileNumber);
  setLeadFormField('Input.Email', lead.email);
  setLeadFormField('Input.Designation', lead.designation);
  setLeadFormField('Input.Location', lead.location);
  setLeadFormField('Input.Source', lead.source);
  setLeadFormField('Input.LeadStatus', lead.leadStatus);
  setLeadFormField('Input.AssignedTo', lead.assignedTo);
  setLeadFormField('Input.Priority', lead.priority);
  setLeadFormField('Input.FollowUpDate', lead.followUpDate);
  setLeadFormField('Input.Remarks', lead.remarks);

  document.getElementById('leadModalTitle').innerText = 'Edit Lead';
  document.getElementById('leadModalSubtitle').innerText = `Update details for ${lead.leadName} (${lead.leadId})`;
  document.getElementById('leadSaveBtnLabel').innerText = 'Update Lead';
  openModal('addLeadModal');
}

// Confirmation guard for delete forms.
function confirmDeleteLead(leadName) {
  return confirm(`Delete lead "${leadName}"? This action cannot be undone.`);
}

// ===== Lead Details: Tab interface (Call / Follow-up / Requirement / Assign) =====

// Switches the visible tab on the Lead Details page without any page reload.
function switchLeadTab(tabName) {
  if (!tabName) return;

  document.querySelectorAll('.tab-btn').forEach(btn => {
    btn.classList.toggle('active', btn.getAttribute('data-tab') === tabName);
  });

  document.querySelectorAll('.tab-panel').forEach(panel => {
    panel.classList.toggle('active', panel.id === `tab-${tabName}`);
  });
}

// Shows/hides the inline "Add Requirement" form within the Requirement tab.
function toggleRequirementForm() {
  const el = document.getElementById('addRequirementForm');
  if (!el) return;
  el.style.display = (el.style.display === 'none' || !el.style.display) ? 'block' : 'none';
}

// ===== Finance tab: Estimation / Quotation / Proposal sub-tabs =====

// Switches the visible Finance sub-tab (Estimation | Quotation | Proposal). Independent of switchLeadTab.
function switchFinanceSubTab(name) {
  if (!name) return;
  document.querySelectorAll('.fin-subtab-btn').forEach(btn => {
    btn.classList.toggle('active', btn.getAttribute('data-subtab') === name);
  });
  document.querySelectorAll('.fin-subpanel').forEach(panel => {
    panel.classList.toggle('active', panel.id === `finsub-${name}`);
  });
}

// Expands/collapses the read-only "View" detail block for a saved finance record.
function toggleFinanceDetail(detailId) {
  const el = document.getElementById(detailId);
  if (el) el.classList.toggle('open');
}

// Appends a new blank Item/Service row to an Estimation or Quotation items table.
function addFinanceItemRow(tbodyId) {
  const tbody = document.getElementById(tbodyId);
  if (!tbody) return;
  const row = document.createElement('tr');
  row.className = 'fin-item-row';
  row.innerHTML =
    '<td><input type="text" name="itemName" placeholder="Item / service name" oninput="recalcFinanceTotals(\'' + tbodyId + '\')" /></td>' +
    '<td><input type="number" name="itemQty" step="0.01" min="0" value="1" oninput="recalcFinanceTotals(\'' + tbodyId + '\')" /></td>' +
    '<td><input type="number" name="itemRate" step="0.01" min="0" value="0" oninput="recalcFinanceTotals(\'' + tbodyId + '\')" /></td>' +
    '<td class="fin-item-amount">0.00</td>' +
    '<td><button type="button" class="fin-remove-row" onclick="this.closest(\'tr\').remove(); recalcFinanceTotals(\'' + tbodyId + '\')"><i class="bi bi-trash"></i></button></td>';
  tbody.appendChild(row);
}

// Recomputes each row's Amount plus the Subtotal / Total preview for an Estimation or Quotation form.
// (Authoritative totals are still recalculated server-side on save.)
function recalcFinanceTotals(tbodyId) {
  const tbody = document.getElementById(tbodyId);
  if (!tbody) return;
  const prefix = tbodyId.replace('ItemsBody', '');

  let subtotal = 0;
  tbody.querySelectorAll('.fin-item-row').forEach(row => {
    const qty = parseFloat(row.querySelector('input[name=itemQty]').value) || 0;
    const rate = parseFloat(row.querySelector('input[name=itemRate]').value) || 0;
    const amt = qty * rate;
    row.querySelector('.fin-item-amount').textContent = amt.toFixed(2);
    subtotal += amt;
  });

  const subtotalEl = document.getElementById(prefix + 'SubtotalDisplay');
  if (subtotalEl) subtotalEl.textContent = subtotal.toFixed(2);

  const discountInput = document.getElementById(prefix + 'Discount');
  const taxInput = document.getElementById(prefix + 'Tax');
  const discount = discountInput ? (parseFloat(discountInput.value) || 0) : 0;
  const tax = taxInput ? (parseFloat(taxInput.value) || 0) : 0;

  const totalEl = document.getElementById(prefix + 'TotalDisplay');
  if (totalEl) totalEl.textContent = (subtotal - discount + tax).toFixed(2);
}

// Resets and opens an Estimation/Quotation/Proposal form for creating a brand-new record.
function newFinanceForm(kind) {
  const form = document.getElementById(kind + 'Form');
  if (!form) return;
  form.style.display = 'block';
  document.getElementById(kind + 'EditingNumber').value = '';
  document.getElementById(kind + 'FormTitle').textContent = 'New ' + capitalize(kind);
  document.getElementById(kind + 'NumberDisplay').textContent = 'Auto-generated on save';

  form.querySelectorAll('input[type=text], input[type=number], input[type=date], textarea').forEach(el => {
    if (el.name !== 'leadId') el.value = '';
  });
  const dateEl = document.getElementById(kind + 'Date');
  if (dateEl) dateEl.value = new Date().toISOString().slice(0, 10);
  const statusEl = document.getElementById(kind + 'Status');
  if (statusEl) statusEl.value = 'Draft';

  resetFinanceAttachment(kind);

  const tbody = document.getElementById(kind + 'ItemsBody');
  if (tbody) {
    tbody.innerHTML = '';
    addFinanceItemRow(kind + 'ItemsBody');
    recalcFinanceTotals(kind + 'ItemsBody');
  }

  form.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

// Hides an Estimation/Quotation/Proposal form (Cancel button).
function cancelFinanceForm(kind) {
  const form = document.getElementById(kind + 'Form');
  if (form) form.style.display = 'none';
}

// Populates an Estimation/Quotation/Proposal form with a saved record's data for editing.
// Reads the record's JSON payload embedded on the page (script#{kind}-json-{number}).
function editFinanceRecord(kind, number) {
  const dataEl = document.getElementById(kind + '-json-' + number);
  if (!dataEl) return;
  const d = JSON.parse(dataEl.textContent);

  const form = document.getElementById(kind + 'Form');
  form.style.display = 'block';
  document.getElementById(kind + 'EditingNumber').value = d.Number;
  document.getElementById(kind + 'FormTitle').textContent = 'Edit ' + capitalize(kind) + ' — ' + d.Number;
  document.getElementById(kind + 'NumberDisplay').textContent = d.Number;

  setVal(kind + 'Date', d.Date);
  setVal(kind + 'ProjectRequirement', d.ProjectRequirement);
  setVal(kind + 'Terms', d.TermsConditions);
  setVal(kind + 'Notes', d.Notes);
  setVal(kind + 'Status', d.Status);
  setVal(kind + 'ValidUntil', d.ValidUntil);
  setVal(kind + 'Discount', d.Discount);
  setVal(kind + 'Tax', d.Tax);

  // Proposal-only fields
  setVal(kind + 'Title', d.Title);
  setVal(kind + 'ScopeOfWork', d.ScopeOfWork);
  setVal(kind + 'Description', d.Description);
  setVal(kind + 'Deliverables', d.Deliverables);
  setVal(kind + 'Timeline', d.Timeline);
  setVal(kind + 'Budget', d.Budget);

  resetFinanceAttachment(kind);
  if (d.AttachmentPath) {
    const currentBox = document.getElementById(kind + 'CurrentAttachment');
    const currentLink = document.getElementById(kind + 'CurrentAttachmentLink');
    if (currentBox && currentLink) {
      currentLink.href = d.AttachmentPath;
      currentLink.textContent = d.AttachmentFileName || 'Attached file';
      currentBox.style.display = 'flex';
    }
  }

  const tbody = document.getElementById(kind + 'ItemsBody');
  if (tbody) {
    tbody.innerHTML = '';
    (d.Items || []).forEach(it => {
      addFinanceItemRow(kind + 'ItemsBody');
      const row = tbody.lastElementChild;
      row.querySelector('input[name=itemName]').value = it.ItemName;
      row.querySelector('input[name=itemQty]').value = it.Quantity;
      row.querySelector('input[name=itemRate]').value = it.Rate;
    });
    if ((d.Items || []).length === 0) addFinanceItemRow(kind + 'ItemsBody');
    recalcFinanceTotals(kind + 'ItemsBody');
  }

  form.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

function setVal(id, val) {
  const el = document.getElementById(id);
  if (el && val !== undefined && val !== null) el.value = val;
}

function capitalize(s) {
  return s.charAt(0).toUpperCase() + s.slice(1);
}

// Shows the picked file's name next to the "Choose File" button on a Finance form.
function previewFinanceAttachment(input, kind) {
  const nameEl = document.getElementById(kind + 'AttachmentFileName');
  if (!nameEl) return;
  nameEl.textContent = (input.files && input.files.length > 0) ? input.files[0].name : 'No file chosen';
}

// Clears the file input and any "current attachment" preview on a Finance form
// (called when opening a blank form and before populating an edit).
function resetFinanceAttachment(kind) {
  const fileInput = document.getElementById(kind + 'Attachment');
  if (fileInput) fileInput.value = '';
  const nameEl = document.getElementById(kind + 'AttachmentFileName');
  if (nameEl) nameEl.textContent = 'No file chosen';
  const currentBox = document.getElementById(kind + 'CurrentAttachment');
  if (currentBox) currentBox.style.display = 'none';
}
