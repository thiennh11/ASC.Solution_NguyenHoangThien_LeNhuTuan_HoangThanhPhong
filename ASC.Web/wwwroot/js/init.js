(function ($) {
    $(function () {
        $('.sidenav').sidenav();
        $('.parallax').parallax();
        $('.collapsible').collapsible();

         //✅ Thêm vào đây
        $('#ancrLogout').click(function () {
            $('#logout_form').submit();
        });

        $('#ancrResetPassword').click(function () {
            $('#resetPassword_form').submit();
        });

        // Prevent browser back and forward buttons
        if (window.history && window.history.pushState) {
            window.history.pushState('forward', '', window.location.href);
            $(window).on('popstate', function (e) {
                window.history.pushState('forward', '', window.location.href);
                e.preventDefault();
            });
        }

        // Prevent right-click on entire window
        $(document).ready(function () {
            $(window).on("contextmenu", function () {
                return false;
            });
        });
    });
})(jQuery);