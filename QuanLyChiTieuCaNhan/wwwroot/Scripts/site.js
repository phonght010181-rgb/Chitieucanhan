/* Keep transient feedback short and respect reduced-motion preferences. */
(function () {
    'use strict';
    if (window.Chart) {
        window.Chart.defaults.font.family = 'Rubik, "Segoe UI", sans-serif';
        window.Chart.defaults.color = '#7184ad';
        window.Chart.defaults.animation.duration = window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : 500;
    }
    document.querySelectorAll('form').forEach(function (form) {
        form.addEventListener('submit', function () {
            if (!form.checkValidity() || (window.jQuery && !window.jQuery(form).valid())) return;
            form.setAttribute('aria-busy', 'true');
        });
    });
    window.addEventListener('pageshow', function () {
        document.querySelectorAll('form[aria-busy]').forEach(function (form) { form.removeAttribute('aria-busy'); });
    });
})();
