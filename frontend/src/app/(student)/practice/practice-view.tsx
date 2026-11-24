"use client";

import { useQuery } from "@tanstack/react-query";
import {
  ArrowRight,
  Award,
  Bell,
  BriefcaseBusiness,
  Building2,
  CalendarDays,
  ClipboardList,
  Eye,
  FileText,
  GraduationCap,
  Upload,
  Users,
} from "lucide-react";
import Link from "next/link";
import { useMemo } from "react";
import { PracticeContextChip, PracticeSelector } from "@/components/PracticeSelector";
import { useToast } from "@/components/ToastProvider";
import {
  Card,
  EmptyState,
  ErrorState,
  PageHeader,
  SkeletonCard,
  SkeletonTable,
  StatusPill,
} from "@/components/ui";
import { api, errorMessage, queryKeys } from "@/lib/api";
import { formatDate, formatDateTime, relativeDays } from "@/lib/format";
import { withParticipant } from "@/lib/navigation";
import { documentTone, participantTone, stageTone } from "@/lib/statuses";
import { usePracticeContext } from "@/lib/use-practice-context";
import type { DocumentListItem, PracticeHistoryItem } from "@/lib/types";

export function PracticeView() {
  const { notify } = useToast();
  const {
    practice,
    selectedId,
    options,
    select,
    isPending: practicePending,
    error: practiceError,
    retry: retryPractice,
  } = usePracticeContext();

  const documentsQuery = useQuery({
    queryKey: queryKeys.documentList(selectedId ?? undefined),
    queryFn: () => api.documents(selectedId ?? undefined),
  });
  const notificationsQuery = useQuery({
    queryKey: queryKeys.notifications,
    queryFn: api.notifications,
  });

  const documents = useMemo(() => documentsQuery.data ?? [], [documentsQuery.data]);
  const unreadNotifications = useMemo(
    () => (notificationsQuery.data ?? []).filter((item) => !item.isRead).slice(0, 2),
    [notificationsQuery.data],
  );

  const retry = () => {
    retryPractice();
    void documentsQuery.refetch();
    void notificationsQuery.refetch();
  };

  if (practiceError) {
    return <ErrorState message={errorMessage(practiceError)} onRetry={retry} />;
  }
  if (practicePending) {
    return (
      <div className="flex flex-col gap-5">
        <div className="skeleton h-9 w-64" />
        <SkeletonCard lines={5} />
        <SkeletonTable />
      </div>
    );
  }

  return (
    <>
      <PageHeader
        title="Моя практика"
        subtitle="Актуальная информация о практике и документах"
        actions={
          practice ? (
            <PracticeContextChip
              practiceName={practice.practiceName}
              professionalModule={practice.professionalModule}
            />
          ) : undefined
        }
      />

      <PracticeSelector options={options} selectedId={selectedId} onSelect={select} />

      <Card className="overflow-hidden p-0">
        {practice ? (
          <div className="grid grid-cols-1 gap-0 lg:grid-cols-[1fr_280px]">
            <div className="p-6">
              <div className="flex flex-wrap items-start gap-4">
                <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-primary text-white">
                  <BriefcaseBusiness size={24} aria-hidden="true" />
                </div>
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-3">
                    <StatusPill
                      label={practice.statusLabel}
                      tone={participantTone(practice.status)}
                    />
                    <span className="text-[12px] text-muted">{practice.practiceType}</span>
                  </div>
                  <h2 className="mt-2 text-[20px] font-semibold leading-snug text-ink">
                    {practice.practiceName}
                  </h2>
                  <p className="mt-1 text-[13px] text-muted">{practice.professionalModule}</p>
                </div>
              </div>

              <div className="mt-6 grid grid-cols-1 gap-x-6 gap-y-4 sm:grid-cols-2">
                <MetaItem
                  icon={CalendarDays}
                  label="Сроки практики"
                  value={`${formatDate(practice.dateStart)} — ${formatDate(practice.dateEnd)}`}
                />
                <MetaItem icon={Building2} label="Организация" value={practice.organization.name} />
                <MetaItem
                  icon={Users}
                  label="Руководитель от организации"
                  value={practice.organizationSupervisor.fullName}
                />
                <MetaItem
                  icon={GraduationCap}
                  label="Руководитель от колледжа"
                  value={practice.collegeSupervisor.fullName}
                />
              </div>
            </div>

            <div className="border-t border-line bg-[#FAFCFF] p-6 lg:border-l lg:border-t-0">
              <div className="text-[12px] font-medium text-muted">Общий статус</div>
              <div className="mt-3 flex items-center gap-2">
                <span className="h-2.5 w-2.5 rounded-full bg-primary" aria-hidden="true" />
                <span className="text-[15px] font-semibold text-ink">{practice.statusLabel}</span>
              </div>
              <div className="mt-6 flex items-end justify-between">
                <span className="text-[12px] text-muted">Выполнение</span>
                <span className="text-[24px] font-bold leading-none text-primary">
                  {practice.progressPercent}%
                </span>
              </div>
              <div className="progress-track mt-2">
                <div
                  className="progress-fill"
                  style={{ width: `${Math.min(100, Math.max(0, practice.progressPercent))}%` }}
                  role="progressbar"
                  aria-valuenow={practice.progressPercent}
                  aria-valuemin={0}
                  aria-valuemax={100}
                />
              </div>
              <div className="mt-4 text-[12px] text-muted">
                Текущий этап: {practice.currentStage?.name ?? "—"}
              </div>
            </div>
          </div>
        ) : (
          <EmptyState
            icon={BriefcaseBusiness}
            title="Нет активной практики"
            description="Как только практика будет назначена, информация появится здесь."
          />
        )}
      </Card>

      <div className="grid grid-cols-1 gap-5 xl:grid-cols-[minmax(0,1fr)_320px]">
        <Card className="min-w-0">
          <div className="card-header">
            <div>
              <h2 className="section-title">Этапы практики</h2>
              <p className="section-subtitle">Ход выполнения по выбранной практике</p>
            </div>
          </div>
          {practice?.stages.length ? (
            <ol className="flex flex-col gap-4 p-5">
              {practice.stages.map((stage, index) => (
                <li key={`${stage.name}-${index}`} className="flex items-start gap-3">
                  <span
                    className={`mt-1 h-2.5 w-2.5 shrink-0 rounded-full ${
                      stageTone(stage.status) === "success"
                        ? "bg-primary"
                        : stageTone(stage.status) === "info"
                          ? "bg-info"
                          : "bg-slate-300"
                    }`}
                    aria-hidden="true"
                  />
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <span className="text-[13px] font-medium text-ink">{stage.name}</span>
                      <StatusPill
                        label={stage.statusLabel}
                        tone={stageTone(stage.status)}
                        dot={false}
                      />
                    </div>
                  </div>
                </li>
              ))}
            </ol>
          ) : (
            <EmptyState icon={ClipboardList} title="Этапы не определены" />
          )}
        </Card>

        <Card>
          <div className="card-header">
            <h2 className="section-title">Оценка</h2>
          </div>
          {practice?.assessment ? (
            <div className="p-5">
              <div className="flex items-start gap-4">
                <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-success-soft text-success">
                  <Award size={22} aria-hidden="true" />
                </div>
                <div className="min-w-0">
                  <div className="text-[24px] font-bold leading-none text-ink">
                    {practice.assessment.grade ?? "—"}
                  </div>
                  <div className="mt-2 text-[13px] font-medium text-ink">
                    {practice.assessment.resultLabel}
                  </div>
                  {practice.assessment.date && (
                    <div className="mt-1 text-[12px] text-muted">
                      {formatDateTime(practice.assessment.date)}
                    </div>
                  )}
                </div>
              </div>
              {practice.assessment.comment && (
                <p className="mt-4 rounded-lg bg-neutral-soft p-3 text-[12px] leading-relaxed text-muted">
                  {practice.assessment.comment}
                </p>
              )}
            </div>
          ) : (
            <EmptyState icon={Award} title="Оценка ещё не выставлена" />
          )}
        </Card>
      </div>

      <Card>
        <div className="card-header">
          <div>
            <h2 className="section-title">Мои документы</h2>
            <p className="section-subtitle">Статусы и сроки сдачи отчётной документации</p>
          </div>
          <Link
            href={withParticipant("/documents", selectedId)}
            className="inline-flex items-center gap-1 text-[13px] font-semibold text-primary hover:text-primary-hover"
          >
            Все документы <ArrowRight size={14} />
          </Link>
        </div>
        {documentsQuery.isError ? (
          <ErrorState
            message={errorMessage(documentsQuery.error)}
            onRetry={() => void documentsQuery.refetch()}
          />
        ) : documentsQuery.isPending ? (
          <SkeletonTable rows={4} />
        ) : documents.length === 0 ? (
          <EmptyState icon={FileText} title="Документы ещё не добавлены" />
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Документ</th>
                  <th>Статус</th>
                  <th>Дата загрузки / обновления</th>
                  <th>Комментарий</th>
                  <th className="actions">Действие</th>
                </tr>
              </thead>
              <tbody>
                {documents.map((document) => (
                  <DocumentRow
                    key={document.id}
                    document={document}
                    participantId={selectedId}
                    onNotify={notify}
                  />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
        <Card>
          <div className="card-header">
            <h2 className="section-title">Ближайшие сроки</h2>
          </div>
          {practice?.deadline ? (
            <div className="flex items-start gap-4">
              <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-primary-soft text-primary">
                <CalendarDays size={22} aria-hidden="true" />
              </div>
              <div>
                <div className="text-[18px] font-semibold text-ink">
                  {formatDate(practice.deadline.date)}
                </div>
                <div className="mt-0.5 text-[13px] text-muted">{practice.deadline.title}</div>
                <span className="mt-2 inline-block rounded-full bg-warning-soft px-2.5 py-1 text-[12px] font-semibold text-warning">
                  {relativeDays(practice.deadline.daysLeft)}
                </span>
              </div>
            </div>
          ) : (
            <EmptyState icon={CalendarDays} title="Ближайших сроков нет" />
          )}
        </Card>

        <Card>
          <div className="card-header">
            <h2 className="section-title">Уведомления</h2>
            <Link
              href="/notifications"
              className="inline-flex items-center gap-1 text-[13px] font-semibold text-primary hover:text-primary-hover"
            >
              Все уведомления <ArrowRight size={14} />
            </Link>
          </div>
          {notificationsQuery.isPending ? (
            <div className="flex flex-col gap-3">
              <div className="skeleton h-12" />
              <div className="skeleton h-12" />
            </div>
          ) : unreadNotifications.length === 0 ? (
            <EmptyState icon={Bell} title="Новых уведомлений нет" />
          ) : (
            <div className="flex flex-col divide-y divide-line-soft">
              {unreadNotifications.map((item) => (
                <Link
                  key={item.id}
                  href={item.link ?? "/notifications"}
                  className="flex gap-3 py-3 first:pt-0 last:pb-0"
                >
                  <span className="mt-1 flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-primary-soft text-primary">
                    <Bell size={16} />
                  </span>
                  <span className="min-w-0">
                    <span className="block truncate text-[13px] font-semibold text-ink">
                      {item.title}
                    </span>
                    <span className="mt-0.5 block truncate text-[12px] text-muted">
                      {item.message}
                    </span>
                    <span className="mt-1 block text-[11px] text-muted">
                      {formatDateTime(item.createdAt)}
                    </span>
                  </span>
                </Link>
              ))}
            </div>
          )}
        </Card>
      </div>

      <HistoryCard
        history={options.slice(1).map((option) => ({
          participantId: option.participantId,
          practiceName: option.practiceName,
          professionalModule: option.professionalModule,
          dateStart: option.dateStart,
          dateEnd: option.dateEnd,
          organization: option.organization,
          status: option.status,
          statusLabel: option.statusLabel,
          grade: option.grade,
          resultLabel: option.statusLabel,
        }))}
        onSelect={select}
      />
    </>
  );
}

function MetaItem({
  icon: Icon,
  label,
  value,
}: {
  icon: typeof CalendarDays;
  label: string;
  value: string;
}) {
  return (
    <div className="flex items-start gap-3">
      <Icon size={18} className="mt-0.5 shrink-0 text-primary" aria-hidden="true" />
      <div className="min-w-0">
        <div className="text-[12px] text-muted">{label}</div>
        <div className="mt-0.5 text-[13px] font-medium text-ink">{value || "—"}</div>
      </div>
    </div>
  );
}

function DocumentRow({
  document,
  participantId,
  onNotify,
}: {
  document: DocumentListItem;
  participantId: string | null;
  onNotify: (message: string, tone?: "success" | "error" | "info") => void;
}) {
  const isEmpty = document.status === "notUploaded";
  return (
    <tr>
      <td>
        <Link
          href={withParticipant(`/documents/${document.id}`, participantId)}
          className="flex items-center gap-2 font-medium hover:text-primary"
        >
          <FileText size={16} className="shrink-0 text-muted" aria-hidden="true" />
          {document.name}
        </Link>
      </td>
      <td>
        <StatusPill label={document.statusLabel} tone={documentTone(document.status)} />
      </td>
      <td className="whitespace-nowrap text-[13px] text-muted">
        {formatDateTime(document.updatedAt)}
      </td>
      <td className="max-w-[220px] text-[13px] text-muted">
        {document.teacherComment ? (
          <span className="break-words">{document.teacherComment}</span>
        ) : (
          "—"
        )}
      </td>
      <td className="actions">
        {isEmpty ? (
          <Link
            href={withParticipant(`/documents/${document.id}/fill`, participantId)}
            className="btn btn-ghost btn-sm"
            onClick={() => onNotify("Загрузите файл или сгенерируйте документ из шаблона.", "info")}
          >
            <Upload size={15} /> Загрузить
          </Link>
        ) : (
          <Link
            href={withParticipant(`/documents/${document.id}`, participantId)}
            className="btn btn-ghost btn-sm"
          >
            <Eye size={15} /> Просмотр
          </Link>
        )}
      </td>
    </tr>
  );
}

function HistoryCard({
  history,
  onSelect,
}: {
  history: PracticeHistoryItem[];
  onSelect: (participantId: string) => void;
}) {
  return (
    <Card>
      <div className="card-header">
        <div>
          <h2 className="section-title">История практик</h2>
          <p className="section-subtitle">Завершённые практики и их результаты</p>
        </div>
      </div>
      {history.length === 0 ? (
        <EmptyState icon={BriefcaseBusiness} title="История практик пуста" />
      ) : (
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>Практика</th>
                <th>Период</th>
                <th>Организация</th>
                <th>Результат</th>
                <th className="actions">Оценка</th>
              </tr>
            </thead>
            <tbody>
              {history.map((item) => (
                <tr key={item.participantId}>
                  <td>
                    <button
                      type="button"
                      onClick={() => onSelect(item.participantId)}
                      className="text-left hover:text-primary"
                    >
                      <div className="font-medium">
                        {item.professionalModule.split(" ").slice(0, 2).join(" ")}
                      </div>
                      <div className="text-[12px] text-muted">{item.practiceName}</div>
                    </button>
                  </td>
                  <td className="whitespace-nowrap text-[13px] text-muted">
                    {formatDate(item.dateStart)} — {formatDate(item.dateEnd)}
                  </td>
                  <td className="text-[13px]">{item.organization}</td>
                  <td>
                    <StatusPill
                      label={item.statusLabel}
                      tone={item.status === "completed" ? "success" : "neutral"}
                    />
                  </td>
                  <td className="actions font-semibold">{item.grade ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Card>
  );
}
