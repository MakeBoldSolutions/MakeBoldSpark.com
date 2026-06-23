import type {
  WebSite,
  Blog,
  Author,
  Category,
  Post,
  Menu,
  Keyword,
  ContentPart,
  Subscriber,
  Newsletter,
  MailSetting,
} from '../api/types';
import type { EntityConfig } from './EntityCrudPage';

export const siteConfig: EntityConfig<WebSite> = {
  resource: 'domains',
  label: 'Site',
  columns: [
    { key: 'name', label: 'Name', type: 'text' },
    { key: 'domainUrl', label: 'URL', type: 'text' },
    { key: 'title', label: 'Title', type: 'text' },
  ],
  fields: [
    { key: 'name', label: 'Name', type: 'text', required: true },
    { key: 'title', label: 'Title', type: 'text', required: true },
    { key: 'description', label: 'Description', type: 'textarea', required: true },
    { key: 'domainUrl', label: 'URL', type: 'text', required: true },
    { key: 'template', label: 'Template', type: 'text', required: true },
    { key: 'galleryFolder', label: 'Gallery Folder', type: 'text', required: true },
    { key: 'style', label: 'Style', type: 'text', required: true },
    { key: 'versionNo', label: 'Version', type: 'number', required: true },
    { key: 'useBreadCrumbUrl', label: 'Use Breadcrumb URL', type: 'boolean' },
    { key: 'isRecipeSite', label: 'Is Recipe Site', type: 'boolean' },
  ],
  defaultValues: {
    name: '',
    description: '',
    template: 'bootstrap5',
    galleryFolder: 'images',
    domainUrl: '',
    title: '',
    useBreadCrumbUrl: false,
    versionNo: 1,
    style: 'light',
    isRecipeSite: false,
  },
};

export const blogConfig: EntityConfig<Blog> = {
  resource: 'blogs',
  label: 'Blog',
  columns: [
    { key: 'title', label: 'Title', type: 'text' },
    { key: 'theme', label: 'Theme', type: 'text' },
    { key: 'itemsPerPage', label: 'Items / Page', type: 'number' },
  ],
  fields: [
    { key: 'title', label: 'Title', type: 'text', required: true },
    { key: 'description', label: 'Description', type: 'textarea', required: true },
    { key: 'theme', label: 'Theme', type: 'text', required: true },
    { key: 'itemsPerPage', label: 'Items Per Page', type: 'number', required: true },
    { key: 'includeFeatured', label: 'Include Featured', type: 'boolean' },
    { key: 'cover', label: 'Cover Image URL', type: 'text' },
    { key: 'logo', label: 'Logo URL', type: 'text' },
    { key: 'headerScript', label: 'Header Script', type: 'textarea' },
    { key: 'footerScript', label: 'Footer Script', type: 'textarea' },
    { key: 'analyticsListType', label: 'Analytics List Type', type: 'number' },
    { key: 'analyticsPeriod', label: 'Analytics Period', type: 'number' },
  ],
  defaultValues: {
    title: '',
    description: '',
    theme: 'minimal',
    includeFeatured: false,
    itemsPerPage: 10,
    cover: null,
    logo: null,
    headerScript: null,
    footerScript: null,
    analyticsListType: 0,
    analyticsPeriod: 30,
  },
};

export const authorConfig: EntityConfig<Author> = {
  resource: 'authors',
  label: 'Author',
  columns: [
    { key: 'displayName', label: 'Display Name', type: 'text' },
    { key: 'email', label: 'Email', type: 'text' },
    { key: 'isAdmin', label: 'Admin', type: 'boolean' },
  ],
  fields: [
    { key: 'displayName', label: 'Display Name', type: 'text', required: true },
    { key: 'email', label: 'Email', type: 'text', required: true },
    { key: 'bio', label: 'Bio', type: 'textarea' },
    { key: 'avatar', label: 'Avatar URL', type: 'text' },
    { key: 'isAdmin', label: 'Administrator', type: 'boolean' },
    // Never rendered or round-tripped through the form (FR-006) — the existing hash, carried
    // unchanged in component state from the loaded record, is preserved by the full-replace
    // PUT regardless. Sign-in credentials are provisioned only via the bootstrap-admin CLI.
    { key: 'password', label: 'Password', type: 'password', hiddenInForm: true },
  ],
  defaultValues: {
    email: '',
    password: '',
    displayName: '',
    bio: null,
    avatar: null,
    isAdmin: false,
  },
};

export const categoryConfig: EntityConfig<Category> = {
  resource: 'categories',
  label: 'Category',
  columns: [
    // Category's display-name field is named `content`, not `name` — see
    // makeboldspark-api-guide.md's Gotchas.
    { key: 'content', label: 'Name', type: 'text' },
    { key: 'description', label: 'Description', type: 'text' },
  ],
  fields: [
    { key: 'content', label: 'Name', type: 'text', required: true },
    { key: 'description', label: 'Description', type: 'textarea', required: true },
  ],
  defaultValues: {
    content: '',
    description: '',
  },
};

export const postConfig: EntityConfig<Post> = {
  resource: 'posts',
  label: 'Post',
  columns: [
    { key: 'title', label: 'Title', type: 'text' },
    { key: 'slug', label: 'Slug', type: 'text' },
    { key: 'isFeatured', label: 'Featured', type: 'boolean' },
  ],
  fields: [
    { key: 'title', label: 'Title', type: 'text', required: true },
    { key: 'slug', label: 'Slug', type: 'text', required: true },
    { key: 'description', label: 'Description', type: 'textarea', required: true },
    { key: 'content', label: 'Content', type: 'richtext', required: true },
    { key: 'cover', label: 'Cover Image URL', type: 'text', required: true },
    { key: 'authorId', label: 'Author Id', type: 'number', required: true },
    { key: 'blogId', label: 'Blog Id', type: 'number', required: true },
    { key: 'isFeatured', label: 'Featured', type: 'boolean' },
    { key: 'postType', label: 'Post Type', type: 'number', required: true },
    { key: 'published', label: 'Published Date', type: 'text', required: true },
    { key: 'rating', label: 'Rating', type: 'number' },
    { key: 'selected', label: 'Selected', type: 'boolean' },
  ],
  defaultValues: {
    authorId: 0,
    blogId: 0,
    title: '',
    slug: '',
    description: '',
    content: '',
    cover: '',
    isFeatured: false,
    postType: 1,
    postViews: 0,
    published: new Date().toISOString(),
    rating: 0,
    selected: false,
  },
};

export const menuConfig: EntityConfig<Menu> = {
  resource: 'menus',
  label: 'Menu Item',
  columns: [
    { key: 'title', label: 'Title', type: 'text' },
    { key: 'url', label: 'URL', type: 'text' },
    { key: 'displayOrder', label: 'Order', type: 'number' },
  ],
  fields: [
    { key: 'title', label: 'Title', type: 'text', required: true },
    { key: 'url', label: 'URL', type: 'text', required: true },
    { key: 'description', label: 'Description', type: 'textarea', required: true, layout: 'full' },
    { key: 'icon', label: 'Icon', type: 'text', required: true },
    { key: 'controller', label: 'Controller', type: 'text', required: true },
    { key: 'action', label: 'Action', type: 'text', required: true },
    { key: 'argument', label: 'Argument', type: 'text' },
    { key: 'keyWords', label: 'Keywords', type: 'text', required: true, layout: 'full' },
    { key: 'pageContent', label: 'Page Content', type: 'markdown', required: true, layout: 'full' },
    { key: 'displayOrder', label: 'Display Order', type: 'number', required: true },
    // SiteSelector owns this value and EntityCrudPage applies the selected domainId on create.
    { key: 'domainId', label: 'Site Id', type: 'number', hiddenInForm: true },
    { key: 'parentId', label: 'Parent Page', type: 'menuParent' },
  ],
  defaultValues: {
    displayOrder: 0,
    title: '',
    description: '',
    keyWords: '',
    controller: '',
    action: '',
    argument: null,
    icon: '',
    url: '',
    pageContent: '',
    domainId: 0,
    parentId: null,
  },
};

export const keywordConfig: EntityConfig<Keyword> = {
  resource: 'keywords',
  label: 'Keyword',
  columns: [
    { key: 'name', label: 'Name', type: 'text' },
    { key: 'description', label: 'Description', type: 'text' },
  ],
  fields: [
    { key: 'name', label: 'Name', type: 'text', required: true },
    { key: 'description', label: 'Description', type: 'textarea', required: true },
  ],
  defaultValues: {
    name: '',
    description: '',
  },
};

export const contentPartConfig: EntityConfig<ContentPart> = {
  resource: 'content-parts',
  label: 'Content Part',
  columns: [
    { key: 'title', label: 'Title', type: 'text' },
    { key: 'description', label: 'Description', type: 'text' },
  ],
  fields: [
    { key: 'title', label: 'Title', type: 'text', required: true },
    { key: 'description', label: 'Description', type: 'textarea', required: true },
    { key: 'content', label: 'Content', type: 'richtext', required: true },
  ],
  defaultValues: {
    title: '',
    description: '',
    content: '',
  },
};

export const subscriberConfig: EntityConfig<Subscriber> = {
  resource: 'subscribers',
  label: 'Subscriber',
  publicRead: false,
  columns: [
    { key: 'email', label: 'Email', type: 'text' },
    { key: 'country', label: 'Country', type: 'text' },
    { key: 'region', label: 'Region', type: 'text' },
  ],
  fields: [
    { key: 'email', label: 'Email', type: 'text', required: true },
    { key: 'blogId', label: 'Blog Id', type: 'number', required: true },
    { key: 'country', label: 'Country', type: 'text' },
    { key: 'region', label: 'Region', type: 'text' },
    { key: 'ip', label: 'IP Address', type: 'text' },
  ],
  defaultValues: {
    email: '',
    blogId: 0,
    country: '',
    region: '',
    ip: '',
  },
};

export const newsletterConfig: EntityConfig<Newsletter> = {
  resource: 'newsletters',
  label: 'Newsletter',
  publicRead: false,
  allowEdit: false,
  columns: [
    { key: 'postId', label: 'Post Id', type: 'number' },
    { key: 'success', label: 'Delivered', type: 'boolean' },
  ],
  fields: [
    { key: 'postId', label: 'Post Id', type: 'number', required: true },
    { key: 'success', label: 'Delivered Successfully', type: 'boolean' },
  ],
  defaultValues: {
    postId: 0,
    success: false,
  },
};

export const mailSettingConfig: EntityConfig<MailSetting> = {
  resource: 'mail-settings',
  label: 'Mail Configuration',
  publicRead: false,
  columns: [
    { key: 'host', label: 'SMTP Host', type: 'text' },
    { key: 'fromEmail', label: 'From Email', type: 'text' },
    { key: 'enabled', label: 'Enabled', type: 'boolean' },
  ],
  fields: [
    { key: 'host', label: 'SMTP Host', type: 'text', required: true },
    { key: 'port', label: 'SMTP Port', type: 'number', required: true },
    { key: 'userEmail', label: 'SMTP User Email', type: 'text', required: true },
    // The API never returns the existing password. Requiring an explicit replacement value
    // prevents a full-replace PUT from silently clearing the stored SMTP credential.
    { key: 'userPassword', label: 'SMTP Password (enter to save)', type: 'maskedCredential', required: true },
    { key: 'fromName', label: 'From Name', type: 'text', required: true },
    { key: 'fromEmail', label: 'From Email', type: 'text', required: true },
    { key: 'toName', label: 'Default Recipient Name', type: 'text', required: true },
    { key: 'enabled', label: 'Enabled', type: 'boolean' },
    { key: 'blogId', label: 'Blog Id', type: 'number', required: true },
  ],
  defaultValues: {
    host: '',
    port: 587,
    userEmail: '',
    userPassword: '',
    fromName: '',
    fromEmail: '',
    toName: '',
    enabled: true,
    blogId: 0,
  },
};
