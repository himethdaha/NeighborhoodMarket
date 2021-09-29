var dataTable;

//When the documents is ready. Render the function
$(document).ready(function () {
    //Get the URL
    var url = window.location.search;
    if (url.includes("inprocess")) {
        loadDataTable("GetOrderList?status=inprocess");
    }
    else {
        if (url.includes("pending")) {
            loadDataTable("GetOrderList?status=pending");
        }
        else {
            if (url.includes("completed")) {
                loadDataTable("GetOrderList?status=completed");
            }
            else {
                if (url.includes("rejected")) {
                    loadDataTable("GetOrderList?status=rejected");
                }
                else {
                    loadDataTable("GetOrderList?status=all");
                }
            }
           
        }
      
    }
});

function loadDataTable(url) {
    dataTable = $('#DataTable').DataTable({
        "ajax": {
            "url": "/Admin/Order/" + url
        },
        "columns": [
            { "data": "id", "width": "10%" },
            { "data": "applicationUser.name", "width": "15%" },
            { "data": "phone", "width": "15%" },
            { "data": "applicationUser.email", "width": "15%" },
            { "data": "orderStatus", "width": "15%" },
            { "data": "orderTotal", "width": "15%" },
            {
                "data": "id",
                "render": function (data) {
                    return ` <div class="text-center">
                                <a href="/Admin/Order/Details/${data}" class="btn btn-success text-white">
                                    <i class="fas fa-pencil-alt"></i>&nbsp;
                                </a>
                            </div>
                    `;
                }, "width": "5%"
            }
        ]
    });
}


