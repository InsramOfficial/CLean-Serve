function showToast(message, type, fontSize = "16px") {
    const toast = document.createElement("div");
    toast.className = `custom-toast ${type}`;
    toast.textContent = message;

    // Styling for right-side toast
    toast.style.position = "fixed";
    toast.style.top = "80px";
    toast.style.right = "-300px"; // start off-screen
    toast.style.backgroundColor = getToastColor(type);
    toast.style.color = "#fff";
    toast.style.padding = "12px 24px";
    toast.style.borderRadius = "30px";
    toast.style.zIndex = 1055;
    toast.style.boxShadow = "0 4px 16px rgba(0,0,0,0.2)";
    toast.style.fontSize = fontSize;
    toast.style.opacity = "0";
    toast.style.transition = "opacity 0.5s ease, right 0.5s ease";

    document.body.appendChild(toast);

    // Animate in
    setTimeout(() => {
        toast.style.right = "20px";
        toast.style.opacity = "1";
    }, 100);

    // Animate out
    setTimeout(() => {
        toast.style.right = "-300px";
        toast.style.opacity = "0";
        setTimeout(() => {
            toast.remove();
        }, 500);
    }, 4000);
}

function getToastColor(type) {
    switch (type) {
        case "success": return "#198754";
        case "error": return "#dc3545";
        case "warning": return "#ffc107";
        case "info": return "#0dcaf0";
        default: return "#6c757d";
    }
}