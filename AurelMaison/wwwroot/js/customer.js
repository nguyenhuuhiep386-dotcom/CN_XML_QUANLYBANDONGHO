(() => {
    "use strict";

    const toggle = document.querySelector(".menu-toggle");
    const nav = document.getElementById("customer-nav");
    if (toggle && nav) {
        document.documentElement.classList.add("has-menu-script");
        toggle.addEventListener("click", () => {
            const open = toggle.getAttribute("aria-expanded") !== "true";
            toggle.setAttribute("aria-expanded", String(open));
            toggle.setAttribute("aria-label", open ? "Đóng menu" : "Mở menu");
            nav.classList.toggle("is-open", open);
        });
        document.addEventListener("keydown", event => {
            if (event.key === "Escape" && toggle.getAttribute("aria-expanded") === "true") {
                toggle.setAttribute("aria-expanded", "false");
                toggle.setAttribute("aria-label", "Mở menu");
                nav.classList.remove("is-open");
                toggle.focus();
            }
        });
    }

    const delivery = document.querySelector("[name='HinhThucNhan']");
    if (delivery) {
        const address = document.querySelector("[data-receive='delivery']");
        const showroom = document.querySelector("[data-receive='pickup']");
        const update = () => {
            if (address) address.hidden = delivery.value !== "GiaoTanNoi";
            if (showroom) showroom.hidden = delivery.value !== "NhanTaiShowroom";
        };
        delivery.addEventListener("change", update);
        update();
    }
})();
