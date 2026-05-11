(function () {
  const toggle = document.querySelector('.nav-toggle');
  const headerRight = document.querySelector('.header-right');
  if (!toggle || !headerRight) return;

  toggle.addEventListener('click', () => {
    const open = headerRight.classList.toggle('open');
    toggle.classList.toggle('open', open);
    toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
  });

  document.querySelectorAll('.has-dropdown > a').forEach(link => {
    link.addEventListener('click', e => {
      if (window.matchMedia('(max-width: 900px)').matches) {
        e.preventDefault();
        link.parentElement.classList.toggle('open');
      }
    });
  });

  window.addEventListener('resize', () => {
    if (!window.matchMedia('(max-width: 900px)').matches) {
      headerRight.classList.remove('open');
      toggle.classList.remove('open');
      toggle.setAttribute('aria-expanded', 'false');
      document.querySelectorAll('.has-dropdown.open').forEach(li => li.classList.remove('open'));
    }
  });
})();
