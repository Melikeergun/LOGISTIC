document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.stat .num').forEach(el => {
        const k = Number(el.getAttribute('data-k') || 0);
        let v = 0, step = Math.max(1, Math.round(k / 60));
        const t = setInterval(() => { v += step; if (v >= k) { v = k; clearInterval(t); } el.textContent = v; }, 16);
    });
});
