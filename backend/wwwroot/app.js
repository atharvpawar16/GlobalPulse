// ── Config ────────────────────────────────────────────────────────────────────
const API = '';
const PAGE_SIZE = 30;
const CAT = {
  conflict:  { color: '#f85149', emoji: '⚔️' },
  disaster:  { color: '#d29922', emoji: '🌊' },
  political: { color: '#58a6ff', emoji: '🏛️' },
  cyber:     { color: '#bc8cff', emoji: '💻' },
  news:      { color: '#3fb950', emoji: '📰' },
};
const SEV_LABEL = ['', 'Low', 'Moderate', 'High', 'Critical', '🔴 CRITICAL'];

// ── Theme ─────────────────────────────────────────────────────────────────────
let isDark = localStorage.getItem('theme') !== 'light';
function applyTheme() {
  document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light');
  const icon = isDark ? '🌙' : '☀️';
  document.getElementById('btnThemeDesktop').textContent = icon;
  document.getElementById('btnThemeMobile').textContent = icon;
}
function toggleTheme() {
  isDark = !isDark;
  localStorage.setItem('theme', isDark ? 'dark' : 'light');
  applyTheme();
}

// ── Map ───────────────────────────────────────────────────────────────────────
const LAYERS = {
  satellite: L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
    attribution: 'Tiles © Esri', maxZoom: 18,
  }),
  hybrid: L.layerGroup([
    L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', { maxZoom: 18 }),
    L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/Reference/World_Boundaries_and_Places/MapServer/tile/{z}/{y}/{x}', { maxZoom: 18, opacity: 0.9 }),
  ]),
  street: L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '© OpenStreetMap contributors', maxZoom: 19,
  }),
};
let activeLayer = 'satellite';

const map = L.map('map', {
  center: [20, 10], zoom: 2, minZoom: 2, maxZoom: 18,
  zoomControl: false, worldCopyJump: true,
  maxBounds: [[-85, -Infinity], [85, Infinity]],
  maxBoundsViscosity: 1.0,
  layers: [LAYERS.satellite],
});
L.control.zoom({ position: 'bottomright' }).addTo(map);

const ro = new ResizeObserver(() => map.invalidateSize());
ro.observe(document.getElementById('mapContainer'));

function switchLayer(name) {
  if (name === activeLayer) return;
  map.removeLayer(LAYERS[activeLayer]);
  map.addLayer(LAYERS[name]);
  activeLayer = name;
  document.querySelectorAll('.layer-btn').forEach(b =>
    b.classList.toggle('active', b.dataset.layer === name));
}

// Marker cluster
const cluster = L.markerClusterGroup({
  maxClusterRadius: 40, spiderfyOnMaxZoom: true, showCoverageOnHover: false,
  iconCreateFunction(c) {
    return L.divIcon({ html: `<div class="cluster-icon">${c.getChildCount()}</div>`, className: '', iconSize: [36, 36] });
  },
});
map.addLayer(cluster);

const markerMap = {};
let allEvents = [];
let visibleCount = PAGE_SIZE;
const eventStore = {};

function makeIcon(category, severity) {
  const col = CAT[category]?.color || '#fff';
  const size = 12 + severity * 4;
  return L.divIcon({
    className: '',
    html: `<div class="ev-marker" style="width:${size}px;height:${size}px;background:${col};box-shadow:0 0 ${4+severity*3}px ${col},0 0 ${8+severity*4}px ${col}44;"></div>`,
    iconSize: [size, size], iconAnchor: [size/2, size/2],
  });
}

function addMarker(ev) {
  if (!ev.lat || !ev.lng || markerMap[ev.id]) return;
  const m = L.marker([ev.lat, ev.lng], { icon: makeIcon(ev.category, ev.severity) });
  m.bindPopup(`<div class="map-popup">
    <div class="popup-cat">${CAT[ev.category]?.emoji||''} ${ev.category}</div>
    <div class="popup-title">${escHtml(ev.title)}</div>
    <div class="popup-meta">${escHtml(ev.source)} · ${timeAgo(ev.occurredAt||ev.occurred_at)}</div>
  </div>`, { maxWidth: 260 });
  m.on('click', () => showDetail(ev));
  cluster.addLayer(m);
  markerMap[ev.id] = m;
}

function clearMarkers() {
  cluster.clearLayers();
  Object.keys(markerMap).forEach(k => delete markerMap[k]);
}

function flyTo(ev) {
  if (!ev.lat || !ev.lng) return;
  map.flyTo([ev.lat, ev.lng], 5, { duration: 1.2, easeLinearity: 0.3 });
}

// ── Geocoding ─────────────────────────────────────────────────────────────────
const geocodeCache = {};
async function geocode(query) {
  if (!query || geocodeCache[query] !== undefined) return geocodeCache[query] || null;
  try {
    const r = await fetch(`https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(query)}&format=json&limit=1`, { headers: { 'Accept-Language': 'en' } });
    const d = await r.json();
    if (d.length) { geocodeCache[query] = { lat: parseFloat(d[0].lat), lng: parseFloat(d[0].lon) }; return geocodeCache[query]; }
  } catch { /* ignore */ }
  geocodeCache[query] = null; return null;
}

async function enrichWithCoords(events) {
  for (const ev of events.filter(e => !e.lat && e.country).slice(0, 15)) {
    const c = await geocode(ev.country);
    if (c) { ev.lat = c.lat; ev.lng = c.lng; }
    await new Promise(r => setTimeout(r, 200));
  }
  return events;
}

// ── Load events ───────────────────────────────────────────────────────────────
async function loadEvents() {
  setLoading(true);
  const category = document.getElementById('filterCategory').value;
  const hours    = document.getElementById('filterHours').value;
  const severity = document.getElementById('filterSeverity').value;
  let url = `${API}/api/events?hours=${hours}&limit=300`;
  if (category) url += `&category=${category}`;
  if (severity) url += `&severity=${severity}`;
  try {
    const res = await fetch(url);
    const events = await res.json();
    if (Array.isArray(events) && events.length > 0) {
      allEvents = await enrichWithCoords(events);
    } else { loadDemoData(); return; }
  } catch { loadDemoData(); return; }
  finally { setLoading(false); }
  visibleCount = PAGE_SIZE;
  renderAll(allEvents);
  setLastUpdated();
}

function setLoading(on) {
  document.getElementById('loadingEvents').classList.toggle('hidden', !on);
}

function setLastUpdated() {
  const el = document.getElementById('lastUpdated');
  if (el) el.textContent = `· updated ${timeAgo(new Date().toISOString())}`;
}

function renderAll(events) {
  clearMarkers();
  events.forEach(addMarker);
  renderList(events);
  updateCount(events.length);
}

function renderList(events) {
  const el = document.getElementById('eventList');
  const btn = document.getElementById('btnLoadMore');
  if (!events.length) { el.innerHTML = `<div class="empty-msg">No events found.</div>`; btn.classList.add('hidden'); return; }
  events.forEach(ev => { eventStore[ev.id] = ev; });
  const slice = events.slice(0, visibleCount);
  el.innerHTML = slice.map(ev => `
    <div class="event-item sev-${ev.severity}" data-id="${ev.id}">
      <div class="ev-title">${CAT[ev.category]?.emoji||'📌'} ${escHtml(ev.title)}</div>
      <div class="ev-meta">${escHtml(ev.source)} · ${timeAgo(ev.occurredAt||ev.occurred_at)}</div>
    </div>`).join('');
  el.querySelectorAll('.event-item').forEach(item =>
    item.addEventListener('click', () => {
      const ev = eventStore[item.dataset.id];
      showDetail(ev); flyTo(ev);
      if (window.innerWidth <= 700) closeSidebar();
    })
  );
  btn.classList.toggle('hidden', visibleCount >= events.length);
}

function loadMore() {
  visibleCount += PAGE_SIZE;
  const q = document.getElementById('searchInput').value.toLowerCase().trim();
  const filtered = q ? allEvents.filter(ev =>
    ev.title?.toLowerCase().includes(q) || ev.summary?.toLowerCase().includes(q) ||
    ev.country?.toLowerCase().includes(q) || ev.source?.toLowerCase().includes(q)
  ) : allEvents;
  renderList(filtered);
}

function updateCount(n) {
  const el = document.getElementById('eventCount');
  if (el) el.textContent = n;
}

// ── Search ────────────────────────────────────────────────────────────────────
function onSearch() {
  const q = document.getElementById('searchInput').value.toLowerCase().trim();
  visibleCount = PAGE_SIZE;
  if (!q) { renderAll(allEvents); return; }
  const filtered = allEvents.filter(ev =>
    ev.title?.toLowerCase().includes(q) || ev.summary?.toLowerCase().includes(q) ||
    ev.country?.toLowerCase().includes(q) || ev.source?.toLowerCase().includes(q)
  );
  clearMarkers(); filtered.forEach(addMarker); renderList(filtered); updateCount(filtered.length);
}

// ── Stats ─────────────────────────────────────────────────────────────────────
async function loadStats() {
  try {
    const res = await fetch(`${API}/api/events/stats?hours=24`);
    const stats = await res.json();
    if (!Array.isArray(stats) || !stats.length) { computeStats(); return; }
    document.getElementById('statsBox').innerHTML = stats.map(s =>
      `<div class="stat-row"><span>${CAT[s.category]?.emoji||'📌'} ${s.category}</span><span class="stat-count">${s.count}</span></div>`
    ).join('');
  } catch { computeStats(); }
}

function computeStats() {
  const counts = {};
  allEvents.forEach(e => { counts[e.category] = (counts[e.category]||0) + 1; });
  document.getElementById('statsBox').innerHTML = Object.keys(counts).length
    ? Object.entries(counts).sort((a,b)=>b[1]-a[1]).map(([cat,n]) =>
        `<div class="stat-row"><span>${CAT[cat]?.emoji||'📌'} ${cat}</span><span class="stat-count">${n}</span></div>`
      ).join('')
    : Object.keys(CAT).map(cat =>
        `<div class="stat-row"><span>${CAT[cat].emoji} ${cat}</span><span class="stat-count">—</span></div>`
      ).join('');
}

function showDetail(ev) {
  if (!ev) return;
  document.getElementById('detailContent').innerHTML = `
    <h2>${escHtml(ev.title)}</h2>
    <div style="display:flex;gap:8px;align-items:center;margin-bottom:10px;flex-wrap:wrap">
      <span class="severity-badge badge-${ev.severity}">${SEV_LABEL[ev.severity]||ev.severity}</span>
      <button class="share-btn" onclick="shareEvent(${ev.id})" title="Copy link">🔗 Share</button>
    </div>
    <div class="meta">
      <b>${escHtml(ev.source)}</b> · ${CAT[ev.category]?.emoji||''} ${ev.category}<br>
      🕐 ${timeAgo(ev.occurredAt||ev.occurred_at)}
      ${ev.country ? `<br>📍 ${escHtml(ev.country)}` : ''}
      ${ev.lat ? `<br>🌐 ${Number(ev.lat).toFixed(2)}°, ${Number(ev.lng).toFixed(2)}°` : ''}
    </div>
    <p>${escHtml(ev.summary||'No summary available.')}</p>
    ${ev.url ? `<br><a href="${ev.url}" target="_blank" rel="noopener">Read full article →</a>` : ''}
  `;
  document.getElementById('detailPanel').classList.remove('hidden');
}

function shareEvent(id) {
  const ev = eventStore[id];
  if (!ev) return;
  const text = `${ev.title}\n${ev.url || window.location.href}`;
  navigator.clipboard?.writeText(text).then(() => showToast({ title: 'Link copied to clipboard', category: 'news', severity: 1 }));
}

function closeDetail() { document.getElementById('detailPanel').classList.add('hidden'); }
document.addEventListener('keydown', e => { if (e.key === 'Escape') closeDetail(); });

// ── Tabs ──────────────────────────────────────────────────────────────────────
function switchTab(name) {
  document.querySelectorAll('.tab').forEach((t,i) =>
    t.classList.toggle('active', ['events','alerts','feeds'][i] === name));
  document.querySelectorAll('.tab-content').forEach(el =>
    el.classList.toggle('hidden', !el.id.endsWith(name)));
  if (name === 'alerts') loadAlerts();
  if (name === 'feeds')  loadFeeds();
}

// ── Alerts ────────────────────────────────────────────────────────────────────
let alertRules = [];

async function loadAlerts() {
  const el = document.getElementById('alertList');
  try {
    alertRules = await (await fetch(`${API}/api/alerts`)).json();
    el.innerHTML = alertRules.length
      ? alertRules.map(r => `
          <div class="alert-item">
            <div class="alert-item-info">
              <div class="alert-item-name">${escHtml(r.name)}</div>
              <div class="alert-item-meta">
                ${r.category ? CAT[r.category]?.emoji+' '+r.category : 'All'}
                · Sev ${r.minSeverity}+
                ${r.country ? '· 📍'+escHtml(r.country) : ''}
              </div>
            </div>
            <button class="btn-delete" onclick="deleteAlert(${r.id})">🗑</button>
          </div>`).join('')
      : `<div class="empty-msg">No rules yet.</div>`;
  } catch { el.innerHTML = `<div class="empty-msg">Not available (no database).</div>`; }
}

function checkAlertRules(ev) {
  for (const rule of alertRules) {
    if (!rule.active) continue;
    if (ev.severity < rule.minSeverity) continue;
    if (rule.category && ev.category !== rule.category) continue;
    if (rule.country && ev.country?.toLowerCase() !== rule.country.toLowerCase()) continue;
    showToast({ ...ev, title: `🔔 Alert: ${rule.name}\n${ev.title}`, severity: Math.max(ev.severity, 3) });
  }
}

async function createAlert() {
  const name = document.getElementById('alertName').value.trim();
  if (!name) { alert('Enter a rule name'); return; }
  try {
    await fetch(`${API}/api/alerts`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        name,
        category:    document.getElementById('alertCategory').value || null,
        country:     document.getElementById('alertCountry').value.trim() || null,
        minSeverity: parseInt(document.getElementById('alertSeverity').value),
        active: true,
      }),
    });
    document.getElementById('alertName').value = '';
    document.getElementById('alertCountry').value = '';
    loadAlerts();
    showToast({ title: `Alert "${name}" created`, category: 'news', severity: 1 });
  } catch { alert('Could not save — database not connected.'); }
}

async function deleteAlert(id) {
  await fetch(`${API}/api/alerts/${id}`, { method: 'DELETE' });
  loadAlerts();
}

// ── Feeds ─────────────────────────────────────────────────────────────────────
const DEMO_FEEDS = [
  { id:1, name:'BBC World',               type:'rss', category:'news',     active:true, last_fetched:null },
  { id:2, name:'Al Jazeera',              type:'rss', category:'news',     active:true, last_fetched:null },
  { id:3, name:'Reuters World',           type:'rss', category:'news',     active:true, last_fetched:null },
  { id:4, name:'USGS Earthquakes M2.5+',  type:'rss', category:'disaster', active:true, last_fetched:null },
  { id:5, name:'GDACS Alerts',            type:'rss', category:'disaster', active:true, last_fetched:null },
  { id:6, name:'Krebs on Security',       type:'rss', category:'cyber',    active:true, last_fetched:null },
  { id:7, name:'The Hacker News',         type:'rss', category:'cyber',    active:true, last_fetched:null },
  { id:8, name:'Foreign Policy',          type:'rss', category:'political',active:true, last_fetched:null },
];

async function loadFeeds() {
  const el = document.getElementById('feedList');
  let feeds = [];
  try {
    const res = await fetch(`${API}/api/feeds`);
    feeds = await res.json();
    if (!feeds.length) feeds = DEMO_FEEDS;
  } catch { feeds = DEMO_FEEDS; }

  el.innerHTML = feeds.map(f => `
    <div class="feed-item">
      <div>
        <div class="feed-item-name">${escHtml(f.name)}</div>
        <div class="feed-item-meta">${(f.type||'').toUpperCase()} · ${f.category||'—'}
          ${f.last_fetched ? '<br>Last: '+timeAgo(f.last_fetched) : '<br>Demo mode'}
        </div>
      </div>
      <label class="toggle-switch">
        <input type="checkbox" ${f.active?'checked':''} onchange="toggleFeed(${f.id},this.checked)"/>
        <span class="toggle-slider"></span>
      </label>
    </div>`).join('');
}

async function toggleFeed(id, active) {
  try { await fetch(`${API}/api/feeds/${id}/toggle?active=${active}`, { method: 'PATCH' }); }
  catch { /* demo mode, ignore */ }
}

// ── SignalR ───────────────────────────────────────────────────────────────────
const connection = new signalR.HubConnectionBuilder()
  .withUrl('/hub/events').withAutomaticReconnect().build();

connection.on('NewEvent', json => {
  const ev = JSON.parse(json);
  if (allEvents.find(e => e.id === ev.id)) return;
  allEvents.unshift(ev);
  eventStore[ev.id] = ev;
  addMarker(ev);
  prependToList(ev);
  updateCount(allEvents.length);
  if (ev.severity >= 3) showToast(ev);
  checkAlertRules(ev);
});
connection.start().catch(() => {});

function prependToList(ev) {
  const el = document.getElementById('eventList');
  const div = document.createElement('div');
  div.className = `event-item sev-${ev.severity}`;
  div.innerHTML = `<div class="ev-title">${CAT[ev.category]?.emoji||'📌'} ${escHtml(ev.title)}</div>
    <div class="ev-meta">${escHtml(ev.source)} · just now</div>`;
  div.onclick = () => { showDetail(ev); flyTo(ev); };
  el.prepend(div);
}

// ── Toast ─────────────────────────────────────────────────────────────────────
function showToast(ev) {
  const t = document.createElement('div');
  t.className = `toast sev-${ev.severity}`;
  t.innerHTML = `<div class="toast-title">${CAT[ev.category]?.emoji||''} ${(ev.category||'').toUpperCase()}</div><div>${escHtml(ev.title)}</div>`;
  t.onclick = () => { showDetail(ev); flyTo(ev); t.remove(); };
  document.getElementById('toastContainer').appendChild(t);
  setTimeout(() => t.remove(), 7000);
}

// ── Mobile sidebar ────────────────────────────────────────────────────────────
function toggleSidebar() {
  const sidebar = document.getElementById('sidebar');
  const overlay = document.getElementById('sidebarOverlay');
  const isOpen = sidebar.classList.toggle('open');
  overlay.classList.toggle('visible', isOpen);
}

function closeSidebar() {
  document.getElementById('sidebar').classList.remove('open');
  document.getElementById('sidebarOverlay').classList.remove('visible');
}

// Close sidebar when tapping the map on mobile
document.getElementById('map').addEventListener('click', () => {
  if (window.innerWidth <= 700) closeSidebar();
});

// ── Demo data ─────────────────────────────────────────────────────────────────
function loadDemoData() {
  allEvents = [
    { id:1,  title:'7.2 Earthquake strikes off coast of Japan',              category:'disaster',  source:'USGS',       severity:4, lat:35.6,  lng:139.6,  country:'Japan',       occurredAt:new Date().toISOString(),                   summary:'A major earthquake was detected off the coast of Japan. Tsunami warnings issued for coastal areas.' },
    { id:2,  title:'Ceasefire negotiations collapse in conflict zone',        category:'conflict',  source:'Reuters',    severity:3, lat:33.8,  lng:35.5,   country:'Lebanon',     occurredAt:new Date(Date.now()-3600000).toISOString(),  summary:'Peace talks have broken down amid renewed fighting. International mediators calling for halt to hostilities.' },
    { id:3,  title:'Major ransomware attack hits European infrastructure',    category:'cyber',     source:'BBC',        severity:4, lat:52.5,  lng:13.4,   country:'Germany',     occurredAt:new Date(Date.now()-7200000).toISOString(),  summary:'Critical infrastructure in Germany targeted by ransomware. Power grid and hospital systems affected.' },
    { id:4,  title:'Wildfire spreads across 10,000 acres in California',     category:'disaster',  source:'NASA FIRMS', severity:3, lat:36.7,  lng:-119.7, country:'USA',         occurredAt:new Date(Date.now()-1800000).toISOString(),  summary:'Fast-moving wildfire forces evacuations across multiple counties in central California.' },
    { id:5,  title:'Emergency UN Security Council session called',           category:'political', source:'Al Jazeera', severity:2, lat:40.7,  lng:-74.0,  country:'USA',         occurredAt:new Date(Date.now()-900000).toISOString(),   summary:'Emergency session called to address escalating regional tensions and humanitarian concerns.' },
    { id:6,  title:'Tropical cyclone makes landfall in Southeast Asia',      category:'disaster',  source:'NOAA',       severity:5, lat:14.5,  lng:121.0,  country:'Philippines', occurredAt:new Date(Date.now()-600000).toISOString(),   summary:'Category 4 cyclone makes landfall. Over 2 million people in evacuation zones.' },
    { id:7,  title:'State-sponsored hacking group targets financial sector', category:'cyber',     source:'Reuters',    severity:3, lat:51.5,  lng:-0.1,   country:'UK',          occurredAt:new Date(Date.now()-5400000).toISOString(),  summary:'Multiple UK banks report coordinated intrusion attempts linked to foreign state actors.' },
    { id:8,  title:'Magnitude 6.1 earthquake hits Turkey',                  category:'disaster',  source:'USGS',       severity:3, lat:39.9,  lng:32.8,   country:'Turkey',      occurredAt:new Date(Date.now()-2700000).toISOString(),  summary:'6.1 magnitude earthquake struck central Turkey. Buildings damaged, casualties reported.' },
    { id:9,  title:'Protests erupt in capital over election results',        category:'political', source:'AP News',    severity:2, lat:-15.8, lng:-47.9,  country:'Brazil',      occurredAt:new Date(Date.now()-10800000).toISOString(), summary:'Tens of thousands take to the streets in Brasília following disputed election results.' },
    { id:10, title:'North Korea conducts ballistic missile test',            category:'conflict',  source:'BBC',        severity:5, lat:39.0,  lng:125.7,  country:'North Korea', occurredAt:new Date(Date.now()-14400000).toISOString(), summary:'ICBM launched, flew over Japan before landing in the Pacific Ocean.' },
  ];
  visibleCount = PAGE_SIZE;
  renderAll(allEvents);
  computeStats();
  setLastUpdated();
}

// ── Helpers ───────────────────────────────────────────────────────────────────
function timeAgo(iso) {
  if (!iso) return '—';
  const m = Math.floor((Date.now() - new Date(iso)) / 60000);
  if (m < 1) return 'just now';
  if (m < 60) return `${m}m ago`;
  const h = Math.floor(m / 60);
  if (h < 24) return `${h}h ago`;
  return `${Math.floor(h/24)}d ago`;
}
function escHtml(s) {
  return String(s??'').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

// ── Init ──────────────────────────────────────────────────────────────────────
applyTheme();
loadEvents();
loadStats();
loadAlerts();
setInterval(loadStats, 60000);
setInterval(() => { setLastUpdated(); }, 30000);
