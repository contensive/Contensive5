import { APIRequestContext } from "@playwright/test";

// ── Response types ──────────────────────────────────────────────────────────

interface ContentApiResponse<T = unknown> {
  success: boolean;
  message: string;
  data: T;
}

export interface PageCreateResult {
  pageId: number;
  url: string;
}

export interface PageGetPage {
  name: string;
  headline: string;
  navHeadline: string;
  metaDescription: string;
  metaKeywordList: string;
  structuredData: string;
  pageTitle: string;
  parentId: number;
  templateId: number;
  templateName: string;
}

export interface PageGetLinkAlias {
  id: number;
  url: string;
  isCanonical: boolean;
}

export interface PageGetWidget {
  instanceGuid: string;
  designBlockTypeGuid: string;
  designBlockTypeName: string;
  position: number;
  hasInstanceContent: boolean;
  contentExists: boolean;
  contentName: string;
  fields: Record<string, string>;
  columns: unknown[];
}

export interface PageGetResult {
  url: string;
  pageId: number;
  queryStringSuffix: string;
  page: PageGetPage;
  linkAliases: PageGetLinkAlias[];
  widgets: PageGetWidget[];
}

export interface PageUpdateResult {
  pageId: number;
  url: string;
}

export interface PageListItem {
  url: string;
  pageId: number;
  queryStringSuffix: string;
  navHeadline: string;
  isDynamicVariant: boolean;
  children: PageListItem[];
}

export interface PageAliasAddResult {
  pageId: number;
  newAlias: string;
  canonicalUrl?: string;
  isCanonical: boolean;
}

export interface PageUpdateFields {
  headline: string;
  navHeadline: string;
  metaDescription: string;
  metaKeywordList: string;
  structuredData: string;
  pageTitle: string;
  templateId: number;
  parentId: number;
  allowInMenus: boolean;
  blockPage: boolean;
}

// ── Helper class ────────────────────────────────────────────────────────────

/**
 * Wraps Contensive Content API endpoints for E2E test data management.
 *
 * All methods require an authenticated APIRequestContext (admin session cookies).
 * Endpoints are invoked as path segments: `/{baseURL}/content-api-<name>`.
 */
export class ContentApi {
  constructor(
    private request: APIRequestContext,
    private baseURL: string
  ) {}

  /** Create a new page under the given parent URL. */
  async pageCreate(
    parentUrl: string,
    navHeadline: string,
    options?: { headline?: string; templateId?: number }
  ): Promise<PageCreateResult> {
    const body: Record<string, unknown> = { parentUrl, navHeadline };
    if (options?.headline) { body.headline = options.headline; }
    if (options?.templateId) { body.templateId = options.templateId; }
    return this.post<PageCreateResult>("content-api-page-create", body);
  }

  /** Get page metadata, widgets, and link aliases by URL. */
  async pageGet(url: string): Promise<PageGetResult> {
    return this.get<PageGetResult>("content-api-page-get", { url });
  }

  /** Update page fields by URL. Only non-null fields are changed. */
  async pageUpdate(
    url: string,
    fields: Partial<PageUpdateFields>
  ): Promise<PageUpdateResult> {
    return this.post<PageUpdateResult>("content-api-page-update", { url, ...fields });
  }

  /** List all pages as a hierarchical tree. */
  async pageList(): Promise<PageListItem[]> {
    return this.get<PageListItem[]>("content-api-page-list");
  }

  /** Add a link alias (URL) to an existing page. */
  async pageAliasAdd(
    url: string,
    newAlias: string,
    makeCanonical = true
  ): Promise<PageAliasAddResult> {
    return this.post<PageAliasAddResult>("content-api-page-alias-add", {
      url,
      newAlias,
      makeCanonical
    });
  }

  // ── Internal ────────────────────────────────────────────────────────────

  private async get<T>(endpoint: string, params?: Record<string, string>): Promise<T> {
    const base = this.baseURL.replace(/\/+$/, "");
    let url = `${base}/${endpoint}`;
    if (params) {
      const qs = Object.entries(params)
        .map(([k, v]) => `${k}=${encodeURIComponent(v)}`)
        .join("&");
      url += `?${qs}`;
    }
    const response = await this.request.get(url);
    return this.parseResponse<T>(response, endpoint);
  }

  private async post<T>(endpoint: string, body: Record<string, unknown>): Promise<T> {
    const base = this.baseURL.replace(/\/+$/, "");
    const url = `${base}/${endpoint}`;
    const response = await this.request.post(url, {
      data: body,
      headers: { "Content-Type": "application/json" }
    });
    return this.parseResponse<T>(response, endpoint);
  }

  private async parseResponse<T>(response: { ok(): boolean; status(): number; text(): Promise<string> }, method: string): Promise<T> {
    const text = await response.text();
    if (!response.ok()) {
      throw new Error(`Content API ${method} returned HTTP ${response.status()}: ${text}`);
    }
    let parsed: ContentApiResponse<T>;
    try {
      parsed = JSON.parse(text);
    } catch {
      throw new Error(`Content API ${method} returned non-JSON: ${text.substring(0, 200)}`);
    }
    if (!parsed.success) {
      throw new Error(`Content API ${method} failed: ${parsed.message}`);
    }
    return parsed.data;
  }
}
