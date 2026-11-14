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

    const stickyHeader = document.querySelector(".top");
    if (stickyHeader && !window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
        let previousScrollY = window.scrollY;
        let scrollFramePending = false;
        window.addEventListener("scroll", () => {
            if (scrollFramePending) return;
            scrollFramePending = true;
            window.requestAnimationFrame(() => {
                const currentScrollY = window.scrollY;
                const scrollDelta = currentScrollY - previousScrollY;
                if (currentScrollY < 100 || scrollDelta < -6) stickyHeader.classList.remove("is-hidden");
                else if (scrollDelta > 6) stickyHeader.classList.add("is-hidden");
                previousScrollY = currentScrollY;
                scrollFramePending = false;
            });
        }, { passive: true });
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

    const dossier = document.getElementById("technical-dossier");
    if (dossier && typeof dossier.showModal === "function") {
        const wrist = document.getElementById("dossier-wrist");
        const wristValue = document.getElementById("dossier-wrist-value");
        const fitValue = document.getElementById("dossier-fit-value");
        const fitMeter = document.getElementById("dossier-fit-meter");
        const fitCopy = document.getElementById("dossier-fit-copy");
        const imageFrame = document.getElementById("dossier-caliber-frame");
        const watchImage = document.getElementById("dossier-image");
        const spinButton = document.querySelector("[data-dossier-spin]");
        const lumeToggle = document.querySelector("[data-dossier-lume]");
        const setText = (id, value) => { document.getElementById(id).textContent = value || "—"; };
        let watchRotation = 0;
        let activePointer = null;
        let previousPointerX = 0;

        const setWatchRotation = () => {
            watchImage.style.setProperty("--dossier-rotation", `${watchRotation}deg`);
        };

        watchImage.addEventListener("pointerdown", event => {
            if (event.button !== 0 && event.pointerType === "mouse") return;
            activePointer = event.pointerId;
            previousPointerX = event.clientX;
            watchImage.classList.add("is-dragging");
            watchImage.setPointerCapture(event.pointerId);
        });
        watchImage.addEventListener("pointermove", event => {
            if (event.pointerId !== activePointer) return;
            const delta = event.clientX - previousPointerX;
            previousPointerX = event.clientX;
            watchRotation = (watchRotation + delta / Math.max(1, imageFrame.clientWidth) * 360) % 360;
            setWatchRotation();
        });
        const endWatchDrag = event => {
            if (event.pointerId !== activePointer) return;
            activePointer = null;
            watchImage.classList.remove("is-dragging");
            if (watchImage.hasPointerCapture(event.pointerId)) watchImage.releasePointerCapture(event.pointerId);
        };
        watchImage.addEventListener("pointerup", endWatchDrag);
        watchImage.addEventListener("pointercancel", endWatchDrag);

        spinButton.addEventListener("click", () => {
            imageFrame.classList.remove("is-spinning");
            watchRotation += 360;
            setWatchRotation();
            if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;
            void watchImage.offsetWidth;
            imageFrame.classList.add("is-spinning");
        });
        watchImage.addEventListener("animationend", event => {
            if (event.animationName !== "dossier-turntable") return;
            imageFrame.classList.remove("is-spinning");
        });

        const updateWristEstimate = () => {
            const wristCm = Number(wrist.value);
            const diameter = Number(dossier.dataset.diameter || 0);
            const percentage = Math.max(25, Math.min(95, Math.round(diameter / (wristCm * 10 / Math.PI) * 100)));
            wristValue.value = `${wristCm.toFixed(1).replace(".0", "")} cm`;
            fitValue.style.width = `${percentage}%`;
            fitMeter.setAttribute("aria-valuenow", String(percentage));
            fitCopy.textContent = `Mặt số rộng khoảng ${percentage}% bề ngang cổ tay ước tính.`;
        };
        wrist.addEventListener("input", updateWristEstimate);

        document.addEventListener("click", event => {
            const trigger = event.target.closest("[data-dossier-open]");
            if (trigger) {
                const card = trigger.closest("[data-dossier-source]");
                if (!card) return;
                event.preventDefault();
                dossier.dataset.diameter = card.dataset.watchSize || "0";
                setText("dossier-brand", card.dataset.watchBrand);
                setText("dossier-title", card.dataset.watchName);
                setText("dossier-description", card.dataset.watchDescription);
                setText("dossier-price", card.dataset.watchPrice);
                setText("dossier-reference", `REF. ${card.dataset.watchId || "—"}`);
                setText("dossier-movement", card.dataset.watchMovement);
                setText("dossier-size", `${card.dataset.watchSize || "—"} mm`);
                setText("dossier-material", card.dataset.watchMaterial);
                setText("dossier-warranty", card.dataset.watchWarranty);
                document.getElementById("dossier-image").src = card.dataset.watchImage;
                document.getElementById("dossier-image").alt = card.dataset.watchName || "Đồng hồ";
                watchRotation = 0;
                watchImage.style.setProperty("--dossier-rotation", "0deg");
                imageFrame.classList.remove("is-spinning", "is-dragging");
                document.getElementById("dossier-cart-id").value = card.dataset.watchId || "";
                const addToTray = dossier.querySelector("form button[type='submit']");
                addToTray.disabled = (card.dataset.watchStock || "").includes("hết hàng");
                addToTray.setAttribute("aria-label", `Thêm ${card.dataset.watchName || "mẫu đồng hồ"} vào khay thử`);
                const detailLink = document.getElementById("dossier-detail");
                detailLink.href = card.dataset.watchDetail || detailLink.href;
                const bookingLink = document.getElementById("dossier-booking");
                const bookingUrl = new URL(bookingLink.href, window.location.origin);
                bookingUrl.searchParams.set("maMau", card.dataset.watchId || "");
                bookingLink.href = bookingUrl.toString();
                wrist.value = "17";
                imageFrame.classList.remove("is-luminous");
                lumeToggle.setAttribute("aria-pressed", "false");
                lumeToggle.textContent = "Bật dạ quang Chromalight";
                updateWristEstimate();
                dossier.showModal();
            }

            if (event.target.closest("[data-dossier-close]")) dossier.close();
        });

        lumeToggle.addEventListener("click", () => {
            const active = imageFrame.classList.toggle("is-luminous");
            lumeToggle.setAttribute("aria-pressed", String(active));
            lumeToggle.textContent = active ? "Tắt mô phỏng dạ quang" : "Bật dạ quang Chromalight";
        });
        dossier.addEventListener("click", event => {
            if (event.target === dossier) dossier.close();
        });
        dossier.addEventListener("close", () => imageFrame.classList.remove("is-luminous"));
    }

    const clockDisplays = [document.getElementById("danang-clock"), document.getElementById("header-danang-clock")].filter(Boolean);
    if (clockDisplays.length) {
        const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        const formatter = new Intl.DateTimeFormat("vi-VN", {
            timeZone: "Asia/Ho_Chi_Minh", hour: "2-digit", minute: "2-digit", second: "2-digit", hourCycle: "h23"
        });
        const hourHand = document.getElementById("live-hour-hand");
        const minuteHand = document.getElementById("live-minute-hand");
        const secondHand = document.getElementById("live-second-hand");
        let lastDigitalTime = "";
        const updateClock = () => {
            const now = new Date();
            const parts = Object.fromEntries(formatter.formatToParts(now).map(part => [part.type, part.value]));
            const hour = Number(parts.hour) % 12;
            const minute = Number(parts.minute);
            const second = Number(parts.second) + now.getMilliseconds() / 1000;
            const digitalTime = formatter.format(now);
            if (digitalTime !== lastDigitalTime) {
                clockDisplays.forEach(clock => {
                    clock.textContent = digitalTime;
                    clock.dateTime = now.toISOString();
                });
                lastDigitalTime = digitalTime;
            }
            if (hourHand && minuteHand && secondHand) {
                hourHand.style.transform = `rotate(${(hour + minute / 60 + second / 3600) * 30}deg)`;
                minuteHand.style.transform = `rotate(${(minute + second / 60) * 6}deg)`;
                secondHand.style.transform = `rotate(${second * 6}deg)`;
            }
            if (reducedMotion) window.setTimeout(updateClock, 1000);
            else window.requestAnimationFrame(updateClock);
        };
        if (reducedMotion) updateClock();
        else window.requestAnimationFrame(updateClock);
    }
})();
