// Toggle de mostrar/ocultar contraseña, con JS puro y delegación de
// eventos en document — no depende del circuito de Blazor Server, así que
// funciona también en páginas que la primera carga renderiza en modo
// estático (ej. Login, ver App.razor: PageRenderMode es InteractiveServer
// solo si HttpContext.AcceptsInteractiveRouting() ya es true).
document.addEventListener("click", (event) => {
    const boton = event.target.closest(".btn-toggle-password");
    if (!boton) {
        return;
    }

    const input = boton.closest(".input-group-password")?.querySelector("input");
    const icono = boton.querySelector(".bi");
    if (!input || !icono) {
        return;
    }

    const mostrando = input.type === "text";
    input.type = mostrando ? "password" : "text";
    icono.classList.toggle("bi-eye-toggle", mostrando);
    icono.classList.toggle("bi-eye-slash-toggle", !mostrando);
});
