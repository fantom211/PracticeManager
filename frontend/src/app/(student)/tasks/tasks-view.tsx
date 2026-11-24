"use client";

import { useQuery } from "@tanstack/react-query";
import {
  CalendarDays,
  CheckCircle2,
  ClipboardList,
  MessageSquareText,
  Users,
} from "lucide-react";
import { useMemo, useState } from "react";
import { PracticeContextChip } from "@/components/PracticeSelector";
import {
  Card,
  EmptyState,
  ErrorState,
  PageHeader,
  SkeletonCard,
  StatusPill,
} from "@/components/ui";
import { api, errorMessage, queryKeys } from "@/lib/api";
import { formatDate } from "@/lib/format";
import { taskFilters, taskTone } from "@/lib/statuses";
import { usePracticeContext } from "@/lib/use-practice-context";

export function TasksView() {
  const [filter, setFilter] = useState("all");
  const {
    selectedId,
    practice,
    error: practiceError,
    retry: retryPractice,
  } = usePracticeContext();
  const query = useQuery({
    queryKey: queryKeys.practiceTasks(selectedId ?? "none"),
    queryFn: () => api.practiceTasks(selectedId as string),
    enabled: Boolean(selectedId),
  });

  const tasks = useMemo(
    () => (query.data ?? []).filter((task) => filter === "all" || task.status === filter),
    [query.data, filter],
  );

  const retry = () => {
    retryPractice();
    void query.refetch();
  };

  return (
    <>
      <PageHeader
        title="Задания"
        subtitle="Задания на практику от руководителя"
        actions={
          practice ? (
            <PracticeContextChip
              practiceName={practice.practiceName}
              professionalModule={practice.professionalModule}
            />
          ) : undefined
        }
      />

      <div className="flex flex-wrap gap-2">
        {taskFilters.map((item) => (
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

      {query.isError || practiceError ? (
        <ErrorState
          message={errorMessage(query.error ?? practiceError)}
          onRetry={retry}
        />
      ) : query.isPending ? (
        <div className="flex flex-col gap-4">
          <SkeletonCard lines={2} />
          <SkeletonCard lines={2} />
        </div>
      ) : tasks.length === 0 ? (
        <Card>
          <EmptyState
            icon={ClipboardList}
            title="Задания не найдены"
            description={
              filter === "all"
                ? "Назначенных заданий пока нет."
                : "Попробуйте выбрать другой статус."
            }
          />
        </Card>
      ) : (
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
          {tasks.map((task) => (
            <Card key={task.id} className="card-interactive">
              <div className="flex flex-wrap items-start justify-between gap-3">
                <h2 className="text-[16px] font-semibold leading-snug text-ink">
                  {task.title}
                </h2>
                <StatusPill label={task.statusLabel} tone={taskTone(task.status)} />
              </div>
              {task.description && (
                <p className="mt-2 text-[13px] text-muted">{task.description}</p>
              )}
              <div className="mt-4 flex flex-wrap items-center gap-x-5 gap-y-2 text-[12px] text-muted">
                <span className="inline-flex items-center gap-1.5">
                  <CalendarDays size={15} className="text-primary" />
                  Срок: <strong className="font-semibold text-ink">{formatDate(task.deadline)}</strong>
                </span>
                <span className="inline-flex items-center gap-1.5">
                  {task.isGroup ? (
                    <Users size={15} className="text-purple" />
                  ) : (
                    <CheckCircle2 size={15} className="text-primary" />
                  )}
                  {task.isGroup ? "Групповое задание" : "Индивидуальное задание"}
                </span>
              </div>
              {task.teacherComment && (
                <div className="mt-4 flex gap-2 rounded-lg bg-warning-soft px-3 py-2 text-[12px] text-warning">
                  <MessageSquareText size={15} className="mt-0.5 shrink-0" />
                  <span>{task.teacherComment}</span>
                </div>
              )}
            </Card>
          ))}
        </div>
      )}
    </>
  );
}
