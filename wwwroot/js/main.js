import { initializeMap } from './mapInit.js';
import { toggleLockAnimation, toggleEditModeButton, toggleDeleteModeButton, toggleEditMenu, toggleEditModeMenuButton, updateEditMenuPosition, handleColorItemSelection, toggleStartFinishModeButton, downloadFile } from './ui.js';
import { drawPoint, drawLine, drawPolygon, clearMap, clearPolygon } from './polygon.js';
import { searchAddress } from './search.js';
import { drawStartFinishPoints, clearDroneStartFinishPoints, drawDronePath, clearDronePath } from './points.js';

window.initializeMap = initializeMap;
window.toggleLockAnimation = toggleLockAnimation;
window.toggleEditModeButton = toggleEditModeButton;
window.toggleDeleteModeButton = toggleDeleteModeButton;
window.toggleEditMenu = toggleEditMenu;
window.toggleEditModeMenuButton = toggleEditModeMenuButton;
window.updateEditMenuPosition = updateEditMenuPosition;
window.drawPoint = drawPoint;
window.drawLine = drawLine;
window.drawPolygon = drawPolygon;
window.clearMap = clearMap;
window.searchAddress = searchAddress;
window.clearPolygon = clearPolygon;
window.drawStartFinishPoints = drawStartFinishPoints;
window.clearDroneStartFinishPoints = clearDroneStartFinishPoints;
window.toggleStartFinishModeButton = toggleStartFinishModeButton;
window.drawDronePath = drawDronePath;
window.clearDronePath = clearDronePath;
window.downloadFile = downloadFile;

window.setDotNetReference = (dotNetRef) => {
    window.dotNetReference = dotNetRef;
};

window.toggleEditMode = (isEditMode) => {
    window.isEditMode = isEditMode;
    toggleEditModeButton(isEditMode);
    toggleEditModeMenuButton(window.isEditMode, window.isDeleteMode);
};

window.toggleDeleteMode = (isDeleteMode) => {
    window.isDeleteMode = isDeleteMode;
    toggleDeleteModeButton(isDeleteMode);
    toggleEditModeMenuButton(window.isEditMode, window.isDeleteMode);
};

window.toggleStartFinishMode = (isStartFinishMode) => {
    window.isStartFinishMode = isStartFinishMode;
    toggleStartFinishModeButton(isStartFinishMode);
    toggleEditModeMenuButton(window.isEditMode, window.isDeleteMode);
    
    // Отключаем другие режимы при активации этого
    if (isStartFinishMode && window.dotNetReference) {
        window.dotNetReference.invokeMethodAsync('ToggleEditMode', false);
        window.dotNetReference.invokeMethodAsync('ToggleDeleteMode', false);
        window.dotNetReference.invokeMethodAsync('OpenEditMenu', false);
    }
};

// Убедимся, что DOM полностью загружен перед добавлением обработчиков
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.color-item').forEach(item => {
        item.addEventListener('click', handleColorItemSelection);
    });

    // Обработчик для кнопки старта/финиша
    const startFinishButton = document.getElementById('start-finish-button');
    if (startFinishButton) {
        startFinishButton.addEventListener('click', function() {
            window.toggleStartFinishMode(!window.isStartFinishMode);
        });
    }
});