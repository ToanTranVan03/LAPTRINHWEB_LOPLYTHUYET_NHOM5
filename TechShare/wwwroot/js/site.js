// ===================================
// TechShare - Interactive JavaScript
// ===================================

document.addEventListener('DOMContentLoaded', function () {

    // 1. Navbar scroll effect
    const navbar = document.querySelector('.navbar-glass');
    if (navbar) {
        window.addEventListener('scroll', () => {
            navbar.classList.toggle('scrolled', window.scrollY > 50);
        });
    }

    // 2. Back to Top button
    const backToTop = document.getElementById('backToTop');
    if (backToTop) {
        window.addEventListener('scroll', () => {
            backToTop.classList.toggle('show', window.scrollY > 300);
        });
        backToTop.addEventListener('click', () => {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });
    }

    // 3. Scroll reveal animation
    const reveals = document.querySelectorAll('.reveal');
    if (reveals.length > 0) {
        const revealObserver = new IntersectionObserver((entries) => {
            entries.forEach((entry, index) => {
                if (entry.isIntersecting) {
                    setTimeout(() => {
                        entry.target.classList.add('visible');
                    }, index * 100);
                    revealObserver.unobserve(entry.target);
                }
            });
        }, { threshold: 0.1 });

        reveals.forEach(el => revealObserver.observe(el));
    }

    // 4. Animated counters
    const counters = document.querySelectorAll('[data-counter]');
    counters.forEach(counter => {
        const target = parseInt(counter.getAttribute('data-counter'));
        const duration = 1500;
        const step = target / (duration / 16);
        let current = 0;

        const update = () => {
            current += step;
            if (current < target) {
                counter.textContent = Math.floor(current).toLocaleString('vi-VN');
                requestAnimationFrame(update);
            } else {
                counter.textContent = target.toLocaleString('vi-VN');
            }
        };

        const observer = new IntersectionObserver((entries) => {
            if (entries[0].isIntersecting) {
                update();
                observer.unobserve(counter);
            }
        });
        observer.observe(counter);
    });

    // 5. Auto-dismiss alerts
    const alerts = document.querySelectorAll('.alert-toast');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            if (bsAlert) bsAlert.close();
        }, 5000);
    });

    // 6. Auto-calculate booking total
    const startDateInput = document.getElementById('StartDate');
    const endDateInput = document.getElementById('EndDate');
    const totalDisplay = document.getElementById('totalDisplay');
    const pricePerDay = document.getElementById('pricePerDay');

    if (startDateInput && endDateInput && totalDisplay && pricePerDay) {
        const calcTotal = () => {
            const start = new Date(startDateInput.value);
            const end = new Date(endDateInput.value);
            const price = parseFloat(pricePerDay.value);
            if (start && end && price && end > start) {
                const days = Math.ceil((end - start) / (1000 * 60 * 60 * 24));
                const total = days * price;
                totalDisplay.textContent = total.toLocaleString('vi-VN') + ' đ';
                totalDisplay.closest('.total-preview')?.classList.add('show');
            }
        };
        startDateInput.addEventListener('change', calcTotal);
        endDateInput.addEventListener('change', calcTotal);
    }

    // 7. Set min date for booking date inputs
    const dateInputs = document.querySelectorAll('input[type="date"]');
    const today = new Date().toISOString().split('T')[0];
    dateInputs.forEach(input => {
        if (input.id === 'StartDate') {
            input.setAttribute('min', today);
        }
    });

});
