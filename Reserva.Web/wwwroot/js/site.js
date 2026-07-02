// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.querySelectorAll(".app-toast").forEach((toastElement) => {
    bootstrap.Toast.getOrCreateInstance(toastElement).show();
});

document.querySelector("[data-sidebar-toggle]")?.addEventListener("click", () => {
    document.body.classList.toggle("sidebar-open");
});

const smartCursor = document.querySelector("[data-smart-cursor]");
const canUseSmartCursor = smartCursor && window.matchMedia("(hover: hover) and (pointer: fine)").matches;

if (canUseSmartCursor) {
    const getCursorLabel = (target) => {
        const element = target.closest("a, button, .dish-card, .calendar-slot, .global-result-item, .today-agenda-item");

        if (!element) {
            return null;
        }

        if (element.matches(".dish-card, .dish-card-action")) {
            return "Ver plato";
        }

        const text = (element.textContent ?? "").trim().toLowerCase();
        const href = element.getAttribute("href") ?? "";

        if (text.includes("reserv") || href.includes("Reservas") || element.classList.contains("whatsapp-floating")) {
            return "Reservar";
        }

        if (text.includes("plato") || href.includes("Platos")) {
            return "Ver plato";
        }

        if (text.includes("buscar")) {
            return "Buscar";
        }

        return "Explorar";
    };

    window.addEventListener("pointermove", (event) => {
        const label = getCursorLabel(event.target);
        smartCursor.style.setProperty("--cursor-x", `${event.clientX + 16}px`);
        smartCursor.style.setProperty("--cursor-y", `${event.clientY + 16}px`);

        if (label) {
            smartCursor.textContent = label;
            smartCursor.classList.add("is-visible");
        } else {
            smartCursor.classList.remove("is-visible");
        }
    }, { passive: true });

    window.addEventListener("pointerleave", () => {
        smartCursor.classList.remove("is-visible");
    });
}

const syncNavbarState = () => {
    document.body.classList.toggle("nav-scrolled", window.scrollY > 18);
};

let navbarTicking = false;

syncNavbarState();
window.addEventListener("scroll", () => {
    if (!navbarTicking) {
        window.requestAnimationFrame(() => {
            syncNavbarState();
            navbarTicking = false;
        });
        navbarTicking = true;
    }
}, { passive: true });

const navSpySections = Array.from(document.querySelectorAll("[data-nav-target]"));
const publicNavLinks = Array.from(document.querySelectorAll(".navbar .nav-link[class*='nav-section-']"));

if (navSpySections.length > 0 && publicNavLinks.length > 0 && "IntersectionObserver" in window) {
    const visibleSections = new Map();

    const setActiveNavSection = (sectionName) => {
        publicNavLinks.forEach((link) => {
            const isActive = link.classList.contains(`nav-section-${sectionName}`);
            link.classList.toggle("active", isActive);

            if (isActive) {
                link.setAttribute("aria-current", "page");
            } else {
                link.removeAttribute("aria-current");
            }
        });
    };

    const navSpyObserver = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
            const sectionName = entry.target.getAttribute("data-nav-target");

            if (!sectionName) {
                return;
            }

            if (entry.isIntersecting) {
                visibleSections.set(sectionName, entry.intersectionRatio);
            } else {
                visibleSections.delete(sectionName);
            }
        });

        const activeSection = Array.from(visibleSections.entries())
            .sort((a, b) => b[1] - a[1])[0]?.[0];

        if (activeSection) {
            setActiveNavSection(activeSection);
        }
    }, {
        rootMargin: "-22% 0px -52% 0px",
        threshold: [0.12, 0.24, 0.36, 0.48, 0.6]
    });

    navSpySections.forEach((section) => navSpyObserver.observe(section));
}

document.querySelectorAll(".js-auto-filter").forEach((form) => {
    let timer;

    form.querySelectorAll(".js-filter-input").forEach((input) => {
        const eventName = input.tagName === "SELECT" || input.type === "date" ? "change" : "input";

        input.addEventListener(eventName, () => {
            window.clearTimeout(timer);
            timer = window.setTimeout(() => form.requestSubmit(), eventName === "input" ? 450 : 0);
        });
    });
});

const deleteModal = document.getElementById("confirmDeleteModal");

if (deleteModal) {
    deleteModal.addEventListener("show.bs.modal", (event) => {
        const button = event.relatedTarget;
        const form = deleteModal.querySelector("[data-delete-form]");
        const message = deleteModal.querySelector("[data-delete-message]");
        const idInput = deleteModal.querySelector("[data-delete-id]");

        form.action = button?.getAttribute("data-delete-url") ?? "";
        if (idInput) {
            idInput.value = button?.getAttribute("data-delete-id") ?? "";
        }
        message.textContent = button?.getAttribute("data-delete-text") ?? "Registro seleccionado";
    });

    document.addEventListener("click", (event) => {
        const button = event.target.closest(".js-confirm-delete");

        if (!button) {
            return;
        }

        event.preventDefault();
        const dropdownToggle = button.closest(".dropdown")?.querySelector("[data-bs-toggle='dropdown']");
        window.bootstrap?.Dropdown.getInstance(dropdownToggle)?.hide();
        window.bootstrap?.Modal.getOrCreateInstance(deleteModal).show(button);
    });
}

const reservationStatusModal = document.getElementById("reservationStatusModal");

if (reservationStatusModal) {
    reservationStatusModal.addEventListener("show.bs.modal", (event) => {
        const button = event.relatedTarget;
        const action = button?.getAttribute("data-reservation-action") ?? "confirmar";
        const isCancel = action === "cancelar";
        const title = reservationStatusModal.querySelector("[data-reservation-modal-title]");
        const note = reservationStatusModal.querySelector("[data-reservation-modal-note]");
        const submit = reservationStatusModal.querySelector("[data-reservation-modal-submit]");

        reservationStatusModal.querySelector("[data-reservation-modal-id]").value =
            button?.getAttribute("data-reservation-id") ?? "";
        reservationStatusModal.querySelector("[data-reservation-modal-state]").value =
            button?.getAttribute("data-reservation-state") ?? "";
        reservationStatusModal.querySelector("[data-reservation-modal-client]").textContent =
            button?.getAttribute("data-reservation-client") ?? "Cliente";
        reservationStatusModal.querySelector("[data-reservation-modal-date]").textContent =
            button?.getAttribute("data-reservation-date") ?? "";
        reservationStatusModal.querySelector("[data-reservation-modal-time]").textContent =
            button?.getAttribute("data-reservation-time") ?? "";
        reservationStatusModal.querySelector("[data-reservation-modal-table]").textContent =
            button?.getAttribute("data-reservation-table") ?? "Mesa asignada";

        if (title) {
            title.textContent = isCancel ? "Cancelar reserva" : "Confirmar reserva";
        }

        if (note) {
            note.textContent = isCancel
                ? "Esta accion marcara la solicitud como cancelada y notificara al cliente si el correo esta configurado."
                : "Esta accion marcara la solicitud como confirmada y notificara al cliente si el correo esta configurado.";
        }

        if (submit) {
            submit.textContent = isCancel ? "Cancelar reserva" : "Confirmar reserva";
            submit.classList.toggle("btn-accent", !isCancel);
            submit.classList.toggle("btn-danger", isCancel);
        }
    });
}

document.querySelectorAll("[data-copy-text]").forEach((button) => {
    button.addEventListener("click", async () => {
        const text = button.getAttribute("data-copy-text") ?? "";

        try {
            await navigator.clipboard.writeText(text);
            button.textContent = "Codigo copiado";
        } catch {
            button.textContent = text;
        }

        window.setTimeout(() => {
            button.textContent = "Copiar codigo";
        }, 2200);
    });
});

const lastReservationElement = document.querySelector("[data-last-reservation]");

if (lastReservationElement) {
    const reservation = {
        code: lastReservationElement.getAttribute("data-code"),
        email: lastReservationElement.getAttribute("data-email"),
        date: lastReservationElement.getAttribute("data-date"),
        time: lastReservationElement.getAttribute("data-time"),
        status: lastReservationElement.getAttribute("data-status")
    };

    localStorage.setItem("mikuy:lastReservation", JSON.stringify(reservation));
}

const lookupForm = document.querySelector(".lookup-form");

if (lookupForm) {
    const methodInputs = lookupForm.querySelectorAll("input[name='MetodoConsulta']");
    const codeSection = lookupForm.querySelector("[data-lookup-section='codigo']");
    const contactSection = lookupForm.querySelector("[data-lookup-section='contacto']");
    const codeInput = lookupForm.querySelector("[name='CodigoReserva']");
    const contactInput = lookupForm.querySelector("[name='Contacto']");
    const lastReservationCard = document.querySelector("[data-last-reservation-card]");
    const lookupResults = Array.from(document.querySelectorAll("[data-lookup-result]"));

    const syncLookupSections = () => {
        const method = lookupForm.querySelector("input[name='MetodoConsulta']:checked")?.value ?? "codigo";
        const byCode = method === "codigo";

        if (codeSection) {
            codeSection.hidden = !byCode;
        }

        if (contactSection) {
            contactSection.hidden = byCode;
        }
    };

    methodInputs.forEach((input) => input.addEventListener("change", syncLookupSections));
    syncLookupSections();

    try {
        const reservation = JSON.parse(localStorage.getItem("mikuy:lastReservation") ?? "null");

        if (reservation?.code && lastReservationCard) {
            const liveResult = lookupResults.find((result) =>
                result.getAttribute("data-code")?.toLowerCase() === reservation.code.toLowerCase()
            );

            if (liveResult) {
                reservation.date = liveResult.getAttribute("data-date") ?? reservation.date;
                reservation.time = liveResult.getAttribute("data-time") ?? reservation.time;
                reservation.status = liveResult.getAttribute("data-status") ?? reservation.status;
                localStorage.setItem("mikuy:lastReservation", JSON.stringify(reservation));
            }

            lastReservationCard.hidden = false;
            lastReservationCard.querySelector("[data-last-reservation-code]").textContent = reservation.code;
            lastReservationCard.querySelector("[data-last-reservation-meta]").textContent =
                `${reservation.date ?? ""} ${reservation.time ?? ""} - ${reservation.status ?? "Pendiente"}`.trim();

            lastReservationCard.querySelector("[data-use-last-reservation]")?.addEventListener("click", () => {
                const codeMethod = lookupForm.querySelector("#lookupByCode");

                if (codeMethod) {
                    codeMethod.checked = true;
                }

                if (codeInput) {
                    codeInput.value = reservation.code;
                }

                if (contactInput && reservation.email) {
                    contactInput.value = reservation.email;
                }

                syncLookupSections();
                lookupForm.requestSubmit();
            });
        }
    } catch {
        localStorage.removeItem("mikuy:lastReservation");
    }
}

document.querySelectorAll("[data-restaurant-status]").forEach((statusElement) => {
    const title = statusElement.querySelector("[data-restaurant-status-title]");
    const detail = statusElement.querySelector("[data-restaurant-status-detail]");
    const openHour = Number(statusElement.getAttribute("data-open-hour") ?? "12");
    const closeHour = Number(statusElement.getAttribute("data-close-hour") ?? "22");
    const now = new Date();
    const currentHour = now.getHours() + now.getMinutes() / 60;
    const isOpen = currentHour >= openHour && currentHour < closeHour;

    statusElement.classList.toggle("status-open", isOpen);
    statusElement.classList.toggle("status-closed", !isOpen);

    if (title) {
        title.textContent = isOpen ? "Abierto ahora" : "Cerrado";
    }

    if (detail) {
        if (isOpen) {
            detail.textContent = "Hasta las 10:00 PM";
        } else if (currentHour < openHour) {
            detail.textContent = "Abrimos hoy a las 12:00 PM";
        } else {
            detail.textContent = "Abrimos manana a las 12:00 PM";
        }
    }
});

const heroParallax = document.querySelector("[data-hero-parallax]");
const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

if (!reduceMotion && window.matchMedia("(pointer: fine)").matches) {
    let targetScroll = window.scrollY;
    let currentScroll = window.scrollY;
    let smoothScrollFrame = null;
    const interactiveSelector = "input, textarea, select, option, button, a, [role='button'], .modal, .dropdown-menu";

    const clampScroll = (value) => {
        const maxScroll = Math.max(0, document.documentElement.scrollHeight - window.innerHeight);
        return Math.max(0, Math.min(value, maxScroll));
    };

    const animateSmoothScroll = () => {
        currentScroll += (targetScroll - currentScroll) * 0.14;
        window.scrollTo(0, currentScroll);

        if (Math.abs(targetScroll - currentScroll) > 0.4) {
            smoothScrollFrame = window.requestAnimationFrame(animateSmoothScroll);
        } else {
            currentScroll = targetScroll;
            window.scrollTo(0, targetScroll);
            smoothScrollFrame = null;
        }
    };

    window.addEventListener("wheel", (event) => {
        if (event.ctrlKey || event.target.closest(interactiveSelector)) {
            return;
        }

        event.preventDefault();
        targetScroll = clampScroll(targetScroll + event.deltaY);

        if (!smoothScrollFrame) {
            currentScroll = window.scrollY;
            smoothScrollFrame = window.requestAnimationFrame(animateSmoothScroll);
        }
    }, { passive: false });

    window.addEventListener("resize", () => {
        targetScroll = clampScroll(window.scrollY);
        currentScroll = targetScroll;
    });

    window.addEventListener("scroll", () => {
        if (!smoothScrollFrame) {
            targetScroll = window.scrollY;
            currentScroll = targetScroll;
        }
    }, { passive: true });
}

if (heroParallax && !reduceMotion) {
    let ticking = false;
    let pointerX = 68;
    let pointerY = 42;
    let driftX = 0;

    const updateHeroParallax = () => {
        const rect = heroParallax.getBoundingClientRect();
        const viewportHeight = window.innerHeight || document.documentElement.clientHeight;

        if (rect.bottom >= 0 && rect.top <= viewportHeight) {
            const offset = Math.max(-70, Math.min(70, -rect.top * 0.3));
            heroParallax.style.setProperty("--hero-parallax-y", `${offset.toFixed(2)}px`);
            heroParallax.style.setProperty("--hero-pointer-x", `${pointerX.toFixed(1)}%`);
            heroParallax.style.setProperty("--hero-pointer-y", `${pointerY.toFixed(1)}%`);
            heroParallax.style.setProperty("--hero-drift-x", `${driftX.toFixed(2)}px`);
        }

        ticking = false;
    };

    const requestHeroParallax = () => {
        if (!ticking) {
            window.requestAnimationFrame(updateHeroParallax);
            ticking = true;
        }
    };

    updateHeroParallax();
    window.addEventListener("scroll", requestHeroParallax, { passive: true });
    window.addEventListener("resize", requestHeroParallax);

    if (window.matchMedia("(pointer: fine)").matches) {
        heroParallax.addEventListener("pointermove", (event) => {
            const rect = heroParallax.getBoundingClientRect();

            pointerX = ((event.clientX - rect.left) / rect.width) * 100;
            pointerY = ((event.clientY - rect.top) / rect.height) * 100;
            driftX = (pointerX - 50) * -0.18;
            requestHeroParallax();
        }, { passive: true });

        heroParallax.addEventListener("pointerleave", () => {
            pointerX = 68;
            pointerY = 42;
            driftX = 0;
            requestHeroParallax();
        }, { passive: true });
    }
}

const heroSlider = document.querySelector("[data-hero-slider]");

if (heroSlider && !reduceMotion) {
    const slides = Array.from(heroSlider.querySelectorAll(".hero-slide"));
    let activeSlide = slides.findIndex((slide) => slide.classList.contains("is-active"));

    if (slides.length > 1) {
        activeSlide = activeSlide < 0 ? 0 : activeSlide;

        window.setInterval(() => {
            slides[activeSlide].classList.remove("is-active");
            activeSlide = (activeSlide + 1) % slides.length;
            slides[activeSlide].classList.add("is-active");
        }, 6000);
    }
}

const canUseCardTilt = !reduceMotion && window.matchMedia("(hover: hover) and (pointer: fine)").matches;

if (canUseCardTilt) {
    document.querySelectorAll(".dish-card").forEach((card) => {
        card.addEventListener("pointermove", (event) => {
            const rect = card.getBoundingClientRect();
            const x = (event.clientX - rect.left) / rect.width;
            const y = (event.clientY - rect.top) / rect.height;
            const rotateX = (0.5 - y) * 5;
            const rotateY = (x - 0.5) * 6;

            card.style.setProperty("--tilt-x", `${rotateX.toFixed(2)}deg`);
            card.style.setProperty("--tilt-y", `${rotateY.toFixed(2)}deg`);
            card.style.setProperty("--glare-x", `${(x * 100).toFixed(2)}%`);
            card.style.setProperty("--glare-y", `${(y * 100).toFixed(2)}%`);
        });

        card.addEventListener("pointerleave", () => {
            card.style.setProperty("--tilt-x", "0deg");
            card.style.setProperty("--tilt-y", "0deg");
            card.style.setProperty("--glare-x", "50%");
            card.style.setProperty("--glare-y", "35%");
        });
    });
}

document.querySelectorAll("[data-scroll-story]").forEach((story) => {
    if (reduceMotion || !("IntersectionObserver" in window)) {
        story.classList.add("story-visible");
        return;
    }

    const observer = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
            if (entry.isIntersecting) {
                story.classList.add("story-visible");
                observer.unobserve(story);
            }
        });
    }, {
        rootMargin: "0px 0px -18% 0px",
        threshold: 0.22
    });

    observer.observe(story);
});

const revealSections = Array.from(document.querySelectorAll(
    "#main-content > section, .admin-dashboard > section, .admin-grid > section"
));

const staggerSelector = [
    ".dish-card",
    ".metric-card",
    ".reviews-grid article",
    ".experience-strip article",
    ".ops-strip article",
    ".operational-signal",
    ".pending-reservation-card",
    ".today-agenda-item",
    ".global-result-item",
    ".lookup-result-card",
    ".calendar-day",
    ".culture-stories article"
].join(",");

revealSections.forEach((section) => {
    section.classList.add("scroll-reveal");

    const items = Array.from(section.querySelectorAll(staggerSelector));
    items.forEach((item, index) => {
        item.classList.add("scroll-stagger-item");
        item.style.setProperty("--scroll-stagger-delay", `${index * 100}ms`);
    });
});

const kpiCounters = Array.from(document.querySelectorAll(".metric-card strong"))
    .map((counter) => {
        const originalText = counter.textContent.trim();
        const match = originalText.match(/^(\d+(?:[.,]\d+)?)(.*)$/);

        if (!match) {
            return null;
        }

        const value = Number(match[1].replace(",", "."));

        if (!Number.isFinite(value)) {
            return null;
        }

        counter.textContent = `0${match[2] ?? ""}`;

        return {
            counter,
            value,
            suffix: match[2] ?? "",
            decimals: match[1].includes(".") || match[1].includes(",") ? 1 : 0
        };
    })
    .filter(Boolean);

const animateCounter = ({ counter, value, suffix, decimals }) => {
    const duration = 1100;
    const startTime = performance.now();

    const tick = (now) => {
        const progress = Math.min((now - startTime) / duration, 1);
        const eased = 1 - Math.pow(1 - progress, 3);
        const currentValue = value * eased;
        counter.textContent = `${currentValue.toFixed(decimals)}${suffix}`;

        if (progress < 1) {
            window.requestAnimationFrame(tick);
        } else {
            counter.textContent = `${value.toFixed(decimals)}${suffix}`;
        }
    };

    window.requestAnimationFrame(tick);
};

if (reduceMotion || !("IntersectionObserver" in window)) {
    revealSections.forEach((section) => section.classList.add("is-visible"));
    document.querySelectorAll(".scroll-stagger-item").forEach((item) => item.classList.add("is-visible"));
    kpiCounters.forEach(({ counter, value, suffix, decimals }) => {
        counter.textContent = `${value.toFixed(decimals)}${suffix}`;
    });
} else {
    const revealObserver = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
            if (!entry.isIntersecting) {
                return;
            }

            const section = entry.target;
            section.classList.add("is-visible");
            section.querySelectorAll(".scroll-stagger-item").forEach((item) => item.classList.add("is-visible"));
            revealObserver.unobserve(section);
        });
    }, {
        rootMargin: "0px 0px -12% 0px",
        threshold: 0.14
    });

    revealSections.forEach((section) => revealObserver.observe(section));

    const counterObserver = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
            if (!entry.isIntersecting) {
                return;
            }

            const data = kpiCounters.find((item) => item.counter === entry.target);

            if (data) {
                animateCounter(data);
            }

            counterObserver.unobserve(entry.target);
        });
    }, {
        rootMargin: "0px 0px -10% 0px",
        threshold: 0.35
    });

    kpiCounters.forEach(({ counter }) => counterObserver.observe(counter));
}
