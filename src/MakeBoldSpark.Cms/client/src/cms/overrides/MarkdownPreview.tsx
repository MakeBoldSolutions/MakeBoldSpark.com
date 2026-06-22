import Markdown from 'react-markdown';
import rehypeRaw from 'rehype-raw';
import rehypeSanitize, { defaultSchema } from 'rehype-sanitize';

interface MarkdownPreviewProps {
  content: string;
}

const markdownSchema = {
  ...defaultSchema,
  tagNames: [...(defaultSchema.tagNames ?? []), 'iframe'],
  attributes: {
    ...defaultSchema.attributes,
    iframe: ['src', 'title', 'width', 'height', 'allow', 'allowFullScreen', 'allowfullscreen'],
  },
};

function isAllowedYouTubeEmbed(src: string): boolean {
  try {
    const url = new URL(src);
    return url.protocol === 'https:'
      && ['youtube.com', 'www.youtube.com', 'www.youtube-nocookie.com'].includes(url.hostname)
      && url.pathname.startsWith('/embed/');
  } catch {
    return false;
  }
}

function YouTubeEmbed({ src, title, allow, allowFullScreen }: {
  src?: string;
  title?: string;
  allow?: string;
  allowFullScreen?: boolean;
}) {
  if (!src || !isAllowedYouTubeEmbed(src)) {
    return <p>Only HTTPS YouTube embed URLs are allowed in page content.</p>;
  }

  return (
    <iframe
      src={src}
      title={title || 'YouTube video'}
      allow={allow}
      allowFullScreen={allowFullScreen}
      loading="lazy"
      referrerPolicy="strict-origin-when-cross-origin"
    />
  );
}

/** Renders Markdown plus sanitized inline HTML; iframe sources are restricted to YouTube embeds. */
export default function MarkdownPreview({ content }: MarkdownPreviewProps) {
  return (
    <Markdown
      rehypePlugins={[rehypeRaw, [rehypeSanitize, markdownSchema]]}
      components={{
        iframe: ({ src, title, allow, allowFullScreen }) => (
          <YouTubeEmbed
            src={typeof src === 'string' ? src : undefined}
            title={typeof title === 'string' ? title : undefined}
            allow={typeof allow === 'string' ? allow : undefined}
            allowFullScreen={allowFullScreen === true}
          />
        ),
      }}
    >
      {content}
    </Markdown>
  );
}
