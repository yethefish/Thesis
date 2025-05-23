export function setupUI(map) {
    document.getElementById('zoom-in').addEventListener('click', () => {
        const view = map.getView();
        view.setZoom(view.getZoom() + 1);
    });

    document.getElementById('zoom-out').addEventListener('click', () => {
        const view = map.getView();
        view.setZoom(view.getZoom() - 1);
    });

    document.getElementById('search-button').addEventListener('click', () => {
        const address = document.getElementById('address-input').value;
        searchAddress(address);
    });
}

export function toggleLockAnimation(isLocked) {
    const shackle = document.getElementById("lock-shackle");

    if (!shackle) {
        console.error("lock-shackle not found!");
        return;
    }

    shackle.setAttribute("d", isLocked 
        ? "M7 10V6a5 5 0 0 1 10 0v4" 
        : "M16 10V6a4 4 0 0 0-8-2");

    toggleMapInteraction(isLocked);
    toggleZoomButtons(isLocked);
    toggleSearchButton(isLocked);
    toggleAddressInput(isLocked);
}

function toggleMapInteraction(isLocked) {
    if (!window.map) {
        console.error("Карта не инициализирована!");
        return;
    }

    const interactions = window.map.getInteractions(); 

    interactions.forEach(interaction => {
        interaction.setActive(!isLocked);
    });
}

function toggleZoomButtons(isLocked) {
    const zoomInButton = document.getElementById("zoom-in");
    const zoomOutButton = document.getElementById("zoom-out");

    if (zoomInButton && zoomOutButton) {
        zoomInButton.disabled = isLocked;
        zoomOutButton.disabled = isLocked;
    }
}

function toggleSearchButton(isLocked) {
    const searchButton = document.getElementById("search-button");

    if (searchButton) {
        searchButton.disabled = isLocked;
    }
}

function toggleAddressInput(isLocked) {
    const addressInput = document.getElementById("address-input");

    if (addressInput) {
        addressInput.disabled = isLocked;
    }
}

export function toggleEditModeButton(isEditMode) {
    const button = document.getElementById("edit-mode-button");
    const editMenuEditButton = document.getElementById("edit-menu-edit-button");

    if (!editMenuEditButton) return;

    if (isEditMode) {
        editMenuEditButton.classList.add("active");
    } else {
        editMenuEditButton.classList.remove("active");
    }
}

export function toggleDeleteModeButton(isDeleteMode) {
    
    const editMenuDeleteButton = document.getElementById("edit-menu-delete-button");

    if (!editMenuDeleteButton) return;

    if (isDeleteMode) {
        editMenuDeleteButton.classList.add("active");
    } else {
        editMenuDeleteButton.classList.remove("active");
    }
}

export function toggleEditModeMenuButton(isEditMode, isDeleteMode) {
    const button = document.getElementById("edit-mode-button");
    if (!button) return;

    if (isDeleteMode || isEditMode) {
        button.classList.add("active");
    } else {
        button.classList.remove("active");
    }
}

export function toggleEditMenu(isOpen) {
    const editMenu = document.getElementById("edit-menu");

    if (!editMenu) return;

    if (isOpen) {
        editMenu.classList.add("show");
        updateEditMenuPosition();
        window.addEventListener("resize", updateEditMenuPosition);
    } else {
        editMenu.classList.remove("show");
        window.removeEventListener("resize", updateEditMenuPosition);
    }
}

export function updateEditMenuPosition() {
    const button = document.getElementById("edit-mode-button");
    const editMenu = document.getElementById("edit-menu");

    if (!button || !editMenu) return;

    const rect = button.getBoundingClientRect();
    const offsetX = 0;
    const offsetY = 12;

    const menuWidth = editMenu.offsetWidth;
    const buttonCenterX = rect.left + rect.width / 2;
    editMenu.style.top = `${rect.bottom + window.scrollY - offsetY}px`;
    editMenu.style.left = `${buttonCenterX - menuWidth / 1.1 + window.scrollX + offsetX}px`;
}

window.getAddressInputValue = function () {
    const addressInput = document.getElementById('address-input');
    if (addressInput) {
        return addressInput.value;
    }
    return null;
};

export function handleColorItemSelection(event) {
    const colorItems = document.querySelectorAll('.color-item');

    colorItems.forEach(item => item.classList.remove('selected'));

    event.currentTarget.classList.add('selected');
}

export function toggleStartFinishModeButton(isStartFinishMode) {
    const button = document.getElementById("start-finish-button");
    if (!button) return;

    if (isStartFinishMode) {
        button.classList.add("active");
    } else {
        button.classList.remove("active");
    }
}

export function downloadFile(filename, base64Content) {
    const link = document.createElement('a');
    link.href = `data:text/csv;base64,${base64Content}`;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}