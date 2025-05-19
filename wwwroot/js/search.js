export function searchAddress(address) 
{
    if (!window.map) {
        console.error("Карта не инициализирована!");
        return;
    }

    // Используем OpenLayers для поиска адреса
    const url = `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(address)}`;

    fetch(url)
        .then(response => response.json())
        .then(data => {
            if (data.length > 0) {
                const firstResult = data[0];
                const lon = parseFloat(firstResult.lon);
                const lat = parseFloat(firstResult.lat);

                // Перемещаем карту к найденному месту
                window.map.getView().setCenter(ol.proj.fromLonLat([lon, lat]));
                window.map.getView().setZoom(12); // Увеличиваем масштаб
            } else {
                console.error("Адрес не найден.");
            }
        })
        .catch(error => {
            console.error("Ошибка при поиске адреса:", error);
        });
}