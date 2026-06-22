import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { MarkdownField } from './MarkdownField';

describe('MarkdownField', () => {
  it('renders a permitted YouTube iframe in the Markdown preview', async () => {
    const user = userEvent.setup();
    render(
      <MarkdownField
        id="page-content"
        label="Page Content"
        value={'<iframe src="https://www.youtube.com/embed/Y530WAkFJGY" title="Example video" allowfullscreen></iframe>'}
        onChange={() => {}}
      />,
    );

    await user.click(screen.getByRole('button', { name: 'Preview Markdown' }));

    expect(await screen.findByTitle('Example video')).toHaveAttribute(
      'src',
      'https://www.youtube.com/embed/Y530WAkFJGY',
    );
  });

  it('does not render an iframe from an untrusted host', async () => {
    const user = userEvent.setup();
    render(
      <MarkdownField
        id="page-content"
        label="Page Content"
        value={'<iframe src="https://example.com/embed/video" title="Unsafe video"></iframe>'}
        onChange={() => {}}
      />,
    );

    await user.click(screen.getByRole('button', { name: 'Preview Markdown' }));

    expect(await screen.findByText('Only HTTPS YouTube embed URLs are allowed in page content.')).toBeInTheDocument();
    expect(screen.queryByTitle('Unsafe video')).not.toBeInTheDocument();
  });
});
