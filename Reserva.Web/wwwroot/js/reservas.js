(() => {
    const form = document.querySelector(".reserva-form");

    if (!form) {
        return;
    }

    const fechaInput = form.querySelector(".reserva-fecha");
    const horaSelect = form.querySelector(".reserva-hora");

    if (!fechaInput || !horaSelect) {
        return;
    }

    const reservaId = form.dataset.reservaId;
    const currentTime = horaSelect.dataset.current;

    const refreshTimes = async () => {
        if (!fechaInput.value) {
            return;
        }

        const params = new URLSearchParams({ fecha: fechaInput.value });

        if (reservaId) {
            params.append("idReserva", reservaId);
        }

        const response = await fetch(`/Reservas/HorariosDisponibles?${params.toString()}`);

        if (!response.ok) {
            return;
        }

        const times = await response.json();
        const selected = horaSelect.value || currentTime;

        horaSelect.innerHTML = '<option value="">Seleccione un horario</option>';

        for (const item of times) {
            const option = document.createElement("option");
            option.value = item.value;
            option.textContent = item.text;
            option.selected = item.value === selected;
            horaSelect.appendChild(option);
        }
    };

    fechaInput.addEventListener("change", refreshTimes);
})();
