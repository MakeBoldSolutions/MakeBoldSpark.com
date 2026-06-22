import { describe, expect, it } from 'vitest';
import { mailSettingConfig, menuConfig, newsletterConfig, subscriberConfig } from './entityConfigs';

describe('US3 entity configurations', () => {
  it('uses the admin-only Subscriber endpoint with the fields needed to manage a subscription', () => {
    expect(subscriberConfig.resource).toBe('subscribers');
    expect(subscriberConfig.publicRead).toBe(false);
    expect(subscriberConfig.fields.map((field) => field.key)).toEqual(
      expect.arrayContaining(['email', 'blogId', 'country', 'region', 'ip']),
    );
  });

  it('makes Newsletter history append-only while preserving deletion of erroneous records', () => {
    expect(newsletterConfig.resource).toBe('newsletters');
    expect(newsletterConfig.publicRead).toBe(false);
    expect(newsletterConfig.allowEdit).toBe(false);
    expect(newsletterConfig.allowDelete).not.toBe(false);
  });

  it('uses an explicitly-entered masked SMTP credential for Mail Settings', () => {
    expect(mailSettingConfig.resource).toBe('mail-settings');
    expect(mailSettingConfig.publicRead).toBe(false);
    expect(mailSettingConfig.fields).toContainEqual(
      expect.objectContaining({ key: 'userPassword', type: 'maskedCredential', required: true }),
    );
  });

  it('uses a scoped parent selector and Markdown editor for menu maintenance', () => {
    expect(menuConfig.fields).toContainEqual(
      expect.objectContaining({ key: 'domainId', hiddenInForm: true }),
    );
    expect(menuConfig.fields).toContainEqual(
      expect.objectContaining({ key: 'parentId', type: 'menuParent' }),
    );
    expect(menuConfig.fields).toContainEqual(
      expect.objectContaining({ key: 'pageContent', type: 'markdown', layout: 'full' }),
    );
  });
});
