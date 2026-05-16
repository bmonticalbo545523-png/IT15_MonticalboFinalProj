/* ── Theme ─────────────────────────────────── */
const html = document.documentElement;
const THEME_KEY = 'projexis-theme';

function applyTheme(t) {
  html.setAttribute('data-theme', t);
  localStorage.setItem(THEME_KEY, t);
  const icon = document.getElementById('themeIcon');
  if (icon) { icon.className = t === 'dark' ? 'bi bi-sun-fill' : 'bi bi-moon-fill'; }
}
function toggleTheme() {
  applyTheme(html.getAttribute('data-theme') === 'dark' ? 'light' : 'dark');
}
applyTheme(localStorage.getItem(THEME_KEY) || 'light');

/* ── Sidebar ───────────────────────────────── */
function toggleSidebar() {
  const wrapper = document.querySelector('.dash-wrapper');
  const sidebar = document.querySelector('.sidebar');
  const overlay = document.querySelector('.sidebar-overlay');
  
  wrapper?.classList.toggle('sidebar-collapsed');
  
  if (window.innerWidth <= 768) {
    sidebar?.classList.toggle('mobile-open');
    if (!overlay) {
      const newOverlay = document.createElement('div');
      newOverlay.className = 'sidebar-overlay';
      newOverlay.onclick = toggleSidebar;
      sidebar?.after(newOverlay);
      // Small timeout to allow CSS transition
      setTimeout(() => newOverlay.classList.add('visible'), 10);
    } else {
      sidebar?.classList.remove('mobile-open');
      overlay.remove();
    }
  }
}

/* ── Toast ─────────────────────────────────── */
function showToast(msg, type = 'success', duration = 4000) {
  let container = document.getElementById('toast-container');
  if (!container) {
    container = document.createElement('div');
    container.id = 'toast-container';
    document.body.appendChild(container);
  }
  const icons = { success: 'bi-check-circle-fill', danger: 'bi-x-circle-fill', warning: 'bi-exclamation-triangle-fill', info: 'bi-info-circle-fill' };
  const toast = document.createElement('div');
  toast.className = `toast-item toast-${type}`;
  toast.innerHTML = `
    <i class="bi ${icons[type] || icons.info} toast-icon"></i>
    <span class="toast-msg">${msg}</span>
    <button class="toast-close" onclick="dismissToast(this.parentElement)"><i class="bi bi-x"></i></button>`;
  container.appendChild(toast);
  setTimeout(() => dismissToast(toast), duration);
}
function dismissToast(el) {
  el.classList.add('toast-hide');
  setTimeout(() => el.remove(), 350);
}

/* ── AJAX CRUD helpers ─────────────────────── */
function ajaxPost(url, formId, onSuccess) {
  const form = document.getElementById(formId);
  if (!form) return;
  const btn = form.querySelector('button[type="submit"]') || form.querySelector('button[onclick*="submit"]');
  if (btn) btn.classList.add('btn-loading');

  const data = new FormData(form);
  fetch(url, { method: 'POST', body: data })
    .then(r => r.json())
    .then(res => {
      if (res.success) {
        if (url.includes('Add') || url.includes('Complete') || url.includes('Confirm')) {
          showSuccessOverlay(res.message);
          setTimeout(() => { 
            const overlay = document.querySelector('.success-overlay');
            if (overlay) overlay.remove();
            if (onSuccess) onSuccess(res); 
            location.reload(); 
          }, 2000);
        } else {
          showToast(res.message, 'success');
          if (onSuccess) onSuccess(res);
          setTimeout(() => location.reload(), 1200);
        }
      } else {
        if (btn) btn.classList.remove('btn-loading');
        showToast(res.message || 'An error occurred.', 'danger');
      }
    })
    .catch(() => {
      if (btn) btn.classList.remove('btn-loading');
      showToast('Network error. Please try again.', 'danger');
    });
}

function showSuccessOverlay(msg) {
  const overlay = document.createElement('div');
  overlay.className = 'success-overlay';
  overlay.style.cursor = 'pointer';
  overlay.onclick = () => { overlay.remove(); location.reload(); };
  overlay.innerHTML = `
    <div class="success-circle"><i class="bi bi-check-lg"></i></div>
    <div class="success-title">Success!</div>
    <div class="success-msg">${msg}</div>
    <div class="mt-4 text-white-50 small">Click anywhere to continue</div>
  `;
  document.body.appendChild(overlay);
}

function loadDetails(url, fields) {
  fetch(url)
    .then(r => r.json())
    .then(data => { fields.forEach(f => { const el = document.getElementById(f.el); if (el) el.value = data[f.key] ?? ''; }); })
    .catch(() => showToast('Could not load details.', 'danger'));
}

/* ── Counter Animation ─────────────────────── */
function animateCounter(el, target, duration = 1200) {
  let start = 0, startTime = null;
  function step(timestamp) {
    if (!startTime) startTime = timestamp;
    const progress = Math.min((timestamp - startTime) / duration, 1);
    el.textContent = Math.floor(progress * target);
    if (progress < 1) requestAnimationFrame(step);
    else el.textContent = target;
  }
  requestAnimationFrame(step);
}
document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('[data-counter]').forEach(el => {
    animateCounter(el, parseInt(el.dataset.counter) || 0);
  });
});

/* ── Session Timeout Warning ───────────────── */
(function sessionWarning() {
  const TIMEOUT = 25 * 60 * 1000;
  let timer = setTimeout(() => {
    showToast('Your session will expire soon. Save your work!', 'warning', 8000);
  }, TIMEOUT);
  ['click', 'keydown', 'mousemove'].forEach(e =>
    document.addEventListener(e, () => { clearTimeout(timer); timer = setTimeout(() => showToast('Session expiring soon!', 'warning', 8000), TIMEOUT); }, { passive: true })
  );
})();

/* ── Active sidebar link ────────────────────── */
document.addEventListener('DOMContentLoaded', () => {
  const path = window.location.pathname;
  document.querySelectorAll('.sidebar-link').forEach(link => {
    if (link.getAttribute('href') === path) link.classList.add('active');
  });
});
