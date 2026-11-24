"use client";

import { useQuery } from "@tanstack/react-query";
import { Award, CheckCircle2, ClipboardCheck, FileCheck2, TrendingUp } from "lucide-react";
import { useMemo } from "react";
import { api, errorMessage, queryKeys } from "@/lib/api";
import { formatDate } from "@/lib/format";
import {
  Card,
  EmptyState,
  ErrorState,
  PageHeader,
  SkeletonCard,
  StatusPill,
} from "@/components/ui";

export default function ProgressPage() {
  const practicesQuery = useQuery({ queryKey: queryKeys.practices, queryFn: api.practices });
  const documentsQuery = useQuery({
    queryKey: queryKeys.documents,
    queryFn: () => api.documents(),
  });
  const tasksQuery = useQuery({ queryKey: queryKeys.tasks, queryFn: api.tasks });

  const assessment = practicesQuery.data?.current?.assessment ?? null;
  const acceptedDocuments = useMemo(
    () => (documentsQuery.data ?? []).filter((item) => item.status === "accepted").length,
    [documentsQuery.data],
  );
  const documentTotal = documentsQuery.data?.length ?? 0;
  const completedTasks = useMemo(
    () => (tasksQuery.data ?? []).filter((task) => task.status === "accepted").length,
    [tasksQuery.data],
  );
  const taskTotal = tasksQuery.data?.length ?? 0;

  if (practicesQuery.isError) {
    return <ErrorState message={errorMessage(practicesQuery.error)} onRetry={() => void practicesQuery.refetch()} />;
  }
  if (practicesQuery.isPending) {
    return (
      <div className="flex flex-col gap-5">
        <div className="skeleton h-9 w-56" />
        <SkeletonCard lines={3} />
        <SkeletonCard lines={3} />
      </div>
    );
  }

  return (
    <>
      <PageHeader title="Успеваемость" subtitle="Результаты практики и выполнение заданий" />

      <Card>
        <div className="card-header">
          <div className="flex items-center gap-3">
            <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-primary-soft text-primary">
              <Award size={22} aria-hidden="true" />
            </div>
            <div>
              <h2 className="section-title">Оценка за практику</h2>
              <p className="section-subtitle">
                {practicesQuery.data?.current?.practiceName ?? "Текущая практика"}
              </p>
            </div>
          </div>
          {assessment && (
            <StatusPill label={assessment.resultLabel} tone={assessment.result === "passed" ? "success" : "neutral"} />
          )}
        </div>
        {assessment ? (
          <div className="grid grid-cols-1 gap-6 sm:grid-cols-[180px_1fr]">
            <div className="flex flex-col items-center justify-center rounded-xl bg-primary-soft py-6">
              <div className="text-[12px] text-muted">Оценка</div>
              <div className="mt-1 text-[32px] font-bold leading-none text-primary">
                {assessment.grade ?? "—"}
              </div>
            </div>
            <div className="flex flex-col gap-3">
              <InfoRow label="Результат" value={assessment.resultLabel} />
              <InfoRow label="Дата" value={formatDate(assessment.date)} />
              <InfoRow label="Комментарий" value={assessment.comment ?? "—"} />
            </div>
          </div>
        ) : (
          <EmptyState
            icon={Award}
            title="Практика ещё не оценена"
            description="Результат появится после аттестации."
          />
        )}
      </Card>

      <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
        <Card>
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-success-soft text-success">
              <FileCheck2 size={20} />
            </div>
            <h2 className="section-title text-[16px]">Документы</h2>
          </div>
          <div className="mt-5 flex items-end justify-between">
            <span className="text-[13px] text-muted">Принято документов</span>
            <span className="text-[24px] font-bold leading-none text-ink">
              {acceptedDocuments} / {documentTotal}
            </span>
          </div>
          <div className="progress-track mt-3">
            <div
              className="progress-fill"
              style={{ width: `${documentTotal ? (acceptedDocuments / documentTotal) * 100 : 0}%` }}
              role="progressbar"
              aria-valuenow={acceptedDocuments}
              aria-valuemin={0}
              aria-valuemax={documentTotal}
            />
          </div>
          <p className="mt-4 flex items-center gap-2 text-[12px] text-muted">
            <CheckCircle2 size={14} className="text-success" />
            Документы, принятые руководителем практики
          </p>
        </Card>

        <Card>
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-purple-soft text-purple">
              <ClipboardCheck size={20} />
            </div>
            <h2 className="section-title text-[16px]">Задания</h2>
          </div>
          <div className="mt-5 flex items-end justify-between">
            <span className="text-[13px] text-muted">Выполнено заданий</span>
            <span className="text-[24px] font-bold leading-none text-ink">
              {completedTasks} / {taskTotal}
            </span>
          </div>
          <div className="progress-track mt-3">
            <div
              className="progress-fill"
              style={{
                width: `${taskTotal ? (completedTasks / taskTotal) * 100 : 0}%`,
                background: "var(--purple)",
              }}
              role="progressbar"
              aria-valuenow={completedTasks}
              aria-valuemin={0}
              aria-valuemax={taskTotal}
            />
          </div>
          <p className="mt-4 flex items-center gap-2 text-[12px] text-muted">
            <TrendingUp size={14} className="text-purple" />
            Задания со статусом «Принято»
          </p>
        </Card>
      </div>

      {practicesQuery.data?.current && (
        <Card>
          <div className="card-header">
            <h2 className="section-title">Этапы практики</h2>
            <span className="text-[13px] font-semibold text-primary">
              {practicesQuery.data.current.progressPercent}%
            </span>
          </div>
          <div className="flex flex-col gap-4">
            {practicesQuery.data.current.stages.map((stage) => (
              <div key={stage.name} className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <div className="text-[14px] font-medium text-ink">{stage.name}</div>
                  <div className="mt-0.5 text-[12px] text-muted">
                    {practicesQuery.data?.current?.currentStage?.name === stage.name
                      ? "Текущий этап"
                      : ""}
                  </div>
                </div>
                <StatusPill
                  label={stage.statusLabel}
                  tone={
                    stage.status === "completed"
                      ? "info"
                      : stage.status === "inProgress"
                        ? "success"
                        : "neutral"
                  }
                />
              </div>
            ))}
          </div>
        </Card>
      )}
    </>
  );
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-wrap justify-between gap-2 border-b border-line-soft pb-2 last:border-0">
      <span className="text-[13px] text-muted">{label}</span>
      <span className="text-[13px] font-medium text-ink">{value}</span>
    </div>
  );
}
