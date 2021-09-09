var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#DataTable').DataTable({
        "ajax": {
            "url": "/Admin/ApplicationUser/GetAll"
        },
        "columns": [
            { "data": "name", "width": "15%" },
            { "data": "streetAddress1", "width": "25%" },
            { "data": "streetAddress2", "width": "10%" },
            { "data": "state", "width": "5%" },
            { "data": "city", "width": "10%" },
            { "data": "postalCode", "width": "10%" },
            { "data": "phoneNumber", "width": "10%" },
            { "data": "role", "width": "10%" },
            {
                "data": {
                    id: "id", lockoutEnd: "lockoutEnd"
                },
                "render": function (data) {
                    var today = new Date().getTime();
                    var lockout = new Date(data.lockoutEnd).getTime();
                    //User locked
                    if (lockout > today) {
                        return ` <div class="text-center">
                               <a onclick=LockUnlock('${data.id}') class="btn btn-danger text-white" style="width=100px;">
                                    <i class="fas fa-lock-open"></i>Unlock
                                </a>
                            </div>
                    `;
                    }
                    else {
                        return ` <div class="text-center">
                               <a onclick=LockUnlock('${data.id}') class="btn btn-success text-white" style="width=100px;">
                                    <i class="fas fa-lock"></i>Lock
                                </a>
                            </div>
                    `;
                    }
                   
                }, "width": "25%"
            }
           
        ]
    });
}
function LockUnlock(id) {

            $.ajax({
                type: "POST",
                url: "/Admin/ApplicationUser/LockUnlock",
                data: JSON.stringify(id),
                contentType: "application/json",
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
