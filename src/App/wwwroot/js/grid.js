const tables = new WeakMap();
const states = new WeakMap();

export function renderGrid(element, columns, rows, options) {
    options = options || {};
    const editable = !!options.editable;
    const keyColumns = options.keyColumns || [];
    const keyIndexes = keyColumns
        .map((k) => columns.findIndex((c) => c.name.toLowerCase() === k.toLowerCase()))
        .filter((i) => i >= 0);

    const columnDefs = columns.map((c, i) => ({
        title: c.type ? `${c.name} (${c.type})` : c.name,
        field: `c${i}`,
        sorter: c.numeric ? "number" : "string",
        formatter: formatCell,
        editor: editable ? "input" : false,
    }));

    const data = rows.map((row, idx) => {
        const record = { _i: idx };
        row.forEach((value, i) => {
            record[`c${i}`] = value;
        });
        return record;
    });

    const existing = tables.get(element);
    if (existing) {
        existing.destroy();
    }

    const state = {
        columns,
        keyIndexes,
        updates: new Map(),
        inserts: new Map(),
        deletes: [],
        originalKeys: new Map(),
        nextInsertId: -1,
    };

    const table = new Tabulator(element, {
        data,
        columns: columnDefs,
        index: "_i",
        height: "100%",
        layout: "fitDataStretch",
        placeholder: "No rows",
        selectableRows: editable,
    });

    const dotnetRef = options.dotnetRef;
    if (dotnetRef) {
        table.on("cellClick", (e, cell) => {
            const value = cell.getValue();
            dotnetRef.invokeMethodAsync("OnCellClicked", value === null || value === undefined ? null : String(value));
        });
    }

    if (editable) {
        data.forEach((record) => state.originalKeys.set(record._i, keyMap(state, record)));
        table.on("cellEdited", (cell) => {
            const record = cell.getRow().getData();
            const id = record._i;
            if (state.inserts.has(id)) {
                return;
            }

            const index = parseInt(cell.getField().slice(1), 10);
            const update = state.updates.get(id) || {};
            update[state.columns[index].name] = stringOf(cell.getValue());
            state.updates.set(id, update);
        });
    }

    tables.set(element, table);
    states.set(element, state);
}

export function addRow(element) {
    const table = tables.get(element);
    const state = states.get(element);
    if (!table || !state) {
        return;
    }

    const id = state.nextInsertId--;
    const record = { _i: id };
    state.columns.forEach((c, i) => {
        record[`c${i}`] = "";
    });
    state.inserts.set(id, true);
    table.addRow(record, true);
}

export function deleteSelected(element) {
    const table = tables.get(element);
    const state = states.get(element);
    if (!table || !state) {
        return;
    }

    table.getSelectedRows().forEach((row) => {
        const record = row.getData();
        const id = record._i;
        if (state.inserts.has(id)) {
            state.inserts.delete(id);
        } else {
            state.deletes.push(state.originalKeys.get(id) || keyMap(state, record));
            state.updates.delete(id);
        }
        row.delete();
    });
}

export function getChanges(element) {
    const table = tables.get(element);
    const state = states.get(element);
    if (!table || !state) {
        return { updates: [], inserts: [], deletes: [] };
    }

    const updates = [];
    state.updates.forEach((values, id) => {
        updates.push({ key: state.originalKeys.get(id) || {}, values });
    });

    const inserts = [];
    table.getRows().forEach((row) => {
        const record = row.getData();
        if (state.inserts.has(record._i)) {
            const values = {};
            state.columns.forEach((c, i) => {
                values[c.name] = stringOf(record[`c${i}`]);
            });
            inserts.push({ values });
        }
    });

    return { updates, inserts, deletes: state.deletes };
}

function keyMap(state, record) {
    const map = {};
    state.keyIndexes.forEach((index) => {
        map[state.columns[index].name] = stringOf(record[`c${index}`]);
    });
    return map;
}

function stringOf(value) {
    return value === null || value === undefined ? null : String(value);
}

function formatCell(cell) {
    const value = cell.getValue();
    if (value === null || value === undefined) {
        return "<span class='cell-null'>NULL</span>";
    }
    return escapeHtml(String(value));
}

function escapeHtml(text) {
    return text
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll("\"", "&quot;")
        .replaceAll("'", "&#39;");
}
