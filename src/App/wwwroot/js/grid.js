const tables = new WeakMap();

export function renderGrid(element, columns, rows) {
    const columnDefs = columns.map((c, i) => ({
        title: c.type ? `${c.name} (${c.type})` : c.name,
        field: `c${i}`,
        sorter: c.numeric ? "number" : "string",
        formatter: formatCell,
    }));

    const data = rows.map((row, rowIndex) => {
        const record = { _i: rowIndex };
        row.forEach((value, i) => {
            record[`c${i}`] = value;
        });
        return record;
    });

    const existing = tables.get(element);
    if (existing) {
        existing.destroy();
    }

    const table = new Tabulator(element, {
        data,
        columns: columnDefs,
        index: "_i",
        height: "440px",
        layout: "fitDataStretch",
        placeholder: "No rows",
    });

    tables.set(element, table);
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
