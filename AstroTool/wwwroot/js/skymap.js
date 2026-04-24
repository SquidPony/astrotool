// AstroTool Sky Map Canvas Renderer
// Handles: alt/az sky projection, solar system view, window mode overlay

'use strict';

window.AstroTool = window.AstroTool || {};

// ─── Sky Map (Alt/Az Stereographic Projection) ───────────────────────────────

window.AstroTool.renderSkyMap = function (canvasId, bodies, options) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    const W = canvas.width = canvas.clientWidth || 800;
    const H = canvas.height = canvas.clientHeight || 800;
    const cx = W / 2, cy = H / 2;
    const R = Math.min(W, H) / 2 - 20; // radius of sky circle

    options = options || {};
    const showGrid    = options.showGrid    !== false;
    const showLabels  = options.showLabels  !== false;
    const showConstellLines = options.showConstellLines !== false;
    const limitMag    = options.limitMag    || 6.5;
    const northUp     = options.northUp     !== false;

    // Clear
    ctx.fillStyle = '#000510';
    ctx.fillRect(0, 0, W, H);

    // Horizon circle
    ctx.beginPath();
    ctx.arc(cx, cy, R, 0, 2 * Math.PI);
    ctx.strokeStyle = '#334';
    ctx.lineWidth = 2;
    ctx.stroke();
    ctx.save();
    ctx.clip();

    // ── Grid lines ──
    if (showGrid) {
        ctx.strokeStyle = '#223344';
        ctx.lineWidth = 0.5;

        // Altitude circles at 30°, 60°
        [30, 60].forEach(alt => {
            const r = altToRadius(alt, R);
            ctx.beginPath();
            ctx.arc(cx, cy, r, 0, 2 * Math.PI);
            ctx.stroke();
        });

        // Azimuth lines every 30°
        for (let az = 0; az < 360; az += 30) {
            const x = cx + R * Math.sin(az * Math.PI / 180);
            const y = cy - R * Math.cos(az * Math.PI / 180);
            ctx.beginPath();
            ctx.moveTo(cx, cy);
            ctx.lineTo(x, y);
            ctx.stroke();
        }
    }

    // Cardinal labels
    if (showLabels) {
        ctx.fillStyle = '#4a7fa5';
        ctx.font = '13px monospace';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        const d = R + 14;
        ctx.fillText('N', cx, cy - d);
        ctx.fillText('S', cx, cy + d);
        ctx.fillText('E', cx - d, cy); // E is left on sky view
        ctx.fillText('W', cx + d, cy);
    }

    // Altitude labels
    if (showGrid) {
        ctx.fillStyle = '#334466';
        ctx.font = '10px monospace';
        ctx.textAlign = 'left';
        [30, 60].forEach(alt => {
            const r = altToRadius(alt, R);
            ctx.fillText(alt + '°', cx + r + 2, cy);
        });
    }

    // ── Draw bodies ──
    if (bodies && bodies.length > 0) {
        bodies.forEach(body => {
            if (body.magnitude > limitMag) return;
            if (body.altitude < -1) return; // below horizon

            const { px, py } = altAzToCanvas(body.altitude, body.azimuth, R, cx, cy);
            const radius = magnitudeToRadius(body.magnitude, body.bodyType);
            const color = body.color || '#ffffff';

            if (body.bodyType === 'Sun' || body.bodyType === 0) {
                drawSun(ctx, px, py, Math.max(6, radius));
            } else if (body.bodyType === 'Moon' || body.bodyType === 4) {
                drawMoon(ctx, px, py, Math.max(5, radius), body.illuminatedFraction || 0.5, body.phaseAngle || 0);
            } else {
                drawStar(ctx, px, py, radius, color);
            }

            if (showLabels && radius > 2) {
                ctx.fillStyle = color;
                ctx.font = `${Math.max(9, Math.min(13, radius * 2))}px monospace`;
                ctx.textAlign = 'left';
                ctx.textBaseline = 'top';
                ctx.fillText(body.name, px + radius + 2, py - 4);
            }
        });
    }

    ctx.restore();

    // Zenith marker
    ctx.beginPath();
    ctx.arc(cx, cy, 3, 0, 2 * Math.PI);
    ctx.fillStyle = '#446';
    ctx.fill();
};

function altToRadius(alt, R) {
    // Stereographic: maps 90° (zenith) → 0, 0° (horizon) → R
    return R * (1 - alt / 90);
}

function altAzToCanvas(alt, az, R, cx, cy) {
    const r = altToRadius(alt, R);
    const azRad = az * Math.PI / 180;
    const px = cx + r * Math.sin(azRad);
    const py = cy - r * Math.cos(azRad);
    return { px, py };
}

function magnitudeToRadius(mag, type) {
    // Bigger = brighter (lower magnitude)
    if (type === 'Sun' || type === 0) return 12;
    if (type === 'Moon' || type === 4) return 9;
    return Math.max(1, 5 - mag * 0.7);
}

function drawStar(ctx, x, y, r, color) {
    // Glow
    if (r > 2) {
        const grd = ctx.createRadialGradient(x, y, 0, x, y, r * 3);
        grd.addColorStop(0, color + 'aa');
        grd.addColorStop(1, 'transparent');
        ctx.beginPath();
        ctx.arc(x, y, r * 3, 0, 2 * Math.PI);
        ctx.fillStyle = grd;
        ctx.fill();
    }
    ctx.beginPath();
    ctx.arc(x, y, Math.max(0.5, r), 0, 2 * Math.PI);
    ctx.fillStyle = color;
    ctx.fill();
}

function drawSun(ctx, x, y, r) {
    // Outer glow
    const grd = ctx.createRadialGradient(x, y, 0, x, y, r * 4);
    grd.addColorStop(0, '#fff7aa');
    grd.addColorStop(0.3, '#ffdd00aa');
    grd.addColorStop(1, 'transparent');
    ctx.beginPath();
    ctx.arc(x, y, r * 4, 0, 2 * Math.PI);
    ctx.fillStyle = grd;
    ctx.fill();

    // Disk
    ctx.beginPath();
    ctx.arc(x, y, r, 0, 2 * Math.PI);
    ctx.fillStyle = '#fff700';
    ctx.fill();
}

function drawMoon(ctx, x, y, r, illum, phaseAngle) {
    // Dark moon background
    ctx.beginPath();
    ctx.arc(x, y, r, 0, 2 * Math.PI);
    ctx.fillStyle = '#222';
    ctx.fill();

    // Illuminated portion (simplified crescent)
    ctx.save();
    ctx.beginPath();
    ctx.arc(x, y, r, 0, 2 * Math.PI);
    ctx.clip();

    const lit = (illum - 0.5) * 2; // -1 to +1
    ctx.beginPath();
    ctx.arc(x, y, r, -Math.PI / 2, Math.PI / 2);
    if (phaseAngle < 180) {
        // Waxing: right side lit
        ctx.bezierCurveTo(x + r * lit * 2, y + r, x + r * lit * 2, y - r, x, y - r);
    } else {
        ctx.bezierCurveTo(x - r * lit * 2, y + r, x - r * lit * 2, y - r, x, y - r);
    }
    ctx.fillStyle = '#d4d4d4';
    ctx.fill();

    ctx.restore();

    // Outline
    ctx.beginPath();
    ctx.arc(x, y, r, 0, 2 * Math.PI);
    ctx.strokeStyle = '#888';
    ctx.lineWidth = 0.5;
    ctx.stroke();
}


// ─── Solar System View ────────────────────────────────────────────────────────

window.AstroTool.renderSolarSystem = function (canvasId, planets, orbitPaths, options) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    const W = canvas.width = canvas.clientWidth || 800;
    const H = canvas.height = canvas.clientHeight || 800;
    const cx = W / 2, cy = H / 2;

    options = options || {};
    const scale      = options.scale || 60;   // pixels per AU
    const showLabels = options.showLabels !== false;
    const logScale   = options.logScale   || false;

    ctx.fillStyle = '#000510';
    ctx.fillRect(0, 0, W, H);

    // Sun
    ctx.beginPath();
    ctx.arc(cx, cy, 8, 0, 2 * Math.PI);
    const sunGrd = ctx.createRadialGradient(cx, cy, 0, cx, cy, 16);
    sunGrd.addColorStop(0, '#fff700');
    sunGrd.addColorStop(1, 'transparent');
    ctx.fillStyle = sunGrd;
    ctx.fill();
    ctx.beginPath();
    ctx.arc(cx, cy, 6, 0, 2 * Math.PI);
    ctx.fillStyle = '#fff700';
    ctx.fill();

    const planetColors = {
        'Mercury': '#b5b5b5', 'Venus': '#e8cda0', 'Earth': '#4fa3e0',
        'Mars': '#c1440e', 'Jupiter': '#c9a84c', 'Saturn': '#e8d5a3',
        'Uranus': '#7de8e8', 'Neptune': '#4169e1', 'Moon': '#c8c8c8'
    };

    const planetRadii = {
        'Mercury': 2, 'Venus': 3, 'Earth': 3, 'Mars': 2.5,
        'Jupiter': 7, 'Saturn': 6, 'Uranus': 4, 'Neptune': 4, 'Moon': 1.5
    };

    function auToPixel(au) {
        if (logScale && au > 0) {
            return Math.log10(Math.abs(au) + 0.1) * scale * 2.5;
        }
        return au * scale;
    }

    // Draw orbit paths
    if (orbitPaths) {
        for (const [name, pts] of Object.entries(orbitPaths)) {
            if (!pts || pts.length < 2) continue;
            ctx.beginPath();
            pts.forEach((pt, i) => {
                const px = cx + auToPixel(pt.x);
                const py = cy - auToPixel(pt.y);
                if (i === 0) ctx.moveTo(px, py);
                else ctx.lineTo(px, py);
            });
            ctx.closePath();
            ctx.strokeStyle = (planetColors[name] || '#333') + '55';
            ctx.lineWidth = 0.75;
            ctx.stroke();
        }
    }

    // Draw planets
    if (planets) {
        planets.forEach(p => {
            const px = cx + auToPixel(p.x);
            const py = cy - auToPixel(p.y);
            const r = planetRadii[p.name] || 3;
            const color = planetColors[p.name] || '#fff';

            ctx.beginPath();
            ctx.arc(px, py, r, 0, 2 * Math.PI);
            ctx.fillStyle = color;
            ctx.fill();

            // Saturn rings
            if (p.name === 'Saturn') {
                ctx.beginPath();
                ctx.ellipse(px, py, r * 2.5, r * 0.7, 0, 0, 2 * Math.PI);
                ctx.strokeStyle = '#e8d5a388';
                ctx.lineWidth = 2;
                ctx.stroke();
            }

            if (showLabels) {
                ctx.fillStyle = color;
                ctx.font = '10px monospace';
                ctx.textAlign = 'left';
                ctx.fillText(p.name, px + r + 3, py - 3);
            }
        });
    }
};


// ─── Window Mode Overlay ──────────────────────────────────────────────────────

window.AstroTool.renderWindowMode = function (canvasId, bodies, azimuth, altitude, fovDeg) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    const W = canvas.width = canvas.clientWidth || 400;
    const H = canvas.height = canvas.clientHeight || 700;

    // Transparent background for overlay effect
    ctx.clearRect(0, 0, W, H);

    // Crosshair
    ctx.strokeStyle = '#0f0';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(W / 2 - 20, H / 2);
    ctx.lineTo(W / 2 + 20, H / 2);
    ctx.moveTo(W / 2, H / 2 - 20);
    ctx.lineTo(W / 2, H / 2 + 20);
    ctx.stroke();

    fovDeg = fovDeg || 30;

    if (!bodies) return;

    bodies.forEach(body => {
        // Angular distance from center
        const dAz = ((body.azimuth - azimuth + 540) % 360) - 180;
        const dAlt = body.altitude - altitude;

        if (Math.abs(dAz) > fovDeg / 2 || Math.abs(dAlt) > fovDeg / 2) return;

        const px = W / 2 + (dAz / fovDeg) * W;
        const py = H / 2 - (dAlt / fovDeg) * H;
        const r = magnitudeToRadius(body.magnitude, body.bodyType);
        const color = body.color || '#ffffff';

        drawStar(ctx, px, py, Math.max(1.5, r), color);

        if (r > 2) {
            ctx.fillStyle = color + 'cc';
            ctx.font = '11px monospace';
            ctx.textAlign = 'left';
            ctx.fillText(body.name, px + r + 3, py - 3);
        }
    });

    // FOV compass
    ctx.fillStyle = '#0f0aa';
    ctx.font = '11px monospace';
    ctx.textAlign = 'right';
    ctx.fillStyle = '#00ff00aa';
    ctx.fillText(`Az: ${azimuth.toFixed(1)}°  Alt: ${altitude.toFixed(1)}°`, W - 5, H - 5);
};


// ─── Click Detection ──────────────────────────────────────────────────────────

window.AstroTool.findBodyAtClick = function (canvasId, clickX, clickY, bodies, viewType, options) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return null;

    const rect = canvas.getBoundingClientRect();
    const W = canvas.width;
    const H = canvas.height;
    const cx = W / 2, cy = H / 2;
    const R = Math.min(W, H) / 2 - 20;

    if (viewType === 'sky') {
        // Find closest body within 15px
        let closest = null, minDist = 15;
        (bodies || []).forEach(body => {
            if (body.altitude < 0) return;
            const { px, py } = altAzToCanvas(body.altitude, body.azimuth, R, cx, cy);
            const d = Math.hypot(clickX - px, clickY - py);
            if (d < minDist) { minDist = d; closest = body; }
        });
        return closest ? closest.name : null;
    }

    return null;
};


// ─── Utility: resize canvas to device pixel ratio ────────────────────────────

window.AstroTool.setupCanvas = function (canvasId) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const dpr = window.devicePixelRatio || 1;
    const rect = canvas.getBoundingClientRect();
    canvas.width = rect.width * dpr;
    canvas.height = rect.height * dpr;
    const ctx = canvas.getContext('2d');
    ctx.scale(dpr, dpr);
};
