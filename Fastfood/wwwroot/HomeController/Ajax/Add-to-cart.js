document.querySelectorAll('.add-to-cart-btn').forEach(button => {
    button.addEventListener('click', function () {
        const itemId = this.getAttribute('data-id');
        const btn = this;
        const btnText = btn.querySelector('span');

        // UI: start loading
        btn.disabled = true;
        btnText.textContent = 'Adding...';
        btn.classList.remove('btn-outline-primary');
        btn.classList.add('btn-warning');

        fetch('/Home/AddToCart', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(itemId)
        })
            .then(async response => {
                const contentType = response.headers.get("content-type");
                if (!contentType || !contentType.includes("application/json")) {
                    throw new Error("Server returned non-JSON response");
                }

                const data = await response.json();

                if (data.success) {
                    console.log("✅ " + data.message);
                    document.getElementById("cartCount").innerText = data.cartCount;

                    // Success feedback
                    btnText.textContent = 'Added!';
                    btn.classList.remove('btn-warning');
                    btn.classList.add('btn-success');

                    // Revert after 2s
                    setTimeout(() => {
                        btnText.textContent = 'Add to Cart';
                        btn.classList.remove('btn-success');
                        btn.classList.add('btn-outline-primary');
                        btn.disabled = false;
                    }, 1500);
                } else {
                    throw new Error(data.message || "Add failed");
                }
            })
            .catch(err => {
                console.error("❌ Error:", err.message);
                btnText.textContent = 'Error';
                btn.classList.remove('btn-warning');
                btn.classList.add('btn-danger');

                setTimeout(() => {
                    btnText.textContent = 'Add to Cart';
                    btn.classList.remove('btn-danger');
                    btn.classList.add('btn-outline-primary');
                    btn.disabled = false;
                }, 1500);
            });
    });
});