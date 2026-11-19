
document.addEventListener("DOMContentLoaded", function () {

    
    const searchInput = document.getElementById("keyword");
    const categorySelect = document.getElementById("categoryId");
    const minPrice = document.getElementById("minPrice");
    const maxPrice = document.getElementById("maxPrice");
    const sortSelect = document.getElementById("sortOrder");
    const filterForm = document.getElementById("filterForm");

   
    if (categorySelect) {
        categorySelect.addEventListener("change", function () {
            filterForm.submit();
        });
    }

    if (sortSelect) {
        sortSelect.addEventListener("change", function () {
            filterForm.submit();
        });
    }

    
    if (filterForm) {
        filterForm.addEventListener("submit", function (event) {

            const min = minPrice && minPrice.value !== ""
                ? Number(minPrice.value) : 0;

            const max = maxPrice && maxPrice.value !== ""
                ? Number(maxPrice.value) : Infinity;

            if (min < 0 || max < 0) {
                event.preventDefault();
                alert("Giá sách không được nhỏ hơn 0!");
                return;
            }

            if (min > max) {
                event.preventDefault();
                alert("Giá thấp nhất không được lớn hơn giá cao nhất!");
                return;
            }
        });
    }

    // reset bộ lọc
    const resetButton = document.getElementById("resetFilter");

    if (resetButton) {
        resetButton.addEventListener("click", function () {
            window.location.href = "/Products/Index";
        });
    }

});
