
(function () {
    var form = document.getElementById("checkoutForm");

    if (!form) return;

    form.addEventListener("submit", function (event) {
        var shipping = form.querySelector(
            'input[name="ShippingPartner"]:checked'
        );

        var payment = form.querySelector(
            'input[name="PaymentMethod"]:checked'
        );

        if (!shipping) {
            event.preventDefault();
            alert("Vui lòng chọn đơn vị vận chuyển.");
            return;
        }

        if (!payment) {
            event.preventDefault();
            alert("Vui lòng chọn phương thức thanh toán.");
            return;
        }

        if (!confirm("Bạn xác nhận đặt đơn hàng này?")) {
            event.preventDefault();
        }
    });
})();
