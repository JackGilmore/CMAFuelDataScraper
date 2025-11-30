/* Frontend logic: fetch retailers and stations, render cards and a Leaflet map.
   - Places assets in /assets/js and /assets/css
   - Uses API: https://m10tia6yk5af.api.tapintodata.com
*/

(() => {
    const API_BASE = 'https://m10tia6yk5af.api.tapintodata.com';
    let map, markersLayer;
    let lastStations = [];
    let selectedRetailerId = null;
    let fetchTimeout = null;

    function el(id) { return document.getElementById(id); }

    function setStatus(msg) {
        const s = el('status');
        if (s) s.textContent = msg || '';
    }

    async function fetchJson(url) {
        const res = await fetch(url);
        if (!res.ok) throw new Error('Network response was not ok: ' + res.status);
        return res.json();
    }

    async function loadRetailers() {
        try {
            setStatus('Loading retailers...');
            const retailers = await fetchJson(`${API_BASE}/fuel_retailers`);
            renderRetailerCards(retailers.data || []);
            setStatus('');
        } catch (err) {
            console.error(err);
            setStatus('Failed to load retailers');
        }
    }

    function renderRetailerCards(retailers) {
        const container = el('retailers');
        if (!container) return;
        container.innerHTML = '';

        // "All" card
        const allCol = document.createElement('div');
        allCol.className = 'col-auto';
        allCol.innerHTML = `<div class="retailer-card p-2 card-all btn btn-light ${selectedRetailerId ? '' : 'active'}" data-retailer-id="">All</div>`;
        container.appendChild(allCol);

        retailers.forEach(r => {
            const col = document.createElement('div');
            col.className = 'col-auto';
            const name = r.name || r.title || r.retailer_name || 'Unknown';
            col.innerHTML = `
        <div class="retailer-card p-2 d-flex flex-column ${selectedRetailerId == name ? 'active' : ''}" data-retailer-id="${escapeHtml(name)}">
          <div class="retailer-name">${escapeHtml(name)}</div>
          ${r.description ? `<div class="retailer-meta">${escapeHtml(r.description)}</div>` : ''}
        </div>`;
            container.appendChild(col);
        });

        // attach click handlers
        container.querySelectorAll('.retailer-card').forEach(card => {
            card.addEventListener('click', () => {
                const id = card.getAttribute('data-retailer-id') || null;
                // toggle
                if (id === (selectedRetailerId || '')) {
                    selectedRetailerId = null;
                } else {
                    selectedRetailerId = id || null;
                }
                // update active classes
                container.querySelectorAll('.retailer-card').forEach(c => c.classList.remove('active'));
                // find matching element and add active
                container.querySelectorAll('.retailer-card').forEach(c => {
                    if ((c.getAttribute('data-retailer-id') || null) === (selectedRetailerId || '')) c.classList.add('active');
                });
                // (re)render markers from lastStations
                renderStations(lastStations);
            });
        });
    }

    function escapeHtml(s) {
        if (!s) return '';
        return String(s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '\"': '&quot;', '\'': '&#39;' }[c]));
    }

    function initMap() {
        map = L.map('map', { attributionControl: true }).setView([55.770394, -3.339844], 6);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; OpenStreetMap contributors'
        }).addTo(map);
        // Use MarkerClusterGroup instead of regular LayerGroup
        markersLayer = L.markerClusterGroup({
            chunkedLoading: true,
            spiderfyOnMaxZoom: true,
            showCoverageOnHover: false,
            zoomToBoundsOnClick: true,
            maxClusterRadius: 60
        });
        map.addLayer(markersLayer);

        map.on('moveend', () => {
            // debounce frequent move events
            if (fetchTimeout) clearTimeout(fetchTimeout);
            fetchTimeout = setTimeout(() => {
                loadStationsForCurrentView();
            }, 350);
        });

        // initial load
        loadStationsForCurrentView();
    }

    function boundsToBboxArray(bounds) {
        // Return [minLon,minLat,maxLon,maxLat]
        return [bounds.getWest(), bounds.getSouth(), bounds.getEast(), bounds.getNorth()];
    }

    async function loadStationsForCurrentView() {
        try {
            const b = map.getBounds();
            const bbox = boundsToBboxArray(b);
            setStatus('Loading stations for current map view...');
            
            // Fetch all stations using pagination with cursor
            let allStations = [];
            let cursor = null;
            const limit = 1000; // Maximum allowed
            
            do {
                const bboxParam = encodeURIComponent(JSON.stringify(bbox));
                let url = `${API_BASE}/fuel_stations?bounding_box=${bboxParam}&limit=${limit}`;
                if (cursor) {
                    url += `&cursor=${encodeURIComponent(cursor)}`;
                }
                
                const response = await fetchJson(url);
                const stations = response.data || [];
                allStations = allStations.concat(stations);
                
                // Update status with progress
                setStatus(`Loading stations... (${allStations.length} so far)`);
                
                // Get next cursor from response metadata
                cursor = response._meta?.cursor || null;
                
                // Stop if no more data or cursor unchanged
                if (!cursor || stations.length === 0) {
                    break;
                }
            } while (true);
            
            lastStations = allStations;
            renderStations(lastStations);
            setStatus(`Loaded ${allStations.length} stations`);
            setTimeout(() => setStatus(''), 2000);
        } catch (err) {
            console.error(err);
            setStatus('Failed to load stations');
        }
    }

    function renderStations(stations) {
        markersLayer.clearLayers();
        if (!Array.isArray(stations)) return;

        const filtered = selectedRetailerId ? stations.filter(s => {
            // Match by retailer_name property
            const stationRetailer = s.retailer_name || s.retailer || s.group || '';
            return stationRetailer === selectedRetailerId;
        }) : stations;

        filtered.forEach(s => {
            const lat = s.latitude || s.lat || (s.location && s.location[1]);
            const lon = s.longitude || s.lng || s.lon || (s.location && s.location[0]);
            if (lat == null || lon == null) return;

            const name = s.name || s.station_name || s.address || s.id || 'Station';
            const retailer = s.retailer_name || s.retailer || s.group || '';
            
            // Build fuel price badges
            let fuelPrices = '';
            if (s.E10 != null) fuelPrices += `<span class="badge bg-success me-1">E10: ${s.E10}p</span>`;
            if (s.E5 != null) fuelPrices += `<span class="badge bg-success me-1">E5: ${s.E5}p</span>`;
            if (s.B7 != null) fuelPrices += `<span class="badge bg-dark me-1">B7: ${s.B7}p</span>`;
            if (s.SDV != null) fuelPrices += `<span class="badge bg-dark me-1">SDV: ${s.SDV}p</span>`;
            
            const popup = `
                <div style="min-width:180px">
                    <strong>${escapeHtml(name)}</strong>
                    <div class="small-muted mb-2">${escapeHtml(retailer)}</div>
                    ${fuelPrices ? `<div class="mt-2">${fuelPrices}</div>` : '<div class="text-muted small">No price data</div>'}
                </div>`;
            const marker = L.marker([parseFloat(lat), parseFloat(lon)]).bindPopup(popup);
            markersLayer.addLayer(marker);
        });

        // zoom to markers when filtering to a subset
        if (selectedRetailerId && markersLayer.getLayers().length) {
            try {
                map.fitBounds(markersLayer.getBounds(), { maxZoom: 14 });
            } catch (e) { /* ignore */ }
        }
    }

    // initialize after DOM ready
    document.addEventListener('DOMContentLoaded', () => {
        try {
            initMap();
            loadRetailers();
        } catch (err) {
            console.error('Init error', err);
            setStatus('Failed to initialize frontend');
        }
    });

})();
