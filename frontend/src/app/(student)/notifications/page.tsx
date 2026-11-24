"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Bell, Check, CheckCheck, ChevronRight } from "lucide-react";
import Link from "next/link";
import { useMemo } from "react";
import { ClientOnly } from "@/components/ClientOnly";
import { useToast } from "@/components/ToastProvider";
import {
  Button,
  Card,
  EmptyState,
  ErrorState,
  PageHeader,
  SkeletonCard,
} from "@/components/ui";
import { api, errorMessage, queryKeys } from "@/lib/api";
import { formatDateTime } from "@/lib/format";

export default function NotificationsPage() {
  const fallback = (
    <div className="flex flex-col gap-4">
      <SkeletonCard lines={2} />
      <SkeletonCard lines={2} />
    </div>
  );

  return (
    <ClientOnly fallback={fallback}>
      <NotificationsContent />
    </ClientOnly>
  );
}

function NotificationsContent() {
  const queryClient = useQueryClient();
  const { notify } = useToast();
  const query = useQuery({
    queryKey: queryKeys.notifications,
    queryFn: api.notifications,
  });

  const unread = useMemo(
    () => (query.data ?? []).filter((item) => !item.isRead).length,
    [query.data],
  );

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.notifications });
    void queryClient.invalidateQueries({ queryKey: queryKeys.practices });
  };

  const readMutation = useMutation({
    mutationFn: (id: string) => api.markNotificationRead(id),
    onSuccess: () => invalidate(),
    onError: (error: unknown) => notify(errorMessage(error), "error"),
  });

  const readAllMutation = useMutation({
    mutationFn: api.markAllNotificationsRead,
    onSuccess: () => {
      invalidate();
      notify("Все уведомления отмечены прочитанными.");
    },
    onError: (error: unknown) => notify(errorMessage(error), "error"),
  });

  return (
    <>
      <PageHeader
        title="Уведомления"
        subtitle={unread > 0 ? `Непрочитанных: ${unread}` : "Все уведомления прочитаны"}
        actions={
          <Button
            icon={CheckCheck}
            variant="secondary"
            disabled={unread === 0 || readAllMutation.isPending}
            onClick={() => readAllMutation.mutate()}
          >
            Прочитать все
          </Button>
        }
      />

      {query.isError ? (
        <ErrorState message={errorMessage(query.error)} onRetry={() => void query.refetch()} />
      ) : query.isPending ? (
        <div className="flex flex-col gap-4">
          <SkeletonCard lines={2} />
          <SkeletonCard lines={2} />
        </div>
      ) : (query.data ?? []).length === 0 ? (
        <Card>
          <EmptyState icon={Bell} title="Уведомлений пока нет" description="Новые события появятся здесь." />
        </Card>
      ) : (
        <Card className="p-0">
          <ul className="divide-y divide-line-soft">
            {query.data?.map((item) => (
              <li
                key={item.id}
                className={`flex flex-wrap items-start gap-4 px-5 py-4 ${item.isRead ? "" : "bg-primary-soft/40"}`}
              >
                <span
                  className={`mt-1 h-2.5 w-2.5 shrink-0 rounded-full ${
                    item.isRead ? "bg-line" : "bg-primary"
                  }`}
                  aria-label={item.isRead ? "Прочитано" : "Не прочитано"}
                />
                <div className="min-w-0 flex-1">
                  <div className="text-[14px] font-semibold text-ink">{item.title}</div>
                  <p className="mt-1 text-[13px] text-muted">{item.message}</p>
                  <div className="mt-2 flex flex-wrap items-center gap-4 text-[12px] text-muted">
                    <span>{formatDateTime(item.createdAt)}</span>
                    {item.link && (
                      <Link
                        href={item.link}
                        className="inline-flex items-center gap-1 font-semibold text-primary hover:text-primary-hover"
                      >
                        Перейти <ChevronRight size={13} />
                      </Link>
                    )}
                  </div>
                </div>
                {!item.isRead && (
                  <Button
                    size="sm"
                    variant="secondary"
                    icon={Check}
                    disabled={readMutation.isPending}
                    onClick={() => readMutation.mutate(item.id)}
                  >
                    Отметить прочитанным
                  </Button>
                )}
              </li>
            ))}
          </ul>
        </Card>
      )}
    </>
  );
}
