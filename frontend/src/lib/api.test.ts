import { afterEach, describe, expect, it, vi } from 'vitest';
import { getAccessToken, setAccessToken } from './api';

describe('access token storage', () => {
  afterEach(() => {
    setAccessToken(null);
    vi.restoreAllMocks();
  });

  it('keeps access tokens in memory instead of localStorage', () => {
    const setItem = vi.spyOn(Storage.prototype, 'setItem');
    const removeItem = vi.spyOn(Storage.prototype, 'removeItem');

    setAccessToken('token-123');

    expect(getAccessToken()).toBe('token-123');
    expect(setItem).not.toHaveBeenCalled();

    setAccessToken(null);

    expect(getAccessToken()).toBeNull();
    expect(removeItem).not.toHaveBeenCalled();
  });
});
