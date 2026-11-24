"use client";

import { useQuery } from "@tanstack/react-query";
import { Eye, FileText, Search, Upload } from "lucide-react";
import Link from "next/link";
import { useMemo, useState } from "react";
import { DeleteDocumentButton } from "@/components/DeleteDocumentButton";
import { PracticeContextChip } from "@/components/PracticeSelector";
import {
  Card,
  EmptyState,
  ErrorState,
  PageHeader,
  SkeletonTable,
  StatusPill,
} from "@/components/ui";
import { api, errorMessage, queryKeys } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import { withParticipant } from "@/lib/navigation";
import { documentFilters, documentTone } from "@/lib/statuses";
import { usePracticeContext } from "@/lib/use-practice-context";
import type { DocumentListItem } from "@/lib/types";

export function DocumentsView() {
  const [filter, setFilter] = useState("all");
  const [search, setSearch] = useState("");
  const { selectedId, practice, error: practiceError, retry: retryPractice } = usePracticeContext();

  const query = useQuery({
    queryKey: queryKeys.documentList(selectedId ?? undefined),
    queryFn: () => api.documents(selectedId ?? undefined),
  });

  const documents = useMemo(() => {
    const term = search.trim().toLocaleLowerCase("ru-RU");
    return (query.data ?? []).filter((document) => {
      const matchesFilter = filter === "all" || document.status === filter;
      const matchesSearch =
        !term ||
        document.name.toLocaleLowerCase("ru-RU").includes(term) ||
        document.typeName.toLocaleLowerCase("ru-RU").includes(term);
      return matchesFilter && matchesSearch;
    });
  }, [query.data, filter, search]);

  const retry = () => {
    retryPractice();
    void query.refetch();
  };

  const documentsLink = withParticipant("/documents", selectedId);
  const templatesLink = withParticipant("/templates", selectedId);

  return (
    <>
      <PageHeader
        title="Документы"
        subtitle="Отчётная документация по практике: статусы, версии и комментарии"
        actions={
          practice ? (
            <PracticeContextChip
              practiceName={practice.practiceName}
              professionalModule={practice.professionalModule}
            />
          ) : undefined
        }
      />

      <Card className="p-0">
        <div className="flex flex-col gap-4 p-5">
          <div className="inline-flex w-fit flex-wrap gap-1 rounded-xl bg-neutral-soft p-1" role="group" aria-label="Раздел документов">
            <Link
              href={documentsLink}
              aria-current={practice ? "page" : undefined}
              className="rounded-lg bg-white px-3.5 py-1.5 text-[13px] font-semibold text-primary shadow-sm"
            >
              Мои документы
            </Link>
            <Link
              href={templatesLink}
              className="rounded-lg px-3.5 py-1.5 text-[13px] font-medium text-muted transition-colors hover:bg-white hover:text-ink"
            >
              Шаблоны документов
            </Link>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            {documentFilters.map((item) => (
              <button
                key={item.key}
                type="button"
                onClick={() => setFilter(item.key)}
                className={`rounded-full px-3.5 py-1.5 text-[13px] font-medium transition-colors ${
                  filter === item.key
                    ? "bg-primary text-white"
                    : "bg-neutral-soft text-muted hover:bg-line hover:text-ink"
                }`}
                aria-pressed={filter === item.key}
              >
                {item.label}
              </button>
            ))}
          </div>
          <div className="relative w-full sm:max-w-sm">
            <Search
              size={16}
              className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-muted"
              aria-hidden="true"
            />
            <input
              className="input pl-9"
              type="search"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Поиск по названию документа"
              aria-label="Поиск по названию документа"
            />
          </div>
        </div>

        {query.isError || practiceError ? (
          <div className="px-5 pb-5">
            <ErrorState
              message={errorMessage(query.error ?? practiceError)}
              onRetry={retry}
            />
          </div>
        ) : query.isPending ? (
          <div className="px-5 pb-5">
            <SkeletonTable rows={4} />
          </div>
        ) : documents.length === 0 ? (
          <EmptyState
            icon={FileText}
            title="Документы не найдены"
            description="Измените фильтр или поисковый запрос."
          />
        ) : (
          <div className="table-wrap rounded-none border-x-0 border-b-0">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Документ</th>
                  <th>Статус</th>
                  <th>Версия</th>
                  <th>Обновлён</th>
                  <th>Комментарий</th>
                  <th className="actions">Действие</th>
                </tr>
              </thead>
              <tbody>
                {documents.map((document: DocumentListItem) => (
                  <tr key={document.id}>
                    <td>
                      <Link
                        href={withParticipant(`/documents/${document.id}`, selectedId)}
                        className="flex items-center gap-2 font-medium hover:text-primary"
                      >
                        <FileText size={16} className="shrink-0 text-muted" aria-hidden="true" />
                        {document.name}
                      </Link>
                      <div className="mt-0.5 text-[12px] text-muted">{document.typeName}</div>
                    </td>
                    <td>
                      <StatusPill label={document.statusLabel} tone={documentTone(document.status)} />
                    </td>
                    <td className="text-[13px] text-muted">
                      {document.currentVersionNumber ? `v${document.currentVersionNumber}` : "—"}
                    </td>
                    <td className="whitespace-nowrap text-[13px] text-muted">
                      {formatDateTime(document.updatedAt)}
                    </td>
                    <td className="max-w-[280px] text-[13px] text-muted">
                      {document.teacherComment ? (
                        <span className="break-words">{document.teacherComment}</span>
                      ) : (
                        "—"
                      )}
                    </td>
                    <td className="actions">
                      <div className="flex items-center justify-end gap-1">
                        <Link
                          href={
                            document.status === "notUploaded"
                              ? withParticipant(`/documents/${document.id}/fill`, selectedId)
                              : withParticipant(`/documents/${document.id}`, selectedId)
                          }
                          className="btn btn-ghost btn-sm"
                        >
                          {document.status === "notUploaded" ? (
                            <>
                              <Upload size={15} /> Загрузить
                            </>
                          ) : (
                            <>
                              <Eye size={15} /> Просмотр
                            </>
                          )}
                        </Link>
                        {document.canDelete === true && (
                          <DeleteDocumentButton
                            documentId={document.id}
                            documentName={document.name}
                            participantId={selectedId}
                            variant="icon"
                          />
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </>
  );
}
