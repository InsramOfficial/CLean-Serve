function showToast(message, type) {
    const toast = document.createElement("div");
    toast.className = `custom-toast ${type}`;
    toast.textContent = message;

    // Styling for top-center position
    toast.style.position = "fixed";
    toast.style.top = "20px";
    toast.style.left = "50%";
    toast.style.transform = "translateX(-50%) translateY(-20px)";
    toast.style.backgroundColor = getToastColor(type);
    toast.style.color = "#fff";
    toast.style.padding = "12px 24px";
    toast.style.borderRadius = "30px";
    toast.style.zIndex = 1055;
    toast.style.boxShadow = "0 4px 16px rgba(0,0,0,0.2)";
    toast.style.fontSize = "16px";
    toast.style.opacity = "0";
    toast.style.transition = "opacity 0.5s ease, transform 0.5s ease";

    document.body.appendChild(toast);

    // Animate in
    setTimeout(() => {
        toast.style.opacity = "1";
        toast.style.transform = "translateX(-50%) translateY(0)";
    }, 100);

    // Animate out
    setTimeout(() => {
        toast.style.opacity = "0";
        toast.style.transform = "translateX(-50%) translateY(-20px)";
        setTimeout(() => {
            toast.remove();
        }, 500);
    }, 4000);
}

function getToastColor(type) {
    switch (type) {
        case "success": return "#198754"; // green
        case "error": return "#dc3545"; // red
        case "warning": return "#ffc107"; // yellow
        case "info": return "#0dcaf0"; // blue
        default: return "#6c757d"; // gray
    }
}