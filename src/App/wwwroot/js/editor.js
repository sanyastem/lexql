let editor = null;
let completionRef = null;
let completionRegistered = false;

export function init(host, dotnetRef, initialValue, theme) {
    window.MonacoEnvironment = { getWorkerUrl: () => "data:text/javascript;charset=utf-8," };
    completionRef = dotnetRef;

    return new Promise((resolve, reject) => {
        try {
            require.config({ paths: { vs: "/lib/monaco/vs" } });
            require(["vs/editor/editor.main"], () => {
                editor = monaco.editor.create(host, {
                    value: initialValue ?? "",
                    language: "sql",
                    theme: monacoTheme(theme),
                    automaticLayout: true,
                    minimap: { enabled: false },
                    lineNumbers: "on",
                    scrollBeyondLastLine: false,
                    fontSize: 13,
                });

                editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.Enter, () => {
                    dotnetRef.invokeMethodAsync("OnRunRequested");
                });

                editor.addCommand(monaco.KeyCode.F8, () => {
                    dotnetRef.invokeMethodAsync("OnRunRequested");
                });

                editor.addCommand(monaco.KeyCode.F5, () => {
                    dotnetRef.invokeMethodAsync("OnRunAllRequested");
                });

                editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS, () => {
                    dotnetRef.invokeMethodAsync("OnSaveRequested");
                });

                editor.onDidChangeModelContent(() => {
                    dotnetRef.invokeMethodAsync("OnContentChanged");
                });

                registerCompletion();
                resolve();
            });
        } catch (error) {
            reject(error);
        }
    });
}

function registerCompletion() {
    if (completionRegistered) {
        return;
    }

    completionRegistered = true;
    monaco.languages.registerCompletionItemProvider("sql", {
        triggerCharacters: [".", " "],
        provideCompletionItems: async (model, position) => {
            if (!completionRef) {
                return { suggestions: [] };
            }

            const offset = model.getOffsetAt(position);
            const entries = await completionRef.invokeMethodAsync("GetCompletions", model.getValue(), offset);
            const word = model.getWordUntilPosition(position);
            const range = {
                startLineNumber: position.lineNumber,
                endLineNumber: position.lineNumber,
                startColumn: word.startColumn,
                endColumn: word.endColumn,
            };

            return {
                suggestions: entries.map((e) => ({
                    label: e.label,
                    kind: kindOf(e.kind),
                    detail: e.detail,
                    insertText: e.label,
                    range,
                })),
            };
        },
    });
}

function monacoTheme(theme) {
    return theme === "light" ? "vs" : "vs-dark";
}

export function setTheme(theme) {
    if (window.monaco) {
        monaco.editor.setTheme(monacoTheme(theme));
    }
}

function kindOf(kind) {
    const kinds = monaco.languages.CompletionItemKind;
    switch (kind) {
        case "Table": return kinds.Struct;
        case "Column": return kinds.Field;
        case "Function": return kinds.Function;
        case "Schema": return kinds.Module;
        case "Alias": return kinds.Variable;
        default: return kinds.Keyword;
    }
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
