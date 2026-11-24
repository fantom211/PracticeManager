import type {
  ApiErrorBody,
  DiaryEntry,
  DiaryEntryInput,
  DocumentDetail,
  DocumentListItem,
  DocumentTemplate,
  GenerateDocumentResponse,
  Me,
  NotificationItem,
  PracticeCurrent,
  PracticesResponse,
  TaskItem,
} from "./types";

export class ApiRequestError extends Error {
  readonly status: number;
  readonly body: ApiErrorBody | null;

  constructor(status: number, message: string, body: ApiErrorBody | null = null) {
    super(message);
    this.name = "ApiRequestError";
    this.status = status;
    this.body = body;
  }
}

function headersFor(init?: RequestInit): HeadersInit | undefined {
  if (!init?.body || init.body instanceof FormData) return init?.headers;
  return { "Content-Type": "application/json", ...init?.headers };
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(path, { ...init, headers: headersFor(init) });
  } catch {
    throw new ApiRequestError(0, "Не удалось связаться с сервером. Проверьте, что API запущен.");
  }

  if (!response.ok) {
    let body: ApiErrorBody | null = null;
    try {
      body = (await response.json()) as ApiErrorBody;
    } catch {
      // Non-JSON error response — fall back to the generic message.
    }
    const message = body?.detail ?? body?.title ?? `Ошибка запроса (${response.status}).`;
    throw new ApiRequestError(response.status, message, body);
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export async function fetchBlob(path: string): Promise<Blob> {
  let response: Response;
  try {
    response = await fetch(path);
  } catch {
    throw new ApiRequestError(0, "Не удалось связаться с сервером. Проверьте, что API запущен.");
  }
  if (!response.ok) {
    let detail = `Ошибка загрузки файла (${response.status}).`;
    try {
      const body = (await response.json()) as ApiErrorBody;
      detail = body.detail ?? body.title ?? detail;
    } catch {
      // Ignore.
    }
    throw new ApiRequestError(response.status, detail);
  }
  return response.blob();
}

export const api = {
  me: () => request<Me>("/api/me"),

  practices: () => request<PracticesResponse>("/api/practices"),
  currentPractice: () => request<PracticeCurrent>("/api/practices/current"),
  practiceDetail: (participantId: string) =>
    request<PracticeCurrent>(`/api/practices/${participantId}`),
  practiceTasks: (participantId: string) =>
    request<TaskItem[]>(`/api/practices/${participantId}/tasks`),
  diary: (participantId: string) =>
    request<DiaryEntry[]>(`/api/practices/${participantId}/diary`),
  updateDiary: (participantId: string, entries: DiaryEntryInput[]) =>
    request<DiaryEntry[]>(`/api/practices/${participantId}/diary`, {
      method: "PUT",
      body: JSON.stringify({ entries }),
    }),

  documents: (participantId?: string, status?: string) => {
    const params = new URLSearchParams();
    if (participantId) params.set("participantId", participantId);
    if (status) params.set("status", status);
    const query = params.toString();
    return request<DocumentListItem[]>(`/api/documents${query ? `?${query}` : ""}`);
  },
  document: (id: string) => request<DocumentDetail>(`/api/documents/${id}`),
  deleteDocument: (id: string) =>
    request<void>(`/api/documents/${id}`, { method: "DELETE" }),
  generate: (id: string, templateId?: string | null) =>
    request<GenerateDocumentResponse>(`/api/documents/${id}/generate`, {
      method: "POST",
      body: JSON.stringify({ templateId: templateId ?? null }),
    }),
  uploadVersion: (id: string, file: File) => {
    const form = new FormData();
    form.append("file", file);
    return request<DocumentDetail>(`/api/documents/${id}/versions`, {
      method: "POST",
      body: form,
    });
  },
  saveContent: (id: string, content: string) =>
    request<DocumentDetail>(`/api/documents/${id}/content`, {
      method: "PUT",
      body: JSON.stringify({ content }),
    }),
  submit: (id: string) =>
    request<DocumentDetail>(`/api/documents/${id}/submit`, { method: "POST" }),
  templates: (participantId?: string) => {
    const query = participantId
      ? `?participantId=${encodeURIComponent(participantId)}`
      : "";
    return request<DocumentTemplate[]>(`/api/document-templates${query}`);
  },

  tasks: () => request<TaskItem[]>("/api/tasks"),
  notifications: () => request<NotificationItem[]>("/api/notifications"),
  markNotificationRead: (id: string) =>
    request<NotificationItem>(`/api/notifications/${id}/read`, { method: "POST" }),
  markAllNotificationsRead: () =>
    request<{ updated: number }>("/api/notifications/read-all", { method: "POST" }),
};

export const queryKeys = {
  me: ["me"] as const,
  practices: ["practices"] as const,
  currentPractice: ["practices", "current"] as const,
  practiceDetail: (id: string) => ["practices", "detail", id] as const,
  practiceTasks: (id: string) => ["practices", id, "tasks"] as const,
  diary: (id: string) => ["practices", id, "diary"] as const,
  /** Prefix retained so existing invalidations still refresh every document query. */
  documents: ["documents"] as const,
  documentList: (participantId?: string, status?: string) =>
    ["documents", "list", participantId ?? "all", status ?? "all"] as const,
  document: (id: string) => ["documents", id] as const,
  templates: ["document-templates"] as const,
  templateList: (participantId?: string) =>
    ["document-templates", participantId ?? "all"] as const,
  tasks: ["tasks"] as const,
  notifications: ["notifications"] as const,
};

export function errorMessage(error: unknown): string {
  if (error instanceof ApiRequestError) return error.message;
  if (error instanceof Error) return error.message;
  return "Произошла непредвиденная ошибка.";
}
