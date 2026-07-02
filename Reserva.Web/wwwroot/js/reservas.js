(() => {
    const form = document.querySelector(".reserva-form");

    if (!form) {
        return;
    }

    const fechaInput = form.querySelector(".reserva-fecha");
    const horaSelect = form.querySelector(".reserva-hora");
    const personasInput = form.querySelector(".reserva-personas");
    const availabilityPanel = form.querySelector("[data-availability-panel]");
    const availabilityTitle = form.querySelector("[data-availability-title]");
    const availabilityMessage = form.querySelector("[data-availability-message]");
    const availabilityTable = form.querySelector("[data-availability-table]");
    const availabilityIcon = form.querySelector("[data-availability-icon]");
    const wizard = form.matches("[data-reservation-wizard]");
    const nextStepButton = form.querySelector("[data-next-step]");
    const prevStepButton = form.querySelector("[data-prev-step]");
    const stepOne = form.querySelector('[data-step="1"]');
    const stepTwo = form.querySelector('[data-step="2"]');
    const stepOneIndicator = form.querySelector('[data-step-indicator="1"]');
    const stepTwoIndicator = form.querySelector('[data-step-indicator="2"]');

    if (!fechaInput || !horaSelect) {
        return;
    }

    const reservaId = form.dataset.reservaId;
    const currentTime = horaSelect.dataset.current;
    let availabilityRequest;
    let peopleTimer;
    let hasAvailableSlot = false;

    const setNextStepEnabled = (enabled) => {
        hasAvailableSlot = enabled;

        if (nextStepButton) {
            nextStepButton.disabled = !enabled;
        }
    };

    const setAvailability = (status, title, message, table = "", icon = "•") => {
        if (!availabilityPanel || !availabilityTitle || !availabilityMessage || !availabilityTable) {
            return;
        }

        availabilityPanel.classList.remove(
            "availability-pending",
            "availability-loading",
            "availability-available",
            "availability-unavailable",
            "availability-offline"
        );
        availabilityPanel.classList.add(`availability-${status}`);
        if (availabilityIcon) {
            availabilityIcon.textContent = icon;
        }
        availabilityTitle.textContent = title;
        availabilityMessage.textContent = message;
        availabilityTable.textContent = table;

        setNextStepEnabled(status === "available");
    };

    const showStep = (step) => {
        if (!wizard || !stepOne || !stepTwo) {
            return;
        }

        const showFirst = step === 1;
        stepOne.hidden = !showFirst;
        stepTwo.hidden = showFirst;
        stepOne.classList.toggle("active", showFirst);
        stepTwo.classList.toggle("active", !showFirst);
        stepOneIndicator?.classList.toggle("active", showFirst);
        stepTwoIndicator?.classList.toggle("active", !showFirst);

        if (!showFirst) {
            form.querySelector("[name='NombreCliente']")?.focus();
        }
    };

    const checkAvailability = async () => {
        if (!availabilityPanel) {
            return;
        }

        if (!fechaInput.value || !horaSelect.value || !personasInput?.value) {
            setAvailability(
                "pending",
                "Disponibilidad en tiempo real",
                "Seleccione fecha, hora y personas para consultar si hay mesa disponible.",
                "",
                "•"
            );
            return;
        }

        availabilityRequest?.abort();
        availabilityRequest = new AbortController();

        const params = new URLSearchParams({
            fecha: fechaInput.value,
            hora: horaSelect.value,
            cantidadPersonas: personasInput.value
        });

        if (reservaId) {
            params.append("idReserva", reservaId);
        }

        setAvailability("loading", "Buscando mesas disponibles...", "Estamos revisando mesas activas para ese horario.", "", "…");

        try {
            const response = await fetch(`/Reservas/Disponibilidad?${params.toString()}`, {
                signal: availabilityRequest.signal
            });

            if (!response.ok) {
                setAvailability("offline", "⚠ No pudimos conectar con el servidor.", "Intente nuevamente en unos segundos.", "", "⚠");
                return;
            }

            const result = await response.json();

            if (result.available) {
                setAvailability("available", "✅ Mesa disponible", result.message, result.table ?? "", "✓");
            } else {
                setAvailability("unavailable", "❌ No encontramos disponibilidad.", result.message, "", "×");
                showStep(1);
            }
        } catch (error) {
            if (error.name !== "AbortError") {
                setAvailability("offline", "⚠ No pudimos conectar con el servidor.", "Revise su conexion e intente nuevamente.", "", "⚠");
                showStep(1);
            }
        }
    };

    const refreshTimes = async () => {
        if (!fechaInput.value) {
            return;
        }

        const params = new URLSearchParams({ fecha: fechaInput.value });

        if (reservaId) {
            params.append("idReserva", reservaId);
        }

        if (personasInput?.value) {
            params.append("cantidadPersonas", personasInput.value);
        }

        const response = await fetch(`/Reservas/HorariosDisponibles?${params.toString()}`);

        if (!response.ok) {
            return;
        }

        const times = await response.json();
        const selected = horaSelect.value || currentTime;

        horaSelect.innerHTML = times.length === 0
            ? '<option value="">Sin horarios disponibles</option>'
            : '<option value="">Seleccione un horario</option>';

        for (const item of times) {
            const option = document.createElement("option");
            option.value = item.value;
            option.textContent = item.text;
            option.selected = item.value === selected;
            horaSelect.appendChild(option);
        }

        await checkAvailability();
    };

    fechaInput.addEventListener("change", refreshTimes);
    horaSelect.addEventListener("change", checkAvailability);
    personasInput?.addEventListener("input", () => {
        setNextStepEnabled(false);
        showStep(1);
        window.clearTimeout(peopleTimer);
        peopleTimer = window.setTimeout(refreshTimes, 300);
    });

    nextStepButton?.addEventListener("click", () => {
        if (hasAvailableSlot) {
            showStep(2);
        }
    });

    prevStepButton?.addEventListener("click", () => showStep(1));

    if (wizard && stepTwo?.querySelector(".field-validation-error, .validation-summary-errors")) {
        showStep(2);
    }
})();
