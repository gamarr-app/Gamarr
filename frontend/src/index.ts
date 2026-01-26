import './polyfills';
import 'Styles/globals.css';
import './index.css';

const initializeUrl = `${
  window.Gamarr.urlBase
}/initialize.json?t=${Date.now()}`;
const response = await fetch(initializeUrl);

window.Gamarr = await response.json();

// @ts-expect-error __webpack_public_path__ is a webpack runtime global
__webpack_public_path__ = `${window.Gamarr.urlBase}/`;

const { bootstrap } = await import('./bootstrap');

await bootstrap();
