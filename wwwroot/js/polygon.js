import { pointSource, lineSource, polygonSource } from './mapInit.js';

export function drawPoint(coordinate) {
    const point = new ol.Feature({
        geometry: new ol.geom.Point(ol.proj.fromLonLat(coordinate))
    });
    pointSource.addFeature(point);
}

export function removePoint(coordinate) {
    const features = pointSource.getFeatures();
    const pointToRemove = features.find(feature => {
        const featureCoord = ol.proj.toLonLat(feature.getGeometry().getCoordinates());
        return featureCoord[0] === coordinate[0] && featureCoord[1] === coordinate[1];
    });

    if (pointToRemove) {
        pointSource.removeFeature(pointToRemove);
    }
}

export function drawLine(startCoordinate, endCoordinate) {
    const line = new ol.Feature({
        geometry: new ol.geom.LineString([
            ol.proj.fromLonLat(startCoordinate),
            ol.proj.fromLonLat(endCoordinate)
        ])
    });
    lineSource.addFeature(line);
}

export function drawPolygon(coordinates, color) {
    if (coordinates.length < 3) return;

    let rgbaColor;
    if (color.startsWith('#')) {
        const r = parseInt(color.slice(1, 3), 16);
        const g = parseInt(color.slice(3, 5), 16);
        const b = parseInt(color.slice(5, 7), 16);
        rgbaColor = `rgba(${r}, ${g}, ${b}, 0.25)`;
    } else {
        rgbaColor = 'rgba(0, 0, 255, 0.5)';
    }

    const polygon = new ol.Feature({
        geometry: new ol.geom.Polygon([coordinates.map(coord => ol.proj.fromLonLat(coord))])
    });

    polygon.setStyle(new ol.style.Style({
        fill: new ol.style.Fill({
            color: rgbaColor
        }),
        stroke: new ol.style.Stroke({
            color: color,
            width: 2
        })
    }));

    if (coordinates.length >= 3) {
        const center = getPolygonCenter(coordinates);
        const area = calculatePolygonArea(coordinates);
        
        const areaText = new ol.Feature({
            geometry: new ol.geom.Point(ol.proj.fromLonLat(center))
        });
        
        areaText.setStyle(new ol.style.Style({
            text: new ol.style.Text({
                text: `${area.toFixed(2)} m²`,
                fill: new ol.style.Fill({ color: '#000000' }),
                backgroundFill: new ol.style.Fill({ color: 'rgba(255,255,255,0.7)' }),
                padding: [2, 2],
                offsetY: -20
            })
        }));
        
        pointSource.addFeature(areaText);
    }

    polygonSource.addFeature(polygon);
}

function getPolygonCenter(coordinates) {
    let latSum = 0;
    let lonSum = 0;
    const count = coordinates.length;
    
    for (const coord of coordinates) {
        latSum += coord[0];
        lonSum += coord[1];
    }
    
    return [lonSum / count, latSum / count];
}

function calculatePolygonArea(coordinates) {
    // Упрощенный расчет площади (для точного нужно использовать сферические формулы)
    let area = 0;
    const n = coordinates.length;
    
    for (let i = 0; i < n; i++) {
        const j = (i + 1) % n;
        area += coordinates[i][0] * coordinates[j][1];
        area -= coordinates[j][0] * coordinates[i][1];
    }
    
    area = Math.abs(area / 2);
    const metersPerDegree = 111320;
    return area * Math.pow(metersPerDegree, 2);
}

export function clearPolygon(polygon) {
    const features = polygonSource.getFeatures();
    features.forEach(feature => {
        if (feature.getGeometry().getType() === 'Polygon' || 
            feature.getGeometry().getType() === 'LineString' || 
            feature.getGeometry().getType() === 'Point') {
            polygonSource.removeFeature(feature);
        }
    });

    const pointFeatures = pointSource.getFeatures();
    pointFeatures.forEach(feature => {
        pointSource.removeFeature(feature);
    });

    const lineFeatures = lineSource.getFeatures();
    lineFeatures.forEach(feature => {
        lineSource.removeFeature(feature);
    });
}

export function clearMap() {
    pointSource.clear();
    lineSource.clear();
    polygonSource.clear();
}