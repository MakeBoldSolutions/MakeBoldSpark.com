import { useEffect, useState } from 'react';
import type { Blog } from '../../api/types';
import { apiFetch } from '../../api/client';
import { EntityCrudPage } from '../EntityCrudPage';
import { postConfig } from '../entityConfigs';

/** A Blog must be selected before its Posts are shown (FR-011) — Posts aren't scoped without it. */
export function BlogSelector() {
  const [blogs, setBlogs] = useState<Blog[]>([]);
  const [blogId, setBlogId] = useState<number | null>(null);

  useEffect(() => {
    apiFetch<Blog[]>('/api/public/makeboldspark/blogs').then(setBlogs);
  }, []);

  return (
    <div className="cms-blog-selector">
      <label htmlFor="blog-select">Blog</label>
      <select
        id="blog-select"
        value={blogId ?? ''}
        onChange={(e) => setBlogId(e.target.value ? Number(e.target.value) : null)}
      >
        <option value="">Select a blog…</option>
        {blogs.map((blog) => (
          <option key={blog.id} value={blog.id}>
            {blog.title}
          </option>
        ))}
      </select>

      {blogId != null && <EntityCrudPage config={postConfig} listQuery={{ blogId }} />}
    </div>
  );
}
