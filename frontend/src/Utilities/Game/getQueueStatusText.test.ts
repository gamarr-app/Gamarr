import getQueueStatusText from './getQueueStatusText';

jest.mock('Utilities/String/translate', () => ({
  __esModule: true,
  default: (key: string) => key,
}));

jest.mock('Utilities/String/titleCase', () => ({
  __esModule: true,
  default: (input: string) => {
    if (!input) return '';
    return input.charAt(0).toUpperCase() + input.slice(1).toLowerCase();
  },
}));

describe('getQueueStatusText', () => {
  it('should return undefined when queueStatus is undefined', () => {
    expect(getQueueStatusText(undefined)).toBeUndefined();
  });

  it('should return titleCase for queue status', () => {
    expect(getQueueStatusText('queue')).toBe('Queue');
  });

  it('should return titleCase for paused status', () => {
    expect(getQueueStatusText('paused')).toBe('Paused');
  });

  it('should return titleCase for failed status', () => {
    expect(getQueueStatusText('failed')).toBe('Failed');
  });

  it('should return titleCase for downloading status', () => {
    expect(getQueueStatusText('downloading')).toBe('Downloading');
  });

  it('should return Pending for delay status', () => {
    expect(getQueueStatusText('delay')).toBe('Pending');
  });

  it('should return Pending for downloadClientUnavailable status', () => {
    expect(getQueueStatusText('downloadClientUnavailable')).toBe('Pending');
  });

  it('should return Error for warning status', () => {
    expect(getQueueStatusText('warning')).toBe('Error');
  });

  it('should return Pending for completed with importPending state', () => {
    expect(getQueueStatusText('completed', 'importPending')).toBe('Pending');
  });

  it('should return Importing for completed with importing state', () => {
    expect(getQueueStatusText('completed', 'importing')).toBe('Importing');
  });

  it('should return Waiting for completed with failedPending state', () => {
    expect(getQueueStatusText('completed', 'failedPending')).toBe('Waiting');
  });

  it('should return Downloading for completed with unknown state', () => {
    expect(getQueueStatusText('completed', 'unknown')).toBe('Downloading');
  });
});
