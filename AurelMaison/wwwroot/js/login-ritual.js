(() => {
    "use strict";

    const ritual = document.getElementById("login-stage");
    const panel = document.getElementById("login-form-panel");
    const cord = document.getElementById("ritual-cord");
    const status = document.getElementById("ritual-status");
    if (!ritual || !panel || !cord) return;

    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const instruction = document.getElementById("ritual-instruction");
    let pointerId = null;
    let startY = 0;
    let pull = 0;
    let open = ritual.dataset.skip === "true";
    let switching = false;

    const updateClock = () => {
        const daNang = new Date(Date.now() + 7 * 60 * 60 * 1000);
        const hours = daNang.getUTCHours() % 12;
        const minutes = daNang.getUTCMinutes();
        const seconds = daNang.getUTCSeconds() + daNang.getUTCMilliseconds() / 1000;
        ritual.style.setProperty("--hour-angle", `${(hours + minutes / 60 + seconds / 3600) * 30}deg`);
        ritual.style.setProperty("--minute-angle", `${(minutes + seconds / 60) * 6}deg`);
        ritual.style.setProperty("--second-angle", `${seconds * 6}deg`);
    };
    updateClock();
    window.setInterval(updateClock, reducedMotion ? 1000 : 100);

    ritual.classList.add("is-ready");
    ritual.classList.toggle("is-open", open);
    panel.inert = !open;
    if (open) instruction.textContent = "KÉO LẦN NỮA ĐỂ QUAY LẠI";
    else panel.setAttribute("aria-hidden", "true");

    const setPull = distance => {
        pull = Math.max(0, Math.min(110, distance));
        cord.style.setProperty("--cord-pull", `${pull}px`);
        ritual.style.setProperty("--wind-angle", `${pull * 2}deg`);
        ritual.style.setProperty("--light-strength", String(Math.min(.8, pull / 110)));
    };

    const toggle = () => {
        if (switching) return;
        switching = true;
        const nextOpen = !open;
        ritual.classList.add("is-switching");
        ritual.classList.remove("is-pulling");
        if (!nextOpen) cord.focus();
        setPull(0);
        window.setTimeout(() => {
            open = nextOpen;
            ritual.classList.toggle("is-open", open);
            panel.inert = !open;
            if (open) panel.removeAttribute("aria-hidden");
            else panel.setAttribute("aria-hidden", "true");
            instruction.textContent = open ? "KÉO LẦN NỮA ĐỂ QUAY LẠI" : "KÉO DÂY ĐỂ LÊN CÓT";
            status.textContent = open ? "Đã mở khung đăng nhập." : "Đã trở về màn hình đồng hồ.";
        }, reducedMotion ? 0 : 180);
        window.setTimeout(() => {
            ritual.classList.remove("is-switching");
            switching = false;
            if (open) panel.querySelector("input[name='Email']")?.focus();
        }, reducedMotion ? 80 : 900);
    };

    cord.addEventListener("pointerdown", event => {
        if (switching || (event.pointerType === "mouse" && event.button !== 0)) return;
        pointerId = event.pointerId;
        startY = event.clientY;
        cord.setPointerCapture(event.pointerId);
        ritual.classList.add("is-pulling");
    });
    cord.addEventListener("pointermove", event => {
        if (event.pointerId !== pointerId || switching) return;
        setPull((event.clientY - startY) * .65);
    });
    const release = (event, cancelled = false) => {
        if (event.pointerId !== pointerId) return;
        pointerId = null;
        if (cord.hasPointerCapture(event.pointerId)) cord.releasePointerCapture(event.pointerId);
        ritual.classList.remove("is-pulling");
        if (!cancelled && pull >= 70) toggle();
        else setPull(0);
    };
    cord.addEventListener("pointerup", release);
    cord.addEventListener("pointercancel", event => release(event, true));
    cord.addEventListener("click", event => {
        if (event.detail === 0 && !switching) toggle();
    });
})();
