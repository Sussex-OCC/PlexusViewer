function addRowHandlers() {
    var table = document.getElementById("exampleRecordsTable");
    var rows = table.getElementsByTagName("tr");
    for (i = 0; i < rows.length; i++) {
        var currentRow = table.rows[i];
        var createClickHandler = function (row) {
            return function () {
                var cell = row.getElementsByTagName("td")[1];
                var id = cell.innerHTML;

                $('#quickSearch').val(id.replace(/\s/g, ""));

            };
        };
        currentRow.onclick = createClickHandler(currentRow);
    }
}