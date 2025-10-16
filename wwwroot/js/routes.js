// /wwwroot/js/routes.js
(() => {
    const $ = s => document.querySelector(s);

    // UI elemanları
    const ilSel = $('#il'), ilceSel = $('#ilce'), placesDiv = $('#places');
    const btnLoad = $('#btnLoadPlaces'), btnOpt = $('#btnOptimize'), btnSave = $('#btnSave');
    const btnSim = $('#btnSim'), btnMyLoc = $('#btnMyLoc'), btnClear = $('#btnClear');
    const btnExport = $('#btnExport'), fileImport = $('#fileImport'), stopList = $('#stopList');
    const routeName = $('#routeName'), summary = $('#summary');
    const speedInp = $('#speed'), consInp = $('#cons'), fuelInp = $('#fuelPrice');

    // API yardımcıları
    const api = {
        provinces: () => fetch('/api/routes/provinces').then(r => r.json()),
        districts: il => fetch(`/api/routes/districts?il=${encodeURIComponent(il)}`).then(r => r.json()),
        places: (il, ilce) => fetch(`/api/routes/places?il=${encodeURIComponent(il)}&ilce=${encodeURIComponent(ilce)}`).then(r => r.json()),
        optimize: stops => fetch('/api/routes/optimize', {
            method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(stops)
        }).then(r => r.json()),
        save: dto => fetch('/api/routes/save', {
            method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(dto)
        }).then(r => r.json())
    };

    // --- Harita ---
    const map = L.map('map', { zoomControl: true, attributionControl: true }).setView([39.0, 35.0], 6);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { maxZoom: 19, attribution: '&copy; OpenStreetMap' }).addTo(map);

    // Durum
    let stops = []; // {title,address,lat,lng,marker}
    let baseBefore = null; // optimize öncesi metrikler
    let polyline = L.polyline([], { weight: 4, opacity: .9 }).addTo(map);
    let carMarker = null, carTimer = null;

    // --- Hesaplayıcılar ---
    const kmBetween = (a, b) => {
        const toR = d => d * Math.PI / 180, R = 6371;
        const dLat = toR(b.lat - a.lat), dLng = toR(b.lng - a.lng), lat1 = toR(a.lat), lat2 = toR(b.lat);
        const h = Math.sin(dLat / 2) ** 2 + Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLng / 2) ** 2;
        return 2 * R * Math.asin(Math.sqrt(h));
    };
    const totalsOf = (arr) => {
        let km = 0; for (let i = 1; i < arr.length; i++) km += kmBetween(arr[i - 1], arr[i]);
        const speed = Math.max(5, +speedInp?.value || 35);   // km/s
        const cons = Math.max(1, +consInp?.value || 8.0);  // L/100km
        const price = Math.max(0, +fuelInp?.value || 43.0); // TL/L
        const serviceMin = Math.max(0, arr.length * 3);      // durak başı 3 dk varsayımı
        const driveMin = (km / speed) * 60;
        const minutes = driveMin + serviceMin;
        const liters = (km * cons) / 100;
        const cost = liters * price;
        return { km, minutes, liters, cost };
    };
    const formatTL = x => (x || 0).toLocaleString('tr-TR', { maximumFractionDigits: 2 });

    // --- Marker / Liste ---
    const popupHtml = s => {
        const addr = s.address ? `<div class="muted">${s.address}</div>` : '';
        return `<b>${s.title}</b>${addr}
      <div class="muted">${s.lat.toFixed(5)}, ${s.lng.toFixed(5)}</div>
      <div style="margin-top:6px"><button id="rmv" class="btn">Bu durağı sil</button></div>`;
    };

    function addStop({ title, address, lat, lng }) {
        const marker = L.marker([lat, lng], { draggable: true }).addTo(map);
        const s = { title: title || 'Durak', address: address || '', lat, lng, marker };
        marker.bindPopup(popupHtml(s));
        marker.on('dragend', e => { const p = e.target.getLatLng(); s.lat = p.lat; s.lng = p.lng; recalc(); });
        marker.on('popupopen', () => { const b = document.getElementById('rmv'); if (b) b.onclick = () => { removeStop(stops.indexOf(s)); map.closePopup(); }; });
        stops.push(s);
        recalc();
    }
    function removeStop(i) { if (i < 0 || i >= stops.length) return; map.removeLayer(stops[i].marker); stops.splice(i, 1); recalc(); }

    function refreshList() {
        if (!stopList) return;
        stopList.innerHTML = '';
        stops.forEach((s, i) => {
            const li = document.createElement('li'); li.draggable = true; li.dataset.index = String(i);
            li.innerHTML = `
        <div class="flex" style="gap:8px;align-items:center">
          <span class="muted">#${i + 1}</span>
          <input class="title" value="${s.title}" style="flex:1;min-width:140px" />
          <button class="btn" data-act="center">Göster</button>
          <button class="btn" data-act="del">Sil</button>
        </div>`;
            li.querySelector('.title').onchange = e => { s.title = e.target.value; s.marker.setPopupContent(popupHtml(s)); suggestName(); };
            li.querySelector('[data-act="center"]').onclick = () => { map.setView([s.lat, s.lng], Math.max(14, map.getZoom())); s.marker.openPopup(); };
            li.querySelector('[data-act="del"]').onclick = () => removeStop(i);

            // Drag & drop
            li.addEventListener('dragstart', ev => ev.dataTransfer.setData('text/plain', li.dataset.index));
            li.addEventListener('dragover', ev => ev.preventDefault());
            li.addEventListener('drop', ev => {
                ev.preventDefault();
                const from = parseInt(ev.dataTransfer.getData('text/plain'), 10);
                const to = parseInt(li.dataset.index, 10);
                if (from === to) return;
                const m = stops.splice(from, 1)[0]; stops.splice(to, 0, m);
                recalc();
            });

            stopList.appendChild(li);
        });
    }

    // --- Hesap/özet ---
    function recalc() {
        const latlngs = stops.map(s => [s.lat, s.lng]);
        polyline.setLatLngs(latlngs);
        if (latlngs.length >= 2) map.fitBounds(polyline.getBounds(), { padding: [20, 20] });

        const now = totalsOf(stops);
        const before = baseBefore;
        let text = `<b>${stops.length}</b> durak · <b>${now.km.toFixed(1)}</b> km · ~<b>${Math.round(now.minutes)}</b> dk · ` +
            `<b>${now.liters.toFixed(2)}</b> L · <b>${formatTL(now.cost)}</b> TL`;
        if (before) {
            const dKm = before.km - now.km, dMin = before.minutes - now.minutes, dLit = before.liters - now.liters, dCost = before.cost - now.cost;
            text += ` | Tasarruf: <b>${dKm.toFixed(1)} km</b>, <b>${Math.round(dMin)} dk</b>, <b>${dLit.toFixed(2)} L</b>, <b>${formatTL(dCost)} TL</b>`;
        }
        if (summary) summary.innerHTML = text;

        // Buton enable
        if (typeof window.__routeWizardReady === 'function') window.__routeWizardReady(stops.length >= 2);
        if (btnOpt) btnOpt.disabled = stops.length < 2;
        if (btnSave) btnSave.disabled = stops.length < 2;

        suggestName();
        updateCarTooltip(); // simülasyon çalışıyorsa
    }

    function suggestName() {
        if (!routeName) return;
        const il = ilSel?.value || 'Rota';
        const ilce = ilceSel?.value || '';
        const d = new Date();
        const date = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
        if (!routeName.value || routeName.value.startsWith('Rota') || routeName.value.match(/\/\d+ durak$/)) {
            routeName.value = `${il}${ilce ? '-(' + ilce + ')' : ''}-${date}/${stops.length} durak`;
        }
    }

    // --- İl/İlçe doldurma ---
    async function loadProvinces() {
        const pr = await api.provinces();
        ilSel.innerHTML = '<option value="">Seçiniz</option>';
        pr.forEach(p => ilSel.appendChild(new Option(p, p)));
    }
    async function loadDistricts() {
        ilceSel.innerHTML = '<option value="">Seçiniz</option>';
        if (!ilSel.value) return;
        const d = await api.districts(ilSel.value);
        ilceSel.appendChild(new Option('Tümü', '*ALL*')); // tüm ilçeler için
        d.forEach(x => ilceSel.appendChild(new Option(x, x)));
    }

    // --- Semtleri yükle (Tümü ise TEK İSTEK) ---
    async function loadPlaces() {
        if (!ilSel.value || !ilceSel.value) return;
        placesDiv.innerHTML = '<small>Yükleniyor...</small>';

        // Sunucu *ALL* destekli: tek istekle tüm ilçe+semt
        const list = await api.places(ilSel.value, ilceSel.value); // {key,semt,lat,lng,ilce}

        // Üst bar: Arama + Tümünü Ekle
        placesDiv.innerHTML = `
      <div class="flex" style="gap:8px;align-items:center;margin-bottom:6px">
        <input id="placeSearch" class="form-control" placeholder="Semt/ilçe ara..." style="flex:1" />
        <button id="btnAddAll" class="btn">Tümünü Ekle</button>
      </div>
      <div id="placeList"></div>
    `;

        const listEl = document.getElementById('placeList');
        const searchEl = document.getElementById('placeSearch');

        function render(rows) {
            listEl.innerHTML = '';
            const frag = document.createDocumentFragment();
            rows.forEach(p => {
                const row = document.createElement('div');
                row.style.cssText = 'display:flex;justify-content:space-between;gap:6px;padding:4px 2px;border-bottom:1px dashed #2a3c58;';
                row.innerHTML = `
          <span>${p.semt} <small class="muted">(${p.ilce})</small></span>
          <div class="flex" style="gap:6px">
            <button class="btn" data-act="add" data-title="${p.semt}" data-lat="${p.lat}" data-lng="${p.lng}">Ekle</button>
            <button class="btn" data-act="show" data-lat="${p.lat}" data-lng="${p.lng}">Göster</button>
          </div>`;
                frag.appendChild(row);
            });
            listEl.appendChild(frag);
        }
        render(list);

        // Arama
        searchEl.addEventListener('input', () => {
            const q = searchEl.value.trim().toLowerCase();
            if (!q) { render(list); return; }
            render(list.filter(p => p.semt.toLowerCase().includes(q) || String(p.ilce || '').toLowerCase().includes(q)));
        });

        // Delegation
        listEl.addEventListener('click', e => {
            const t = e.target;
            if (!(t instanceof HTMLElement)) return;
            const act = t.dataset.act;
            if (act === 'show') {
                map.setView([+t.dataset.lat, +t.dataset.lng], 13);
            } else if (act === 'add') {
                addStop({ title: t.dataset.title, address: '', lat: +t.dataset.lat, lng: +t.dataset.lng });
                map.setView([+t.dataset.lat, +t.dataset.lng], 13);
            }
        });

        // Tümünü Ekle (parça parça ekleyerek performans koru)
        document.getElementById('btnAddAll').addEventListener('click', () => {
            if (list.length > 300 && !confirm(`${list.length} semt eklenecek. Emin misiniz?`)) return;
            let i = 0;
            (function chunk() {
                const end = Math.min(i + 200, list.length);
                for (; i < end; i++) {
                    const p = list[i];
                    addStop({ title: p.semt, address: '', lat: p.lat, lng: p.lng });
                }
                if (i < list.length) {
                    (window.requestIdleCallback || setTimeout)(chunk, 0);
                }
            })();
        });

        if (list.length) map.setView([list[0].lat, list[0].lng], ilceSel.value === '*ALL*' ? 11 : 12);
    }

    // --- Optimize & Kaydet ---
    async function doOptimize() {
        if (stops.length < 2) return;
        if (!baseBefore) baseBefore = totalsOf(stops.slice());

        const payload = stops.map(s => ({ key: null, title: s.title, address: s.address, lat: s.lat, lng: s.lng }));
        const res = await api.optimize(payload);

        // Gelen sıraya göre diz
        const reordered = [];
        res.stops.forEach(o => {
            let i = stops.findIndex(s => Math.abs(s.lat - o.lat) < 1e-6 && Math.abs(s.lng - o.lng) < 1e-6 && s.title === o.title);
            if (i === -1) i = stops.findIndex(s => Math.abs(s.lat - o.lat) < 1e-6 && Math.abs(s.lng - o.lng) < 1e-6);
            if (i > -1) reordered.push(stops[i]);
            else {
                const m = L.marker([o.lat, o.lng], { draggable: true }).addTo(map);
                const s = { title: o.title, address: o.address || '', lat: o.lat, lng: o.lng, marker: m };
                m.bindPopup(popupHtml(s));
                m.on('dragend', e => { const p = e.target.getLatLng(); s.lat = p.lat; s.lng = p.lng; recalc(); });
                reordered.push(s);
            }
        });

        stops = reordered;
        recalc();
    }

    async function doSave() {
        if (stops.length < 2) return;
        const dto = {
            name: (routeName?.value || 'Yeni Rota').trim(),
            vehiclePlate: ($('#plate')?.value || '').trim(),
            stops: stops.map(s => ({ key: null, title: s.title, address: s.address, lat: s.lat, lng: s.lng }))
        };
        const res = await api.save(dto);
        if (res?.id) summary.innerHTML = `Kaydedildi: <a href="/routes/${res.id}">RoutePlan #${res.code}</a>`;
        else alert('Kaydetme sırasında sorun oluştu.');
    }

    // --- Simülasyon (hareket eden araç) ---
    function startSimulation() {
        if (stops.length < 2) { alert('Önce en az 2 durak ekleyin.'); return; }
        stopSimulation();

        const latlngs = stops.map(s => [s.lat, s.lng]);
        const speed = Math.max(5, +speedInp?.value || 35); // km/s
        const mps = speed * 1000 / 3600; // metre/s
        const carIcon = L.divIcon({ html: '🚚', className: 'car-emoji', iconSize: [24, 24], iconAnchor: [12, 12] });

        carMarker = L.marker(latlngs[0], { icon: carIcon }).addTo(map);
        carMarker.bindTooltip('Hazır', { permanent: false, direction: 'top', offset: [0, -12], sticky: true }).openTooltip();

        const total = totalsOf(stops);
        let distAccum = 0, timeAccum = 0, fuelAccum = 0, idx = 0;
        const cons = Math.max(1, +consInp?.value || 8.0);

        function step() {
            if (idx >= latlngs.length - 1) { stopSimulation(); return; }
            const a = L.latLng(latlngs[idx]), b = L.latLng(latlngs[idx + 1]);
            const segMeters = a.distanceTo(b);
            const segSeconds = segMeters / mps;
            const frames = Math.max(10, Math.round(segSeconds * 30)); // ~30 fps
            let f = 0;

            const mover = setInterval(() => {
                f++;
                const t = f / frames;
                const lat = a.lat + (b.lat - a.lat) * t;
                const lng = a.lng + (b.lng - a.lng) * t;
                carMarker.setLatLng([lat, lng]);

                const m = segMeters / frames;
                distAccum += m / 1000;
                timeAccum += 1 / 30;
                fuelAccum = distAccum * cons / 100;

                updateCarTooltip({ dist: distAccum, time: timeAccum, fuel: fuelAccum, total });
                if (f >= frames) { clearInterval(mover); idx++; step(); }
            }, 33);
            carTimer = mover;
        }
        step();
    }
    function stopSimulation() {
        if (carTimer) clearInterval(carTimer);
        carTimer = null;
        if (carMarker) { map.removeLayer(carMarker); carMarker = null; }
    }
    function updateCarTooltip(stats) {
        if (!carMarker || !stats) return;
        const min = Math.round(stats.time / 60);
        const text = `İlerleme: ${stats.dist.toFixed(1)} km · ~${min} dk · ${stats.fuel.toFixed(2)} L / ` +
            `Toplam: ${stats.total.km.toFixed(1)} km, ~${Math.round(stats.total.minutes)} dk`;
        carMarker.setTooltipContent(text);
    }

    // --- Diğer aksiyonlar ---
    function myLocation() {
        if (!navigator.geolocation) { alert('Geolocation desteklenmiyor.'); return; }
        navigator.geolocation.getCurrentPosition(p => {
            const { latitude, longitude } = p.coords;
            map.setView([latitude, longitude], 15);
            addStop({ title: 'Benim Konumum', address: '', lat: latitude, lng: longitude });
        }, _ => alert('Konum alınamadı.'));
    }
    function clearAll() { stopSimulation(); stops.forEach(s => map.removeLayer(s.marker)); stops = []; baseBefore = null; recalc(); }
    function exportData() {
        const rows = stops.map(s => ({ title: s.title, address: s.address, lat: s.lat, lng: s.lng }));
        if (!confirm('JSON olarak dışa aktar? (Hayır dersen CSV)')) {
            const header = 'title,address,lat,lng';
            const lines = rows.map(r => `"${r.title.replace(/"/g, '""')}","${(r.address || '').replace(/"/g, '""')}",${r.lat},${r.lng}`);
            const blob = new Blob([[header, ...lines].join('\n')], { type: 'text/csv' });
            const a = document.createElement('a'); a.href = URL.createObjectURL(blob); a.download = `rota-${new Date().toISOString().slice(0, 19)}.csv`; a.click();
            return;
        }
        const blob = new Blob([JSON.stringify(rows, null, 2)], { type: 'application/json' });
        const a = document.createElement('a'); a.href = URL.createObjectURL(blob); a.download = `rota-${new Date().toISOString().slice(0, 19)}.json`; a.click();
    }
    function importJson(file) {
        const r = new FileReader();
        r.onload = e => {
            try { const arr = JSON.parse(String(e.target.result)); clearAll(); (arr || []).forEach(o => addStop({ title: o.title, address: o.address, lat: o.lat, lng: o.lng })); }
            catch { alert('Geçersiz JSON.'); }
        };
        r.readAsText(file);
    }

    // --- Eventler ---
    map.on('dblclick', e => addStop({ title: `Nokta ${stops.length + 1}`, address: '', lat: e.latlng.lat, lng: e.latlng.lng }));
    map.on('contextmenu', e => { L.popup().setLatLng(e.latlng).setContent('Çift tıkla: durak ekle').openOn(map); setTimeout(() => map.closePopup(), 1200); });

    ilSel.addEventListener('change', async () => { await loadDistricts(); btnLoad.disabled = !(ilSel.value && ilceSel.value); suggestName(); });
    ilceSel.addEventListener('change', () => { btnLoad.disabled = !(ilSel.value && ilceSel.value); suggestName(); });

    btnLoad?.addEventListener('click', loadPlaces);
    btnOpt?.addEventListener('click', doOptimize);
    btnSave?.addEventListener('click', doSave);
    btnSim?.addEventListener('click', startSimulation);
    btnMyLoc?.addEventListener('click', myLocation);
    btnClear?.addEventListener('click', clearAll);
    btnExport?.addEventListener('click', exportData);
    fileImport?.addEventListener('change', e => { if (e.target.files?.length) importJson(e.target.files[0]); e.target.value = ''; });

    // Başlangıç
    (async () => { await loadProvinces(); })();
})();
