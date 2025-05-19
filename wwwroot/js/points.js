import { pointSource, lineSource } from './mapInit.js';

export function drawStartFinishPoints(startCoord, endCoord, color) {
    // Не очищаем все точки старта/финиша, а только добавляем новые
    clearDroneStartFinishPoints(color);
    
    // Рисуем стартовую точку с надписью "Н"
    if (startCoord) {
        const startPoint = new ol.Feature({
            geometry: new ol.geom.Point(ol.proj.fromLonLat(startCoord))
        });
        startPoint.set('isStartPoint', true);
        startPoint.set('droneColor', color); // Сохраняем цвет дрона
        
        const startStyle = new ol.style.Style({
            image: new ol.style.Circle({
                radius: 6,
                fill: new ol.style.Fill({ color: color }),
                stroke: new ol.style.Stroke({ color: 'green', width: 1 })
            }),
            text: new ol.style.Text({
                text: 'Н',
                fill: new ol.style.Fill({ color: 'green' }),
                font: 'bold 12px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Oxygen, Ubuntu, Cantarell',
                offsetY: -15
            })
        });
        startPoint.setStyle(startStyle);
        pointSource.addFeature(startPoint);
    }

    // Рисуем конечную точку с надписью "К"
    if (endCoord) {
        const endPoint = new ol.Feature({
            geometry: new ol.geom.Point(ol.proj.fromLonLat(endCoord))
        });
        endPoint.set('isEndPoint', true);
        endPoint.set('droneColor', color); // Сохраняем цвет дрона
        
        const endStyle = new ol.style.Style({
            image: new ol.style.Circle({
                radius: 6,
                fill: new ol.style.Fill({ color: color }),
                stroke: new ol.style.Stroke({ color: 'red', width: 1 })
            }),
            text: new ol.style.Text({
                text: 'К',
                fill: new ol.style.Fill({ color: 'red' }),
                font: 'bold 12px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Oxygen, Ubuntu, Cantarell',
                offsetY: -15
            })
        });
        endPoint.setStyle(endStyle);
        pointSource.addFeature(endPoint);
    }
}

export function clearDroneStartFinishPoints(droneColor) {
    const features = pointSource.getFeatures();
    const featuresToRemove = features.filter(feature => {
        return (feature.get('isStartPoint') || feature.get('isEndPoint')) && 
               feature.get('droneColor') === droneColor;
    });
    
    featuresToRemove.forEach(feature => {
        pointSource.removeFeature(feature);
    });
}

export function drawDronePath(coordinates, color) {
    clearDronePath(color);

    if (!coordinates || coordinates.length < 2) return;

    const line = new ol.Feature({
        geometry: new ol.geom.LineString(coordinates.map(coord => ol.proj.fromLonLat(coord)))
    });

    line.set('isDronePath', true);
    line.set('droneColor', color);

    line.setStyle(new ol.style.Style({
        stroke: new ol.style.Stroke({
            color: color,
            width: 2,
            lineDash: [5, 5]
        })
    }));

    lineSource.addFeature(line);

    // Добавляем маркеры для ключевых точек
    if (coordinates.length > 0) {
        // Стартовая точка пути (не путать с основной стартовой точкой)
        const startPoint = new ol.Feature({
            geometry: new ol.geom.Point(ol.proj.fromLonLat(coordinates[0]))
        });
        
        startPoint.set('isDronePath', true);
        startPoint.set('droneColor', color);
        
        startPoint.setStyle(new ol.style.Style({
            image: new ol.style.Circle({
                radius: 5,
                fill: new ol.style.Fill({ color: color }),
                stroke: new ol.style.Stroke({ color: 'white', width: 1 })
            })
        }));
        
        pointSource.addFeature(startPoint);

        // Конечная точка пути (не путать с основной финишной точкой)
        const endPoint = new ol.Feature({
            geometry: new ol.geom.Point(ol.proj.fromLonLat(coordinates[coordinates.length - 1]))
        });
        
        endPoint.set('isDronePath', true);
        endPoint.set('droneColor', color);
        
        endPoint.setStyle(new ol.style.Style({
            image: new ol.style.Circle({
                radius: 5,
                fill: new ol.style.Fill({ color: color }),
                stroke: new ol.style.Stroke({ color: 'white', width: 1 })
            })
        }));
        
        pointSource.addFeature(endPoint);
    }
}

export function clearDronePath(color) {
    // Очищаем линии пути
    const lineFeatures = lineSource.getFeatures();
    lineFeatures.forEach(feature => {
        if (feature.get('isDronePath') && feature.get('droneColor') === color) {
            lineSource.removeFeature(feature);
        }
    });

    // Очищаем маркеры пути (кроме стартовых/финишных точек)
    const pointFeatures = pointSource.getFeatures();
    pointFeatures.forEach(feature => {
        if (feature.get('isDronePath') && feature.get('droneColor') === color) {
            pointSource.removeFeature(feature);
        }
    });
}