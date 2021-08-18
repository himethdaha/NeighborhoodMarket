var dataTable;

//When the documents is ready. Render the function
$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#DataTable').DataTable({
        "ajax": {
            "url": "/Admin/Category/GetAll"
        },
        "columns": [
            { "data": "categoryName", "width": "60%" },
            {
                "data": "id",
                "render": function (data) {
                    return ` <div class="text-center">
                                <a href="/Admin/Category/Upsert/${data}" class="btn btn-success text-white">
                                    <i class="fas fa-pencil-alt"></i>&nbsp;
                                </a>
                                <a onclick=Delete("/Admin/Category/Delete/${data}") class="btn btn-danger text-white">
                                    <i class="fas fa-minus-circle"></i>&nbsp;
                                </a>
                            </div>
                    `;
                }, "width": "40%"
            }
        ]
    });
}

function Delete(url) {
    swal({
        icon: "warning",
        dangerMode: true,
        title: "Do you really want to delete this?",
        text: "You will not be able to revert this action",
        buttons: true
    }).then((willDelete) => {
        if (willDelete) {
            $.ajax({
                type: "DELETE",
                url: url,
                success: function (data) {
                    if (data.success) {
                        toastr.success(data.message);
                        dataTable.ajax.reload();
                    }
                    else {
                        toastr.error(data.message);
                    }
                }
            });
        }
    });
}

