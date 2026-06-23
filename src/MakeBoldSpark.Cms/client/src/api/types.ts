export interface BaseEntity {
  id: number;
  createdDate: string;
  updatedDate: string;
  createdID: number | null;
  updatedID: number | null;
}

export interface WebSite extends BaseEntity {
  name: string;
  description: string;
  template: string;
  galleryFolder: string;
  domainUrl: string;
  title: string;
  useBreadCrumbUrl: boolean;
  versionNo: number;
  style: string;
  isRecipeSite: boolean;
}

export interface Blog extends BaseEntity {
  title: string;
  description: string;
  theme: string;
  includeFeatured: boolean;
  itemsPerPage: number;
  cover: string | null;
  logo: string | null;
  headerScript: string | null;
  footerScript: string | null;
  analyticsListType: number;
  analyticsPeriod: number;
}

export interface Author extends BaseEntity {
  email: string;
  password: string;
  displayName: string;
  bio: string | null;
  avatar: string | null;
  isAdmin: boolean;
}

export type PostType = number;

export interface Post extends BaseEntity {
  authorId: number;
  blogId: number;
  title: string;
  slug: string;
  description: string;
  content: string;
  cover: string;
  isFeatured: boolean;
  postType: PostType;
  postViews: number;
  published: string;
  rating: number;
  selected: boolean;
}

export interface Category extends BaseEntity {
  content: string;
  description: string;
}

export interface Menu extends BaseEntity {
  displayOrder: number;
  title: string;
  description: string;
  keyWords: string;
  controller: string;
  action: string;
  argument: string | null;
  icon: string;
  url: string;
  pageContent: string;
  domainId: number;
  parentId: number | null;
}

export interface Keyword extends BaseEntity {
  name: string;
  description: string;
}

export interface ContentPart extends BaseEntity {
  title: string;
  description: string;
  content: string;
}

export interface Subscriber extends BaseEntity {
  email: string;
  ip: string;
  country: string;
  region: string;
  blogId: number;
}

export interface Newsletter extends BaseEntity {
  postId: number;
  success: boolean;
}

export interface MailSetting extends BaseEntity {
  host: string;
  port: number;
  userEmail: string;
  /** Never returned by the API; a replacement is entered explicitly before saving. */
  userPassword?: string;
  fromName: string;
  fromEmail: string;
  toName: string;
  enabled: boolean;
  blogId: number;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  displayName: string;
}
