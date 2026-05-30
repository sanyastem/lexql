let editor = null;

export function init(host, dotnetRef, initialValue) {
    window.MonacoEnvironment = { getWorkerUrl: () => "data:text/javascript;charset=utf-8," };

    return new Promise((resolve, reject) => {
        try {
            require.config({ paths: { vs: "/lib/monaco/vs" } });
            require(["vs/editor/editor.main"], () => {
                editor = monaco.editor.create(host, {
                    value: initialValue ?? "",
                    language: "sql",
                    theme: "vs",
                    automaticLayout: true,
                    minimap: { enabled: false },
                    lineNumbers: "on",
                    scrollBeyondLastLine: false,
                    fontSize: 13,
                });

                editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.Enter, () => {
                    dotnetRef.invokeMethodAsync("OnRunRequested");
                });

                resolve();
            });
        } catch (error) {
            reject(error);
        }
    });
}

export function getValue() {
    return editor ? editor.getValue() : "";
}

export function setValue(value) {
    if (editor) {
        editor.setValue(value ?? "");
    }
}

export function getSelectedText() {
    if (!editor) {
        return "";
    }

    const selection = editor.getSelection();
    if (!selection || selection.isEmpty()) {
        return "";
    }

    return editor.getModel().getValueInRange(selection);
}

export function getCursorOffset() {
    if (!editor) {
        return 0;
    }

    return editor.getModel().getOffsetAt(editor.getPosition());
}

export function dispose() {
    if (editor) {
        editor.dispose();
        editor = null;
    }
}
