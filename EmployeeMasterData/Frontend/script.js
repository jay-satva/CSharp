let employees = [];
let currentPage = 1;
let rowsPerPage = 10;

const departments = {
    0: { name: "Sales", class: "text-danger" },
    1: { name: "Marketing", class: "text-success" },
    2: { name: "Development", class: "text-dark" },
    3: { name: "QA", class: "text-primary" },
    4: { name: "HR", class: "text-warning" },
    5: { name: "SEO", class: "text-pink" }
};

$(document).ready(function () {
    loadEmployees();

    $("#searchInput").on("keyup", function () {
        currentPage = 1;
        renderTable();
        renderPagination();
    });
});

function loadEmployees() {
    $.ajax({
        url: "../Data/EmployeeData_13March.json",
        method: "GET",
        dataType: "json",
        success: function (data) {
            employees = data;
            renderTable();
            renderPagination();
        }
    });
}

function renderTable() {
    let tbody = $("#employeeTable tbody");
    tbody.empty();

    let search = $("#searchInput").val().toLowerCase();

    let filtered = employees.filter(e => e.Name.toLowerCase().includes(search));

    let start = (currentPage - 1) * rowsPerPage;

    let pageData = filtered.slice(start, start + rowsPerPage);

    pageData.forEach((emp, index) => {
        let dept = departments[emp.Department] || { name: "Unknown", class: "bg-secondary" };

        let row = `
            <tr>
                <td>${start + index + 1}</td>
                <td>${emp.Name}</td>
                <td>
                    <span class="badge ${dept.class}">${dept.name}</span>
                </td>
                <td>
                    <a href="mailto:${emp.Email}" class="text-decoration-none">${emp.Email}</a>
                </td>
                <td>
                    <a href="tel:${emp.PhoneNumber}" class="text-decoration-none">${emp.PhoneNumber}</a>
                </td>
                <td>${emp.Gender}</td>
                <td>
                    <i class="bi bi-eye text-primary"
                       role="button"
                       data-id="${emp.Id}"
                       onclick="showDetails('${emp.Id}')">
                    </i>
                </td>
            </tr>
        `;

        tbody.append(row);
    });
}

function renderPagination() {
    let totalPages = Math.ceil(employees.length / rowsPerPage);
    let pagination = $("#pagination");
    pagination.empty();

    for (let i = 1; i <= totalPages; i++) {
        pagination.append(`
            <li class="page-item ${i === currentPage ? 'active' : ''}">
                <a class="page-link" role="button" onclick="goToPage(${i})">${i}</a>
            </li>
        `);
    }
}

function goToPage(page) {
    currentPage = page;
    renderTable();
    renderPagination();
}

function showDetails(id) {
    let emp = employees.find(e => e.Id === id);

    let html = `
        <div class="row g-3">
            <div class="col-md-6"><strong>Name:</strong> ${emp.Name}</div>
            <div class="col-md-6"><strong>Email:</strong> ${emp.Email}</div>
            <div class="col-md-6"><strong>Phone:</strong> ${emp.PhoneNumber}</div>
            <div class="col-md-6"><strong>Gender:</strong> ${emp.Gender}</div>
            <div class="col-md-6"><strong>DOB:</strong> ${emp.DOB}</div>
            <div class="col-md-6"><strong>Joining Date:</strong> ${emp.DatOfJoining}</div>
            <div class="col-md-6"><strong>City:</strong> ${emp.City}</div>
            <div class="col-md-6"><strong>State:</strong> ${emp.State}</div>
            <div class="col-md-6"><strong>Designation:</strong> ${emp.Designation}</div>
            <div class="col-md-6"><strong>Salary:</strong> ${emp.Salary}</div>
            <div class="col-md-6"><strong>Total Experience:</strong> ${emp.TotExperience} years</div>
            <div class="col-md-12"><strong>Remarks:</strong> ${emp.Remarks}</div>
        </div>
    `;

    $("#modalBody").html(html);

    let modal = new bootstrap.Modal(document.getElementById("employeeModal"));
    modal.show();
}