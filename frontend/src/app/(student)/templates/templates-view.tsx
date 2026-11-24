"use client";

import { useQuery } from "@tanstack/react-query";
import { Download, FileDown, FileText } from "lucide-react";
import { useMemo } from "react";
import { EmptyState, ErrorState, PageHeader, SkeletonTable } from "@/components/ui";
import { Card } from "@/components/ui";
import { PracticeContextChip } from "@/components/PracticeSelector";
import { api, errorMessage, queryKeys } from "@/lib/api";
import { formatBytes } from "@/lib/format";
import { usePracticeContext } from "@/lib/use-practice-context";
import type { DocumentTemplate } from "@/lib/types";

const typeLabels: Record<string, string> = {
  report: "Отчёт по практике",
  diary: "Дневник практики",
  characteristic: "Характеристика",
  "individual-plan": "Индивидуальный план",
};

function typeLabel(template: DocumentTemplate): string {
  const code = template.documentType ?? template.documentTypeCode ?? "";
  return typeLabels[code] ?? code ?? "—";
}

export function TemplatesView() {
  const { selectedId, practice, isPending: practicePending } = usePracticeContext();
  const query = useQuery({
    queryKey: queryKeys.templateList(selectedId ?? undefined),
    queryFn: () => api.templates(selectedId ?? undefined),
  });

  const templates = useMemo(
    () => (query.data ?? []).filter((template) => template.isActive !== false),
    [query.data],
  );

  return (
    <>
      <PageHeader
        title="Шаблоны документов"
        subtitle="Действующие шаблоны DOCX для отчётной документации"
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
        {query.isError ? (
          <div className="p-5">
            <ErrorState
              message={errorMessage(query.error)}
              onRetry={() => void query.refetch()}
            />
          </div>
        ) : query.isPending || practicePending ? (
          <div className="p-5">
            <SkeletonTable rows={6} />
          </div>
        ) : templates.length === 0 ? (
          <EmptyState
            icon={FileDown}
            title="Шаблоны не найдены"
            description="Активные шаблоны появятся здесь после обновления данных."
          />
        ) : (
          <div className="table-wrap rounded-none border-x-0 border-b-0">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Шаблон</th>
                  <th>Тип документа</th>
                  <th>ПМ</th>
                  <th>Версия</th>
                  <th>Размер</th>
                  <th className="actions">Действие</th>
                </tr>
              </thead>
              <tbody>
                {templates.map((template) => (
                  <tr key={template.id}>
                    <td>
                      <div className="flex items-center gap-2 font-medium">
                        <FileText size={16} className="shrink-0 text-muted" aria-hidden="true" />
                        <span>{template.name}</span>
                      </div>
                      <div className="mt-0.5 text-[12px] text-muted">{template.code}</div>
                    </td>
                    <td className="text-[13px]">{typeLabel(template)}</td>
                    <td className="whitespace-nowrap text-[13px] text-muted">
                      {template.professionalModule ?? "—"}
                    </td>
                    <td className="text-[13px] text-muted">v{template.version}</td>
                    <td className="whitespace-nowrap text-[13px] text-muted">
                      {typeof template.sizeBytes === "number"
                        ? formatBytes(template.sizeBytes)
                        : "—"}
                    </td>
                    <td className="actions">
                      {template.downloadUrl ? (
                        <a
                          href={template.downloadUrl}
                          download
                          className="btn btn-primary btn-sm"
                        >
                          <Download size={15} aria-hidden="true" /> Скачать
                        </a>
                      ) : (
                        <span className="text-[12px] text-muted">Недоступно</span>
                      )}
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
