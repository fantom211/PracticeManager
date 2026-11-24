"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowLeft,
  ChevronRight,
  Download,
  FileText,
  RefreshCw,
  RotateCcw,
  Send,
  Upload,
} from "lucide-react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useMemo, useRef, useState } from "react";
import { DeleteDocumentButton } from "@/components/DeleteDocumentButton";
import { DocumentPreview } from "@/components/DocumentPreview";
import { TeacherCommentList } from "@/components/TeacherCommentList";
import { useToast } from "@/components/ToastProvider";
import {
  Alert,
  Button,
  Card,
  EmptyState,
  ErrorState,
  LoadingScreen,
  StatusPill,
} from "@/components/ui";
import { api, errorMessage, queryKeys } from "@/lib/api";
import { formatBytes, formatDateTime } from "@/lib/format";
import { withParticipant } from "@/lib/navigation";
import { documentTone, sourceTone } from "@/lib/statuses";
import { usePracticeContext } from "@/lib/use-practice-context";
import type { DocumentListItem, DocumentVersion } from "@/lib/types";

const dotColor: Record<string, string> = {
  neutral: "#94A3B8",
  success: "#16A34A",
  warning: "#EA580C",
  danger: "#DC2626",
  info: "#2563EB",
  purple: "#7C3AED",
};

export function DocumentDetailView() {
  const params = useParams<{ id: string }>();
  const id = params.id;
  const queryClient = useQueryClient();
  const { notify } = useToast();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const { selectedId, practice } = usePracticeContext();

  const documentQuery = useQuery({
    queryKey: queryKeys.document(id),
    queryFn: () => api.document(id),
  });
  const documentsQuery = useQuery({
    queryKey: queryKeys.documentList(selectedId ?? undefined),
    queryFn: () => api.documents(selectedId ?? undefined),
  });
  const templatesQuery = useQuery({
    queryKey: queryKeys.templateList(selectedId ?? undefined),
    queryFn: () => api.templates(selectedId ?? undefined),
  });

  const document = documentQuery.data;

  const matchingTemplates = useMemo(
    () =>
      (templatesQuery.data ?? []).filter(
        (item) => (item.documentTypeCode ?? item.documentType) === document?.typeCode,
      ),
    [templatesQuery.data, document?.typeCode],
  );
  const professionalModule = practice?.professionalModule;
  const defaultTemplateId = useMemo(() => {
    const preferred = professionalModule
      ? matchingTemplates.find((item) => item.professionalModule === professionalModule)
      : undefined;
    return preferred?.id ?? matchingTemplates[0]?.id ?? "";
  }, [matchingTemplates, professionalModule]);
  const [selectedTemplateId, setSelectedTemplateId] = useState<string | null>(null);
  const templateId = selectedTemplateId ?? defaultTemplateId;

  const invalidateAll = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.document(id) });
    void queryClient.invalidateQueries({ queryKey: queryKeys.documents });
    void queryClient.invalidateQueries({ queryKey: queryKeys.practices });
    void queryClient.invalidateQueries({ queryKey: queryKeys.notifications });
  };

  const generateMutation = useMutation({
    mutationFn: () => api.generate(id, templateId || null),
    onSuccess: (result) => {
      invalidateAll();
      notify(
        result.warnings.length
          ? `Документ сгенерирован с предупреждениями: ${result.warnings.join("; ")}`
          : "Документ сгенерирован из шаблона.",
        result.warnings.length ? "info" : "success",
      );
    },
    onError: (error: unknown) => notify(errorMessage(error), "error"),
  });

  const uploadMutation = useMutation({
    mutationFn: (file: File) => api.uploadVersion(id, file),
    onSuccess: () => {
      setUploadError(null);
      invalidateAll();
      notify("Новая версия документа загружена.");
    },
    onError: (error: unknown) => notify(errorMessage(error), "error"),
  });

  const submitMutation = useMutation({
    mutationFn: () => api.submit(id),
    onSuccess: () => {
      invalidateAll();
      notify("Документ отправлен на проверку.");
    },
    onError: (error: unknown) => notify(errorMessage(error), "error"),
  });

  const handleFile = (file: File | undefined) => {
    if (!file) return;
    const name = file.name.toLowerCase();
    if (!name.endsWith(".docx") && !name.endsWith(".pdf")) {
      setUploadError("Можно загрузить только файл .docx или .pdf.");
      return;
    }
    if (file.size > 10 * 1024 * 1024) {
      setUploadError("Размер файла не должен превышать 10 МБ.");
      return;
    }
    setUploadError(null);
    uploadMutation.mutate(file);
  };

  if (documentQuery.isError) {
    return <ErrorState message={errorMessage(documentQuery.error)} onRetry={() => void documentQuery.refetch()} />;
  }
  if (documentQuery.isPending || !document) {
    return <LoadingScreen />;
  }

  const isReturned = document.status === "returned";
  const currentVersion = document.versions[0]?.versionNumber ?? null;

  return (
    <>
      <Link
        href={withParticipant("/documents", selectedId)}
        className="inline-flex w-fit items-center gap-1.5 text-[13px] font-medium text-muted hover:text-primary"
      >
        <ArrowLeft size={15} /> К списку документов
      </Link>

      <Card>
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div className="flex min-w-0 items-start gap-4">
            <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-primary-soft text-primary">
              <FileText size={24} aria-hidden="true" />
            </div>
            <div className="min-w-0">
              <h1 className="page-title">{document.name}</h1>
              <p className="page-subtitle">
                {practice?.practiceName ?? "Производственная практика"} ·{" "}
                {practice?.professionalModule ?? document.typeName}
              </p>
              <div className="mt-3 flex flex-wrap items-center gap-2">
                <StatusPill label={document.statusLabel} tone={documentTone(document.status)} />
                {currentVersion && (
                  <span className="text-[12px] text-muted">Текущая версия: v{currentVersion}</span>
                )}
              </div>
            </div>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            {document.downloadUrl && (
              <a href={document.downloadUrl} download className="btn btn-secondary">
                <Download size={16} /> Скачать
              </a>
            )}
            {isReturned && (
              <Link
                href={withParticipant(`/documents/${id}/fill`, selectedId)}
                className="btn btn-danger"
              >
                <RotateCcw size={16} /> Исправить
              </Link>
            )}
            {document.canGenerate && (
              <Button
                icon={RefreshCw}
                variant="secondary"
                disabled={generateMutation.isPending}
                onClick={() => {
                  if (document.versions.length > 0 && !window.confirm("Сгенерировать новую версию документа?")) return;
                  generateMutation.mutate();
                }}
              >
                Сгенерировать из шаблона
              </Button>
            )}
            {document.canUpload && (
              <>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept=".docx,.pdf"
                  className="hidden"
                  onChange={(event) => {
                    handleFile(event.target.files?.[0]);
                    event.target.value = "";
                  }}
                />
                <Button
                  icon={Upload}
                  variant="secondary"
                  disabled={uploadMutation.isPending}
                  onClick={() => fileInputRef.current?.click()}
                >
                  Загрузить новую версию
                </Button>
              </>
            )}
            {document.canSubmit && (
              <Button
                icon={Send}
                variant="primary"
                disabled={submitMutation.isPending}
                onClick={() => submitMutation.mutate()}
              >
                Отправить на проверку
              </Button>
            )}
            {document.canDelete === true && (
              <DeleteDocumentButton
                documentId={document.id}
                documentName={document.name}
                participantId={selectedId}
                navigateOnSuccess
              />
            )}
          </div>
        </div>

        {document.canGenerate && matchingTemplates.length > 0 && (
          <div className="mt-5 flex flex-wrap items-center gap-3 border-t border-line pt-4">
            <label htmlFor="template-select" className="label mb-0">
              Шаблон
            </label>
            <select
              id="template-select"
              className="input max-w-sm"
              value={templateId}
              onChange={(event) => setSelectedTemplateId(event.target.value)}
            >
              {matchingTemplates.map((template) => (
                <option key={template.id} value={template.id}>
                  {template.name} (v{template.version})
                </option>
              ))}
            </select>
          </div>
        )}
        {uploadError && (
          <div className="mt-4">
            <Alert tone="error">{uploadError}</Alert>
          </div>
        )}
      </Card>

      <div className="grid min-h-0 grid-cols-1 items-start gap-5 xl:grid-cols-[220px_minmax(0,1fr)_300px]">
        <Card className="min-w-0 p-4">
          <h2 className="mb-3 text-[13px] font-semibold text-muted">Документы практики</h2>
          {documentsQuery.isPending ? (
            <div className="flex flex-col gap-3">
              <div className="skeleton h-8" />
              <div className="skeleton h-8" />
            </div>
          ) : (
            <nav className="flex flex-col gap-1">
              {(documentsQuery.data ?? []).map((item: DocumentListItem) => (
                <Link
                  key={item.id}
                  href={withParticipant(`/documents/${item.id}`, selectedId)}
                  className={`flex items-center gap-2 rounded-lg px-2.5 py-2 text-[13px] transition-colors ${
                    item.id === id
                      ? "bg-primary-soft font-semibold text-primary"
                      : "text-muted hover:bg-neutral-soft hover:text-ink"
                  }`}
                >
                  <span
                    className="h-2 w-2 shrink-0 rounded-full"
                    style={{
                      backgroundColor: dotColor[documentTone(item.status)] ?? dotColor.neutral,
                    }}
                    aria-hidden="true"
                  />
                  <span className="min-w-0 flex-1 truncate">{item.name}</span>
                  <ChevronRight size={14} className="shrink-0 opacity-50" />
                </Link>
              ))}
            </nav>
          )}
        </Card>

        <div className="min-w-0">
          <DocumentPreview
            previewUrl={document.previewUrl}
            downloadUrl={document.downloadUrl}
            fileName={document.versions[0]?.fileName ?? null}
            version={currentVersion}
          />
        </div>

        <div className="flex min-w-0 flex-col gap-5">
          <Card className="p-5">
            <h2 className="section-title text-[16px]">Комментарии преподавателя</h2>
            <div className="mt-3">
              <TeacherCommentList
                comments={document.comments ?? []}
                documentName={document.name}
                documentVersion={currentVersion}
                isReturned={isReturned}
                legacyComment={document.teacherComment}
                legacyCreatedAt={document.updatedAt}
              />
            </div>
            {document.updatedAt && (
              <p className="mt-4 text-[12px] text-muted">
                Обновлён: {formatDateTime(document.updatedAt)}
              </p>
            )}
          </Card>

          <Card className="p-5">
            <h2 className="section-title text-[16px]">История версий</h2>
            {document.versions.length === 0 ? (
              <div className="mt-4">
                <EmptyState icon={FileText} title="Версий пока нет" />
              </div>
            ) : (
              <ol className="mt-4 flex flex-col gap-4">
                {document.versions.map((version) => (
                  <VersionItem key={version.id} version={version} />
                ))}
              </ol>
            )}
          </Card>
        </div>
      </div>
    </>
  );
}

function VersionItem({ version }: { version: DocumentVersion }) {
  return (
    <li className="relative border-l-2 border-line pl-4">
      <span
        className="absolute -left-[5px] top-1.5 h-2 w-2 rounded-full"
        style={{
          backgroundColor: dotColor[sourceTone(version.source)] ?? dotColor.neutral,
        }}
        aria-hidden="true"
      />
      <div className="flex flex-wrap items-center gap-2">
        <span className="text-[13px] font-semibold text-ink">v{version.versionNumber}</span>
        <StatusPill label={version.sourceLabel} tone={sourceTone(version.source)} dot={false} />
      </div>
      <div className="mt-1 text-[12px] text-muted">
        Студент · {formatDateTime(version.createdAt)} · {formatBytes(version.sizeBytes)}
      </div>
      <div className="mt-1 truncate text-[12px] text-muted" title={version.fileName}>
        {version.fileName}
      </div>
      {version.teacherComment && (
        <div className="mt-2 text-[12px] text-danger">Комментарий: {version.teacherComment}</div>
      )}
      <a
        href={version.downloadUrl}
        download
        className="mt-2 inline-flex items-center gap-1 text-[12px] font-semibold text-primary hover:text-primary-hover"
      >
        <Download size={13} /> Скачать версию
      </a>
    </li>
  );
}
