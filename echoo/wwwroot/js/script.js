window.addEventListener("DOMContentLoaded", () => {
    const body = document.body;

    // Fade in effect for promo page
    if (body.classList.contains("fade-start")) {
        requestAnimationFrame(() => {
            body.classList.remove("fade-start");
            body.classList.add("fade-end");
        });
    }

    // Handle transition from index to promo
    const getStartedBtn = document.getElementById("anchor");
    if (getStartedBtn) {
        getStartedBtn.addEventListener("click", () => {
            body.classList.remove("fade-end");
            body.classList.add("fade-start");

            // Delay to let fade-out animation play
            setTimeout(() => {
                window.location.href = "promo.html";
            }, 1000); // matches transition duration
        });
    }

    // Mobile menu functionality
    const menuToggle = document.getElementById('menuToggle');
    const closeMenu = document.getElementById('closeMenu');
    const navRight = document.getElementById('navRight');
    const overlay = document.getElementById('overlay');

    // Open menu
    menuToggle.addEventListener('click', function () {
        navRight.classList.add('active');
        overlay.classList.add('active');
    });

    // Close menu
    closeMenu.addEventListener('click', function () {
        navRight.classList.remove('active');
        overlay.classList.remove('active');
    });

    // Close menu when clicking overlay
    overlay.addEventListener('click', function () {
        navRight.classList.remove('active');
        overlay.classList.remove('active');
    });

    // Close menu when window is resized above mobile breakpoint
    window.addEventListener('resize', function () {
        if (window.innerWidth > 768) {
            navRight.classList.remove('active');
            overlay.classList.remove('active');
        }
    });



});



