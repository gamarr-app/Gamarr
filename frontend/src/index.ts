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

const error = console.error;

// Monkey patch console.error to filter out some warnings from React
// TODO: Remove this after the great TypeScript migration

function logError(...parameters: unknown[]) {
  const filter = parameters.find((parameter) => {
    return (
      typeof parameter === 'string' &&
      (parameter.includes(
        'Support for defaultProps will be removed from function components in a future major release'
      ) ||
        parameter.includes(
          'findDOMNode is deprecated and will be removed in the next major release'
        ))
    );
  });

  if (!filter) {
    error(...parameters);
  }
}

console.error = logError;

const { bootstrap } = await import('./bootstrap');

await bootstrap();
