// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.querySelectorAll(".app-toast").forEach((toastElement) => {
    bootstrap.Toast.getOrCreateInstance(toastElement).show();
});

document.querySelector("[data-sidebar-toggle]")?.addEventListener("click", () => {
    document.body.classList.toggle("sidebar-open");
});

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

        form.action = button?.getAttribute("data-delete-url") ?? "";
        message.textContent = button?.getAttribute("data-delete-text") ?? "Registro seleccionado";
    });
}
