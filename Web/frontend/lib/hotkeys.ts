type ShortcutHandler = (any) => void;

let keyboardHotKeys = {
};

function getHotKeyName(event) {
    if (event.ctrlKey) {
        return "ctrl+"+event.key;
    }

    return event.key;
}

export function registerHotKey(hotKey: string, handler: ShortcutHandler) {
    keyboardHotKeys[hotKey] = handler;
}

export function globalHotKeyHandler(event) {
    const hotKeyName = getHotKeyName(event);

    if (keyboardHotKeys[hotKeyName]) {
        keyboardHotKeys[hotKeyName](event);
        event.preventDefault();
    }
}