import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { MaskedCredentialField } from './MaskedCredentialField';

describe('MaskedCredentialField', () => {
  it('masks the credential by default and reveals it only after explicit action (FR-005/FR-006)', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    render(
      <MaskedCredentialField
        id="smtp-password"
        label="SMTP Password"
        value="secret-smtp-credential"
        onChange={onChange}
      />,
    );

    const credential = screen.getByLabelText('SMTP Password');
    expect(credential).toHaveAttribute('type', 'password');
    expect(screen.getByRole('button', { name: 'Reveal' })).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Reveal' }));

    expect(credential).toHaveAttribute('type', 'text');
    expect(screen.getByRole('button', { name: 'Hide' })).toBeInTheDocument();
    expect(onChange).not.toHaveBeenCalled();
  });
});
