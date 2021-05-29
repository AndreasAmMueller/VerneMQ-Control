$(function () {
	$(document).on('click', 'a', event => {
		let isBlank = $(event.currentTarget).hasClass('blank');

		let href = $(event.currentTarget).attr('href');
		if (href)
			href = href.trim();

		if (href === '#' || href === '' || isBlank)
			event.preventDefault();

		if (href !== '#' && href !== '' && isBlank) {
			let site = window.open(href);
			site.focus();
		}
	});

	$(document).on('click', '.toggle-password', event => {
		event.preventDefault();
		let self = $(event.currentTarget);
		let input = self.closest('.input-group').find('input').first();

		if (self.is(':disabled') || input.is(':disabled'))
			return;

		if (input.attr('type') === 'password') {
			input.attr('type', 'text');
			self.html('<span class="fas fa-fw fa-eye-slash"></span>');
		}
		else {
			input.attr('type', 'password');
			self.html('<span class="fas fa-fw fa-eye"></span>');
		}
	});

	$('.modal').each((_, item) => {
		let modal = new bootstrap.Modal(item);
		$(item).data('modal', modal);
	});
});
