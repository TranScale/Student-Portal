$(function () {
    "use strict";

    //sidebar menu js
    $.sidebarMenu($('.sidebar-menu'));

    // === toggle-menu js
    $(".toggle-menu").on("click", function (e) {
        e.preventDefault();
        $("#wrapper").toggleClass("toggled");
    });

    // === sidebar menu activation js
    for (var i = window.location, o = $(".sidebar-menu a").filter(function () {
        return this.href == i;
    }).addClass("active").parent().addClass("active"); ;) {
        if (!o.is("li")) break;
        o = o.parent().addClass("in").parent().addClass("active");
    }

    /* Top Header */
    $(window).on("scroll", function () {
        if ($(this).scrollTop() > 60) {
            $('.topbar-nav .navbar').addClass('bg-dark');
        } else {
            $('.topbar-nav .navbar').removeClass('bg-dark');
        }
    });

    /* Back To Top */
    $(window).on("scroll", function () {
        if ($(this).scrollTop() > 300) {
            $('.back-to-top').fadeIn();
        } else {
            $('.back-to-top').fadeOut();
        }
    });

    $('.back-to-top').on("click", function () {
        $("html, body").animate({ scrollTop: 0 }, 600);
        return false;
    });

    $('[data-toggle="popover"]').popover();
    $('[data-toggle="tooltip"]').tooltip();

    // theme setting
    $(".switcher-icon").on("click", function (e) {
        e.preventDefault();
        $(".right-sidebar").toggleClass("right-toggled");
    });

    // Load theme from LocalStorage when page loads
    var savedTheme = localStorage.getItem('selectedTheme');
    if (savedTheme) {
        $('body').removeClass(function (index, className) {
            return (className.match(/(^|\s)bg-theme\S+/g) || []).join(' '); // Remove old bg-themeX classes
        });
        $('body').addClass('bg-theme bg-theme' + savedTheme); // Apply saved theme (e.g., bg-theme bg-theme2)
        // Highlight the selected switcher item
        $('.switcher li').removeClass('active');
        $('#theme' + savedTheme).addClass('active');
    }

    // Handle click on switcher items to change and save theme
    $('.switcher li').on('click', function () {
        var themeId = $(this).attr('id'); // e.g., 'theme2'
        if (themeId) {
            var themeNumber = themeId.replace('theme', ''); // e.g., '2'
            $('body').removeClass(function (index, className) {
                return (className.match(/(^|\s)bg-theme\S+/g) || []).join(' '); // Remove old bg-themeX classes
            });
            $('body').addClass('bg-theme bg-theme' + themeNumber); // Add new theme classes
            localStorage.setItem('selectedTheme', themeNumber); // Save the number (e.g., '2')
            $('.switcher li').removeClass('active');
            $(this).addClass('active');
        }
    });
});