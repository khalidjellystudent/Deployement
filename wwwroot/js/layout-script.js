document.addEventListener('DOMContentLoaded', function () {
    // Mobile menu functionality
    const mobileMenuButton = document.getElementById('mobile-menu-button');
    const mobileMenu = document.getElementById('mobile-menu');
    const navLinks = document.querySelectorAll('a[href^="#"]');

    // Toggle mobile menu
    if (mobileMenuButton && mobileMenu) {
        mobileMenuButton.addEventListener('click', function (e) {
            e.stopPropagation();
            const isExpanded = this.getAttribute('aria-expanded') === 'true' || false;
            this.setAttribute('aria-expanded', !isExpanded);
            mobileMenu.classList.toggle('hidden');
            
            // Toggle menu icon between bars and times
            const icon = this.querySelector('i');
            if (icon) {
                if (isExpanded) {
                    icon.classList.remove('fa-times');
                    icon.classList.add('fa-bars');
                } else {
                    icon.classList.remove('fa-bars');
                    icon.classList.add('fa-times');
                }
            }
        });
    }

    // Close mobile menu when clicking outside
    document.addEventListener('click', function (event) {
        if (mobileMenu && !mobileMenu.classList.contains('hidden')) {
            const isClickInsideMenu = mobileMenu.contains(event.target);
            const isClickOnMenuButton = mobileMenuButton && mobileMenuButton.contains(event.target);

            if (!isClickInsideMenu && !isClickOnMenuButton) {
                mobileMenu.classList.add('hidden');
                if (mobileMenuButton) {
                    mobileMenuButton.setAttribute('aria-expanded', 'false');
                    const icon = mobileMenuButton.querySelector('i');
                    if (icon) {
                        icon.classList.remove('fa-times');
                        icon.classList.add('fa-bars');
                    }
                }
            }
        }
    });

    // Smooth scrolling for anchor links
    navLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            const targetId = this.getAttribute('href');
            if (targetId.startsWith('#')) {
                e.preventDefault();
                const targetElement = document.querySelector(targetId);
                if (targetElement) {
                    window.scrollTo({
                        top: targetElement.offsetTop - 100,
                        behavior: 'smooth'
                    });
                    
                    // Close mobile menu if open
                    if (mobileMenu && !mobileMenu.classList.contains('hidden')) {
                        mobileMenu.classList.add('hidden');
                        if (mobileMenuButton) {
                            mobileMenuButton.setAttribute('aria-expanded', 'false');
                            const icon = mobileMenuButton.querySelector('i');
                            if (icon) {
                                icon.classList.remove('fa-times');
                                icon.classList.add('fa-bars');
                            }
                        }
                    }
                }
            }
        });
    });

    // Add shadow to header on scroll
    const header = document.querySelector('header');
    if (header) {
        window.addEventListener('scroll', function() {
            if (window.scrollY > 10) {
                header.classList.add('shadow-lg');
                header.classList.add('bg-opacity-95');
                header.classList.remove('shadow-md');
            } else {
                header.classList.remove('shadow-lg');
                header.classList.remove('bg-opacity-95');
                header.classList.add('shadow-md');
            }
        });
    }

    // Animate elements when they come into view
    const animateOnScroll = function() {
        const elements = document.querySelectorAll('.animate-on-scroll');
        elements.forEach(element => {
            const elementPosition = element.getBoundingClientRect().top;
            const screenPosition = window.innerHeight / 1.3;
            
            if (elementPosition < screenPosition) {
                element.classList.add('opacity-100', 'translate-y-0');
                element.classList.remove('opacity-0', 'translate-y-6');
            }
        });
    };

    // Run once on page load
    animateOnScroll();
    
    // Run on scroll
    window.addEventListener('scroll', animateOnScroll);

    // Handle window resize to ensure proper menu state
    window.addEventListener('resize', function () {
        if (window.innerWidth >= 1024 && mobileMenu) {
            mobileMenu.classList.add('hidden');
            if (mobileMenuButton) {
                mobileMenuButton.setAttribute('aria-expanded', 'false');
                const icon = mobileMenuButton.querySelector('i');
                if (icon) {
                    icon.classList.remove('fa-times');
                    icon.classList.add('fa-bars');
                }
            }
        }
    });

    // Add animation classes to elements with data-animate attribute
    document.querySelectorAll('[data-animate]').forEach(element => {
        const animation = element.getAttribute('data-animate');
        element.classList.add('transition-all', 'duration-500', 'ease-out', 'opacity-0');
        
        switch(animation) {
            case 'fade-up':
                element.classList.add('translate-y-6');
                break;
            case 'fade-down':
                element.classList.add('-translate-y-6');
                break;
            case 'fade-left':
                element.classList.add('translate-x-6');
                break;
            case 'fade-right':
                element.classList.add('-translate-x-6');
                break;
            default:
                element.classList.add('translate-y-6');
        }
        
        // Add to global animate-on-scroll class
        element.classList.add('animate-on-scroll');
    });
});