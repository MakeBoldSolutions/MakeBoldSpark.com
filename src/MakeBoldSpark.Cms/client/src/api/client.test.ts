import { afterEach, describe, expect, it, vi } from 'vitest';
import type { BaseEntity } from './types';
import { adminCrud } from './client';

interface TestEntity extends BaseEntity {
  name: string;
}

const entity: TestEntity = {
  id: 7,
  name: 'Example',
  createdDate: '2026-01-01T00:00:00Z',
  updatedDate: '2026-01-01T00:00:00Z',
  createdID: 1,
  updatedID: 1,
};

describe('adminCrud', () => {
  afterEach(() => vi.restoreAllMocks());

  it('sends both Authorization and JSON Content-Type for an authenticated update', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      status: 200,
      json: () => Promise.resolve(entity),
    } as Response);

    await adminCrud<TestEntity>('test-entities').update('test-token', entity.id, entity);

    expect(fetchSpy).toHaveBeenCalledWith(
      '/api/admin/makeboldspark/test-entities/7',
      expect.objectContaining({
        method: 'PUT',
        headers: expect.objectContaining({
          Authorization: 'Bearer test-token',
          'Content-Type': 'application/json',
        }),
      }),
    );
  });
});
