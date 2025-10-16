

window.UI = (function () {
    "use strict";

    const $ = (s, c = document) => c.querySelector(s);
    const $$ = (s, c = document) => [...c.querySelectorAll(s)];

    const redirectToLogin = () => {
        const ret = encodeURIComponent(location.pathname + location.search);
        location.href = `/account/login?returnUrl=${ret}`;
    };

    // Genel API sarmalayıcı 
    const API = async (u, o = {}) => {
        const hasBody = o.body != null;
        const headers = Object.assign(
            { Accept: "application/json" },
            hasBody ? { "Content-Type": "application/json" } : {},
            o.headers || {}
        );

        const r = await fetch(u, Object.assign({ credentials: "include" }, o, { headers }));

        if (r.status === 401) {
            redirectToLogin();
            throw new Error("401 Unauthorized");
        }
        if (!r.ok) {
            // 404 gibi durumlarda 
            const txt = await r.text().catch(() => `${r.status} ${r.statusText}`);
            const err = new Error(txt);
            err.status = r.status;
            throw err;
        }
        const ct = r.headers.get("content-type") || "";
        return ct.includes("json") ? r.json() : r.text();
    };

    // /api/auth/me mevcutsa oturum metni
    const refreshMe = async () => {
        const box = $("#meBox");
        if (!box) return;
        try {
            const me = await API("/api/auth/me");
            if (typeof me === "string") { box.textContent = me; return; }
            box.textContent = `Oturum: ${(me.fullName || me.username || "-")} • Rol: ${me.role || "-"}`;
        } catch (e) {
            if (e.status === 404) return; 
            box.textContent = "Oturum yok";
        }
    };

    // ---- Metadata helpers----
    const getSchema = () => API("/api/metadata/schema");
    const getOptions = (field) =>
        API(`/api/metadata/options?field=${encodeURIComponent(field)}&maxPerField=50`);

    function buildInput(field, options, value) {
        const wrap = document.createElement("div");
        wrap.className = "form-row";
        const label = document.createElement("label");
        label.textContent = field;
        wrap.appendChild(label);

        const list = (options || []).filter((x) => x != null).map(String);
        const useSelect = list.length > 0 && list.length <= 30;

        let input;
        if (useSelect) {
            input = document.createElement("select");
            input.innerHTML =
                `<option value="">(seçiniz)</option>` +
                list.map((o) => `<option>${o}</option>`).join("");
            if (value != null) input.value = String(value);
        } else {
            input = document.createElement("input");
            const isNum = value != null && !isNaN(Number(value));
            input.type = isNum ? "number" : "text";
            input.value = value ?? "";
        }
        input.dataset.field = field;
        wrap.appendChild(input);
        return wrap;
    }

    // ---- Pages ----

    // Ana ekran (rol ve düzenlenebilir alan 
    async function pageIndex() {
        try {
            const s = await getSchema();
            const r = $("#roleName");
            if (r) r.textContent = s.role;
            const e = $("#editableList");
            if (e) e.innerHTML = (s.editableFields || []).map((f) => `<li>${f}</li>`).join("");
            const ro = $("#readonlyList");
            if (ro) ro.innerHTML = (s.readOnlyFields || []).map((f) => `<li>${f}</li>`).join("");
        } catch {
            const r = $("#roleName");
            if (r) r.textContent = "—";
            const e = $("#editableList");
            if (e) e.innerHTML = "<li>Giriş gerekli veya metadata uçları kapalı.</li>";
            const ro = $("#readonlyList");
            if (ro) ro.innerHTML = "<li>Giriş gerekli veya metadata uçları kapalı.</li>";
        }
        refreshMe();
    }

    // Sipariş listesi: Kaggle + Kullanıcı birleşik
    async function pageOrders() {
        const tbody = $("#ordersTable tbody");

        async function load() {
            if (tbody) tbody.innerHTML = `<tr><td colspan="7">Yükleniyor...</td></tr>`;

            let data;
            try {
                // Yeni birleşik uç
                data = await API(`/api/orders/browse?take=200`);
            } catch (e) {
                if (e.status === 404) {
                    // Eski uç (yalnızca dinamik siparişler)
                    data = await API(`/api/orders?take=100`);
                } else {
                    throw e;
                }
            }

            // /api/orders/browse -> dizi, /api/orders -> { items:[...] }
            const items = Array.isArray(data) ? data : (data.items || []);

            if (!tbody) return;
            tbody.innerHTML = items
                .map((o) => {
                    // browse nesnesi: source, orderId, status, purchasedAt, deliveredAt, customerId
                    if ("source" in o) {
                        return `<tr>
              <td>${o.source}</td>
              <td>${o.orderId}</td>
              <td>${o.status || "-"}</td>
              <td>${o.customerId || "-"}</td>
              <td>${o.purchasedAt ? new Date(o.purchasedAt).toLocaleString() : "-"}</td>
              <td>${o.deliveredAt ? new Date(o.deliveredAt).toLocaleString() : "-"}</td>
              <td></td>
            </tr>`;
                    } else {
                        const f = (() => { try { return JSON.parse(o.fieldsJson || "{}"); } catch { return {}; } })();
                        const pv = ["order_fulfillment_status", "delivery_preference", "warehouse_inventory_level"]
                            .map((k) => (f[k] ? `${k}: ${f[k]}` : null))
                            .filter(Boolean)
                            .join(" • ");
                        return `<tr>
              <td>user</td>
              <td>USR-${o.id}</td>
              <td>${o.status}</td>
              <td>${o.createdBy || "-"}</td>
              <td>${new Date(o.createdAt).toLocaleString()}</td>
              <td>${pv}</td>
              <td><a class="btn" href="/ui/orders/${o.id}">Düzenle</a></td>
            </tr>`;
                    }
                })
                .join("");
        }

        const btnS = $("#btnSearch");
        if (btnS) btnS.onclick = load;
        const btnC = $("#btnCreate");
        if (btnC) btnC.onclick = () => (location.href = "/ui/orders/create");

        await load();
        refreshMe();
    }

    // Yeni dinamik sipariş
    async function pageCreate() {
        const form = $("#dynForm");

        // Metadata tabanlı alanlar (opsiyonel)
        try {
            const s = await getSchema();
            for (const f of s.editableFields || []) {
                try {
                    const o = await getOptions(f);
                    form && form.appendChild(buildInput(f, o.options));
                } catch {
                    form && form.appendChild(buildInput(f, [], ""));
                }
            }
        } catch {
            // Metadata uçları yoksa boş formu göster
        }

        const btn = $("#btnSubmit");
        if (btn)
            btn.onclick = async () => {
                const dto = { status: "created", fields: {} };
                $$(".form-row input,.form-row select", form).forEach((i) => {
                    if (i.value !== "")
                        dto.fields[i.dataset.field] =
                            i.type === "number" && isFinite(+i.value) ? +i.value : i.value;
                });

                try {
                    // Yeni uç
                    const r = await API("/api/orders/dynamic", {
                        method: "POST",
                        body: JSON.stringify(dto),
                    });
                    alert("Oluşturuldu #" + r.id);
                    location.href = `/ui/orders`;
                } catch (e) {
                    if (e.status === 404) {
                        // Eski uç
                        const r = await API("/api/orders", {
                            method: "POST",
                            body: JSON.stringify(dto),
                        });
                        alert("Oluşturuldu #" + r.id);
                        location.href = `/ui/orders/${r.id}`;
                    } else {
                        throw e;
                    }
                }
            };

        refreshMe();
    }

    // Dinamik sipariş düzenleme (eski uçlar)
    async function pageEdit(id) {
        const wrap = $("#editWrap");
        const header = $("#ordHeader");

        const o = await API(`/api/orders/${id}`);
        if (header) header.textContent = `#${o.id} – ${o.orderNo || "USR-" + o.id} – ${o.status}`;

        let schema;
        try {
            schema = await getSchema();
        } catch {
            schema = { editableFields: [] };
        }
        const bag = (() => {
            try {
                return JSON.parse(o.fieldsJson || "{}");
            } catch {
                return {};
            }
        })();

        for (const f of schema.editableFields || []) {
            try {
                const opt = await getOptions(f);
                wrap && wrap.appendChild(buildInput(f, opt.options, bag[f]));
            } catch {
                wrap && wrap.appendChild(buildInput(f, [], bag[f]));
            }
        }

        const btn = $("#btnSave");
        if (btn)
            btn.onclick = async () => {
                const dto = { fields: {} };
                $$(".form-row input,.form-row select", wrap).forEach((i) => {
                    if (i.value !== "")
                        dto.fields[i.dataset.field] =
                            i.type === "number" && isFinite(+i.value) ? +i.value : i.value;
                });
                await API(`/api/orders/${id}`, {
                    method: "PUT",
                    body: JSON.stringify(dto),
                });
                alert("Kaydedildi.");
                location.reload();
            };

        refreshMe();
    }

    // Basit dashboard (opsiyonel)
    async function pageDashboard() {
        try {
            const res = await API("/api/reports/kpi");
            const k = res.kpi || res || {};
            $("#kpiLastEtl") && ($("#kpiLastEtl").textContent = res.lastEtlUtc ? new Date(res.lastEtlUtc).toLocaleString() : "—");
            $("#kpiEtlOrders30") && ($("#kpiEtlOrders30").textContent = k.orders_30d ?? k.etl_orders_30d ?? 0);
            $("#kpiAvgDelivery") && ($("#kpiAvgDelivery").textContent = k.avg_delivery_days ?? k.etl_avg_delivery_days ?? 0);
            $("#kpiDynReturn") && ($("#kpiDynReturn").textContent = (k.dyn_return_rate ?? 0) + "%");
        } catch (e) {
            console.warn(e);
        }
        refreshMe();
    }

    // public API
    return {
        refreshMe,
        pageIndex,
        pageOrders,
        pageCreate,
        pageEdit,
        pageDashboard,
    };
})();
