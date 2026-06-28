if (window.Gamarr.analytics) {
  const d = document;
  const g = d.createElement('script');
  const s = d.getElementsByTagName('script')[0];

  g.type = 'text/javascript';
  g.async = true;
  g.defer = true;
  g.src = '//piwik.sonarr.tv/piwik.js';

  if (s) {
    s.parentNode?.insertBefore(g, s);
  }
}
