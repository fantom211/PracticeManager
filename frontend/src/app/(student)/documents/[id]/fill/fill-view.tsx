"use client";

import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, BriefcaseBusiness, Plus, Send, Trash2 } from "lucide-react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useMemo, useState } from "react";
import { FileDropzone } from "@/components/FileDropzone";
import { useToast } from "@/components/ToastProvider";
import { Alert, Button, Card, ErrorState, LoadingScreen, StatusPill } from "@/components/ui";
import { api, errorMessage, queryKeys } from "@/lib/api";
import { formatDate, toDateInput } from "@/lib/format";
import { withParticipant } from "@/lib/navigation";
import { documentTone } from "@/lib/statuses";
import { usePracticeContext } from "@/lib/use-practice-context";
import type { DiaryEntryInput } from "@/lib/types";

interface EditableDiaryRow extends DiaryEntryInput {
  key: string;
}

export function FillView() {
  const params = useParams<{ id: string }>();
  const id = params.id;
  const queryClient = useQueryClient();
  const { notify } = useToast();
  const { selectedId, practice } = usePracticeContext();

  const documentQuery = useQuery({
    queryKey: queryKeys.document(id),
    queryFn: () => api.document(id),
  });
  const meQuery = useQuery({ queryKey: queryKeys.me, queryFn: api.me });
  const diaryQuery = useQuery({
    queryKey: queryKeys.diary(selectedId ?? "none"),
    queryFn: () => api.diary(selectedId as string),
    enabled: Boolean(selectedId),
  });

  const document = documentQuery.data;
  const isDiary = document?.typeCode === "diary";
  const isReport = document?.typeCode === "report";

  const [rows, setRows] = useState<EditableDiaryRow[]>([]);
  const [content, setContent] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);
  const [validationError, setValidationError] = useState<string | null>(null);

  // Render-phase synchronisation keeps the form in step with freshly loaded or re-fetched data
  // without discarding the student's in-progress edits.
  const serverContent = document?.content ?? "";
  const [syncedContent, setSyncedContent] = useState<string | null>(null);
  if (document && serverContent !== syncedContent) {
    setSyncedContent(serverContent);
    setContent(serverContent);
  }

  const diaryData = isDiary ? diaryQuery.data : undefined;
  const diarySignature = diaryData
    ? diaryData
        .map(
          (entry) =>
            `${entry.id}|${entry.date}|${entry.workDescription}|${entry.supervisorMark ?? ""}`,
        )
        .join("||")
    : "";
  const [syncedDiary, setSyncedDiary] = useState<string | null>(null);
  if (diaryData && diarySignature !== syncedDiary) {
    setSyncedDiary(diarySignature);
    setRows(
      diaryData.map((entry) => ({
        key: entry.id,
        date: toDateInput(entry.date),
        workDescription: entry.workDescription,
        supervisorMark: entry.supervisorMark,
      })),
    );
  }

  const canSubmit = useMemo(
    () => Boolean(document && (document.versions.length > 0 || file)),
    [document, file],
  );

  if (documentQuery.isError) {
    return <ErrorState message={errorMessage(documentQuery.error)} onRetry={() => void documentQuery.refetch()} />;
  }
  if (documentQuery.isPending || !document) return <LoadingScreen />;

  const validate = (): boolean => {
    if (isDiary) {
      if (rows.length === 0) {
        setValidationError("Добавьте хотя бы одну запись в дневник.");
        return false;
      }
      if (rows.some((row) => !row.date || !row.workDescription.trim())) {
        setValidationError("Заполните дату и выполненную работу для каждой записи.");
        return false;
      }
    }
    if (isReport && !content.trim()) {
      setValidationError("Заполните содержание отчёта.");
      return false;
    }
    if (file && file.size > 10 * 1024 * 1024) {
      setValidationError("Размер файла не должен превышать 10 МБ.");
      return false;
    }
    setValidationError(null);
    return true;
  };

  const handleSave = async (submitAfter: boolean) => {
    if (!validate()) return;
    setSaving(true);
    setServerError(null);
    try {
      if (isDiary && selectedId) {
        const entries = rows.map(({ date, workDescription, supervisorMark }) => ({
          date,
          workDescription: workDescription.trim(),
          supervisorMark: supervisorMark?.trim() || null,
        }));
        await api.updateDiary(selectedId, entries);
      }
      if (isReport) {
        await api.saveContent(id, content);
      }
      if (file) {
        await api.uploadVersion(id, file);
        setFile(null);
      }
      if (submitAfter) {
        await api.submit(id);
      }
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.document(id) }),
        queryClient.invalidateQueries({ queryKey: queryKeys.documents }),
        queryClient.invalidateQueries({ queryKey: queryKeys.practices }),
        queryClient.invalidateQueries({ queryKey: queryKeys.notifications }),
        ...(selectedId
          ? [queryClient.invalidateQueries({ queryKey: queryKeys.diary(selectedId) })]
          : []),
      ]);
      notify(
        submitAfter
          ? "Документ сохранён и отправлен на проверку."
          : "Черновик сохранён.",
      );
    } catch (error: unknown) {
      setServerError(errorMessage(error));
      notify(errorMessage(error), "error");
    } finally {
      setSaving(false);
    }
  };

  const updateRow = (key: string, patch: Partial<EditableDiaryRow>) => {
    setRows((current) => current.map((row) => (row.key === key ? { ...row, ...patch } : row)));
  };

  return (
    <>
      <Link
        href={withParticipant("/documents", selectedId)}
        className="inline-flex w-fit items-center gap-1.5 text-[13px] font-medium text-muted hover:text-primary"
      >
        <ArrowLeft size={15} /> К списку документов
      </Link>

      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="page-title">{document.name}</h1>
          <p className="page-subtitle">Заполнение документа по практике</p>
          <div className="mt-3">
            <StatusPill label={document.statusLabel} tone={documentTone(document.status)} />
          </div>
        </div>
      </div>

      {practice && (
        <Card className="flex flex-wrap items-center gap-4 bg-primary-soft p-4">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-white text-primary">
            <BriefcaseBusiness size={20} aria-hidden="true" />
          </div>
          <div className="min-w-0 flex-1">
            <div className="text-[14px] font-semibold text-ink">{practice.practiceName}</div>
            <div className="text-[12px] text-muted">{practice.professionalModule}</div>
          </div>
          <div className="text-[12px] text-muted">
            {formatDate(practice.dateStart)} — {formatDate(practice.dateEnd)}
          </div>
          <div className="text-[12px] font-medium text-ink">{practice.organization.name}</div>
        </Card>
      )}

      <Card>
        <div className="card-header">
          <div>
            <h2 className="section-title">Сведения о практике</h2>
            <p className="section-subtitle">Данные заполняются автоматически</p>
          </div>
        </div>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <ReadOnlyField label="ФИО студента" value={meQuery.data?.fullName ?? "—"} />
          <ReadOnlyField label="Группа" value={meQuery.data?.group ?? "—"} />
          <ReadOnlyField
            label="Руководитель от организации"
            value={practice?.organizationSupervisor.fullName ?? "—"}
          />
          <ReadOnlyField
            label="Руководитель от учебного учреждения"
            value={practice?.collegeSupervisor.fullName ?? "—"}
          />
        </div>
      </Card>

      {isDiary && (
        <Card>
          <div className="card-header">
            <div>
              <h2 className="section-title">Дневник практики</h2>
              <p className="section-subtitle">Записи о выполненной работе</p>
            </div>
          </div>
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th className="w-[170px]">Дата</th>
                  <th>Выполненная работа</th>
                  <th className="w-[180px]">Примечание</th>
                  <th className="actions w-[56px]" aria-label="Действие" />
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.key}>
                    <td>
                      <input
                        type="date"
                        className="input"
                        value={row.date}
                        aria-label="Дата записи"
                        onChange={(event) => updateRow(row.key, { date: event.target.value })}
                      />
                    </td>
                    <td>
                      <input
                        type="text"
                        className="input"
                        value={row.workDescription}
                        placeholder="Опишите выполненную работу"
                        aria-label="Выполненная работа"
                        onChange={(event) =>
                          updateRow(row.key, { workDescription: event.target.value })
                        }
                      />
                    </td>
                    <td>
                      <input
                        type="text"
                        className="input"
                        value={row.supervisorMark ?? ""}
                        placeholder="Примечание"
                        aria-label="Примечание"
                        onChange={(event) =>
                          updateRow(row.key, { supervisorMark: event.target.value })
                        }
                      />
                    </td>
                    <td className="actions">
                      <button
                        type="button"
                        className="icon-btn text-danger"
                        aria-label="Удалить запись"
                        onClick={() => {
                          if (window.confirm("Удалить эту запись дневника?")) {
                            setRows((current) => current.filter((item) => item.key !== row.key));
                          }
                        }}
                      >
                        <Trash2 size={16} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="mt-4">
            <Button
              icon={Plus}
              variant="secondary"
              onClick={() =>
                setRows((current) => [
                  ...current,
                  { key: `new-${Date.now()}-${current.length}`, date: "", workDescription: "", supervisorMark: null },
                ])
              }
            >
              Добавить запись
            </Button>
          </div>
        </Card>
      )}

      {isReport && (
        <Card>
          <label htmlFor="report-content" className="label">
            Содержание отчёта
          </label>
          <textarea
            id="report-content"
            className="textarea"
            value={content}
            onChange={(event) => setContent(event.target.value)}
            placeholder="Опишите работу, выполненную в период практики…"
          />
          <p className="mt-2 text-[12px] text-muted">
            Текст попадёт в сгенерированный документ при нажатии «Сгенерировать из шаблона».
          </p>
        </Card>
      )}

      {!isDiary && !isReport && (
        <Alert tone="info">
          Содержание документа формируется на основе данных практики. При необходимости
          сгенерируйте файл из шаблона на странице просмотра документа.
        </Alert>
      )}

      <Card>
        <FileDropzone
          label="Прикрепите итоговый отчёт (опционально)"
          file={file}
          onFile={setFile}
        />
      </Card>

      {(validationError || serverError) && (
        <div className="flex flex-col gap-2" role="alert">
          {validationError && <Alert tone="error">{validationError}</Alert>}
          {serverError && <Alert tone="error">{serverError}</Alert>}
        </div>
      )}

      <div className="sticky bottom-0 z-10 -mx-2 flex flex-wrap items-center justify-end gap-3 rounded-xl border border-line bg-white/95 px-4 py-3 backdrop-blur">
        <span className="mr-auto text-[12px] text-muted">
          {saving ? "Сохранение…" : "Изменения сохраняются при нажатии кнопки"}
        </span>
        <Button
          variant="secondary"
          disabled={saving || (isDiary && rows.length === 0)}
          onClick={() => void handleSave(false)}
        >
          Сохранить черновик
        </Button>
        <Button
          variant="primary"
          icon={Send}
          disabled={saving || !canSubmit}
          title={!canSubmit ? "Сначала загрузите версию документа" : undefined}
          onClick={() => void handleSave(true)}
        >
          Отправить на проверку
        </Button>
      </div>
    </>
  );
}

function ReadOnlyField({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <span className="label">{label}</span>
      <input className="input" value={value} readOnly aria-label={label} />
    </div>
  );
}
