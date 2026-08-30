import * as sentry from '@sentry/browser';
import { cleanseData } from './createSentryMiddleware';

function eventFor(message: string) {
  const event = { exception: { values: [{ value: message }] } };

  return cleanseData(
    event as sentry.ErrorEvent,
    {
      originalException: new Error(message),
    } as sentry.EventHint
  );
}

describe('cleanseData', () => {
  it.each([
    'Server returned handshake error: Handshake was canceled.',
    "Failed to start the transport 'WebSockets': Error: WebSocket failed to connect.",
    'The connection was stopped during negotiation.',
    'The underlying connection was closed before the hub handshake could complete.',
    'Unable to connect to the server with any of the available transports.',
    'Server timeout elapsed without receiving a message from the server.',
  ])('drops the SignalR connection error %p', (message) => {
    expect(eventFor(message)).toBeNull();
  });

  it('drops innocuous browser errors', () => {
    expect(eventFor('ResizeObserver loop limit exceeded')).toBeNull();
  });

  it('keeps errors from our own code', () => {
    expect(
      eventFor("Cannot destructure property 'title' of 'r' as it is undefined.")
    ).not.toBeNull();
  });
});
