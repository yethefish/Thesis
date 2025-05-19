import { setupUI } from './ui.js';

export let pointLayer;
export let pointSource;
export let lineLayer;
export let lineSource;
export let polygonLayer;
export let polygonSource;

// Объявляем переменные для управления перетаскиванием
let isDragging = false;
let selectedFeature = null;

export function initializeMap() {
    // Проверяем, что OpenLayers загружен
    if (!window.ol) {
        console.error("OpenLayers не загружен!");
        return;
    }

    // Создаем карту
    window.map = new ol.Map({
        target: 'map',
        layers: [
            new ol.layer.Tile({
                source: new ol.source.OSM()
            })
        ],
        view: new ol.View({
            center: ol.proj.fromLonLat([37.6173, 55.7558]), 
            zoom: 10
        }),
        controls: [
            // Добавляем ScaleLine в стандартные контролы
            new ol.control.ScaleLine()
        ]
    });

    // Проверяем, что карта создана
    if (!window.map) {
        console.error("Карта не была создана!");
        return;
    }

    // Инициализация слоев
    pointSource = new ol.source.Vector();
    pointLayer = new ol.layer.Vector({
        source: pointSource
    });

    lineSource = new ol.source.Vector();
    lineLayer = new ol.layer.Vector({
        source: lineSource
    });

    polygonSource = new ol.source.Vector();
    polygonLayer = new ol.layer.Vector({
        source: polygonSource,
        style: new ol.style.Style({
            fill: new ol.style.Fill({
                color: 'rgba(0, 0, 255, 0.3)'
            }),
            stroke: new ol.style.Stroke({
                color: 'blue',
                width: 2
            })
        })
    });

    // Добавляем слои на карту
    window.map.addLayer(polygonLayer);
    window.map.addLayer(pointLayer);
    window.map.addLayer(lineLayer);

    setupUI(window.map);

    window.map.on('click', function (event) {
        if (window.isStartFinishMode && window.dotNetReference) {
            const coordinate = ol.proj.toLonLat(event.coordinate);
            window.dotNetReference.invokeMethodAsync('SetStartPoint', coordinate);
            return;
        }

        if (window.isDeleteMode && window.dotNetReference) {
            // Находим ближайшую точку к месту клика
            const pixel = event.pixel;
            const feature = window.map.forEachFeatureAtPixel(pixel, function(f) {
                if (f.getGeometry().getType() === 'Point' && !f.get('isStartPoint') && !f.get('isEndPoint')) {
                    return f;
                }
            });
            
            if (feature) {
                const coord = ol.proj.toLonLat(feature.getGeometry().getCoordinates());
                window.dotNetReference.invokeMethodAsync('RemovePoint', coord);
            }
        }
    });

    window.map.on('contextmenu', function (event) {
        if (window.isStartFinishMode && window.dotNetReference) {
            const coordinate = ol.proj.toLonLat(event.coordinate);
            window.dotNetReference.invokeMethodAsync('SetEndPoint', coordinate);
            return;
        }

        if (window.isEditMode && window.dotNetReference) {
            const coordinate = ol.proj.toLonLat(event.coordinate);
            window.dotNetReference.invokeMethodAsync('AddPoint', coordinate);
        }
    });

    window.map.on('pointerdown', function (event) {
        if (!window.isEditMode || window.isStartFinishMode) return;
    
        // Выбираем точку, которая находится под курсором
        window.map.forEachFeatureAtPixel(event.pixel, function (feature) {
            if (feature.getGeometry().getType() === 'Point') {
                // Получаем координаты точки
                const coord = ol.proj.toLonLat(feature.getGeometry().getCoordinates());
    
                // Проверяем через dotNetReference, принадлежит ли точка выбранному дрону
                window.dotNetReference.invokeMethodAsync('IsPointBelongsToSelectedDrone', coord)
                    .then(isBelongs => {
                        if (isBelongs && !feature.get('isStartPoint') && !feature.get('isEndPoint')) {
                            selectedFeature = feature; // Фиксируем выбранную точку
                            isDragging = true;
                        }
                    });
                return true; // Прерываем цикл после нахождения точки
            }
        });
    });
    
    window.map.on('pointermove', function (event) {
        if (!isDragging || !selectedFeature) return;
    
        // Обновляем координаты выбранной точки
        const newCoord = ol.proj.toLonLat(event.coordinate);
        selectedFeature.getGeometry().setCoordinates(event.coordinate);
    
        // Обновляем координаты в C# через JSInterop
        if (window.dotNetReference) {
            window.dotNetReference.invokeMethodAsync('UpdatePoint', newCoord);
        }
    });
    
    window.map.on('pointerup', function (event) {
        if (!isDragging || !selectedFeature) return;
    
        // Завершаем перетаскивание и сбрасываем выбранную точку
        isDragging = false;
        selectedFeature = null;
    
        // Перерисовываем полигон только после завершения перемещения
        if (window.dotNetReference) {
            window.dotNetReference.invokeMethodAsync('RedrawAllPolygons');
        }
    });

    // Добавляем обработчик для обновления карты при изменении размера окна
    window.addEventListener('resize', function() {
        window.map.updateSize();
    });
}

// Экспортируем функцию для проверки режима старта/финиша
export function isInStartFinishMode() {
    return isStartFinishMode;
}